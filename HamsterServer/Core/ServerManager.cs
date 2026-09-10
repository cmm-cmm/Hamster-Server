using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using HamsterServer.Models;

namespace HamsterServer.Core;

public enum ServerStatus { Stopped, Starting, Running, Stopping, Error }

public class ServerManager : IDisposable
{
    private Process? _apacheProcess;
    private Process? _phpProcess;
    private bool _disposed;
    private volatile bool _apacheStopRequested; // set true when WE stop Apache intentionally
    private volatile bool _phpStopRequested;    // set true when WE stop PHP intentionally
    private volatile bool _phpDesiredRunning;
    private readonly SemaphoreSlim _apacheGate = new(1, 1);
    private readonly SemaphoreSlim _phpGate = new(1, 1);
    private readonly object _disposeLock = new();
    private readonly object _phpRestartLock = new();
    private CancellationTokenSource? _phpRestartCts;

    // Watchdog: auto-restart PHP-CGI when it crashes unexpectedly.
    // Throttle: max MaxPhpAutoRestarts attempts within PhpAutoRestartWindowSec seconds.
    private int _phpCrashCount;
    private DateTime _phpFirstCrashTime = DateTime.MinValue;
    private readonly object _phpCrashLock = new();
    private const int MaxPhpAutoRestarts = 5;
    private const int PhpAutoRestartWindowSec = 120;
    private const int PhpAutoRestartDelayMs = 2000;

    // Paths cached so stop/dispose can find surviving child processes
    private string? _httpdExePath;
    private string? _phpCgiPath;

    private readonly ApacheConfigWriter _configWriter;

    public AppConfig Config { get; }
    public string RootDir { get; }

    public ServerStatus ApacheStatus { get; private set; } = ServerStatus.Stopped;
    public ServerStatus PhpStatus { get; private set; } = ServerStatus.Stopped;

    public bool IsApacheRunning => _apacheProcess is { } p && !SafeHasExited(p);
    public bool IsPhpRunning => _phpProcess is { } p && !SafeHasExited(p);

    // PID of the tracked parent process, or null when not running — shown in the Services list.
    public int? ApachePid => SafePid(_apacheProcess);
    public int? PhpPid => SafePid(_phpProcess);

    private static int? SafePid(Process? p)
    {
        if (p is null) return null;
        try { return p.HasExited ? null : p.Id; } catch { return null; }
    }

    /// <summary>Product version of the bundled httpd.exe, or "2.4" when it cannot be read.</summary>
    public string GetApacheVersion()
    {
        if (_apacheVersion is not null) return _apacheVersion;
        var dir = FindApacheDir();
        var exe = dir is null ? null : Path.Combine(dir, "bin", "httpd.exe");
        try
        {
            if (exe != null && File.Exists(exe))
            {
                var info = FileVersionInfo.GetVersionInfo(exe);
                var v = info.ProductVersion ?? info.FileVersion;
                if (!string.IsNullOrWhiteSpace(v)) return _apacheVersion = v.Trim();
            }
        }
        catch { /* fall through to the default below */ }
        return _apacheVersion = "2.4";
    }

    private string? _apacheVersion;

    private static bool SafeHasExited(Process p)
    {
        try { return p.HasExited; } catch { return true; }
    }

    public event EventHandler<string>? LogReceived;
    public event EventHandler? StatusChanged;

    private const int DelayBetweenStopMs = 300;
    private const int DelayRestartPauseMs = 500;
    private const int PortProbeTimeoutMs = 15_000; // Fix 2.2: 8s→15s — đủ cho máy HDD / ít RAM (24 workers cần 10-12s)
    private const int PortProbeIntervalMs = 150;

