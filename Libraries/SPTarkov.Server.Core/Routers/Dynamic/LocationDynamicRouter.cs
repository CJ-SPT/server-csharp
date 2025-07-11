﻿using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace SPTarkov.Server.Core.Routers.Dynamic;

[Injectable]
public class LocationDynamicRouter : DynamicRouter
{
    public LocationDynamicRouter(JsonUtil jsonUtil)
        : base(jsonUtil, []) { }

    public override string GetTopLevelRoute()
    {
        return "spt-loot";
    }
}
