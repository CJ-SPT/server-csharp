using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace SPTarkov.Server.Core.Models.Eft.Dialog;

public record PinDialogRequestData : IRequestData
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("dialogId")]
    public string? DialogId { get; set; }
}
