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
        Assert.IsTrue(File.Exists(legacyPath), "Migration should leave the legacy file as a fallback.");
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
}
