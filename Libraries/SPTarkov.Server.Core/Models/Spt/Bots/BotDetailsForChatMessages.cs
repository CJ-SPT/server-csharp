using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Enums;

namespace SPTarkov.Server.Core.Models.Spt.Bots;

public record BotDetailsForChatMessages
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    public string Nickname { get; set; } = string.Empty;

    public DogtagSide Side { get; set; }

    public int? Aid { get; set; }

    public int? Level { get; set; }

    public MemberCategory? Type { get; set; }
}
