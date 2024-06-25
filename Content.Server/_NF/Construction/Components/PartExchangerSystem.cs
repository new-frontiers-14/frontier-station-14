using System.Linq;
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
using Robust.Shared.Audio;
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
            return; //the parts are stored in here

        var machinePartQuery = GetEntityQuery<MachinePartComponent>();
        var stackQuery = GetEntityQuery<StackComponent>();
        var partsByType = new Dictionary<ProtoId<MachinePartPrototype>, List<(EntityUid, MachinePartState)>>();

        foreach (var item in storage.Container.ContainedEntities) //get parts in RPED
        {
            if (machinePartQuery.TryGetComponent(item, out var part))
            {
                MachinePartState partState = new MachinePartState
                {
                    Part = part
                };
                stackQuery.TryGetComponent(item, out partState.Stack);
                if (!partsByType.ContainsKey(part.PartType))
                    partsByType[part.PartType] = new List<(EntityUid, MachinePartState)>();
                partsByType[part.PartType].Add((item, partState));
            }
        }

        TryExchangeMachineParts(args.Args.Target.Value, uid, partsByType);
        TryConstructMachineParts(args.Args.Target.Value, uid, partsByType);

        args.Handled = true;
    }

    private void TryExchangeMachineParts(EntityUid uid, EntityUid storageUid, Dictionary<ProtoId<MachinePartPrototype>, List<(EntityUid part, MachinePartState state)>> partsByType)
    {
        if (!TryComp<MachineComponent>(uid, out var machine))
            return;

        var machinePartQuery = GetEntityQuery<MachinePartComponent>();
        var stackQuery = GetEntityQuery<StackComponent>();
        var board = machine.BoardContainer.ContainedEntities.FirstOrNull();

        if (board == null || !TryComp<MachineBoardComponent>(board, out var macBoardComp))
            return;

        // Add all components in the machine to form a complete set of available components.
        foreach (var item in new ValueList<EntityUid>(machine.PartContainer.ContainedEntities)) //clone so don't modify during enumeration
        {
            if (machinePartQuery.TryGetComponent(item, out var part))
            {
                MachinePartState partState = new MachinePartState
                {
                    Part = part
                };
                stackQuery.TryGetComponent(item, out partState.Stack);

                if (!partsByType.ContainsKey(part.PartType))
                    partsByType[part.PartType] = new List<(EntityUid, MachinePartState)>();
                partsByType[part.PartType].Add((item, partState));

                _container.RemoveEntity(uid, item);
            }
        }

        foreach (var partList in partsByType.Values)
            partList.Sort((x, y) => y.state.Part.Rating.CompareTo(x.state.Part.Rating));

        var updatedParts = new List<(EntityUid id, MachinePartState state)>();
        foreach (var (type, amount) in macBoardComp.Requirements)
        {
            if (partsByType.ContainsKey(type))
            {
                var partsNeeded = amount;
                foreach ((var part, var state) in partsByType[type])
                {
                    // No more space for components
                    if (partsNeeded <= 0)
                        break;

                    // This part is stackable - either split off what we need, or add it entirely to the set to be moved.
                    if (state.Stack is not null)
                    {
                        var count = state.Stack.Count;
                        if (count <= partsNeeded)
                        {
                            updatedParts.Add((part, state));
                            partsNeeded -= count;
                        }
                        else
                        {
                            EntityUid splitStack = _stack.Split(part, partsNeeded, Transform(uid).Coordinates, state.Stack) ?? EntityUid.Invalid;

                            // TODO: better error handling?  Why would this fail?
                            if (splitStack == EntityUid.Invalid)
                                continue;

                            // Create a new MachinePartState out of our new entity
                            MachinePartState splitState = new MachinePartState();
                            if (TryComp(splitStack, out MachinePartComponent? splitPart) && splitPart is not null) // Nullable type - fix this.
                                splitState.Part = splitPart;
                            TryComp(splitStack, out splitState.Stack);

                            updatedParts.Add((splitStack, splitState));
                            partsNeeded = 0;
                        }
                    }
                    else
                    {
                        updatedParts.Add((part, state));
                        partsNeeded--;
                    }
                }
            }
        }

        foreach (var part in updatedParts)
        {
            _container.Insert(part.id, machine.PartContainer);
            partsByType[part.state.Part.PartType].Remove(part);
        }

        //put the unused parts back into rped. (this also does the "swapping")
        //NOTE: this may destroy removed parts if there is not enough space in the RPED (due to stacking issues)
        foreach (var partSet in partsByType.Values)
        {
            foreach (var partState in partSet)
            {
                _storage.Insert(storageUid, partState.part, out _, playSound: false);
            }
        }
        _construction.RefreshParts(uid, machine);
    }

    private void TryConstructMachineParts(EntityUid uid, EntityUid storageEnt, Dictionary<ProtoId<MachinePartPrototype>, List<(EntityUid part, MachinePartState state)>> partsByType)
    {
        if (!TryComp<MachineFrameComponent>(uid, out var machine))
            return;

        var machinePartQuery = GetEntityQuery<MachinePartComponent>();
        var stackQuery = GetEntityQuery<StackComponent>();
        var board = machine.BoardContainer.ContainedEntities.FirstOrNull();

        if (!machine.HasBoard || !TryComp<MachineBoardComponent>(board, out var macBoardComp))
            return;

        // Add all components in the machine to form a complete set of available components.
        foreach (var item in new ValueList<EntityUid>(machine.PartContainer.ContainedEntities)) //clone so don't modify during enumeration
        {
            if (machinePartQuery.TryGetComponent(item, out var part))
            {
                MachinePartState partState = new MachinePartState
                {
                    Part = part
                };
                stackQuery.TryGetComponent(item, out partState.Stack);

                if (!partsByType.ContainsKey(part.PartType))
                    partsByType[part.PartType] = new List<(EntityUid, MachinePartState)>();
                partsByType[part.PartType].Add((item, partState));

                machine.Progress[part.PartType] -= partState.Quantity();
                machine.Progress[part.PartType] = int.Max(0, machine.Progress[part.PartType]); // Ensure progress isn't negative.

                _container.RemoveEntity(uid, item);
            }
        }

        foreach (var partList in partsByType.Values)
            partList.Sort((x, y) => y.state.Part.Rating.CompareTo(x.state.Part.Rating));

        var updatedParts = new List<(EntityUid id, MachinePartState state)>();
        foreach (var (type, amount) in macBoardComp.Requirements)
        {
            if (partsByType.ContainsKey(type))
            {
                var partsNeeded = amount;
                foreach ((var part, var state) in partsByType[type])
                {
                    // No more space for components
                    if (partsNeeded <= 0)
                        break;

                    // This part is stackable - either split off what we need, or add it entirely to the set to be moved.
                    if (state.Stack is not null)
                    {
                        var count = state.Stack.Count;
                        if (count <= partsNeeded)
                        {
                            updatedParts.Add((part, state));
                            partsNeeded -= count;
                        }
                        else
                        {
                            EntityUid splitStack = _stack.Split(part, partsNeeded, Transform(uid).Coordinates, state.Stack) ?? EntityUid.Invalid;

                            // TODO: better error handling?  Why would this fail?
                            if (splitStack == EntityUid.Invalid)
                                continue;

                            // Create a new MachinePartState out of our new entity
                            MachinePartState splitState = new MachinePartState();
                            if (TryComp(splitStack, out MachinePartComponent? splitPart) && splitPart is not null) // Nullable type - fix this.
                                splitState.Part = splitPart;
                            TryComp(splitStack, out splitState.Stack);

                            updatedParts.Add((splitStack, splitState));
                            partsNeeded = 0;
                        }
                    }
                    else
                    {
                        updatedParts.Add((part, state));
                        partsNeeded--;
                    }
                }
            }
        }

        foreach (var element in updatedParts)
        {
            _container.Insert(element.id, machine.PartContainer);
            partsByType[element.state.Part.PartType].Remove(element); // Frontier: 
            machine.Progress[element.state.Part.PartType] += element.state.Quantity();
        }

        //put the unused parts back into rped. (this also does the "swapping")
        //NOTE: this may destroy removed parts if there is not enough space in the RPED (due to stacking issues)
        foreach (var partSet in partsByType.Values)
        {
            foreach (var partState in partSet)
            {
                _storage.Insert(storageEnt, partState.part, out _, playSound: false);
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
