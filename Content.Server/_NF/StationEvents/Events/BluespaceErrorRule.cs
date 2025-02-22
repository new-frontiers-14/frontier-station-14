using System.Numerics;
using Content.Server.Cargo.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;
using Content.Server._NF.Salvage;
using Content.Server._NF.Bank;
using Content.Shared._NF.Bank.BUI;
using Content.Server.GameTicking;
using Content.Server.Procedural;
using Robust.Shared.Prototypes;
using Content.Shared.Salvage;
using Content.Server.Maps.NameGenerators;
using Content.Server.StationEvents.Events;
using Content.Server._NF.Station.Systems;
using Content.Server._NF.StationEvents.Components;

namespace Content.Server._NF.StationEvents.Events;

public sealed class BluespaceErrorRule : StationEventSystem<BluespaceErrorRuleComponent>
{
    NanotrasenNameGenerator _nameGenerator = new();
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly LinkedLifecycleGridSystem _linkedLifecycleGrid = default!;
    [Dependency] private readonly StationRenameWarpsSystems _renameWarps = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly SharedSalvageSystem _salvage = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Started(EntityUid uid, BluespaceErrorRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!_mapSystem.TryGetMap(GameTicker.DefaultMap, out var mapUid))
            return;

        var spawnCoords = new EntityCoordinates(mapUid.Value, Vector2.Zero);

        // Spawn on a dummy map and try to FTL if possible, otherwise dump it.
        _mapSystem.CreateMap(out var mapId);

        foreach (var group in component.Groups.Values)
        {
            var count = _random.Next(group.MinCount, group.MaxCount + 1);

            for (var i = 0; i < count; i++)
            {
                EntityUid spawned;

                if (group.MinimumDistance > 0f)
                {
                    spawnCoords = spawnCoords.WithPosition(_random.NextVector2(group.MinimumDistance, group.MaximumDistance));
                }

                switch (group)
                {
                    case BluespaceDungeonSpawnGroup dungeon:
                        if (!TryDungeonSpawn(spawnCoords, component, ref dungeon, i, out spawned))
                            continue;

                        break;
                    case BluespaceGridSpawnGroup grid:
                        if (!TryGridSpawn(spawnCoords, uid, mapId, ref grid, i, out spawned))
                            continue;

                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (group.NameLoc != null && group.NameLoc.Count > 0)
                {
                    _metadata.SetEntityName(spawned, Loc.GetString(_random.Pick(group.NameLoc)));

                }
                else if (_protoManager.TryIndex(group.NameDataset, out var dataset))
                {
                    string gridName;
                    switch (group.NameDatasetType)
                    {
                        case BluespaceDatasetNameType.FTL:
                            gridName = _salvage.GetFTLName(dataset, _random.Next());
                            break;
                        case BluespaceDatasetNameType.Nanotrasen:
                            gridName = _nameGenerator.FormatName(Loc.GetString(_random.Pick(dataset.Values)) + " {1}"); // We need the prefix.
                            break;
                        case BluespaceDatasetNameType.Verbatim:
                        default:
                            gridName = Loc.GetString(_random.Pick(dataset.Values));
                            break;
                    }

                    _metadata.SetEntityName(spawned, gridName);
                }

                if (group.NameWarp)
                {
                    bool? adminOnly = group.HideWarp ? true : null;
                    _renameWarps.SyncWarpPointsToGrid(spawned, forceAdminOnly: adminOnly);
                }

                EntityManager.AddComponents(spawned, group.AddComponents);

                component.GridsUid.Add(spawned);
            }
        }

        _mapManager.DeleteMap(mapId);
    }

