using System.Text.Json.Serialization;

namespace SPTarkov.Server.Core.Models.Eft.Ws;

/// <summary>
///     This can likely be used for other things, but im naming it specific for its use case for now - Cj
/// </summary>
public record WsStashRowsChanged : WsNotificationEvent
{
    [JsonPropertyName("Changes")]
    public Dictionary<string, double?> Changes { get; set; }
}
