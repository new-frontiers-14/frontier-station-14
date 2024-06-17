using Content.Server.Construction.Components;
using Content.Shared.Construction.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Construction;

public sealed partial class ConstructionSystem
{
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
            var partProto = _prototypeManager.Index<MachinePartPrototype>(part);
            for (var i = 0; i < amount; i++)
            {
                var p = EntityManager.SpawnEntity(partProto.StockPartPrototype, xform.Coordinates);

                if (!_container.Insert(p, partContainer))
                    throw new Exception($"Couldn't insert machine part of type {part} to machine with prototype {partProto.StockPartPrototype ?? "N/A"}!");
            }
        }

        foreach (var (stackType, amount) in machineBoard.MaterialRequirements)
        {
            var stack = _stackSystem.Spawn(amount, stackType, Transform(uid).Coordinates); // MERGE: xform.Coordinates?
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
