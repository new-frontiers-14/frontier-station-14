using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;
using Robust.Shared.Random;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="WallMountDunGen"/>
    /// </summary>
    private async Task PostGen(WallMountDunGen gen, DungeonData data, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        if (!data.Tiles.TryGetValue(DungeonDataKey.FallbackTile, out var tileProto))
        {
            _sawmill.Error($"Tried to run {nameof(WallMountDunGen)} without any dungeon data set which is unsupported");
            return;
        }

        var tileDef = _prototype.Index(tileProto);
        bool validProto = data.SpawnGroups.TryGetValue(DungeonDataKey.WallMounts, out var spawnProto); // Frontier: assign to validProto
        if (!validProto) // Frontier: add error handling
            _sawmill.Warning($"No wallmount spawn group for dungeon type."); // Frontier: add error handling

        var checkedTiles = new HashSet<Vector2i>();
        var allExterior = new HashSet<Vector2i>(dungeon.CorridorExteriorTiles);
        allExterior.UnionWith(dungeon.RoomExteriorTiles);
        var count = 0;

        foreach (var neighbor in allExterior)
        {
            // Occupado
            if (dungeon.RoomTiles.Contains(neighbor) || checkedTiles.Contains(neighbor) || !_anchorable.TileFree(_grid, neighbor, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                continue;

            if (!random.Prob(gen.Prob) || !checkedTiles.Add(neighbor))
                continue;

            _maps.SetTile(_gridUid, _grid, neighbor, _tile.GetVariantTile(tileDef, random));
            var gridPos = _maps.GridTileToLocal(_gridUid, _grid, neighbor);

            if (!validProto) // Frontier: error handling
                continue; // Frontier: error handling
            var protoNames = EntitySpawnCollection.GetSpawns(_prototype.Index(spawnProto).Entries, random);

            _entManager.SpawnEntities(gridPos, protoNames);
            count += protoNames.Count;

            if (count > 20)
            {
                count -= 20;
                await SuspendDungeon();

                if (!ValidateResume())
                    return;
            }
        }
    }
}
