﻿using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace SPTarkov.Server.Core.Callbacks;

[Injectable]
public class AchievementCallbacks(
    AchievementController _achievementController,
    HttpResponseUtil _httpResponseUtil
)
{
    /// <summary>
    ///     Handle client/achievement/list
    /// </summary>
    /// <returns></returns>
    public string GetAchievements(string url, EmptyRequestData _, string sessionID)
    {
        return _httpResponseUtil.GetBody(_achievementController.GetAchievements(sessionID));
    }

    /// <summary>
    ///     Handle client/achievement/statistic
    /// </summary>
    /// <returns></returns>
    public string Statistic(string url, EmptyRequestData _, string sessionID)
    {
        return _httpResponseUtil.GetBody(_achievementController.GetAchievementStatics(sessionID));
    }
}
