using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Tests;

[TestClass]
public sealed class PhpExtensionManagerTests
{
    [TestMethod]
    public void RecommendedProfile_IsAvailableInEveryBundledRuntime()
    {
        var repositoryRoot = FindRepositoryRoot();
        var phpBaseDir = Path.Combine(repositoryRoot, "HamsterServer", "php");
        var runtimes = Directory.GetDirectories(phpBaseDir);
        Assert.IsNotEmpty(runtimes);

        foreach (var phpDir in runtimes)
        {
            var available = PhpExtensionManager.GetAvailableExtensions(phpDir)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = PhpExtensionManager.RecommendedExtensions.Where(extension => !available.Contains(extension));
            Assert.IsEmpty(missing, $"PHP {Path.GetFileName(phpDir)} is missing recommended extensions.");
        }
    }

    [TestMethod]
    public void GetAvailableExtensions_ExcludesOpcacheFromStandardExtensions()
    {
        using var root = new TestDirectory();
        var phpDir = Path.Combine(root.Path, "php", "8.4");
        var extDir = Path.Combine(phpDir, "ext");
        Directory.CreateDirectory(extDir);
        File.WriteAllBytes(Path.Combine(extDir, "php_curl.dll"), []);
        File.WriteAllBytes(Path.Combine(extDir, "php_opcache.dll"), []);

        CollectionAssert.AreEqual(
            new[] { "curl" },
            PhpExtensionManager.GetAvailableExtensions(phpDir).ToArray());
    }

    [TestMethod]
    public void ResolveForRuntime_FiltersExtensionsUnavailableInSelectedVersion()
    {
        using var root = new TestDirectory();
        var phpDir = Path.Combine(root.Path, "php", "8.5");
        var extDir = Path.Combine(phpDir, "ext");
        Directory.CreateDirectory(extDir);
        File.WriteAllBytes(Path.Combine(extDir, "php_curl.dll"), []);

        var resolved = PhpExtensionManager.ResolveForRuntime(
            phpDir, ["curl", "imap", "invalid-name", "opcache"]);

        CollectionAssert.AreEqual(new[] { "curl" }, resolved.ToArray());
    }

    [TestMethod]
    public void ServerManager_SynchronizesPhpIniAcrossInstalledRuntimes()
    {
        using var root = new TestDirectory();
        foreach (var version in new[] { "8.4.24", "8.5.9" })
        {
            var phpDir = Path.Combine(root.Path, "php", version);
            Directory.CreateDirectory(phpDir);
            File.WriteAllBytes(Path.Combine(phpDir, "php-cgi.exe"), []);
        }

        using var manager = new ServerManager(root.Path, new AppConfig());
        manager.SynchronizePhpIni();

        foreach (var version in new[] { "8.4.24", "8.5.9" })
        {
            var phpIni = Path.Combine(root.Path, "php", version, "php.ini");
            Assert.IsTrue(File.Exists(phpIni), $"php.ini was not generated for PHP {version}.");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HamsterServer.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate HamsterServer.slnx from the test output directory.");
    }
}
