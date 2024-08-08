using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Physics;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.CCVar;

namespace Content.Server.StationEvents.Events;

public sealed class BluespaceCargoRule : StationEventSystem<BluespaceCargoRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;

    protected override void Added(EntityUid uid, BluespaceCargoRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);
    }

    protected override void Started(EntityUid uid, BluespaceCargoRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation, HasComp<StationJobsComponent>))
            return;

        if (!TryComp<StationDataComponent>(chosenStation, out var stationData))
            return;

        var grid = StationSystem.GetLargestGrid(stationData);

        if (grid is null)
            return;

        var amountToSpawn = _random.Next(component.MinimumSpawns, component.MaximumSpawns + 1); // +1 required: [min, max)
        for (var i = 0; i < amountToSpawn; i++)
        {
            SpawnOnRandomGridLocation(grid.Value, component.SpawnerPrototype, component.FlashPrototype, component.RequireSafeAtmosphere);
        }
    }

    public void SpawnOnRandomGridLocation(EntityUid grid, string toSpawn, string toSpawnFlash, bool safeAtmosphere)
    {
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var xform = Transform(grid);

        var targetCoords = xform.Coordinates;
        var gridBounds = gridComp.LocalAABB.Scale(_configuration.GetCVar(CCVars.CargoGenerationGridBoundsScale));

        for (var i = 0; i < 25; i++)
        {
            var randomX = _random.Next((int) gridBounds.Left, (int) gridBounds.Right);
            var randomY = _random.Next((int) gridBounds.Bottom, (int) gridBounds.Top);

            var tile = new Vector2i(randomX, randomY);

            // no air-blocked areas.
            if (_atmosphere.IsTileSpace(grid, xform.MapUid, tile) ||
                _atmosphere.IsTileAirBlocked(grid, tile, mapGridComp: gridComp))
            {
                continue;
            }

            // don't spawn inside of solid objects
            var physQuery = GetEntityQuery<PhysicsComponent>();
            var valid = true;
            foreach (var ent in gridComp.GetAnchoredEntities(tile))
            {
                if (!physQuery.TryGetComponent(ent, out var body))
                    continue;
                if (body.BodyType != BodyType.Static ||
                    !body.Hard ||
                    (body.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                    continue;

                valid = false;
                break;
            }
            if (!valid)
                continue;

            if (safeAtmosphere && !_atmosphere.IsTileMixtureProbablySafe(grid, grid, tile))
            {
                continue;
            }

            targetCoords = gridComp.GridTileToLocal(tile);
            break;
        }

        Spawn(toSpawn, targetCoords);
        Spawn(toSpawnFlash, targetCoords);

        Sawmill.Info($"Spawning random cargo at {targetCoords}");
    }
}
