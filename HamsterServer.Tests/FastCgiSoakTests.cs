using System.Diagnostics;
using System.Net;
using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Tests;

/// <summary>
/// Drives real load through Apache and php-cgi to prove the connection-reuse policy.
/// <para>
/// Marked <c>Soak</c> and skipped unless <c>HAMSTER_SOAK=1</c>: each case starts the real server
/// stack, binds a TCP port and takes tens of seconds. Run explicitly with
/// <c>dotnet test HamsterServer.slnx --filter "TestCategory=Soak" -e HAMSTER_SOAK=1</c>.
/// </para>
/// <para>
/// A <c>.runsettings</c> <c>TestCaseFilter</c> cannot do this job: VSTest ANDs it with the command
/// line <c>--filter</c>, so <c>TestCategory!=Soak</c> there would make <c>--filter TestCategory=Soak</c>
/// match nothing. The opt-in has to live in the tests themselves.
/// </para>
/// <para>
/// WARNING: <see cref="ServerManager.Dispose"/> kills every httpd/php-cgi whose image path belongs
/// to this installation, so close the app before running these.
/// </para>
/// </summary>
[TestClass]
public sealed class FastCgiSoakTests
{
    private const int Requests = 12_000;
    private const int Concurrency = 32;

    public TestContext TestContext { get; set; } = null!;

    internal const string OptInVariable = "HAMSTER_SOAK";

    // A bare `dotnet test` used to run these too, and their cleanup kills every httpd/php-cgi of the
    // Debug build — including an instance of the app the developer had open. Skipping is reported
    // as such (Inconclusive → Skipped), so nothing passes silently.
    [TestInitialize]
    public void RequireExplicitOptIn()
    {
        if (Environment.GetEnvironmentVariable(OptInVariable) != "1")
        {
            Assert.Inconclusive(
                "Soak test skipped: it starts the real stack on ports 18080-18083 and kills every " +
                "httpd/php-cgi of this build, including a running app. Opt in with: " +
                $"dotnet test HamsterServer.slnx --filter \"TestCategory=Soak\" -e {OptInVariable}=1");
        }
    }

    // The whole point of the regression: a worker that exits mid-pool used to hand Apache a socket
    // it was still reusing, which surfaced as rare 503s. Pooling is on here (MaxRequests = 0).
    [TestMethod]
    [TestCategory("Soak")]
    public async Task PooledFastCgi_ServesSustainedLoadWithoutServerErrors()
    {
        var result = await RunSoakAsync(maxRequests: 0, httpPort: 18080, fcgiPort: 39100);
        TestContext.WriteLine(result.ToString());

        Assert.AreEqual(0, result.ServerErrors,
            $"Pooled FastCGI returned {result.ServerErrors} 5xx responses. {result}");
        Assert.AreEqual(0, result.TransportErrors,
            $"Pooled FastCGI dropped {result.TransportErrors} connections. {result}");
        Assert.AreEqual(Requests, result.Successful, result.ToString());
    }

    // The opt-in path: recycling on, so the generator must have dropped pooling. Forcing many
    // recycles (MaxRequests is deliberately tiny) must still not produce a single 5xx.
    [TestMethod]
    [TestCategory("Soak")]
    public async Task RecyclingWorkers_SurviveManyRecycleCyclesWithoutServerErrors()
    {
        var result = await RunSoakAsync(maxRequests: 200, httpPort: 18081, fcgiPort: 39101);
        TestContext.WriteLine(result.ToString());

        Assert.AreEqual(0, result.ServerErrors,
            $"Recycling workers returned {result.ServerErrors} 5xx responses. {result}");
        Assert.AreEqual(0, result.TransportErrors,
            $"Recycling workers dropped {result.TransportErrors} connections. {result}");
        Assert.AreEqual(Requests, result.Successful, result.ToString());
    }

