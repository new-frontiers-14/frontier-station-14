
using Content.Server.GameTicking.Events;
using Content.Server._NF.PublicTransit.Components;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared.NF14.CCVar;
using Content.Shared.Shuttles.Components;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Server._NF.PublicTransit;

/// <summary>
/// If enabled spawns players on a separate arrivals station before they can transfer to the main station.
/// </summary>
public sealed class PublicTransitSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;

    /// <summary>
    /// If enabled then spawns players on an alternate map so they can take a shuttle to the station.
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
        SubscribeLocalEvent<TransitShuttleComponent, FTLTagEvent>(OnShuttleTag);

        // Don't invoke immediately as it will get set in the natural course of things.
        Enabled = _cfgManager.GetCVar(NF14CVars.PublicTransit);
        FlyTime = _cfgManager.GetCVar(NF14CVars.PublicTransitFlyTime);
        Counter = 0;
        StationList.Clear();
        _cfgManager.OnValueChanged(NF14CVars.PublicTransit, SetTransit);
        _cfgManager.OnValueChanged(NF14CVars.PublicTransitFlyTime, SetFly);
    }

    private void OnShuttleTag(EntityUid uid, TransitShuttleComponent component, ref FTLTagEvent args)
    {
        if (args.Handled)
            return;

        // Just saves mappers forgetting.
        args.Handled = true;
        args.Tag = "DockTransit";
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfgManager.UnsubValueChanged(NF14CVars.PublicTransit, SetTransit);
    }

    private void OnStationStartup(EntityUid uid, StationTransitComponent component, ComponentStartup args)
    {
        if (Transform(uid).MapID == _ticker.DefaultMap)
        {
            if (!StationList.Contains(uid))
                StationList.Add(uid);
            if (Enabled)
                SetupPublicTransit();
        }
    }

    private void OnStationShutdown(EntityUid uid, StationTransitComponent component, ComponentShutdown args)
    {
        if (StationList.Contains(uid))
            StationList.Remove(uid);
    }

    private void OnShuttleStartup(EntityUid uid, TransitShuttleComponent component, ComponentStartup args)
    {
        EnsureComp<PreventPilotComponent>(uid);
        EnsureComp<ProtectedGridComponent>(uid);
    }

    private void OnShuttleUnpaused(EntityUid uid, TransitShuttleComponent component, ref EntityUnpausedEvent args)
    {
        component.NextTransfer += args.PausedTime;
    }

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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TransitShuttleComponent, ShuttleComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var shuttle))
        {
            if (comp.NextTransfer > curTime)
                continue;

            _shuttles.FTLTravel(uid, shuttle, comp.NextStation, hyperspaceTime: FlyTime, dock: true);

            if (TryGetNextStation(out var nextStation) && nextStation is {Valid : true} destination)
                comp.NextStation = destination;

            comp.NextTransfer += TimeSpan.FromSeconds(FlyTime + _cfgManager.GetCVar(NF14CVars.PublicTransitWaitTime));
        }
    }

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

    private void SetFly(float obj)
    {
        FlyTime = obj;
    }

    private void SetupPublicTransit()
    {
        var query = EntityQueryEnumerator<TransitShuttleComponent>();
        while (query.MoveNext(out var euid, out _))
        {
            if (!Deleted(euid))
                return;
        }
        // Spawn arrivals on a dummy map then dock it to the source.
        var dummyMap = _mapManager.CreateMap();
        var busMap = _cfgManager.GetCVar(NF14CVars.PublicTransitBusMap);
        if (_loader.TryLoad(dummyMap, busMap, out var shuttleUids))
        {
            var shuttleComp = Comp<ShuttleComponent>(shuttleUids[0]);
            var transitComp = EnsureComp<TransitShuttleComponent>(shuttleUids[0]);
            if (TryGetNextStation(out var station) && station is { Valid : true } destination)
            {
                transitComp.NextStation = destination; //we set up a default in case theres only 1
                // EnsureComp<StationEmpImmuneComponent>(uid); Enable in the case we want to ensure EMP immune grid

                _shuttles.FTLTravel(shuttleUids[0], shuttleComp, destination, hyperspaceTime: 5f, dock: true);
                transitComp.NextTransfer = _timing.CurTime +
                                           TimeSpan.FromSeconds(_cfgManager.GetCVar(NF14CVars.PublicTransitWaitTime));

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

        // Don't start the arrivals shuttle immediately docked so power has a time to stabilise?
        var timer = AddComp<TimedDespawnComponent>(_mapManager.GetMapEntityId(dummyMap));
        timer.Lifetime = 15f;
    }
}
