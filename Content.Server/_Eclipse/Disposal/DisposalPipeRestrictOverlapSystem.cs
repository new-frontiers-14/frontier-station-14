using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Tube.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Construction.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// This handles restricting pipe-based entities from overlapping outlets/inlets with other entities.
/// </summary>
public sealed class DisposalPipeRestrictOverlapSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    private readonly List<EntityUid> _anchoredEntities = new();
    private EntityQuery<DisposalTubeComponent> _nodeContainerQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DisposalPipeRestrictOverlapComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<DisposalPipeRestrictOverlapComponent, AnchorAttemptEvent>(OnAnchorAttempt);

        _nodeContainerQuery = GetEntityQuery<DisposalTubeComponent>();
    }

    private void OnAnchorStateChanged(Entity<DisposalPipeRestrictOverlapComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        if (HasComp<AnchorableComponent>(ent) && CheckOverlap(ent))
        {
            _popup.PopupEntity(Loc.GetString("pipe-restrict-overlap-popup-blocked", ("pipe", ent.Owner)), ent);
            _xform.Unanchor(ent, Transform(ent));
        }
    }

    private void OnAnchorAttempt(Entity<DisposalPipeRestrictOverlapComponent> ent, ref AnchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_nodeContainerQuery.TryComp(ent, out var node))
            return;

        var xform = Transform(ent);
        if (CheckOverlap((ent, node, xform)))
        {
            _popup.PopupEntity(Loc.GetString("pipe-restrict-overlap-popup-blocked", ("pipe", ent.Owner)), ent, args.User);
            args.Cancel();
        }
    }

    [PublicAPI]
    public bool CheckOverlap(EntityUid uid)
    {
        if (!_nodeContainerQuery.TryComp(uid, out var node))
            return false;

        return CheckOverlap((uid, node, Transform(uid)));
    }

    public bool CheckOverlap(Entity<DisposalTubeComponent, TransformComponent> ent)
    {
        if (ent.Comp2.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        var indices = _map.TileIndicesFor(grid, gridComp, ent.Comp2.Coordinates);
        _anchoredEntities.Clear();
        _map.GetAnchoredEntities((grid, gridComp), indices, _anchoredEntities);

        foreach (var otherEnt in _anchoredEntities)
        {
            // this should never actually happen but just for safety
            if (otherEnt == ent.Owner)
                continue;

            if (!_nodeContainerQuery.TryComp(otherEnt, out var otherComp))
                continue;

            if (PipeNodesOverlap(ent, (otherEnt, otherComp, Transform(otherEnt))))
                return true;
        }

        return false;
    }

    public bool PipeNodesOverlap(Entity<DisposalTubeComponent, TransformComponent> ent, Entity<DisposalTubeComponent, TransformComponent> other)
    {
        var entDirs = GetAllDirections(ent).ToList();
        var otherDirs = GetAllDirections(other).ToList();

        foreach (var dir in entDirs)
        {
            foreach (var otherDir in otherDirs)
            {
                if (dir == otherDir)
                    return true;
            }
        }

        return false;
    }

    private Direction[] GetAllDirections(Entity<DisposalTubeComponent, TransformComponent> pipe)
    {
        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(pipe, ref ev);
        return ev.Connectable;
    }
}
