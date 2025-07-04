using System.Text.Json.Serialization;

namespace SPTarkov.Server.Core.Models.Eft.HttpResponse;

public record NullResponseData
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("err")]
    public int? Err { get; set; }

    [JsonPropertyName("errmsg")]
    public object? ErrMsg { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
