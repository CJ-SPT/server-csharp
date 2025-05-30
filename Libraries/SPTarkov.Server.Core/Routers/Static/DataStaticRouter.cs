﻿using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace SPTarkov.Server.Core.Routers.Static;

[Injectable]
public class DataStaticRouter : StaticRouter
{
    public DataStaticRouter(
        JsonUtil jsonUtil,
        DataCallbacks dataCallbacks
    ) : base(
        jsonUtil,
        [
            new RouteAction(
                "/client/settings",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => dataCallbacks.GetSettings(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/globals",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => dataCallbacks.GetGlobals(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/items",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => dataCallbacks.GetTemplateItems(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/handbook/templates",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => dataCallbacks.GetTemplateHandbook(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/customization",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => dataCallbacks.GetTemplateSuits(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/account/customization",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => dataCallbacks.GetTemplateCharacter(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/hideout/production/recipes",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => dataCallbacks.GetHideoutProduction(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/hideout/settings",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => dataCallbacks.GetHideoutSettings(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/hideout/areas",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => dataCallbacks.GetHideoutAreas(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/languages",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => dataCallbacks.GetLocalesLanguages(url, info as EmptyRequestData, sessionID)
            ),
            new RouteAction(
                "/client/hideout/qte/list",
                (
                    url,
                    info,
                    sessionID,
                    output
                ) => dataCallbacks.GetQteList(url, info as EmptyRequestData, sessionID)
            )
        ]
    )
    {
    }
}
