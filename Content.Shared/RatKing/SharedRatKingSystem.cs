using Content.Shared.Abilities;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.RatKing;

public abstract class SharedRatKingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!; // Used for rummage cooldown
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RatKingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RatKingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RatKingComponent, RatKingOrderActionEvent>(OnOrderAction);

        SubscribeLocalEvent<RatKingServantComponent, ComponentShutdown>(OnServantShutdown);

        SubscribeLocalEvent<RatKingRummageableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<RatKingRummageableComponent, RatKingRummageDoAfterEvent>(OnDoAfterComplete);

        SubscribeLocalEvent<RatKingRummageableComponent, ComponentInit>(OnComponentInit); // Goobstation - #660
        SubscribeLocalEvent<RummagerComponent, ComponentInit>(OnRummgerComponentInit); // Frontier
    }

    private void OnStartup(EntityUid uid, RatKingComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        _action.AddAction(uid, ref component.ActionRaiseArmyEntity, component.ActionRaiseArmy, component: comp);
        _action.AddAction(uid, ref component.ActionDomainEntity, component.ActionDomain, component: comp);
        _action.AddAction(uid, ref component.ActionOrderStayEntity, component.ActionOrderStay, component: comp);
        _action.AddAction(uid, ref component.ActionOrderFollowEntity, component.ActionOrderFollow, component: comp);
        _action.AddAction(uid, ref component.ActionOrderCheeseEmEntity, component.ActionOrderCheeseEm, component: comp);
        _action.AddAction(uid, ref component.ActionOrderLooseEntity, component.ActionOrderLoose, component: comp);

        UpdateActions(uid, component);
    }

    private void OnShutdown(EntityUid uid, RatKingComponent component, ComponentShutdown args)
    {
        foreach (var servant in component.Servants)
        {
            if (TryComp(servant, out RatKingServantComponent? servantComp))
                servantComp.King = null;
        }

        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        _action.RemoveAction(uid, component.ActionRaiseArmyEntity, comp);
        _action.RemoveAction(uid, component.ActionDomainEntity, comp);
        _action.RemoveAction(uid, component.ActionOrderStayEntity, comp);
        _action.RemoveAction(uid, component.ActionOrderFollowEntity, comp);
        _action.RemoveAction(uid, component.ActionOrderCheeseEmEntity, comp);
        _action.RemoveAction(uid, component.ActionOrderLooseEntity, comp);
    }

    private void OnOrderAction(EntityUid uid, RatKingComponent component, RatKingOrderActionEvent args)
    {
        if (component.CurrentOrder == args.Type)
            return;
        args.Handled = true;

        component.CurrentOrder = args.Type;
        Dirty(uid, component);

        DoCommandCallout(uid, component);
        UpdateActions(uid, component);
        UpdateAllServants(uid, component);
    }

    private void OnServantShutdown(EntityUid uid, RatKingServantComponent component, ComponentShutdown args)
    {
        if (TryComp(component.King, out RatKingComponent? ratKingComponent))
            ratKingComponent.Servants.Remove(uid);
    }

    private void UpdateActions(EntityUid uid, RatKingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _action.SetToggled(component.ActionOrderStayEntity, component.CurrentOrder == RatKingOrderType.Stay);
        _action.SetToggled(component.ActionOrderFollowEntity, component.CurrentOrder == RatKingOrderType.Follow);
        _action.SetToggled(component.ActionOrderCheeseEmEntity, component.CurrentOrder == RatKingOrderType.CheeseEm);
        _action.SetToggled(component.ActionOrderLooseEntity, component.CurrentOrder == RatKingOrderType.Loose);
        _action.StartUseDelay(component.ActionOrderStayEntity);
        _action.StartUseDelay(component.ActionOrderFollowEntity);
        _action.StartUseDelay(component.ActionOrderCheeseEmEntity);
        _action.StartUseDelay(component.ActionOrderLooseEntity);
    }

    public void OnComponentInit(EntityUid uid, RatKingRummageableComponent component, ComponentInit args) // Goobstation - #660 Disposal unit rummage cooldown now start on spawn to prevent rummage abuse.
    {
        component.LastLooted = _gameTiming.CurTime;
        Dirty(uid, component);
    }

    public void OnRummgerComponentInit(EntityUid uid, RummagerComponent component, ComponentInit args) // Frontier - per-rummager cooldown
    {
        component.LastRummaged = _gameTiming.CurTime;
        Dirty(uid, component);
    }

    private void OnGetVerb(EntityUid uid, RatKingRummageableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<RummagerComponent>(args.User, out var rummager)
            || component.Looted
            || _gameTiming.CurTime < component.LastLooted + component.RummageCooldown
            || _gameTiming.CurTime < rummager.LastRummaged + rummager.Cooldown) // Frontier: cooldown per rummager
            // DeltaV - Use RummagerComponent instead of RatKingComponent
            // (This is so we can give Rodentia rummage abilities)
            // Additionally, adds a cooldown check
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rat-king-rummage-text"),
            Priority = 0,
            Act = () =>
            {
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.RummageDuration,
                    new RatKingRummageDoAfterEvent(), uid, uid)
                {
                    BlockDuplicate = true,
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    DistanceThreshold = 2f
                });
            }
        });
    }

    private void OnDoAfterComplete(EntityUid uid, RatKingRummageableComponent component, RatKingRummageDoAfterEvent args)
    {
        // DeltaV - Rummaging an object updates the looting cooldown rather than a "previously looted" check.
        // Note that the "Looted" boolean can still be checked (by mappers/admins) 
        // to disable rummaging on the object indefinitely, but rummaging will no
        // longer permanently prevent future rummaging.
        var time = _gameTiming.CurTime;
        if (args.Cancelled
            || component.Looted
            || time < component.LastLooted + component.RummageCooldown
            || !TryComp<RummagerComponent>(args.User, out var rummager) // Frontier: must be a rummager (also, verify cooldowns)
            || time < rummager.LastRummaged + rummager.Cooldown) // Frontier: check cooldown
            return;

        component.LastLooted = time;
        // End DeltaV change
        rummager.LastRummaged = time; // Frontier: set rummager cooldown

        Dirty(uid, component);
        _audio.PlayPredicted(component.Sound, uid, args.User);

        var spawn = PrototypeManager.Index<WeightedRandomEntityPrototype>(component.RummageLoot).Pick(Random);
        if (_net.IsServer)
            Spawn(spawn, Transform(uid).Coordinates);
    }

    public void UpdateAllServants(EntityUid uid, RatKingComponent component)
    {
        foreach (var servant in component.Servants)
        {
            UpdateServantNpc(servant, component.CurrentOrder);
        }
    }

    public virtual void UpdateServantNpc(EntityUid uid, RatKingOrderType orderType)
    {

    }

    public virtual void DoCommandCallout(EntityUid uid, RatKingComponent component)
    {

    }
}

[Serializable, NetSerializable]
public sealed partial class RatKingRummageDoAfterEvent : SimpleDoAfterEvent
{

}
