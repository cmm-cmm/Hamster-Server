using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Tests;

[TestClass]
public sealed class PhpUpdateTests
{
    // ── Carrying extensions between runtimes ─────────────────────────────────

    [TestMethod]
    public void FindMissingFiles_OffersUserAddedDllsFromTheSameBranchOnly()
    {
        using var root = new TestDirectory();
        CreateRuntime(root.Path, "8.5.1", "ext/php_curl.dll", "ext/php_sqlsrv.dll", "ext/php_pdo_sqlsrv.dll",
                      "CORE_RL_MagickCore_.dll", "php8.dll");
        CreateRuntime(root.Path, "8.5.2", "ext/php_curl.dll", "php8.dll");
        CreateRuntime(root.Path, "8.4.9", "ext/php_xdebug.dll");
        var manager = new PhpVersionManager(root.Path);

        var missing = manager.FindMissingFiles("8.5.2").Select(f => f.RelativePath).ToArray();

        CollectionAssert.AreEquivalent(
            new[] { "CORE_RL_MagickCore_.dll", Path.Combine("ext", "php_pdo_sqlsrv.dll"), Path.Combine("ext", "php_sqlsrv.dll") },
            missing,
            "Only same-branch extras: never the 8.4 xdebug (other ABI), never php*.dll core files, never files the target has.");
        Assert.IsTrue(manager.FindMissingFiles("8.4.9").Count == 0, "8.4 has no same-branch sibling to copy from.");
    }

    [TestMethod]
    public void CopyMissingFiles_CopiesWithoutOverwritingTheNewerRuntime()
    {
        using var root = new TestDirectory();
        CreateRuntime(root.Path, "8.5.1", "ext/php_sqlsrv.dll", "ext/php_curl.dll");
        CreateRuntime(root.Path, "8.5.2", "ext/php_curl.dll");
        File.WriteAllText(Path.Combine(root.Path, "php", "8.5.1", "ext", "php_curl.dll"), "old official");
        File.WriteAllText(Path.Combine(root.Path, "php", "8.5.2", "ext", "php_curl.dll"), "new official");
        var manager = new PhpVersionManager(root.Path);
        manager.Scan();

        var copied = manager.CopyMissingFiles("8.5.2", "8.5.1");

        CollectionAssert.AreEqual(new[] { Path.Combine("ext", "php_sqlsrv.dll") }, copied.ToArray());
        Assert.IsTrue(File.Exists(Path.Combine(root.Path, "php", "8.5.2", "ext", "php_sqlsrv.dll")));
        Assert.AreEqual("new official", File.ReadAllText(Path.Combine(root.Path, "php", "8.5.2", "ext", "php_curl.dll")));
    }

    [TestMethod]
    public void GetSupersededVersions_ListsOlderPatchesButNeverTheActiveOne()
    {
        using var root = new TestDirectory();
        foreach (var version in new[] { "8.5.1", "8.5.2", "8.5.3", "8.4.9" })
            CreateRuntime(root.Path, version);
        var manager = new PhpVersionManager(root.Path);

        var superseded = manager.GetSupersededVersions(protectedVersion: "8.5.1").Select(v => v.Version).ToArray();

        CollectionAssert.AreEquivalent(new[] { "8.5.2" }, superseded);
    }

    // ── The updater ──────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Auto_InstallsThePatchCarriesExtensionsOverAndSwitches()
    {
        using var root = new TestDirectory();
        CreateRuntime(root.Path, "8.5.1", "ext/php_curl.dll", "ext/php_sqlsrv.dll", "ext/php_pdo_sqlsrv.dll");
        var (service, server, updated) = CreateService(root.Path, "8.5.1", "8.5.2");

        await service.CheckAsync(PhpUpdateMode.Auto);

        Assert.AreEqual(PhpUpdateState.Updated, service.State, service.LastError);
        Assert.AreEqual("8.5.2", server.Config.Php.CurrentVersion);
        var newExt = Path.Combine(root.Path, "php", "8.5.2", "ext");
        Assert.IsTrue(File.Exists(Path.Combine(newExt, "php_sqlsrv.dll")), "sqlsrv must follow the user to 8.5.2.");
        Assert.IsTrue(File.Exists(Path.Combine(newExt, "php_pdo_sqlsrv.dll")));
        Assert.IsTrue(Directory.Exists(Path.Combine(root.Path, "php", "8.5.1")), "The old patch is kept for rollback.");
        Assert.IsNotNull(updated.Value);
        Assert.IsTrue(updated.Value.Switched);
        Assert.AreEqual(2, updated.Value.CopiedFiles.Count);
    }

