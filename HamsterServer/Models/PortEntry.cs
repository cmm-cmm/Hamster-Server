using System.Text.Json.Serialization;

namespace HamsterServer.Models;

public class PortEntry
{
    [JsonPropertyName("port")]
    public int Port { get; set; } = 8080;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("isSsl")]
    public bool IsSsl { get; set; } = false;

    public override string ToString() => Port.ToString();
}
