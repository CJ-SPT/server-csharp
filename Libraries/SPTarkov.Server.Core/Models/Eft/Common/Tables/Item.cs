using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils.Json.Converters;

namespace SPTarkov.Server.Core.Models.Eft.Common.Tables;

public record Item
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    private string? _parentId;

    private string? _slotId;

    // MongoId
    [JsonPropertyName("_id")]
    public virtual required MongoId Id { get; set; }

    [JsonPropertyName("_tpl")]
    // MongoId
    public MongoId Template { get; set; }

    [JsonPropertyName("parentId")]
    public string? ParentId
    {
        get { return _parentId; }
        set { _parentId = value == null ? null : string.Intern(value); }
    }

    [JsonPropertyName("slotId")]
    public string? SlotId
    {
        get { return _slotId; }
        set { _slotId = value == null ? null : string.Intern(value); }
    }

    [JsonPropertyName("location")]
    public object? Location { get; set; } // TODO: Can be IItemLocation or number

    [JsonPropertyName("desc")]
    public string? Desc { get; set; }

    [JsonPropertyName("upd")]
    public Upd? Upd { get; set; }
}

public record HideoutItem
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    /// <summary>
    ///     Hideout inventory id that was used by improvement action
    /// </summary>
    [JsonPropertyName("_id")]
    public MongoId _Id
    {
        get { return Id; }
        set { Id = value; }
    }

    [JsonPropertyName("id")]
    public MongoId Id { get; set; }

    [JsonPropertyName("_tpl")]
    public required MongoId Template { get; set; }

    [JsonPropertyName("upd")]
    public Upd? Upd { get; set; }

    [JsonPropertyName("count")]
    public double? Count { get; set; }

    public Item ConvertToItem()
    {
        return new Item
        {
            Id = Id,
            Template = Template,
            Upd = Upd,
        };
    }
}

public record ItemLocation
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("x")]
    public int? X { get; set; }

    [JsonPropertyName("y")]
    public int? Y { get; set; }

    [JsonPropertyName("isSearched")]
    public bool? IsSearched { get; set; }

    /// <summary>
    ///     SPT property?
    /// </summary>
    [JsonPropertyName("rotation")]
    public bool? Rotation { get; set; }

    [JsonPropertyName("r")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ItemRotation R { get; set; }
}

public enum ItemRotation
{
    // Token: 0x0400259F RID: 9631
    Horizontal,

    // Token: 0x040025A0 RID: 9632
    Vertical,
}

public record Upd
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    public UpdBuff? Buff { get; set; }

    public double? OriginalStackObjectsCount { get; set; }

    public UpdTogglable? Togglable { get; set; }

    public UpdMap? Map { get; set; }

    public UpdTag? Tag { get; set; }

    /// <summary>
    ///     SPT specific property, not made by BSG
    /// </summary>
    [JsonPropertyName("sptPresetId")]
    public string? SptPresetId { get; set; }

    public UpdFaceShield? FaceShield { get; set; }

    [JsonConverter(typeof(StringToNumberFactoryConverter))]
    public double? StackObjectsCount { get; set; } // TODO: LootDumpGen is outputting doubles, we can turn back to int once fixed

    public bool? UnlimitedCount { get; set; }

    public UpdRepairable? Repairable { get; set; }

    public UpdRecodableComponent? RecodableComponent { get; set; }

    public UpdFireMode? FireMode { get; set; }

    public bool? SpawnedInSession { get; set; }

    public UpdLight? Light { get; set; }

    public UpdKey? Key { get; set; }

    public UpdResource? Resource { get; set; }

    public UpdSight? Sight { get; set; }

    public UpdMedKit? MedKit { get; set; }

    public UpdFoodDrink? FoodDrink { get; set; }

    public UpdDogtag? Dogtag { get; set; }

    public int? BuyRestrictionMax { get; set; }

    public int? BuyRestrictionCurrent { get; set; }

    public UpdFoldable? Foldable { get; set; }

    public UpdSideEffect? SideEffect { get; set; }

    public UpdRepairKit? RepairKit { get; set; }

    public UpdCultistAmulet? CultistAmulet { get; set; }

    public PinLockState? PinLockState { get; set; }

    public LockableComponent? Lockable { get; set; }
}

