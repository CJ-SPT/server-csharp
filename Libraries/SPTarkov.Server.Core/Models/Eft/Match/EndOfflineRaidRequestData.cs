using System.Text.Json.Serialization;

namespace SPTarkov.Server.Core.Models.Eft.Match;

public record EndOfflineRaidRequestData
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("crc")]
    public int? Crc { get; set; }

    [JsonPropertyName("exitStatus")]
    public string? ExitStatus { get; set; }

    [JsonPropertyName("exitName")]
    public string? ExitName { get; set; }

    [JsonPropertyName("raidSeconds")]
    public int? RaidSeconds { get; set; }
}
