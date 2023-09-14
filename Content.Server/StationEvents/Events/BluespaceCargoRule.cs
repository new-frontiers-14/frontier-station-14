using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Physics;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.CCVar;

namespace Content.Server.StationEvents.Events;

public sealed class BluespaceCargoRule : StationEventSystem<BluespaceCargoRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;
    //[Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    //[Dependency] private readonly IMapManager _mapManager = default!;

    protected override void Added(EntityUid uid, BluespaceCargoRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        var str = Loc.GetString("bluespace-cargo-event-announcement");
        ChatSystem.DispatchGlobalAnnouncement(str, colorOverride: Color.FromHex("#18abf5"));
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

        var amountToSpawn = Math.Max(1, (int) MathF.Round(GetSeverityModifier() / 1.5f));
        for (var i = 0; i < amountToSpawn; i++)
        {
            SpawnOnRandomGridLocation(grid.Value, component.CargoSpawnerPrototype, component.CargoGenericSpawnerPrototype, component.CargoFlashPrototype);
        }
    }

    public void SpawnOnRandomGridLocation(EntityUid grid, string toSpawn, string toSpawnGeneric, string toSpawnFlash)
    {
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var xform = Transform(grid);

        var toSpawnCrate = toSpawn;
        var targetCoords = xform.Coordinates;
        var gridBounds = gridComp.LocalAABB.Scale(_configuration.GetCVar(CCVars.CargoGenerationGridBoundsScale));

        for (var i = 0; i < 25; i++)
        {
            var randomX = _random.Next((int) gridBounds.Left, (int) gridBounds.Right);
            var randomY = _random.Next((int) gridBounds.Bottom, (int) gridBounds.Top);

            var tile = new Vector2i(randomX, randomY);

            // no air-blocked areas.
            if (_atmosphere.IsTileSpace(grid, xform.MapUid, tile, mapGridComp: gridComp) ||
                _atmosphere.IsTileAirBlocked(grid, tile, mapGridComp: gridComp))
            {
                continue;
            }

            if (_atmosphere.IsTileMixtureProbablySafe(grid, grid, tile))
            {
                toSpawnCrate = toSpawn;
            }
            else
            {
                toSpawnCrate = toSpawnGeneric; // Dont let an animal die!
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

            targetCoords = gridComp.GridTileToLocal(tile);
            break;
        }

        Spawn(toSpawnCrate, targetCoords);
        Spawn(toSpawnFlash, targetCoords);

        Sawmill.Info($"Spawning random cargo at {targetCoords}");
    }
}
