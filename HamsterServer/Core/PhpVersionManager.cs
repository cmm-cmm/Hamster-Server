using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using HamsterServer.Models;

namespace HamsterServer.Core;

public sealed class PhpVersionInfo
{
    public string Version { get; init; } = "";
    public string DirectoryPath { get; init; } = "";
    public string CgiPath { get; init; } = "";

    public override string ToString() => Version;
}

public sealed partial class PhpVersionManager
{
    private static readonly Uri DefaultCatalogUri =
        new("https://windows.php.net/downloads/releases/releases.json");
    private static readonly Uri ReleasesBaseUri =
        new("https://windows.php.net/downloads/releases/");
    private static readonly HttpClient SharedHttpClient = CreateHttpClient();

    private readonly string _phpBaseDir;
    private readonly HttpClient _httpClient;
    private readonly Uri _catalogUri;

    public List<PhpVersionInfo> Versions { get; private set; } = [];

    public PhpVersionManager(string rootDir, HttpClient? httpClient = null, Uri? catalogUri = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDir);
        _phpBaseDir = Path.GetFullPath(Path.Combine(rootDir, "php"));
        _httpClient = httpClient ?? SharedHttpClient;
        _catalogUri = catalogUri ?? DefaultCatalogUri;
    }

    public List<PhpVersionInfo> Scan()
    {
        Versions = [];

        if (!Directory.Exists(_phpBaseDir))
        {
            Directory.CreateDirectory(_phpBaseDir);
            return Versions;
        }

        foreach (var dir in Directory.GetDirectories(_phpBaseDir)
                     .Where(d => !Path.GetFileName(d).StartsWith(".hamster-server-", StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(d => ParseVersion(Path.GetFileName(d)))
                     .ThenByDescending(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase))
        {
            var cgiPath = Path.Combine(dir, "php-cgi.exe");
            if (File.Exists(cgiPath))
            {
                Versions.Add(new PhpVersionInfo
                {
                    Version = Path.GetFileName(dir),
                    DirectoryPath = dir,
                    CgiPath = cgiPath
                });
            }
        }

        return Versions;
    }

    public PhpVersionInfo? GetVersion(string version) =>
        Versions.FirstOrDefault(v => v.Version.Equals(version, StringComparison.OrdinalIgnoreCase));

    public PhpVersionInfo? GetLatest() => Versions.FirstOrDefault();

    public PhpVersionInfo? GetLatestForBranch(string version)
    {
        if (!Version.TryParse(version, out var requested)) return null;
        return Versions.FirstOrDefault(v =>
            Version.TryParse(v.Version, out var candidate) &&
            candidate.Major == requested.Major &&
            candidate.Minor == requested.Minor);
    }

    public async Task<IReadOnlyList<PhpReleaseInfo>> GetAvailableReleasesAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            _catalogUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        EnsureTrustedPhpUri(response.RequestMessage?.RequestUri, "release catalog");

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await ParseReleasesAsync(stream, cancellationToken);
    }

    public static async Task<IReadOnlyList<PhpReleaseInfo>> ParseReleasesAsync(
        Stream json, CancellationToken cancellationToken = default)
    {
        using var document = await JsonDocument.ParseAsync(json, cancellationToken: cancellationToken);
        var releases = new List<PhpReleaseInfo>();

        foreach (var branchProperty in document.RootElement.EnumerateObject())
        {
            if (branchProperty.Value.ValueKind != JsonValueKind.Object ||
                !branchProperty.Value.TryGetProperty("version", out var versionElement))
                continue;

            string version = versionElement.GetString() ?? "";
            if (!IsSafeVersion(version)) continue;

            var packageProperty = branchProperty.Value.EnumerateObject().FirstOrDefault(p =>
                p.Name.StartsWith("nts-", StringComparison.OrdinalIgnoreCase) &&
                p.Name.EndsWith("-x64", StringComparison.OrdinalIgnoreCase) &&
                p.Value.ValueKind == JsonValueKind.Object);
            if (packageProperty.Value.ValueKind != JsonValueKind.Object ||
                !packageProperty.Value.TryGetProperty("zip", out var zip) ||
                zip.ValueKind != JsonValueKind.Object ||
                !zip.TryGetProperty("path", out var pathElement) ||
                !zip.TryGetProperty("sha256", out var hashElement))
                continue;

            string packagePath = pathElement.GetString() ?? "";
            string hash = hashElement.GetString() ?? "";
            if (!IsSafePackageName(packagePath) || !IsSha256(hash)) continue;

            DateTimeOffset? publishedAt = null;
            if (packageProperty.Value.TryGetProperty("mtime", out var mtime) &&
                DateTimeOffset.TryParse(mtime.GetString(), out var parsedDate))
                publishedAt = parsedDate;

            releases.Add(new PhpReleaseInfo
            {
                Branch = branchProperty.Name,
                Version = version,
                PackageFileName = packagePath,
                DownloadUri = new Uri(ReleasesBaseUri, packagePath),
                Sha256 = hash.ToLowerInvariant(),
                Size = zip.TryGetProperty("size", out var size) ? size.GetString() ?? "" : "",
                PublishedAt = publishedAt
            });
        }

        return releases
            .OrderByDescending(release => ParseVersion(release.Version))
            .ToList();
    }

    public PhpReleaseInfo? GetUpdate(
        string installedVersion, IEnumerable<PhpReleaseInfo> availableReleases)
    {
        if (!Version.TryParse(installedVersion, out var installed)) return null;

        return availableReleases
            .Where(release => Version.TryParse(release.Version, out var candidate) &&
                              candidate.Major == installed.Major &&
                              candidate.Minor == installed.Minor &&
                              candidate > installed)
            .OrderByDescending(release => ParseVersion(release.Version))
            .FirstOrDefault();
    }

    public async Task<PhpVersionInfo> InstallAsync(
        PhpReleaseInfo release,
        IProgress<PhpDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(release);
        ValidateRelease(release);

        Directory.CreateDirectory(_phpBaseDir);
        string destination = GetVersionDirectory(release.Version, requireNumericVersion: true);
        if (Directory.Exists(destination))
            throw new InvalidOperationException($"PHP {release.Version} is already installed.");

        string stagingDirectory = Path.Combine(_phpBaseDir, $".hamster-server-install-{Guid.NewGuid():N}");
        string archivePath = Path.Combine(stagingDirectory, release.PackageFileName);
        string extractionDirectory = Path.Combine(stagingDirectory, "runtime");

        Directory.CreateDirectory(stagingDirectory);
        try
        {
            await DownloadAsync(release.DownloadUri, archivePath, progress, cancellationToken);
            progress?.Report(new PhpDownloadProgress { Stage = "Verifying SHA-256…" });
            await VerifyChecksumAsync(archivePath, release.Sha256, cancellationToken);

            progress?.Report(new PhpDownloadProgress { Stage = "Extracting PHP runtime…" });
            Directory.CreateDirectory(extractionDirectory);
            await ExtractZipSafelyAsync(archivePath, extractionDirectory, cancellationToken);

            string runtimeDirectory = FindRuntimeDirectory(extractionDirectory);
            if (!File.Exists(Path.Combine(runtimeDirectory, "php.exe")))
                throw new InvalidDataException("The PHP archive does not contain php.exe.");

            cancellationToken.ThrowIfCancellationRequested();
            if (Directory.Exists(destination))
                throw new InvalidOperationException($"PHP {release.Version} was installed by another operation.");

            Directory.Move(runtimeDirectory, destination);
            Scan();
            return GetVersion(release.Version)
                ?? throw new InvalidDataException("The installed PHP runtime could not be detected.");
        }
        finally
        {
            DeleteStagingDirectory(stagingDirectory);
        }
    }

    public void DeleteVersion(string version, string? protectedVersion = null)
    {
        string directory = GetVersionDirectory(version, requireNumericVersion: false);
        if (version.Equals(protectedVersion, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The current PHP version cannot be deleted.");

        Scan();
        if (Versions.Count <= 1)
            throw new InvalidOperationException("Hamster Server must keep at least one installed PHP runtime.");
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"PHP {version} is not installed.");
        if (!File.Exists(Path.Combine(directory, "php-cgi.exe")))
            throw new InvalidOperationException("The selected folder is not a valid PHP runtime.");

        Directory.Delete(directory, recursive: true);
        Scan();
    }

    private async Task DownloadAsync(
        Uri uri,
        string destination,
        IProgress<PhpDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        EnsureTrustedPhpUri(response.RequestMessage?.RequestUri, "PHP package");

        long? totalBytes = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = new FileStream(
            destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        var buffer = new byte[81920];
        long received = 0;
        while (true)
        {
            int read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            received += read;
            progress?.Report(new PhpDownloadProgress
            {
                Stage = "Downloading…",
                BytesReceived = received,
                TotalBytes = totalBytes
            });
        }
    }

    private static async Task VerifyChecksumAsync(
        string archivePath, string expectedHash, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            archivePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        byte[] actual = await SHA256.HashDataAsync(stream, cancellationToken);
        byte[] expected = Convert.FromHexString(expectedHash);
        if (!CryptographicOperations.FixedTimeEquals(actual, expected))
            throw new InvalidDataException("SHA-256 verification failed. The downloaded archive was not installed.");
    }

    private static async Task ExtractZipSafelyAsync(
        string archivePath, string destination, CancellationToken cancellationToken)
    {
        string root = Path.GetFullPath(destination);
        string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        await using var stream = File.OpenRead(archivePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string targetPath = Path.GetFullPath(Path.Combine(root, entry.FullName));
            if (!targetPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The PHP archive contains an unsafe path.");

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(targetPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            await using var source = entry.Open();
            await using var target = new FileStream(
                targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await source.CopyToAsync(target, cancellationToken);
        }
    }

    private static string FindRuntimeDirectory(string extractionDirectory)
    {
        string directCgi = Path.Combine(extractionDirectory, "php-cgi.exe");
        if (File.Exists(directCgi)) return extractionDirectory;

        string[] cgiFiles = Directory.GetFiles(
            extractionDirectory, "php-cgi.exe", SearchOption.AllDirectories);
        if (cgiFiles.Length != 1)
            throw new InvalidDataException("The PHP archive does not contain exactly one php-cgi.exe.");
        return Path.GetDirectoryName(cgiFiles[0])!;
    }

    private string GetVersionDirectory(string version, bool requireNumericVersion)
    {
        if (!IsSafeDirectoryName(version))
            throw new ArgumentException("The PHP version is not a valid runtime folder name.", nameof(version));
        if (requireNumericVersion && !IsSafeVersion(version))
            throw new ArgumentException("The PHP version must use the numeric major.minor.patch format.", nameof(version));

        string directory = Path.GetFullPath(Path.Combine(_phpBaseDir, version));
        string prefix = _phpBaseDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!directory.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The PHP version resolves outside the runtime directory.", nameof(version));
        return directory;
    }

    private static void ValidateRelease(PhpReleaseInfo release)
    {
        if (!IsSafeVersion(release.Version))
            throw new ArgumentException("The release contains an invalid PHP version.", nameof(release));
        if (!IsSafePackageName(release.PackageFileName))
            throw new ArgumentException("The release contains an invalid package name.", nameof(release));
        if (!IsSha256(release.Sha256))
            throw new ArgumentException("The release does not contain a valid SHA-256 checksum.", nameof(release));
        if (!release.DownloadUri.IsAbsoluteUri || release.DownloadUri.Scheme != Uri.UriSchemeHttps ||
            release.DownloadUri.Host is not ("windows.php.net" or "downloads.php.net"))
            throw new ArgumentException("PHP packages must be downloaded over HTTPS from php.net.", nameof(release));
    }

    private static void EnsureTrustedPhpUri(Uri? uri, string resource)
    {
        if (uri is null || uri.Scheme != Uri.UriSchemeHttps ||
            uri.Host is not ("windows.php.net" or "downloads.php.net"))
            throw new InvalidDataException($"The {resource} was redirected outside the trusted php.net hosts.");
    }

    private static void DeleteStagingDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // A locked temporary file stays hidden and is never exposed as an installed runtime.
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("HamsterServer-PHP-Version-Manager/1.2");
        return client;
    }

    private static bool IsSafePackageName(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        Path.GetFileName(value).Equals(value, StringComparison.Ordinal) &&
        value.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

    private static bool IsSafeDirectoryName(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value is not "." and not ".." &&
        !value.StartsWith(".hamster-server-", StringComparison.OrdinalIgnoreCase) &&
        Path.GetFileName(value).Equals(value, StringComparison.Ordinal) &&
        value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    private static bool IsSha256(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private static bool IsSafeVersion(string value) => PhpVersionPattern().IsMatch(value);

    private static Version ParseVersion(string value) =>
        Version.TryParse(value, out var version) ? version : new Version(0, 0);

    [GeneratedRegex(@"^\d+\.\d+\.\d+(?:\.\d+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex PhpVersionPattern();
}
