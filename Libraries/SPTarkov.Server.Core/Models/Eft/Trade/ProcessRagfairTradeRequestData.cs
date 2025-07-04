using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Inventory;

namespace SPTarkov.Server.Core.Models.Eft.Trade;

public record ProcessRagfairTradeRequestData : InventoryBaseActionRequestData
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("offers")]
    public List<OfferRequest>? Offers { get; set; }
}

public record OfferRequest
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }

    [JsonPropertyName("items")]
    public List<IdWithCount>? Items { get; set; }
}
