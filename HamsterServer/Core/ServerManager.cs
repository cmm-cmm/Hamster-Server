using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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

    // Memory watchdog: with MaxRequests = 0 nothing recycles workers, so leaked memory is only
    // reclaimed by restarting the pool. Sweeps on a timer, and only acts while the pool is idle.
    // System.Threading.Timer, not the WinForms one: Core has no UI thread to marshal to, and the
    // sweep deliberately runs off the UI thread because it enumerates processes.
    private System.Threading.Timer? _phpMemoryTimer;
    private int _phpMemorySweepRunning;          // 0/1 guard against overlapping timer callbacks
    private int _phpMemorySweepSeconds;          // current timer period, for RescheduleSweep
    private int _phpMemoryMissedWindows;         // consecutive checks that never caught an idle gap
    private DateTime _lastPhpMemoryRestart = DateTime.MinValue;
    private const int PhpMemorySweepIntervalSec = 60;
    private const int PhpMemorySweepBusyIntervalSec = 15;  // within 80% of the threshold
    private const int PhpMemoryIdleWaitSec = 300;     // give the pool this long to fall quiet
    private const int PhpMemoryIdlePollMs = 250;
    private const int PhpMemoryCooldownMin = 10;      // settle time after a successful restart
    private const int MaxMissedWindowsBeforeWarning = 3;
    // A pool that has just restarted weighing this much of the threshold can never get under it.
    private const double FloorToThresholdLimit = 0.8;

    /// <summary>
    /// Working set of one idle php-cgi worker, in MB. Measured at ~35 MB when the worker count was
    /// tuned, and confirmed at 40.2 MB across nine recycles of a 12-worker pool in production use.
    /// </summary>
    public const int WorkerBaselineMb = 40;

    /// <summary>
    /// How much room the automatic threshold leaves above the pool's floor. Three times the floor
    /// puts a leaky application roughly two hours between recycles instead of fifty minutes, while
    /// still reclaiming memory long before it matters on a development machine.
    /// </summary>
    public const int AutoThresholdFactor = 3;

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
            StartPhpMemoryWatchdog();
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
        StopPhpMemoryWatchdog();
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

    // ─── Memory watchdog ─────────────────────────────────────────────────────
    //
    // Php.MaxRequests = 0 is what makes FastCGI connection pooling safe (see ApacheConfigWriter),
    // but it also means a worker never exits on its own, so anything a script or extension leaks
    // stays for the life of the server. This replaces "recycle every N requests" with "recycle
    // when the pool actually got fat", and only while nothing is in flight, so no request is cut.

    private void StartPhpMemoryWatchdog()
    {
        StopPhpMemoryWatchdog();
        if (Config.Php.MemoryRestartThreshold <= 0) return;

        _phpMemorySweepSeconds = PhpMemorySweepIntervalSec;
        _phpMemoryMissedWindows = 0;
        var period = TimeSpan.FromSeconds(PhpMemorySweepIntervalSec);
        _phpMemoryTimer = new System.Threading.Timer(
            _ => _ = SweepPhpMemoryAsync(), null, period, period);
    }

    private void StopPhpMemoryWatchdog()
    {
        var timer = Interlocked.Exchange(ref _phpMemoryTimer, null);
        timer?.Dispose();
    }

    private async Task SweepPhpMemoryAsync()
    {
        // Timer callbacks can overlap when a sweep spends minutes waiting for the pool to fall
        // idle; only one may be in flight.
        if (Interlocked.CompareExchange(ref _phpMemorySweepRunning, 1, 0) != 0) return;
        try
        {
            int thresholdMb = EffectiveMemoryThresholdMb;
            if (_disposed || thresholdMb <= 0 || !_phpDesiredRunning || !IsPhpRunning) return;
            if ((DateTime.UtcNow - _lastPhpMemoryRestart).TotalMinutes < PhpMemoryCooldownMin) return;

            long usedBytes = await Task.Run(GetPhpPoolWorkingSetBytes);
            long usedMb = usedBytes / 1024 / 1024;
            RescheduleSweep(usedMb, thresholdMb);

            if (usedMb < thresholdMb)
            {
                _phpMemoryMissedWindows = 0;
                return;
            }

            Log($"[WARN] PHP pool is using {usedMb} MB (threshold {thresholdMb} MB) — " +
                $"waiting for it to go idle before restarting…");

            if (!await WaitForPhpPoolIdleAsync())
            {
                ReportMissedRestartWindow(usedMb, thresholdMb);
                return;
            }

            await RestartPhpPoolForMemoryAsync(usedMb, thresholdMb);
        }
        catch (Exception ex)
        {
            Log($"[WARN] PHP memory check failed: {ex.Message}");
        }
        finally
        {
            Volatile.Write(ref _phpMemorySweepRunning, 0);
        }
    }

    /// <summary>
    /// Tightens the sweep interval as the pool approaches the threshold, so the moment it crosses
    /// is noticed in seconds rather than up to a minute — and relaxes it again afterwards. The
    /// sweep itself only enumerates processes, so the faster cadence costs close to nothing.
    /// </summary>
    private void RescheduleSweep(long usedMb, int thresholdMb)
    {
        int seconds = usedMb >= thresholdMb * 0.8
            ? PhpMemorySweepBusyIntervalSec
            : PhpMemorySweepIntervalSec;
        if (seconds == _phpMemorySweepSeconds) return;

        var timer = _phpMemoryTimer;
        if (timer is null) return;
        try
        {
            var period = TimeSpan.FromSeconds(seconds);
            timer.Change(period, period);
            _phpMemorySweepSeconds = seconds;
        }
        catch (ObjectDisposedException) { /* watchdog stopped while we were sweeping */ }
    }

    /// <summary>
    /// Decides whether the threshold itself is wrong, by looking at what the pool weighs
    /// immediately after a recycle. That reading is the pool's floor; if it already sits near the
    /// threshold, no amount of recycling will ever get under it and the watchdog would thrash.
    /// <para>
    /// This replaces a "max N restarts per hour" budget, which measured the wrong thing. Real
    /// operational logs show a leaky application needing seven recycles across a working day, two
    /// of them 47 minutes apart — healthy behaviour that the old rule would have read as a fault
    /// and switched the watchdog off for an hour. The 10-minute cooldown already caps the rate;
    /// this checks the one condition the cooldown cannot.
    /// </para>
    /// </summary>
    private void CheckThresholdIsReachable(long floorMb, int thresholdMb)
    {
        if (floorMb < thresholdMb * FloorToThresholdLimit) return;

        Log($"[ERROR] The PHP pool still uses {floorMb} MB right after a recycle, against a " +
            $"{thresholdMb} MB threshold — recycling cannot get it below that, so memory restarts " +
            "are paused. Raise Settings → PHP → \"Restart pool above\" (or switch it to Auto), " +
            "or lower Workers.");
        StopPhpMemoryWatchdog();
    }

    /// <summary>
    /// The pool never fell quiet inside the wait window. That is not an error — refusing to cut a
    /// live request is the whole policy — but if it keeps happening the memory is never reclaimed,
    /// so say so plainly instead of leaving an INFO line nobody reads.
    /// </summary>
    private void ReportMissedRestartWindow(long usedMb, int thresholdMb)
    {
        int missed = ++_phpMemoryMissedWindows;
        if (missed < MaxMissedWindowsBeforeWarning)
        {
            Log($"[INFO] PHP pool stayed busy for {PhpMemoryIdleWaitSec / 60} minutes at {usedMb} MB — " +
                "leaving it alone and rechecking later.");
            return;
        }

        Log($"[WARN] PHP pool has been above the {thresholdMb} MB threshold for {missed} checks " +
            $"(now {usedMb} MB) and never went idle long enough to recycle. Traffic has not paused — " +
            "restart PHP from the Service menu at a convenient moment, or raise the threshold.");
    }

    private async Task<bool> WaitForPhpPoolIdleAsync()
    {
        // Polled tightly rather than every few seconds: even a machine under steady load has gaps
        // between requests, and a 5 s poll walked straight past them. Each poll is one loopback
        // request to mod_status, so four per second is noise next to the traffic it is watching.
        var deadline = DateTime.UtcNow.AddSeconds(PhpMemoryIdleWaitSec);
        while (DateTime.UtcNow < deadline)
        {
            if (_disposed || !_phpDesiredRunning || !IsPhpRunning) return false;
            if (await IsPhpPoolIdleAsync()) return true;
            await Task.Delay(PhpMemoryIdlePollMs);
        }
        return false;
    }

    /// <summary>Returns true only when the pool was actually recycled.</summary>
    private async Task<bool> RestartPhpPoolForMemoryAsync(long usedMb, int thresholdMb)
    {
        await _phpGate.WaitAsync();
        try
        {
            // Re-check under the gate: the user may have stopped PHP while we waited for idle.
            if (_disposed || !_phpDesiredRunning || !IsPhpRunning) return false;

            // One last look — a request can arrive between the idle check and taking the gate.
            if (!await IsPhpPoolIdleAsync())
            {
                Log("[INFO] PHP pool became busy again — postponing the memory restart.");
                return false;
            }

            Log($"Recycling the PHP pool to release {usedMb} MB…");
            await StopPhpCoreAsync();
            await Task.Delay(DelayRestartPauseMs);
            await StartPhpCoreAsync();
            _lastPhpMemoryRestart = DateTime.UtcNow;
            _phpMemoryMissedWindows = 0;

            long afterMb = GetPhpPoolWorkingSetBytes() / 1024 / 1024;
            Log($"[OK] PHP pool recycled — memory is now {afterMb} MB (was {usedMb} MB).");

            // The post-recycle figure is the pool's floor, and the only reliable signal that the
            // threshold was set below what the pool can ever reach.
            CheckThresholdIsReachable(afterMb, thresholdMb);
            return true;
        }
        finally
        {
            _phpGate.Release();
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

    /// <summary>
    /// Runs <c>httpd -t</c> against a throw-away httpd.conf built from the live configuration with
    /// <paramref name="customDirectives"/> substituted, so a broken directive is rejected before it
    /// is saved. Without this, saving one while Apache ran stopped Apache and then failed its own
    /// pre-start config test — leaving the server down over a typo.
    /// </summary>
    public async Task<(bool Ok, string Output)> TestApacheConfigAsync(string customDirectives)
    {
        var apacheDir = FindApacheDir();
        if (apacheDir is null) return (true, "Apache not found — configuration was not tested.");

        var configDir = Path.Combine(RootDir, "config");
        var testConf = Path.Combine(configDir, $".httpd-test-{Guid.NewGuid():N}.conf");
        try
        {
            var candidate = JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(Config))!;
            candidate.Apache.CustomDirectives = customDirectives;
            new ApacheConfigWriter(RootDir, candidate)
                .WriteHttpdConf(testConf, apacheDir, Path.Combine(RootDir, "www"), configDir);

            var httpdExe = Path.Combine(apacheDir, "bin", "httpd.exe");
            var (ok, output) = await RunCaptureAsync(httpdExe, $"-t -f \"{testConf}\"", apacheDir);
            return (ok, output.Replace(testConf, "httpd.conf", StringComparison.OrdinalIgnoreCase).Trim());
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
        finally
        {
            try { File.Delete(testConf); } catch { /* a leftover test file is harmless */ }
        }
    }

    private static async Task<(bool ok, string output)> RunCaptureAsync(
        string exe, string args, string workDir, int timeoutMs = 30_000)
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
            // A config test that never returns (a directive waiting on a network share, an
            // antivirus hold on the binary) used to park the start sequence — and the gate it
            // holds — forever.
            using var timeout = new CancellationTokenSource(timeoutMs);
            try
            {
                await p.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { p.Kill(entireProcessTree: true); } catch { }
                return (false, $"{Path.GetFileName(exe)} {args} did not finish within {timeoutMs / 1000} s.");
            }
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

    /// <summary>
    /// The threshold the watchdog actually uses: either the configured number, or one derived from
    /// the worker count when Auto is on. Exposed so the settings dialog can show the same figure
    /// the server will apply rather than a second guess at it.
    /// </summary>
    public int EffectiveMemoryThresholdMb => Config.Php.MemoryRestartAuto
        ? AutoMemoryThresholdMb(Config.Php.Workers)
        : Config.Php.MemoryRestartThreshold;

    /// <summary>
    /// Automatic threshold for a given worker count: the pool's expected floor times
    /// <see cref="AutoThresholdFactor"/>, never below 512 MB (a small pool still needs headroom to
    /// leak into) and never above half the machine's RAM (recycling is pointless if the box is
    /// already swapping).
    /// </summary>
    public static int AutoMemoryThresholdMb(int workers)
    {
        long floor = (long)Math.Clamp(workers, 1, 256) * WorkerBaselineMb;
        long ceiling = 65_536;
        long totalRamMb = TotalRamMb;
        if (totalRamMb > 0) ceiling = Math.Min(ceiling, Math.Max(512, totalRamMb / 2));

        return (int)Math.Clamp(floor * AutoThresholdFactor, 512, ceiling);
    }

    /// <summary>Total RAM the runtime can address, in MB, or 0 when it cannot be read.</summary>
    public static long TotalRamMb
    {
        get
        {
            try { return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024; }
            catch { return 0; }
        }
    }

    /// <summary>What the machine allows, and which of the two limits is doing the deciding.</summary>
    public readonly record struct WorkerAdvice(int Workers, int Cores, long TotalRamMb, bool RamLimited);

    /// <summary>
    /// Worker count this machine can actually carry, given its cores and its RAM.
    /// <para>
    /// CPU sets the useful ceiling: workers are processes, and a measured load ramp showed 24 of
    /// them on a 12-core box idling at 891 MB while serving fewer requests than 8 did — so this
    /// never suggests more than 8 however many cores are present.
    /// </para>
    /// <para>
    /// RAM sets the hard limit, and it bites on small machines: the worst case is every worker
    /// reaching <paramref name="memoryLimitMb"/> at once, so the suggestion is cut to fit inside
    /// 70% of RAM. On 4 GB with a 512 MB limit that is 5 workers, not 8.
    /// </para>
    /// </summary>
    public static WorkerAdvice SuggestWorkers(int memoryLimitMb)
    {
        int cores = Environment.ProcessorCount;
        long totalRamMb = TotalRamMb;

        int byCpu = Math.Clamp(cores, 4, 8);
        int byRam = byCpu;
        bool ramLimited = false;

        if (totalRamMb > 0 && memoryLimitMb > 0)
        {
            byRam = (int)Math.Max(1, totalRamMb * 70 / 100 / memoryLimitMb);
            ramLimited = byRam < byCpu;
        }

        return new WorkerAdvice(Math.Clamp(Math.Min(byCpu, byRam), 1, 256), cores, totalRamMb, ramLimited);
    }

    /// <summary>
    /// Total working set of every php-cgi process belonging to this installation, in bytes, or 0
    /// when PHP is not running. Counts the whole pool, not just the tracked parent: the parent is
    /// a supervisor and the memory that actually accumulates lives in the worker children.
    /// </summary>
    public long GetPhpPoolWorkingSetBytes() =>
        _phpCgiPath is null ? 0 : SumWorkingSetFromPath("php-cgi", _phpCgiPath);

    /// <summary>
    /// True when Apache is not serving any request, so recycling the PHP pool cannot cut one in
    /// half. Reads <c>BusyWorkers</c> from mod_status on the loopback interface.
    /// <para>
    /// Counting TCP connections to the FastCGI port does NOT work here, which is worth recording:
    /// <c>ttl=15</c> only discards a pooled connection when Apache goes to reuse it, there is no
    /// background reaper, so with <c>enablereuse=on</c> the sockets sit ESTABLISHED indefinitely
    /// whether or not a request is on them. A pool that had ever served traffic would then never
    /// look idle and the memory restart would never fire.
    /// </para>
    /// </summary>
    public async Task<bool> IsPhpPoolIdleAsync()
    {
        // Nothing can reach php-cgi while Apache is down, so the pool is trivially idle.
        if (!IsApacheRunning) return true;

        int busy = await GetApacheBusyWorkersAsync();
        // Unreadable status: assume busy rather than recycle on top of live requests.
        // The poll itself occupies one worker, hence <= 1 rather than == 0.
        return busy >= 0 && busy <= 1;
    }

    private static readonly HttpClient StatusClient = new() { Timeout = TimeSpan.FromSeconds(3) };

    /// <summary>Number of Apache workers currently serving a request, or -1 when unreadable.</summary>
    private async Task<int> GetApacheBusyWorkersAsync()
    {
        try
        {
            var url = $"http://127.0.0.1:{Config.Apache.Port}/server-status?auto";
            var body = await StatusClient.GetStringAsync(url);
            foreach (var line in body.Split('\n'))
            {
                if (!line.StartsWith("BusyWorkers:", StringComparison.OrdinalIgnoreCase)) continue;
                return int.TryParse(line.AsSpan("BusyWorkers:".Length).Trim(), out int busy) ? busy : -1;
            }
            return -1;
        }
        catch
        {
            return -1;
        }
    }

    private static long SumWorkingSetFromPath(string processName, string exePath)
    {
        string target;
        try { target = Path.GetFullPath(exePath).ToLowerInvariant(); }
        catch { return 0; }

        long total = 0;
        foreach (var proc in Process.GetProcessesByName(processName))
        {
            using (proc)
            {
                try
                {
                    var actual = TryGetProcessImagePath(proc.Id);
                    if (actual is null) continue;
                    if (!string.Equals(actual, target, StringComparison.OrdinalIgnoreCase)) continue;
                    total += proc.WorkingSet64;
                }
                catch { /* exited between enumeration and read */ }
            }
        }
        return total;
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
            StopPhpMemoryWatchdog();

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

    /// <summary>
    /// Re-reads the memory settings after the settings dialog saved. Changing the threshold does
    /// not warrant bouncing PHP, and editing it is also how the user answers a "restarts are
    /// paused" message — re-arming the timer here is what lifts that pause.
    /// </summary>
    public void ApplyPhpMemorySettings()
    {
        _phpMemoryMissedWindows = 0;
        if (!_disposed && _phpDesiredRunning && IsPhpRunning) StartPhpMemoryWatchdog();
        else StopPhpMemoryWatchdog();
    }

    /// <summary>
    /// Regenerates php.ini for every installed runtime from <see cref="Config"/>.
    /// <para>
    /// Never throws. Every caller is an <c>async void</c> UI handler, where an exception — a php.ini
    /// held open by an editor or an antivirus scan, a read-only folder — escaped as the generic crash
    /// box and abandoned the restart sequence half-way. A runtime that cannot be written is logged and
    /// skipped so the others still get the new profile.
    /// </para>
    /// </summary>
    /// <returns><c>true</c> when every runtime was written.</returns>
    public bool SynchronizePhpIni()
    {
        var configDir = Path.Combine(RootDir, "config");
        var phpBaseDir = Path.Combine(RootDir, "php");
        if (!Directory.Exists(phpBaseDir)) return true;

        string[] runtimes;
        try
        {
            runtimes = Directory.GetDirectories(phpBaseDir);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log($"[ERROR] Cannot list PHP runtimes in {phpBaseDir}: {ex.Message}");
            return false;
        }

        bool allWritten = true;
        foreach (var phpDir in runtimes)
        {
            if (!File.Exists(Path.Combine(phpDir, "php-cgi.exe"))) continue;
            try
            {
                _configWriter.WritePhpIni(phpDir, configDir, forceRegenerate: true);
            }
            catch (Exception ex)
            {
                allWritten = false;
                Log($"[ERROR] Cannot regenerate php.ini for PHP {Path.GetFileName(phpDir)}: {ex.Message}");
            }
        }
        return allWritten;
    }
}
