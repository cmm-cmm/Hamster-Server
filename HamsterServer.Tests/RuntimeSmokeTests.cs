using System.Diagnostics;
using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Tests;

[TestClass]
public sealed class RuntimeSmokeTests
{
    [TestMethod]
    [TestCategory("Integration")]
    public async Task BundledApache_AcceptsGeneratedConfiguration()
    {
        var repositoryRoot = FindRepositoryRoot();
        var apacheDir = Path.Combine(repositoryRoot, "HamsterServer", "apache", "Apache24");
        var httpd = Path.Combine(apacheDir, "bin", "httpd.exe");
        Assert.IsTrue(File.Exists(httpd), $"Bundled Apache not found: {httpd}");

        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        var configPath = Path.Combine(configDir, "httpd.conf");
        var wwwDir = Path.Combine(root.Path, "www");
        Directory.CreateDirectory(wwwDir);
        var config = new AppConfig();
        config.Php.FcgiPort = 39000;
        var writer = new ApacheConfigWriter(root.Path, config);
        writer.WriteHttpdConf(configPath, apacheDir, wwwDir, configDir);
        writer.WriteAllVHosts(Path.Combine(configDir, "vhosts"));

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = httpd,
            ArgumentList = { "-t", "-f", configPath },
            WorkingDirectory = apacheDir,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        });
        Assert.IsNotNull(process);
        var stderr = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));

        Assert.AreEqual(0, process.ExitCode, $"{await stderr}\n{await stdout}");
    }

    // Turning recycling back on drops the reuse parameters from every ProxyPassMatch, which is a
    // different directive shape than the default config exercises above.
    [TestMethod]
    [TestCategory("Integration")]
    public async Task BundledApache_AcceptsConfigurationWithWorkerRecycling()
    {
        var repositoryRoot = FindRepositoryRoot();
        var apacheDir = Path.Combine(repositoryRoot, "HamsterServer", "apache", "Apache24");
        var httpd = Path.Combine(apacheDir, "bin", "httpd.exe");
        Assert.IsTrue(File.Exists(httpd), $"Bundled Apache not found: {httpd}");

        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        var configPath = Path.Combine(configDir, "httpd.conf");
        var wwwDir = Path.Combine(root.Path, "www");
        Directory.CreateDirectory(wwwDir);

        var config = new AppConfig
        {
            VHosts = [new VHostEntry { ServerName = "recycle.test", DocumentRoot = wwwDir }]
        };
        config.Php.FcgiPort = 39002;
        config.Php.MaxRequests = 500;

        var writer = new ApacheConfigWriter(root.Path, config);
        writer.WriteHttpdConf(configPath, apacheDir, wwwDir, configDir);
        var vhostDir = Path.Combine(configDir, "vhosts");
        writer.WriteAllVHosts(vhostDir);

        foreach (var file in Directory.GetFiles(vhostDir, "*.conf"))
            Assert.IsFalse(File.ReadAllText(file).Contains("enablereuse", StringComparison.Ordinal),
                $"{Path.GetFileName(file)} pooled connections while workers recycle.");

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = httpd,
            ArgumentList = { "-t", "-f", configPath },
            WorkingDirectory = apacheDir,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        });
        Assert.IsNotNull(process);
        var stderr = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));

        Assert.AreEqual(0, process.ExitCode, $"{await stderr}\n{await stdout}");
    }

    // Folder names of the bundled runtimes, verbatim from the windows.php.net catalog.
    private static readonly string[] BundledPhpVersions = ["8.2.35-1", "8.3.36-1", "8.4.27-1", "8.5.12-1"];

    [TestMethod]
    [TestCategory("Integration")]
    public async Task BundledPhpExecutables_ReportExpectedVersions()
    {
        var repositoryRoot = FindRepositoryRoot();
        foreach (var version in BundledPhpVersions)
        {
            var php = Path.Combine(repositoryRoot, "HamsterServer", "php", version, "php.exe");
            Assert.IsTrue(File.Exists(php), $"Bundled PHP not found: {php}");
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = php,
                ArgumentList = { "-n", "-v" },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            Assert.IsNotNull(process);
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
            Assert.AreEqual(0, process.ExitCode);
            // A Windows rebuild ("8.5.12-1") still reports the plain PHP version.
            StringAssert.StartsWith(output, $"PHP {version.Split('-')[0]} ");
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task BundledPhpRecommendedExtensions_LoadWithoutWarnings()
    {
        var repositoryRoot = FindRepositoryRoot();
        foreach (var version in BundledPhpVersions)
        {
            var phpDir = Path.Combine(repositoryRoot, "HamsterServer", "php", version);
            var php = Path.Combine(phpDir, "php.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = php,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("-n");
            startInfo.ArgumentList.Add("-d");
            startInfo.ArgumentList.Add($"extension_dir={Path.Combine(phpDir, "ext")}");
            foreach (var extension in PhpExtensionManager.RecommendedExtensions)
            {
                startInfo.ArgumentList.Add("-d");
                startInfo.ArgumentList.Add($"extension={extension}");
            }
            if (PhpExtensionManager.HasOpcacheDll(phpDir))
            {
                startInfo.ArgumentList.Add("-d");
                startInfo.ArgumentList.Add("zend_extension=opcache");
            }
            startInfo.ArgumentList.Add("-m");

            using var process = Process.Start(startInfo);
            Assert.IsNotNull(process);
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
            var modules = (await stdout).ReplaceLineEndings("\n").Split('\n')
                .Select(module => module.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var errors = await stderr;

            Assert.AreEqual(0, process.ExitCode, $"PHP {version}: {errors}");
            Assert.IsTrue(string.IsNullOrWhiteSpace(errors), $"PHP {version}: {errors}");
            foreach (var extension in PhpExtensionManager.RecommendedExtensions)
                Assert.IsTrue(modules.Contains(extension), $"PHP {version} did not load {extension}.");
            Assert.IsTrue(modules.Contains("Zend OPcache"), $"PHP {version} did not load OPcache.");
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task BundledApache_AcceptsEveryFrameworkProfile()
    {
        var repositoryRoot = FindRepositoryRoot();
        var apacheDir = Path.Combine(repositoryRoot, "HamsterServer", "apache", "Apache24");
        var httpd = Path.Combine(apacheDir, "bin", "httpd.exe");
        Assert.IsTrue(File.Exists(httpd), $"Bundled Apache not found: {httpd}");

        using var root = new TestDirectory();
        var configDir = Path.Combine(root.Path, "config");
        var configPath = Path.Combine(configDir, "httpd.conf");
        var wwwDir = Path.Combine(root.Path, "www");
        Directory.CreateDirectory(wwwDir);

        var config = new AppConfig();
        config.Php.FcgiPort = 39001;
        foreach (var profile in FrameworkProfiles.All.Where(profile => profile.Preset != FrameworkPreset.None))
        {
            var projectRoot = Path.Combine(root.Path, "projects", profile.Preset.ToString());
            string target;
            if (profile.IsReverseProxy)
            {
                target = profile.ProxyDefault;
            }
            else
            {
                target = projectRoot;
                var webRoot = string.IsNullOrEmpty(profile.PreferredWebRoot)
                    ? projectRoot
                    : Path.Combine(projectRoot, profile.PreferredWebRoot);
                Directory.CreateDirectory(webRoot);
                File.WriteAllText(Path.Combine(webRoot, "index.php"), "<?php");
            }

            config.VHosts.Add(new VHostEntry
            {
                ServerName = $"{profile.Preset.ToString().ToLowerInvariant()}.test",
                DocumentRoot = target,
                Framework = profile.Preset
            });
        }

        var writer = new ApacheConfigWriter(root.Path, config);
        writer.WriteHttpdConf(configPath, apacheDir, wwwDir, configDir);
        writer.WriteAllVHosts(Path.Combine(configDir, "vhosts"));

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = httpd,
            ArgumentList = { "-t", "-f", configPath },
            WorkingDirectory = apacheDir,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        });
        Assert.IsNotNull(process);
        var stderr = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));

        Assert.AreEqual(0, process.ExitCode, $"{await stderr}\n{await stdout}");
    }

    // The Apache config dialog validates custom directives with this before saving. Saving a broken
    // one while Apache ran used to stop Apache and then fail the pre-start test, leaving it down.
    [TestMethod]
    [TestCategory("Integration")]
    public async Task TestApacheConfig_AcceptsValidAndRejectsInvalidCustomDirectives()
    {
        // The source tree has the same shape as the app output (php/ + apache/), and nothing here
        // starts a process that binds a port — httpd only parses the file.
        var appDir = Path.Combine(FindRepositoryRoot(), "HamsterServer");
        var server = new ServerManager(appDir, new AppConfig());
        var configDir = Path.Combine(appDir, "config");
        string[] TestFiles() => Directory.Exists(configDir)
            ? Directory.GetFiles(configDir, ".httpd-test-*.conf")
            : [];
        var before = TestFiles();

        var valid = await server.TestApacheConfigAsync("ServerSignature Off");
        var invalid = await server.TestApacheConfigAsync("ThisIsNotAnApacheDirective on");

        Assert.IsTrue(valid.Ok, valid.Output);
        Assert.IsFalse(invalid.Ok, "httpd -t accepted a directive that does not exist.");
        StringAssert.Contains(invalid.Output, "ThisIsNotAnApacheDirective");
        CollectionAssert.AreEquivalent(before, TestFiles(), "The throw-away httpd.conf must be deleted.");
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
