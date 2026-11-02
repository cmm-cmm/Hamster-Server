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
    // Auto is off here so the stored number is the one that counts; with Auto on, 0 means "fall
    // back to a usable manual value", which is covered separately below.
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
            "{\"php\":{\"memoryRestartAuto\":false,\"memoryRestartThreshold\":" + stored + "}}");

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

    // The automatic threshold has to scale with the worker count; a fixed number cannot. Real
    // operational logs show a 12-worker pool settling at 482 MB after each recycle, so a 1024 MB
    // fixed threshold was only 2.1x its floor and recycled the pool seven times in a working day.
    [DataTestMethod]
    [DataRow(4, 512)]      // 4 x 40 x 3 = 480, lifted to the 512 MB floor
    [DataRow(8, 960)]
    [DataRow(12, 1440)]    // comfortably above the 482 MB measured in production
    [DataRow(24, 2880)]
    public void AutoMemoryThreshold_ScalesWithWorkerCount(int workers, int expected)
    {
        int actual = ServerManager.AutoMemoryThresholdMb(workers);

        // The result is also capped at half of system RAM, so only assert the formula when that
        // cap is not the binding constraint on the machine running the test.
        long halfRamMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024 / 2;
        if (expected <= halfRamMb)
            Assert.AreEqual(expected, actual);
        else
            Assert.IsTrue(actual <= halfRamMb, $"{actual} MB exceeds half of RAM ({halfRamMb} MB).");
    }

    [TestMethod]
    public void AutoMemoryThreshold_StaysAboveTheFloorItIsDerivedFrom()
    {
        foreach (int workers in new[] { 1, 4, 8, 12, 32, 256 })
        {
            int floor = workers * ServerManager.WorkerBaselineMb;
            int auto = ServerManager.AutoMemoryThresholdMb(workers);
            Assert.IsTrue(auto > floor,
                $"{workers} workers: threshold {auto} MB is not above the {floor} MB floor, " +
                "so the pool could never get under it.");
        }
    }

    // CPU decides the useful ceiling (4-8, from the measured load ramp); RAM decides the safe one.
    [TestMethod]
    public void SuggestWorkers_NeverExceedsWhatTheCpuCanUse()
    {
        var advice = ServerManager.SuggestWorkers(memoryLimitMb: 512);

        Assert.IsTrue(advice.Workers is >= 1 and <= 8,
            $"Suggested {advice.Workers} workers; the measured ceiling is 8 however many cores exist.");
        Assert.AreEqual(Environment.ProcessorCount, advice.Cores);
    }

    // An absurd per-worker memory limit has to pull the suggestion down to 1, and say why.
    [TestMethod]
    public void SuggestWorkers_IsCutDownWhenRamCannotCoverTheMemoryLimit()
    {
        long totalRamMb = ServerManager.TotalRamMb;
        if (totalRamMb <= 0) Assert.Inconclusive("Total RAM could not be read on this machine.");

        var advice = ServerManager.SuggestWorkers(memoryLimitMb: (int)totalRamMb);

        Assert.AreEqual(1, advice.Workers, "One worker at the full size of RAM is already over budget.");
        Assert.IsTrue(advice.RamLimited, "The advice should report RAM as the binding limit.");
    }

    [TestMethod]
    public void SuggestWorkers_ReportsCpuAsTheLimitWhenRamIsPlentiful()
    {
        // 1 MB per worker cannot exhaust any machine's RAM, so CPU must be the constraint.
        var advice = ServerManager.SuggestWorkers(memoryLimitMb: 1);

        Assert.IsFalse(advice.RamLimited);
        Assert.AreEqual(Math.Clamp(Environment.ProcessorCount, 4, 8), advice.Workers);
    }

    [TestMethod]
    public void Load_EnablesAutoThresholdByDefault()
    {
        using var root = new TestDirectory();

        var config = new ConfigManager(root.Path).Load();

        Assert.IsTrue(config.Php.MemoryRestartAuto);
    }

    // Turning Auto off has to leave a usable number behind, not 0 — which would silently disable
    // the watchdog rather than hand back manual control.
    [TestMethod]
    public void Load_KeepsAUsableThresholdWhenAutoIsOnAndTheStoredValueIsZero()
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "hamster-server.json"),
            """{"php":{"memoryRestartAuto":true,"memoryRestartThreshold":0}}""");

        var config = new ConfigManager(root.Path).Load();

        Assert.AreEqual(1024, config.Php.MemoryRestartThreshold);
    }

    [TestMethod]
    public void Load_KeepsZeroAsDisabledWhenAutoIsOff()
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "hamster-server.json"),
            """{"php":{"memoryRestartAuto":false,"memoryRestartThreshold":0}}""");

        var config = new ConfigManager(root.Path).Load();

        Assert.AreEqual(0, config.Php.MemoryRestartThreshold);
    }

    [TestMethod]
    public void Load_NormalizesPhpIniSettings()
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "hamster-server.json"),
            """{"php":{"uploadMaxFilesize":0,"postMaxSize":999999,"errorReporting":42}}""");

        var config = new ConfigManager(root.Path).Load();

        Assert.AreEqual(128, config.Php.UploadMaxFilesize, "A non-positive size falls back to the old template value.");
        Assert.AreEqual(4096, config.Php.PostMaxSize);
        Assert.AreEqual(PhpErrorReporting.Auto, config.Php.ErrorReporting);
    }

    [TestMethod]
    public void Load_ConfigurationWithoutPhpIniSettingsKeepsPreviousTemplateValues()
    {
        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "hamster-server.json"), """{"php":{}}""");

        var config = new ConfigManager(root.Path).Load();

        // Exactly what the php.ini template hard-coded before these became settings, so an
        // upgrade regenerates an identical file.
        Assert.AreEqual(128, config.Php.UploadMaxFilesize);
        Assert.AreEqual(128, config.Php.PostMaxSize);
        Assert.IsFalse(config.Php.ShortOpenTag);
        Assert.AreEqual(PhpErrorReporting.Auto, config.Php.ErrorReporting);
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
