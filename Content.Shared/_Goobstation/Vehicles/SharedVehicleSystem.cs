using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes; // Frontier
using Content.Shared.Light.Components; // Frontier
using Content.Shared.Light.EntitySystems; // Frontier
using Content.Shared.Movement.Pulling.Components; // Frontier
using Content.Shared.Popups; // Frontier
using Robust.Shared.Network; // Frontier
using Content.Shared._NF.Vehicle.Components; // Frontier
using Content.Shared.Movement.Pulling.Events; // Frontier
using Robust.Shared.Timing; // Frontier

namespace Content.Shared._Goobstation.Vehicles; // Frontier: migrate under _Goobstation

public abstract partial class SharedVehicleSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly INetManager _net = default!; // Frontier
    [Dependency] private readonly UnpoweredFlashlightSystem _flashlight = default!; // Frontier
    [Dependency] private readonly SharedPopupSystem _popup = default!; // Frontier
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!; // Frontier
    [Dependency] private readonly IGameTiming _timing = default!; // Frontier

    public static readonly EntProtoId HornActionId = "ActionHorn";
    public static readonly EntProtoId SirenActionId = "ActionSiren";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VehicleComponent, MapInitEvent>(OnMapInit); // Frontier
        SubscribeLocalEvent<VehicleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<VehicleComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<VehicleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<VehicleComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<VehicleComponent, VirtualItemDeletedEvent>(OnDropped);

        SubscribeLocalEvent<VehicleComponent, EntInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<VehicleComponent, EntRemovedFromContainerMessage>(OnEject);

        SubscribeLocalEvent<VehicleComponent, HornActionEvent>(OnHorn);
        SubscribeLocalEvent<VehicleComponent, SirenActionEvent>(OnSiren);

        SubscribeLocalEvent<VehicleRiderComponent, PullAttemptEvent>(OnRiderPull); // Frontier
    }

    private void OnInit(EntityUid uid, VehicleComponent component, ComponentInit args)
    {
        _appearance.SetData(uid, VehicleState.Animated, component.EngineRunning && component.Driver != null); // Frontier: add Driver != null
        _appearance.SetData(uid, VehicleState.DrawOver, false);
    }

    // Frontier
    private void OnMapInit(EntityUid uid, VehicleComponent component, MapInitEvent args)
    {
        bool actionsUpdated = false;
        if (component.HornSound != null)
        {
            _actionContainer.EnsureAction(uid, ref component.HornAction, HornActionId);
            actionsUpdated = true;
        }

        if (component.SirenSound != null)
        {
            _actionContainer.EnsureAction(uid, ref component.SirenAction, SirenActionId);
            actionsUpdated = true;
        }

        if (actionsUpdated)
            Dirty(uid, component);
    }
    // End Frontier

    private void OnRemove(EntityUid uid, VehicleComponent component, ComponentRemove args)
    {
        if (component.Driver == null)
            return;

        _buckle.TryUnbuckle(component.Driver.Value, component.Driver.Value);
        Dismount(component.Driver.Value, uid);
        _appearance.SetData(uid, VehicleState.DrawOver, false);
    }

    private void OnInsert(EntityUid uid, VehicleComponent component, ref EntInsertedIntoContainerMessage args)
    {
        if (HasComp<InstantActionComponent>(args.Entity))
            return;

        // Frontier: check key slot
        if (args.Container.ID != component.KeySlotId)
            return;
        if (!_timing.IsFirstTimePredicted)
            return;
        // End Frontier: check key slot

        component.EngineRunning = true;
        _appearance.SetData(uid, VehicleState.Animated, component.Driver != null);

        _ambientSound.SetAmbience(uid, true);

        if (component.Driver == null)
            return;

        Mount(component.Driver.Value, uid);
    }

    private void OnEject(EntityUid uid, VehicleComponent component, ref EntRemovedFromContainerMessage args)
    {
        // Frontier: check key slot
        if (args.Container.ID != component.KeySlotId)
            return;
        if (!_timing.IsFirstTimePredicted)
            return;
        // End Frontier: check key slot

        component.EngineRunning = false;
        _appearance.SetData(uid, VehicleState.Animated, false);

        _ambientSound.SetAmbience(uid, false);

        if (component.Driver == null)
            return;

        Dismount(component.Driver.Value, uid, removeDriver: false); // Frontier: add removeDriver: false - the driver is still around.
    }

    private void OnHorn(EntityUid uid, VehicleComponent component, InstantActionEvent args)
    {
        if (args.Handled == true || component.Driver != args.Performer || component.HornSound == null)
            return;

        _audio.PlayPredicted(component.HornSound, uid, args.Performer); // Frontier: PlayPvs<PlayPredicted, add args.Performer
        args.Handled = true;
    }

    private void OnSiren(EntityUid uid, VehicleComponent component, InstantActionEvent args)
    {
        if (_net.IsClient) // Frontier: _audio.Stop hates client-side entities, only create this serverside
            return; // Frontier

        if (args.Handled == true || component.Driver != args.Performer || component.SirenSound == null)
            return;

        if (component.SirenStream != null) // Frontier: SirenEnabled<SirenStream != null
        {
            component.SirenStream = _audio.Stop(component.SirenStream);
        }
        else
        {
            var sirenParams = component.SirenSound.Params.WithLoop(true); // Frontier: force loop
            component.SirenStream = _audio.PlayPvs(component.SirenSound, uid, audioParams: sirenParams)?.Entity; // Frontier: set params
        }

        // component.SirenEnabled = component.SirenStream != null; // Frontier: remove (unneeded state)
        args.Handled = true;
    }


    private void OnStrapAttempt(Entity<VehicleComponent> ent, ref StrapAttemptEvent args)
    {
        var driver = args.Buckle.Owner; // i dont want to re write this shit 100 fucking times

        if (ent.Comp.Driver != null)
        {
            args.Cancelled = true;
            return;
        }

        // Frontier: no pulling when riding
        if (TryComp<PullerComponent>(args.Buckle, out var puller) && puller.Pulling != null)
        {
            _popup.PopupPredicted(Loc.GetString("vehicle-cannot-pull", ("object", puller.Pulling), ("vehicle", ent)), ent, args.Buckle);
            args.Cancelled = true;
            return;
        }
        // End Frontier

        if (ent.Comp.RequiredHands != 0)
        {
            for (int hands = 0; hands < ent.Comp.RequiredHands; hands++)
            {
                if (!_virtualItem.TrySpawnVirtualItemInHand(ent.Owner, driver, false))
                {
                    args.Cancelled = true;
                    _virtualItem.DeleteInHandsMatching(driver, ent.Owner);
                    return;
                }
            }
        }

        // AddHorns(driver, ent); // Frontier: delay until mounted
    }

    protected virtual void OnStrapped(Entity<VehicleComponent> ent, ref StrappedEvent args) // Frontier: private<protected virtual
    {
        var driver = args.Buckle.Owner;

        if (!TryComp(driver, out MobMoverComponent? mover) || ent.Comp.Driver != null)
            return;

        ent.Comp.Driver = driver;
        Dirty(ent); // Frontier
        _appearance.SetData(ent.Owner, VehicleState.DrawOver, true);
        _appearance.SetData(ent.Owner, VehicleState.Animated, ent.Comp.EngineRunning); // Frontier
        var rider = EnsureComp<VehicleRiderComponent>(driver); // Frontier
        Dirty(driver, rider); // Frontier

        if (!ent.Comp.EngineRunning)
            return;

        Mount(driver, ent.Owner);
    }

    protected virtual void OnUnstrapped(Entity<VehicleComponent> ent, ref UnstrappedEvent args) // Frontier: private<protected virtual
    {
        if (ent.Comp.Driver != args.Buckle.Owner)
            return;

        Dismount(args.Buckle.Owner, ent);
        _appearance.SetData(ent.Owner, VehicleState.DrawOver, false);
        _appearance.SetData(ent.Owner, VehicleState.Animated, false); // Frontier
        RemComp<VehicleRiderComponent>(args.Buckle.Owner); // Frontier
    }

    private void OnDropped(EntityUid uid, VehicleComponent comp, VirtualItemDeletedEvent args)
    {
        if (comp.Driver != args.User)
            return;

        _buckle.TryUnbuckle(args.User, args.User);

        Dismount(args.User, uid);
        _appearance.SetData(uid, VehicleState.DrawOver, false);
        _appearance.SetData(uid, VehicleState.Animated, false); // Frontier
        RemComp<VehicleRiderComponent>(args.User); // Frontier
    }

    private void AddHorns(EntityUid driver, EntityUid vehicle)
    {
        if (!TryComp<VehicleComponent>(vehicle, out var vehicleComp))
            return;

        // Frontier: grant existing actions
        List<EntityUid> grantedActions = new();
        if (vehicleComp.HornAction != null)
            grantedActions.Add(vehicleComp.HornAction.Value);

        if (vehicleComp.SirenAction != null)
            grantedActions.Add(vehicleComp.SirenAction.Value);

        if (TryComp<UnpoweredFlashlightComponent>(vehicle, out var flashlight) && flashlight.ToggleActionEntity != null)
        {
            grantedActions.Add(flashlight.ToggleActionEntity.Value);
            _flashlight.SetLight((vehicle, flashlight), flashlight.LightOn, quiet: true);
        }
        // Only try to grant actions if the vehicle actually has them.
        if (grantedActions.Count > 0)
            _actions.GrantActions(driver, grantedActions, vehicle);
        // End Frontier
    }

    private void Mount(EntityUid driver, EntityUid vehicle)
    {
        if (TryComp<AccessComponent>(vehicle, out var accessComp))
        {
            var accessSources = _access.FindPotentialAccessItems(driver);
            var access = _access.FindAccessTags(driver, accessSources);

            foreach (var tag in access)
            {
                accessComp.Tags.Add(tag);
            }
        }

        _mover.SetRelay(driver, vehicle);

        AddHorns(driver, vehicle); // Frontier
    }

    private void Dismount(EntityUid driver, EntityUid vehicle, bool removeDriver = true) // Frontier: add removeDriver
    {
        if (!TryComp<VehicleComponent>(vehicle, out var vehicleComp) || vehicleComp.Driver != driver)
            return;

        RemComp<RelayInputMoverComponent>(driver);

        if (removeDriver) // Frontier
            vehicleComp.Driver = null;

        _actions.RemoveProvidedActions(driver, vehicle); // Frontier: don't remove actions, just provide/revoke them

        if (removeDriver) // Frontier
            _virtualItem.DeleteInHandsMatching(driver, vehicle);

        if (TryComp<AccessComponent>(vehicle, out var accessComp))
            accessComp.Tags.Clear();
    }

    // Frontier: prevent drivers from pulling things
    private void OnRiderPull(Entity<VehicleRiderComponent> ent, ref PullAttemptEvent args)
    {
        if (args.PullerUid == ent.Owner)
            args.Cancelled = true;
    }
    // End Frontier
}

public sealed partial class HornActionEvent : InstantActionEvent;

public sealed partial class SirenActionEvent : InstantActionEvent;
