using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._NF.Salvage; // Frontier: job complete event
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.CPUJob.JobQueues;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Atmos;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Dataset;
using Content.Shared.Gravity;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Loot;
using Content.Shared.Salvage;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Content.Shared.Shuttles.Components;
using Content.Shared.Storage;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Server.Shuttles.Components;
using Content.Shared.Coordinates;

namespace Content.Server.Salvage;

public sealed class SpawnSalvageMissionJob : Job<bool>
{
    private readonly IEntityManager _entManager;
    private readonly IGameTiming _timing;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _prototypeManager;
    private readonly AnchorableSystem _anchorable;
    private readonly BiomeSystem _biome;
    private readonly DungeonSystem _dungeon;
    private readonly MetaDataSystem _metaData;
    private readonly ShuttleSystem _shuttle;
    private readonly StationSystem _stationSystem;
    private readonly SalvageSystem _salvage;
    private readonly SharedTransformSystem _xforms;
    private readonly SharedMapSystem _map;

    public readonly EntityUid Station;
    public readonly EntityUid? CoordinatesDisk;
    private readonly SalvageMissionParams _missionParams;

    public SpawnSalvageMissionJob(
        double maxTime,
        IEntityManager entManager,
        IGameTiming timing,
        IMapManager mapManager,
        IPrototypeManager protoManager,
        AnchorableSystem anchorable,
        BiomeSystem biome,
        DungeonSystem dungeon,
        ShuttleSystem shuttle,
        StationSystem stationSystem,
        MetaDataSystem metaData,
        SalvageSystem salvage,
        SharedTransformSystem xform,
        SharedMapSystem map,
        EntityUid station,
        EntityUid? coordinatesDisk,
        SalvageMissionParams missionParams,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _entManager = entManager;
        _timing = timing;
        _mapManager = mapManager;
        _prototypeManager = protoManager;
        _anchorable = anchorable;
        _biome = biome;
        _dungeon = dungeon;
        _shuttle = shuttle;
        _stationSystem = stationSystem;
        _metaData = metaData;
        _salvage = salvage;
        _xforms = xform;
        _map = map;
        Station = station;
        CoordinatesDisk = coordinatesDisk;
        _missionParams = missionParams;
    }

    protected override async Task<bool> Process()
    {
        // Frontier: gracefully handle expedition failures
        bool success = true;
        string? errorStackTrace = null;
        try
        {
            await InternalProcess().ContinueWith((t) => { success = false; errorStackTrace = t.Exception?.InnerException?.StackTrace; }, TaskContinuationOptions.OnlyOnFaulted);
        }
        finally
        {
            ExpeditionSpawnCompleteEvent ev = new(Station, success, _missionParams.Index);
            _entManager.EventBus.RaiseLocalEvent(Station, ev); // We have no idea who spawned this, so broadcast our success/failure.
            if (errorStackTrace != null)
                Logger.ErrorS("salvage", $"Expedition generation failed with exception: {errorStackTrace}!");
        }
        return success;
        // End Frontier: gracefully handle expedition failures
    }

