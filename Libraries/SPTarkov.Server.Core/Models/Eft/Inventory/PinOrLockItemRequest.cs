using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace SPTarkov.Server.Core.Models.Eft.Inventory;

public record PinOrLockItemRequest : InventoryBaseActionRequestData
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    /// <summary>
    ///     Id of item being pinned
    /// </summary>
    [JsonPropertyName("Item")]
    public MongoId? Item { get; set; }

    /// <summary>
    ///     "Pinned"/"Locked"/"Free"
    /// </summary>
    [JsonPropertyName("State")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PinLockState? State { get; set; }
}
