using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace SPTarkov.Server.Core.Models.Spt.Config;

public record ItemConfig : BaseConfig
{
    [JsonPropertyName("kind")]
    public override string Kind { get; set; } = "spt-item";

    /// <summary>
    ///     Items that should be globally blacklisted
    /// </summary>
    [JsonPropertyName("blacklist")]
    public required HashSet<MongoId> Blacklist { get; set; }

    /// <summary>
    ///     Items that should not be lootable from any location
    /// </summary>
    [JsonPropertyName("lootableItemBlacklist")]
    public required HashSet<MongoId> LootableItemBlacklist { get; set; }

    /// <summary>
    ///     items that should not be given as rewards
    /// </summary>
    [JsonPropertyName("rewardItemBlacklist")]
    public required HashSet<MongoId> RewardItemBlacklist { get; set; }

    /// <summary>
    ///     Item base types that should not be given as rewards
    /// </summary>
    [JsonPropertyName("rewardItemTypeBlacklist")]
    public required HashSet<MongoId> RewardItemTypeBlacklist { get; set; }

    /// <summary>
    ///     Items that can only be found on bosses
    /// </summary>
    [JsonPropertyName("bossItems")]
    public required HashSet<MongoId> BossItems { get; set; }

    [JsonPropertyName("handbookPriceOverride")]
    public required Dictionary<MongoId, HandbookPriceOverride> HandbookPriceOverride { get; set; }

    /// <summary>
    ///     Presets to add to the globals.json `ItemPresets` dictionary on server start
    /// </summary>
    [JsonPropertyName("customItemGlobalPresets")]
    public required List<Preset> CustomItemGlobalPresets { get; set; }
}

public record HandbookPriceOverride
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    /// <summary>
    ///     Price in roubles
    /// </summary>
    [JsonPropertyName("price")]
    public double Price { get; set; }

    /// <summary>
    ///     NOT parentId from items.json, but handbook.json
    /// </summary>
    [JsonPropertyName("parentId")]
    public MongoId ParentId { get; set; } = MongoId.Empty();
}