    private async Task<bool> InternalProcess() // Frontier: make process an internal function (for a try block indenting an entire)
    {
        Logger.DebugS("salvage", $"Spawning salvage mission with seed {_missionParams.Seed}");
        var config = _missionParams.MissionType;
        var mapUid = _map.CreateMap(out var mapId, runMapInit: false);
        MetaDataComponent? metadata = null;
        var grid = _entManager.EnsureComponent<MapGridComponent>(mapUid);
        var random = new Random(_missionParams.Seed);
        var destComp = _entManager.AddComponent<FTLDestinationComponent>(mapUid);
        destComp.BeaconsOnly = true;
        destComp.RequireCoordinateDisk = true;
        destComp.Enabled = true;
        _metaData.SetEntityName(mapUid, SharedSalvageSystem.GetFTLName(_prototypeManager.Index<DatasetPrototype>("names_borer"), _missionParams.Seed));
        _entManager.AddComponent<FTLBeaconComponent>(mapUid);

        // Saving the mission mapUid to a CD is made optional, in case one is somehow made in a process without a CD entity
        if (CoordinatesDisk.HasValue)
        {
            var cd = _entManager.EnsureComponent<ShuttleDestinationCoordinatesComponent>(CoordinatesDisk.Value);
            cd.Destination = mapUid;
            _entManager.Dirty(CoordinatesDisk.Value, cd);
        }

        // Setup mission configs
        // As we go through the config the rating will deplete so we'll go for most important to least important.

        var mission = _entManager.System<SharedSalvageSystem>()
            .GetMission(_missionParams.MissionType, _missionParams.Difficulty, _missionParams.Seed);

        var missionBiome = _prototypeManager.Index<SalvageBiomeMod>(mission.Biome);
        BiomeComponent? biome = null;

        if (missionBiome.BiomePrototype != null)
        {
            biome = _entManager.AddComponent<BiomeComponent>(mapUid);
            var biomeSystem = _entManager.System<BiomeSystem>();
            biomeSystem.SetTemplate(mapUid, biome, _prototypeManager.Index<BiomeTemplatePrototype>(missionBiome.BiomePrototype));
            biomeSystem.SetSeed(mapUid, biome, mission.Seed);
            _entManager.Dirty(biome);

            // Gravity
            var gravity = _entManager.EnsureComponent<GravityComponent>(mapUid);
            gravity.Enabled = true;
            _entManager.Dirty(gravity, metadata);

            // Atmos
            var air = _prototypeManager.Index<SalvageAirMod>(mission.Air);
            // copy into a new array since the yml deserialization discards the fixed length
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            air.Gases.CopyTo(moles, 0);
            var atmos = _entManager.EnsureComponent<MapAtmosphereComponent>(mapUid);
            _entManager.System<AtmosphereSystem>().SetMapSpace(mapUid, air.Space, atmos);
            _entManager.System<AtmosphereSystem>().SetMapGasMixture(mapUid, new GasMixture(moles, mission.Temperature), atmos);

            if (mission.Color != null)
            {
                var lighting = _entManager.EnsureComponent<MapLightComponent>(mapUid);
                lighting.AmbientLightColor = mission.Color.Value;
                _entManager.Dirty(lighting);
            }
        }

        _mapManager.DoMapInitialize(mapId);
        _mapManager.SetMapPaused(mapId, true);

        // Setup expedition
        var expedition = _entManager.AddComponent<SalvageExpeditionComponent>(mapUid);
        expedition.Station = Station;
        expedition.EndTime = _timing.CurTime + mission.Duration;
        expedition.MissionParams = _missionParams;
        expedition.Difficulty = _missionParams.Difficulty;
        expedition.Rewards = mission.Rewards;

        // On Frontier, we cant share our locations it breaks ftl in a bad bad way
        // Don't want consoles to have the incorrect name until refreshed.
        /*var ftlUid = _entManager.CreateEntityUninitialized("FTLPoint", new EntityCoordinates(mapUid, grid.TileSizeHalfVector));
        _metaData.SetEntityName(ftlUid, SharedSalvageSystem.GetFTLName(_prototypeManager.Index<DatasetPrototype>("names_borer"), _missionParams.Seed));
        _entManager.InitializeAndStartEntity(ftlUid);*/

        // so we just gunna yeet them there instead why not. they chose this life.
        /*var stationData = _entManager.GetComponent<StationDataComponent>(Station);
        var shuttleUid = _stationSystem.GetLargestGrid(stationData);
        if (shuttleUid is { Valid : true } vesselUid)
        {
            var shuttle = _entManager.GetComponent<ShuttleComponent>(vesselUid);
            _shuttle.FTLToCoordinates(vesselUid, shuttle, new EntityCoordinates(mapUid, Vector2.Zero), 0f, 5.5f, 50f);
        }*/

        var landingPadRadius = 4; // Frontier: 24<4 - using this as a margin (4-16), not a radius
        var minDungeonOffset = landingPadRadius + 4;

        // We'll use the dungeon rotation as the spawn angle
        var dungeonRotation = _dungeon.GetDungeonRotation(_missionParams.Seed);

        Dungeon dungeon = default!; // Frontier: explicitly type as Dungeon

        Vector2 dungeonOffset = new Vector2(); // Frontier: needed for dungeon offset
        if (config != SalvageMissionType.Mining) // Frontier: why?
        {
            var maxDungeonOffset = minDungeonOffset + 12;
            var dungeonOffsetDistance = minDungeonOffset + (maxDungeonOffset - minDungeonOffset) * random.NextFloat();
            dungeonOffset = new Vector2(0f, dungeonOffsetDistance);
            dungeonOffset = dungeonRotation.RotateVec(dungeonOffset);
            var dungeonMod = _prototypeManager.Index<SalvageDungeonModPrototype>(mission.Dungeon);
            var dungeonConfig = _prototypeManager.Index(dungeonMod.Proto);
            var dungeons = await WaitAsyncTask(_dungeon.GenerateDungeonAsync(dungeonConfig, mapUid, grid, (Vector2i) dungeonOffset,
                    _missionParams.Seed));

            dungeon = dungeons.First();

            // Aborty
            if (dungeon.Rooms.Count == 0)
            {
                return false;
            }

            expedition.DungeonLocation = dungeonOffset;
        }

        // Frontier: get map bounding box
        Box2 dungeonBox = new Box2(dungeonOffset, dungeonOffset);
        foreach (var tile in dungeon.AllTiles)
        {
            dungeonBox = dungeonBox.ExtendToContain(tile);
        }

        var stationData = _entManager.GetComponent<StationDataComponent>(Station);

        // Frontier: get ship bounding box relative to largest grid coords
        var shuttleUid = _stationSystem.GetLargestGrid(stationData);
        Box2 shuttleBox = new Box2();

        if (shuttleUid is { Valid: true } vesselUid &&
            _entManager.TryGetComponent<MapGridComponent>(vesselUid, out var gridComp))
        {
            shuttleBox = gridComp.LocalAABB;
        }

        // Frontier: offset ship spawn point from bounding boxes
        Vector2 dungeonProjection = new Vector2(dungeonBox.Width * (float) -Math.Sin(dungeonRotation) / 2, dungeonBox.Height * (float) Math.Cos(dungeonRotation) / 2); // Project boxes to get relevant offset for dungeon rotation.
        Vector2 shuttleProjection = new Vector2(shuttleBox.Width * (float) -Math.Sin(dungeonRotation) / 2, shuttleBox.Height * (float) Math.Cos(dungeonRotation) / 2); // Note: sine is negative because of CCW rotation (starting north, then west)
        Vector2 coords = dungeonBox.Center - dungeonProjection - dungeonOffset - shuttleProjection - shuttleBox.Center; // Coordinates to spawn the ship at to center it with the dungeon's bounding boxes
        coords = coords.Rounded(); // Ensure grid is aligned to map coords

        // Frontier: delay ship FTL
        if (shuttleUid is { Valid: true })
        {
            var shuttle = _entManager.GetComponent<ShuttleComponent>(shuttleUid.Value);
            _shuttle.FTLToCoordinates(shuttleUid.Value, shuttle, new EntityCoordinates(mapUid, coords), 0f, 5.5f, 50f);
        }

        List<Vector2i> reservedTiles = new();

        // Frontier: no need for intersecting tiles, we offset the map

        // Vector2 clearBoxCenter = dungeonBox.Center - dungeonProjection - dungeonOffset - shuttleProjection;
        // float clearBoxHalfWidth = shuttleBox.Width / 2.0f + 4.0f;
        // float clearBoxHalfHeight = shuttleBox.Height / 2.0f + 4.0f;
        // Box2 shuttleClearBox = new Box2(clearBoxCenter.X - clearBoxHalfWidth,
        //     clearBoxCenter.Y - clearBoxHalfHeight,
        //     clearBoxCenter.X + clearBoxHalfWidth,
        //     clearBoxCenter.Y + clearBoxHalfHeight);

        // foreach (var tile in _map.GetTilesIntersecting(mapUid, grid, shuttleClearBox, false))
        // {
        //     if (!_biome.TryGetBiomeTile(mapUid, grid, tile.GridIndices, out _))
        //         continue;

        //     reservedTiles.Add(tile.GridIndices);
        // }
        // End Frontier

        // Mission setup
        switch (config)
        {
            case SalvageMissionType.Mining:
                await SetupMining(mission, mapUid);
                break;
            case SalvageMissionType.Destruction:
                await SetupStructure(mission, dungeon, mapUid, grid, random);
                break;
            case SalvageMissionType.Elimination:
                await SetupElimination(mission, dungeon, mapUid, grid, random);
                break;
            default:
                throw new NotImplementedException();
        }

        // Handle loot
        // We'll always add this loot if possible
        foreach (var lootProto in _prototypeManager.EnumeratePrototypes<SalvageLootPrototype>())
        {
            if (!lootProto.Guaranteed)
                continue;
            await SpawnDungeonLoot(dungeon, missionBiome, lootProto, mapUid, grid, random, reservedTiles);
        }
        return true;
    }

