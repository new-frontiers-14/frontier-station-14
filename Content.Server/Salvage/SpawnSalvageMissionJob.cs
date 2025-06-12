using System.Collections;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.CPUJob.JobQueues;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Shared.Atmos;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Dataset;
using Content.Shared.Gravity;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Loot;
using Content.Shared.Random;
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
using Content.Server._NF.Salvage.Expeditions; // Frontier
using Content.Server.Station.Components; // Frontier
using Content.Server.Station.Systems; // Frontier
using Content.Server.Shuttles.Systems;
using Content.Server._NF.Salvage.Expeditions.Structure; // Frontier

namespace Content.Server.Salvage;

public sealed class SpawnSalvageMissionJob : Job<bool>
{
    private readonly IEntityManager _entManager;
    private readonly IGameTiming _timing;
    private readonly IPrototypeManager _prototypeManager;
    private readonly AnchorableSystem _anchorable;
    private readonly BiomeSystem _biome;
    private readonly DungeonSystem _dungeon;
    private readonly MetaDataSystem _metaData;
    private readonly SharedMapSystem _map;
    private readonly StationSystem _station; // Frontier
    private readonly ShuttleSystem _shuttle; // Frontier
    private readonly SalvageSystem _salvage; // Frontier

    public readonly EntityUid Station;
    public readonly EntityUid? CoordinatesDisk;
    private readonly SalvageMissionParams _missionParams;

    private readonly ISawmill _sawmill;

    // Frontier: Used for saving state between async job
#pragma warning disable IDE1006 // suppressing prefix warnings to reduce merge conflict area
    private EntityUid mapUid = EntityUid.Invalid;
#pragma warning restore IDE1006
    private static readonly ProtoId<SalvageDifficultyPrototype> FallbackDifficulty = "NFModerate";
    // End Frontier

