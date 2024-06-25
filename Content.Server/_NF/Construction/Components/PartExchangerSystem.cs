using System.Linq;
using Content.Server._NF.Construction.Components;
using Content.Server.Construction;
using Content.Server.Construction.Components;
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

namespace Content.Server._NF.Construction;

public sealed class PartExchangerSystem : EntitySystem
{
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StorageSystem _storage = default!;

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
        var machineParts = new List<(EntityUid, MachinePartState)>();

        foreach (var item in storage.Container.ContainedEntities) //get parts in RPED
        {
            if (machinePartQuery.TryGetComponent(item, out var part))
            {
                MachinePartState partState = new MachinePartState
                {
                    Part = part
                };
                stackQuery.TryGetComponent(item, out partState.Stack);
                machineParts.Add((item, partState));
            }
        }

        TryExchangeMachineParts(args.Args.Target.Value, uid, machineParts);
        TryConstructMachineParts(args.Args.Target.Value, uid, machineParts);

        args.Handled = true;
    }

    private void TryExchangeMachineParts(EntityUid uid, EntityUid storageUid, List<(EntityUid part, MachinePartState state)> machineParts)
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
                machineParts.Add((item, partState));
                _container.RemoveEntity(uid, item);
            }
        }

        machineParts.Sort((x, y) => y.state.Part.Rating.CompareTo(x.state.Part.Rating));

        var updatedParts = new List<(EntityUid id, MachinePartState state)>();
        foreach (var (type, amount) in macBoardComp.Requirements)
        {
            var target = machineParts.Where(p => p.state.Part.PartType == type).Take(amount);
            updatedParts.AddRange(target);
        }

        foreach (var part in updatedParts)
        {
            _container.Insert(part.id, machine.PartContainer);
            machineParts.Remove(part);
        }

        //put the unused parts back into rped. (this also does the "swapping")
        foreach (var (unused, _) in machineParts)
        {
            _storage.Insert(storageUid, unused, out _, playSound: false);
        }
        _construction.RefreshParts(uid, machine);
    }

    private void TryConstructMachineParts(EntityUid uid, EntityUid storageEnt, List<(EntityUid part, MachinePartState state)> machineParts)
    {
        if (!TryComp<MachineFrameComponent>(uid, out var machine))
            return;

        var machinePartQuery = GetEntityQuery<MachinePartComponent>();
        var stackQuery = GetEntityQuery<StackComponent>();
        var board = machine.BoardContainer.ContainedEntities.FirstOrNull();

        if (!machine.HasBoard || !TryComp<MachineBoardComponent>(board, out var macBoardComp))
            return;

        foreach (var item in new ValueList<EntityUid>(machine.PartContainer.ContainedEntities)) //clone so don't modify during enumeration
        {
            if (machinePartQuery.TryGetComponent(item, out var part))
            {
                MachinePartState partState = new MachinePartState
                {
                    Part = part
                };
                stackQuery.TryGetComponent(item, out partState.Stack);
                machineParts.Add((item, partState));
                _container.RemoveEntity(uid, item);
                /*if (part.StackType is not null) // Frontier: FIXME: HACKS
                {
                    var stackType = part.StackType ?? new ProtoId<StackPrototype>();
                    machine.MaterialProgress[stackType]--;
                }*/
            }
        }

        machineParts.Sort((x, y) => y.state.Part.Rating.CompareTo(x.state.Part.Rating));

        var updatedParts = new List<(EntityUid part, MachinePartState state)>();
        foreach (var (type, amount) in macBoardComp.Requirements)
        {
            var target = machineParts.Where(p => p.state.Part.PartType == type).Take(amount);
            updatedParts.AddRange(target);
        }
        foreach (var pair in updatedParts)
        {
            var part = pair.state;
            var partEnt = pair.part;

            //var stackType = part.StackType ?? new ProtoId<StackPrototype>(); //FRONTIER: FIXME

            /*if (!machine.MaterialRequirements.ContainsKey(stackType)) // FRONTIER: FIXME
                continue;*/

            _container.Insert(partEnt, machine.PartContainer);
            /*if (part.StackType is not null)
                machine.MaterialProgress[stackType]++;*/ //FRONTIER: FIXME
            machineParts.Remove(pair);
        }

        //put the unused parts back into rped. (this also does the "swapping")
        foreach (var (unused, _) in machineParts)
        {
            _storage.Insert(storageEnt, unused, out _, playSound: false);
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
