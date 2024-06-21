using System.Linq; // Frontier
using Content.Server.Construction.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes; // Frontier
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
    }

    private void OnMachineMapInit(EntityUid uid, MachineComponent component, MapInitEvent args)
    {
        CreateBoardAndStockParts(uid, component);
    }

    // FRONTIER MERGE: ADDED THIS
    public List<MachinePartComponent> GetAllParts(EntityUid uid, MachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new List<MachinePartComponent>();

        return GetAllParts(component);
    }

    public List<MachinePartComponent> GetAllParts(MachineComponent component)
    {
        var parts = new List<MachinePartComponent>();

        foreach (var entity in component.PartContainer.ContainedEntities)
        {
            if (TryComp<MachinePartComponent>(entity, out var machinePart))
                parts.Add(machinePart);
        }

        return parts;
    }

    public Dictionary<string, float> GetPartsRatings(List<MachinePartComponent> parts)
    {
        var output = new Dictionary<string, float>();
        foreach (var type in _prototypeManager.EnumeratePrototypes<MachinePartPrototype>())
        {
            var amount = 0f;
            var sumRating = 0f;
            foreach (var part in parts.Where(part => part.PartType == type.ID))
            {
                amount++;
                sumRating += part.Rating;
            }
            var rating = amount != 0 ? sumRating / amount : 0;
            output.Add(type.ID, rating);
        }

        return output;
    }

    public void RefreshParts(EntityUid uid, MachineComponent component)
    {
        var parts = GetAllParts(component);
        EntityManager.EventBus.RaiseLocalEvent(uid, new RefreshPartsEvent
        {
            Parts = parts,
            PartRatings = GetPartsRatings(parts),
        }, true);
    }
    // END FRONTIER MERGE

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

        foreach (var (stackType, amount) in machineBoard.StackRequirements)
        {
            var stack = _stackSystem.Spawn(amount, stackType, xform.Coordinates);
            if (!_container.Insert(stack, partContainer))
                throw new Exception($"Couldn't insert machine material of type {stackType} to machine with prototype {Prototype(uid)?.ID ?? "N/A"}");
            // FRONTIER MERGE: COMMENTED THIS OLD CHUNK OUT
            var partProto = _prototypeManager.Index<MachinePartPrototype>(stackType);
            for (var i = 0; i < amount; i++)
            {
                var p = EntityManager.SpawnEntity(partProto.StockPartPrototype, xform.Coordinates);

                if (!_container.Insert(p, partContainer))
                    throw new Exception($"Couldn't insert machine part of type {stackType} to machine with prototype {partProto.StockPartPrototype ?? "N/A"}!");
            }
            // END FRONTIER MERGE
        }

        // FRONTIER MERGE: COMMENTED THIS OLD CHUNK OUT
        foreach (var (stackType, amount) in machineBoard.StackRequirements)
        {
            var stack = _stackSystem.Spawn(amount, stackType, xform.Coordinates);
            if (!_container.Insert(stack, partContainer))
                throw new Exception($"Couldn't insert machine material of type {stackType} to machine with prototype {Prototype(uid)?.ID ?? "N/A"}");
        }
        // END FRONTIER MERGE

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
    }
}

public sealed class RefreshPartsEvent : EntityEventArgs
{
    public IReadOnlyList<MachinePartComponent> Parts = new List<MachinePartComponent>();

    public Dictionary<string, float> PartRatings = new Dictionary<string, float>();
}

public sealed class UpgradeExamineEvent : EntityEventArgs
{
    private FormattedMessage Message;

    public UpgradeExamineEvent(ref FormattedMessage message)
    {
        Message = message;
    }

    /// <summary>
    /// Add a line to the upgrade examine tooltip with a percentage-based increase or decrease.
    /// </summary>
    public void AddPercentageUpgrade(string upgradedLocId, float multiplier)
    {
        var percent = Math.Round(100 * MathF.Abs(multiplier - 1), 2);
        var locId = multiplier switch {
            < 1 => "machine-upgrade-decreased-by-percentage",
            1 or float.NaN => "machine-upgrade-not-upgraded",
            > 1 => "machine-upgrade-increased-by-percentage",
        };
        var upgraded = Loc.GetString(upgradedLocId);
        this.Message.AddMarkup(Loc.GetString(locId, ("upgraded", upgraded), ("percent", percent)) + '\n');
    }

    /// <summary>
    /// Add a line to the upgrade examine tooltip with a numeric increase or decrease.
    /// </summary>
    public void AddNumberUpgrade(string upgradedLocId, int number)
    {
        var difference = Math.Abs(number);
        var locId = number switch {
            < 0 => "machine-upgrade-decreased-by-amount",
            0 => "machine-upgrade-not-upgraded",
            > 0 => "machine-upgrade-increased-by-amount",
        };
        var upgraded = Loc.GetString(upgradedLocId);
        this.Message.AddMarkup(Loc.GetString(locId, ("upgraded", upgraded), ("difference", difference)) + '\n');
    }
}