    public SpawnSalvageMissionJob(
        double maxTime,
        IEntityManager entManager,
        IGameTiming timing,
        ILogManager logManager,
        IPrototypeManager protoManager,
        AnchorableSystem anchorable,
        BiomeSystem biome,
        DungeonSystem dungeon,
        MetaDataSystem metaData,
        SharedMapSystem map,
        StationSystem stationSystem, // Frontier
        ShuttleSystem shuttleSystem, // Frontier
        SalvageSystem salvageSystem, // Frontier
        EntityUid station,
        EntityUid? coordinatesDisk,
        SalvageMissionParams missionParams,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _entManager = entManager;
        _timing = timing;
        _prototypeManager = protoManager;
        _anchorable = anchorable;
        _biome = biome;
        _dungeon = dungeon;
        _metaData = metaData;
        _map = map;
        _station = stationSystem; // Frontier
        _shuttle = shuttleSystem; // Frontier
        _salvage = salvageSystem; // Frontier
        Station = station;
        CoordinatesDisk = coordinatesDisk;
        _missionParams = missionParams;
        _sawmill = logManager.GetSawmill("salvage_job");
#if !DEBUG
        _sawmill.Level = LogLevel.Info;
#endif
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
            _entManager.EventBus.RaiseLocalEvent(Station, ev);
            if (errorStackTrace != null)
                _sawmill.Error("salvage", $"Expedition generation failed with exception: {errorStackTrace}!");
            if (!success)
            {
                // Invalidate station, expedition cancellation will be handled by task handler
                if (_entManager.TryGetComponent<SalvageExpeditionComponent>(mapUid, out var salvage))
                    salvage.Station = EntityUid.Invalid;

                _entManager.QueueDeleteEntity(mapUid);
            }
        }
        return success;
        // End Frontier: gracefully handle expedition failures
    }

    private async Task<bool> InternalProcess() // Frontier: make process an internal function (for a try block indenting an entire), add "out EntityUid mapUid" param
    {
        _sawmill.Debug("salvage", $"Spawning salvage mission with seed {_missionParams.Seed}");
        mapUid = _map.CreateMap(out var mapId, runMapInit: false); // Frontier: remove var
        MetaDataComponent? metadata = null;
        var grid = _entManager.EnsureComponent<MapGridComponent>(mapUid);
        var random = new Random(_missionParams.Seed);
        var destComp = _entManager.AddComponent<FTLDestinationComponent>(mapUid);
        destComp.BeaconsOnly = true;
        destComp.RequireCoordinateDisk = true;
        destComp.Enabled = true;
        _metaData.SetEntityName(
            mapUid,
            _entManager.System<SharedSalvageSystem>().GetFTLName(_prototypeManager.Index<LocalizedDatasetPrototype>("NamesBorer"), _missionParams.Seed));
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
        // Frontier: custom difficulty
        if (!_prototypeManager.TryIndex<SalvageDifficultyPrototype>(_missionParams.Difficulty, out var difficultyProto))
            difficultyProto = _prototypeManager.Index<SalvageDifficultyPrototype>(FallbackDifficulty);
        // End Frontier

        var mission = _entManager.System<SharedSalvageSystem>()
            .GetMission(_missionParams.MissionType, difficultyProto, _missionParams.Seed); // Frontier: add MissionType

        var missionBiome = _prototypeManager.Index<SalvageBiomeModPrototype>(mission.Biome);

        if (missionBiome.BiomePrototype != null)
        {
            var biome = _entManager.AddComponent<BiomeComponent>(mapUid);
            var biomeSystem = _entManager.System<BiomeSystem>();
            biomeSystem.SetTemplate(mapUid, biome, _prototypeManager.Index<BiomeTemplatePrototype>(missionBiome.BiomePrototype));
            biomeSystem.SetSeed(mapUid, biome, mission.Seed);
            _entManager.Dirty(mapUid, biome);

            // Gravity
            var gravity = _entManager.EnsureComponent<GravityComponent>(mapUid);
            gravity.Enabled = true;
            _entManager.Dirty(mapUid, gravity, metadata);

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
                _entManager.Dirty(mapUid, lighting);
            }
        }

        _map.InitializeMap(mapId);
        _map.SetPaused(mapUid, true);

        // Setup expedition
        var expedition = _entManager.AddComponent<SalvageExpeditionComponent>(mapUid);
        expedition.Station = Station;
        expedition.EndTime = _timing.CurTime + mission.Duration;
        expedition.MissionParams = _missionParams;

        var landingPadRadius = 4; // Frontier: 24<4 - using this as a margin (4-16), not a radius
        var minDungeonOffset = landingPadRadius + 4;

        // We'll use the dungeon rotation as the spawn angle
        var dungeonRotation = _dungeon.GetDungeonRotation(_missionParams.Seed);

        var maxDungeonOffset = minDungeonOffset + 12;
        var dungeonOffsetDistance = minDungeonOffset + (maxDungeonOffset - minDungeonOffset) * random.NextFloat();
        var dungeonOffset = new Vector2(0f, dungeonOffsetDistance);
        dungeonOffset = dungeonRotation.RotateVec(dungeonOffset);
        var dungeonMod = _prototypeManager.Index<SalvageDungeonModPrototype>(mission.Dungeon);
        var dungeonConfig = _prototypeManager.Index(dungeonMod.Proto);
        var dungeons = await WaitAsyncTask(_dungeon.GenerateDungeonAsync(dungeonConfig, dungeonMod.Proto, mapUid, grid, (Vector2i)dungeonOffset, // Frontier: add dungeonMod.Proto
            _missionParams.Seed));

        var dungeon = dungeons.First();

        // Aborty
        if (dungeon.Rooms.Count == 0)
        {
            return false;
        }

        expedition.DungeonLocation = dungeonOffset;

        // Frontier: map generation and offset
        #region Frontier map generation

        // Get map bounding box
        Box2 dungeonBox = new Box2(dungeonOffset, dungeonOffset);
        foreach (var tile in dungeon.AllTiles)
        {
            dungeonBox = dungeonBox.ExtendToContain(tile);
        }

        var stationData = _entManager.GetComponent<StationDataComponent>(Station);

        // Get ship bounding box relative to largest grid coords
        var shuttleUid = _station.GetLargestGrid(stationData);
        Box2 shuttleBox = new Box2();

        if (shuttleUid is { Valid: true } vesselUid &&
            _entManager.TryGetComponent<MapGridComponent>(vesselUid, out var gridComp))
        {
            shuttleBox = gridComp.LocalAABB;
        }

        // Offset ship spawn point from bounding boxes
        float sin = (float)Math.Sin(dungeonRotation);
        float cos = (float)Math.Cos(dungeonRotation);
        Vector2 dungeonProjection = new Vector2(dungeonBox.Width * -sin / 2, dungeonBox.Height * cos / 2); // Project boxes to get relevant offset for dungeon rotation.
        Vector2 shuttleProjection = new Vector2(shuttleBox.Width * -sin / 2, shuttleBox.Height * cos / 2); // Note: sine is negative because of CCW rotation (starting north, then west)
        Vector2 coords = dungeonBox.Center - dungeonProjection - dungeonOffset - shuttleProjection - shuttleBox.Center; // Coordinates to spawn the ship at to center it with the dungeon's bounding boxes
        coords = coords.Rounded(); // Ensure grid is aligned to map coords

        // List<Vector2i> reservedTiles = new();

        // foreach (var tile in _map.GetTilesIntersecting(mapUid, grid, new Circle(Vector2.Zero, landingPadRadius), false))
        // {
        //     if (!_biome.TryGetBiomeTile(mapUid, grid, tile.GridIndices, out _))
        //         continue;

        //     reservedTiles.Add(tile.GridIndices);
        // }
        #endregion Frontier map generation
        // End Frontier: map generation and offset

        // Frontier: mission setup
        switch (_missionParams.MissionType)
        {
            case SalvageMissionType.Destruction:
                await SetupStructure(mission, dungeon, grid, random);
                break;
            case SalvageMissionType.Elimination:
                await SetupElimination(mission, dungeon, grid, random);
                break;
            default:
                _sawmill.Warning($"No setup function for salvage mission type {_missionParams.MissionType}!");
                break;
        }
        // End Frontier: mission setup

        var budgetEntries = new List<IBudgetEntry>();

        /*
         * GUARANTEED LOOT
         */

        // We'll always add this loot if possible
        // mainly used for ore layers.
        foreach (var lootProto in _prototypeManager.EnumeratePrototypes<SalvageLootPrototype>())
        {
            if (!lootProto.Guaranteed)
                continue;

            try
            {
                await SpawnDungeonLoot(lootProto, mapUid);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Failed to spawn guaranteed loot {lootProto.ID}: {e}");
            }
        }

        // Handle boss loot (when relevant).

        // Handle mob loot.

        // Handle remaining loot

        /*
         * MOB SPAWNS
         */

        var mobBudget = difficultyProto.MobBudget;
        var faction = _prototypeManager.Index<SalvageFactionPrototype>(mission.Faction);
        var randomSystem = _entManager.System<RandomSystem>();

        foreach (var entry in faction.MobGroups)
        {
            budgetEntries.Add(entry);
        }

        var probSum = budgetEntries.Sum(x => x.Prob);

        while (mobBudget > 0f)
        {
            var entry = randomSystem.GetBudgetEntry(ref mobBudget, ref probSum, budgetEntries, random);
            if (entry == null)
                break;

            try
            {
                await SpawnRandomEntry((mapUid, grid), entry, dungeon, random);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Failed to spawn mobs for {entry.Proto}: {e}");
            }
        }

        // Frontier: difficulty-based loot tables
        var lootTable = difficultyProto.LootTable ?? SharedSalvageSystem.ExpeditionsLootProto;
        var allLoot = _prototypeManager.Index<SalvageLootPrototype>(lootTable);
        // End Frontier
        var lootBudget = difficultyProto.LootBudget;

        foreach (var rule in allLoot.LootRules)
        {
            switch (rule)
            {
                case RandomSpawnsLoot randomLoot:
                    budgetEntries.Clear();

                    foreach (var entry in randomLoot.Entries)
                    {
                        budgetEntries.Add(entry);
                    }

                    probSum = budgetEntries.Sum(x => x.Prob);

                    while (lootBudget > 0f)
                    {
                        var entry = randomSystem.GetBudgetEntry(ref lootBudget, ref probSum, budgetEntries, random);
                        if (entry == null)
                            break;

                        _sawmill.Debug($"Spawning dungeon loot {entry.Proto}");
                        await SpawnRandomEntry((mapUid, grid), entry, dungeon, random);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        // Frontier: delay ship FTL
        if (shuttleUid is { Valid: true })
        {
            var shuttle = _entManager.GetComponent<ShuttleComponent>(shuttleUid.Value);
            _shuttle.FTLToCoordinates(shuttleUid.Value, shuttle, new EntityCoordinates(mapUid, coords), 0f, 5.5f, _salvage.TravelTime);
        }
        // End Frontier

        return true;
    }

    private async Task SpawnRandomEntry(Entity<MapGridComponent> grid, IBudgetEntry entry, Dungeon dungeon, Random random)
    {
        await SuspendIfOutOfTime();

        var availableRooms = new ValueList<DungeonRoom>(dungeon.Rooms);
        var availableTiles = new List<Vector2i>();

        while (availableRooms.Count > 0)
        {
            availableTiles.Clear();
            var roomIndex = random.Next(availableRooms.Count);
            var room = availableRooms.RemoveSwap(roomIndex);
            availableTiles.AddRange(room.Tiles);

            while (availableTiles.Count > 0)
            {
                var tile = availableTiles.RemoveSwap(random.Next(availableTiles.Count));

                if (!_anchorable.TileFree(grid, tile, (int)CollisionGroup.MachineLayer,
                        (int)CollisionGroup.MachineLayer))
                {
                    continue;
                }

                var uid = _entManager.SpawnAtPosition(entry.Proto, _map.GridTileToLocal(grid, grid, tile));
                _entManager.RemoveComponent<GhostRoleComponent>(uid);
                _entManager.RemoveComponent<GhostTakeoverAvailableComponent>(uid);
                return;
            }
        }

        // oh noooooooooooo
    }

    private async Task SpawnDungeonLoot(SalvageLootPrototype loot, EntityUid gridUid)
    {
        for (var i = 0; i < loot.LootRules.Count; i++)
        {
            var rule = loot.LootRules[i];

            switch (rule)
            {
                case BiomeMarkerLoot biomeLoot:
                    {
                        if (_entManager.TryGetComponent<BiomeComponent>(gridUid, out var biome))
                        {
                            _biome.AddMarkerLayer(gridUid, biome, biomeLoot.Prototype);
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

    // Frontier: mission-specific setup functions
    private async Task SetupStructure(
        SalvageMission mission,
        Dungeon dungeon,
        MapGridComponent grid,
        Random random)
    {
        await SuspendIfOutOfTime();

        var structureComp = _entManager.EnsureComponent<SalvageDestructionExpeditionComponent>(mapUid);
        var faction = _prototypeManager.Index<SalvageFactionPrototype>(mission.Faction);
        var difficulty = _prototypeManager.Index(mission.Difficulty);

        var shaggy = faction.Configs["DefenseStructure"];

        var availableRooms = new ValueList<DungeonRoom>(dungeon.Rooms);
        var availableTiles = new List<Vector2i>();

        while (availableRooms.Count > 0 && structureComp.Structures.Count < difficulty.DestructionStructures)
        {
            availableTiles.Clear();
            var roomIndex = random.Next(availableRooms.Count);
            var room = availableRooms.RemoveSwap(roomIndex);
            availableTiles.AddRange(room.Tiles);

            while (availableTiles.Count > 0)
            {
                var tile = availableTiles.RemoveSwap(random.Next(availableTiles.Count));

                if (!_anchorable.TileFree(grid, tile, (int)CollisionGroup.MachineLayer,
                        (int)CollisionGroup.MachineLayer))
                {
                    continue;
                }

                var uid = _entManager.SpawnEntity(shaggy, _map.GridTileToLocal(mapUid, grid, tile));
                _entManager.AddComponent<SalvageStructureComponent>(uid);
                structureComp.Structures.Add(uid);
                break;
            }
        }
    }

    private async Task SetupElimination(
        SalvageMission mission,
        Dungeon dungeon,
        MapGridComponent grid,
        Random random)
    {
        await SuspendIfOutOfTime();

        // spawn megafauna in a random place
        var faction = _prototypeManager.Index<SalvageFactionPrototype>(mission.Faction);
        var prototype = faction.Configs["Megafauna"];

        var availableRooms = new ValueList<DungeonRoom>(dungeon.Rooms);
        var availableTiles = new List<Vector2i>();

        var uid = EntityUid.Invalid;
        while (availableRooms.Count > 0 && uid == EntityUid.Invalid)
        {
            availableTiles.Clear();
            var roomIndex = random.Next(availableRooms.Count);
            var room = availableRooms.RemoveSwap(roomIndex);
            availableTiles.AddRange(room.Tiles);

            while (availableTiles.Count > 0)
            {
                var tile = availableTiles.RemoveSwap(random.Next(availableTiles.Count));

                if (!_anchorable.TileFree(grid, tile, (int)CollisionGroup.MachineLayer,
                        (int)CollisionGroup.MachineLayer))
                {
                    continue;
                }

                uid = _entManager.SpawnAtPosition(prototype, _map.GridTileToLocal(mapUid, grid, tile));
                break;
            }
        }

        var eliminationComp = _entManager.EnsureComponent<SalvageEliminationExpeditionComponent>(mapUid);
        if (uid != EntityUid.Invalid)
            eliminationComp.Megafauna.Add(uid);
    }
    // End Frontier: mission-specific setup functions
}
