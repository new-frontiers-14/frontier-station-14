using Content.Server._Park.Species.Shadowkin.Components;
using Content.Server._Park.Species.Shadowkin.Events;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cuffs.Components;
using Content.Shared._Park.Species.Shadowkin.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Park.Species.Shadowkin.Systems;

public sealed class ShadowkinRestSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ShadowkinPowerSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowkinRestPowerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ShadowkinRestPowerComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ShadowkinRestPowerComponent, ShadowkinRestEvent>(Rest);
    }


    private void OnStartup(EntityUid uid, ShadowkinRestPowerComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.RestActionEntity, component.RestAction);
        // _actions.AddAction(uid, new InstantAction(_prototype.Index<InstantActionPrototype>("ShadowkinRest")), null);
    }

    private void OnShutdown(EntityUid uid, ShadowkinRestPowerComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.RestActionEntity);
        // _actions.RemoveAction(uid, new InstantAction(_prototype.Index<InstantActionPrototype>("ShadowkinRest")));
    }

    private void Rest(EntityUid uid, ShadowkinRestPowerComponent component, ShadowkinRestEvent args)
    {
        // Need power to modify power
        if (!_entity.HasComponent<ShadowkinComponent>(args.Performer))
            return;

        // Rest is a funny ability, keep it :)
        // // Don't activate abilities if handcuffed
        if (_entity.HasComponent<HandcuffComponent>(args.Performer))
            return;


        // Now doing what you weren't before
        component.IsResting = !component.IsResting;

        // Resting
        if (component.IsResting)
        {
            // Sleepy time
            _entity.EnsureComponent<ForcedSleepingComponent>(args.Performer);
            // No waking up normally (it would do nothing)
            // _actions.RemoveAction(uid, component.RestActionEntity);
            // _actions.RemoveAction(args.Performer, new InstantAction(_prototype.Index<InstantActionPrototype>("Wake")));
            _power.TryAddMultiplier(args.Performer, 1.5f);
            // No action cooldown
            args.Handled = false;
        }
        // Waking
        else
        {
            // Wake up
            _entity.RemoveComponent<ForcedSleepingComponent>(args.Performer);
            _entity.RemoveComponent<SleepingComponent>(args.Performer);
            _power.TryAddMultiplier(args.Performer, -1.5f);
            // Action cooldown
            args.Handled = true;
        }
    }
}
