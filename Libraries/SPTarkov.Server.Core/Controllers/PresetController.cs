using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Presets;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace SPTarkov.Server.Core.Controllers;

[Injectable]
public class PresetController(
    ISptLogger<PresetController> _logger,
    PresetHelper _presetHelper,
    DatabaseService _databaseService
)
{
    /// <summary>
    ///     Keyed by item tpl, value = collection of preset ids
    /// </summary>
    public void Initialize()
    {
        var presets = _databaseService.GetGlobals().ItemPresets;
        var result = new Dictionary<MongoId, PresetCacheDetails>();
        foreach (var (presetId, preset) in presets)
        {
            if (presetId != preset.Id)
            {
                _logger.Error(
                    $"Preset for template tpl: '{preset.Items.FirstOrDefault()?.Template} {preset.Name}' has invalid key: ({presetId} != {preset.Id}). Skipping"
                );

                continue;
            }

            // Get root items tpl
            var tpl = preset.Items.FirstOrDefault()?.Template;
            result.TryAdd(tpl.Value, new PresetCacheDetails { PresetIds = [] });

            result.TryGetValue(tpl.Value, out var details);
            details.PresetIds.Add(presetId);
            if (preset.Encyclopedia is not null)
            {
                // Flag this preset as being the default for the weapon
                details.DefaultId = preset.Id;
            }
        }

        _presetHelper.HydratePresetStore(result);
    }
}