    public ServerManager(string rootDir, AppConfig config)
    {
        RootDir = rootDir;
        Config = config;
        _configWriter = new ApacheConfigWriter(rootDir, config);
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    public async Task StartAllAsync()
    {
        // PHP and Apache can start concurrently — Apache only connects to FastCGI
        // on demand (per request), not at startup, so no ordering dependency.
        await Task.WhenAll(StartPhpAsync(), StartApacheAsync());
    }

    public async Task StopAllAsync()
    {
        await StopApacheAsync();
        await Task.Delay(DelayBetweenStopMs);
        await StopPhpAsync();
    }

    public async Task RestartAllAsync()
    {
        Log("Restarting all services…");
        await StopAllAsync();
        await Task.Delay(DelayRestartPauseMs);
        await StartAllAsync();
    }

    // ─── Apache ───────────────────────────────────────────────────────────────

    public async Task StartApacheAsync()
    {
        await _apacheGate.WaitAsync();
        try { await StartApacheCoreAsync(); }
        finally { _apacheGate.Release(); }
    }

    private async Task StartApacheCoreAsync()
    {
        if (_disposed) return;
        if (IsApacheRunning) { Log("Apache is already running."); return; }

        var apacheDir = FindApacheDir();
        if (apacheDir is null)
        {
            SetApache(ServerStatus.Error);
            Log($"[ERROR] Apache not found in: {RootDir}");
            Log("[ERROR] Expected: apache\\Apache24\\bin\\httpd.exe  (or Apache\\Apache24, Apache24, Apache)");
            return;
        }

        var httpdExe = Path.Combine(apacheDir, "bin", "httpd.exe");
        _httpdExePath = httpdExe;   // cache for StopApacheAsync / Dispose
        var configDir = Path.Combine(RootDir, "config");
        var httpdConf = Path.Combine(configDir, "httpd.conf");
        var wwwDir = Path.Combine(RootDir, "www");

        Directory.CreateDirectory(wwwDir);
        Directory.CreateDirectory(Path.Combine(configDir, "vhosts"));
        Directory.CreateDirectory(Path.Combine(configDir, "logs"));

        // ── Kill any orphaned Apache from our own installation ─────────────────
        // This handles the case where Hamster Server was closed without stopping Apache
        // (debug stop, task kill, crash). We only kill httpd.exe that belongs to
        // our bundled Apache — never touch other Apache/IIS installations.
        bool killedOrphans = await Task.Run(() => KillOrphanedApache(httpdExe));
        if (killedOrphans)
            await Task.Delay(600);   // give OS time to release the port socket

        // ── Pre-flight: check each port is free BEFORE starting Apache ──────────
        bool sslReady = Config.Apache.EnableSsl &&
                        !string.IsNullOrWhiteSpace(Config.Apache.SslCertFile) &&
                        !string.IsNullOrWhiteSpace(Config.Apache.SslKeyFile);
        var allPorts = new[] { Config.Apache.Port }
            .Concat(sslReady ? [Config.Apache.SslPort] : [])
            .Concat((Config.Apache.ExtraPorts ?? []).Where(p => p.Enabled).Select(p => p.Port))
            .Distinct()
            .ToList();

        if (allPorts.Count > 1)
            Log($"Ports configured: {string.Join(", ", allPorts)}");

        // One snapshot of the TCP listener table answers every port at once — cheaper and more
        // reliable than opening a probe connection per port (a firewall that drops instead of
        // refusing made each probe wait for its full timeout).
        var listeningPorts = await Task.Run(GetListeningPorts);
        bool portConflict = false;
        foreach (var port in allPorts.Where(listeningPorts.Contains))
        {
            var owner = await Task.Run(() => FindPortOwner(port));
            Log($"[WARN] Port {port} is already in use — {owner}");
            portConflict = true;
        }
        if (portConflict)
        {
            Log("[ERROR] Cannot start Apache — port conflict detected.");
            Log("[ERROR] Solutions:");
            Log("[ERROR]   1. Stop the conflicting process (see [WARN] above).");
            Log("[ERROR]   2. Change Apache HTTP Port in ⚙ Settings → Apache tab.");
            SetApache(ServerStatus.Error);
            return;
        }

        // Fix 3.1: cảnh báo nếu EnableBrotli=true nhưng không có mod_brotli.so
        if (Config.Apache.EnableBrotli && !File.Exists(Path.Combine(apacheDir, "modules", "mod_brotli.so")))
            Log("[WARN] Brotli compression enabled but mod_brotli.so not found — Brotli will be skipped." +
                " Place mod_brotli.so in apache/Apache24/modules/ or disable Brotli in Settings.");

        Log("Generating Apache configuration…");
        try
        {
            _configWriter.WriteHttpdConf(httpdConf, apacheDir, wwwDir, configDir);
            _configWriter.WriteAllVHosts(Path.Combine(configDir, "vhosts"));
        }
        catch (Exception ex)
        {
            SetApache(ServerStatus.Error);
            Log($"[ERROR] Cannot generate Apache configuration: {ex.Message}");
            return;
        }

        Log("Testing Apache config…");
        var (ok, output) = await RunCaptureAsync(httpdExe, $"-t -f \"{httpdConf}\"", apacheDir);
        if (!string.IsNullOrWhiteSpace(output))
            Log($"Config test: {output.Trim()}");
        if (!ok)
        {
            SetApache(ServerStatus.Error);
            Log("[ERROR] Config test failed — see output above.");
            return;
        }

        SetApache(ServerStatus.Starting);
        Log("Starting Apache…");

        var apacheConfigDir = Path.Combine(RootDir, "config");

        _apacheProcess?.Dispose();
        var apacheProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = httpdExe,
                Arguments = $"-f \"{httpdConf}\"",
                WorkingDirectory = apacheDir,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,   // capture startup errors
                RedirectStandardOutput = true
            },
            EnableRaisingEvents = true
        };
        _apacheProcess = apacheProcess;

