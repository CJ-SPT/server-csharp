using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.Inventory;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;

namespace SPTarkov.Server.Core.Helpers;

[Injectable]
public class HideoutHelper(
    ISptLogger<HideoutHelper> _logger,
    TimeUtil _timeUtil,
    ServerLocalisationService _serverLocalisationService,
    DatabaseService _databaseService,
    EventOutputHolder _eventOutputHolder,
    HttpResponseUtil _httpResponseUtil,
    ProfileHelper _profileHelper,
    InventoryHelper _inventoryHelper,
    ItemHelper _itemHelper,
    ICloner _cloner
)
{
    public static readonly MongoId BitcoinProductionId = new("5d5c205bd582a50d042a3c0e");
    public static readonly MongoId WaterCollectorId = new("5d5589c1f934db045e6c5492");
    public const int MaxSkillPoint = 5000;

    /// <summary>
    ///     Add production to profiles' Hideout.Production array
    /// </summary>
    /// <param name="pmcData">Profile to add production to</param>
    /// <param name="productionRequest">Production request</param>
    /// <param name="sessionID">Session id</param>
    /// <returns>client response</returns>
    public void RegisterProduction(
        PmcData pmcData,
        HideoutSingleProductionStartRequestData productionRequest,
        string sessionID
    )
    {
        var recipe = _databaseService
            .GetHideout()
            .Production.Recipes.FirstOrDefault(production =>
                production.Id == productionRequest.RecipeId
            );
        if (recipe is null)
        {
            _logger.Error(
                _serverLocalisationService.GetText(
                    "hideout-missing_recipe_in_db",
                    productionRequest.RecipeId
                )
            );

            _httpResponseUtil.AppendErrorToOutput(_eventOutputHolder.GetOutput(sessionID));
        }

        // @Important: Here we need to be very exact:
        // - normal recipe: Production time value is stored in attribute "productionType" with small "p"
        // - scav case recipe: Production time value is stored in attribute "ProductionType" with capital "P"
        if (pmcData.Hideout?.Production is null)
        {
            pmcData.Hideout.Production = new Dictionary<string, Production?>();
        }

        var modifiedProductionTime = GetAdjustedCraftTimeWithSkills(
            pmcData,
            productionRequest.RecipeId
        );

        var production = InitProduction(
            productionRequest.RecipeId,
            modifiedProductionTime ?? 0,
            recipe.NeedFuelForAllProductionTime
        );

        // Store the tools used for this production, so we can return them later
        if (productionRequest.Tools?.Count > 0)
        {
            production.SptRequiredTools = [];

            foreach (var tool in productionRequest.Tools)
            {
                var toolItem = _cloner.Clone(
                    pmcData.Inventory.Items.FirstOrDefault(x => x.Id == tool.Id)
                );

                // Make sure we only return as many as we took
                _itemHelper.AddUpdObjectToItem(toolItem);

                toolItem.Upd.StackObjectsCount = tool.Count;

                production.SptRequiredTools.Add(
                    new Item
                    {
                        Id = new MongoId(),
                        Template = toolItem.Template,
                        Upd = toolItem.Upd,
                    }
                );
            }
        }

        pmcData.Hideout.Production[productionRequest.RecipeId] = production;
    }

    /// <summary>
    ///     Add production to profiles' Hideout.Production array
    /// </summary>
    /// <param name="pmcData">Profile to add production to</param>
    /// <param name="productionRequest">Production request</param>
    /// <param name="sessionID">Session id</param>
    /// <returns>client response</returns>
    public void RegisterProduction(
        PmcData pmcData,
        HideoutContinuousProductionStartRequestData productionRequest,
        string sessionID
    )
    {
        var recipe = _databaseService
            .GetHideout()
            .Production.Recipes.FirstOrDefault(production =>
                production.Id == productionRequest.RecipeId
            );
        if (recipe is null)
        {
            _logger.Error(
                _serverLocalisationService.GetText(
                    "hideout-missing_recipe_in_db",
                    productionRequest.RecipeId
                )
            );

            _httpResponseUtil.AppendErrorToOutput(_eventOutputHolder.GetOutput(sessionID));
        }

        // @Important: Here we need to be very exact:
        // - normal recipe: Production time value is stored in attribute "productionType" with small "p"
        // - scav case recipe: Production time value is stored in attribute "ProductionType" with capital "P"
        if (pmcData.Hideout?.Production is null)
        {
            pmcData.Hideout.Production = new Dictionary<string, Production?>();
        }

        var modifiedProductionTime = GetAdjustedCraftTimeWithSkills(
            pmcData,
            productionRequest.RecipeId
        );

        var production = InitProduction(
            productionRequest.RecipeId,
            modifiedProductionTime ?? 0,
            recipe.NeedFuelForAllProductionTime
        );

        pmcData.Hideout.Production[productionRequest.RecipeId] = production;
    }

    /// <summary>
    ///     This convenience function initializes new Production Object
    ///     with all the constants.
    /// </summary>
    public Production InitProduction(
        string recipeId,
        double productionTime,
        bool? needFuelForAllProductionTime
    )
    {
        return new Production
        {
            Progress = 0,
            InProgress = true,
            RecipeId = recipeId,
            StartTimestamp = _timeUtil.GetTimeStamp(),
            ProductionTime = productionTime,
            Products = [],
            GivenItemsInStart = [],
            Interrupted = false,
            NeedFuelForAllProductionTime = needFuelForAllProductionTime, // Used when sending to client
            needFuelForAllProductionTime = needFuelForAllProductionTime, // used when stored in production.json
            SkipTime = 0,
        };
    }

    /// <summary>
    ///     Apply bonus to player profile given after completing hideout upgrades
    /// </summary>
    /// <param name="profileData">Profile to add bonus to</param>
    /// <param name="bonus">Bonus to add to profile</param>
    public void ApplyPlayerUpgradesBonuses(PmcData profileData, Bonus bonus)
    {
        // Handle additional changes some bonuses need before being added
        switch (bonus.Type)
        {
            case BonusType.StashSize:
            {
                // Find stash item and adjust tpl to new tpl from bonus
                var stashItem = profileData.Inventory.Items.FirstOrDefault(x =>
                    x.Id == profileData.Inventory.Stash
                );
                if (stashItem is null)
                {
                    _logger.Warning(
                        _serverLocalisationService.GetText(
                            "hideout-unable_to_apply_stashsize_bonus_no_stash_found",
                            profileData.Inventory.Stash
                        )
                    );
                }

                stashItem.Template = bonus.TemplateId;

                break;
            }
            case BonusType.MaximumEnergyReserve:
                // Amend max energy in profile
                profileData.Health.Energy.Maximum += bonus.Value;
                break;
            case BonusType.TextBonus:
                // Delete values before they're added to profile
                bonus.IsPassive = null;
                bonus.IsProduction = null;
                bonus.IsVisible = null;
                break;
        }

        // Add bonus to player bonuses array in profile
        // EnergyRegeneration, HealthRegeneration, RagfairCommission, ScavCooldownTimer, SkillGroupLevelingBoost, ExperienceRate, QuestMoneyReward etc
        if (_logger.IsLogEnabled(LogLevel.Debug))
        {
            _logger.Debug($"Adding bonus: {bonus.Type} to profile, value: {bonus.Value}");
        }

        profileData.Bonuses.Add(bonus);
    }

    /// <summary>
    ///     Process a players hideout, update areas that use resources + increment production timers
    /// </summary>
    /// <param name="sessionID">Session id</param>
    public void UpdatePlayerHideout(string sessionID)
    {
        var pmcData = _profileHelper.GetPmcProfile(sessionID);
        var hideoutProperties = GetHideoutProperties(pmcData);

        pmcData.Hideout.SptUpdateLastRunTimestamp ??= _timeUtil.GetTimeStamp();

        UpdateAreasWithResources(sessionID, pmcData, hideoutProperties);
        UpdateProductionTimers(pmcData, hideoutProperties);
        pmcData.Hideout.SptUpdateLastRunTimestamp = _timeUtil.GetTimeStamp();
    }

    /// <summary>
    ///     Get various properties that will be passed to hideout update-related functions
    /// </summary>
    /// <param name="pmcData">Player profile</param>
    /// <returns>Hideout-related values</returns>
    protected HideoutProperties GetHideoutProperties(PmcData pmcData)
    {
        var bitcoinFarm = pmcData.Hideout.Areas.FirstOrDefault(area =>
            area.Type == HideoutAreas.BitcoinFarm
        );
        var bitcoinCount = (bitcoinFarm?.Slots).Count(slot => slot.Items is not null); // Get slots with an item property

        var hideoutProperties = new HideoutProperties
        {
            BtcFarmGcs = bitcoinCount,
            IsGeneratorOn =
                pmcData
                    .Hideout.Areas.FirstOrDefault(area => area.Type == HideoutAreas.Generator)
                    ?.Active ?? false,
            WaterCollectorHasFilter = DoesWaterCollectorHaveFilter(
                pmcData.Hideout.Areas.FirstOrDefault(area =>
                    area.Type == HideoutAreas.WaterCollector
                )
            ),
        };

        return hideoutProperties;
    }

    /// <summary>
    ///     Does a water collection hideout area have a water filter installed
    /// </summary>
    /// <param name="waterCollector">Hideout area to check</param>
    /// <returns></returns>
    protected static bool DoesWaterCollectorHaveFilter(BotHideoutArea waterCollector)
    {
        // Can put filters in from L3
        if (waterCollector.Level == 3)
        // Has filter in at least one slot
        {
            return waterCollector.Slots.Any(slot => slot.Items is not null);
        }

        // No Filter
        return false;
    }

    /// <summary>
    ///     Iterate over productions and update their progress timers
    /// </summary>
    /// <param name="pmcData">Profile to check for productions and update</param>
    /// <param name="hideoutProperties">Hideout properties</param>
    protected void UpdateProductionTimers(PmcData pmcData, HideoutProperties hideoutProperties)
    {
        var recipes = _databaseService.GetHideout().Production;

        // Check each production and handle edge cases if necessary
        foreach (var prodId in pmcData.Hideout?.Production)
        {
            if (pmcData.Hideout.Production.TryGetValue(prodId.Key, out var craft) && craft is null)
            {
                // Craft value is undefined, get rid of it (could be from cancelling craft that needs cleaning up)
                pmcData.Hideout.Production.Remove(prodId.Key);

                continue;
            }

            if (craft.Progress == null)
            {
                _logger.Warning(
                    _serverLocalisationService.GetText(
                        "hideout-craft_has_undefined_progress_value_defaulting",
                        prodId
                    )
                );
                craft.Progress = 0;
            }

            // Skip processing (Don't skip continuous crafts like bitcoin farm or cultist circle)
            if (craft.IsCraftComplete())
            {
                continue;
            }

            // Special handling required
            if (craft.IsCraftOfType(HideoutAreas.ScavCase))
            {
                UpdateScavCaseProductionTimer(pmcData, prodId.Key);

                continue;
            }

            if (craft.IsCraftOfType(HideoutAreas.WaterCollector))
            {
                UpdateWaterCollectorProductionTimer(pmcData, prodId.Key, hideoutProperties);

                continue;
            }

            // Continuous craft
            if (craft.IsCraftOfType(HideoutAreas.BitcoinFarm))
            {
                UpdateBitcoinFarm(
                    pmcData,
                    pmcData.Hideout.Production.GetValueOrDefault(prodId.Key),
                    hideoutProperties.BtcFarmGcs,
                    hideoutProperties.IsGeneratorOn
                );

                continue;
            }

            // No recipe, needs special handling
            if (craft.IsCraftOfType(HideoutAreas.CircleOfCultists))
            {
                UpdateCultistCircleCraftProgress(pmcData, prodId.Key);

                continue;
            }

            // Ensure recipe exists before using it in updateProductionProgress()
            var recipe = recipes?.Recipes?.FirstOrDefault(r => r.Id == prodId.Key);
            if (recipe is null)
            {
                _logger.Error(
                    _serverLocalisationService.GetText("hideout-missing_recipe_for_area", prodId)
                );

                continue;
            }

            UpdateProductionProgress(pmcData, prodId.Key, recipe, hideoutProperties);
        }
    }

    /// <summary>
    ///     Update progress timer for water collector
    /// </summary>
    /// <param name="pmcData">profile to update</param>
    /// <param name="productionId">id of water collection production to update</param>
    /// <param name="hideoutProperties">Hideout properties</param>
    protected void UpdateWaterCollectorProductionTimer(
        PmcData pmcData,
        string productionId,
        HideoutProperties hideoutProperties
    )
    {
        var timeElapsed = GetTimeElapsedSinceLastServerTick(
            pmcData,
            hideoutProperties.IsGeneratorOn
        );
        if (hideoutProperties.WaterCollectorHasFilter)
        {
            pmcData.Hideout.Production[productionId].Progress += timeElapsed;
        }
    }

    /// <summary>
    ///     Update a productions progress value based on the amount of time that has passed
    /// </summary>
    /// <param name="pmcData">Player profile</param>
    /// <param name="prodId">Production id being crafted</param>
    /// <param name="recipe">Recipe data being crafted</param>
    /// <param name="hideoutProperties"></param>
    protected void UpdateProductionProgress(
        PmcData pmcData,
        string prodId,
        HideoutProduction recipe,
        HideoutProperties hideoutProperties
    )
    {
        // Production is complete, no need to do any calculations
        if (DoesProgressMatchProductionTime(pmcData, prodId))
        {
            return;
        }

        // Get seconds since last hideout update + now
        var timeElapsed = GetTimeElapsedSinceLastServerTick(
            pmcData,
            hideoutProperties.IsGeneratorOn,
            recipe
        );

        // Increment progress by time passed
        pmcData.Hideout.Production.TryGetValue(prodId, out var production);

        // Some items NEED power to craft (e.g. DSP)
        if (
            production.needFuelForAllProductionTime.GetValueOrDefault()
            && hideoutProperties.IsGeneratorOn
        )
        {
            production.Progress += timeElapsed;
        }
        else if (!production.needFuelForAllProductionTime.GetValueOrDefault())
        // Increment progress if production does not necessarily need fuel to continue
        {
            production.Progress += timeElapsed;
        }

        // Limit progress to total production time if progress is over (don't run for continuous crafts)
        if (!(recipe.Continuous ?? false))
        // If progress is larger than prod time, return ProductionTime, hard cap the value
        {
            production.Progress = Math.Min(
                production.Progress ?? 0,
                production.ProductionTime ?? 0
            );
        }
    }

    protected void UpdateCultistCircleCraftProgress(PmcData pmcData, string prodId)
    {
        pmcData.Hideout.Production.TryGetValue(prodId, out var production);

        // Check if we're already complete, skip
        if (
            (production.AvailableForFinish ?? false)
            && !production.InProgress.GetValueOrDefault(false)
        )
        {
            return;
        }

        // Get seconds since last hideout update
        var timeElapsedSeconds =
            _timeUtil.GetTimeStamp() - pmcData.Hideout.SptUpdateLastRunTimestamp;

        // Increment progress by time passed if progress is less than time needed
        if (production.Progress < production.ProductionTime)
        {
            production.Progress += timeElapsedSeconds;

            // Check if craft is complete
            if (production.Progress >= production.ProductionTime)
            {
                FlagCultistCircleCraftAsComplete(production);
            }

            return;
        }

        // Craft in complete
        FlagCultistCircleCraftAsComplete(production);
    }

    protected static void FlagCultistCircleCraftAsComplete(Production production)
    {
        // Craft is complete, flag as such
        production.AvailableForFinish = true;

        // The client expects `Progress` to be 0, and `inProgress` to be false when a circle is complete
        production.Progress = 0;
        production.InProgress = false;
    }

    /// <summary>
    ///     Check if a productions progress value matches its corresponding recipes production time value
    /// </summary>
    /// <param name="pmcData">Player profile</param>
    /// <param name="prodId">Production id</param>
    /// <returns>progress matches productionTime from recipe</returns>
    protected static bool DoesProgressMatchProductionTime(PmcData pmcData, string prodId)
    {
        return pmcData.Hideout.Production[prodId].Progress
            == pmcData.Hideout.Production[prodId].ProductionTime;
    }

    /// <summary>
    ///     Update progress timer for scav case
    /// </summary>
    /// <param name="pmcData">Profile to update</param>
    /// <param name="productionId">Id of scav case production to update</param>
    protected void UpdateScavCaseProductionTimer(PmcData pmcData, string productionId)
    {
        var timeElapsed =
            _timeUtil.GetTimeStamp()
            - pmcData.Hideout.Production[productionId].StartTimestamp
            - pmcData.Hideout.Production[productionId].Progress;

        pmcData.Hideout.Production[productionId].Progress += timeElapsed;
    }

    /// <summary>
    ///     Iterate over hideout areas that use resources (fuel/filters etc) and update associated values
    /// </summary>
    /// <param name="sessionID">Session id</param>
    /// <param name="pmcData">Profile to update areas of</param>
    /// <param name="hideoutProperties">hideout properties</param>
    protected void UpdateAreasWithResources(
        string sessionID,
        PmcData pmcData,
        HideoutProperties hideoutProperties
    )
    {
        foreach (var area in pmcData.Hideout.Areas)
        {
            switch (area.Type)
            {
                case HideoutAreas.Generator:
                    if (hideoutProperties.IsGeneratorOn)
                    {
                        UpdateFuel(area, pmcData, hideoutProperties.IsGeneratorOn);
                    }

                    break;
                case HideoutAreas.WaterCollector:
                    UpdateWaterCollector(sessionID, pmcData, area, hideoutProperties);
                    break;

                case HideoutAreas.AirFilteringUnit:
                    if (hideoutProperties.IsGeneratorOn)
                    {
                        UpdateAirFilters(area, pmcData, hideoutProperties.IsGeneratorOn);
                    }

                    break;
            }
        }
    }

    /// <summary>
    ///     Decrease fuel from generator slots based on amount of time since last time this occurred
    /// </summary>
    /// <param name="generatorArea">Hideout area</param>
    /// <param name="pmcData">Player profile</param>
    /// <param name="isGeneratorOn">Is the generator turned on since last update</param>
    protected void UpdateFuel(BotHideoutArea generatorArea, PmcData pmcData, bool isGeneratorOn)
    {
        // 1 resource last 14 min 27 sec, 1/14.45/60 = 0.00115
        // 10-10-2021 From wiki, 1 resource last 12 minutes 38 seconds, 1/12.63333/60 = 0.00131
        var fuelUsedSinceLastTick =
            _databaseService.GetHideout().Settings.GeneratorFuelFlowRate
            * GetTimeElapsedSinceLastServerTick(pmcData, isGeneratorOn);

        // Get all fuel consumption bonuses, returns an empty array if none found
        var profileFuelConsomptionBonusSum = pmcData.GetBonusValueFromProfile(
            BonusType.FuelConsumption
        );

        // An increase in "bonus" consumption is actually an increase in consumption, so invert this for later use
        var fuelConsumptionBonusRate = -(profileFuelConsomptionBonusSum / 100);

        // An increase in hideout management bonus is a decrease in consumption
        var hideoutManagementConsumptionBonusRate = GetHideoutManagementConsumptionBonus(pmcData);

        var combinedBonus =
            1.0 - (fuelConsumptionBonusRate + hideoutManagementConsumptionBonusRate);

        // Sanity check, never let fuel consumption go negative, otherwise it returns fuel to the player
        if (combinedBonus < 0)
        {
            combinedBonus = 0;
        }

        fuelUsedSinceLastTick *= combinedBonus;

        var hasFuelRemaining = false;
        double pointsConsumed;
        for (var i = 0; i < generatorArea.Slots.Count; i++)
        {
            var generatorSlot = generatorArea.Slots[i];
            if (generatorSlot?.Items is null)
            // No item in slot, skip
            {
                continue;
            }

            var fuelItemInSlot = generatorSlot?.Items.FirstOrDefault();
            if (fuelItemInSlot is null)
            // No item in slot, skip
            {
                continue;
            }

            var fuelRemaining = fuelItemInSlot.Upd?.Resource?.Value;
            if (fuelRemaining == 0)
            // No fuel left, skip
            {
                continue;
            }

            // Undefined fuel, fresh fuel item and needs its max fuel amount looked up
            if (fuelRemaining is null)
            {
                var fuelItemTemplate = _itemHelper.GetItem(fuelItemInSlot.Template).Value;
                pointsConsumed = fuelUsedSinceLastTick ?? 0;
                fuelRemaining = fuelItemTemplate.Properties.MaxResource - fuelUsedSinceLastTick;
            }
            else
            {
                // Fuel exists already, deduct fuel from item remaining value
                pointsConsumed = (double)(
                    (fuelItemInSlot.Upd.Resource.UnitsConsumed ?? 0) + fuelUsedSinceLastTick
                );
                fuelRemaining -= fuelUsedSinceLastTick;
            }

            // Round values to keep accuracy
            fuelRemaining = Math.Round(fuelRemaining * 10000 ?? 0) / 10000;
            pointsConsumed = Math.Round(pointsConsumed * 10000) / 10000;

            // Fuel consumed / 10 is over 1, add hideout management skill point
            if (pmcData is not null && Math.Floor(pointsConsumed / 10) >= 1)
            {
                _profileHelper.AddSkillPointsToPlayer(pmcData, SkillTypes.HideoutManagement, 1);
                pointsConsumed -= 10;
            }

            var isFuelItemFoundInRaid = fuelItemInSlot.Upd?.SpawnedInSession ?? false;
            if (fuelRemaining > 0)
            {
                // Deducted all used fuel from this container, clean up and exit loop
                fuelItemInSlot.Upd = GetAreaUpdObject(
                    1,
                    fuelRemaining,
                    pointsConsumed,
                    isFuelItemFoundInRaid
                );

                if (_logger.IsLogEnabled(LogLevel.Debug))
                {
                    _logger.Debug(
                        $"Profile: {pmcData.Id} Generator has: {fuelRemaining} fuel left in slot {i + 1}"
                    );
                }

                hasFuelRemaining = true;

                break; // Break to avoid updating all the fuel tanks
            }

            fuelItemInSlot.Upd = GetAreaUpdObject(1, 0, 0, isFuelItemFoundInRaid);

            // Ran out of fuel items to deduct fuel from
            fuelUsedSinceLastTick = Math.Abs(fuelRemaining ?? 0);
            if (_logger.IsLogEnabled(LogLevel.Debug))
            {
                _logger.Debug($"Profile: {pmcData.Id} Generator ran out of fuel");
            }
        }

        // Out of fuel, flag generator as offline
        if (!hasFuelRemaining)
        {
            generatorArea.Active = false;
        }
    }

    protected void UpdateWaterCollector(
        string sessionId,
        PmcData pmcData,
        BotHideoutArea area,
        HideoutProperties hideoutProperties
    )
    {
        // Skip water collector when not level 3 (cant collect until 3)
        if (area.Level != 3)
        {
            return;
        }

        if (!hideoutProperties.WaterCollectorHasFilter)
        {
            return;
        }

        // Canister with purified water craft exists
        if (
            pmcData.Hideout.Production.TryGetValue(WaterCollectorId, out var purifiedWaterCraft)
            && purifiedWaterCraft.GetType() == typeof(Production)
        )
        {
            // Update craft time to account for increases in players craft time skill
            purifiedWaterCraft.ProductionTime = GetAdjustedCraftTimeWithSkills(
                pmcData,
                purifiedWaterCraft.RecipeId,
                true
            );

            UpdateWaterFilters(area, purifiedWaterCraft, hideoutProperties.IsGeneratorOn, pmcData);
        }
        else
        {
            // continuousProductionStart()
            // seem to not trigger consistently
            var recipe = new HideoutSingleProductionStartRequestData
            {
                RecipeId = WaterCollectorId,
                Action = "HideoutSingleProductionStart",
                Items = [],
                Tools = [],
                Timestamp = _timeUtil.GetTimeStamp(),
            };

            RegisterProduction(pmcData, recipe, sessionId);
        }
    }

    /// <summary>
    ///     Get craft time and make adjustments to account for dev profile + crafting skill level
    /// </summary>
    /// <param name="pmcData">Player profile making craft</param>
    /// <param name="recipeId">Recipe being crafted</param>
    /// <param name="applyHideoutManagementBonus">Should the hideout management bonus be applied to the calculation</param>
    /// <returns>Items craft time with bonuses subtracted</returns>
    public double? GetAdjustedCraftTimeWithSkills(
        PmcData pmcData,
        string recipeId,
        bool applyHideoutManagementBonus = false
    )
    {
        var globalSkillsDb = _databaseService.GetGlobals().Configuration.SkillsSettings;

        var recipe = _databaseService
            .GetHideout()
            .Production.Recipes.FirstOrDefault(production => production.Id == recipeId);
        if (recipe is null)
        {
            _logger.Error(
                _serverLocalisationService.GetText("hideout-missing_recipe_in_db", recipeId)
            );

            return null;
        }

        var timeReductionSeconds = 0D;

        // Bitcoin farm is excluded from crafting skill cooldown reduction
        if (recipeId != BitcoinProductionId)
        // Seconds to deduct from crafts total time
        {
            timeReductionSeconds += GetSkillProductionTimeReduction(
                pmcData,
                recipe.ProductionTime ?? 0,
                SkillTypes.Crafting,
                globalSkillsDb.Crafting.ProductionTimeReductionPerLevel ?? 0
            );
        }

        // Some crafts take into account hideout management, e.g. fuel, water/air filters
        if (applyHideoutManagementBonus)
        {
            timeReductionSeconds += GetSkillProductionTimeReduction(
                pmcData,
                recipe.ProductionTime ?? 0,
                SkillTypes.HideoutManagement,
                globalSkillsDb.HideoutManagement.ConsumptionReductionPerLevel ?? 0
            );
        }

        var modifiedProductionTime = recipe.ProductionTime - timeReductionSeconds;
        if (modifiedProductionTime > 0 && _profileHelper.IsDeveloperAccount(pmcData.Id))
        {
            modifiedProductionTime = 40;
        }

        // Sanity check, don't let anything craft in less than 5 seconds
        if (modifiedProductionTime < 5)
        {
            modifiedProductionTime = 5;
        }

        return modifiedProductionTime;
    }

    /// <summary>
    ///     Adjust water filter objects resourceValue or delete when they reach 0 resource
    /// </summary>
    /// <param name="waterFilterArea">Water filter area to update</param>
    /// <param name="production">Production object</param>
    /// <param name="isGeneratorOn">Is generator enabled</param>
    /// <param name="pmcData">Player profile</param>
    protected void UpdateWaterFilters(
        BotHideoutArea waterFilterArea,
        Production production,
        bool isGeneratorOn,
        PmcData pmcData
    )
    {
        var filterDrainRate = GetWaterFilterDrainRate(pmcData);
        var craftProductionTime = GetTotalProductionTimeSeconds(WaterCollectorId);
        var secondsSinceServerTick = GetTimeElapsedSinceLastServerTick(pmcData, isGeneratorOn);

        filterDrainRate = GetTimeAdjustedWaterFilterDrainRate(
            secondsSinceServerTick ?? 0,
            craftProductionTime,
            production.Progress ?? 0,
            filterDrainRate
        );

        // Production hasn't completed
        var pointsConsumed = 0D;

        // Check progress against the productions craft time (don't use base time as it doesn't include any time bonuses profile has)
        if (production.Progress > production.ProductionTime)
        // Craft is complete nothing to do
        {
            return;
        }

        // Check all slots that take water filters until we find one with filter in it
        for (var i = 0; i < waterFilterArea.Slots.Count; i++)
        {
            // No water filter in slot, skip
            if (waterFilterArea.Slots[i].Items is null)
            {
                continue;
            }

            var waterFilterItemInSlot = waterFilterArea.Slots[i].Items[0];

            // How many units of filter are left
            var resourceValue = waterFilterItemInSlot.Upd?.Resource is not null
                ? waterFilterItemInSlot.Upd.Resource.Value
                : null;
            if (resourceValue is null)
            {
                // Missing, is new filter, add default and subtract usage
                resourceValue = 100 - filterDrainRate;
                pointsConsumed = filterDrainRate;
            }
            else
            {
                pointsConsumed =
                    (waterFilterItemInSlot.Upd.Resource.UnitsConsumed ?? 0) + filterDrainRate;
                resourceValue -= filterDrainRate;
            }

            // Round to get values to 3dp
            resourceValue = Math.Round(resourceValue * 1000 ?? 0) / 1000;
            pointsConsumed = Math.Round(pointsConsumed * 1000) / 1000;

            // Check units consumed for possible increment of hideout mgmt skill point
            if (pmcData is not null && Math.Floor(pointsConsumed / 10) >= 1)
            {
                _profileHelper.AddSkillPointsToPlayer(pmcData, SkillTypes.HideoutManagement, 1);
                pointsConsumed -= 10;
            }

            // Filter has some fuel left in it after our adjustment
            if (resourceValue > 0)
            {
                var isWaterFilterFoundInRaid = waterFilterItemInSlot.Upd?.SpawnedInSession ?? false;

                // Set filters consumed amount
                waterFilterItemInSlot.Upd = GetAreaUpdObject(
                    1,
                    resourceValue,
                    pointsConsumed,
                    isWaterFilterFoundInRaid
                );
                if (_logger.IsLogEnabled(LogLevel.Debug))
                {
                    _logger.Debug($"Water filter has: {resourceValue} units left in slot {i + 1}");
                }

                break; // Break here to avoid iterating other filters now w're done
            }

            // Filter ran out / used up
            waterFilterArea.Slots[i].Items = null;
            // Update remaining resources to be subtracted
            filterDrainRate = Math.Abs(resourceValue ?? 0);
        }
    }

    /// <summary>
    ///     Get an adjusted water filter drain rate based on time elapsed since last run,
    ///     handle edge case when craft time has gone on longer than total production time
    /// </summary>
    /// <param name="secondsSinceServerTick">Time passed</param>
    /// <param name="totalProductionTime">Total time collecting water</param>
    /// <param name="productionProgress">How far water collector has progressed</param>
    /// <param name="baseFilterDrainRate">Base drain rate</param>
    /// <returns>Drain rate (adjusted)</returns>
    protected static double GetTimeAdjustedWaterFilterDrainRate(
        long secondsSinceServerTick,
        double totalProductionTime,
        double productionProgress,
        double baseFilterDrainRate
    )
    {
        var drainTimeSeconds =
            secondsSinceServerTick > totalProductionTime
                ? totalProductionTime - productionProgress // More time passed than prod time, get total minus the current progress
                : secondsSinceServerTick;

        // Multiply base drain rate by time passed
        return baseFilterDrainRate * drainTimeSeconds;
    }

    /// <summary>
    ///     Get the water filter drain rate based on hideout bonuses player has
    /// </summary>
    /// <param name="pmcData">Player profile</param>
    /// <returns>Drain rate</returns>
    protected double GetWaterFilterDrainRate(PmcData pmcData)
    {
        var globalSkillsDb = _databaseService.GetGlobals().Configuration.SkillsSettings;

        // 100 resources last 8 hrs 20 min, 100/8.33/60/60 = 0.00333
        const double filterDrainRate = 0.00333d;

        var hideoutManagementConsumptionBonus = GetSkillBonusMultipliedBySkillLevel(
            pmcData,
            SkillTypes.HideoutManagement,
            globalSkillsDb.HideoutManagement.ConsumptionReductionPerLevel ?? 0
        );
        var craftSkillTimeReductionMultiplier = GetSkillBonusMultipliedBySkillLevel(
            pmcData,
            SkillTypes.Crafting,
            globalSkillsDb.Crafting.CraftTimeReductionPerLevel ?? 0
        );

        // Never let bonus become 0
        var reductionBonus =
            hideoutManagementConsumptionBonus + craftSkillTimeReductionMultiplier == 0
                ? 1
                : 1 - (hideoutManagementConsumptionBonus + craftSkillTimeReductionMultiplier);

        return filterDrainRate * reductionBonus;
    }

    /// <summary>
    ///     Get the production time in seconds for the desired production
    /// </summary>
    /// <param name="prodId">Id, e.g. Water collector id</param>
    /// <returns>Seconds to produce item</returns>
    protected double GetTotalProductionTimeSeconds(string prodId)
    {
        return _databaseService
                .GetHideout()
                .Production.Recipes.FirstOrDefault(prod => prod.Id == prodId)
                ?.ProductionTime ?? 0;
    }

    /// <summary>
    ///     Create an upd object using passed in parameters
    /// </summary>
    /// <param name="stackCount"></param>
    /// <param name="resourceValue"></param>
    /// <param name="resourceUnitsConsumed"></param>
    /// <param name="isFoundInRaid"></param>
    /// <returns>Upd</returns>
    protected Upd GetAreaUpdObject(
        double stackCount,
        double? resourceValue,
        double resourceUnitsConsumed,
        bool isFoundInRaid
    )
    {
        return new Upd
        {
            StackObjectsCount = stackCount,
            Resource = new UpdResource
            {
                Value = resourceValue,
                UnitsConsumed = resourceUnitsConsumed,
            },
            SpawnedInSession = isFoundInRaid,
        };
    }

    protected void UpdateAirFilters(
        BotHideoutArea airFilterArea,
        PmcData pmcData,
        bool isGeneratorOn
    )
    {
        // 300 resources last 20 hrs, 300/20/60/60 = 0.00416
        /* 10-10-2021 from WIKI (https://escapefromtarkov.fandom.com/wiki/FP-100_filter_absorber)
            Lasts for 17 hours 38 minutes and 49 seconds (23 hours 31 minutes and 45 seconds with elite hideout management skill),
            300/17.64694/60/60 = 0.004722
        */
        var filterDrainRate =
            _databaseService.GetHideout().Settings.AirFilterUnitFlowRate
            * GetTimeElapsedSinceLastServerTick(pmcData, isGeneratorOn);

        // Hideout management resource consumption bonus:
        var hideoutManagementConsumptionBonus = 1.0 - GetHideoutManagementConsumptionBonus(pmcData);
        filterDrainRate *= hideoutManagementConsumptionBonus;
        double pointsConsumed;

        for (var i = 0; i < airFilterArea.Slots.Count; i++)
        {
            if (airFilterArea.Slots[i].Items is not null)
            {
                var resourceValue = airFilterArea.Slots[i].Items[0].Upd?.Resource is not null
                    ? airFilterArea.Slots[i].Items[0].Upd.Resource.Value
                    : null;

                if (resourceValue is null)
                {
                    resourceValue = 300 - filterDrainRate;
                    pointsConsumed = filterDrainRate ?? 0;
                }
                else
                {
                    pointsConsumed =
                        (airFilterArea.Slots[i].Items[0].Upd.Resource.UnitsConsumed ?? 0)
                            + filterDrainRate
                        ?? 0;
                    resourceValue -= filterDrainRate;
                }

                resourceValue = Math.Round(resourceValue * 10000 ?? 0) / 10000;
                pointsConsumed = Math.Round(pointsConsumed * 10000) / 10000;

                // check unit consumed for increment skill point
                if (pmcData is not null && Math.Floor(pointsConsumed / 10) >= 1)
                {
                    _profileHelper.AddSkillPointsToPlayer(pmcData, SkillTypes.HideoutManagement, 1);
                    pointsConsumed -= 10;
                }

                if (resourceValue > 0)
                {
                    airFilterArea.Slots[i].Items[0].Upd = new Upd
                    {
                        StackObjectsCount = 1,
                        Resource = new UpdResource
                        {
                            Value = resourceValue,
                            UnitsConsumed = pointsConsumed,
                        },
                    };
                    if (_logger.IsLogEnabled(LogLevel.Debug))
                    {
                        _logger.Debug($"Air filter: {resourceValue} filter left on slot {i + 1}");
                    }

                    break; // Break here to avoid updating all filters
                }

                airFilterArea.Slots[i].Items = null;
                // Update remaining resources to be subtracted
                filterDrainRate = Math.Abs(resourceValue ?? 0);
            }
        }
    }

    /// <summary>
    /// Increment bitcoin farm progress
    /// </summary>
    /// <param name="pmcData">Player profile</param>
    /// <param name="btcProduction">Hideout btc craft</param>
    /// <param name="btcFarmCGs"></param>
    /// <param name="isGeneratorOn">Is hideout generator powered</param>
    protected void UpdateBitcoinFarm(
        PmcData pmcData,
        Production? btcProduction,
        int? btcFarmCGs,
        bool isGeneratorOn
    )
    {
        if (btcProduction is null)
        {
            _logger.Error(_serverLocalisationService.GetText("hideout-bitcoin_craft_missing"));

            return;
        }

        var isBtcProd = btcProduction.GetType() == typeof(Production);
        if (!isBtcProd)
        {
            return;
        }

        // The wiki has a wrong formula!
        // Do not change unless you validate it with the Client code files!
        // This formula was found on the client files:
        // *******************************************************
        /*
                public override int InstalledSuppliesCount
             {
              get
              {
               return this.int_1;
              }
              protected set
              {
               if (this.int_1 === value)
                        {
                            return;
                        }
                        this.int_1 = value;
                        base.Single_0 = ((this.int_1 === 0) ? 0f : (1f + (float)(this.int_1 - 1) * this.float_4));
                    }
                }
            */
        // **********************************************************
        // At the time of writing this comment, this was GClass1667
        // To find it in case of weird results, use DNSpy and look for usages on class AreaData
        // Look for a GClassXXXX that has a method called "InitDetails" and the only parameter is the AreaData
        // That should be the bitcoin farm production. To validate, try to find the snippet below:
        /*
                protected override void InitDetails(AreaData data)
                {
                    base.InitDetails(data);
                    this.gclass1678_1.Type = EDetailsType.Farming;
                }
            */
        // Needs power to function
        if (!isGeneratorOn)
        // Return with no changes
        {
            return;
        }

        var coinSlotCount = GetBTCSlots(pmcData);

        // Full of bitcoins, halt progress
        if (btcProduction.Products.Count >= coinSlotCount)
        {
            // Set progress to 0
            btcProduction.Progress = 0;

            return;
        }

        var bitcoinProdData = _databaseService
            .GetHideout()
            .Production.Recipes.FirstOrDefault(production => production.Id == BitcoinProductionId);

        // BSG finally fixed their settings, they now get loaded from the settings and used in the client
        var adjustedCraftTime =
            (
                _profileHelper.IsDeveloperAccount(pmcData.SessionId)
                    ? 40
                    : bitcoinProdData.ProductionTime
            ) / (1 + (btcFarmCGs - 1) * _databaseService.GetHideout().Settings.GpuBoostRate);

        // The progress should be adjusted based on the GPU boost rate, but the target is still the base productionTime
        var timeMultiplier = bitcoinProdData.ProductionTime / adjustedCraftTime;
        var timeElapsedSeconds = GetTimeElapsedSinceLastServerTick(pmcData, isGeneratorOn);
        btcProduction.Progress += Math.Floor(timeElapsedSeconds * timeMultiplier ?? 0);

        while (btcProduction.Progress >= bitcoinProdData.ProductionTime)
        {
            if (btcProduction.Products.Count < coinSlotCount)
            // Has space to add a coin to production rewards
            {
                AddBtcToProduction(btcProduction, bitcoinProdData.ProductionTime ?? 0);
            }
            else
            // Filled up bitcoin storage
            {
                btcProduction.Progress = 0;
            }
        }

        btcProduction.StartTimestamp = _timeUtil.GetTimeStamp();
    }

    /// <summary>
    ///     Add bitcoin object to btc production products array and set progress time
    /// </summary>
    /// <param name="btcProd">Bitcoin production object</param>
    /// <param name="coinCraftTimeSeconds">Time to craft a bitcoin</param>
    protected void AddBtcToProduction(Production btcProd, double coinCraftTimeSeconds)
    {
        btcProd.Products.Add(
            new Item
            {
                Id = new MongoId(),
                Template = ItemTpl.BARTER_PHYSICAL_BITCOIN,
                Upd = new Upd { StackObjectsCount = 1 },
            }
        );

        // Deduct time spent crafting from progress
        btcProd.Progress -= coinCraftTimeSeconds;
    }

    /// <summary>
    ///     Get number of ticks that have passed since hideout areas were last processed, reduced when generator is off
    /// </summary>
    /// <param name="pmcData">Player profile</param>
    /// <param name="isGeneratorOn">Is the generator on for the duration of elapsed time</param>
    /// <param name="recipe">Hideout production recipe being crafted we need the ticks for</param>
    /// <returns>Amount of time elapsed in seconds</returns>
    protected long? GetTimeElapsedSinceLastServerTick(
        PmcData pmcData,
        bool isGeneratorOn,
        HideoutProduction? recipe = null
    )
    {
        // Reduce time elapsed (and progress) when generator is off
        var timeElapsed = _timeUtil.GetTimeStamp() - pmcData.Hideout.SptUpdateLastRunTimestamp;

        if (recipe is not null)
        {
            var hideoutArea = _databaseService
                .GetHideout()
                .Areas.FirstOrDefault(area => area.Type == recipe.AreaType);
            if (!(hideoutArea.NeedsFuel ?? false))
            // e.g. Lavatory works at 100% when power is on / off
            {
                return timeElapsed;
            }
        }

        if (!isGeneratorOn)
        {
            timeElapsed = (long)(
                timeElapsed * _databaseService.GetHideout().Settings.GeneratorSpeedWithoutFuel
            );
        }

        return timeElapsed;
    }

    /// <summary>
    ///     Get a count of how many possible BTC can be gathered by the profile
    /// </summary>
    /// <param name="pmcData">Profile to look up</param>
    /// <returns>Coin slot count</returns>
    protected double GetBTCSlots(PmcData pmcData)
    {
        var bitcoinProductions = _databaseService
            .GetHideout()
            .Production.Recipes.FirstOrDefault(production => production.Id == BitcoinProductionId);
        var productionSlots = bitcoinProductions?.ProductionLimitCount ?? 3; // Default to 3 if none found
        var hasManagementSkillSlots = _profileHelper.HasEliteSkillLevel(
            SkillTypes.HideoutManagement,
            pmcData
        );
        var managementSlotsCount = GetEliteSkillAdditionalBitcoinSlotCount() ?? 2;

        return productionSlots + (hasManagementSkillSlots ? managementSlotsCount : 0);
    }

    /// <summary>
    ///     Get a count of how many additional bitcoins player hideout can hold with elite skill
    /// </summary>
    protected double? GetEliteSkillAdditionalBitcoinSlotCount()
    {
        return _databaseService
            .GetGlobals()
            .Configuration.SkillsSettings.HideoutManagement.EliteSlots.BitcoinFarm.Container;
    }

    /// <summary>
    ///     HideoutManagement skill gives a consumption bonus the higher the level
    ///     0.5% per level per 1-51, (25.5% at max)
    /// </summary>
    /// <param name="pmcData">Profile to get hideout consumption level from</param>
    /// <returns>Consumption bonus</returns>
    protected double? GetHideoutManagementConsumptionBonus(PmcData pmcData)
    {
        var hideoutManagementSkill = pmcData.GetSkillFromProfile(SkillTypes.HideoutManagement);
        if (hideoutManagementSkill is null || hideoutManagementSkill.Progress == 0)
        {
            return 0;
        }

        // If the level is 51 we need to round it at 50 so on elite you dont get 25.5%
        // at level 1 you already get 0.5%, so it goes up until level 50. For some reason the wiki
        // says that it caps at level 51 with 25% but as per dump data that is incorrect apparently
        var roundedLevel = Math.Floor(hideoutManagementSkill.Progress / 100 ?? 0D);
        roundedLevel = roundedLevel == 51 ? roundedLevel - 1 : roundedLevel;

        return roundedLevel
            * _databaseService
                .GetGlobals()
                .Configuration.SkillsSettings.HideoutManagement.ConsumptionReductionPerLevel
            / 100;
    }

    /// <summary>
    ///     Get a multiplier based on player's skill level and value per level
    /// </summary>
    /// <param name="pmcData">Player profile</param>
    /// <param name="skill">Player skill from profile</param>
    /// <param name="valuePerLevel">Value from globals.config.SkillsSettings - `PerLevel`</param>
    /// <returns>Multiplier from 0 to 1</returns>
    protected double GetSkillBonusMultipliedBySkillLevel(
        PmcData pmcData,
        SkillTypes skill,
        double valuePerLevel
    )
    {
        var profileSkill = pmcData.GetSkillFromProfile(skill);
        if (profileSkill is null || profileSkill.Progress == 0)
        {
            return 0;
        }

        // If the level is 51 we need to round it at 50 so on elite you dont get 25.5%
        // at level 1 you already get 0.5%, so it goes up until level 50. For some reason the wiki
        // says that it caps at level 51 with 25% but as per dump data that is incorrect apparently
        var roundedLevel = Math.Floor(profileSkill.Progress / 100 ?? 0D);
        roundedLevel = roundedLevel == 51 ? roundedLevel - 1 : roundedLevel;

        return roundedLevel * valuePerLevel / 100;
    }

    /// <summary>
    /// </summary>
    /// <param name="pmcData">Player profile</param>
    /// <param name="productionTime">Time to complete hideout craft in seconds</param>
    /// <param name="skill">Skill bonus to get reduction from</param>
    /// <param name="amountPerLevel">Skill bonus amount to apply</param>
    /// <returns>Seconds to reduce craft time by</returns>
    public double GetSkillProductionTimeReduction(
        PmcData pmcData,
        double productionTime,
        SkillTypes skill,
        double amountPerLevel
    )
    {
        var skillTimeReductionMultiplier = GetSkillBonusMultipliedBySkillLevel(
            pmcData,
            skill,
            amountPerLevel
        );

        return productionTime * skillTimeReductionMultiplier;
    }

    /// <summary>
    ///     Gather crafted BTC from hideout area and add to inventory
    ///     Reset production start timestamp if hideout area at full coin capacity
    /// </summary>
    /// <param name="pmcData">Player profile</param>
    /// <param name="request">Take production request</param>
    /// <param name="sessionId">Session id</param>
    /// <param name="output">Output object to update</param>
    public void GetBTC(
        PmcData pmcData,
        HideoutTakeProductionRequestData request,
        string sessionId,
        ItemEventRouterResponse output
    )
    {
        // Get how many coins were crafted and ready to pick up
        pmcData.Hideout.Production.TryGetValue(BitcoinProductionId, out var bitcoinCraft);
        var craftedCoinCount = bitcoinCraft?.Products?.Count;
        if (bitcoinCraft is null || craftedCoinCount is null)
        {
            var errorMsg = _serverLocalisationService.GetText("hideout-no_bitcoins_to_collect");
            _logger.Error(errorMsg);

            _httpResponseUtil.AppendErrorToOutput(output, errorMsg);

            return;
        }

        List<List<Item>> itemsToAdd = [];
        for (var index = 0; index < craftedCoinCount; index++)
        {
            itemsToAdd.Add(
                [
                    new Item
                    {
                        Id = new MongoId(),
                        Template = ItemTpl.BARTER_PHYSICAL_BITCOIN,
                        Upd = new Upd { StackObjectsCount = 1 },
                    },
                ]
            );
        }

        // Create request for what we want to add to stash
        var addItemsRequest = new AddItemsDirectRequest
        {
            ItemsWithModsToAdd = itemsToAdd,
            FoundInRaid = true,
            UseSortingTable = false,
            Callback = null,
        };

        // Add FiR coins to player inventory
        _inventoryHelper.AddItemsToStash(sessionId, addItemsRequest, pmcData, output);
        if (output.Warnings?.Count > 0)
        {
            return;
        }

        // Is at max capacity + we collected all coins - reset production start time
        var coinSlotCount = GetBTCSlots(pmcData);
        if (pmcData.Hideout.Production[BitcoinProductionId].Products.Count >= coinSlotCount)
        // Set start to now
        {
            pmcData.Hideout.Production[BitcoinProductionId].StartTimestamp =
                _timeUtil.GetTimeStamp();
        }

        // Remove crafted coins from production in profile now they've been collected
        // Can only collect all coins, not individually
        pmcData.Hideout.Production[BitcoinProductionId].Products = [];
    }

    /// <summary>
    ///     Hideout improvement is flagged as complete
    /// </summary>
    /// <param name="improvement">hideout improvement object</param>
    /// <returns>true if complete</returns>
    protected bool HideoutImprovementIsComplete(HideoutImprovement improvement)
    {
        return improvement?.Completed ?? false;
    }

    /// <summary>
    ///     Iterate over hideout improvements not completed and check if they need to be adjusted
    /// </summary>
    /// <param name="profileData">Profile to adjust</param>
    public void SetHideoutImprovementsToCompleted(PmcData profileData)
    {
        foreach (var improvementId in profileData.Hideout.Improvements)
        {
            if (
                !profileData.Hideout.Improvements.TryGetValue(
                    improvementId.Key,
                    out var improvementDetails
                )
            )
            {
                continue;
            }

            if (
                improvementDetails.Completed == false
                && improvementDetails.ImproveCompleteTimestamp < _timeUtil.GetTimeStamp()
            )
            {
                improvementDetails.Completed = true;
            }
        }
    }

    /// <summary>
    ///     Add/remove bonus combat skill based on number of dogtags in place of fame hideout area
    /// </summary>
    /// <param name="profileData">Player profile</param>
    public void ApplyPlaceOfFameDogtagBonus(PmcData pmcData)
    {
        var fameAreaProfile = pmcData.Hideout.Areas.FirstOrDefault(area =>
            area.Type == HideoutAreas.PlaceOfFame
        );

        // Get hideout area 16 bonus array
        var fameAreaDb = _databaseService
            .GetHideout()
            .Areas.FirstOrDefault(area => area.Type == HideoutAreas.PlaceOfFame);

        // Get SkillGroupLevelingBoost object
        var combatBoostBonusDb = fameAreaDb
            .Stages[fameAreaProfile.Level.ToString()]
            .Bonuses.FirstOrDefault(bonus => bonus.Type.ToString() == "SkillGroupLevelingBoost");

        // Get SkillGroupLevelingBoost object in profile
        var combatBonusProfile = pmcData.Bonuses.FirstOrDefault(bonus =>
            bonus.Id == combatBoostBonusDb.Id
        );

        // Get all slotted dogtag items
        var activeDogtags = pmcData
            .Inventory.Items.Where(item => item?.SlotId?.StartsWith("dogtag") ?? false)
            .ToList();

        // Calculate bonus percent (apply hideoutManagement bonus)
        var hideoutManagementSkill = pmcData.GetSkillFromProfile(SkillTypes.HideoutManagement);
        var hideoutManagementSkillBonusPercent = 1 + hideoutManagementSkill.Progress / 10000; // 5100 becomes 0.51, add 1 to it, 1.51
        var bonus =
            GetDogtagCombatSkillBonusPercent(pmcData, activeDogtags)
            * hideoutManagementSkillBonusPercent;

        // Update bonus value to above calcualted value
        combatBonusProfile.Value = Math.Round(bonus ?? 0, 2);
    }

    /// <summary>
    ///     Calculate the raw dogtag combat skill bonus for place of fame based on number of dogtags
    ///     Reverse engineered from client code
    /// </summary>
    /// <param name="pmcData">Player profile</param>
    /// <param name="activeDogtags">Active dogtags in place of fame dogtag slots</param>
    /// <returns>Combat bonus</returns>
    protected static double GetDogtagCombatSkillBonusPercent(
        PmcData pmcData,
        List<Item> activeDogtags
    )
    {
        // Not own dogtag
        // Side = opposite of player
        var result = 0D;
        foreach (var dogtag in activeDogtags)
        {
            if (dogtag.Upd.Dogtag is null)
            {
                continue;
            }

            if (int.Parse(dogtag.Upd.Dogtag?.AccountId) == pmcData.Aid)
            {
                continue;
            }

            result += 0.01 * dogtag.Upd.Dogtag.Level ?? 0;
        }

        return result;
    }

    /// <summary>
    ///     The wall pollutes a profile with various temp buffs/debuffs,
    ///     Remove them all
    /// </summary>
    /// <param name="wallAreaDb">Hideout area data</param>
    /// <param name="pmcData">Player profile</param>
    public void RemoveHideoutWallBuffsAndDebuffs(HideoutArea wallAreaDb, PmcData pmcData)
    {
        // Smush all stage bonuses into one array for easy iteration
        var wallBonuses = wallAreaDb.Stages.SelectMany(stage => stage.Value.Bonuses);

        // Get all bonus Ids that the wall adds
        HashSet<string> bonusIdsToRemove = [];
        foreach (var bonus in wallBonuses)
        {
            bonusIdsToRemove.Add(bonus.Id);
        }

        if (_logger.IsLogEnabled(LogLevel.Debug))
        {
            _logger.Debug($"Removing: {bonusIdsToRemove.Count} bonuses from profile");
        }

        // Remove the wall bonuses from profile by id
        pmcData.Bonuses = pmcData
            .Bonuses.Where(bonus => !bonusIdsToRemove.Contains(bonus.Id))
            .ToList();
    }
}
