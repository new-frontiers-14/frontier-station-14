using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server._NF.GameTicking.Events;
using Content.Server._NF.PublicTransit.Components;
using Content.Server._NF.PublicTransit.Prototypes;
using Content.Server._NF.SectorServices;
using Content.Server._NF.Station.Systems;
using Content.Server.Chat.Systems;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._NF.CCVar;
using Content.Shared._NF.PublicTransit;
using Content.Shared._NF.PublicTransit.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Examine;
using Content.Shared.Random.Helpers;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
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
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly SectorServiceSystem _sectorService = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// If enabled then spawns the bus and sets up the bus line.
    /// </summary>
    public bool Enabled { get; private set; }

    private TimeSpan _hyperspaceTimePerRoute = TimeSpan.FromSeconds(10);
    private const float ShuttleSpawnBuffer = 4f;
    private const ushort TransitShuttleScreenFrequency = 10000;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationTransitComponent, ComponentStartup>(OnStationStartup);
        SubscribeLocalEvent<StationTransitComponent, ComponentRemove>(OnStationRemove);
        SubscribeLocalEvent<TransitShuttleComponent, FTLCompletedEvent>(OnShuttleArrival);
        SubscribeLocalEvent<TransitShuttleComponent, FTLTagEvent>(OnShuttleTag);
        SubscribeLocalEvent<PublicTransitVisualsComponent, MapInitEvent>(OnPublicTransitVisualsInit);
        SubscribeLocalEvent<BusScheduleComponent, ExaminedEvent>(OnScheduleExamined);
        SubscribeLocalEvent<StationsGeneratedEvent>(OnStationsGenerated);

        Enabled = _cfgManager.GetCVar(NFCCVars.PublicTransit);
        _cfgManager.OnValueChanged(NFCCVars.PublicTransit, SetTransit);
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

    private void OnPublicTransitVisualsInit(Entity<PublicTransitVisualsComponent> ent, ref MapInitEvent args)
    {
        TrySetGridVisuals(ent);
    }

    private bool TrySetGridVisuals(Entity<PublicTransitVisualsComponent> ent)
    {
        if (!TryComp(ent, out TransformComponent? xform))
            return false;

        PublicTransitRoutePrototype? transitRoute;

        // Exception: if this is a route-dedicated bus schedule, just get its route's livery colour
        if (TryComp(ent, out BusScheduleComponent? comp)
            && comp.RouteId != null
            && _proto.TryIndex(comp.RouteId, out transitRoute))
        {
            _appearance.SetData(ent, PublicTransitVisuals.Livery, transitRoute.LiveryColor);
            return true;
        }

        // Otherwise, check the grid we're on.
        if (TryComp(xform.GridUid, out TransitShuttleComponent? transitShuttle)
            && _proto.TryIndex(transitShuttle.RouteId, out transitRoute))
        {
            _appearance.SetData(ent, PublicTransitVisuals.Livery, transitRoute.LiveryColor);
            return true;
        }
        else if (TryComp(xform.GridUid, out StationTransitComponent? stationTransit)
            && stationTransit.Routes.Count > 0
            && _proto.TryIndex(stationTransit.Routes.First().Key, out transitRoute))
        {
            _appearance.SetData(ent, PublicTransitVisuals.Livery, transitRoute.LiveryColor);
            return true;
        }

        return false;
    }

    private void OnScheduleExamined(Entity<BusScheduleComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(BusScheduleComponent)))
        {
            if (!TryComp(ent, out TransformComponent? xform)
                || xform.GridUid == null)
            {
                args.PushMarkup(Loc.GetString("bus-schedule-no-bus"));
                return;
            }

            if (TryComp<TransitShuttleComponent>(xform.GridUid, out var transitShuttle))
            {
                // This is a bus, it only serves one route - even if the schedule is for a different route, give our info.
                PrintBusSchedule(transitShuttle.RouteId, (xform.GridUid.Value, transitShuttle), ref args);
            }
            else if (TryComp<StationTransitComponent>(xform.GridUid, out var stationTransit))
            {
                // Get the route associated with this grid.
                if (stationTransit.Routes.Count <= 0)
                {
                    args.PushMarkup(Loc.GetString("bus-schedule-no-bus"));
                    return;
                }

                var route = ent.Comp.RouteId;
                if (route == null)
                {
                    route = stationTransit.Routes.First().Key;
                }
                else if (!stationTransit.Routes.ContainsKey(route.Value))
                {
                    args.PushMarkup(Loc.GetString("bus-schedule-no-buses-on-route"));
                    return;
                }

                PrintStationSchedule(route.Value, xform.GridUid.Value, ref args);
            }
            else
            {
                // If any of the above have failed, or if no case was met, this thing isn't a bus and doesn't have bus service.
                args.PushMarkup(Loc.GetString("bus-schedule-no-bus"));
                return;
            }
        }
    }

    private void PrintBusSchedule(ProtoId<PublicTransitRoutePrototype> route, Entity<TransitShuttleComponent> grid, ref ExaminedEvent args)
    {
        if (!TryComp<SectorPublicTransitComponent>(_sectorService.GetServiceEntity(), out var sectorPublicTransit)
            || !sectorPublicTransit.Routes.TryGetValue(route, out var routeData)
            || !routeData.StopIndicesByGrid.TryGetValue(grid.Comp.CurrentGrid, out var destInfo))
        {
            args.PushMarkup(Loc.GetString("bus-schedule-no-stops-on-route"));
            return;
        }

        FormattedMessage message = new();
        message.AddMarkupPermissive(Loc.GetString("bus-schedule-arrival-header"));

        var arrivalTime = grid.Comp.NextTransfer - _timing.CurTime + routeData.Prototype.TravelTime;
        // On the way to the next grid
        int maxIndex = routeData.GridStops.Count;
        if (HasComp<FTLComponent>(grid))
        {
            var nextStopArrival = arrivalTime - routeData.Prototype.WaitTime - routeData.Prototype.TravelTime;
            message.PushNewline();
            if (nextStopArrival.TotalSeconds >= 1)
                message.AddMarkupPermissive(Loc.GetString("bus-schedule-arrival", ("station", Name(grid.Comp.CurrentGrid)), ("time", nextStopArrival.ToString(@"hh\:mm\:ss"))));
            else
                message.AddMarkupPermissive(Loc.GetString("bus-schedule-arrival-now", ("station", Name(grid.Comp.CurrentGrid))));
            maxIndex -= 1; // Don't double count the furthest index.
        }

        for (int i = 1; i <= maxIndex; i++)
        {
            var stopUid = routeData.GridStops.GetValueAtIndex((destInfo.stopIndex + i) % routeData.GridStops.Count);

            message.PushNewline();
            if (arrivalTime.TotalSeconds >= 1)
                message.AddMarkupPermissive(Loc.GetString("bus-schedule-arrival", ("station", Name(stopUid)), ("time", arrivalTime.ToString(@"hh\:mm\:ss"))));
            else
                message.AddMarkupPermissive(Loc.GetString("bus-schedule-arrival-now", ("station", Name(stopUid))));
            arrivalTime += routeData.Prototype.TravelTime + routeData.Prototype.WaitTime;
        }
        args.PushMessage(message);
    }

    private void PrintStationSchedule(ProtoId<PublicTransitRoutePrototype> route, EntityUid grid, ref ExaminedEvent args)
    {
        // Get stop index on requested route
        if (!TryComp<SectorPublicTransitComponent>(_sectorService.GetServiceEntity(), out var sectorPublicTransit)
            || !sectorPublicTransit.Routes.TryGetValue(route, out var routeData)
            || !routeData.StopIndicesByGrid.TryGetValue(grid, out var destInfo))
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
            if (busComp.RouteId != route)
                continue;

            // Compare the grid the bus is at (or going to) with our grid's info.
            if (!routeData.StopIndicesByGrid.TryGetValue(busComp.CurrentGrid, out var busInfo))
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
        var departureTime = nextBus.Comp.NextTransfer + stopDistance * (routeData.Prototype.TravelTime + routeData.Prototype.WaitTime) - _timing.CurTime;

        FormattedMessage message = new();
        if (departureTime.TotalSeconds >= 1)
            message.AddMarkupPermissive(Loc.GetString("bus-schedule-next-departure", ("bus", Name(nextBus)), ("time", departureTime.ToString(@"hh\:mm\:ss"))));
        else
            message.AddMarkupPermissive(Loc.GetString("bus-schedule-next-departure-now", ("bus", Name(nextBus))));
        message.PushNewline();
        message.AddMarkupPermissive(Loc.GetString("bus-schedule-arrival-header"));

        var arrivalTime = departureTime + routeData.Prototype.TravelTime;
        for (int i = 1; i <= routeData.GridStops.Count - 1; i++) // Don't double count our station.
        {
            var stopUid = routeData.GridStops.GetValueAtIndex((destInfo.stopIndex + i) % routeData.GridStops.Count);

            message.PushNewline();
            if (arrivalTime.TotalSeconds >= 1)
                message.AddMarkupPermissive(Loc.GetString("bus-schedule-arrival", ("station", Name(stopUid)), ("time", arrivalTime.ToString(@"hh\:mm\:ss"))));
            else
                message.AddMarkupPermissive(Loc.GetString("bus-schedule-arrival-now", ("station", Name(stopUid))));
            arrivalTime += routeData.Prototype.TravelTime + routeData.Prototype.WaitTime;
        }
        args.PushMessage(message);
    }

    private void OnStationsGenerated(StationsGeneratedEvent args)
    {
        if (!TryComp<SectorPublicTransitComponent>(_sectorService.GetServiceEntity(), out var sectorPublicTransit))
            return;

        if (Enabled && !sectorPublicTransit.RoutesCreated)
            SetupPublicTransit(sectorPublicTransit);

        sectorPublicTransit.StationsGenerated = true;
    }

    /// <summary>
    /// Checks to make sure the grid is on the appropriate playfield, i.e., not in mapping space being worked on.
    /// If so, adds the grid to the list of bus stops, but only if its not already there
    /// </summary>
    private void OnStationStartup(Entity<StationTransitComponent> ent, ref ComponentStartup args)
    {
        if (Transform(ent).MapID != _ticker.DefaultMap) //best solution i could find because of componentinit/mapinit race conditions
            return;

        if (!TryComp<SectorPublicTransitComponent>(_sectorService.GetServiceEntity(), out var sectorPublicTransit))
            return;

        // Add each present route
        foreach (var (routeId, routeIndex) in ent.Comp.Routes)
        {
            if (!sectorPublicTransit.Routes.TryGetValue(routeId, out var route))
            {
                if (!_proto.TryIndex(routeId, out var routeProto))
                    continue;
                route = new PublicTransitRoute(routeProto);
                sectorPublicTransit.Routes.Add(routeId, route);
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
        if (!TryComp<SectorPublicTransitComponent>(_sectorService.GetServiceEntity(), out var sectorPublicTransit))
            return;

        foreach (var route in sectorPublicTransit.Routes.Values)
        {
            var index = route.GridStops.IndexOfValue(ent);
            if (index != -1)
                route.GridStops.RemoveAt(index);

            CalculateGridIndices(route);
        }
        // TODO: could add logic to rebalance the buses here.
    }

    private void OnShuttleArrival(Entity<TransitShuttleComponent> ent, ref FTLCompletedEvent args)
    {
        if (!TryComp<SectorPublicTransitComponent>(_sectorService.GetServiceEntity(), out var sectorPublicTransit))
            return;

        var consoleQuery = EntityQueryEnumerator<ShuttleConsoleComponent, TransformComponent>();

        while (consoleQuery.MoveNext(out var consoleUid, out _, out var xform))
        {
            if (xform.GridUid != ent)
                continue;

            // Find route details.
            if (!sectorPublicTransit.Routes.TryGetValue(ent.Comp.RouteId, out var route))
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

        if (!TryComp<SectorPublicTransitComponent>(_sectorService.GetServiceEntity(), out var sectorPublicTransit))
            return;

        var curTime = _timing.CurTime;
        // Update periodically, no need to have the buses on time to the millisecond.
        if (sectorPublicTransit.NextUpdate > curTime)
            return;
        sectorPublicTransit.NextUpdate = curTime + sectorPublicTransit.UpdatePeriod;

        var query = EntityQueryEnumerator<TransitShuttleComponent, ShuttleComponent>();

        while (query.MoveNext(out var uid, out var comp, out var shuttle))
        {
            if (comp.NextTransfer > curTime)
                continue;

            if (!sectorPublicTransit.Routes.TryGetValue(comp.RouteId, out var route))
                continue;

            // Regardless of whether we have a station to go to, don't rerun the same conditions frequently.
            comp.NextTransfer = curTime + route.Prototype.TravelTime + route.Prototype.WaitTime;

            if (!TryGetNextStop(route, comp.CurrentGrid, out var nextGrid))
                continue; // NOTE: this bus is dead, should we despawn it?

            // FTL to next station if it exists.  Do this before the print.
            var hyperspaceTime = MathF.Max(0.0f, (float)route.Prototype.TravelTime.TotalSeconds - _shuttles.DefaultStartupTime);
            _shuttles.FTLToDock(uid, shuttle, nextGrid.Value, startupTime: _shuttles.DefaultStartupTime, hyperspaceTime: hyperspaceTime, priorityTag: comp.DockTag); // TODO: Unhard code the priorityTag as it should be added from the system.
            comp.CurrentGrid = nextGrid.Value;

            if (!TryComp(nextGrid, out MetaDataComponent? metadata))
                continue;

            var consoleQuery = EntityQueryEnumerator<ShuttleConsoleComponent, TransformComponent>();

            while (consoleQuery.MoveNext(out var consoleUid, out _, out var xform))
            {
                if (xform.GridUid != uid)
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

            if (TryComp<SectorPublicTransitComponent>(_sectorService.GetServiceEntity(), out var publicTransit))
                publicTransit.RoutesCreated = false;
        }
        else if (TryComp<SectorPublicTransitComponent>(_sectorService.GetServiceEntity(), out var publicTransit)
                && !publicTransit.RoutesCreated
                && publicTransit.StationsGenerated)
        {
            SetupPublicTransit(publicTransit);
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
    private void SetupPublicTransit(SectorPublicTransitComponent comp)
    {
        Dictionary<ProtoId<PublicTransitRoutePrototype>, List<EntityUid>> busesByRoute = new();
        // Count the existing buses.
        var query = EntityQueryEnumerator<TransitShuttleComponent>();
        while (query.MoveNext(out var ent, out var transit))
        {
            if (!busesByRoute.ContainsKey(transit.RouteId))
                busesByRoute[transit.RouteId] = new();
            busesByRoute[transit.RouteId].Add(ent);
        }

        // Set up bus depot
        // NOTE: this only works with one depot at the moment.
        foreach (var route in comp.Routes.Values)
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
            foreach (var route in comp.Routes.Values)
            {
                if (route.GridStops.Count <= 0)
                    continue;

                route.GridStops.Add(0, depotGrid.Value);
                transit.Routes[route.Prototype.ID] = 0;
                // Route changed, recalculate grid indices
                CalculateGridIndices(route);
            }
        }

        var shuttleOffset = 500.0f;
        var dummyMapUid = _map.CreateMap(out var dummyMap);
        var initialHyperspaceTime = _hyperspaceTimePerRoute;

        // For each route: find out the number of buses we need on it, then add more buses until we get to that count.
        // Leave the excess buses for now.
        foreach (var route in comp.Routes.Values)
        {
            var numBuses = 0;
            if (busesByRoute.TryGetValue(route.Prototype.ID, out var routeBuses))
                numBuses = routeBuses.Count;

            var neededBuses = 1;
            if (route.Prototype.StationsPerBus > 0)
                neededBuses += route.GridStops.Count / route.Prototype.StationsPerBus;

            if (numBuses >= neededBuses)
                continue;

            var routeHopTime = route.Prototype.WaitTime + route.Prototype.TravelTime;

            while (numBuses < neededBuses)
            {
                var busProto = _random.Pick(route.Prototype.BusVessels);
                if (!_proto.TryIndex(busProto, out var busVessel))
                    continue;

                // Spawn the bus onto a dummy map
                if (!_loader.TryLoadGrid(dummyMap, busVessel.ShuttlePath, out var shuttleMaybe, offset: new Vector2(shuttleOffset, 1f))
                    || shuttleMaybe is not { } shuttleEnt
                    || !TryComp<MapGridComponent>(shuttleEnt, out var mapGrid)
                    || !TryComp<ShuttleComponent>(shuttleEnt, out var shuttleComp))
                {
                    break;
                }

                shuttleOffset += mapGrid.LocalAABB.Width + ShuttleSpawnBuffer;

                // Here we are making sure that the shuttle has the TransitShuttle comp onto it, in case of dynamically changing the bus grid.
                var transitComp = EnsureComp<TransitShuttleComponent>(shuttleEnt.Owner);
                transitComp.RouteId = route.Prototype.ID;
                transitComp.DockTag = route.Prototype.DockTag;
                var busSuffix = (char)('A' + numBuses);
                transitComp.ScreenText = Loc.GetString("public-transit-shuttle-screen-text", ("number", route.Prototype.RouteNumber), ("suffix", busSuffix));

                EnsureComp<PreventPilotComponent>(shuttleEnt.Owner);

                var shuttleName = Loc.GetString("public-transit-shuttle-name", ("number", route.Prototype.RouteNumber), ("suffix", busSuffix));

                // Set both the bus grid and station name, adjust warp points
                _meta.SetEntityName(shuttleEnt.Owner, shuttleName);
                if (_proto.TryIndex<GameMapPrototype>(busVessel.ID, out var stationProto))
                {
                    var shuttleStation = _station.InitializeNewStation(stationProto.Stations[busVessel.ID], [shuttleEnt], shuttleName);
                }
                _renameWarps.SyncWarpPointsToGrid(shuttleEnt);

                // Space each bus out in the schedule (in the next station if fractional time remaining, with that time added to the delay before leaving)
                var relativePosition = (float)(numBuses * route.GridStops.Count) / neededBuses;
                var relativeIndex = MathF.Ceiling(relativePosition);
                var extraTime = (relativeIndex - relativePosition) * routeHopTime;

                // We set up a default in case the second time we call it fails for some reason
                var nextGrid = route.GridStops.GetValueAtIndex((int)relativeIndex);
                _shuttles.FTLToDock(shuttleEnt, shuttleComp, nextGrid, hyperspaceTime: (float)initialHyperspaceTime.TotalSeconds, priorityTag: transitComp.DockTag);
                transitComp.CurrentGrid = nextGrid;
                transitComp.NextTransfer = _timing.CurTime + route.Prototype.WaitTime + extraTime + initialHyperspaceTime;

                // Set up the screen text on the bus
                var netComp = EnsureComp<DeviceNetworkComponent>(shuttleEnt);
                _deviceNetwork.SetTransmitFrequency(shuttleEnt, TransitShuttleScreenFrequency, netComp);
                var payload = new NetworkPayload
                {
                    [ScreenMasks.Text] = transitComp.ScreenText,
                    [ScreenMasks.LocalGrid] = shuttleEnt.Owner,
                };
                _deviceNetwork.QueuePacket(shuttleEnt, null, payload, TransitShuttleScreenFrequency, device: netComp);

                numBuses++;
            }

            // Space out routes so they don't all FTL at once.
            initialHyperspaceTime += _hyperspaceTimePerRoute;
        }

        // the FTL sequence takes a few seconds to warm up and send the grid, so we give the temp dummy map
        // some buffer time before calling a self-delete
        var timer = AddComp<TimedDespawnComponent>(dummyMapUid);
        timer.Lifetime = (float)initialHyperspaceTime.TotalSeconds + 10f;
        comp.RoutesCreated = true;

        // Set up livery for route-based visuals
        var visualsEnumerator = EntityQueryEnumerator<PublicTransitVisualsComponent>();
        while (visualsEnumerator.MoveNext(out var visualUid, out var visualComp))
        {
            TrySetGridVisuals((visualUid, visualComp));
        }
    }
}
