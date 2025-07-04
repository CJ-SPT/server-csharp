using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace SPTarkov.Server.Core.Models.Spt.Quests;

public record GetRepeatableByIdResult
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("quest")]
    public RepeatableQuest? Quest { get; set; }

    [JsonPropertyName("repeatableType")]
    public PmcDataRepeatableQuest? RepeatableType { get; set; }
}