    [TestMethod]
    public async Task Notify_ReportsTheUpdateWithoutInstallingAnything()
    {
        using var root = new TestDirectory();
        CreateRuntime(root.Path, "8.5.1", "ext/php_curl.dll");
        var (service, server, _) = CreateService(root.Path, "8.5.1", "8.5.2");

        await service.CheckAsync(PhpUpdateMode.Notify);

        Assert.AreEqual(PhpUpdateState.UpdateAvailable, service.State);
        Assert.AreEqual("8.5.2", service.AvailableUpdate?.Version);
        Assert.AreEqual("8.5.1", server.Config.Php.CurrentVersion);
        Assert.IsFalse(Directory.Exists(Path.Combine(root.Path, "php", "8.5.2")));
    }

    [TestMethod]
    public async Task Auto_NeverMovesToAnotherBranch()
    {
        using var root = new TestDirectory();
        CreateRuntime(root.Path, "8.4.9", "ext/php_curl.dll");
        // The catalog only knows 8.5.2: a newer branch, not a patch of 8.4.
        var (service, server, _) = CreateService(root.Path, "8.4.9", "8.5.2");

        await service.CheckAsync(PhpUpdateMode.Auto);

        Assert.AreEqual(PhpUpdateState.UpToDate, service.State);
        Assert.AreEqual("8.4.9", server.Config.Php.CurrentVersion);
        Assert.IsFalse(Directory.Exists(Path.Combine(root.Path, "php", "8.5.2")));
    }