    private async Task SpawnDungeonLoot(Dungeon? dungeon, SalvageBiomeMod biomeMod, SalvageLootPrototype loot, EntityUid gridUid, MapGridComponent grid, Random random, List<Vector2i> reservedTiles)
    {
        for (var i = 0; i < loot.LootRules.Count; i++)
        {
            var rule = loot.LootRules[i];

            switch (rule)
            {
                case BiomeMarkerLoot biomeLoot:
                    {
                        if (_entManager.TryGetComponent<BiomeComponent>(gridUid, out var biome) &&
                            biomeLoot.Prototype.TryGetValue(biomeMod.ID, out var mod))
                        {
                            _biome.AddMarkerLayer(gridUid, biome, mod);
                        }
                    }
                    break;
                case BiomeTemplateLoot biomeLoot:
                    {
                        if (_entManager.TryGetComponent<BiomeComponent>(gridUid, out var biome))
                        {
                            _biome.AddTemplate(gridUid, biome, "Loot", _prototypeManager.Index<BiomeTemplatePrototype>(biomeLoot.Prototype), i);
                        }
                    }
                    break;
            }
        }
    }

    #region Mission Specific

    private async Task SetupMining(
        SalvageMission mission,
        EntityUid gridUid)
    {
        var faction = _prototypeManager.Index<SalvageFactionPrototype>(mission.Faction);

        if (_entManager.TryGetComponent<BiomeComponent>(gridUid, out var biome))
        {
            // TODO: Better
            for (var i = 0; i < _salvage.GetDifficulty(mission.Difficulty); i++)
            {
                _biome.AddMarkerLayer(gridUid, biome, faction.Configs["Mining"]);
            }
        }
    }

