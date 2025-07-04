using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Request;
using SPTarkov.Server.Core.Models.Eft.Health;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Enums;

namespace SPTarkov.Server.Core.Routers.ItemEvents;

[Injectable]
public class HealthItemEventRouter : ItemEventRouterDefinition
{
    protected HealthCallbacks _healthCallbacks;

    public HealthItemEventRouter(HealthCallbacks healthCallbacks)
    {
        _healthCallbacks = healthCallbacks;
    }

    protected override List<HandledRoute> GetHandledRoutes()
    {
        return
        [
            new HandledRoute(ItemEventActions.EAT, false),
            new HandledRoute(ItemEventActions.HEAL, false),
            new HandledRoute(ItemEventActions.RESTORE_HEALTH, false),
        ];
    }

    public override ValueTask<ItemEventRouterResponse> HandleItemEvent(
        string url,
        PmcData pmcData,
        BaseInteractionRequestData body,
        string sessionID,
        ItemEventRouterResponse output
    )
    {
        switch (url)
        {
            case ItemEventActions.EAT:
                return new ValueTask<ItemEventRouterResponse>(
                    _healthCallbacks.OffraidEat(pmcData, body as OffraidEatRequestData, sessionID)
                );
            case ItemEventActions.HEAL:
                return new ValueTask<ItemEventRouterResponse>(
                    _healthCallbacks.OffraidHeal(pmcData, body as OffraidHealRequestData, sessionID)
                );
            case ItemEventActions.RESTORE_HEALTH:
                return new ValueTask<ItemEventRouterResponse>(
                    _healthCallbacks.HealthTreatment(
                        pmcData,
                        body as HealthTreatmentRequestData,
                        sessionID
                    )
                );
            default:
                throw new Exception(
                    $"HealthItemEventRouter being used when it cant handle route {url}"
                );
        }
    }
}
