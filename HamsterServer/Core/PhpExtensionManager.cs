namespace HamsterServer.Core;

/// <summary>
/// Discovers bundled PHP extensions and resolves the configured extension profile for a runtime.
/// </summary>
public static class PhpExtensionManager
{
    public const string Opcache = "opcache";

    /// <summary>
    /// Extensions that are available and load cleanly in every PHP runtime bundled with Hamster Server.
    /// This profile covers the common XAMPP development stack and Hamster Server framework presets.
    /// </summary>
    public static IReadOnlyList<string> RecommendedExtensions { get; } =
    [
        "bz2", "curl", "exif", "fileinfo", "ftp", "gd", "gettext", "intl",
        "mbstring", "mysqli", "odbc", "openssl", "pdo_mysql", "pdo_sqlite",
        "sockets", "sodium", "sqlite3", "xsl", "zip"
    ];

    public static IReadOnlyList<string> GetAvailableExtensions(string phpDir)
    {
        var extDir = Path.Combine(phpDir, "ext");
        if (!Directory.Exists(extDir)) return [];

        return Directory.GetFiles(extDir, "php_*.dll")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(name => name.StartsWith("php_", StringComparison.OrdinalIgnoreCase))
            .Select(name => name[4..])
            // OPcache is controlled by the dedicated setting and must use zend_extension.
            .Where(name => !name.Equals(Opcache, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<string> GetRecommendedExtensions(string phpDir)
    {
        var available = GetAvailableExtensions(phpDir).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return RecommendedExtensions.Where(available.Contains).ToArray();
    }

    /// <summary>
    /// Keeps the global selection portable between PHP versions by emitting only extensions
    /// whose DLL exists in the selected runtime. If ext/ is unavailable, retain the configured
    /// names so a temporary filesystem issue does not silently erase the generated profile.
    /// </summary>
    public static IReadOnlyList<string> ResolveForRuntime(
        string phpDir, IEnumerable<string> configuredExtensions)
    {
        var configured = configuredExtensions
            .Where(IsSafeExtensionName)
            .Where(name => !name.Equals(Opcache, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var extDir = Path.Combine(phpDir, "ext");
        if (!Directory.Exists(extDir)) return configured;

        var available = GetAvailableExtensions(phpDir).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return configured.Where(available.Contains).ToArray();
    }

    public static bool HasOpcacheDll(string phpDir) =>
        File.Exists(Path.Combine(phpDir, "ext", "php_opcache.dll"));

    private static bool IsSafeExtensionName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.All(ch => char.IsLetterOrDigit(ch) || ch == '_');
}
