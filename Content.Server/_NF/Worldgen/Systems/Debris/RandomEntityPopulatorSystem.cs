using Content.Server.Atmos.EntitySystems;
using Content.Server.Worldgen.Components.Debris;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Worldgen.Systems.Debris;

/// <summary>
///     This is for placing a finite, random number of entities on separate tiles on a structure.
/// </summary>
public sealed class RandomEntityPopulatorSystem : BaseWorldSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<RandomEntityPopulatorComponent, LocalStructureLoadedEvent>(OnFloorPlanBuilt);
    }

    private void OnFloorPlanBuilt(EntityUid uid, RandomEntityPopulatorComponent component, LocalStructureLoadedEvent args)
    {
        if (!TryComp<MapGridComponent>(uid, out var mapGrid))
            return;

        var placeables = new List<string?>(4);
        // For each entity populator in the set, select a number between min and max
        foreach (var (paramSet, cache) in component.Caches)
        {
            if (!_random.Prob(paramSet.Prob))
                continue;

            var numToGenerate = _random.Next(paramSet.Min, paramSet.Max + 1);
            for (int i = 0; i < numToGenerate; i++)
            {
                // Then find a spot (if we can) - on any failure, assume the asteroid is full and move onto the next one, which may have different parameters
                if (!SelectRandomTile(uid, mapGrid, paramSet.CanBeAirSealed, out var coords))
                    break;

                cache.GetSpawns(_random, ref placeables);

                foreach (var proto in placeables)
                {
                    if (proto is null)
                        continue;

                    Spawn(proto, coords);
                }
            }
        }
    }

    private bool SelectRandomTile(EntityUid gridUid,
        MapGridComponent mapComp,
        bool canBeAirSealed,
        out EntityCoordinates targetCoords)
    {
        targetCoords = default;

        var aabb = mapComp.LocalAABB;

        bool found = false;
        // Try to place on the local bounding box - this may fail.
        for (var i = 0; i < 25; i++)
        {
            var randomX = _random.Next((int)aabb.Left, (int)aabb.Right);
            var randomY = _random.Next((int)aabb.Bottom, (int)aabb.Top);

            var tile = new Vector2i(randomX, randomY);
            if (_atmosphere.IsTileSpace(gridUid, Transform(gridUid).MapUid, tile)
                || !canBeAirSealed && _atmosphere.IsTileAirBlocked(gridUid, tile, mapGridComp: mapComp))
            {
                continue;
            }

            found = true;
            targetCoords = _map.GridTileToLocal(gridUid, mapComp, tile);
            break;
        }

        return found;
    }
}
