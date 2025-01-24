using Content.Server._NF.PublicTransit.Components;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared.GameTicking;
using Content.Shared._NF.CCVar;
using Content.Shared.Shuttles.Components;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;
using Content.Server.Station.Systems;
using Robust.Shared.Prototypes;
using Content.Server._NF.PublicTransit.Prototypes;
using System.Diagnostics.CodeAnalysis;
using Robust.Server.Maps;
using System.Numerics;
using Robust.Shared.Map.Components;
using Content.Shared.Shipyard.Prototypes;

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

    /// <summary>
    /// If enabled then spawns the bus and sets up the bus line.
    /// </summary>
    public bool Enabled { get; private set; }
    private Dictionary<ProtoId<PublicTransitRoutePrototype>, PublicTransitRoute> _routeList = new();
    private readonly TimeSpan _updatePeriod = TimeSpan.FromSeconds(2);
    private TimeSpan _nextUpdate = TimeSpan.FromSeconds(2);
    private const float ShuttleSpawnBuffer = 1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationTransitComponent, ComponentStartup>(OnStationStartup);
        SubscribeLocalEvent<StationTransitComponent, ComponentShutdown>(OnStationShutdown);
        SubscribeLocalEvent<TransitShuttleComponent, ComponentStartup>(OnShuttleStartup);
        SubscribeLocalEvent<TransitShuttleComponent, FTLCompletedEvent>(OnShuttleArrival);
        SubscribeLocalEvent<TransitShuttleComponent, FTLTagEvent>(OnShuttleTag);
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStart);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        Enabled = _cfgManager.GetCVar(NFCCVars.PublicTransit);
        _routeList.Clear();
        _cfgManager.OnValueChanged(NFCCVars.PublicTransit, SetTransit);
        _nextUpdate = _timing.CurTime;
    }

    public void OnRoundStart(RoundStartedEvent args)
    {
        if (Enabled)
            SetupPublicTransit();
    }

    public void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        _routeList.Clear();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfgManager.UnsubValueChanged(NFCCVars.PublicTransit, SetTransit);
    }


    /// <summary>
    /// Hardcoded snippit to intercept FTL events. It catches the transit shuttle and ensures its looking for the "DockTransit" priority dock.
    /// </summary>
    private void OnShuttleTag(EntityUid uid, TransitShuttleComponent component, ref FTLTagEvent args)
    {
        if (args.Handled)
            return;

        // Just saves mappers forgetting, or ensuring that a non-standard grid forced to be a bus will prioritize the "DockTransit" tagged docks
        args.Tag = component.DockTag;
        args.Handled = true;
    }

    /// <summary>
    /// Checks to make sure the grid is on the appropriate playfield, i.e., not in mapping space being worked on.
    /// If so, adds the grid to the list of bus stops, but only if its not already there
    /// </summary>
    private void OnStationStartup(EntityUid uid, StationTransitComponent component, ComponentStartup args)
    {
        if (Transform(uid).MapID != _ticker.DefaultMap) //best solution i could find because of componentinit/mapinit race conditions
            return;

        // Add each present route
        foreach (var route in component.Routes)
        {
            if (!_routeList.ContainsKey(route))
            {
                if (!_proto.TryIndex<PublicTransitRoutePrototype>(route.Id, out var routeProto))
                    continue;
                _routeList.Add(route, new PublicTransitRoute(routeProto));
            }
            _routeList[route].Stations.Add(uid); //add it to the list (TODO: add priority - stations could have a relative order)
        }
    }

    /// <summary>
    /// When a bus stop gets deleted in-game, we need to remove it from the list of bus stops, or else we get FTL problems
    /// </summary>
    private void OnStationShutdown(EntityUid uid, StationTransitComponent component, ComponentShutdown args)
    {
        foreach (var route in _routeList.Values)
            route.Stations.Remove(uid);
        // TODO: could add logic to rebalance the buses here.
    }

    /// <summary>
    /// Again, this can and likely should be instructed to mappers to do, but just in case it was either forgotten or we are doing admemes,
    /// we make sure that the bus is (mostly) griefer protected and that it cant be hijacked
    /// </summary>
    private void OnShuttleStartup(EntityUid uid, TransitShuttleComponent component, ComponentStartup args)
    {
        EnsureComp<PreventPilotComponent>(uid);
        var prot = EnsureComp<ProtectedGridComponent>(uid);
        prot.PreventArtifactTriggers = true;
        prot.PreventEmpEvents = true;
        prot.PreventExplosions = true;
        prot.PreventFloorPlacement = true;
        prot.PreventFloorRemoval = true;
        prot.PreventRCDUse = true;

        var stationName = Loc.GetString(component.Name);

        var meta = EnsureComp<MetaDataComponent>(uid);
        _meta.SetEntityName(uid, stationName, meta);

        _renameWarps.SyncWarpPointsToGrid(uid);
    }

    private void OnShuttleArrival(EntityUid uid, TransitShuttleComponent comp, ref FTLCompletedEvent args)
    {
        var consoleQuery = EntityQueryEnumerator<ShuttleConsoleComponent, TransformComponent>();

        while (consoleQuery.MoveNext(out var consoleUid, out _, out var xform))
        {
            if (xform.GridUid != uid)
                continue;

            // Find route details.
            if (!_routeList.TryGetValue(comp.RouteID, out var route))
                continue;

            // Note: the next grid is not cached in case stations are added or removed.
            if (!TryGetNextStation(route, comp.CurrentGrid, out var nextGrid))
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
    private bool TryGetNextStation(PublicTransitRoute route, EntityUid currentGrid, [NotNullWhen(true)] out EntityUid? nextGrid)
    {
        nextGrid = null;
        if (route.Stations.Count <= 0)
            return false;

        // If not in array, move to first item (-1 to 0).  If in array, move to next item (if last, revert to first).
        var currentIndex = route.Stations.FindIndex(ent => ent == currentGrid);
        nextGrid = route.Stations[currentIndex + 1 % route.Stations.Count];

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

            if (!_routeList.TryGetValue(comp.RouteID, out var route))
                continue;

            // Regardless of whether we have a station to go to, don't rerun the same conditions frequently.
            comp.NextTransfer = curTime + route.Prototype.TravelTime + route.Prototype.WaitTime;

            if (!TryGetNextStation(route, comp.CurrentGrid, out var nextGrid))
                continue; // NOTE: this bus is dead, should we despawn it?

            // FTL to next station if it exists.  Do this before the print.
            _shuttles.FTLToDock(uid, shuttle, nextGrid.Value, hyperspaceTime: route.Prototype.TravelTime.Seconds, priorityTag: comp.DockTag); // TODO: Unhard code the priorityTag as it should be added from the system.

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

        if (Enabled)
        {
            SetupPublicTransit();
        }
        else
        {
            var shuttleQuery = AllEntityQuery<TransitShuttleComponent>();

            while (shuttleQuery.MoveNext(out var uid, out _))
            {
                QueueDel(uid);
            }
        }
    }

    /// <summary>
    /// Here is where we handle setting up the transit system, including sanity checks.
    /// This is called multiple times, from a few different sources, to ensure that if the system is activated dynamically
    /// it will still function as intended
    /// </summary>
    /// <remarks>
    /// Bus scheduling may be clumped if 
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

        var dummyMapEnt = _map.CreateMap(out var dummyMap);
        var busMap = _cfgManager.GetCVar(NFCCVars.PublicTransitBusMap);

        // For each route: find out the number of buses we need on it, then add more buses until we get to that count.
        // Leave the excess buses for now.
        foreach (var route in _routeList.Values)
        {
            var numBuses = 0;
            if (busesByRoute.ContainsKey(route.Prototype.ID))
                numBuses = busesByRoute[route.Prototype.ID].Count;

            var neededBuses = 1;
            if (route.Prototype.StationsPerBus > 0)
                neededBuses += route.Stations.Count / route.Prototype.StationsPerBus;

            var shuttleOffset = 500.0f;

            if (numBuses >= neededBuses)
                continue;

            // TODO: default to 
            if (!_proto.TryIndex<VesselPrototype>(route.Prototype.BusVessel, out var busVessel))
                continue;

            while (numBuses < neededBuses)
            {
                var loadOptions = new MapLoadOptions()
                {
                    Offset = new Vector2(shuttleOffset, 1f)
                };

                // Spawn the bus onto a dummy map
                if (!_loader.TryLoad(dummyMap, busVessel.ShuttlePath.ToString(), out var shuttleUids, loadOptions) ||
                    !TryComp<MapGridComponent>(shuttleUids[0], out var mapGrid) ||
                    !TryComp<ShuttleComponent>(shuttleUids[0], out var shuttleComp))
                    break;

                shuttleOffset += mapGrid.LocalAABB.Width + ShuttleSpawnBuffer;

                // Here we are making sure that the shuttle has the TransitShuttle comp onto it, in case of dynamically changing the bus grid
                var transitComp = EnsureComp<TransitShuttleComponent>(shuttleUids[0]);
                transitComp.RouteID = route.Prototype.ID;
                transitComp.DockTag = route.Prototype.DockTag;
                var shuttleName = Loc.GetString("public-transit-shuttle-name", ("number", route.Prototype.RouteNumber), ("suffix", neededBuses > 1 ? (char)('A' + numBuses) : ""));

                // Space each bus out in the schedule.
                int index = numBuses * route.Stations.Count / neededBuses;

                //we set up a default in case the second time we call it fails for some reason
                _shuttles.FTLToDock(shuttleUids[0], shuttleComp, route.Stations[index], hyperspaceTime: 5f, priorityTag: transitComp.DockTag);
                transitComp.NextTransfer = _timing.CurTime + route.Prototype.WaitTime;

                numBuses++;
            }
        }

        // the FTL sequence takes a few seconds to warm up and send the grid, so we give the temp dummy map
        // some buffer time before calling a self-delete
        var timer = AddComp<TimedDespawnComponent>(dummyMapEnt);
        timer.Lifetime = 15f;
    }
}

sealed class PublicTransitRoute(PublicTransitRoutePrototype prototype)
{
    public PublicTransitRoutePrototype Prototype = prototype;
    public List<EntityUid> Stations = new();
}