    private bool TryDungeonSpawn(EntityCoordinates spawnCoords, BluespaceErrorRuleComponent component, ref BluespaceDungeonSpawnGroup group, int i, out EntityUid spawned)
    {
        spawned = EntityUid.Invalid;

        // handle empty prototype list, _random.Pick throws
        if (group.Protos.Count <= 0)
            return false;

        // Enforce randomness with some round-robin-ish behaviour
        int maxIndex = group.Protos.Count - (i % group.Protos.Count);
        int index = _random.Next(maxIndex);
        var dungeonProtoId = group.Protos[index];
        // Move selected item to the end of the list
        group.Protos.RemoveAt(index);
        group.Protos.Add(dungeonProtoId);

        if (!_protoManager.TryIndex(dungeonProtoId, out var dungeonProto))
        {
            return false;
        }

        _mapSystem.CreateMap(out var mapId);

        var spawnedGrid = _mapManager.CreateGridEntity(mapId);

        _transform.SetMapCoordinates(spawnedGrid, new MapCoordinates(Vector2.Zero, mapId));
        _dungeon.GenerateDungeon(dungeonProto, dungeonProto.ID, spawnedGrid.Owner, spawnedGrid.Comp, Vector2i.Zero, _random.Next(), spawnCoords); // Frontier: add dungeonProto.ID

        spawned = spawnedGrid.Owner;
        component.MapsUid.Add(mapId);
        return true;
    }

    private bool TryGridSpawn(EntityCoordinates spawnCoords, EntityUid stationUid, MapId mapId, ref BluespaceGridSpawnGroup group, int i, out EntityUid spawned)
    {
        spawned = EntityUid.Invalid;

        if (group.Paths.Count == 0)
        {
            Log.Error($"Found no paths for GridSpawn");
            return false;
        }

        // Enforce randomness with some round-robin-ish behaviour
        int maxIndex = group.Paths.Count - (i % group.Paths.Count);
        int index = _random.Next(maxIndex);
        var path = group.Paths[index];
        // Move selected item to the end of the list
        group.Paths.RemoveAt(index);
        group.Paths.Add(path);

        // Do we support maps with multiple grids?
        if (_loader.TryLoad(mapId, path.ToString(), out var ent) && ent.Count == 1)
        {
            if (HasComp<ShuttleComponent>(ent[0]))
            {
                _shuttle.TryFTLProximity(ent[0], spawnCoords);
            }

            if (group.NameGrid)
            {
                var name = path.FilenameWithoutExtension;
                _metadata.SetEntityName(ent[0], name);
            }

            spawned = ent[0];
            return true;
        }

        Log.Error($"Error loading gridspawn for {ToPrettyString(stationUid)} / {path}");
        return false;
    }

    protected override void Ended(EntityUid uid, BluespaceErrorRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (component.GridsUid == null)
            return;

        foreach (var componentGridUid in component.GridsUid)
        {
            if (!EntityManager.TryGetComponent<TransformComponent>(componentGridUid, out var gridTransform))
            {
                Log.Error("bluespace error objective was missing transform component");
                return;
            }

            if (gridTransform.GridUid is not EntityUid gridUid)
            {
                Log.Error("bluespace error has no associated grid?");
                return;
            }

            if (component.DeleteGridsOnEnd)
            {
                // Handle mobrestrictions getting deleted
                var query = AllEntityQuery<NFSalvageMobRestrictionsComponent>();

                while (query.MoveNext(out var salvUid, out var salvMob))
                {
                    if (!salvMob.DespawnIfOffLinkedGrid)
                    {
                        var transform = Transform(salvUid);
                        if (transform.GridUid != salvMob.LinkedGridEntity)
                        {
                            RemComp<NFSalvageMobRestrictionsComponent>(salvUid);
                            continue;
                        }
                    }

                    if (gridTransform.GridUid == salvMob.LinkedGridEntity)
                    {
                        QueueDel(salvUid);
                    }
                }

                var playerMobs = _linkedLifecycleGrid.GetEntitiesToReparent(gridUid);
                foreach (var mob in playerMobs)
                {
                    _transform.DetachEntity(mob.Entity.Owner, mob.Entity.Comp);
                }

                var gridValue = _pricing.AppraiseGrid(gridUid, null);

                // Deletion has to happen before grid traversal re-parents players.
                Del(gridUid);

                foreach (var mob in playerMobs)
                {
                    _transform.SetCoordinates(mob.Entity.Owner, new EntityCoordinates(mob.MapUid, mob.MapPosition));
                }

                foreach (var (account, rewardCoeff) in component.RewardAccounts)
                {
                    var reward = (int)(gridValue * rewardCoeff);
                    _bank.TrySectorDeposit(account, reward, LedgerEntryType.BluespaceReward);
                }
            }
        }

        foreach (MapId mapId in component.MapsUid)
        {
            if (_mapManager.MapExists(mapId))
                _mapManager.DeleteMap(mapId);
        }
    }
}
