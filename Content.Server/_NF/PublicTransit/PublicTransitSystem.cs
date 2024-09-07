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
using Robust.Shared.Map;
using Robust.Shared.Timing;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Server._NF.PublicTransit;

/// <summary>
/// If enabled, spawns a public trasnport grid as definied by cvar, to act as an automatic transit shuttle between designated grids
/// </summary>
public sealed class PublicTransitSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    /// <summary>
    /// If enabled then spawns the bus and sets up the bus line.
    /// </summary>
    public bool Enabled { get; private set; }
    public float FlyTime = 50f;
    public int Counter = 0;
    public List<EntityUid> StationList = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationTransitComponent, ComponentStartup>(OnStationStartup);
        SubscribeLocalEvent<StationTransitComponent, ComponentShutdown>(OnStationShutdown);
        SubscribeLocalEvent<TransitShuttleComponent, ComponentStartup>(OnShuttleStartup);
        SubscribeLocalEvent<TransitShuttleComponent, EntityUnpausedEvent>(OnShuttleUnpaused);
        SubscribeLocalEvent<TransitShuttleComponent, FTLCompletedEvent>(OnShuttleArrival);
        SubscribeLocalEvent<TransitShuttleComponent, FTLTagEvent>(OnShuttleTag);
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStart);

        Enabled = _cfgManager.GetCVar(NFCCVars.PublicTransit);
        FlyTime = _cfgManager.GetCVar(NFCCVars.PublicTransitFlyTime);
        Counter = 0;
        StationList.Clear();
        _cfgManager.OnValueChanged(NFCCVars.PublicTransit, SetTransit);
        _cfgManager.OnValueChanged(NFCCVars.PublicTransitFlyTime, SetFly);
    }

    public void OnRoundStart(RoundStartedEvent args)
    {
        Counter = 0;
        if (Enabled)
            SetupPublicTransit();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfgManager.UnsubValueChanged(NFCCVars.PublicTransitFlyTime, SetFly);
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
        args.Handled = true;
        args.Tag = "DockTransit";
    }

    /// <summary>
    /// Checks to make sure the grid is on the appropriate playfield, i.e., not in mapping space being worked on.
    /// If so, adds the grid to the list of bus stops, but only if its not already there
    /// </summary>
    private void OnStationStartup(EntityUid uid, StationTransitComponent component, ComponentStartup args)
    {
        if (Transform(uid).MapID == _ticker.DefaultMap) //best solution i could find because of componentinit/mapinit race conditions
        {
            if (!StationList.Contains(uid)) //if the grid isnt already in
                StationList.Add(uid); //add it to the list
        }
    }

    /// <summary>
    /// When a bus stop gets deleted in-game, we need to remove it from the list of bus stops, or else we get FTL problems
    /// </summary>
    private void OnStationShutdown(EntityUid uid, StationTransitComponent component, ComponentShutdown args)
    {
        if (StationList.Contains(uid))
            StationList.Remove(uid);
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
    }

    /// <summary>
    /// ensuring that pausing the shuttle for any reason doesnt mess up our timing
    /// </summary>
    private void OnShuttleUnpaused(EntityUid uid, TransitShuttleComponent component, ref EntityUnpausedEvent args)
    {
        component.NextTransfer += args.PausedTime;
    }

    private void OnShuttleArrival(EntityUid uid, TransitShuttleComponent comp, ref FTLCompletedEvent args)
    {
        var consoleQuery = EntityQueryEnumerator<ShuttleConsoleComponent>();

        while (consoleQuery.MoveNext(out var consoleUid, out _))
        {
            if (Transform(consoleUid).GridUid == uid && TryComp(comp.NextStation, out MetaDataComponent? metadata))
            {
                var destinationString = metadata.EntityName;

                _chat.TrySendInGameICMessage(consoleUid, Loc.GetString("public-transit-arrival",
                        ("destination", destinationString), ("waittime", _cfgManager.GetCVar(NFCCVars.PublicTransitWaitTime))),
                    InGameICChatType.Speak, ChatTransmitRange.HideChat, hideLog: true, checkRadioPrefix: false,
                    ignoreActionBlocker: true);
            }
        }
    }

    /// <summary>
    /// Here is our bus stop list handler. Theres probably a better way...
    /// First, sets our output to null just in case
    /// then, makes sure that our counter/index isnt out of range (reaching the end of the list will force you back to the beginning, like a loop)
    /// Then, it checks to make sure that there even is anything in the list
    /// and if so, we return the next station, and then increment our counter for the next time its ran
    /// </summary>
    private bool TryGetNextStation(out EntityUid? station)
    {
        station = null;

        if (Counter >= StationList.Count)
            Counter = 0;

        if (!(StationList.Count > 0))
            return false;

        station = StationList[Counter];
        Counter++;
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

        var query = EntityQueryEnumerator<TransitShuttleComponent, ShuttleComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var shuttle))
        {
            if (comp.NextTransfer > curTime)
                continue;

            var consoleQuery = EntityQueryEnumerator<ShuttleConsoleComponent>();

            while (consoleQuery.MoveNext(out var consoleUid, out _))
            {
                if (Transform(consoleUid).GridUid == uid && TryComp(comp.NextStation, out MetaDataComponent? metadata))
                {
                    var destinationString = metadata.EntityName;

                    _chat.TrySendInGameICMessage(consoleUid, Loc.GetString("public-transit-departure",
                        ("destination", destinationString), ("flytime", FlyTime)),
                        InGameICChatType.Speak, ChatTransmitRange.HideChat, hideLog: true, checkRadioPrefix: false,
                        ignoreActionBlocker: true);
                }
            }
            _shuttles.FTLToDock(uid, shuttle, comp.NextStation, hyperspaceTime: FlyTime, priorityTag: "DockTransit"); // TODO: Unhard code the priorityTag as it should be added from the system.

            if (TryGetNextStation(out var nextStation) && nextStation is { Valid: true } destination)
                comp.NextStation = destination;

            comp.NextTransfer = curTime + TimeSpan.FromSeconds(FlyTime + _cfgManager.GetCVar(NFCCVars.PublicTransitWaitTime));
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
    /// Simple cache reflection
    /// </summary>
    private void SetFly(float obj)
    {
        FlyTime = obj;
    }

    /// <summary>
    /// Here is where we handle setting up the transit system, including sanity checks.
    /// This is called multiple times, from a few different sources, to ensure that if the system is activated dynamically
    /// it will still function as intended
    /// </summary>
    private void SetupPublicTransit()
    {
        // If a public bus alraedy exists, we simply return. No need to set up the system again.
        var query = EntityQueryEnumerator<TransitShuttleComponent>();
        while (query.MoveNext(out var euid, out _))
        {
            if (!Deleted(euid))
                return;
        }

        // Spawn the bus onto a dummy map
        var dummyMap = _mapManager.CreateMap();
        var busMap = _cfgManager.GetCVar(NFCCVars.PublicTransitBusMap);
        if (_loader.TryLoad(dummyMap, busMap, out var shuttleUids))
        {
            var shuttleComp = Comp<ShuttleComponent>(shuttleUids[0]);
            // Here we are making sure that the shuttle has the TransitShuttle comp onto it, in case of dynamically changing the bus grid
            var transitComp = EnsureComp<TransitShuttleComponent>(shuttleUids[0]);

            //We run our bus station function to try to get a valid station to FTL to. If for some reason, there are no bus stops, we will instead just delete the shuttle
            if (TryGetNextStation(out var station) && station is { Valid : true } destination)
            {
                //we set up a default in case the second time we call it fails for some reason
                transitComp.NextStation = destination;
                _shuttles.FTLToDock(shuttleUids[0], shuttleComp, destination, hyperspaceTime: 5f);
                transitComp.NextTransfer = _timing.CurTime + TimeSpan.FromSeconds(_cfgManager.GetCVar(NFCCVars.PublicTransitWaitTime));

                //since the initial cached value of the next station is actually the one we are 'starting' from, we need to run the
                //bus stop list code one more time so that our first trip isnt just Frontier - Frontier
                if (TryGetNextStation(out var firstStop) && firstStop is { Valid : true } firstDestination)
                    transitComp.NextStation = firstDestination;
            }
            else
            {
                foreach (var shuttle in shuttleUids)
                {
                    QueueDel(shuttle);
                }
            }
        }

        // the FTL sequence takes a few seconds to warm up and send the grid, so we give the temp dummy map
        // some buffer time before calling a self-delete
        var timer = AddComp<TimedDespawnComponent>(_mapManager.GetMapEntityId(dummyMap));
        timer.Lifetime = 15f;
    }
}
