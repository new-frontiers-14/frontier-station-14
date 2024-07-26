using Content.Server.Explosion.Components;
using Content.Shared.Implants;
using Content.Server.Body.Components;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    private void InitializeBeingGibbed()
    {
        SubscribeLocalEvent<TriggerOnBeingGibbedComponent, ImplantRelayEvent<BeforeGibbedEvent>>(OnBeingGibbedRelay);
    }

    private void OnBeingGibbedRelay(EntityUid uid, TriggerOnBeingGibbedComponent component, ImplantRelayEvent<BeforeGibbedEvent> args)
    {
        Trigger(uid);
    }
}
