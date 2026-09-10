using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Tests;

[TestClass]
public sealed class PhpVersionManagerTests
{
    [TestMethod]
    public void Scan_OrdersVersionsSemantically()
    {
        using var root = new TestDirectory();
        CreatePhp(root.Path, "8.9.12");
        CreatePhp(root.Path, "8.10.1");

        var manager = new PhpVersionManager(root.Path);
        manager.Scan();

        Assert.AreEqual("8.10.1", manager.GetLatest()?.Version);
        Assert.AreEqual("8.9.12", manager.GetLatestForBranch("8.9.1")?.Version);
    }

    [TestMethod]
    public async Task ParseReleasesAsync_SelectsWindowsX64NtsZip()
    {
        const string json = """
        {
          "8.4": {
            "version": "8.4.25",
            "ts-vs17-x64": {
              "zip": { "path": "php-8.4.25-Win32-vs17-x64.zip", "sha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" }
            },
            "nts-vs17-x64": {
              "mtime": "2026-08-25T19:20:06+00:00",
              "zip": {
                "path": "php-8.4.25-nts-Win32-vs17-x64.zip",
                "size": "32 MB",
                "sha256": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
              }
            }
          }
        }
        """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var releases = await PhpVersionManager.ParseReleasesAsync(stream);

        Assert.AreEqual(1, releases.Count);
        Assert.AreEqual("8.4.25", releases[0].Version);
        Assert.AreEqual("php-8.4.25-nts-Win32-vs17-x64.zip", releases[0].PackageFileName);
        Assert.AreEqual("windows.php.net", releases[0].DownloadUri.Host);
        Assert.AreEqual(new DateTimeOffset(2026, 8, 25, 19, 20, 6, TimeSpan.Zero), releases[0].PublishedAt);
    }

    [TestMethod]
    public void GetUpdate_ReturnsLatestPatchInSameBranch()
    {
        using var root = new TestDirectory();
        var manager = new PhpVersionManager(root.Path);
        var releases = new[]
        {
            Release("8.3.12", []),
            Release("8.4.2", []),
            Release("8.3.15", [])
        };

        var update = manager.GetUpdate("8.3.10", releases);

        Assert.AreEqual("8.3.15", update?.Version);
        Assert.IsNull(manager.GetUpdate("8.3.15", releases));
    }

    [TestMethod]
    public async Task InstallAsync_VerifiesAndMovesCompleteRuntimeIntoVersionFolder()
    {
        using var root = new TestDirectory();
        byte[] archive = CreateRuntimeArchive();
        using var client = CreateClient(archive);
        var manager = new PhpVersionManager(root.Path, client);

        var installed = await manager.InstallAsync(Release("8.6.1", archive));

        Assert.AreEqual("8.6.1", installed.Version);
        Assert.IsTrue(File.Exists(Path.Combine(root.Path, "php", "8.6.1", "php-cgi.exe")));
        Assert.IsTrue(File.Exists(Path.Combine(root.Path, "php", "8.6.1", "php.exe")));
        Assert.IsFalse(Directory.GetDirectories(Path.Combine(root.Path, "php"))
            .Any(path => Path.GetFileName(path).StartsWith(".hamster-server-install-")));
    }

    [TestMethod]
    public async Task InstallAsync_ChecksumMismatchLeavesNoRuntimeOrStagingFolder()
    {
        using var root = new TestDirectory();
        byte[] archive = CreateRuntimeArchive();
        using var client = CreateClient(archive);
        var manager = new PhpVersionManager(root.Path, client);
        var release = WithHash(Release("8.6.2", archive),
            "0000000000000000000000000000000000000000000000000000000000000000");

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => manager.InstallAsync(release));

        Assert.IsFalse(Directory.Exists(Path.Combine(root.Path, "php", "8.6.2")));
        Assert.IsFalse(Directory.GetDirectories(Path.Combine(root.Path, "php"))
            .Any(path => Path.GetFileName(path).StartsWith(".hamster-server-install-")));
    }

    [TestMethod]
    public void DeleteVersion_RemovesOnlySelectedValidRuntime()
    {
        using var root = new TestDirectory();
        CreatePhp(root.Path, "8.3.1");
        CreatePhp(root.Path, "8.4.1");
        var manager = new PhpVersionManager(root.Path);
        manager.Scan();

        manager.DeleteVersion("8.3.1");

        Assert.IsFalse(Directory.Exists(Path.Combine(root.Path, "php", "8.3.1")));
        Assert.IsTrue(Directory.Exists(Path.Combine(root.Path, "php", "8.4.1")));
        Assert.ThrowsExactly<ArgumentException>(() => manager.DeleteVersion("..\\config"));
    }

    [TestMethod]
    public void DeleteVersion_ProtectsCurrentAndFinalRuntime()
    {
        using var root = new TestDirectory();
        CreatePhp(root.Path, "8.4.1");
        CreatePhp(root.Path, "8.5.1");
        var manager = new PhpVersionManager(root.Path);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            manager.DeleteVersion("8.4.1", protectedVersion: "8.4.1"));

        manager.DeleteVersion("8.4.1");
        Assert.ThrowsExactly<InvalidOperationException>(() => manager.DeleteVersion("8.5.1"));
    }

    [TestMethod]
    public void DeleteVersion_AllowsSafeCustomRuntimeFolderNames()
    {
        using var root = new TestDirectory();
        CreatePhp(root.Path, "php-8.3-custom");
        CreatePhp(root.Path, "8.4.1");
        var manager = new PhpVersionManager(root.Path);

        manager.DeleteVersion("php-8.3-custom");

        Assert.IsFalse(Directory.Exists(Path.Combine(root.Path, "php", "php-8.3-custom")));
        Assert.IsTrue(Directory.Exists(Path.Combine(root.Path, "php", "8.4.1")));
    }

    private static void CreatePhp(string root, string version)
    {
        var dir = Path.Combine(root, "php", version);
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, "php-cgi.exe"), []);
    }

    private static byte[] CreateRuntimeArchive()
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "php-cgi.exe", "cgi");
            WriteEntry(archive, "php.exe", "cli");
            WriteEntry(archive, "ext/php_curl.dll", "extension");
        }
        return output.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string path, string value)
    {
        using var writer = new StreamWriter(archive.CreateEntry(path).Open(), Encoding.UTF8);
        writer.Write(value);
    }

    private static PhpReleaseInfo Release(string version, byte[] archive) => new()
    {
        Branch = string.Join('.', version.Split('.').Take(2)),
        Version = version,
        PackageFileName = $"php-{version}-nts-Win32-vs17-x64.zip",
        DownloadUri = new Uri($"https://windows.php.net/downloads/releases/php-{version}.zip"),
        Sha256 = Convert.ToHexString(SHA256.HashData(archive)).ToLowerInvariant()
    };

    private static PhpReleaseInfo WithHash(PhpReleaseInfo release, string hash) => new()
    {
        Branch = release.Branch,
        Version = release.Version,
        PackageFileName = release.PackageFileName,
        DownloadUri = release.DownloadUri,
        Sha256 = hash,
        Size = release.Size,
        PublishedAt = release.PublishedAt
    };

    private static HttpClient CreateClient(byte[] content) =>
        new(new StaticResponseHandler(content));

    private sealed class StaticResponseHandler(byte[] content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new ByteArrayContent(content)
            });
    }
}
