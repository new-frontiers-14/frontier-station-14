using Content.Server.Explosion.Components;
using Content.Shared.Implants;
using Content.Server.Body.Components;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    private void InitializeBeingGibbed()
    {
        SubscribeLocalEvent<TriggerOnBeingGibbedComponent, ImplantRelayEvent<BeingGibbedEvent>>(OnBeingGibbedRelay);
    }

    private void OnBeingGibbedRelay(EntityUid uid, TriggerOnBeingGibbedComponent component, ImplantRelayEvent<BeingGibbedEvent> args)
    {
        Trigger(uid);
    }
}
