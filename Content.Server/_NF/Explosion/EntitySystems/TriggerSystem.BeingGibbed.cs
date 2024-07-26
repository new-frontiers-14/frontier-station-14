using Content.Server.Explosion.Components;
using Content.Shared.Implants;
using Content.Server.Body.Components;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    private void InitializeBeingGibbed()
    {
        SubscribeLocalEvent<TriggerOnBeingGibbedComponent, BeforeGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<TriggerOnBeingGibbedComponent, ImplantRelayEvent<BeforeGibbedEvent>>(OnBeingGibbedRelay);
    }

    private void OnBeingGibbed(EntityUid uid, TriggerOnBeingGibbedComponent component, BeforeGibbedEvent args)
    {
        Trigger(uid);
    }

    private void OnBeingGibbedRelay(EntityUid uid, TriggerOnBeingGibbedComponent component, ImplantRelayEvent<BeforeGibbedEvent> args)
    {
        Trigger(uid);
    }
}