        // Capture any startup output Apache writes to stderr/stdout
        apacheProcess.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data)) Log($"[Apache] {e.Data}");
        };
        apacheProcess.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data)) Log($"[Apache] {e.Data}");
        };

        apacheProcess.Exited += (_, _) =>
        {
            bool wasIntentional = _apacheStopRequested;
            _apacheStopRequested = false;

            SetApache(ServerStatus.Stopped);
            Log("Apache process stopped.");

            // Only show the error log when Apache crashed unexpectedly.
            // Intentional stops (Kill()) produce normal "AH00341 / AH02538" warnings
            // that look alarming but are not real errors — suppress them.
            if (!wasIntentional)
            {
                var logPath = Path.Combine(apacheConfigDir, "logs", "error.log");
                Task.Run(async () =>
                {
                    await Task.Delay(300);
                    var lastErrors = ReadLastErrorLog(logPath);
                    if (!string.IsNullOrWhiteSpace(lastErrors))
                        Log($"[ERROR] Apache crashed. error.log (last entries):\n{lastErrors}");
                });
            }
        };

        using var cts = new CancellationTokenSource();
        void apacheExited(object? s, EventArgs e) { try { cts.Cancel(); } catch (ObjectDisposedException) { } }
        apacheProcess.Exited += apacheExited;  // register BEFORE Start to avoid race condition

        try
        {
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    apacheProcess.Dispose();
                    if (ReferenceEquals(_apacheProcess, apacheProcess))
                        _apacheProcess = null;
                    SetApache(ServerStatus.Stopped);
                    return;
                }
                apacheProcess.Start();
                apacheProcess.BeginErrorReadLine();
                apacheProcess.BeginOutputReadLine();
            }
        }
        catch (Exception ex)
        {
            apacheProcess.Exited -= apacheExited;
            apacheProcess.Dispose();
            if (ReferenceEquals(_apacheProcess, apacheProcess))
                _apacheProcess = null;
            SetApache(ServerStatus.Error);
            Log($"[ERROR] Cannot start Apache: {ex.Message}");
            return;
        }

        bool ready = await WaitForPortAsync(Config.Apache.Port, cts.Token);
        apacheProcess.Exited -= apacheExited;
        if (IsApacheRunning && ready)
        {
            SetApache(ServerStatus.Running);
            Log("[OK] Apache running");

            // Primary HTTP port — already confirmed above
            Log($"[OK] Port {Config.Apache.Port} ready. (Primary HTTP)  → http://localhost:{Config.Apache.Port}");

            // Fix 2.1: probe SSL + extra ports in parallel (was sequential → 2s×N wait)
            var supplementaryProbes = new List<Task>();

            if (Config.Apache.EnableSsl)
            {
                supplementaryProbes.Add(Task.Run(async () =>
                {
                    using var sslCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    bool ok = await WaitForPortAsync(Config.Apache.SslPort, sslCts.Token);
                    if (ok)
                        Log($"[OK] Port {Config.Apache.SslPort} ready. (Primary HTTPS)  → https://localhost:{Config.Apache.SslPort}");
                    else
                        Log($"[WARN] Port {Config.Apache.SslPort} not responding — check SSL cert config." +
                            $" Run: netstat -ano | findstr :{Config.Apache.SslPort}");
                }));
            }

            foreach (var ep in (Config.Apache.ExtraPorts ?? []).Where(p => p.Enabled))
            {
                var capturedEp = ep;
                supplementaryProbes.Add(Task.Run(async () =>
                {
                    using var epCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    bool ok = await WaitForPortAsync(capturedEp.Port, epCts.Token);
                    var scheme = capturedEp.IsSsl ? "https" : "http";
                    var kind = capturedEp.IsSsl ? "Extra HTTPS" : "Extra HTTP";
                    if (ok)
                        Log($"[OK] Port {capturedEp.Port} ready. ({kind})  → {scheme}://localhost:{capturedEp.Port}");
                    else
                        Log($"[WARN] Port {capturedEp.Port} not responding — may already be used by another app." +
                            $" Run: netstat -ano | findstr :{capturedEp.Port}");
                }));
            }

            if (supplementaryProbes.Count > 0)
                await Task.WhenAll(supplementaryProbes);
        }
        else
        {
            if (IsApacheRunning) await StopApacheCoreAsync();
            SetApache(ServerStatus.Error);
            Log("[ERROR] Apache failed to start. See config/logs/error.log");
        }
    }

    public async Task StopApacheAsync()
    {
        await _apacheGate.WaitAsync();
        try { await StopApacheCoreAsync(); }
        finally { _apacheGate.Release(); }
    }

    private async Task StopApacheCoreAsync()
    {
        if (!IsApacheRunning)
        {
            _apacheProcess?.Dispose();
            _apacheProcess = null;
            SetApache(ServerStatus.Stopped);
            return;
        }

        SetApache(ServerStatus.Stopping);
        Log("Stopping Apache…");
        _apacheStopRequested = true;    // signal Exited handler: this is intentional

        try
        {
            _apacheProcess?.Kill(entireProcessTree: true);
            await Task.Run(() => _apacheProcess?.WaitForExit(10_000)); // Fix 2.3: 5s→10s graceful shutdown
        }
        catch (Exception ex)
        {
            Log($"[WARN] Apache stop error: {ex.Message}");
        }

        // mpm_winnt spawns a child httpd.exe that may outlive the parent.
        // Kill any surviving httpd processes from our installation.
        if (_httpdExePath != null)
            KillSurvivingChildren("httpd", _httpdExePath);

        _apacheProcess?.Dispose();
        _apacheProcess = null;
        SetApache(ServerStatus.Stopped);
        Log("Apache stopped.");
    }

    // ─── PHP-CGI FastCGI ──────────────────────────────────────────────────────

    public async Task StartPhpAsync()
    {
        _phpDesiredRunning = true;
        CancelPendingPhpRestart();
        await _phpGate.WaitAsync();
        try { await StartPhpCoreAsync(); }
        finally { _phpGate.Release(); }
    }

    private async Task StartPhpCoreAsync()
    {
        if (_disposed) return;
        if (IsPhpRunning) { Log("PHP-CGI is already running."); return; }

        if (string.IsNullOrWhiteSpace(Config.Php.CurrentVersion))
        {
            SetPhp(ServerStatus.Error);
            Log("[ERROR] No PHP version selected.");
            Log($"[ERROR] Expected: php\\<version>\\php-cgi.exe  (e.g. {Path.Combine(RootDir, "php", "8.3", "php-cgi.exe")})");
            return;
        }

        var phpDir = Path.Combine(RootDir, "php", Config.Php.CurrentVersion);
        var phpCgi = Path.Combine(phpDir, "php-cgi.exe");
        _phpCgiPath = phpCgi;       // cache for StopPhpAsync / Dispose

        if (!File.Exists(phpCgi))
        {
            SetPhp(ServerStatus.Error);
            Log($"[ERROR] Not found: {phpCgi}");
            return;
        }

        var configDir = Path.Combine(RootDir, "config");
        try
        {
            _configWriter.WritePhpIni(phpDir, configDir);
        }
        catch (Exception ex)
        {
            SetPhp(ServerStatus.Error);
            Log($"[ERROR] Cannot generate php.ini: {ex.Message}");
            return;
        }

        // Kill orphaned php-cgi workers from our installation (leftover from previous sessions)
        bool killedPhpOrphans = await Task.Run(() => KillOrphanedProcess("php-cgi", phpCgi));
        if (killedPhpOrphans)
            await Task.Delay(400);

        // Fix 2.2: cảnh báo nếu workers × memoryLimit > 70% RAM hệ thống
        {
            long totalRamMb = (long)(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024);
            long projectedMb = (long)Config.Php.Workers * Config.Php.MemoryLimit;
            if (totalRamMb > 0 && projectedMb > totalRamMb * 0.70)
            {
                int safeWorkers = Math.Max(2, (int)(totalRamMb * 0.70 / Config.Php.MemoryLimit));
                Log($"[WARN] {Config.Php.Workers} workers × {Config.Php.MemoryLimit}MB = {projectedMb}MB projected max. " +
                    $"Available RAM: ~{totalRamMb}MB. " +
                    $"Suggested safe max: {safeWorkers} workers (Settings → PHP).");
            }
        }

        SetPhp(ServerStatus.Starting);
        Log($"Starting PHP {Config.Php.CurrentVersion} FastCGI on 127.0.0.1:{Config.Php.FcgiPort}…");

        var psi = new ProcessStartInfo
        {
            FileName = phpCgi,
            Arguments = $"-b 127.0.0.1:{Config.Php.FcgiPort}",
            WorkingDirectory = phpDir,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true
        };

        psi.Environment["PHP_FCGI_CHILDREN"] = Config.Php.Workers.ToString();
        psi.Environment["PHP_FCGI_MAX_REQUESTS"] = Config.Php.MaxRequests.ToString();
        // PHP_FCGI_MAX_CONNECTIONS is NOT set — it is non-standard on Windows and can cause
        // PHP-CGI to refuse connections or crash when combined with PHP_FCGI_CHILDREN.
        psi.Environment["PHPRC"] = phpDir;

        _phpProcess?.Dispose();
        var phpProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _phpProcess = phpProcess;
        phpProcess.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                Log($"[PHP] {e.Data}");
        };
        phpProcess.Exited += (_, _) =>
        {
            bool wasIntentional = _phpStopRequested;
            _phpStopRequested = false;
            SetPhp(ServerStatus.Stopped);

            if (!wasIntentional && !_disposed)
            {
                // Throttle: count crashes within the rolling window
                int attempt;
                int delayMs;
                lock (_phpCrashLock)
                {
                    var now = DateTime.UtcNow;
                    if ((now - _phpFirstCrashTime).TotalSeconds > PhpAutoRestartWindowSec)
                    {
                        _phpCrashCount = 0;
                        _phpFirstCrashTime = now;
                    }
                    _phpCrashCount++;
                    attempt = _phpCrashCount;
                    // Exponential backoff: 2s, 4s, 6s, 8s, 10s
                    delayMs = PhpAutoRestartDelayMs * attempt;
                }

                if (attempt <= MaxPhpAutoRestarts)
                {
                    Log($"[WARN] PHP-CGI crashed unexpectedly " +
                        $"(attempt {attempt}/{MaxPhpAutoRestarts}). " +
                        $"Auto-restarting in {delayMs / 1000}s…");
                    SchedulePhpRestart(delayMs);
                }
                else
                {
                    SetPhp(ServerStatus.Error);
                    Log($"[ERROR] PHP-CGI crashed {MaxPhpAutoRestarts} times in {PhpAutoRestartWindowSec}s — " +
                        $"giving up. Check config/logs/php_error.log for details."); // Fix 4.1: nhất quán với MaxPhpAutoRestarts
                }
            }
            else
            {
                Log("PHP-CGI process stopped.");
            }
        };

        using var cts = new CancellationTokenSource();
        void phpExited(object? s, EventArgs e) { try { cts.Cancel(); } catch (ObjectDisposedException) { } }
        phpProcess.Exited += phpExited;    // register BEFORE Start to avoid race condition

        try
        {
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    phpProcess.Dispose();
                    if (ReferenceEquals(_phpProcess, phpProcess))
                        _phpProcess = null;
                    SetPhp(ServerStatus.Stopped);
                    return;
                }
                phpProcess.Start();
                phpProcess.BeginErrorReadLine();
            }
        }
        catch (Exception ex)
        {
            phpProcess.Exited -= phpExited;
            phpProcess.Dispose();
            if (ReferenceEquals(_phpProcess, phpProcess))
                _phpProcess = null;
            SetPhp(ServerStatus.Error);
            Log($"[ERROR] Cannot start PHP-CGI: {ex.Message}");
            return;
        }

        bool ready = await WaitForPortAsync(Config.Php.FcgiPort, cts.Token);
        phpProcess.Exited -= phpExited;
        if (IsPhpRunning && ready)
        {
            SetPhp(ServerStatus.Running);
            Log($"[OK] PHP {Config.Php.CurrentVersion} FastCGI started " +
                $"(Workers: {Config.Php.Workers}, MaxReq: {Config.Php.MaxRequests})");
        }
        else
        {
            if (IsPhpRunning) await StopPhpCoreAsync();
            SetPhp(ServerStatus.Error);
            Log("[ERROR] PHP-CGI failed. Ensure php-cgi.exe is in php/{version}/");
        }
    }

    public async Task StopPhpAsync()
    {
        _phpDesiredRunning = false;
        CancelPendingPhpRestart();
        await _phpGate.WaitAsync();
        try { await StopPhpCoreAsync(); }
        finally { _phpGate.Release(); }
    }

    private async Task StopPhpCoreAsync()
    {
        if (!IsPhpRunning)
        {
            _phpProcess?.Dispose();
            _phpProcess = null;
            SetPhp(ServerStatus.Stopped);
            return;
        }

        SetPhp(ServerStatus.Stopping);
        Log("Stopping PHP-CGI…");
        _phpStopRequested = true;   // signal Exited handler: this is intentional

        try
        {
            _phpProcess?.Kill(entireProcessTree: true);
            await Task.Run(() => _phpProcess?.WaitForExit(10_000)); // Fix 2.3: 5s→10s graceful shutdown
        }
        catch (Exception ex)
        {
            Log($"[WARN] PHP stop error: {ex.Message}");
        }

        // Kill any surviving php-cgi worker processes from our installation
        if (_phpCgiPath != null)
            KillSurvivingChildren("php-cgi", _phpCgiPath);

        _phpProcess?.Dispose();
        _phpProcess = null;
        SetPhp(ServerStatus.Stopped);
        Log("PHP-CGI stopped.");
    }

    public async Task SwitchPhpVersionAsync(string version)
    {
        if (version == Config.Php.CurrentVersion) return;  // Fix 4.3: không restart nếu chọn lại version đang dùng
        if (Path.GetFileName(version) != version ||
            !File.Exists(Path.Combine(RootDir, "php", version, "php-cgi.exe")))
        {
            SetPhp(ServerStatus.Error);
            Log($"[ERROR] PHP version is not installed: {version}");
            return;
        }

        var apacheWas = IsApacheRunning;
        var phpWas = IsPhpRunning;

        if (apacheWas) await StopApacheAsync();
        if (phpWas) await StopPhpAsync();

        Config.Php.CurrentVersion = version;
        Log($"PHP version → {version}");

        if (phpWas) await StartPhpAsync();
        if (apacheWas) await StartApacheAsync();
    }

    private void SchedulePhpRestart(int delayMs)
    {
        CancellationToken token;
        lock (_phpRestartLock)
        {
            _phpRestartCts?.Cancel();
            _phpRestartCts?.Dispose();
            _phpRestartCts = new CancellationTokenSource();
            token = _phpRestartCts.Token;
        }
        _ = RestartPhpAfterCrashAsync(delayMs, token);
    }

    private async Task RestartPhpAfterCrashAsync(int delayMs, CancellationToken token)
    {
        try
        {
            await Task.Delay(delayMs, token);
            await _phpGate.WaitAsync(token);
            try
            {
                if (!_disposed && _phpDesiredRunning && PhpStatus == ServerStatus.Stopped)
                    await StartPhpCoreAsync();
            }
            finally
            {
                _phpGate.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // A user-requested stop or a newer crash restart superseded this attempt.
        }
        catch (Exception ex)
        {
            SetPhp(ServerStatus.Error);
            Log($"[ERROR] PHP auto-restart failed: {ex.Message}");
        }
    }

    private void CancelPendingPhpRestart()
    {
        lock (_phpRestartLock)
        {
            _phpRestartCts?.Cancel();
            _phpRestartCts?.Dispose();
            _phpRestartCts = null;
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    public string? FindApacheDir()
    {
        // Primary structure: ./apache/Apache24  (bundled lowercase)
        string[] candidates =
        [
            Path.Combine(RootDir, "apache", "Apache24"),
            Path.Combine(RootDir, "apache"),
            Path.Combine(RootDir, "Apache", "Apache24"),
            Path.Combine(RootDir, "Apache24"),
            Path.Combine(RootDir, "Apache"),
        ];
        return candidates.FirstOrDefault(c => File.Exists(Path.Combine(c, "bin", "httpd.exe")));
    }

    private static async Task<(bool ok, string output)> RunCaptureAsync(
        string exe, string args, string workDir)
    {
        try
        {
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    WorkingDirectory = workDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            p.Start();
            // Read both streams concurrently to avoid deadlock when one buffer fills
            var errTask = p.StandardError.ReadToEndAsync();
            var stdTask = p.StandardOutput.ReadToEndAsync();
            await p.WaitForExitAsync();
            return (p.ExitCode == 0, await errTask + await stdTask);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    /// <summary>Ports with a listening socket right now, from the OS TCP table (no connections made).</summary>
    private static HashSet<int> GetListeningPorts()
    {
        try
        {
            return IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Select(ep => ep.Port)
                .ToHashSet();
        }
        catch { return []; }
    }

    private static async Task<bool> WaitForPortAsync(int port, CancellationToken cancel = default)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(PortProbeTimeoutMs);
        while (DateTime.UtcNow < deadline && !cancel.IsCancellationRequested)
        {
            try
            {
                using var tcp = new TcpClient();
                // Cancel the connect itself rather than abandoning it via WaitAsync — an
                // abandoned ConnectAsync keeps running against a disposed socket and its
                // failure surfaces later as an unobserved task exception.
                using var attempt = CancellationTokenSource.CreateLinkedTokenSource(cancel);
                attempt.CancelAfter(200);
                await tcp.ConnectAsync("127.0.0.1", port, attempt.Token).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) when (cancel.IsCancellationRequested) { break; }
            catch { }
            try { await Task.Delay(PortProbeIntervalMs, cancel).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
        return false;
    }

    // Kills orphaned processes whose executable matches our bundled path exactly.
    // Safe: only kills processes we own (same install path) — leaves system/other installs alone.
    private bool KillOrphanedApache(string httpdExePath) =>
        KillOrphanedProcess("httpd", httpdExePath);

    /// <summary>Pre-start cleanup: these really are leftovers from an earlier run.</summary>
    private bool KillOrphanedProcess(string processName, string exePath) =>
        KillMatching(processName, exePath, "leftover from a previous session");

    /// <summary>Post-stop sweep: children of the run we just ended that outlived their parent.</summary>
    private void KillSurvivingChildren(string processName, string exePath) =>
        KillMatching(processName, exePath, "surviving child of the stopped process");

    private bool KillMatching(string processName, string exePath, string reason)
    {
        bool killed = false;
        KillProcessesFromPath(processName, exePath, pid =>
        {
            Log($"Killed {processName} (PID {pid}) — {reason}.");
            killed = true;
        });
        return killed;
    }

    /// <summary>
    /// Kills every process named <paramref name="processName"/> whose executable is exactly
    /// <paramref name="exePath"/>. Every <see cref="Process"/> returned by the enumeration owns
    /// an OS handle, so each one is disposed — including the ones that do not match, which is
    /// most of them.
    /// </summary>
    private static void KillProcessesFromPath(string processName, string exePath, Action<int>? onKilled = null)
    {
        string target;
        try { target = Path.GetFullPath(exePath).ToLowerInvariant(); }
        catch { return; }

        foreach (var proc in Process.GetProcessesByName(processName))
        {
            using (proc)
            {
                try
                {
                    var actual = TryGetProcessImagePath(proc.Id);
                    if (actual is null) continue;   // access denied, or the process already exited
                    if (!string.Equals(actual, target, StringComparison.OrdinalIgnoreCase)) continue;

                    int pid = proc.Id;
                    proc.Kill(entireProcessTree: true);
                    onKilled?.Invoke(pid);
                }
                catch { /* access denied or already exited */ }
            }
        }
    }

    private const int ProcessQueryLimitedInformation = 0x1000;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "QueryFullProcessImageNameW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder exeName, ref int size);

    /// <summary>
    /// Full image path of a process, lower-cased, or null when it cannot be read.
    /// Process.MainModule answers the same question, but it enumerates every loaded module of
    /// the target — with a couple of dozen PHP workers that turned a stop into a ~19 s wait.
    /// </summary>
    private static string? TryGetProcessImagePath(int pid)
    {
        IntPtr handle = OpenProcess(ProcessQueryLimitedInformation, false, pid);
        if (handle == IntPtr.Zero) return null;
        try
        {
            var buffer = new StringBuilder(1024);
            int size = buffer.Capacity;
            return QueryFullProcessImageName(handle, 0, buffer, ref size)
                ? buffer.ToString(0, size).ToLowerInvariant()
                : null;
        }
        catch { return null; }
        finally { CloseHandle(handle); }
    }

    // Returns human-readable description of which process owns the port, e.g. "PID 29052 (iisexpress)"
    private static string FindPortOwner(int port)
    {
        try
        {
            // netstat directly — going through cmd.exe just to pipe into findstr spawned two
            // extra processes to do filtering we already do below.
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netstat.exe",
                    Arguments = "-ano -p TCP",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(3000);

            foreach (var line in output.Split('\n'))
            {
                // Match lines like "  TCP    0.0.0.0:80    0.0.0.0:0    LISTENING    29052"
                var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5 &&
                    parts[1].EndsWith($":{port}") &&
                    parts[3] == "LISTENING" &&
                    int.TryParse(parts[4], out int pid))
                {
                    try
                    {
                        using var proc = Process.GetProcessById(pid);
                        var name = proc.ProcessName;

                        // Provide actionable hints for common culprits
                        var hint = name.ToLower() switch
                        {
                            "iisexpress" or "w3wp" => " — Stop IIS: run  iisreset /stop",
                            "httpd" => " — Another Apache instance is running",
                            "nginx" => " — Stop Nginx: run  nginx -s stop",
                            "node" => " — A Node.js server is using this port",
                            "python" => " — A Python server is using this port",
                            _ => ""
                        };
                        return $"PID {pid} ({name}){hint}";
                    }
                    catch
                    {
                        return $"PID {pid} (process not found — may have exited)";
                    }
                }
            }
        }
        catch { }
        return "unknown process — run: netstat -ano | findstr LISTENING";
    }

    private static string ReadLastErrorLog(string errorLogPath)
    {
        try
        {
            if (!File.Exists(errorLogPath)) return "";
            const int maxBytes = 8192;
            using var fs = new FileStream(errorLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            long startPos = Math.Max(0, fs.Length - maxBytes);
            fs.Seek(startPos, SeekOrigin.Begin);
            using var reader = new StreamReader(fs);
            var lines = reader.ReadToEnd()
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .TakeLast(8);
            return string.Join("\n", lines).Trim();
        }
        catch { return ""; }
    }

    private void SetApache(ServerStatus s) { ApacheStatus = s; StatusChanged?.Invoke(this, EventArgs.Empty); }
    private void SetPhp(ServerStatus s) { PhpStatus = s; StatusChanged?.Invoke(this, EventArgs.Empty); }

    private void Log(string msg) =>
        LogReceived?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] {msg}");

    // ─── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_disposed) return;
            _disposed = true;
            _phpDesiredRunning = false;
            CancelPendingPhpRestart();

            // Signal watchdog not to restart after we kill the processes
            _apacheStopRequested = true;
            _phpStopRequested = true;

            // Kill tracked processes synchronously (no async/await in Dispose)
            try { _apacheProcess?.Kill(entireProcessTree: true); } catch { }
            try { _phpProcess?.Kill(entireProcessTree: true); } catch { }

            _apacheProcess?.Dispose();
            _phpProcess?.Dispose();
        }

        // Kill any surviving child processes from our installation.
        // mpm_winnt child httpd.exe and php-cgi workers may outlive their parents.
        if (_httpdExePath != null) KillProcessesFromPath("httpd", _httpdExePath);
        if (_phpCgiPath != null) KillProcessesFromPath("php-cgi", _phpCgiPath);

        // The gates are deliberately NOT disposed: a watchdog restart may still be sitting in
        // WaitAsync, and disposing under it would throw out of its finally-block Release().
        // SemaphoreSlim only allocates an OS handle if AvailableWaitHandle is read, which we never do.
    }

    // ─── Settings invalidation ────────────────────────────────────────────────

    public void SynchronizePhpIni()
    {
        var configDir = Path.Combine(RootDir, "config");
        var phpBaseDir = Path.Combine(RootDir, "php");
        if (!Directory.Exists(phpBaseDir)) return;

        foreach (var phpDir in Directory.GetDirectories(phpBaseDir))
        {
            if (File.Exists(Path.Combine(phpDir, "php-cgi.exe")))
                _configWriter.WritePhpIni(phpDir, configDir, forceRegenerate: true);
        }
    }
}
