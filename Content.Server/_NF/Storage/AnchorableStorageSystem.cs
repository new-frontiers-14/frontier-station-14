
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Construction.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Nyanotrasen.Item.PseudoItem;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;

namespace Content.Server._NF.Storage;

/// <summary>
/// This is used for restricting anchor operations on storage (one bag max per tile)
/// and ejecting living contents on anchor.
/// </summary>
public sealed class AnchorableStorageSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    private readonly List<EntityUid> _anchoredEntities = new();
    private EntityQuery<AnchorableStorageComponent> _anchorableStorageQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AnchorableStorageComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<AnchorableStorageComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<AnchorableStorageComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        _anchorableStorageQuery = GetEntityQuery<AnchorableStorageComponent>();
    }

    private void OnAnchorStateChanged(Entity<AnchorableStorageComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        if (CheckOverlap((ent, ent.Comp, Transform(ent))))
        {
            _popup.PopupEntity(Loc.GetString("anchored-storage-already-present"), ent);
            _xform.Unanchor(ent, Transform(ent));
            return;
        }

        // Eject any sapient creatures inside the storage.
        // Hack: does not recurse down into bags in bags.
        if (!TryComp(ent.Owner, out StorageComponent? storage))
            return;
        List<EntityUid> entsToRemove = new();
        foreach (var storedItem in storage.StoredItems.Keys)
        {
            if (HasComp<MindContainerComponent>(storedItem) || HasComp<PseudoItemComponent>(storedItem))
                entsToRemove.Add(storedItem);
        }
        foreach (var removeUid in entsToRemove)
        {
            _container.RemoveEntity(ent.Owner, removeUid);
        }
    }

    private void OnAnchorAttempt(Entity<AnchorableStorageComponent> ent, ref AnchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (CheckOverlap((ent, ent.Comp, Transform(ent))))
        {
            _popup.PopupEntity(Loc.GetString("anchored-storage-already-present"), ent, args.User);
            args.Cancel();
        }
    }

    private void OnInsertAttempt(Entity<AnchorableStorageComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // Check for living things, they should not insert when anchored.
        if (HasComp<MindContainerComponent>(args.EntityUid) || HasComp<PseudoItemComponent>(args.EntityUid))
        {
            if (Transform(ent.Owner).Anchored)
                args.Cancel();
        }
    }

    [PublicAPI]
    public bool CheckOverlap(EntityUid uid)
    {
        if (!_anchorableStorageQuery.TryComp(uid, out var node))
            return false;

        return CheckOverlap((uid, node, Transform(uid)));
    }

    public bool CheckOverlap(Entity<AnchorableStorageComponent, TransformComponent> ent)
    {
        if (ent.Comp2.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        var indices = _map.TileIndicesFor(grid, gridComp, ent.Comp2.Coordinates);
        _anchoredEntities.Clear();
        _map.GetAnchoredEntities((grid, gridComp), indices, _anchoredEntities);

        foreach (var otherEnt in _anchoredEntities)
        {
            // Don't match yourself.
            if (otherEnt == ent.Owner)
                continue;

            // If this other entity is anchorable storage, return true.
            if (_anchorableStorageQuery.TryComp(otherEnt, out var otherComp))
                return true;
        }

        return false;
    }
}
