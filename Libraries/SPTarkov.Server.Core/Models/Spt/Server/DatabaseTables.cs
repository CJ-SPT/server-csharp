using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace SPTarkov.Server.Core.Models.Spt.Server;

public record DatabaseTables
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    public Bots.Bots? Bots { get; set; }

    public Hideout.Hideout? Hideout { get; set; }

    public LocaleBase? Locales { get; set; }

    public Locations? Locations { get; set; }

    public Match? Match { get; set; }

    public Templates.Templates? Templates { get; set; }

    public Dictionary<MongoId, Trader> Traders { get; set; }

    public Globals? Globals { get; set; }

    public ServerBase? Server { get; set; }

    public SettingsBase? Settings { get; set; }
}
