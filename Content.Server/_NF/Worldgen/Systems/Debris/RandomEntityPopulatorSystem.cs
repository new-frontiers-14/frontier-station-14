using System.Linq;
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

    private void OnFloorPlanBuilt(Entity<RandomEntityPopulatorComponent> ent, ref LocalStructureLoadedEvent args)
    {
        if (!TryComp<MapGridComponent>(ent, out var mapGrid))
            return;

        var placeables = new List<string?>(4);
        List<Vector2i>? validTileIndices = null;
        // For each entity populator in the set, select a number between min and max
        foreach (var (paramSet, cache) in ent.Comp.Caches)
        {
            if (!_random.Prob(paramSet.Prob))
                continue;

            var numToGenerate = _random.Next(paramSet.Min, paramSet.Max + 1);
            for (var i = 0; i < numToGenerate; i++)
            {
                // Then find a spot (if we can) - on any failure, assume the asteroid is full and move onto the next one, which may have different parameters
                if (!SelectRandomTile(ent, mapGrid, paramSet.CanBeAirSealed, ref validTileIndices, out var coords))
                    break;

                cache.GetSpawns(_random, ref placeables);

                foreach (var proto in placeables)
                {
                    if (proto is null)
                        continue;

                    Spawn(proto, coords);
                }
                placeables.Clear();
            }
        }
    }

    private bool SelectRandomTile(EntityUid gridUid,
        MapGridComponent mapComp,
        bool canBeAirSealed,
        ref List<Vector2i>? tileIndices,
        out EntityCoordinates targetCoords)
    {
        targetCoords = default;

        if (tileIndices == null)
        {
            var tileIterator = _map.GetAllTiles(gridUid, mapComp, true);
            tileIndices = tileIterator.Select(tile => tile.GridIndices).ToList();
        }

        var found = false;
        for (var i = 0; i < 10; i++)
        {
            if (tileIndices.Count <= 0)
                return false;

            var idx = _random.Next(tileIndices.Count);
            if (!canBeAirSealed && _atmosphere.IsTileAirBlocked(gridUid, tileIndices[idx], mapGridComp: mapComp))
                continue;

            found = true;
            targetCoords = _map.GridTileToLocal(gridUid, mapComp, tileIndices[idx]);
            tileIndices.RemoveAt(idx);
            break;
        }

        return found;
    }
}
