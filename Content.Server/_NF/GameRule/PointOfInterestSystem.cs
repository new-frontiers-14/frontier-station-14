using System.Linq;
using System.Numerics;
using Content.Server._NF.Trade;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Station.Systems;
using Content.Shared._NF.CCVar;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server._NF.Station.Systems;

namespace Content.Server._NF.GameRule;

/// <summary>
/// This handles the dungeon and trading post spawning, as well as round end capitalism summary
/// </summary>
//[Access(typeof(NfAdventureRuleSystem))]
public sealed class PointOfInterestSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly StationRenameWarpsSystems _renameWarps = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private List<Vector2> _stationCoords = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _stationCoords.Clear();
    }

    private void AddStationCoordsToSet(Vector2 coords)
    {
        _stationCoords.Add(coords);
    }

    public void GenerateDepots(MapId mapUid, List<PointOfInterestPrototype> depotPrototypes, out List<EntityUid> depotStations)
    {
        //For depots, we want them to fill a circular type dystance formula to try to keep them as far apart as possible
        //Therefore, we will be taking our range properties and treating them as magnitudes of a direction vector divided
        //by the number of depots set in our corresponding cvar

        depotStations = new List<EntityUid>();
        var depotCount = _cfg.GetCVar(NFCCVars.CargoDepots);
        var rotation = 2 * Math.PI / depotCount;
        var rotationOffset = _random.NextAngle() / depotCount;

        if (_ticker.CurrentPreset is null)
            return;

        var currentPreset = _ticker.CurrentPreset.ID;

        for (int i = 0; i < depotCount && depotPrototypes.Count > 0; i++)
        {
            var proto = _random.Pick(depotPrototypes);

            // Safety check: ensure selected POIs are either fine in any preset or accepts this current one.
            if (proto.SpawnGamePreset.Length > 0 && !proto.SpawnGamePreset.Contains(currentPreset))
                continue;

            Vector2i offset = new Vector2i((int) _random.Next(proto.MinimumDistance, proto.MaximumDistance), 0);
            offset = offset.Rotate(rotationOffset);
            rotationOffset += rotation;
            // Append letter to depot name.

            string overrideName = proto.Name;
            if (i < 26)
                overrideName += $" {(char)('A' + i)}"; // " A" ... " Z"
            else
                overrideName += $" {i + 1}"; // " 27", " 28"...
            if (TrySpawnPoiGrid(mapUid, proto, offset, out var depotUid, overrideName: overrideName) && depotUid is { Valid: true } depot)
            {
                // Nasty jank: set up destination in the station.
                var depotStation = _station.GetOwningStation(depot);
                if (TryComp<TradeCrateDestinationComponent>(depotStation, out var destComp))
                {
                    if (i < 26)
                        destComp.DestinationProto = $"Cargo{(char)('A' + i)}";
                    else
                        destComp.DestinationProto = "CargoOther";
                }
                depotStations.Add(depot);
                AddStationCoordsToSet(offset); // adjust list of actual station coords
            }
        }
    }

    public void GenerateMarkets(MapId mapUid, List<PointOfInterestPrototype> marketPrototypes, out List<EntityUid> marketStations)
    {
        //For market stations, we are going to allow for a bit of randomness and a different offset configuration. We dont
        //want copies of this one, since these can be more themed and duplicate names, for instance, can make for a less
        //ideal world

        marketStations = new List<EntityUid>();
        var marketCount = _cfg.GetCVar(NFCCVars.MarketStations);
        _random.Shuffle(marketPrototypes);
        int marketsAdded = 0;

        if (_ticker.CurrentPreset is null)
            return;
        var currentPreset = _ticker.CurrentPreset.ID;

        foreach (var proto in marketPrototypes)
        {
            // Safety check: ensure selected POIs are either fine in any preset or accepts this current one.
            if (proto.SpawnGamePreset.Length > 0 && !proto.SpawnGamePreset.Contains(currentPreset))
                continue;

            if (marketsAdded >= marketCount)
                break;

            var offset = GetRandomPOICoord(proto.MinimumDistance, proto.MaximumDistance);

            if (TrySpawnPoiGrid(mapUid, proto, offset, out var marketUid) && marketUid is { Valid: true } market)
            {
                marketStations.Add(market);
                marketsAdded++;
                AddStationCoordsToSet(offset);
            }
        }
    }

    public void GenerateOptionals(MapId mapUid, List<PointOfInterestPrototype> optionalPrototypes, out List<EntityUid> optionalStations)
    {
        //Stations that do not have a defined grouping in their prototype get a default of "Optional" and get put into the
        //generic random rotation of POIs. This should include traditional places like Tinnia's rest, the Science Lab, The Pit,
        //and most RP places. This will essentially put them all into a pool to pull from, and still does not use the RNG function.

        optionalStations = new List<EntityUid>();
        var optionalCount = _cfg.GetCVar(NFCCVars.OptionalStations);
        _random.Shuffle(optionalPrototypes);
        int optionalsAdded = 0;

        if (_ticker.CurrentPreset is null)
            return;
        var currentPreset = _ticker.CurrentPreset.ID;

        foreach (var proto in optionalPrototypes)
        {
            // Safety check: ensure selected POIs are either fine in any preset or accepts this current one.
            if (proto.SpawnGamePreset.Length > 0 && !proto.SpawnGamePreset.Contains(currentPreset))
                continue;

            if (optionalsAdded >= optionalCount)
                break;

            var offset = GetRandomPOICoord(proto.MinimumDistance, proto.MaximumDistance);

            if (TrySpawnPoiGrid(mapUid, proto, offset, out var optionalUid) && optionalUid is { Valid: true } uid)
            {
                optionalStations.Add(uid);
                AddStationCoordsToSet(offset);
            }
        }
    }

    public void GenerateRequireds(MapId mapUid, List<PointOfInterestPrototype> requiredPrototypes, out List<EntityUid> requiredStations)
    {
        //Stations are required are ones that are vital to function but otherwise still follow a generic random spawn logic
        //Traditionally these would be stations like Expedition Lodge, NFSD station, Prison/Courthouse POI, etc.
        //There are no limit to these, and any prototype marked alwaysSpawn = true will get pulled out of any list that isnt Markets/Depots
        //And will always appear every time, and also will not be included in other optional/dynamic lists

        requiredStations = new List<EntityUid>();

        if (_ticker.CurrentPreset is null)
            return;
        var currentPreset = _ticker.CurrentPreset!.ID;

        foreach (var proto in requiredPrototypes)
        {
            // Safety check: ensure selected POIs are either fine in any preset or accepts this current one.
            if (proto.SpawnGamePreset.Length > 0 && !proto.SpawnGamePreset.Contains(currentPreset))
                continue;

            var offset = GetRandomPOICoord(proto.MinimumDistance, proto.MaximumDistance);

            if (TrySpawnPoiGrid(mapUid, proto, offset, out var requiredUid) && requiredUid is { Valid: true } uid)
            {
                requiredStations.Add(uid);
                AddStationCoordsToSet(offset);
            }
        }
    }

    public void GenerateUniques(MapId mapUid, Dictionary<string, List<PointOfInterestPrototype>> uniquePrototypes, out List<EntityUid> uniqueStations)
    {
        //Unique locations are semi-dynamic groupings of POIs that rely each independantly on the SpawnChance per POI prototype
        //Since these are the remainder, and logically must have custom-designated groupings, we can then know to subdivide
        //our random pool into these found groups.
        //To do this with an equal distribution on a per-POI, per-round percentage basis, we are going to ensure a random
        //pick order of which we analyze our weighted chances to spawn, and if successful, remove every entry of that group
        //entirely.

        uniqueStations = new List<EntityUid>();

        if (_ticker.CurrentPreset is null)
            return;
        var currentPreset = _ticker.CurrentPreset!.ID;

        foreach (var prototypeList in uniquePrototypes.Values)
        {
            // Try to spawn
            _random.Shuffle(prototypeList);
            foreach (var proto in prototypeList)
            {
                // Safety check: ensure selected POIs are either fine in any preset or accepts this current one.
                if (proto.SpawnGamePreset.Length > 0 && !proto.SpawnGamePreset.Contains(currentPreset))
                    continue;

                var chance = _random.NextFloat(0, 1);
                if (chance <= proto.SpawnChance)
                {
                    var offset = GetRandomPOICoord(proto.MinimumDistance, proto.MaximumDistance);

                    if (TrySpawnPoiGrid(mapUid, proto, offset, out var optionalUid) && optionalUid is { Valid: true } uid)
                    {
                        uniqueStations.Add(uid);
                        AddStationCoordsToSet(offset);
                        break;
                    }
                }
            }
        }
    }

    private bool TrySpawnPoiGrid(MapId mapUid, PointOfInterestPrototype proto, Vector2 offset, out EntityUid? gridUid, string? overrideName = null)
    {
        gridUid = null;
        if (_map.TryLoad(mapUid, proto.GridPath.ToString(), out var mapUids,
                new MapLoadOptions
                {
                    Offset = offset,
                    Rotation = _random.NextAngle()
                }))
        {

            string stationName = string.IsNullOrEmpty(overrideName) ? proto.Name : overrideName;

            EntityUid? stationUid = null;
            if (_proto.TryIndex<GameMapPrototype>(proto.ID, out var stationProto))
            {
                stationUid = _station.InitializeNewStation(stationProto.Stations[proto.ID], mapUids, stationName);
            }

            foreach (var grid in mapUids)
            {
                var meta = EnsureComp<MetaDataComponent>(grid);
                _meta.SetEntityName(grid, stationName, meta);

                EntityManager.AddComponents(grid, proto.AddComponents);
            }

            // Rename warp points after set up if needed
            if (proto.NameWarp)
            {
                bool? hideWarp = proto.HideWarp ? true : null;
                if (stationUid != null)
                    _renameWarps.SyncWarpPointsToStation(stationUid.Value, forceAdminOnly: hideWarp);
                else
                    _renameWarps.SyncWarpPointsToGrids(mapUids, forceAdminOnly: hideWarp);
            }

            gridUid = mapUids[0];
            return true;
        }

        return false;
    }

    private Vector2 GetRandomPOICoord(float unscaledMinRange, float unscaledMaxRange)
    {
        int numRetries = int.Max(_cfg.GetCVar(NFCCVars.POIPlacementRetries), 0);
        float minDistance = float.Max(_cfg.GetCVar(NFCCVars.MinPOIDistance), 0); // Constant at the end to avoid NaN weirdness

        Vector2 coords = _random.NextVector2(unscaledMinRange, unscaledMaxRange);
        for (int i = 0; i < numRetries; i++)
        {
            bool positionIsValid = true;
            foreach (var station in _stationCoords)
            {
                if (Vector2.Distance(station, coords) < minDistance)
                {
                    positionIsValid = false;
                    break;
                }
            }

            // We have a valid position
            if (positionIsValid)
                break;

            // No vector yet, get next value.
            coords = _random.NextVector2(unscaledMinRange, unscaledMaxRange);
        }

        return coords;
    }
}
