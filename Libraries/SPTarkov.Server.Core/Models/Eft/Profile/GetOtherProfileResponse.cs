using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace SPTarkov.Server.Core.Models.Eft.Profile;

public record GetOtherProfileResponse
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("id")]
    public MongoId? Id { get; set; }

    [JsonPropertyName("aid")]
    public int? Aid { get; set; }

    [JsonPropertyName("info")]
    public OtherProfileInfo? Info { get; set; }

    [JsonPropertyName("customization")]
    public OtherProfileCustomization? Customization { get; set; }

    [JsonPropertyName("skills")]
    public Skills? Skills { get; set; }

    [JsonPropertyName("equipment")]
    public OtherProfileEquipment? Equipment { get; set; }

    [JsonPropertyName("achievements")]
    public Dictionary<MongoId, long>? Achievements { get; set; }

    [JsonPropertyName("favoriteItems")]
    public List<Item>? FavoriteItems { get; set; }

    [JsonPropertyName("pmcStats")]
    public OtherProfileStats? PmcStats { get; set; }

    [JsonPropertyName("scavStats")]
    public OtherProfileStats? ScavStats { get; set; }

    [JsonPropertyName("hideout")]
    public Common.Tables.Hideout Hideout { get; set; }

    [JsonPropertyName("customizationStash")]
    public string CustomizationStash { get; set; }

    [JsonPropertyName("hideoutAreaStashes")]
    public Dictionary<string, MongoId> HideoutAreaStashes { get; set; }

    [JsonPropertyName("items")]
    public List<Item> Items { get; set; }
}

public record OtherProfileInfo
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    [JsonPropertyName("side")]
    public string? Side { get; set; }

    [JsonPropertyName("experience")]
    public int? Experience { get; set; }

    [JsonPropertyName("memberCategory")]
    public int? MemberCategory { get; set; }

    [JsonPropertyName("bannedState")]
    public bool? BannedState { get; set; }

    [JsonPropertyName("bannedUntil")]
    public long? BannedUntil { get; set; }

    [JsonPropertyName("registrationDate")]
    public long? RegistrationDate { get; set; }
}

public record OtherProfileCustomization
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("head")]
    public string? Head { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("feet")]
    public string? Feet { get; set; }

    [JsonPropertyName("hands")]
    public string? Hands { get; set; }

    [JsonPropertyName("dogtag")]
    public string? Dogtag { get; set; }
}

public record OtherProfileEquipment
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("Id")]
    public string? Id { get; set; }

    [JsonPropertyName("Items")]
    public List<Item>? Items { get; set; }
}

public record OtherProfileStats
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("eft")]
    public OtherProfileSubStats? Eft { get; set; }
}

public record OtherProfileSubStats
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("totalInGameTime")]
    public long? TotalInGameTime { get; set; }

    [JsonPropertyName("overAllCounters")]
    public OverallCounters? OverAllCounters { get; set; }
}
