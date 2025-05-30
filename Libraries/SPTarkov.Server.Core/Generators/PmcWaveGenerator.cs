﻿using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace SPTarkov.Server.Core.Generators;

[Injectable]
public class PmcWaveGenerator
{
    protected ConfigServer _configServer;
    protected DatabaseService _databaseService;
    protected ISptLogger<PmcWaveGenerator> _logger;
    protected PmcConfig _pmcConfig;
    protected RandomUtil _randomUtil;

    public PmcWaveGenerator(
        ISptLogger<PmcWaveGenerator> _logger,
        RandomUtil _randomUtil,
        DatabaseService _databaseService,
        ConfigServer _configServer
    )
    {
        this._logger = _logger;
        this._randomUtil = _randomUtil;
        this._databaseService = _databaseService;
        this._configServer = _configServer;
        _pmcConfig = _configServer.GetConfig<PmcConfig>();
    }

    /// <summary>
    ///     Add a pmc wave to a map
    /// </summary>
    /// <param name="locationId"> e.g. factory4_day, bigmap </param>
    /// <param name="waveToAdd"> Boss wave to add to map </param>
    public void AddPmcWaveToLocation(string locationId, BossLocationSpawn waveToAdd)
    {
        _pmcConfig.CustomPmcWaves[locationId].Add(waveToAdd);
    }

    /// <summary>
    ///     Add custom boss and normal waves to all maps found in config/location.json to db
    /// </summary>
    public void ApplyWaveChangesToAllMaps()
    {
        foreach (var location in _pmcConfig.CustomPmcWaves)
        {
            ApplyWaveChangesToMapByName(location.Key);
        }
    }

    /// <summary>
    ///     Add custom boss and normal waves to a map found in config/location.json to db by name
    /// </summary>
    /// <param name="name"> e.g. factory4_day, bigmap </param>
    public void ApplyWaveChangesToMapByName(string name)
    {
        if (!_pmcConfig.CustomPmcWaves.TryGetValue(name, out var pmcWavesToAdd))
        {
            return;
        }

        var location = _databaseService.GetLocation(name);
        if (location is null)
        {
            return;
        }

        location.Base.BossLocationSpawn.AddRange(pmcWavesToAdd);
    }

    /// <summary>
    ///     Add custom boss and normal waves to a map found in config/location.json to db by LocationBase
    /// </summary>
    /// <param name="location"> Location Object </param>
    public void ApplyWaveChangesToMap(LocationBase location)
    {
        if (!_pmcConfig.CustomPmcWaves.TryGetValue(location.Id.ToLower(), out var pmcWavesToAdd))
        {
            return;
        }

        location.BossLocationSpawn.AddRange(pmcWavesToAdd);
    }
}
