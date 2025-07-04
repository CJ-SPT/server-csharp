using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Enums;

namespace SPTarkov.Server.Core.Models.Spt.Weather;

public record GetLocalWeatherResponseData
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("season")]
    public Season? Season { get; set; }

    [JsonPropertyName("weather")]
    public List<Eft.Weather.Weather>? Weather { get; set; }
}
