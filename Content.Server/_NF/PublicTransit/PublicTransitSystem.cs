using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server._NF.GameTicking.Events;
using Content.Server._NF.PublicTransit.Components;
using Content.Server._NF.PublicTransit.Prototypes;
using Content.Server._NF.Station.Systems;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._NF.CCVar;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._NF.PublicTransit;

/// <summary>
/// If enabled, spawns a public trasnport grid as definied by cvar, to act as an automatic transit shuttle between designated grids
/// </summary>
public sealed class PublicTransitSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly StationRenameWarpsSystems _renameWarps = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StationSystem _station = default!;

    /// <summary>
    /// If enabled then spawns the bus and sets up the bus line.
    /// </summary>
    public bool Enabled { get; private set; }
    public bool StationsGenerated { get; private set; }
    public bool RoutesCreated { get; private set; }
    private Dictionary<ProtoId<PublicTransitRoutePrototype>, PublicTransitRoute> _routes = new();
    private readonly TimeSpan _updatePeriod = TimeSpan.FromSeconds(2);
    private TimeSpan _nextUpdate = TimeSpan.FromSeconds(2);
    private TimeSpan _initialHyperspaceTime = TimeSpan.FromSeconds(5);
    private const float ShuttleSpawnBuffer = 1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationTransitComponent, ComponentStartup>(OnStationStartup);
        SubscribeLocalEvent<StationTransitComponent, ComponentRemove>(OnStationRemove);
        SubscribeLocalEvent<TransitShuttleComponent, ComponentStartup>(OnShuttleStartup);
        SubscribeLocalEvent<TransitShuttleComponent, FTLCompletedEvent>(OnShuttleArrival);
        SubscribeLocalEvent<TransitShuttleComponent, FTLTagEvent>(OnShuttleTag);
        SubscribeLocalEvent<BusScheduleComponent, ExaminedEvent>(OnScheduleExamined);
        SubscribeLocalEvent<StationsGeneratedEvent>(OnStationsGenerated);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        Enabled = _cfgManager.GetCVar(NFCCVars.PublicTransit);
        StationsGenerated = false;
        RoutesCreated = false;
        _routes.Clear();
        _cfgManager.OnValueChanged(NFCCVars.PublicTransit, SetTransit);
        _nextUpdate = _timing.CurTime;
    }

    public void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        _routes.Clear();
        StationsGenerated = false;
        RoutesCreated = false;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfgManager.UnsubValueChanged(NFCCVars.PublicTransit, SetTransit);
    }


    /// <summary>
    /// Hardcoded snippit to intercept FTL events. It catches the transit shuttle and ensures its looking for the "DockTransit" priority dock.
    /// </summary>
    private void OnShuttleTag(Entity<TransitShuttleComponent> ent, ref FTLTagEvent args)
    {
        if (args.Handled)
            return;

        // Just saves mappers forgetting, or ensuring that a non-standard grid forced to be a bus will prioritize the "DockTransit" tagged docks
        args.Tag = ent.Comp.DockTag;
        args.Handled = true;
    }

    private void OnScheduleExamined(Entity<BusScheduleComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryComp(ent, out TransformComponent? xform)
            || xform.GridUid == null
            || !TryComp<StationTransitComponent>(xform.GridUid, out var transit))
        {
            args.PushMarkup(Loc.GetString("bus-schedule-no-bus"));
            return;
        }

        // Get the route associated with this grid.
        if (transit.Routes.Count <= 0)
        {
            args.PushMarkup(Loc.GetString("bus-schedule-no-bus"));
            return;
        }

        var route = ent.Comp.RouteID;
        if (route == null)
        {
            route = transit.Routes.First().Key;
        }
        else if (!transit.Routes.ContainsKey(route.Value))
        {
            args.PushMarkup(Loc.GetString("bus-schedule-no-buses-on-route"));
            return;
        }

        // Get stop index on requested route
        if (!_routes.TryGetValue(route.Value, out var routeData)
            || !routeData.StopIndicesByGrid.TryGetValue(xform.GridUid.Value, out var destInfo))
        {
            args.PushMarkup(Loc.GetString("bus-schedule-no-buses-on-route"));
            return;
        }

        Entity<TransitShuttleComponent>? nextBusMaybe = null;
        var stopDistance = int.MaxValue;
        var numStops = routeData.StopIndicesByGrid.Count;
        // Get the buses associated with this route, find the closest one before this stop.
        var busQuery = EntityQueryEnumerator<TransitShuttleComponent>();
        while (busQuery.MoveNext(out var busUid, out var busComp))
        {
            if (busComp.RouteID != route)
                continue;

            // Find grid index
            if (!routeData.StopIndicesByGrid.TryGetValue(xform.GridUid.Value, out var busInfo))
                continue;

            // Find distance (ensure positive modulo)
            var distance = (destInfo.stopIndex - busInfo.stopIndex + numStops) % numStops;
            if (distance < stopDistance)
            {
                stopDistance = distance;
                nextBusMaybe = (busUid, busComp);
            }
        }

        if (nextBusMaybe is not { } nextBus)
        {
            args.PushMarkup(Loc.GetString("bus-schedule-no-buses-on-route"));
            return;
        }

        // Calculate the departure time from this stop and the arrival time at the next stops.
        var departureTime = nextBus.Comp.NextTransfer + stopDistance * (routeData.Prototype.TravelTime + routeData.Prototype.WaitTime) - _ticker.RoundStartTimeSpan;

        args.PushMarkup(Loc.GetString("bus-schedule-next-departure", ("time", departureTime.ToString(@"hh\:mm\:ss"))));
        args.PushMarkup(Loc.GetString("bus-schedule-arrival-header"));

        var arrivalTime = departureTime + routeData.Prototype.TravelTime;
        for (int i = 1; i <= routeData.GridStops.Count; i++)
        {
            var stopUid = routeData.GridStops.GetValueAtIndex(destInfo.stopIndex + i % routeData.GridStops.Count);

            args.PushMarkup(Loc.GetString("bus-schedule-next-departure", ("station", Name(stopUid)), ("time", arrivalTime.ToString(@"hh\:mm\:ss"))));
            arrivalTime += routeData.Prototype.TravelTime + routeData.Prototype.WaitTime;
        }
    }

    private void OnStationsGenerated(StationsGeneratedEvent args)
    {
        if (Enabled && !RoutesCreated)
            SetupPublicTransit();

        StationsGenerated = true;
    }

    /// <summary>
    /// Checks to make sure the grid is on the appropriate playfield, i.e., not in mapping space being worked on.
    /// If so, adds the grid to the list of bus stops, but only if its not already there
    /// </summary>
    private void OnStationStartup(Entity<StationTransitComponent> ent, ref ComponentStartup args)
    {
        if (Transform(ent).MapID != _ticker.DefaultMap) //best solution i could find because of componentinit/mapinit race conditions
            return;

        // Add each present route
        foreach (var (routeId, routeIndex) in ent.Comp.Routes)
        {
            if (!_routes.TryGetValue(routeId, out var route))
            {
                if (!_proto.TryIndex(routeId, out var routeProto))
                    continue;
                route = new PublicTransitRoute(routeProto);
                _routes.Add(routeId, route);
            }

            // Already added (running from startup, reasonable)
            if (route.GridStops.ContainsValue(ent))
                continue;

            // Handle duplicate values (e.g. three trade stations)
            int actualRouteIndex = routeIndex;
            while (!route.GridStops.TryAdd(actualRouteIndex, ent))
                actualRouteIndex++;

            CalculateGridIndices(route);
        }
        // TODO: add bus if needed, adjust departure times
    }

    private void CalculateGridIndices(PublicTransitRoute route)
    {
        // Recalculate grid indices
        route.StopIndicesByGrid.Clear();
        var index = 0;
        foreach (var stop in route.GridStops)
        {
            route.StopIndicesByGrid[stop.Value] = (stop.Key, index++);
        }
    }

    /// <summary>
    /// When a bus stop gets deleted in-game, we need to remove it from the list of bus stops, or else we get FTL problems
    /// </summary>
    private void OnStationRemove(Entity<StationTransitComponent> ent, ref ComponentRemove args)
    {
        foreach (var route in _routes.Values)
        {
            var index = route.GridStops.IndexOfValue(ent);
            if (index != -1)
                route.GridStops.RemoveAt(index);

            CalculateGridIndices(route);
        }
        // TODO: could add logic to rebalance the buses here.
    }

    private void OnShuttleStartup(Entity<TransitShuttleComponent> ent, ref ComponentStartup args)
    {
        _renameWarps.SyncWarpPointsToGrid(ent);
    }

    private void OnShuttleArrival(Entity<TransitShuttleComponent> ent, ref FTLCompletedEvent args)
    {
        var consoleQuery = EntityQueryEnumerator<ShuttleConsoleComponent, TransformComponent>();

        while (consoleQuery.MoveNext(out var consoleUid, out _, out var xform))
        {
            if (xform.GridUid != ent)
                continue;

            // Find route details.
            if (!_routes.TryGetValue(ent.Comp.RouteID, out var route))
                continue;

            // Note: the next grid is not cached in case stations are added or removed.
            if (!TryGetNextStop(route, ent.Comp.CurrentGrid, out var nextGrid))
                continue;

            if (!TryComp(nextGrid, out MetaDataComponent? metadata))
                continue;

            _chat.TrySendInGameICMessage(consoleUid, Loc.GetString("public-transit-arrival",
                    ("destination", metadata.EntityName), ("waittime", route.Prototype.WaitTime)),
                InGameICChatType.Speak, ChatTransmitRange.HideChat, hideLog: true, checkRadioPrefix: false,
                ignoreActionBlocker: true);
        }
    }

    /// <summary>
    /// Here is our bus stop list handler. Theres probably a better way...
    /// First, sets our output to null just in case
    /// then, makes sure that our counter/index isnt out of range (reaching the end of the list will force you back to the beginning, like a loop)
    /// Then, it checks to make sure that there even is anything in the list
    /// and if so, we return the next station, and then increment our counter for the next time its ran
    /// </summary>
    private bool TryGetNextStop(PublicTransitRoute route, EntityUid currentGrid, [NotNullWhen(true)] out EntityUid? nextGrid)
    {
        nextGrid = null;
        if (route.GridStops.Count <= 0)
            return false;

        // If not in array, move to first item (-1 to 0).  If in array, move to next item (if last, revert to first).
        var currentIndex = route.GridStops.IndexOfValue(currentGrid);
        nextGrid = route.GridStops.GetValueAtIndex((currentIndex + 1) % route.GridStops.Count);

        return true;
    }

    /// <summary>
    /// We check the current time every tick, and if its not yet time, we just ignore.
    /// If the timer is ready, we send the shuttle on an FTL journey to the destination it has saved
    /// then we check our bus list, and if it returns true with the next station, we cache it on the component and reset the timer
    /// if it returns false or gives a bad grid, we are just going to FTL back to where we are and try again until theres a proper destination
    /// This could cause unintended behavior, if a destination is deleted while it's next in the cache, the shuttle is going to be stuck in FTL space
    /// However the timer is going to force it to FTL to the next bus stop
    /// If it happens that all bus stops are deleted and we never get a valid stop again, we are going to be stuck FTL'ing forever in ftl space
    /// but at that point, theres nowhere to return to anyway
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        // Update periodically, no need to have the buses on time to the millisecond.
        if (_nextUpdate < curTime)
            return;
        _nextUpdate = curTime + _updatePeriod;

        var query = EntityQueryEnumerator<TransitShuttleComponent, ShuttleComponent>();

        while (query.MoveNext(out var uid, out var comp, out var shuttle))
        {
            if (comp.NextTransfer > curTime)
                continue;

            if (!_routes.TryGetValue(comp.RouteID, out var route))
                continue;

            // Regardless of whether we have a station to go to, don't rerun the same conditions frequently.
            comp.NextTransfer = curTime + route.Prototype.TravelTime + route.Prototype.WaitTime;

            if (!TryGetNextStop(route, comp.CurrentGrid, out var nextGrid))
                continue; // NOTE: this bus is dead, should we despawn it?

            // FTL to next station if it exists.  Do this before the print.
            _shuttles.FTLToDock(uid, shuttle, nextGrid.Value, hyperspaceTime: route.Prototype.TravelTime.Seconds, priorityTag: comp.DockTag); // TODO: Unhard code the priorityTag as it should be added from the system.
            comp.CurrentGrid = nextGrid.Value;

            if (!TryComp(nextGrid, out MetaDataComponent? metadata))
                continue;

            var consoleQuery = EntityQueryEnumerator<ShuttleConsoleComponent, TransformComponent>();

            while (consoleQuery.MoveNext(out var consoleUid, out _, out var xform))
            {
                if (Transform(consoleUid).GridUid != uid)
                    continue;

                _chat.TrySendInGameICMessage(consoleUid, Loc.GetString("public-transit-departure",
                        ("destination", metadata.EntityName), ("flytime", route.Prototype.TravelTime)),
                    InGameICChatType.Speak, ChatTransmitRange.HideChat, hideLog: true, checkRadioPrefix: false,
                    ignoreActionBlocker: true);
            }
        }
    }

    /// <summary>
    /// Here is handling a simple CVAR change to enable/disable the system
    /// if the cvar is changed to enabled, we setup the transit system
    /// if its changed to disabled, we delete any bus grids that exist
    /// along with anyone/thing riding the bus
    /// you've been warned
    /// </summary>
    private void SetTransit(bool obj)
    {
        Enabled = obj;

        if (!Enabled)
        {
            var shuttleQuery = AllEntityQuery<TransitShuttleComponent>();

            while (shuttleQuery.MoveNext(out var uid, out _))
            {
                QueueDel(uid);
            }
            RoutesCreated = false;
        }
        else if (!RoutesCreated && StationsGenerated)
        {
            SetupPublicTransit();
        }
    }

    /// <summary>
    /// Here is where we handle setting up the transit system, including sanity checks.
    /// This is called multiple times, from a few different sources, to ensure that if the system is activated dynamically
    /// it will still function as intended
    /// </summary>
    /// <remarks>
    /// Bus scheduling may be clumped if disabled and reenabled with enough stops to require additional buses.
    /// </remarks>
    private void SetupPublicTransit()
    {
        Dictionary<ProtoId<PublicTransitRoutePrototype>, List<EntityUid>> busesByRoute = new();
        // Count the existing buses.
        var query = EntityQueryEnumerator<TransitShuttleComponent>();
        while (query.MoveNext(out var ent, out var transit))
        {
            if (!busesByRoute.ContainsKey(transit.RouteID))
                busesByRoute[transit.RouteID] = new();
            busesByRoute[transit.RouteID].Add(ent);
        }

        // Set up bus depot
        // NOTE: this only works with one depot at the moment.
        foreach (var route in _routes.Values)
        {
            route.GridStops.Remove(0);
        }
        var busDepotEnumerator = EntityQueryEnumerator<StationBusDepotComponent>();
        while (busDepotEnumerator.MoveNext(out var depotStation, out _))
        {
            if (!TryComp<StationDataComponent>(depotStation, out var stationData))
                continue;

            // Assuming the largest grid is the depot.
            var depotGrid = _station.GetLargestGrid(stationData);
            if (depotGrid == null)
                continue;

            var transit = EnsureComp<StationTransitComponent>(depotGrid.Value);
            transit.Routes.Clear();
            foreach (var route in _routes.Values)
            {
                if (route.GridStops.Count <= 0)
                    continue;

                route.GridStops.Add(0, depotGrid.Value);
                transit.Routes[route.Prototype.ID] = 0;
            }
        }

        var shuttleOffset = 500.0f;
        var shuttleArrivalOffset = 5.0f;
        var dummyMapUid = _map.CreateMap(out var dummyMap);

        // For each route: find out the number of buses we need on it, then add more buses until we get to that count.
        // Leave the excess buses for now.
        foreach (var route in _routes.Values)
        {
            var numBuses = 0;
            if (busesByRoute.ContainsKey(route.Prototype.ID))
                numBuses = busesByRoute[route.Prototype.ID].Count;

            var neededBuses = 1;
            if (route.Prototype.StationsPerBus > 0)
                neededBuses += route.GridStops.Count / route.Prototype.StationsPerBus;

            if (numBuses >= neededBuses)
                continue;

            // TODO: default to 
            if (!_proto.TryIndex(route.Prototype.BusVessel, out var busVessel))
                continue;

            var routeHopTime = route.Prototype.WaitTime + route.Prototype.TravelTime;

            while (numBuses < neededBuses)
            {
                // Spawn the bus onto a dummy map
                if (!_loader.TryLoadGrid(dummyMap, busVessel.ShuttlePath, out var shuttleMaybe, offset: new Vector2(shuttleOffset, 1f))
                    || shuttleMaybe is not { } shuttleEnt
                    || !TryComp<MapGridComponent>(shuttleEnt, out var mapGrid)
                    || !TryComp<ShuttleComponent>(shuttleEnt, out var shuttleComp))
                {
                    break;
                }

                shuttleOffset += mapGrid.LocalAABB.Width + ShuttleSpawnBuffer;

                // Here we are making sure that the shuttle has the TransitShuttle comp onto it, in case of dynamically changing the bus grid
                var transitComp = EnsureComp<TransitShuttleComponent>(shuttleEnt.Owner);
                transitComp.RouteID = route.Prototype.ID;
                transitComp.DockTag = route.Prototype.DockTag;
                // If this thing should be a station, set up the station.
                var shuttleName = Loc.GetString("public-transit-shuttle-name", ("number", route.Prototype.RouteNumber), ("suffix", neededBuses > 1 ? (char)('A' + numBuses) : ""));
                if (_proto.TryIndex<GameMapPrototype>(busVessel.ID, out var stationProto))
                {
                    var shuttleStation = _station.InitializeNewStation(stationProto.Stations[busVessel.ID], [shuttleEnt]);
                    _meta.SetEntityName(shuttleStation, shuttleName);
                }
                // Set both the bus grid and station name
                _meta.SetEntityName(shuttleEnt.Owner, shuttleName);

                // Space each bus out in the schedule.
                var relativePosition = (float)(numBuses * route.GridStops.Count) / neededBuses;
                var relativeIndex = MathF.Ceiling(relativePosition);
                var extraTime = (relativeIndex - relativePosition) * routeHopTime;

                //we set up a default in case the second time we call it fails for some reason
                var nextGrid = route.GridStops.GetValueAtIndex((int)relativeIndex);
                _shuttles.FTLToDock(shuttleEnt, shuttleComp, nextGrid, hyperspaceTime: (float)_initialHyperspaceTime.TotalSeconds, priorityTag: transitComp.DockTag);
                transitComp.CurrentGrid = nextGrid;
                transitComp.NextTransfer = _timing.CurTime + route.Prototype.WaitTime + extraTime + _initialHyperspaceTime;

                numBuses++;
            }
        }

        // the FTL sequence takes a few seconds to warm up and send the grid, so we give the temp dummy map
        // some buffer time before calling a self-delete
        var timer = AddComp<TimedDespawnComponent>(dummyMapUid);
        timer.Lifetime = 15f;
        RoutesCreated = true;
    }
}

sealed class PublicTransitRoute(PublicTransitRoutePrototype prototype)
{
    /// <summary>
    /// The prototype this route is based off of.
    /// </summary>
    public PublicTransitRoutePrototype Prototype = prototype;

    /// <summary>
    /// The list of grids this route stops at sorted by relative order.
    /// </summary>
    public SortedList<int, EntityUid> GridStops = new();

    /// <summary>
    /// The relative order (key in GridStops) and index of each stop by its UID
    /// </summary>
    public Dictionary<EntityUid, (int stopOrder, int stopIndex)> StopIndicesByGrid = new();
}