    private async Task SetupStructure(
        SalvageMission mission,
        Dungeon dungeon,
        EntityUid gridUid,
        MapGridComponent grid,
        Random random)
    {
        var structureComp = _entManager.EnsureComponent<SalvageStructureExpeditionComponent>(gridUid);
        var availableRooms = dungeon.Rooms.ToList();
        var faction = _prototypeManager.Index<SalvageFactionPrototype>(mission.Faction);
        await SpawnMobsRandomRooms(mission, dungeon, faction, grid, random);

        var structureCount = _salvage.GetStructureCount(mission.Difficulty);
        var shaggy = faction.Configs["DefenseStructure"];
        var validSpawns = new List<Vector2i>();

        // Spawn the objectives
        for (var i = 0; i < structureCount; i++)
        {
            var structureRoom = availableRooms[random.Next(availableRooms.Count)];
            validSpawns.Clear();
            validSpawns.AddRange(structureRoom.Tiles);
            random.Shuffle(validSpawns);

            while (validSpawns.Count > 0)
            {
                var spawnTile = validSpawns[^1];
                validSpawns.RemoveAt(validSpawns.Count - 1);

                if (!_anchorable.TileFree(grid, spawnTile, (int) CollisionGroup.MachineLayer,
                        (int) CollisionGroup.MachineLayer))
                {
                    continue;
                }

                var spawnPosition = grid.GridTileToLocal(spawnTile);
                var uid = _entManager.SpawnEntity(shaggy, spawnPosition);
                _entManager.AddComponent<SalvageStructureComponent>(uid);
                structureComp.Structures.Add(uid);
                break;
            }
        }
    }

