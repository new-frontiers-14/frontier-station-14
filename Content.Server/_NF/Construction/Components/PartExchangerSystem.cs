using Content.Server._NF.Construction.Components;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Stack;
using Content.Server.Storage.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Construction.Components;
using Content.Shared.Exchanger;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Utility;
using Content.Shared.Wires;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Verbs;
using Content.Shared._NF.Vehicle.Components;
using Content.Server._NF.Vehicle.EntitySystems;

namespace Content.Server._NF.Construction;

public sealed class PartExchangerSystem : EntitySystem
{
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StorageSystem _storage = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly VehicleUpgradeSystem _upgradableVehicle = default!;

    private const string UpgradeIconPath = "/Textures/Interface/VerbIcons/pickup.svg.192dpi.png";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PartExchangerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PartExchangerComponent, ExchangerDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<VehicleUpgradeComponent, GetVerbsEvent<InteractionVerb>>(OnGetVehicleInteractionVerbs);
    }

    private struct UpgradePartState
    {
        public MachinePartComponent Part;
        public StackComponent? Stack;
        public bool InExchanger;
    }

    private struct UpgradeStepCallbacks
    {
        public Action<UpgradePartState>? OnPartRemovedFromTarget;
        public Action<MachinePartState>? OnPartInsertedIntoTarget;
    }

    #region Event handlers

    private void OnAfterInteract(EntityUid uid, PartExchangerComponent component, AfterInteractEvent args)
    {
        if (component.DoDistanceCheck && !args.CanReach)
            return;

        if (args.Target == null)
            return;

        TryStartDoAfter((uid, component), args.Target.Value, args.User);
    }

    private void OnDoAfter(EntityUid uid, PartExchangerComponent exchanger, DoAfterEvent args)
    {
        if (args.Cancelled)
        {
            exchanger.AudioStream = _audio.Stop(exchanger.AudioStream);
            return;
        }

        if (args.Handled || args.Args.Target is not { } target)
            return;

        if (!TryComp<StorageComponent>(uid, out var storage) || storage.Container == null)
            return;

        // Exchange machine parts with the target.
        Entity<PartExchangerComponent, StorageComponent> exchangerEnt = (uid, exchanger, storage);
        if (TryComp<MachineComponent>(target, out var machine))
            TryExchangeMachineParts((target, machine), exchangerEnt);
        else if (TryComp<MachineFrameComponent>(target, out var machineFrame))
            TryConstructMachineParts((target, machineFrame), exchangerEnt);
        else if (TryComp<VehicleUpgradeComponent>(target, out var vehicle))
            TryUpgradeVehicle((target, vehicle), exchangerEnt);

        args.Handled = true;
    }

    private void OnGetVehicleInteractionVerbs(Entity<VehicleUpgradeComponent> vehicle, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<PartExchangerComponent>(args.Using, out var exchanger))
            return;

        // The user is holding a PartExchanger and is targetting a VehicleUpgrade.
        var exchangerUid = args.Using.Value;
        var user = args.User;
        InteractionVerb verb = new()
        {
            Act = () =>
            {
                TryStartDoAfter((exchangerUid, exchanger), vehicle, user);
            },
            Icon = new SpriteSpecifier.Texture(new(UpgradeIconPath)),
            Text = exchanger.PreferHigherRating
                ? Loc.GetString("vehicle-verb-upgrade")
                : Loc.GetString("vehicle-verb-downgrade")
        };
        args.Verbs.Add(verb);
    }

    #endregion

    #region Upgrading

    private void TryExchangeMachineParts(Entity<MachineComponent> target, Entity<PartExchangerComponent, StorageComponent> exchanger)
    {
        // Get the container machine board so we can figure out what parts are needed
        var board = target.Comp.BoardContainer.ContainedEntities.FirstOrNull();
        if (board == null || !TryComp<MachineBoardComponent>(board, out var macBoardComp))
            return;

        PerformUpgrade(target,
            target.Comp.PartContainer,
            macBoardComp.Requirements,
            exchanger,
            callbacks: default);
        _construction.RefreshParts(target);
    }

    private void TryConstructMachineParts(Entity<MachineFrameComponent> target, Entity<PartExchangerComponent, StorageComponent> exchanger)
    {
        // Get the container machine board so we can figure out what parts are needed
        var board = target.Comp.BoardContainer.ContainedEntities.FirstOrNull();
        if (board == null || !TryComp<MachineBoardComponent>(board, out var macBoardComp))
            return;

        UpgradeStepCallbacks callbacks;
        callbacks.OnPartRemovedFromTarget = upgrade =>
        {
            // Make sure the construction status is consistent with the removed parts.
            var partType = upgrade.Part.PartType;
            var progress = target.Comp.Progress[partType];
            target.Comp.Progress[partType] = int.Max(0, progress - (upgrade.Stack?.Count ?? 1));
        };
        callbacks.OnPartInsertedIntoTarget = part =>
        {
            // Same here, make sure construction status is consistent with the inserted parts.
            target.Comp.Progress[part.Part.PartType] += part.Quantity();
        };
        PerformUpgrade(target,
            target.Comp.PartContainer,
            macBoardComp.Requirements,
            exchanger,
            callbacks);
    }

    private void TryUpgradeVehicle(Entity<VehicleUpgradeComponent> target, Entity<PartExchangerComponent, StorageComponent> exchanger)
    {
        PerformUpgrade(target,
            target.Comp.PartContainer,
            target.Comp.Requirements,
            exchanger,
            callbacks: default);
        _upgradableVehicle.UpdateParts(target);
    }

    private void PerformUpgrade(EntityUid targetUid,
        Container targetPartContainer,
        Dictionary<ProtoId<MachinePartPrototype>, int> targetPartRequirements,
        Entity<PartExchangerComponent, StorageComponent> exchanger,
        UpgradeStepCallbacks callbacks)
    {
        // High-level summary of steps taken in this method:
        // 1. collect *all* available parts, from the exchanger and the target.
        // 2. sort parts by rating (either descending or ascending, depending on the exchanger)
        // 3. pick the best parts that match the target's requirements.
        // 4. put the remaining parts in the exchanger.

        // Collect all the parts in the exchanger.
        // Note: these parts remain in the exchanger.
        var partsByType = new Dictionary<ProtoId<MachinePartPrototype>, List<(EntityUid, UpgradePartState)>>();
        foreach (var (item, upgrade) in ContainedMachineParts(exchanger.Comp2.Container, inExchanger: true))
        {
            partsByType.GetOrNew(upgrade.Part.PartType).Add((item, upgrade));
        }

        // Add all components in the machine to form a complete set of available components.
        foreach (var (item, upgrade) in ContainedMachineParts(targetPartContainer, inExchanger: false))
        {
            var partType = upgrade.Part.PartType;
            partsByType.GetOrNew(partType).Add((item, upgrade));
            callbacks.OnPartRemovedFromTarget?.Invoke(upgrade);
            _container.RemoveEntity(targetUid, item);
        }

        // Sort the preferred parts first so they are selected for the machine frame.
        Comparison<(EntityUid part, UpgradePartState state)> comparison = exchanger.Comp1.PreferHigherRating
            ? (x, y) => y.state.Part.Rating.CompareTo(x.state.Part.Rating)
            : (x, y) => x.state.Part.Rating.CompareTo(y.state.Part.Rating);
        foreach (var partList in partsByType.Values)
        {
            partList.Sort(comparison);
        }

        // Keep track of which parts have been taken.
        var takenParts = new List<(EntityUid id, MachinePartState state, int index)>();
        foreach (var (type, amount) in targetPartRequirements)
        {
            if (!partsByType.TryGetValue(type, out var partsOfType))
                // We don't have any parts of this type.
                continue;

            var partsNeeded = amount;
            var index = 0;
            foreach (var (part, state) in partsOfType)
            {
                // No more space for components
                if (partsNeeded <= 0)
                    break;

                if (state.Stack is not null)
                {
                    var count = state.Stack.Count;
                    // Entire stack is needed, add it to the things to bring over.
                    if (count <= partsNeeded)
                    {
                        MachinePartState partState;
                        partState.Part = state.Part;
                        partState.Stack = state.Stack;

                        takenParts.Add((part, partState, index));
                        partsNeeded -= count;
                    }
                    else
                    {
                        // Partial stack is needed, split off what we need, ensure the new entry is moved.
                        var splitStack = _stack.Split(part, partsNeeded, Transform(targetUid).Coordinates, state.Stack) ?? EntityUid.Invalid;

                        if (splitStack == EntityUid.Invalid)
                            continue;

                        // Create a new MachinePartState out of our new entity
                        if (_construction.GetMachinePartState(splitStack, out var splitState))
                        {
                            // New entity, nothing to remove, set index to -1 to flag this.
                            takenParts.Add((splitStack, splitState, -1));
                            partsNeeded = 0;
                        }
                    }
                }
                else
                {
                    // Not a stack, move the single part.
                    MachinePartState partState;
                    partState.Part = state.Part;
                    partState.Stack = state.Stack;

                    takenParts.Add((part, partState, index));
                    partsNeeded--;
                }
                // Adjust the index for parts being removed from the container.
                index++;
            }
        }

        // Move selected parts to the machine, removing them from the dictionary of contained parts.
        // Iterate through list backwards, remove later entries first (maintain validity of earlier indices).
        for (int i = takenParts.Count - 1; i >= 0; i--)
        {
            var (id, state, index) = takenParts[i];
            _container.Insert(id, targetPartContainer, force: true);
            if (index >= 0)
                partsByType[state.Part.PartType].RemoveAt(index);
            callbacks.OnPartInsertedIntoTarget?.Invoke(state);
        }

        // Put the unused parts back into the exchanger (if they aren't already there)
        foreach (var (partType, partSet) in partsByType)
        {
            foreach (var (part, state) in partSet)
            {
                if (!state.InExchanger)
                    _storage.Insert(exchanger, part, out _, playSound: false);
            }
        }
    }

    #endregion

    #region Helpers

    private void TryStartDoAfter(Entity<PartExchangerComponent> exchanger, EntityUid target, EntityUid user)
    {
        if (!HasComp<MachineComponent>(target) &&
            !HasComp<MachineFrameComponent>(target) &&
            !HasComp<VehicleUpgradeComponent>(target))
            return;

        if (TryComp<WiresPanelComponent>(target, out var panel) && !panel.Open)
        {
            _popup.PopupEntity(Loc.GetString("construction-step-condition-wire-panel-open"), target);
            return;
        }

        var audioStream = _audio.PlayPvs(exchanger.Comp.ExchangeSound, exchanger);
        if (audioStream != null)
        {
            exchanger.Comp.AudioStream = audioStream.Value.Entity;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, exchanger.Comp.ExchangeDuration, new ExchangerDoAfterEvent(), exchanger, target: target, used: exchanger)
        {
            BreakOnDamage = true,
            BreakOnMove = true
        });
    }

    /// <summary>
    /// Helper method for walking the machine parts in a container.
    /// </summary>
    /// <param name="inExchanger">True if the container belongs to the part exchanger; false if it's the target.</param>
    /// <returns></returns>
    private IEnumerable<(EntityUid, UpgradePartState)> ContainedMachineParts(Container partContainer, bool inExchanger)
    {
        // Make a copy of the list so it's safe to modify
        foreach (var item in new ValueList<EntityUid>(partContainer.ContainedEntities))
        {
            if (_construction.GetMachinePartState(item, out var partState))
            {
                UpgradePartState upgrade;
                upgrade.Part = partState.Part;
                upgrade.Stack = partState.Stack;
                upgrade.InExchanger = inExchanger;
                yield return (item, upgrade);
            }
        }
    }

    #endregion
}
