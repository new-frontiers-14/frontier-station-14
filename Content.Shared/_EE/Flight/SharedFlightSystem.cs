using Content.Shared.Actions;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared._EE.Flight.Events;
using Content.Shared.Standing;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Zombies;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._EE.Flight;

public abstract class SharedFlightSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlightComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FlightComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FlightComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<FlightComponent, RefreshFrictionModifiersEvent>(OnRefreshFrictionModifiers);
        SubscribeLocalEvent<FlightComponent, RefreshWeightlessModifiersEvent>(OnRefreshWeightlessModifiers);

        SubscribeLocalEvent<FlightComponent, ToggleFlightEvent>(OnToggleFlight);
        SubscribeLocalEvent<FlightComponent, FlightDoAfterEvent>(OnFlightDoAfter);
        SubscribeLocalEvent<FlightComponent, MobStateChangedEvent>(OnMobStateChangedEvent);
        SubscribeLocalEvent<FlightComponent, EntityZombifiedEvent>(OnZombified);
        SubscribeLocalEvent<FlightComponent, KnockedDownEvent>(OnKnockedDown);
        SubscribeLocalEvent<FlightComponent, StunnedEvent>(OnStunned);
        SubscribeLocalEvent<FlightComponent, SleepStateChangedEvent>(OnSleep);
        SubscribeLocalEvent<FlightComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FlightComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.IsCurrentlyFlying)
                continue;

            component.TimeUntilFlap -= frameTime;

            if (component.TimeUntilFlap > 0f)
                continue;

            _audio.PlayPredicted(component.FlapSound, uid, uid);
            component.TimeUntilFlap = component.FlapInterval;

        }
    }
    #region Query Functions

    public bool IsFlying(Entity<FlightComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        return entity.Comp.IsCurrentlyFlying;
    }

    #endregion


    #region Core Functions

    public void ToggleActive(Entity<FlightComponent> ent, bool active)
    {
        ent.Comp.IsCurrentlyFlying = active;
        ent.Comp.TimeUntilFlap = 0f;
        _actionsSystem.SetToggled(ent.Comp.ToggleActionEntity, ent.Comp.IsCurrentlyFlying);
        RaiseNetworkEvent(new FlightEvent(GetNetEntity(ent), ent.Comp.IsCurrentlyFlying, ent.Comp.IsAnimated));
        UpdateHands(ent, active);
        _stamina.TryTakeStamina(ent.Owner, ent.Comp.InitialStaminaCost, visual: false);
        _stamina.ToggleStaminaDrain(ent, ent.Comp.StaminaDrainRate, active, false);

        _gravity.RefreshWeightless(ent.Owner, active);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
        _movementSpeed.RefreshFrictionModifiers(ent);
        _movementSpeed.RefreshWeightlessModifiers(ent);

        Dirty(ent, ent.Comp);
    }

    private bool CanFly(EntityUid uid, FlightComponent component)
    {
        if (TryComp<StandingStateComponent>(uid, out var standing) && _standing.IsDown((uid, standing)))
        {
            _popupSystem.PopupClient(Loc.GetString("no-flight-while-down"), uid, uid, PopupType.Small);
            return false;
        }

        if (TryComp<CuffableComponent>(uid, out var cuffableComp) && !cuffableComp.CanStillInteract)
        {
            _popupSystem.PopupClient(Loc.GetString("no-flight-while-restrained"), uid, uid, PopupType.Small);
            return false;
        }

        if (HasComp<ZombieComponent>(uid))
        {
            _popupSystem.PopupClient(Loc.GetString("no-flight-while-zombified"), uid, uid, PopupType.Small);
            return false;
        }

        // Got to have stamina to fly
        if (!TryComp<StaminaComponent>(uid, out var stam))
            return false;

        var hasEnoughStamina = stam.StaminaDamage + component.InitialStaminaCost < stam.CritThreshold || stam.Critical;
        if (!hasEnoughStamina)
        {
            _popupSystem.PopupClient(Loc.GetString("no-flight-exhausted"), uid, uid, PopupType.MediumCaution);
            return false;
        }

        // All preflight checks complete, ready for take-off!
        return true;
    }

    private void UpdateHands(EntityUid uid, bool flying)
    {
        if (!TryComp<HandsComponent>(uid, out var handsComponent))
            return;

        if (flying)
            BlockHands(uid, handsComponent);
        else
            FreeHands(uid);
    }

    private void BlockHands(EntityUid uid, HandsComponent handsComponent)
    {
        var freeHands = 0;
        foreach (var hand in _hands.EnumerateHands((uid, handsComponent)))
        {
            if (!_hands.TryGetHeldItem((uid, handsComponent), hand, out var heldItem))
            {
                freeHands++;
                continue;
            }

            // Is this entity removable? (they might have handcuffs on)
            if (HasComp<UnremoveableComponent>(heldItem) && heldItem != uid)
                continue;

            if (_hands.TryDrop((uid, handsComponent), hand))
            {
                freeHands++;
            }

            if (freeHands == 2)
                break;
        }
        if (_virtualItem.TrySpawnVirtualItemInHand(uid, uid, out var virtItem1))
            EnsureComp<UnremoveableComponent>(virtItem1.Value);

        if (_virtualItem.TrySpawnVirtualItemInHand(uid, uid, out var virtItem2))
            EnsureComp<UnremoveableComponent>(virtItem2.Value);
    }

    private void FreeHands(EntityUid uid)
    {
        _virtualItem.DeleteInHandsMatching(uid, uid);
    }

    #endregion

    #region Events
    private void OnStartup(EntityUid uid, FlightComponent component, ComponentStartup args)
    {
        _actionsSystem.AddAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
    }

    private void OnShutdown(EntityUid uid, FlightComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ToggleActionEntity);
    }
    private void OnRefreshMoveSpeed(EntityUid uid, FlightComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.IsCurrentlyFlying) // If we're not flying, don't apply flying's modifier
            return;

        args.ModifySpeed(component.SpeedModifier, component.SpeedModifier);
    }

    // DeltaV - Since we use the new movement system and EE doesn't, we got to also apply friction modifiers.
    private void OnRefreshFrictionModifiers(Entity<FlightComponent> ent, ref RefreshFrictionModifiersEvent args)
    {
        if (!ent.Comp.IsCurrentlyFlying) // If we're not flying, don't apply flying's modifier
            return;

        args.ModifyFriction(ent.Comp.FrictionModifier, ent.Comp.FrictionModifier);
        args.ModifyAcceleration(ent.Comp.AccelerationModifer);
    }

    private void OnRefreshWeightlessModifiers(Entity<FlightComponent> ent, ref RefreshWeightlessModifiersEvent args)
    {
        if (!ent.Comp.IsCurrentlyFlying) // If we're not flying, don't apply flying's modifier
            return;

        //args.ModifyFriction(ent.Comp.FrictionModifier, ent.Comp.FrictionModifier);
        args.ModifyAcceleration(ent.Comp.AccelerationModifer);
    }

    private void OnToggleFlight(EntityUid uid, FlightComponent component, ToggleFlightEvent args)
    {
        // If the user isnt flying, we check for conditionals and initiate a doafter.
        if (!component.IsCurrentlyFlying)
        {
            if (!CanFly(uid, component))
                return;

            var doAfterArgs = new DoAfterArgs(EntityManager,
            uid, component.ActivationDelay,
            new FlightDoAfterEvent(), uid, target: uid)
            {
                BlockDuplicate = true,
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

            if (!_doAfter.TryStartDoAfter(doAfterArgs))
                return;
        }
        else
            ToggleActive((uid, component), false);
    }

    private void OnFlightDoAfter(EntityUid uid, FlightComponent component, FlightDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        ToggleActive((uid, component), true);
        args.Handled = true;
    }

    private void OnMobStateChangedEvent(EntityUid uid, FlightComponent component, MobStateChangedEvent args)
    {
        if (!component.IsCurrentlyFlying || args.NewMobState is MobState.Critical or MobState.Dead)
            return;

        ToggleActive((args.Target, component), false);
    }

    private void OnZombified(EntityUid uid, FlightComponent component, ref EntityZombifiedEvent args)
    {
        if (!component.IsCurrentlyFlying)
            return;

        ToggleActive((args.Target, component), false);

        if (!TryComp<StaminaComponent>(uid, out var stamina))
            return;

        Dirty(uid, stamina);
    }

    private void OnKnockedDown(EntityUid uid, FlightComponent component, ref KnockedDownEvent args)
    {
        if (!component.IsCurrentlyFlying)
            return;

        ToggleActive((uid, component), false);
    }

    private void OnStunned(EntityUid uid, FlightComponent component, ref StunnedEvent args)
    {
        if (!component.IsCurrentlyFlying)
            return;

        ToggleActive((uid, component), false);
    }

    private void OnSleep(EntityUid uid, FlightComponent component, ref SleepStateChangedEvent args)
    {
        if (!component.IsCurrentlyFlying || !args.FellAsleep)
            return;

        ToggleActive((uid, component), false);
        if (!TryComp<StaminaComponent>(uid, out var stamina))
            return;

        Dirty(uid, stamina);
    }
    private void OnStepTriggerAttempt(Entity<FlightComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (ent.Comp.IsCurrentlyFlying)
            args.Cancelled = true;
    }

    #endregion
}
public sealed partial class ToggleFlightEvent : InstantActionEvent { }