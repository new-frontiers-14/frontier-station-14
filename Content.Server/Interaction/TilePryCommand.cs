using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Interaction;

[AdminCommand(AdminFlags.Debug)]
public sealed class TilePryCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "tilepry";
    public string Description => "Pries up all tiles in a radius around the user.";
    public string Help => $"Usage: {Command} <radius>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player?.AttachedEntity is not { } attached)
        {
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!int.TryParse(args[0], out var radius))
        {
            shell.WriteError($"{args[0]} isn't a valid integer.");
            return;
        }

        if (radius < 0)
        {
            shell.WriteError("Radius must be positive.");
            return;
        }

        var mapSystem = _entities.System<SharedMapSystem>();
        var xform = _entities.GetComponent<TransformComponent>(attached);

        var playerGrid = xform.GridUid;

        if (!_entities.TryGetComponent<MapGridComponent>(playerGrid, out var mapGrid))
            return;

        var playerPosition = xform.Coordinates;
        var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();

        for (var i = -radius; i <= radius; i++)
        {
            for (var j = -radius; j <= radius; j++)
            {
                var tile = mapSystem.GetTileRef(playerGrid.Value, mapGrid, playerPosition.Offset(new Vector2(i, j)));
                var coordinates = mapSystem.GridTileToLocal(playerGrid.Value, mapGrid, tile.GridIndices);
                var tileDef = (ContentTileDefinition)tileDefinitionManager[tile.Tile.TypeId];

                if (!tileDef.CanCrowbar) continue;

<<<<<<< HEAD
            if (!int.TryParse(args[0], out var radius))
            {
                shell.WriteLine($"{args[0]} isn't a valid integer.");
                return;
            }

            if (radius < 0)
            {
                shell.WriteLine("Radius must be positive.");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            var xform = _entities.GetComponent<TransformComponent>(attached);
            var playerGrid = xform.GridUid;

            if (!_entities.TryGetComponent<MapGridComponent>(playerGrid, out var mapGrid))
                return;

            var playerPosition = xform.Coordinates;
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();

            for (var i = -radius; i <= radius; i++)
            {
                for (var j = -radius; j <= radius; j++)
                {
                    var tile = mapGrid.GetTileRef(playerPosition.Offset(new Vector2(i, j)));
                    var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);
                    var tileDef = (ContentTileDefinition) tileDefinitionManager[tile.Tile.TypeId];

                    if (tileDef.IsSubFloor) continue;

                    var plating = tileDefinitionManager["Plating"];
                    mapGrid.SetTile(coordinates, new Tile(plating.TileId));
                }
=======
                var plating = tileDefinitionManager["Plating"];
                mapSystem.SetTile(playerGrid.Value, mapGrid, coordinates, new Tile(plating.TileId));
>>>>>>> a7e29f2878a63d62c9c23326e2b8f2dc64d40cc4
            }
        }
    }
}
