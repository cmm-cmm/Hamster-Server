using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Tests;

[TestClass]
public sealed class ConfigManagerTests
{
    [TestMethod]
    public void Load_MigratesLegacyXServerConfiguration()
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        var legacyPath = Path.Combine(configDir, "xserver.json");
        File.WriteAllText(legacyPath, """{"apache":{"serverName":"legacy.test"}}""");

        var config = new ConfigManager(root.Path).Load();

        Assert.AreEqual("legacy.test", config.Apache.ServerName);
        Assert.IsTrue(File.Exists(Path.Combine(configDir, "hamster-server.json")));
        // Once the data lives under the new name the legacy file is only clutter, so
        // DeleteLegacyLeftovers removes it — but strictly after the copy succeeded.
        Assert.IsFalse(File.Exists(legacyPath),
            "Migration should remove the legacy file once the new one exists.");
    }

    [TestMethod]
    public void Constructor_KeepsLegacyFileWhenTheCopyCannotBeMade()
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        var legacyPath = Path.Combine(configDir, "xserver.json");
        File.WriteAllText(legacyPath, """{"apache":{"serverName":"legacy.test"}}""");

        // Hold the source exclusively so File.Copy cannot read it. The cleanup that follows a
        // successful migration must not run, or the only copy of the config would be gone.
        using (new FileStream(legacyPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.ThrowsExactly<IOException>(() => new ConfigManager(root.Path));
        }

        Assert.IsTrue(File.Exists(legacyPath),
            "A failed migration must not delete the only copy of the user's configuration.");
        Assert.IsFalse(File.Exists(Path.Combine(configDir, "hamster-server.json")));
    }

    [TestMethod]
    public void Load_UpgradesLegacyDefaultExtensionProfile()
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "hamster-server.json"),
            """
            {
              "php": {
                "enabledExtensions": [
                  "curl", "fileinfo", "gd", "intl", "mbstring", "openssl",
                  "pdo_mysql", "pdo_sqlite", "sockets", "sodium", "zip"
                ]
              }
            }
            """);

        var config = new ConfigManager(root.Path).Load();

        CollectionAssert.AreEquivalent(
            PhpExtensionManager.RecommendedExtensions.ToArray(),
            config.Php.EnabledExtensions.ToArray());
    }

    [TestMethod]
    public void Load_PreservesCustomEmptyExtensionProfile()
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "hamster-server.json"),
            """{"php":{"enabledExtensions":[]}}""");

        var config = new ConfigManager(root.Path).Load();

        Assert.IsEmpty(config.Php.EnabledExtensions);
    }

    [TestMethod]
    public void Load_RemovesOpcacheFromStandardExtensionProfile()
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "hamster-server.json"),
            """{"php":{"enabledExtensions":["curl","opcache"]}}""");

        var config = new ConfigManager(root.Path).Load();

        CollectionAssert.AreEqual(new[] { "curl" }, config.Php.EnabledExtensions);
        Assert.IsTrue(config.Php.EnableOpcache);
    }

    [TestMethod]
    public void Load_RecoversLastValidBackup_WhenPrimaryIsCorrupted()
    {
        using var root = new TestDirectory();
        var manager = new ConfigManager(root.Path);
        manager.Config.Apache.ServerName = "backup.test";
        manager.Save();
        manager.Config.Apache.ServerName = "primary.test";
        manager.Save();

        var configPath = Path.Combine(root.Path, "config", "hamster-server.json");
        File.WriteAllText(configPath, "{ invalid json");

        var recovered = new ConfigManager(root.Path);
        recovered.Load();

        Assert.IsTrue(recovered.LastLoadRecovered);
        Assert.IsFalse(recovered.LastLoadCorrupted);
        Assert.AreEqual("backup.test", recovered.Config.Apache.ServerName);

        recovered.Save();
        var reloaded = new ConfigManager(root.Path);
        reloaded.Load();
        Assert.AreEqual("backup.test", reloaded.Config.Apache.ServerName);
        StringAssert.Contains(File.ReadAllText(configPath + ".bak"), "backup.test");
    }

    [TestMethod]
    public void Load_NormalizesNullSectionsAndConflictingPorts()
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "hamster-server.json"),
            """{"apache":{"port":80,"sslPort":80},"php":null,"vhosts":null}""");

        var manager = new ConfigManager(root.Path);
        var config = manager.Load();

        Assert.IsNotNull(config.Php);
        Assert.IsNotNull(config.VHosts);
        Assert.AreNotEqual(config.Apache.Port, config.Apache.SslPort);
        Assert.AreNotEqual(config.Apache.Port, config.Php.FcgiPort);
        Assert.IsTrue(config.Php.UseBundledCaCertificate,
            "Existing configurations should opt into the bundled CA certificate by default.");
    }

    [TestMethod]
    public void Load_NormalizesInactiveVHostPortAndUnknownProfile()
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "hamster-server.json"),
            """
            {
              "apache": { "port": 8080, "sslPort": 8443 },
              "php": { "fcgiPort": 9000 },
              "vhosts": [
                { "serverName": "app.test", "documentRoot": "www/app", "port": 12345, "framework": 999 }
              ]
            }
            """);

        var config = new ConfigManager(root.Path).Load();

        Assert.IsTrue(config.Php.UseBundledCaCertificate,
            "The bundled CA certificate should be enabled when an existing PHP section omits the new setting.");
        Assert.AreEqual(0, config.VHosts[0].Port);
        Assert.AreEqual(FrameworkPreset.None, config.VHosts[0].Framework);
    }

    // ── Php.MaxRequests ──────────────────────────────────────────────────────
    // 0 means "never recycle", which is what makes FastCGI connection pooling safe. Clamping it up
    // to 1 — as the old range did — would recycle after every single request instead.
    [DataTestMethod]
    [DataRow(0, 0)]
    [DataRow(-5, 0)]
    [DataRow(5000, 5000)]
    [DataRow(2_000_000, 1_000_000)]
    public void Load_NormalizesMaxRequestsWithoutClampingZeroUp(int stored, int expected)
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "hamster-server.json"),
            "{\"php\":{\"maxRequests\":" + stored + "}}");

        var config = new ConfigManager(root.Path).Load();

        Assert.AreEqual(expected, config.Php.MaxRequests);
    }

    [TestMethod]
    public void Load_UpgradesLegacyDefaultMaxRequestsToNeverRecycle()
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "hamster-server.json"),
            """{"php":{"maxRequests":1000}}""");

        var config = new ConfigManager(root.Path).Load();

        Assert.AreEqual(0, config.Php.MaxRequests,
            "The previous default recycled workers often enough to race connection reuse.");
    }

    // 0 disables the memory watchdog. A small positive value is raised to a floor instead: a
    // threshold below what a freshly started pool already uses would restart it forever.
    [DataTestMethod]
    [DataRow(0, 0)]
    [DataRow(-1, 0)]
    [DataRow(64, 128)]
    [DataRow(2048, 2048)]
    [DataRow(999_999, 65_536)]
    public void Load_NormalizesMemoryRestartThreshold(int stored, int expected)
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "hamster-server.json"),
            "{\"php\":{\"memoryRestartThreshold\":" + stored + "}}");

        var config = new ConfigManager(root.Path).Load();

        Assert.AreEqual(expected, config.Php.MemoryRestartThreshold);
    }

    [TestMethod]
    public void Load_EnablesMemoryWatchdogForConfigurationsPredatingIt()
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "hamster-server.json"),
            """{"php":{"workers":8}}""");

        var config = new ConfigManager(root.Path).Load();

        Assert.AreEqual(1024, config.Php.MemoryRestartThreshold,
            "Without recycling, an older config has nothing reclaiming leaked memory.");
    }

    [TestMethod]
    public void NewConfiguration_DefaultsToNeverRecyclingWorkers()
    {
        using var root = new TestDirectory();

        var config = new ConfigManager(root.Path).Load();

        Assert.AreEqual(0, config.Php.MaxRequests);
        Assert.IsTrue(config.Php.Workers is >= 4 and <= 8,
            "Worker count is capped rather than scaled by core count; each worker costs ~35 MB.");
    }
}
