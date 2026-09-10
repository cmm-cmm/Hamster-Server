using System.Text.Json;
using HamsterServer.Models;
using Microsoft.Win32;

namespace HamsterServer.Core;

public class ConfigManager
{
    private const string RunRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Hamster Server";
    private const string LegacyAppName = "XServer";
    private const string ConfigFileName = "hamster-server.json";
    private const string LegacyConfigFileName = "xserver.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly HashSet<string> LegacyDefaultExtensions = new(
        [
            "curl", "fileinfo", "gd", "intl", "mbstring", "openssl",
            "pdo_mysql", "pdo_sqlite", "sockets", "sodium", "zip"
        ],
        StringComparer.OrdinalIgnoreCase);

    private readonly string _configPath;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private bool _preserveBackupOnNextSave;

    public AppConfig Config { get; private set; } = new();

    public bool LastLoadCorrupted { get; private set; }
    public bool LastLoadRecovered { get; private set; }
    public string? LastLoadError { get; private set; }

    public ConfigManager(string rootDir)
    {
        var configDir = Path.Combine(rootDir, "config");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, ConfigFileName);
        MigrateLegacyConfiguration(configDir);
    }

    public AppConfig Load()
    {
        LastLoadCorrupted = false;
        LastLoadRecovered = false;
        LastLoadError = null;
        if (File.Exists(_configPath))
        {
            try
            {
                Config = ReadAndNormalize(_configPath);
            }
            catch (Exception ex)
            {
                LastLoadError = ex.Message;
                var backup = _configPath + ".bak";
                try
                {
                    Config = ReadAndNormalize(backup);
                    LastLoadRecovered = true;
                    _preserveBackupOnNextSave = true;
                    try
                    {
                        RestorePrimaryFromBackup(backup);
                    }
                    catch (Exception recoveryEx)
                    {
                        LastLoadError += $" Recovery write failed: {recoveryEx.Message}";
                    }
                }
                catch
                {
                    Config = new AppConfig();
                    LastLoadCorrupted = true;
                }
            }
        }
        else
        {
            Config = Normalize(new AppConfig());
        }
        return Config;
    }

    public void Save()
    {
        _saveGate.Wait();
        try
        {
            SaveCore();
        }
        finally
        {
            _saveGate.Release();
        }
    }

    public async Task SaveAsync()
    {
        await _saveGate.WaitAsync().ConfigureAwait(false);
        try
        {
            Config = Normalize(Config);
            var json = JsonSerializer.Serialize(Config, JsonOptions);
            var tmp = _configPath + ".tmp";
            await File.WriteAllTextAsync(tmp, json).ConfigureAwait(false);
            ReplaceConfigFile(tmp);
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private void SaveCore()
    {
        Config = Normalize(Config);
        var json = JsonSerializer.Serialize(Config, JsonOptions);
        var tmp = _configPath + ".tmp";
        File.WriteAllText(tmp, json);
        ReplaceConfigFile(tmp);
    }

    private static AppConfig ReadAndNormalize(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Configuration file not found.", path);
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)
            ?? throw new JsonException("Configuration root cannot be null.");
        return Normalize(config);
    }

    private void MigrateLegacyConfiguration(string configDir)
    {
        var legacyPath = Path.Combine(configDir, LegacyConfigFileName);
        CopyLegacyFileIfNeeded(legacyPath, _configPath);
        CopyLegacyFileIfNeeded(legacyPath + ".bak", _configPath + ".bak");
    }

    private static void CopyLegacyFileIfNeeded(string legacyPath, string destinationPath)
    {
        if (!File.Exists(destinationPath) && File.Exists(legacyPath))
            File.Copy(legacyPath, destinationPath);
    }

    private void RestorePrimaryFromBackup(string backup)
    {
        var recovery = _configPath + ".recovery.tmp";
        File.Copy(backup, recovery, overwrite: true);
        try
        {
            if (File.Exists(_configPath))
                File.Replace(recovery, _configPath, destinationBackupFileName: null);
            else
                File.Move(recovery, _configPath);
        }
        catch
        {
            try { File.Delete(recovery); } catch { }
            throw;
        }
    }

    private static AppConfig Normalize(AppConfig config)
    {
        config.Apache ??= new ApacheSettings();
        config.Php ??= new PhpSettings();
        config.VHosts ??= [];
        config.Apache.ExtraPorts ??= [];
        config.Php.EnabledExtensions ??= PhpExtensionManager.RecommendedExtensions.ToList();

        config.Apache.Port = NormalizePort(config.Apache.Port, 80);
        config.Apache.SslPort = NormalizePort(config.Apache.SslPort, 443);
        config.Php.FcgiPort = NormalizePort(config.Php.FcgiPort, 9000);
        var reservedPorts = new HashSet<int> { config.Apache.Port };
        if (!reservedPorts.Add(config.Apache.SslPort))
            config.Apache.SslPort = FindAvailablePort(443, reservedPorts);
        reservedPorts.Add(config.Apache.SslPort);
        if (!reservedPorts.Add(config.Php.FcgiPort))
            config.Php.FcgiPort = FindAvailablePort(9000, reservedPorts);
        reservedPorts.Add(config.Php.FcgiPort);
        config.Apache.MaxClients = Math.Clamp(config.Apache.MaxClients, 1, 2000);
        config.Apache.KeepAliveTimeout = Math.Clamp(config.Apache.KeepAliveTimeout, 1, 300);
        config.Php.Workers = Math.Clamp(config.Php.Workers, 1, 256);
        config.Php.MaxRequests = Math.Clamp(config.Php.MaxRequests, 1, 1_000_000);
        config.Php.MemoryLimit = Math.Clamp(config.Php.MemoryLimit, 16, 16_384);
        config.Php.MaxExecutionTime = Math.Clamp(config.Php.MaxExecutionTime, 1, 86_400);
        config.Php.OpcacheMemory = Math.Clamp(config.Php.OpcacheMemory, 16, 4_096);

        config.Apache.ServerName = string.IsNullOrWhiteSpace(config.Apache.ServerName)
            ? "localhost"
            : config.Apache.ServerName.Trim();
        config.Php.CurrentVersion = Path.GetFileName(config.Php.CurrentVersion?.Trim() ?? "");
        config.Php.Timezone = string.IsNullOrWhiteSpace(config.Php.Timezone)
            ? "Asia/Ho_Chi_Minh"
            : config.Php.Timezone.Trim();
        if (!config.Php.Timezone.All(ch => char.IsLetterOrDigit(ch) || ch is '/' or '_' or '-' or '+'))
            config.Php.Timezone = "Asia/Ho_Chi_Minh";
        config.Php.EnabledExtensions = config.Php.EnabledExtensions
            .Where(IsSafeExtensionName)
            .Where(name => !name.Equals(PhpExtensionManager.Opcache, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (LegacyDefaultExtensions.SetEquals(config.Php.EnabledExtensions))
            config.Php.EnabledExtensions = PhpExtensionManager.RecommendedExtensions.ToList();
        config.Apache.ExtraPorts = config.Apache.ExtraPorts
            .Where(port => port is not null && port.Port is >= 1 and <= 65535 &&
                           !reservedPorts.Contains(port.Port))
            .GroupBy(port => port.Port)
            .Select(group => group.First())
            .ToList();
        config.VHosts = config.VHosts.Where(vhost => vhost is not null).ToList();

        var validVHostPorts = new HashSet<int> { config.Apache.Port };
        if (config.Apache.EnableSsl) validVHostPorts.Add(config.Apache.SslPort);
        foreach (var extraPort in config.Apache.ExtraPorts.Where(port => port.Enabled))
            validVHostPorts.Add(extraPort.Port);

        foreach (var vhost in config.VHosts)
        {
            vhost.ServerName = vhost.ServerName?.Trim() ?? "";
            vhost.ServerAlias = vhost.ServerAlias?.Trim() ?? "";
            vhost.DocumentRoot = vhost.DocumentRoot?.Trim() ?? "";
            if (vhost.Port != 0 && !validVHostPorts.Contains(vhost.Port))
                vhost.Port = 0;
            if (!Enum.IsDefined(vhost.Framework))
                vhost.Framework = FrameworkPreset.None;
        }
        return config;
    }

    private static int NormalizePort(int value, int fallback) =>
        value is >= 1 and <= 65535 ? value : fallback;

    private static int FindAvailablePort(int preferred, HashSet<int> used)
    {
        for (var port = preferred; port <= 65535; port++)
            if (!used.Contains(port)) return port;
        for (var port = 1024; port < preferred; port++)
            if (!used.Contains(port)) return port;
        throw new InvalidDataException("No valid TCP port is available for the configuration.");
    }

    private static bool IsSafeExtensionName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.All(ch => char.IsLetterOrDigit(ch) || ch == '_');

    // Atomic swap: write to a temp file then move it over the real one so a crash
    // mid-write can never leave hamster-server.json half-written/corrupted.
    // .bak holds the previous good config — recoverable if corruption occurs.
    private void ReplaceConfigFile(string tmp)
    {
        var backup = _configPath + ".bak";
        try
        {
            if (File.Exists(_configPath))
                File.Replace(tmp, _configPath, _preserveBackupOnNextSave ? null : backup);
            else
                File.Move(tmp, _configPath);
            _preserveBackupOnNextSave = false;
        }
        catch
        {
            try { File.Delete(tmp); } catch { }
            throw;
        }
    }

    public static void ApplyStartWithWindows(bool enable, string exePath)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: true);
            if (key is null) return;
            if (enable)
            {
                key.SetValue(AppName, $"\"{exePath}\"");
                key.DeleteValue(LegacyAppName, throwOnMissingValue: false);
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
                key.DeleteValue(LegacyAppName, throwOnMissingValue: false);
            }
        }
        catch { }
    }
}
