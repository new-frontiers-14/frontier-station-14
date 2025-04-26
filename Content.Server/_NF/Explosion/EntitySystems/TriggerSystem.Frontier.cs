using Content.Server._NF.Explosion.Components;
using Content.Shared.Implants;
using Content.Server.Body.Components;
using Content.Shared._NF.Interaction.Events;
using Content.Shared.Projectiles;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    private void NFInitialize()
    {
        SubscribeLocalEvent<TriggerOnBeingGibbedComponent, BeforeGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<TriggerOnBeingGibbedComponent, ImplantRelayEvent<BeforeGibbedEvent>>(OnBeingGibbedRelay);
        SubscribeLocalEvent<TriggerOnInteractionPopupUseComponent, InteractionPopupOnUseFailureEvent>(OnPopupInteractionFailure);
        SubscribeLocalEvent<TriggerOnInteractionPopupUseComponent, InteractionPopupOnUseSuccessEvent>(OnPopupInteractionSuccess);

        SubscribeLocalEvent<ReplaceOnTriggerComponent, TriggerEvent>(OnReplaceTrigger);
        SubscribeLocalEvent<TriggerOnProjectileHitComponent, ProjectileHitEvent>(OnProjectileHitEvent);
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

    private void OnReplaceTrigger(Entity<ReplaceOnTriggerComponent> ent, ref TriggerEvent args)
    {
        var xform = Transform(ent);

        if (_container.TryGetContainingContainer((ent, xform), out var container))
        {
            _container.Remove(ent.Owner, container, force: true);
            SpawnInContainerOrDrop(ent.Comp.Proto, container.Owner, container.ID);
        }
        else
        {
            Spawn(ent.Comp.Proto, xform.Coordinates);
        }
        QueueDel(ent);
    }

    private void OnProjectileHitEvent(EntityUid uid, TriggerOnProjectileHitComponent component, ref ProjectileHitEvent args)
    {
        Trigger(uid, args.Target);
    }
}