public record LockableKeyComponent
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    public float? RelativeValue { get; set; }
    public int? NumberOfUsages { get; set; }
}

public record LockableComponent
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    public string[]? KeyIds { get; set; }
    public bool? Locked { get; set; }
    public LockableKeyComponent? KeyComponent { get; set; }
}

[EftEnumConverter]
public enum PinLockState
{
    Free,
    Pinned,
    Locked,
}

public record UpdBuff
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("Rarity")]
    public string? Rarity { get; set; }

    [JsonPropertyName("BuffType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BuffType? BuffType { get; set; }

    [JsonPropertyName("Value")]
    public double? Value { get; set; }

    [JsonPropertyName("ThresholdDurability")]
    public double? ThresholdDurability { get; set; }
}

public record UpdTogglable
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("On")]
    public bool? On { get; set; }
}

public record UpdMap
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("Markers")]
    public List<MapMarker>? Markers { get; set; }
}

public record MapMarker
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    [JsonPropertyName("X")]
    public double? X { get; set; }

    [JsonPropertyName("Y")]
    public double? Y { get; set; }

    [JsonPropertyName("Note")]
    public string? Note { get; set; }
}

public record UpdTag
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("Color")]
    public int? Color { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }
}

public record UpdFaceShield
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("Hits")]
    public int? Hits { get; set; }

    [JsonPropertyName("HitSeed")]
    public int? HitSeed { get; set; }
}

public record UpdRepairable
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("Durability")]
    public double? Durability { get; set; }

    [JsonPropertyName("MaxDurability")]
    public double? MaxDurability { get; set; }
}

public record UpdRecodableComponent
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("IsEncoded")]
    public bool? IsEncoded { get; set; }
}

public record UpdMedKit
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("HpResource")]
    public double? HpResource { get; set; }
}

public record UpdSight
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("ScopesCurrentCalibPointIndexes")]
    public List<int>? ScopesCurrentCalibPointIndexes { get; set; }

    [JsonPropertyName("ScopesSelectedModes")]
    public List<int>? ScopesSelectedModes { get; set; }

    [JsonPropertyName("SelectedScope")]
    public int? SelectedScope { get; set; }

    public double? ScopeZoomValue { get; set; }
}

public record UpdFoldable
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("Folded")]
    public bool? Folded { get; set; }
}

public record UpdFireMode
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("FireMode")]
    public string? FireMode { get; set; }
}

public record UpdFoodDrink
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("HpPercent")]
    public double? HpPercent { get; set; }
}

public record UpdKey
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    // Checked in client
    [JsonPropertyName("NumberOfUsages")]
    public int? NumberOfUsages { get; set; }
}

public record UpdResource
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("Value")]
    public double? Value { get; set; }

    [JsonPropertyName("UnitsConsumed")]
    public double? UnitsConsumed { get; set; }
}

public record UpdLight
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("IsActive")]
    public bool? IsActive { get; set; }

    [JsonPropertyName("SelectedMode")]
    public int? SelectedMode { get; set; }
}

public record UpdDogtag
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("AccountId")]
    public string? AccountId { get; set; }

    [JsonPropertyName("ProfileId")]
    public string? ProfileId { get; set; }

    [JsonPropertyName("Nickname")]
    public string? Nickname { get; set; }

    [JsonPropertyName("Side")]
    [JsonConverter(typeof(DogtagSideConverter))]
    public DogtagSide? Side { get; set; }

    [JsonPropertyName("Level")]
    public double? Level { get; set; }

    [JsonPropertyName("Time")]
    public string? Time { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("KillerAccountId")]
    public string? KillerAccountId { get; set; }

    [JsonPropertyName("KillerProfileId")]
    public string? KillerProfileId { get; set; }

    [JsonPropertyName("KillerName")]
    public string? KillerName { get; set; }

    [JsonPropertyName("WeaponName")]
    public string? WeaponName { get; set; }

    public bool? CarriedByGroupMember { get; set; }
}

public record UpdSideEffect
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("Value")]
    public double? Value { get; set; }
}

public record UpdRepairKit
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("Resource")]
    public double? Resource { get; set; }
}

public record UpdCultistAmulet
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("NumberOfUsages")]
    public double? NumberOfUsages { get; set; }
}
