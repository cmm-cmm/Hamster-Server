using System.Text.Json.Serialization;

namespace HamsterServer.Models;

public class VHostEntry
{
    [JsonPropertyName("serverName")]
    public string ServerName { get; set; } = "";

    [JsonPropertyName("serverAlias")]
    public string ServerAlias { get; set; } = "";

    [JsonPropertyName("documentRoot")]
    public string DocumentRoot { get; set; } = "";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 0;

    [JsonPropertyName("framework")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FrameworkPreset Framework { get; set; } = FrameworkPreset.None;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    public override string ToString() => ServerName;
}
