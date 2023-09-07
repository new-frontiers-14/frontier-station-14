using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Physics;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Content.Server.Station.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Map;
using Content.Shared.Maps;

namespace Content.Server.StationEvents.Events;

public sealed class BluespaceCargoRule : StationEventSystem<BluespaceCargoRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    protected override void Added(EntityUid uid, BluespaceCargoRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        var str = Loc.GetString("bluespace-cargo-event-announcement",
            ("sighting", Loc.GetString(RobustRandom.Pick(component.PossibleSighting))));
        ChatSystem.DispatchGlobalAnnouncement(str, colorOverride: Color.FromHex("#18abf5"));
    }

    protected override void Started(EntityUid uid, BluespaceCargoRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var amountToSpawn = Math.Max(1, (int) MathF.Round(GetSeverityModifier() / 1.5f));
        for (var i = 0; i < amountToSpawn; i++)
        {
            if (!TryFindRandomTile(out _, out _, out _, out var coords))
                return;

            Spawn(component.CargoSpawnerPrototype, coords);
            Spawn(component.CargoFlashPrototype, coords);

            Sawmill.Info($"Spawning random cargo at {coords}");
        }
    }

    public bool TryFindRandomTile(EntityUid targetGrid, EntityUid targetMap, int maxAttempts, out EntityCoordinates targetCoords)
    {
        targetCoords = EntityCoordinates.Invalid;

        //if (!TryGetRandomStation(out var targetGrid, HasComp<StationJobsComponent>))
        //    return;



        if (!TryGetRandomStation(out var grid, HasComp<StationJobsComponent>))
            return false;

        var xform = Transform(targetGrid.Value);

        if (!grid.TryGetTileRef(xform.Coordinates, out var tileRef))
            return false;

        var tile = tileRef.GridIndices;

        var found = false;
        var (gridPos, _, gridMatrix) = xform.GetWorldPositionRotationMatrix();
        var gridBounds = gridMatrix.TransformBox(grid.LocalAABB);

        //Obviously don't put anything ridiculous in here
        for (var i = 0; i < maxAttempts; i++)
        {
            var randomX = _random.Next((int) gridBounds.Left, (int) gridBounds.Right);
            var randomY = _random.Next((int) gridBounds.Bottom, (int) gridBounds.Top);

            tile = new Vector2i(randomX - (int) gridPos.X, randomY - (int) gridPos.Y);
            var mapPos = grid.GridTileToWorldPos(tile);
            var mapTarget = grid.WorldToTile(mapPos);
            var circle = new Circle(mapPos, 2);

            foreach (var newTileRef in grid.GetTilesIntersecting(circle))
            {
                if (newTileRef.IsSpace(_tileDefinitionManager) || newTileRef.IsBlockedTurf(true) || !_atmosphere.IsTileMixtureProbablySafe(targetGrid, targetMap, mapTarget))
                    continue;

                found = true;
                targetCoords = grid.GridTileToLocal(tile);
                break;
            }

            //Found a safe tile, no need to continue
            if (found)
                break;
        }

        if (!found)
            return false;

        return true;
    }
}
