namespace HamsterServer.Tests;

internal sealed class TestDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), $"hamster-server-tests-{Guid.NewGuid():N}");

    public TestDirectory() => Directory.CreateDirectory(Path);

    public void Dispose()
    {
        if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
    }
}
