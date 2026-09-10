using HamsterServer.Core;

namespace HamsterServer.Tests;

[TestClass]
public sealed class LogWriterTests
{
    [TestMethod]
    public void CompleteAndFlush_PersistsBurstInOrder()
    {
        using var root = new TestDirectory();
        using var writer = new LogWriter(root.Path);

        const int messageCount = 6000;
        for (int index = 0; index < messageCount; index++)
            Assert.IsTrue(writer.Enqueue($"message-{index:D4}"));

        Assert.IsTrue(writer.CompleteAndFlush(TimeSpan.FromSeconds(5)));

        var path = Path.Combine(
            root.Path, "config", "logs", "server", $"hamster-server-{DateTime.Today:yyyy-MM-dd}.log");
        var lines = File.ReadAllLines(path);
        Assert.HasCount(messageCount, lines);
        Assert.AreEqual("message-0000", lines[0]);
        Assert.AreEqual("message-5999", lines[^1]);
    }
}