    private async Task SetupElimination(
        SalvageMission mission,
        Dungeon dungeon,
        EntityUid gridUid,
        MapGridComponent grid,
        Random random)
    {
        // spawn megafauna in a random place
        var roomIndex = random.Next(dungeon.Rooms.Count);
        var room = dungeon.Rooms[roomIndex];
        var tile = room.Tiles.ElementAt(random.Next(room.Tiles.Count));
        var position = grid.GridTileToLocal(tile);

        var faction = _prototypeManager.Index<SalvageFactionPrototype>(mission.Faction);
        var prototype = faction.Configs["Megafauna"];
        var uid = _entManager.SpawnEntity(prototype, position);
        // not removing ghost role since its 1 megafauna, expect that you won't be able to cheese it.
        var eliminationComp = _entManager.EnsureComponent<SalvageEliminationExpeditionComponent>(gridUid);
        eliminationComp.Megafauna.Add(uid);

        // spawn less mobs than usual since there's megafauna to deal with too
        await SpawnMobsRandomRooms(mission, dungeon, faction, grid, random, 0.5f);
    }

    private async Task SpawnMobsRandomRooms(SalvageMission mission, Dungeon dungeon, SalvageFactionPrototype faction, MapGridComponent grid, Random random, float scale = 1f)
    {
        // scale affects how many groups are spawned, not the size of the groups themselves
        var groupSpawns = _salvage.GetSpawnCount(mission.Difficulty) * scale;
        var groupSum = faction.MobGroups.Sum(o => o.Prob);
        var validSpawns = new List<Vector2i>();

        for (var i = 0; i < groupSpawns; i++)
        {
            var roll = random.NextFloat() * groupSum;
            var value = 0f;

            foreach (var group in faction.MobGroups)
            {
                value += group.Prob;

                if (value < roll)
                    continue;

                var mobGroupIndex = random.Next(faction.MobGroups.Count);
                var mobGroup = faction.MobGroups[mobGroupIndex];

                var spawnRoomIndex = random.Next(dungeon.Rooms.Count);
                var spawnRoom = dungeon.Rooms[spawnRoomIndex];
                validSpawns.Clear();
                validSpawns.AddRange(spawnRoom.Tiles);
                random.Shuffle(validSpawns);

                foreach (var entry in EntitySpawnCollection.GetSpawns(mobGroup.Entries, random))
                {
                    while (validSpawns.Count > 0)
                    {
                        var spawnTile = validSpawns[^1];
                        validSpawns.RemoveAt(validSpawns.Count - 1);

                        if (!_anchorable.TileFree(grid, spawnTile, (int) CollisionGroup.MachineLayer,
                                (int) CollisionGroup.MachineLayer))
                        {
                            continue;
                        }

                        var spawnPosition = grid.GridTileToLocal(spawnTile);

                        var uid = _entManager.CreateEntityUninitialized(entry, spawnPosition);
                        _entManager.RemoveComponent<GhostTakeoverAvailableComponent>(uid);
                        _entManager.RemoveComponent<GhostRoleComponent>(uid);
                        _entManager.InitializeAndStartEntity(uid);

                        break;
                    }
                }

                await SuspendIfOutOfTime();
                break;
            }
        }
    }

    #endregion
}
