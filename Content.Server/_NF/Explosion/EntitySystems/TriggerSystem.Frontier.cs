using Content.Server._NF.Explosion.Components;
using Content.Shared.Implants;
using Content.Server.Body.Components;
using Content.Shared._NF.Interaction.Events;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    private void NFInitialize()
    {
        SubscribeLocalEvent<TriggerOnBeingGibbedComponent, BeforeGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<TriggerOnBeingGibbedComponent, ImplantRelayEvent<BeforeGibbedEvent>>(OnBeingGibbedRelay);
        SubscribeLocalEvent<TriggerOnInteractionPopupUseComponent, InteractionPopupOnUseFailureEvent>(OnPopupInteractionFailure);
        SubscribeLocalEvent<TriggerOnInteractionPopupUseComponent, InteractionPopupOnUseSuccessEvent>(OnPopupInteractionSuccess);
    }

    private void OnBeingGibbed(EntityUid uid, TriggerOnBeingGibbedComponent component, BeforeGibbedEvent args)
    {
        Trigger(uid);
    }

    private void OnBeingGibbedRelay(EntityUid uid, TriggerOnBeingGibbedComponent component, ImplantRelayEvent<BeforeGibbedEvent> args)
    {
        Trigger(uid);
    }

    private void OnPopupInteractionFailure(EntityUid uid, TriggerOnInteractionPopupUseComponent component, InteractionPopupOnUseFailureEvent args)
    {
        if (component.TriggerOnFailure)
            Trigger(uid);
    }

    private void OnPopupInteractionSuccess(EntityUid uid, TriggerOnInteractionPopupUseComponent component, InteractionPopupOnUseSuccessEvent args)
    {
        if (component.TriggerOnSuccess)
            Trigger(uid);
    }
}
