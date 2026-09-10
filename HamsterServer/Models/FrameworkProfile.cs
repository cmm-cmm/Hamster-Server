namespace HamsterServer.Models;

/// <summary>
/// Describes the web-server conventions of a PHP framework or application.
/// Extension names match php_*.dll names and AppConfig.Php.EnabledExtensions.
/// </summary>
public sealed record FrameworkProfile(
    FrameworkPreset Preset,
    string DisplayName,
    string Description,
    string[] WebRootCandidates,
    bool AppendPreferredWebRoot,
    bool UsesFrontController,
    string[] ProjectMarkers,
    string[] RequiredExtensions,
    string SensitiveFilesPattern,
    bool IsReverseProxy = false,
    string ProxyDefault = "")
{
    public string PreferredWebRoot => WebRootCandidates.FirstOrDefault() ?? "";
}

/// <summary>Single source of truth for the framework picker and Apache generator.</summary>
public static class FrameworkProfiles
{
    private const string CommonSensitiveFiles =
        @"(^\.|\.env(\..*)?$|composer\.(json|lock)$|package(-lock)?\.json$|yarn\.lock$|phpunit\.xml(\.dist)?$)";

    // Modules compiled into the bundled Windows PHP builds do not need extension= entries.
    private static readonly HashSet<string> BuiltInModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "bcmath", "ctype", "dom", "iconv", "json", "libxml", "pdo", "simplexml",
        "tokenizer", "xml", "xmlreader", "xmlwriter", "zlib"
    };

    public static IReadOnlyList<FrameworkProfile> All { get; } =
    [
        new(FrameworkPreset.None, "None (Custom)",
            "Serve a custom PHP document root without framework routing rules.",
            [], false, false, [], [], ""),

        new(FrameworkPreset.Laravel, "Laravel",
            "Serve only public/ and route missing files through public/index.php.",
            ["public"], true, true, ["artisan", "bootstrap/app.php"],
            ["curl", "fileinfo", "mbstring", "openssl", "pdo_mysql"], CommonSensitiveFiles),

        new(FrameworkPreset.Symfony, "Symfony",
            "Use public/ as the document root and index.php as the front controller.",
            ["public"], true, true, ["bin/console", "config/bundles.php"],
            ["intl", "mbstring", "openssl", "pdo_mysql"], CommonSensitiveFiles),

        new(FrameworkPreset.CodeIgniter, "CodeIgniter 4",
            "Keep application files outside public/ and route requests through index.php.",
            ["public"], true, true, ["spark", "app/Config/Paths.php"],
            ["intl", "mbstring", "mysqli"], CommonSensitiveFiles),

        new(FrameworkPreset.Yii, "Yii 2", "Use web/ as the public entry directory.",
            ["web"], true, true, ["yii", "config/web.php"],
            ["mbstring", "pdo_mysql"], CommonSensitiveFiles),

        new(FrameworkPreset.CakePHP, "CakePHP", "Use webroot/ as the public entry directory.",
            ["webroot"], true, true, ["bin/cake", "config/app.php"],
            ["intl", "mbstring", "pdo_mysql"], CommonSensitiveFiles),

        new(FrameworkPreset.Phalcon, "Phalcon", "Use public/ and the Phalcon front controller.",
            ["public"], true, true, ["app/config", "composer.json"],
            ["mbstring", "pdo_mysql", "phalcon"], CommonSensitiveFiles),

        new(FrameworkPreset.Slim, "Slim", "Use public/ and route application requests to index.php.",
            ["public"], true, true, ["composer.json"],
            ["mbstring"], CommonSensitiveFiles),

        new(FrameworkPreset.WordPress, "WordPress", "Serve the WordPress installation root with permalink rewrites.",
            [], false, true, ["wp-settings.php", "wp-config-sample.php"],
            ["curl", "fileinfo", "gd", "mbstring", "mysqli", "openssl", "zip"],
            @"(^\.|\.env(\..*)?$|wp-config\.php$|composer\.(json|lock)$)"),

        new(FrameworkPreset.Drupal, "Drupal",
            "Auto-detect a Composer web/ or docroot/ directory; legacy layouts may use the selected root.",
            ["web", "docroot"], false, true, ["core/lib/Drupal.php", "composer.json"],
            ["curl", "gd", "mbstring", "openssl", "pdo_mysql"], CommonSensitiveFiles),

        new(FrameworkPreset.Magento, "Magento / Adobe Commerce",
            "Serve only pub/ so application and configuration files remain private.",
            ["pub"], true, true, ["bin/magento", "app/etc/di.xml"],
            ["bcmath", "curl", "gd", "intl", "mbstring", "pdo_mysql", "soap", "sockets", "sodium", "xsl", "zip"],
            CommonSensitiveFiles),

        new(FrameworkPreset.Hyperf, "Hyperf (Swoole Proxy)",
            "Reverse proxy HTTP and WebSocket traffic to an already running Swoole server.",
            [], false, false, ["bin/hyperf.php", "config/autoload"], [], "",
            IsReverseProxy: true, ProxyDefault: "http://127.0.0.1:9501")
    ];

    public static FrameworkProfile Get(FrameworkPreset preset) =>
        All.FirstOrDefault(profile => profile.Preset == preset) ?? All[0];

    public static string[] GetMissingManagedExtensions(
        FrameworkProfile profile, IEnumerable<string> enabledExtensions)
    {
        var enabled = enabledExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return profile.RequiredExtensions
            .Where(extension => !BuiltInModules.Contains(extension) && !enabled.Contains(extension))
            .ToArray();
    }

    /// <summary>
    /// Resolves a project root to the safest public directory. Existing candidate directories
    /// win; strict profiles append their preferred directory for projects not created yet.
    /// </summary>
    public static string ResolveDocumentRoot(string selectedPath, FrameworkPreset preset)
    {
        var profile = Get(preset);
        if (profile.IsReverseProxy || profile.WebRootCandidates.Length == 0)
            return selectedPath;

        var path = selectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var lastName = Path.GetFileName(path);
        if (profile.WebRootCandidates.Any(candidate =>
                lastName.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
        {
            return selectedPath;
        }

        foreach (var candidate in profile.WebRootCandidates)
        {
            var child = Path.Combine(selectedPath, candidate);
            if (File.Exists(Path.Combine(child, "index.php")) || Directory.Exists(child))
                return child;
        }

        return profile.AppendPreferredWebRoot
            ? Path.Combine(selectedPath, profile.PreferredWebRoot)
            : selectedPath;
    }

    public static string GetProjectRoot(string documentRoot, FrameworkPreset preset)
    {
        var profile = Get(preset);
        var path = documentRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var lastName = Path.GetFileName(path);
        return profile.WebRootCandidates.Any(candidate =>
                lastName.Equals(candidate, StringComparison.OrdinalIgnoreCase))
            ? Directory.GetParent(path)?.FullName ?? path
            : path;
    }
}
