using Content.Server._NF.Construction.Components; // Frontier
using Content.Shared.Construction;
using Content.Shared.Construction.Components;
using JetBrains.Annotations;
using Robust.Server.Containers;

namespace Content.Server.Construction.NodeEntities;

/// <summary>
///     Works for both <see cref="ComputerBoardComponent"/> and <see cref="MachineBoardComponent"/>
///     because duplicating code just for this is really stinky.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class BoardNodeEntity : IGraphNodeEntity
{
    [DataField("container")] public string Container { get; private set; } = string.Empty;
    [DataField] public ComputerType Computer { get; private set; } = ComputerType.Default; // Frontier

    public string? GetId(EntityUid? uid, EntityUid? userUid, GraphNodeEntityArgs args)
    {
        if (uid == null)
            return null;

        var containerSystem = args.EntityManager.EntitySysManager.GetEntitySystem<ContainerSystem>();

        if (!containerSystem.TryGetContainer(uid.Value, Container, out var container)
            || container.ContainedEntities.Count == 0)
            return null;

        var board = container.ContainedEntities[0];

        // Frontier - alternative computer variants
        switch (Computer)
        {
            case ComputerType.Tabletop:
                if (args.EntityManager.TryGetComponent(board, out ComputerTabletopBoardComponent? tabletopComputer))
                    return tabletopComputer.Prototype;
                break;
            case ComputerType.Wallmount:
                if (args.EntityManager.TryGetComponent(board, out ComputerWallmountBoardComponent? wallmountComputer))
                    return wallmountComputer.Prototype;
                break;
            case ComputerType.Default:
            default:
                break;
        }
        // End Frontier

        // There should not be a case where both of these components exist on the same entity...
        if (args.EntityManager.TryGetComponent(board, out MachineBoardComponent? machine))
            return machine.Prototype;

        if(args.EntityManager.TryGetComponent(board, out ComputerBoardComponent? computer))
            return computer.Prototype;

        return null;
    }

    // Frontier: support for multiple computer types
    public enum ComputerType : byte
    {
        Default, // Default machines
        Tabletop,
        Wallmount
    }
    // End Frontier
}
