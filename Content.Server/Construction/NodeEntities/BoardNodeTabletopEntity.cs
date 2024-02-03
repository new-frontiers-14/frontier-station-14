using Content.Server.Construction.Components;
using Content.Shared.Construction;
using Content.Shared.Construction.Components;
using JetBrains.Annotations;
using Robust.Server.Containers;

namespace Content.Server.Construction.NodeEntities;

/// <summary>
///     Works for both <see cref="ComputerTabletopBoardComponent"/>
///     Duplicating code from BoardNode just for this. Yep, really stinky.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class BoardNodeTabletopEntity : IGraphNodeEntity
{
    [DataField("container")] public string Container { get; private set; } = string.Empty;

    public string? GetId(EntityUid? uid, EntityUid? userUid, GraphNodeEntityArgs args)
    {
        if (uid == null)
            return null;

        var containerSystem = args.EntityManager.EntitySysManager.GetEntitySystem<ContainerSystem>();

        if (!containerSystem.TryGetContainer(uid.Value, Container, out var container)
            || container.ContainedEntities.Count == 0)
            return null;

        var board = container.ContainedEntities[0];

        if (args.EntityManager.TryGetComponent(board, out ComputerTabletopBoardComponent? tabletopcomputer)) // How thee fuck can I check if the construction graph I'm following is for tabletop comp?
            return tabletopcomputer.Prototype;

        return null;
    }
}
