namespace HamsterServer.Models;

public sealed class PhpReleaseInfo
{
    public string Branch { get; init; } = "";
    public string Version { get; init; } = "";
    public string PackageFileName { get; init; } = "";
    public Uri DownloadUri { get; init; } = null!;
    public string Sha256 { get; init; } = "";
    public string Size { get; init; } = "";
    public DateTimeOffset? PublishedAt { get; init; }

    public override string ToString() => Version;
}

public sealed class PhpDownloadProgress
{
    public string Stage { get; init; } = "";
    public long BytesReceived { get; init; }
    public long? TotalBytes { get; init; }

    public int? Percentage => TotalBytes is > 0
        ? (int)Math.Clamp(BytesReceived * 100L / TotalBytes.Value, 0, 100)
        : null;
}
