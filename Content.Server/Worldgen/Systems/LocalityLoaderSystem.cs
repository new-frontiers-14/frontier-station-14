using Content.Server.Worldgen.Components;
using Robust.Server.GameObjects;
using Content.Server._NF.Worldgen.Components.Debris; // Frontier
using Content.Shared.Humanoid; // Frontier
using Content.Shared.Mobs.Components; // Frontier
using System.Numerics; // Frontier
using Robust.Shared.Map; // Frontier
using Content.Server._NF.Salvage; // Frontier

using EntityPosition = (Robust.Shared.GameObjects.EntityUid Entity, Robust.Shared.Map.EntityCoordinates Coordinates); // Frontier

namespace Content.Server.Worldgen.Systems;

/// <summary>
///     This handles loading in objects based on distance from player, using some metadata on chunks.
/// </summary>
public sealed class LocalityLoaderSystem : BaseWorldSystem
{
    [Dependency] private readonly TransformSystem _xformSys = default!;

    // Frontier
    private List<(Entity<TransformComponent> Entity, EntityUid MapUid, Vector2 LocalPosition)> _detachEnts = new(); // Frontier
    private EntityQuery<SpaceDebrisComponent> _debrisQuery;
    private readonly List<(EntityUid Debris, List<EntityPosition> Entity)> _terminatingDebris = [];

    public override void Initialize()
    {
        _debrisQuery = GetEntityQuery<SpaceDebrisComponent>();
        SubscribeLocalEvent<SpaceDebrisComponent, EntityTerminatingEvent>(OnDebrisDespawn);
    }
    // Frontier

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        var e = EntityQueryEnumerator<LocalityLoaderComponent, TransformComponent>();
        var loadedQuery = GetEntityQuery<LoadedChunkComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var controllerQuery = GetEntityQuery<WorldControllerComponent>();

        while (e.MoveNext(out var uid, out var loadable, out var xform))
        {
            if (!controllerQuery.TryGetComponent(xform.MapUid, out var controller))
            {
                RaiseLocalEvent(uid, new LocalStructureLoadedEvent());
                RemCompDeferred<LocalityLoaderComponent>(uid);
                continue;
            }

            var coords = GetChunkCoords(uid, xform);
            var done = false;
            for (var i = -1; i < 2 && !done; i++)
            {
                for (var j = -1; j < 2 && !done; j++)
                {
                    var chunk = GetOrCreateChunk(coords + (i, j), xform.MapUid!.Value, controller);
                    if (!loadedQuery.TryGetComponent(chunk, out var loaded) || loaded.Loaders is null)
                        continue;

                    foreach (var loader in loaded.Loaders)
                    {
                        if (!xformQuery.TryGetComponent(loader, out var loaderXform))
                            continue;

                        if ((_xformSys.GetWorldPosition(loaderXform) - _xformSys.GetWorldPosition(xform)).Length() > loadable.LoadingDistance)
                            continue;

                        RaiseLocalEvent(uid, new LocalStructureLoadedEvent());
                        RemCompDeferred<LocalityLoaderComponent>(uid);
                        done = true;
                        break;
                    }
                }
            }
        }
    }

    // Frontier
    private void OnDebrisDespawn(EntityUid entity, SpaceDebrisComponent component, EntityTerminatingEvent e)
    {
        if (entity != null)
        {
            // Handle mobrestrictions getting deleted
            var query = AllEntityQuery<NFSalvageMobRestrictionsComponent>();

            while (query.MoveNext(out var salvUid, out var salvMob))
            {
                if (entity == salvMob.LinkedGridEntity)
                {
                    QueueDel(salvUid);
                }
            }

            var mobQuery = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent, TransformComponent>();
            _detachEnts.Clear();

            while (mobQuery.MoveNext(out var mobUid, out _, out _, out var xform))
            {
                if (xform.GridUid == null || entity != xform.GridUid.Value || xform.MapUid == null)
                    continue;

                // Can't parent directly to map as it runs grid traversal.
                _detachEnts.Add(((mobUid, xform), xform.MapUid.Value, _xformSys.GetWorldPosition(xform)));
                _xformSys.DetachParentToNull(mobUid, xform);
            }

            foreach (var detachEnt in _detachEnts)
            {
                _xformSys.SetCoordinates(detachEnt.Entity.Owner, new EntityCoordinates(detachEnt.MapUid, detachEnt.LocalPosition));
            }
        }
    }
    // Frontier
}

/// <summary>
///     A directed fired on a loadable entity when a local loader enters it's vicinity.
/// </summary>
public record struct LocalStructureLoadedEvent;