    [TestMethod]
    public async Task CatalogFailure_IsReportedNotThrown()
    {
        using var root = new TestDirectory();
        CreateRuntime(root.Path, "8.5.1");
        using var client = new HttpClient(new FailingHandler());
        var manager = new PhpVersionManager(root.Path, client);
        var server = new ServerManager(root.Path, new AppConfig { Php = { CurrentVersion = "8.5.1" } });
        var service = new PhpUpdateService(manager, server);

        await service.CheckAsync(PhpUpdateMode.Auto);

        Assert.AreEqual(PhpUpdateState.Failed, service.State);
        Assert.IsNotNull(service.LastError);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (PhpUpdateService Service, ServerManager Server, StrongBox<PhpUpdatedEventArgs?> Updated)
        CreateService(string root, string current, string catalogVersion)
    {
        byte[] archive = CreateArchive();
        var handler = new CatalogHandler(CatalogJson(catalogVersion, archive), archive);
        var manager = new PhpVersionManager(root, new HttpClient(handler));
        var server = new ServerManager(root, new AppConfig { Php = { CurrentVersion = current } });
        var service = new PhpUpdateService(manager, server) { IdlePollMs = 10 };
        var updated = new StrongBox<PhpUpdatedEventArgs?>();
        service.Updated += (_, e) => updated.Value = e;
        return (service, server, updated);
    }

    private static void CreateRuntime(string root, string version, params string[] files)
    {
        var dir = Path.Combine(root, "php", version);
        Directory.CreateDirectory(Path.Combine(dir, "ext"));
        File.WriteAllBytes(Path.Combine(dir, "php-cgi.exe"), []);
        foreach (var file in files)
        {
            var path = Path.Combine(dir, file.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, file);
        }
    }

    private static byte[] CreateArchive()
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in new[] { "php-cgi.exe", "php.exe", "ext/php_curl.dll" })
            {
                using var writer = new StreamWriter(zip.CreateEntry(entry).Open(), Encoding.UTF8);
                writer.Write(entry);
            }
        }
        return output.ToArray();
    }

    private static byte[] CatalogJson(string version, byte[] archive)
    {
        string branch = string.Join('.', version.Split('.').Take(2));
        string hash = Convert.ToHexString(SHA256.HashData(archive)).ToLowerInvariant();
        return Encoding.UTF8.GetBytes($$"""
            {
              "{{branch}}": {
                "version": "{{version}}",
                "nts-vs17-x64": {
                  "mtime": "2026-10-24T10:00:00+00:00",
                  "zip": { "path": "php-{{version}}-nts-Win32-vs17-x64.zip", "size": "31.2MB", "sha256": "{{hash}}" }
                }
              }
            }
            """);
    }

    private sealed class StrongBox<T>
    {
        public T? Value { get; set; }
    }

    /// <summary>
    /// Serves releases.json and the zip. With <paramref name="redirectCatalog"/> it answers the catalog
    /// the way windows.php.net does since 2026 — from downloads.php.net/~windows/releases/ after a 302,
    /// which HttpClient follows and reports through <see cref="HttpResponseMessage.RequestMessage"/>.
    /// </summary>
    private sealed class CatalogHandler(byte[] catalog, byte[] archive, bool redirectCatalog = false) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            bool isCatalog = request.RequestUri!.AbsolutePath.EndsWith("releases.json", StringComparison.Ordinal);
            var answeredBy = request;
            if (isCatalog && redirectCatalog)
                answeredBy = new HttpRequestMessage(HttpMethod.Get, "https://downloads.php.net/~windows/releases/releases.json");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = answeredBy,
                Content = new ByteArrayContent(isCatalog ? catalog : archive)
            });
        }
    }

    /// <summary>
    /// Talks to the real php.net, so it only runs with HAMSTER_LIVE_PHPNET=1. This is the check that
    /// would have caught the rebuild-suffix regression: the fixtures above only know what we expected.
    /// </summary>
    [TestMethod]
    [TestCategory("Integration")]
    public async Task LiveCatalog_ListsEverySupportedBranch()
    {
        if (Environment.GetEnvironmentVariable("HAMSTER_LIVE_PHPNET") != "1")
            Assert.Inconclusive("Live php.net check skipped; set HAMSTER_LIVE_PHPNET=1 to run it.");

        using var root = new TestDirectory();
        var releases = await new PhpVersionManager(root.Path).GetAvailableReleasesAsync();

        foreach (var release in releases)
            Console.WriteLine($"{release.Version,-10} {release.PackageFileName}  {release.DownloadUri}");
        var branches = releases.Select(r => PhpVersionKey.ParseOrZero(r.Version).Branch).ToHashSet();
        foreach (var branch in new[] { "8.2", "8.3", "8.4", "8.5" })
            Assert.IsTrue(branches.Contains(branch), $"PHP {branch} is missing from the parsed catalog.");

        // HAMSTER_LIVE_INSTALL=1 additionally downloads (~35 MB), verifies and installs the newest
        // release into the temp root, then asks the installed php.exe for its version.
        if (Environment.GetEnvironmentVariable("HAMSTER_LIVE_INSTALL") != "1") return;
        var newest = releases[0];
        var manager = new PhpVersionManager(root.Path);
        var installed = await manager.InstallAsync(newest);
        using var php = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = Path.Combine(installed.DirectoryPath, "php.exe"),
            ArgumentList = { "-n", "-v" },
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;
        string output = await php.StandardOutput.ReadToEndAsync();
        await php.WaitForExitAsync();
        Console.WriteLine(output);
        StringAssert.StartsWith(output, $"PHP {PhpVersionKey.ParseOrZero(newest.Version).Version} ");
        Assert.AreEqual(newest.Version, Path.GetFileName(installed.DirectoryPath));
    }

    // ── The real October 2026 catalog shape ──────────────────────────────────

    [TestMethod]
    public void PhpVersionKey_UnderstandsWindowsRebuildSuffixes()
    {
        string[] ascending = ["8.5.9", "8.5.12", "8.5.12-1", "8.5.12-2", "8.5.13"];
        var keys = ascending.Select(PhpVersionKey.ParseOrZero).ToArray();
        for (int i = 1; i < keys.Length; i++)
            Assert.IsTrue(keys[i] > keys[i - 1], $"{ascending[i]} must be newer than {ascending[i - 1]}");

        Assert.AreEqual("8.5", PhpVersionKey.ParseOrZero("8.5.12-1").Branch);
        Assert.AreEqual("8.5.12-1", PhpVersionKey.ParseOrZero("8.5.12-1").ToString());
        foreach (var bad in new[] { "8.5", "8.5.12-", "8.5.12-rc1", "..\\8.5.12", "8.5.12 ", "", "8.5.12-1-2" })
            Assert.IsFalse(PhpVersionKey.IsValid(bad), $"'{bad}' must be rejected: it doubles as a folder-name guard.");
    }

    [TestMethod]
    public async Task ParseReleases_KeepsRebuiltReleasesAndIgnoresNonPackageEntries()
    {
        // Trimmed from https://downloads.php.net/~windows/releases/releases.json as served on
        // 2026-11-02: every supported branch carries a "-1" rebuild suffix and non-package keys.
        const string json = """
        {
          "8.5": {
            "version": "8.5.12-1",
            "source": { "path": "php-8.5.12-1-src.zip", "size": "28.4MB" },
            "test_pack": { "path": "php-test-pack-8.5.12-1.zip", "size": "16.3MB" },
            "ts-vs17-x64": { "mtime": "2026-10-26T13:05:05+00:00",
              "zip": { "path": "php-8.5.12-1-Win32-vs17-x64.zip", "size": "34.7MB",
                       "sha256": "1111111111111111111111111111111111111111111111111111111111111111" } },
            "nts-vs17-x64": { "mtime": "2026-10-26T13:05:05+00:00",
              "zip": { "path": "php-8.5.12-1-nts-Win32-vs17-x64.zip", "size": "34.6MB",
                       "sha256": "2222222222222222222222222222222222222222222222222222222222222222" } }
          },
          "8.4": {
            "version": "8.4.27-1",
            "nts-vs17-x64": { "mtime": "2026-10-26T13:06:17+00:00",
              "zip": { "path": "php-8.4.27-1-nts-Win32-vs17-x64.zip", "size": "33.1MB",
                       "sha256": "3333333333333333333333333333333333333333333333333333333333333333" } }
          }
        }
        """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var releases = await PhpVersionManager.ParseReleasesAsync(
            stream, new Uri("https://downloads.php.net/~windows/releases/"));

        CollectionAssert.AreEqual(new[] { "8.5.12-1", "8.4.27-1" }, releases.Select(r => r.Version).ToArray());
        Assert.AreEqual("php-8.5.12-1-nts-Win32-vs17-x64.zip", releases[0].PackageFileName);
        Assert.AreEqual("https://downloads.php.net/~windows/releases/php-8.5.12-1-nts-Win32-vs17-x64.zip",
            releases[0].DownloadUri.AbsoluteUri);
        Assert.AreEqual("8.5.12-1", new PhpVersionManager(Path.GetTempPath()).GetUpdate("8.5.9", releases)?.Version);
    }

    [TestMethod]
    public async Task Auto_UpdatesFrom859ToARebuiltRelease()
    {
        using var root = new TestDirectory();
        CreateRuntime(root.Path, "8.5.9", "ext/php_curl.dll", "ext/php_sqlsrv.dll");
        byte[] archive = CreateArchive();
        var handler = new CatalogHandler(CatalogJson("8.5.12-1", archive), archive, redirectCatalog: true);
        var manager = new PhpVersionManager(root.Path, new HttpClient(handler));
        var server = new ServerManager(root.Path, new AppConfig { Php = { CurrentVersion = "8.5.9" } });
        var service = new PhpUpdateService(manager, server) { IdlePollMs = 10 };

        await service.CheckAsync(PhpUpdateMode.Auto);

        Assert.AreEqual(PhpUpdateState.Updated, service.State, service.LastError);
        Assert.AreEqual("8.5.12-1", server.Config.Php.CurrentVersion);
        Assert.IsTrue(File.Exists(Path.Combine(root.Path, "php", "8.5.12-1", "ext", "php_sqlsrv.dll")));
        Assert.AreEqual("downloads.php.net", handler.Requests.Last().Host,
            "The package is fetched from where the catalog really came from, not via a second redirect.");

        // A later rebuild of the same PHP version is an update as well.
        Assert.AreEqual("8.5.12-2", manager.GetUpdate("8.5.12-1",
            [new PhpReleaseInfo { Version = "8.5.12-2" }, new PhpReleaseInfo { Version = "8.5.12-1" }])?.Version);
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("No route to windows.php.net");
    }
}
