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
    [Dependency] private readonly EntityManager _entity = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PartExchangerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PartExchangerComponent, ExchangerDoAfterEvent>(OnDoAfter);
    }

    private struct UpgradePartState
    {
        public MachinePartComponent Part;
        public StackComponent? Stack;
        public bool InContainer;
    }

    private void OnDoAfter(EntityUid uid, PartExchangerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled)
        {
            component.AudioStream = _audio.Stop(component.AudioStream);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        if (!TryComp<StorageComponent>(uid, out var storage) || storage.Container == null)
            return;

        var partsByType = new Dictionary<ProtoId<MachinePartPrototype>, List<(EntityUid, UpgradePartState)>>();

        // Insert the contained parts into a dictionary for indexing.
        // Note: these parts remain in the starting container.
        foreach (var item in storage.Container.ContainedEntities)
        {
            if (_construction.GetMachinePartState(item, out var partState))
            {
                UpgradePartState upgrade;
                upgrade.Part = partState.Part;
                upgrade.Stack = partState.Stack;
                upgrade.InContainer = true;

                var partType = upgrade.Part.PartType;
                if (!partsByType.ContainsKey(partType))
                    partsByType[partType] = new List<(EntityUid, UpgradePartState)>();
                partsByType[partType].Add((item, upgrade));
            }
        }

        // Exchange machine parts with the machine or frame.
        if (TryComp<MachineComponent>(args.Args.Target.Value, out var machine))
            TryExchangeMachineParts(machine, args.Args.Target.Value, uid, partsByType);
        else if (TryComp<MachineFrameComponent>(args.Args.Target.Value, out var machineFrame))
            TryConstructMachineParts(machineFrame, args.Args.Target.Value, uid, partsByType);

        args.Handled = true;
    }

    private void TryExchangeMachineParts(MachineComponent machine, EntityUid uid, EntityUid storageUid, Dictionary<ProtoId<MachinePartPrototype>, List<(EntityUid part, UpgradePartState state)>> partsByType)
    {
        var board = machine.BoardContainer.ContainedEntities.FirstOrNull();

        if (board == null || !TryComp<MachineBoardComponent>(board, out var macBoardComp))
            return;

        // Add all components in the machine to form a complete set of available components.
        foreach (var item in new ValueList<EntityUid>(machine.PartContainer.ContainedEntities)) //clone so don't modify during enumeration
        {
            if (_construction.GetMachinePartState(item, out var partState))
            {
                UpgradePartState upgrade;
                upgrade.Part = partState.Part;
                upgrade.Stack = partState.Stack;
                upgrade.InContainer = false;

                var partType = upgrade.Part.PartType;
                if (!partsByType.ContainsKey(partType))
                    partsByType[partType] = new List<(EntityUid, UpgradePartState)>();
                partsByType[partType].Add((item, upgrade));

                _container.RemoveEntity(uid, item);
            }
        }

        // Sort by rating in descending order (highest rated parts first)
        foreach (var (partKey, partList) in partsByType)
            partList.Sort((x, y) => y.state.Part.Rating.CompareTo(x.state.Part.Rating));

        var updatedParts = new List<(EntityUid id, MachinePartState state, int index)>();
        foreach (var (type, amount) in macBoardComp.Requirements)
        {
            if (partsByType.ContainsKey(type))
            {
                var partsNeeded = amount;
                int index = 0;
                foreach ((var part, var state) in partsByType[type])
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

                            updatedParts.Add((part, partState, index));
                            partsNeeded -= count;
                        }
                        else
                        {
                            // Partial stack is needed, split off what we need, ensure the new entry is moved.
                            EntityUid splitStack = _stack.Split(part, partsNeeded, Transform(uid).Coordinates, state.Stack) ?? EntityUid.Invalid;

                            if (splitStack == EntityUid.Invalid)
                                continue;

                            // Create a new MachinePartState out of our new entity
                            if (_construction.GetMachinePartState(splitStack, out var splitState))
                            {
                                updatedParts.Add((splitStack, splitState, -1)); // Use -1 for index, nothing to remove
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

                        updatedParts.Add((part, partState, index));
                        partsNeeded--;
                    }
                    // Adjust the index for parts being removed from the container.
                    index++;
                }
            }
        }

        // Move selected parts to the machine, removing them from the dictionary of contained parts.
        // Iterate through list backwards, remove later entries first (maintain validity of earlier indices).
        for (int i = updatedParts.Count - 1; i >= 0; i--)
        {
            var part = updatedParts[i];
            bool inserted = _container.Insert(part.id, machine.PartContainer);
            if (part.index >= 0)
                partsByType[part.state.Part.PartType].RemoveAt(part.index);
        }

        //Put the unused parts back into the container (if they aren't already there)
        foreach (var (partType, partSet) in partsByType)
        {
            foreach (var partState in partSet)
            {
                if (!partState.state.InContainer)
                    _storage.Insert(storageUid, partState.part, out _, playSound: false);
            }
        }
        _construction.RefreshParts(uid, machine);
    }

    private void TryConstructMachineParts(MachineFrameComponent machine, EntityUid uid, EntityUid storageEnt, Dictionary<ProtoId<MachinePartPrototype>, List<(EntityUid part, UpgradePartState state)>> partsByType)
    {
        var board = machine.BoardContainer.ContainedEntities.FirstOrNull();

        if (!machine.HasBoard || !TryComp<MachineBoardComponent>(board, out var macBoardComp))
            return;

        // Add all components in the machine to form a complete set of available components.
        foreach (var item in new ValueList<EntityUid>(machine.PartContainer.ContainedEntities)) //clone so don't modify during enumeration
        {
            if (_construction.GetMachinePartState(item, out var partState))
            {
                // Construct our entry
                UpgradePartState upgrade;
                upgrade.Part = partState.Part;
                upgrade.Stack = partState.Stack;
                upgrade.InContainer = false;

                // Add it to the table
                var partType = upgrade.Part.PartType;
                if (!partsByType.ContainsKey(partType))
                    partsByType[partType] = new List<(EntityUid, UpgradePartState)>();
                partsByType[partType].Add((item, upgrade));

                // Make sure the construction status is consistent with the removed parts.
                machine.Progress[partType] -= partState.Quantity();
                machine.Progress[partType] = int.Max(0, machine.Progress[partType]); // Ensure progress isn't negative.

                _container.RemoveEntity(uid, item);
            }
        }

        // Sort parts in descending order of rating (highest rated parts first)
        foreach (var partList in partsByType.Values)
            partList.Sort((x, y) => y.state.Part.Rating.CompareTo(x.state.Part.Rating));

        var updatedParts = new List<(EntityUid id, MachinePartState state, int index)>();
        foreach (var (type, amount) in macBoardComp.Requirements)
        {
            if (partsByType.ContainsKey(type))
            {
                var partsNeeded = amount;
                var index = 0;
                foreach ((var part, var state) in partsByType[type])
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

                            updatedParts.Add((part, partState, index));
                            partsNeeded -= count;
                        }
                        else
                        {
                            // Partial stack is needed, split off what we need, ensure the new entry is moved.
                            EntityUid splitStack = _stack.Split(part, partsNeeded, Transform(uid).Coordinates, state.Stack) ?? EntityUid.Invalid;

                            if (splitStack == EntityUid.Invalid)
                                continue;

                            // Create a new MachinePartState out of our new entity
                            if (_construction.GetMachinePartState(splitStack, out var splitState))
                            {
                                updatedParts.Add((splitStack, splitState, -1)); // New entity, nothing to remove, set index to -1 to flag this.
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

                        updatedParts.Add((part, partState, index));
                        partsNeeded--;
                    }
                    // Adjust the index for parts being removed from the container.
                    index++;
                }
            }
        }

        // Move selected parts to the machine, removing them from the dictionary of contained parts.
        // Iterate through list backwards, remove later entries first (maintain validity of earlier indices).
        for (int i = updatedParts.Count - 1; i >= 0; i--)
        {
            var part = updatedParts[i];
            _container.Insert(part.id, machine.PartContainer, force: true);
            if (part.index >= 0)
                partsByType[part.state.Part.PartType].RemoveAt(part.index);
            machine.Progress[part.state.Part.PartType] += part.state.Quantity();
        }

        //Put the unused parts back into the container (if they aren't already there)
        foreach (var (partType, partSet) in partsByType)
        {
            foreach (var partState in partSet)
            {
                if (!partState.state.InContainer)
                    _storage.Insert(uid, partState.part, out _, playSound: false);
            }
        }
    }

    private void OnAfterInteract(EntityUid uid, PartExchangerComponent component, AfterInteractEvent args)
    {
        if (component.DoDistanceCheck && !args.CanReach)
            return;

        if (args.Target == null)
            return;

        if (!HasComp<MachineComponent>(args.Target) && !HasComp<MachineFrameComponent>(args.Target))
            return;

        if (TryComp<WiresPanelComponent>(args.Target, out var panel) && !panel.Open)
        {
            _popup.PopupEntity(Loc.GetString("construction-step-condition-wire-panel-open"),
                args.Target.Value);
            return;
        }

        component.AudioStream = _audio.PlayPvs(component.ExchangeSound, uid).Value.Entity;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.ExchangeDuration, new ExchangerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true
        });
    }
}
