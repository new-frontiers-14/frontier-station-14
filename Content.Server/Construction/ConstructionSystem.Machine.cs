using System.Linq; // Frontier
using Content.Server._NF.BindToStation; // Frontier
using Content.Server.Construction.Components;
using Content.Server.Station.Systems; // Frontier
using Content.Shared._NF.BindToStation; // Frontier
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes; // Frontier
using Robust.Shared.Utility;

namespace Content.Server.Construction;

public sealed partial class ConstructionSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!; // Frontier

    private void InitializeMachines()
    {
        SubscribeLocalEvent<MachineComponent, ComponentInit>(OnMachineInit);
        SubscribeLocalEvent<MachineComponent, MapInitEvent>(OnMachineMapInit);
    }

    private void OnMachineInit(EntityUid uid, MachineComponent component, ComponentInit args)
    {
        component.BoardContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.BoardContainerName);
        component.PartContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.PartContainerName);

        // Frontier - we mirror the bind to grid component from any existing machine board onto the resultant machine to prevent high-grading
        foreach (var board in component.BoardContainer.ContainedEntities)
        {
            if (TryComp<StationBoundObjectComponent>(board, out var binding))
                _bindToStation.BindToStation(uid, binding.BoundStation, binding.Enabled);
        }
        // End Frontier
    }

    private void OnMachineMapInit(EntityUid uid, MachineComponent component, MapInitEvent args)
    {
        CreateBoardAndStockParts(uid, component);
        RefreshParts(uid, component); // Frontier: get initial upgrade values
    }

    private void CreateBoardAndStockParts(EntityUid uid, MachineComponent component)
    {
        // Entity might not be initialized yet.
        var boardContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.BoardContainerName);
        var partContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.PartContainerName);

        if (string.IsNullOrEmpty(component.Board))
            return;

        // We're done here, let's suppose all containers are correct just so we don't screw SaveLoadSave.
        if (boardContainer.ContainedEntities.Count > 0)
            return;

        var xform = Transform(uid);
        if (!TrySpawnInContainer(component.Board, uid, MachineFrameComponent.BoardContainerName, out var board))
        {
            throw new Exception($"Couldn't insert board with prototype {component.Board} to machine with prototype {Prototype(uid)?.ID ?? "N/A"}!");
        }

        if (!TryComp<MachineBoardComponent>(board, out var machineBoard))
        {
            throw new Exception($"Entity with prototype {component.Board} doesn't have a {nameof(MachineBoardComponent)}!");
        }

        // Frontier: Only bind the board if the machine itself has the BindToStationComponent and the board doesn't already have BindToStationComponent  
        if (HasComp<BindToStationComponent>(uid) && board != null)
        {
            var machineStation = _station.GetOwningStation(uid);
            if (machineStation != null)
            {
                _bindToStation.BindToStation(board.Value, machineStation.Value);
            }
        }
        // End Frontier

        foreach (var (stackType, amount) in machineBoard.StackRequirements)
        {
            var stack = _stackSystem.Spawn(amount, stackType, xform.Coordinates);
            if (!_container.Insert(stack, partContainer))
                throw new Exception($"Couldn't insert machine material of type {stackType} to machine with prototype {Prototype(uid)?.ID ?? "N/A"}");
        }

        foreach (var (compName, info) in machineBoard.ComponentRequirements)
        {
            for (var i = 0; i < info.Amount; i++)
            {
                if(!TrySpawnInContainer(info.DefaultPrototype, uid, MachineFrameComponent.PartContainerName, out _))
                    throw new Exception($"Couldn't insert machine component part with default prototype '{compName}' to machine with prototype {Prototype(uid)?.ID ?? "N/A"}");
            }
        }

        foreach (var (tagName, info) in machineBoard.TagRequirements)
        {
            for (var i = 0; i < info.Amount; i++)
            {
                if(!TrySpawnInContainer(info.DefaultPrototype, uid, MachineFrameComponent.PartContainerName, out _))
                    throw new Exception($"Couldn't insert machine component part with default prototype '{tagName}' to machine with prototype {Prototype(uid)?.ID ?? "N/A"}");
            }
        }

        // Frontier: keep separate lists for upgradeable parts
        foreach (var (part, amount) in machineBoard.Requirements)
        {
            var partProto = _prototypeManager.Index<MachinePartPrototype>(part);
            for (var i = 0; i < amount; i++)
            {
                var p = EntityManager.SpawnEntity(partProto.StockPartPrototype, xform.Coordinates);

                if (!_container.Insert(p, partContainer))
                    throw new Exception($"Couldn't insert machine part of type {part} to machine with prototype {partProto.StockPartPrototype.ToString() ?? "N/A"}!");
            }
        }
        // End Frontier: keep separate lists for upgradeable parts
    }
}