    /// <summary>
    /// The memory watchdog rests on two readings: how much the pool uses, and whether it is idle.
    /// Both come from the OS rather than from anything the tests can fake, so this checks them
    /// against a real running pool — including that a live request actually reads as busy.
    /// </summary>
    [TestMethod]
    [TestCategory("Soak")]
    public async Task MemoryWatchdogReadings_ReflectTheRunningPool()
    {
        var rootDir = FindApplicationOutputDirectory();
        var probe = Path.Combine(rootDir, "www", "hamster-idle-probe.php");
        // Sleeps long enough that the connection is unambiguously open while we sample.
        File.WriteAllText(probe, "<?php usleep(1500000); echo 'ok';");

        var phpManager = new PhpVersionManager(rootDir);
        phpManager.Scan();
        var php = phpManager.GetLatest();
        Assert.IsNotNull(php, $"No PHP runtime found under {Path.Combine(rootDir, "php")}.");

        var config = new AppConfig();
        config.Apache.Port = 18082;
        config.Apache.EnableSsl = false;
        config.Php.CurrentVersion = php.Version;
        config.Php.FcgiPort = 39102;
        config.Php.Workers = 4;
        config.Php.MaxRequests = 0;

        var server = new ServerManager(rootDir, config);
        var log = new List<string>();
        server.LogReceived += (_, line) => { lock (log) log.Add(line); };

        try
        {
            await server.StartAllAsync();
            var url = $"http://127.0.0.1:18082/hamster-idle-probe.php";
            await WaitUntilServingAsync(url, log);

            long bytes = server.GetPhpPoolWorkingSetBytes();
            long mb = bytes / 1024 / 1024;
            TestContext.WriteLine($"Pool working set: {mb} MB across {config.Php.Workers} workers");
            Assert.IsTrue(bytes > 0, "A running pool must report a non-zero working set.");
            Assert.IsTrue(mb < 4096, $"Implausible reading of {mb} MB — the path match is probably wrong.");

            // Busy while a request is in flight…
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            var inFlight = client.GetAsync(url);
            await Task.Delay(500);
            Assert.IsFalse(await server.IsPhpPoolIdleAsync(),
                "The pool must not read as idle while a request is being served.");
            (await inFlight).Dispose();

            // …and idle again once traffic stops and the pooled connections age out (ttl=15).
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < deadline && !await server.IsPhpPoolIdleAsync())
                await Task.Delay(1000);

            Assert.IsTrue(await server.IsPhpPoolIdleAsync(),
                "The pool never went idle after traffic stopped, so a memory restart would never fire.");
        }
        finally
        {
            try { await server.StopAllAsync(); } catch { /* the sweep below is the real cleanup */ }
            server.Dispose();
            try { File.Delete(probe); } catch { /* best effort */ }
        }
    }

    /// <summary>
    /// End-to-end proof that the memory watchdog fires: start a pool, set the threshold below what
    /// it already uses, and wait for the sweep to recycle it. A changed PID is the only evidence
    /// that cannot be faked by log text.
    /// </summary>
    [TestMethod]
    [TestCategory("Soak")]
    [Timeout(180_000)]
    public async Task MemoryWatchdog_RecyclesThePoolOnceItPassesTheThreshold()
    {
        var rootDir = FindApplicationOutputDirectory();
        var phpManager = new PhpVersionManager(rootDir);
        phpManager.Scan();
        var php = phpManager.GetLatest();
        Assert.IsNotNull(php, $"No PHP runtime found under {Path.Combine(rootDir, "php")}.");

        var config = new AppConfig();
        config.Apache.Port = 18083;
        config.Apache.EnableSsl = false;
        config.Php.CurrentVersion = php.Version;
        config.Php.FcgiPort = 39103;
        config.Php.Workers = 4;
        config.Php.MaxRequests = 0;
        // Below what four workers use at rest, so the very first sweep must trip.
        // Auto is on by default and would derive 512 MB for four workers, well above what
        // the pool weighs — pin it manually so the sweep actually has something to cross.
        config.Php.MemoryRestartAuto = false;
        config.Php.MemoryRestartThreshold = 128;

        var server = new ServerManager(rootDir, config);
        var log = new List<string>();
        server.LogReceived += (_, line) => { lock (log) log.Add(line); };

        try
        {
            await server.StartAllAsync();
            Assert.IsTrue(server.IsPhpRunning, "PHP did not start.");

            int? pidBefore = server.PhpPid;
            long mbBefore = server.GetPhpPoolWorkingSetBytes() / 1024 / 1024;
            TestContext.WriteLine($"Before: PID {pidBefore}, {mbBefore} MB (threshold 128 MB)");
            Assert.IsTrue(mbBefore > 128,
                $"Pool only uses {mbBefore} MB, so the 128 MB threshold would never trip.");

            // The sweep runs on a 60 s timer, then waits for idle (immediate here — no traffic).
            // Wait for a NEW pid, not merely a different one: sampling between StopPhpCoreAsync and
            // StartPhpCoreAsync sees a null pid, which is the recycle in flight rather than its end.
            var deadline = DateTime.UtcNow.AddSeconds(150);
            while (DateTime.UtcNow < deadline &&
                   (server.PhpPid is null || server.PhpPid == pidBefore))
                await Task.Delay(1000);

            int? pidAfter = server.PhpPid;
            string transcript;
            lock (log) transcript = string.Join('\n', log);
            TestContext.WriteLine($"After: PID {pidAfter}");
            TestContext.WriteLine(transcript);

            Assert.AreNotEqual(pidBefore, pidAfter,
                $"The pool was never recycled within 150 s.\n{transcript}");
            Assert.IsTrue(server.IsPhpRunning, $"PHP is not running after the recycle.\n{transcript}");
            StringAssert.Contains(transcript, "Recycling the PHP pool");
        }
        finally
        {
            try { await server.StopAllAsync(); } catch { /* the sweep below is the real cleanup */ }
            server.Dispose();
        }
    }

    /// <summary>
    /// The interesting case for the idle policy: traffic never stops, it only has gaps. Tight
    /// polling is supposed to find one of those gaps, recycle there, and cost no failed requests.
    /// This measures both halves — that the recycle happens at all, and what it costs.
    /// </summary>
    [TestMethod]
    [TestCategory("Soak")]
    [Timeout(240_000)]
    public async Task MemoryWatchdog_RecyclesUnderContinuousTrafficWithoutFailingRequests()
    {
        var rootDir = FindApplicationOutputDirectory();
        var probe = Path.Combine(rootDir, "www", "hamster-traffic-probe.php");
        File.WriteAllText(probe, "<?php echo 'ok';");

        var phpManager = new PhpVersionManager(rootDir);
        phpManager.Scan();
        var php = phpManager.GetLatest();
        Assert.IsNotNull(php, $"No PHP runtime found under {Path.Combine(rootDir, "php")}.");

        var config = new AppConfig();
        config.Apache.Port = 18084;
        config.Apache.EnableSsl = false;
        config.Php.CurrentVersion = php.Version;
        config.Php.FcgiPort = 39104;
        config.Php.Workers = 4;
        config.Php.MaxRequests = 0;
        // Auto is on by default and would derive 512 MB for four workers, well above what
        // the pool weighs — pin it manually so the sweep actually has something to cross.
        config.Php.MemoryRestartAuto = false;
        config.Php.MemoryRestartThreshold = 128;

        var server = new ServerManager(rootDir, config);
        var log = new List<string>();
        server.LogReceived += (_, line) => { lock (log) log.Add(line); };

        try
        {
            await server.StartAllAsync();
            var url = "http://127.0.0.1:18084/hamster-traffic-probe.php";
            await WaitUntilServingAsync(url, log);

            int? pidBefore = server.PhpPid;
            TestContext.WriteLine($"Before: PID {pidBefore}, {server.GetPhpPoolWorkingSetBytes() / 1024 / 1024} MB");

            // Steady, unsaturated traffic: one request every 200 ms leaves real gaps between
            // requests, which is what a developer's machine actually looks like.
            using var cts = new CancellationTokenSource();
            int ok = 0, serverErrors = 0, transportErrors = 0;
            var traffic = Task.Run(async () =>
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        using var response = await client.GetAsync(url, cts.Token);
                        if (response.IsSuccessStatusCode) Interlocked.Increment(ref ok);
                        else if ((int)response.StatusCode >= 500) Interlocked.Increment(ref serverErrors);
                    }
                    catch (OperationCanceledException) { break; }
                    catch { Interlocked.Increment(ref transportErrors); }

                    try { await Task.Delay(200, cts.Token); } catch (OperationCanceledException) { break; }
                }
            });

            // As above: a null pid means the recycle is mid-flight, so keep waiting for the new one.
            var deadline = DateTime.UtcNow.AddSeconds(180);
            while (DateTime.UtcNow < deadline &&
                   (server.PhpPid is null || server.PhpPid == pidBefore))
                await Task.Delay(1000);

            int? pidAfter = server.PhpPid;
            await cts.CancelAsync();
            await traffic;

            string transcript;
            lock (log) transcript = string.Join('\n', log);
            TestContext.WriteLine(
                $"After: PID {pidAfter} — {ok} OK, {serverErrors} 5xx, {transportErrors} transport errors " +
                "while traffic ran continuously");
            TestContext.WriteLine(transcript);

            Assert.AreNotEqual(pidBefore, pidAfter,
                $"Tight polling never found an idle gap under continuous traffic.\n{transcript}");
            Assert.AreEqual(0, serverErrors,
                $"Recycling under traffic produced {serverErrors} 5xx responses.\n{transcript}");
            Assert.AreEqual(0, transportErrors,
                $"Recycling under traffic dropped {transportErrors} connections.\n{transcript}");
        }
        finally
        {
            try { await server.StopAllAsync(); } catch { /* the sweep below is the real cleanup */ }
            server.Dispose();
            try { File.Delete(probe); } catch { /* best effort */ }
        }
    }

    private static async Task<SoakResult> RunSoakAsync(int maxRequests, int httpPort, int fcgiPort)
    {
        var rootDir = FindApplicationOutputDirectory();
        var probe = Path.Combine(rootDir, "www", "hamster-soak-probe.php");
        File.WriteAllText(probe, "<?php echo 'ok';");

        // A fresh AppConfig has no CurrentVersion, and ServerManager refuses to start without one.
        var phpManager = new PhpVersionManager(rootDir);
        phpManager.Scan();
        var php = phpManager.GetLatest();
        Assert.IsNotNull(php, $"No PHP runtime found under {Path.Combine(rootDir, "php")}.");

        var config = new AppConfig();
        config.Apache.Port = httpPort;
        config.Apache.EnableSsl = false;
        config.Php.CurrentVersion = php.Version;
        config.Php.FcgiPort = fcgiPort;
        config.Php.Workers = 4;
        config.Php.MaxRequests = maxRequests;

        var server = new ServerManager(rootDir, config);
        var log = new List<string>();
        server.LogReceived += (_, line) => { lock (log) log.Add(line); };

        try
        {
            await server.StartAllAsync();
            var url = $"http://127.0.0.1:{httpPort}/hamster-soak-probe.php";
            await WaitUntilServingAsync(url, log);
            return await DriveLoadAsync(url, maxRequests);
        }
        finally
        {
            try { await server.StopAllAsync(); } catch { /* the sweep below is the real cleanup */ }
            server.Dispose();
            try { File.Delete(probe); } catch { /* best effort */ }
        }
    }

    private static async Task WaitUntilServingAsync(string url, List<string> log)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { /* not listening yet */ }
            catch (TaskCanceledException) { /* still warming up */ }

            await Task.Delay(250);
        }

        string tail;
        lock (log) tail = string.Join('\n', log.TakeLast(20));
        Assert.Fail($"Server never served {url} within 60 s.\n{tail}");
        throw new UnreachableException();
    }

    private static async Task<SoakResult> DriveLoadAsync(string url, int maxRequests)
    {
        // One handler shared by every worker, capped at the concurrency level, so the client side
        // mirrors a browser pool rather than opening a socket per request.
        using var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = Concurrency,
            PooledConnectionLifetime = Timeout.InfiniteTimeSpan
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        int successful = 0, serverErrors = 0, otherStatus = 0, transportErrors = 0;
        int issued = -1;
        var stopwatch = Stopwatch.StartNew();

        var workers = Enumerable.Range(0, Concurrency).Select(async _ =>
        {
            while (Interlocked.Increment(ref issued) < Requests)
            {
                try
                {
                    using var response = await client.GetAsync(url);
                    if (response.StatusCode == HttpStatusCode.OK)
                        Interlocked.Increment(ref successful);
                    else if ((int)response.StatusCode >= 500)
                        Interlocked.Increment(ref serverErrors);
                    else
                        Interlocked.Increment(ref otherStatus);
                }
                catch (Exception)
                {
                    // A reset or timed-out connection is exactly the failure mode under test.
                    Interlocked.Increment(ref transportErrors);
                }
            }
        });

        await Task.WhenAll(workers);
        stopwatch.Stop();

        return new SoakResult(maxRequests, successful, serverErrors, otherStatus,
                              transportErrors, stopwatch.Elapsed);
    }

    /// <summary>
    /// The soak needs a real installation layout (apache/, php/, www/), which only the app's own
    /// build output has — the test project deliberately does not copy the runtimes.
    /// </summary>
    private static string FindApplicationOutputDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HamsterServer.slnx")))
                break;
            directory = directory.Parent;
        }
        Assert.IsNotNull(directory, "Could not locate HamsterServer.slnx.");

        var configuration = Path.GetFileName(Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd(
            Path.DirectorySeparatorChar))) ?? "Debug";
        var output = Path.Combine(directory.FullName, "HamsterServer", "bin", configuration,
                                  "net10.0-windows");

        Assert.IsTrue(Directory.Exists(Path.Combine(output, "php")),
            $"Build the app first — no php/ under {output}");
        Assert.IsTrue(Directory.Exists(Path.Combine(output, "www")),
            $"Build the app first — no www/ under {output}");
        return output;
    }

    private readonly record struct SoakResult(
        int MaxRequests,
        int Successful,
        int ServerErrors,
        int OtherStatus,
        int TransportErrors,
        TimeSpan Elapsed)
    {
        public override string ToString() =>
            $"MaxRequests={MaxRequests}: {Successful} OK, {ServerErrors} 5xx, " +
            $"{OtherStatus} other, {TransportErrors} transport errors in " +
            $"{Elapsed.TotalSeconds:F1}s ({Successful / Math.Max(Elapsed.TotalSeconds, 0.001):F0} req/s)";
    }
}
