using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Station.Components;
using FastAccessors;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class PostMapInitTest
    {
        private const bool SkipTestMaps = true;
        private const string TestMapsPath = "/Maps/_NF/Test/"; // Frontier: _NF

        // Frontier: the following definitions are our maps
        private static readonly string[] NoSpawnMaps =
        {
        };

        private static readonly string[] Grids =
        {
            "/Maps/_NF/Shuttles/akupara.yml",
            "/Maps/_NF/Shuttles/apothecary.yml",
            "/Maps/_NF/Shuttles/barge.yml",
            "/Maps/_NF/Shuttles/bazaar.yml",
            "/Maps/_NF/Shuttles/beaker.yml",
            "/Maps/_NF/Shuttles/bocadillo.yml",
            "/Maps/_NF/Shuttles/bookworm.yml",
            "/Maps/_NF/Shuttles/bulker.yml",
            "/Maps/_NF/Shuttles/caduceus.yml",
            "/Maps/_NF/Shuttles/camper.yml",
            "/Maps/_NF/Shuttles/ceres.yml",
            "/Maps/_NF/Shuttles/chisel.yml",
            "/Maps/_NF/Shuttles/cleithro.yml",
            "/Maps/_NF/Shuttles/comet.yml",
            "/Maps/_NF/Shuttles/condor.yml",
            "/Maps/_NF/Shuttles/construct.yml",
            "/Maps/_NF/Shuttles/crescent.yml",
            "/Maps/_NF/Shuttles/crown.yml",
            "/Maps/_NF/Shuttles/eagle.yml",
            "/Maps/_NF/Shuttles/esquire.yml",
            "/Maps/_NF/Shuttles/garden.yml",
            "/Maps/_NF/Shuttles/hammer.yml",
            "/Maps/_NF/Shuttles/harbormaster.yml",
            "/Maps/_NF/Shuttles/hauler.yml",
            "/Maps/_NF/Shuttles/helix.yml",
            "/Maps/_NF/Shuttles/honker.yml",
            "/Maps/_NF/Shuttles/investigator.yml",
            "/Maps/_NF/Shuttles/kestrel.yml",
            "/Maps/_NF/Shuttles/kilderkin.yml",
            "/Maps/_NF/Shuttles/knuckleverse.yml",
            "/Maps/_NF/Shuttles/lantern.yml",
            "/Maps/_NF/Shuttles/legman.yml",
            "/Maps/_NF/Shuttles/liquidator.yml",
            "/Maps/_NF/Shuttles/loader.yml",
            "/Maps/_NF/Shuttles/lyrae.yml",
            "/Maps/_NF/Shuttles/mccargo.yml",
            "/Maps/_NF/Shuttles/mcdelivery.yml",
            //"/Maps/_NF/Shuttles/metastable.yml", // Does not pass tests.
            "/Maps/_NF/Shuttles/mission.yml",
            "/Maps/_NF/Shuttles/phoenix.yml",
            "/Maps/_NF/Shuttles/piecrust.yml",
            "/Maps/_NF/Shuttles/pioneer.yml",
            "/Maps/_NF/Shuttles/placebo.yml",
            "/Maps/_NF/Shuttles/prospector.yml",
            "/Maps/_NF/Shuttles/pts.yml",
            "/Maps/_NF/Shuttles/pulse.yml",
            "/Maps/_NF/Shuttles/rosebudmki.yml",
            "/Maps/_NF/Shuttles/searchlight.yml",
            "/Maps/_NF/Shuttles/skipper.yml",
            "/Maps/_NF/Shuttles/sparrow.yml",
            "/Maps/_NF/Shuttles/spectre.yml",
            "/Maps/_NF/Shuttles/spirit.yml",
            "/Maps/_NF/Shuttles/stasis.yml",
            "/Maps/_NF/Shuttles/stellaris.yml",
            "/Maps/_NF/Shuttles/stratos.yml",
            "/Maps/_NF/Shuttles/vagabond.yml",
            "/Maps/_NF/Shuttles/waveshot.yml",
            // Admin
            "/Maps/_NF/Shuttles/Admin/fishbowl.yml",
            // Black Market
            "/Maps/_NF/Shuttles/BlackMarket/barnacle.yml",
            "/Maps/_NF/Shuttles/BlackMarket/bocakillo.yml",
            "/Maps/_NF/Shuttles/BlackMarket/falcon.yml",
            "/Maps/_NF/Shuttles/BlackMarket/menace.yml",
            "/Maps/_NF/Shuttles/BlackMarket/schooner.yml",
            // Bus
            "/Maps/_NF/Shuttles/Bus/publicts.yml",
            // Expedition
            "/Maps/_NF/Shuttles/Expedition/ambition.yml",
            "/Maps/_NF/Shuttles/Expedition/anchor.yml",
            "/Maps/_NF/Shuttles/Expedition/brigand.yml",
            "/Maps/_NF/Shuttles/Expedition/courserx.yml",
            "/Maps/_NF/Shuttles/Expedition/dartx.yml",
            "/Maps/_NF/Shuttles/Expedition/decadedove.yml",
            "/Maps/_NF/Shuttles/Expedition/dragonfly.yml",
            "/Maps/_NF/Shuttles/Expedition/gasbender.yml",
            "/Maps/_NF/Shuttles/Expedition/gourd.yml",
            "/Maps/_NF/Shuttles/Expedition/pathfinder.yml",
            "/Maps/_NF/Shuttles/Expedition/rosebudmkii.yml",
            "/Maps/_NF/Shuttles/Expedition/sprinter.yml",
            // Nfsd
            "/Maps/_NF/Shuttles/Nfsd/broadhead.yml",
            "/Maps/_NF/Shuttles/Nfsd/cleric.yml",
            "/Maps/_NF/Shuttles/Nfsd/empress.yml",
            "/Maps/_NF/Shuttles/Nfsd/fighter.yml",
            "/Maps/_NF/Shuttles/Nfsd/hospitaller.yml",
            "/Maps/_NF/Shuttles/Nfsd/inquisitor.yml",
            "/Maps/_NF/Shuttles/Nfsd/interceptor.yml",
            "/Maps/_NF/Shuttles/Nfsd/marauder.yml",
            "/Maps/_NF/Shuttles/Nfsd/opportunity.yml",
            "/Maps/_NF/Shuttles/Nfsd/prowler.yml",
            "/Maps/_NF/Shuttles/Nfsd/rogue.yml",
            "/Maps/_NF/Shuttles/Nfsd/templar.yml",
            "/Maps/_NF/Shuttles/Nfsd/wasp.yml",
            "/Maps/_NF/Shuttles/Nfsd/whiskey.yml",
            // Scrap
            "/Maps/_NF/Shuttles/Scrap/bison.yml",
            "/Maps/_NF/Shuttles/Scrap/canister.yml",
            "/Maps/_NF/Shuttles/Scrap/disciple.yml",
            "/Maps/_NF/Shuttles/Scrap/nugget.yml",
            "/Maps/_NF/Shuttles/Scrap/orange.yml",
            "/Maps/_NF/Shuttles/Scrap/point.yml",
            "/Maps/_NF/Shuttles/Scrap/tide.yml",
            // Sr
            "/Maps/_NF/Shuttles/Sr/bottleneck.yml",
            "/Maps/_NF/Shuttles/Sr/broom.yml",
            "/Maps/_NF/Shuttles/Sr/mailpod.yml",
            "/Maps/_NF/Shuttles/Sr/parcel.yml",
            "/Maps/_NF/Shuttles/Sr/watchdog.yml",
            // Syndicate
            "/Maps/_NF/Shuttles/Syndicate/hunter.yml",
            "/Maps/_NF/Shuttles/Syndicate/infiltrator.yml",
        };

        private static readonly string[] GameMaps =
        {
            "Frontier",
            "NFDev"
        };
        // End Frontier

        /// <summary>
        /// Asserts that specific files have been saved as grids and not maps.
        /// </summary>
        [Test, TestCaseSource(nameof(Grids))]
        public async Task GridsLoadableTest(string mapFile)
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var entManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = entManager.System<MapLoaderSystem>();
            var mapSystem = entManager.System<SharedMapSystem>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            await server.WaitPost(() =>
            {
                mapSystem.CreateMap(out var mapId);
                try
                {
#pragma warning disable NUnit2045
                    Assert.That(mapLoader.TryLoad(mapId, mapFile, out var roots));
                    Assert.That(roots.Where(uid => entManager.HasComponent<MapGridComponent>(uid)), Is.Not.Empty);
#pragma warning restore NUnit2045
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load map {mapFile}, was it saved as a map instead of a grid?", ex);
                }

                try
                {
                    mapManager.DeleteMap(mapId);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to delete map {mapFile}", ex);
                }
            });
            await server.WaitRunTicks(1);

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task NoSavedPostMapInitTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var resourceManager = server.ResolveDependency<IResourceManager>();
            var mapFolder = new ResPath("/Maps/_NF"); // Frontier: add _NF
            var maps = resourceManager
                .ContentFindFiles(mapFolder)
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith(".", StringComparison.Ordinal))
                .ToArray();

            foreach (var map in maps)
            {
                var rootedPath = map.ToRootedPath();

                // ReSharper disable once RedundantLogicalConditionalExpressionOperand
                if (SkipTestMaps && rootedPath.ToString().StartsWith(TestMapsPath, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!resourceManager.TryContentFileRead(rootedPath, out var fileStream))
                {
                    Assert.Fail($"Map not found: {rootedPath}");
                }

                using var reader = new StreamReader(fileStream);
                var yamlStream = new YamlStream();

                yamlStream.Load(reader);

                var root = yamlStream.Documents[0].RootNode;
                var meta = root["meta"];
                var postMapInit = meta["postmapinit"].AsBool();

                Assert.That(postMapInit, Is.False, $"Map {map.Filename} was saved postmapinit");
            }
            await pair.CleanReturnAsync();
        }

        [Test, TestCaseSource(nameof(GameMaps))]
        public async Task GameMapsLoadableTest(string mapProto)
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings
            {
                Dirty = true // Stations spawn a bunch of nullspace entities and maps like centcomm.
            });
            var server = pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = entManager.System<MapLoaderSystem>();
            var mapSystem = entManager.System<SharedMapSystem>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();
            var ticker = entManager.EntitySysManager.GetEntitySystem<GameTicker>();
            var shuttleSystem = entManager.EntitySysManager.GetEntitySystem<ShuttleSystem>();
            var xformQuery = entManager.GetEntityQuery<TransformComponent>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            await server.WaitPost(() =>
            {
                mapSystem.CreateMap(out var mapId);
                try
                {
                    ticker.LoadGameMap(protoManager.Index<GameMapPrototype>(mapProto), mapId, null);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load map {mapProto}", ex);
                }

                mapSystem.CreateMap(out var shuttleMap);
                var largest = 0f;
                EntityUid? targetGrid = null;
                var memberQuery = entManager.GetEntityQuery<StationMemberComponent>();

                var grids = mapManager.GetAllGrids(mapId).ToList();
                var gridUids = grids.Select(o => o.Owner).ToList();
                targetGrid = gridUids.First();

                foreach (var grid in grids)
                {
                    var gridEnt = grid.Owner;
                    if (!memberQuery.HasComponent(gridEnt))
                        continue;

                    var area = grid.Comp.LocalAABB.Width * grid.Comp.LocalAABB.Height;

                    if (area > largest)
                    {
                        largest = area;
                        targetGrid = gridEnt;
                    }
                }

                // Test shuttle can dock.
                // This is done inside gamemap test because loading the map takes ages and we already have it.
                var station = entManager.GetComponent<StationMemberComponent>(targetGrid!.Value).Station;
                if (entManager.TryGetComponent<StationEmergencyShuttleComponent>(station, out var stationEvac))
                {
                    var shuttlePath = stationEvac.EmergencyShuttlePath;
#pragma warning disable NUnit2045
                    Assert.That(mapLoader.TryLoad(shuttleMap, shuttlePath.ToString(), out var roots));
                    EntityUid shuttle = default!;
                    Assert.DoesNotThrow(() =>
                    {
                        shuttle = roots.First(uid => entManager.HasComponent<MapGridComponent>(uid));
                    }, $"Failed to load {shuttlePath}");
                    Assert.That(
                        shuttleSystem.TryFTLDock(shuttle,
                            entManager.GetComponent<ShuttleComponent>(shuttle), targetGrid.Value),
                        $"Unable to dock {shuttlePath} to {mapProto}");
#pragma warning restore NUnit2045
                }

                mapManager.DeleteMap(shuttleMap);

                if (entManager.HasComponent<StationJobsComponent>(station))
                {
                    // Test that the map has valid latejoin spawn points or container spawn points
                    if (!NoSpawnMaps.Contains(mapProto))
                    {
                        var lateSpawns = 0;

                        lateSpawns += GetCountLateSpawn<SpawnPointComponent>(gridUids, entManager);
                        lateSpawns += GetCountLateSpawn<ContainerSpawnPointComponent>(gridUids, entManager);

                        Assert.That(lateSpawns, Is.GreaterThan(0), $"Found no latejoin spawn points on {mapProto}");
                    }

                    // Test all availableJobs have spawnPoints
                    // This is done inside gamemap test because loading the map takes ages and we already have it.
                    var comp = entManager.GetComponent<StationJobsComponent>(station);
                    var jobs = new HashSet<ProtoId<JobPrototype>>(comp.SetupAvailableJobs.Keys);

                    var spawnPoints = entManager.EntityQuery<SpawnPointComponent>()
                        .Where(x => x.SpawnType == SpawnPointType.Job && x.Job != null)
                        .Select(x => x.Job.Value);

                    jobs.ExceptWith(spawnPoints);

                    spawnPoints = entManager.EntityQuery<ContainerSpawnPointComponent>()
                        .Where(x => x.SpawnType is SpawnPointType.Job or SpawnPointType.Unset && x.Job != null)
                        .Select(x => x.Job.Value);

                    jobs.ExceptWith(spawnPoints);

                    Assert.That(jobs, Is.Empty, $"There is no spawnpoints for {string.Join(", ", jobs)} on {mapProto}.");
                }

                try
                {
                    mapManager.DeleteMap(mapId);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to delete map {mapProto}", ex);
                }
            });
            await server.WaitRunTicks(1);

            await pair.CleanReturnAsync();
        }



        private static int GetCountLateSpawn<T>(List<EntityUid> gridUids, IEntityManager entManager)
            where T : ISpawnPoint, IComponent
        {
            var resultCount = 0;
            var queryPoint = entManager.AllEntityQueryEnumerator<T, TransformComponent>();
#nullable enable
            while (queryPoint.MoveNext(out T? comp, out var xform))
            {
                var spawner = (ISpawnPoint) comp;

                if (spawner.SpawnType is not SpawnPointType.LateJoin
                || xform.GridUid == null
                || !gridUids.Contains(xform.GridUid.Value))
                {
                    continue;
                }
#nullable disable
                resultCount++;
                break;
            }

            return resultCount;
        }

        [Test]
        public async Task AllMapsTested()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var protoMan = server.ResolveDependency<IPrototypeManager>();

            var gameMaps = protoMan.EnumeratePrototypes<GameMapPrototype>()
                .Where(x => !pair.IsTestPrototype(x))
                .Select(x => x.ID)
                .ToHashSet();

            Assert.That(gameMaps.Remove(PoolManager.TestMap));

            Assert.That(gameMaps, Is.EquivalentTo(GameMaps.ToHashSet()), "Game map prototype missing from test cases.");

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task NonGameMapsLoadableTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var resourceManager = server.ResolveDependency<IResourceManager>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            var mapSystem = server.System<SharedMapSystem>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            var gameMaps = protoManager.EnumeratePrototypes<GameMapPrototype>().Select(o => o.MapPath).ToHashSet();

            var mapFolder = new ResPath("/Maps");
            var maps = resourceManager
                .ContentFindFiles(mapFolder)
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith(".", StringComparison.Ordinal))
                .ToArray();

            var mapNames = new List<string>();
            foreach (var map in maps)
            {
                if (gameMaps.Contains(map))
                    continue;

                var rootedPath = map.ToRootedPath();
                if (SkipTestMaps && rootedPath.ToString().StartsWith(TestMapsPath, StringComparison.Ordinal))
                {
                    continue;
                }
                mapNames.Add(rootedPath.ToString());
            }

            await server.WaitPost(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var mapName in mapNames)
                    {
                        mapSystem.CreateMap(out var mapId);
                        try
                        {
                            Assert.That(mapLoader.TryLoad(mapId, mapName, out _));
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Failed to load map {mapName}", ex);
                        }

                        try
                        {
                            mapManager.DeleteMap(mapId);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Failed to delete map {mapName}", ex);
                        }
                    }
                });
            });

            await server.WaitRunTicks(1);
            await pair.CleanReturnAsync();
        }
    }
}
