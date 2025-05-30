﻿using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace SPTarkov.Server.Core.Routers.Static;

[Injectable]
public class WeatherStaticRouter : StaticRouter
{
    public WeatherStaticRouter(
        JsonUtil jsonUtil,
        WeatherCallbacks weatherCallbacks
    ) : base(
        jsonUtil,
        [
            new RouteAction(
                "/client/weather",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => weatherCallbacks.GetWeather(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/localGame/weather",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => weatherCallbacks.GetLocalWeather(url, info as EmptyRequestData, sessionID)
            )
        ]
    )
    {
    }
}
