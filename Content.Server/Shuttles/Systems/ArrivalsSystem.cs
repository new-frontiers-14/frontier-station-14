using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Spawners.Components;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Shuttles.Systems;

/// <summary>
/// If enabled spawns players on a separate arrivals station before they can transfer to the main station.
/// </summary>
public sealed class ArrivalsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StationSystem _station = default!;

    /// <summary>
    /// If enabled then spawns players on an alternate map so they can take a shuttle to the station.
    /// </summary>
    public bool Enabled { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawn, before: new []{typeof(SpawnPointSystem)});
        SubscribeLocalEvent<StationArrivalsComponent, ComponentStartup>(OnArrivalsStartup);

        SubscribeLocalEvent<ArrivalsShuttleComponent, ComponentStartup>(OnShuttleStartup);
        SubscribeLocalEvent<ArrivalsShuttleComponent, EntityUnpausedEvent>(OnShuttleUnpaused);
        SubscribeLocalEvent<ArrivalsShuttleComponent, FTLTagEvent>(OnShuttleTag);

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<ArrivalsShuttleComponent, FTLStartedEvent>(OnArrivalsFTL);

        // Don't invoke immediately as it will get set in the natural course of things.
        Enabled = _cfgManager.GetCVar(CCVars.ArrivalsShuttles);
        _cfgManager.OnValueChanged(CCVars.ArrivalsShuttles, SetArrivals);

        // Command so admins can set these for funsies
        _console.RegisterCommand("arrivals", ArrivalsCommand, ArrivalsCompletion);
    }

    private void OnShuttleTag(EntityUid uid, ArrivalsShuttleComponent component, ref FTLTagEvent args)
    {
        if (args.Handled)
            return;

        // Just saves mappers forgetting. (v2 boogaloo)
        args.Handled = true;
        args.Tag = "DockArrivals";
    }

    private CompletionResult ArrivalsCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        return new CompletionResult(new CompletionOption[]
        {
            // Enables and disable are separate comms in case you don't want to accidentally toggle it, compared to
            // returns which doesn't have an immediate effect
            new("enable", Loc.GetString("cmd-arrivals-enable-hint")),
            new("disable", Loc.GetString("cmd-arrivals-disable-hint")),
            new("returns", Loc.GetString("cmd-arrivals-returns-hint")),
            new ("force", Loc.GetString("cmd-arrivals-force-hint"))
        }, "Option");
    }

    [AdminCommand(AdminFlags.Fun)]
    private void ArrivalsCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-arrivals-invalid"));
            return;
        }

        switch (args[0])
        {
            case "enable":
                _cfgManager.SetCVar(CCVars.ArrivalsShuttles, true);
                break;
            case "disable":
                _cfgManager.SetCVar(CCVars.ArrivalsShuttles, false);
                break;
            case "returns":
                var existing = _cfgManager.GetCVar(CCVars.ArrivalsReturns);
                _cfgManager.SetCVar(CCVars.ArrivalsReturns, !existing);
                shell.WriteLine(Loc.GetString("cmd-arrivals-returns", ("value", !existing)));
                break;
            case "force":
                var query = AllEntityQuery<PendingClockInComponent, TransformComponent>();
                var spawnPoints = EntityQuery<SpawnPointComponent, TransformComponent>().ToList();

                TryGetArrivals(out var arrivalsUid);

                while (query.MoveNext(out var uid, out _, out var pendingXform))
                {
                    _random.Shuffle(spawnPoints);

                    foreach (var (point, xform) in spawnPoints)
                    {
                        if (point.SpawnType != SpawnPointType.LateJoin || xform.GridUid == arrivalsUid)
                            continue;

                        _transform.SetCoordinates(uid, pendingXform, xform.Coordinates);
                        break;
                    }

                    RemCompDeferred<AutoOrientComponent>(uid);
                    RemCompDeferred<PendingClockInComponent>(uid);
                    shell.WriteLine(Loc.GetString("cmd-arrivals-forced", ("uid", ToPrettyString(uid))));
                }
                break;
            default:
                shell.WriteError(Loc.GetString($"cmd-arrivals-invalid"));
                break;
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfgManager.UnsubValueChanged(CCVars.ArrivalsShuttles, SetArrivals);
    }

    private void OnArrivalsFTL(EntityUid shuttleUid, ArrivalsShuttleComponent component, ref FTLStartedEvent args)
    {
        if (!TryGetArrivals(out EntityUid arrivals))
            return;

        var arrivalsMapUid = Transform(arrivals).MapUid;
        // Don't do anything here when leaving arrivals.
        if (args.FromMapUid == arrivalsMapUid)
            return;

        // Any mob then yeet them off the shuttle.
        if (!_cfgManager.GetCVar(CCVars.ArrivalsReturns) && args.FromMapUid != null)
        {
            var pendingEntQuery = GetEntityQuery<PendingClockInComponent>();
            var arrivalsBlacklistQuery = GetEntityQuery<ArrivalsBlacklistComponent>();
            var mobQuery = GetEntityQuery<MobStateComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            DumpChildren(shuttleUid, ref args, pendingEntQuery, arrivalsBlacklistQuery, mobQuery, xformQuery);
        }

        var pendingQuery = AllEntityQuery<PendingClockInComponent, TransformComponent>();

        // We're heading from the station back to arrivals (if leaving arrivals, would have returned above).
        // Process everyone who holds a PendingClockInComponent
        // Note, due to way DumpChildren works, anyone who doesn't have a PendingClockInComponent gets left in space
        // and will not warp. This is intended behavior.
        while (pendingQuery.MoveNext(out var pUid, out _, out var xform))
        {
            if (xform.GridUid == shuttleUid)
            {
                // Warp all players who are still on this shuttle to a spawn point. This doesn't let them return to
                // arrivals. It also ensures noobs, slow players or AFK players safely leave the shuttle.
                TryTeleportToMapSpawn(pUid, component.Station, xform);
            }

            // Players who have remained at arrives keep their warp coupon (PendingClockInComponent) for now.
            if (xform.MapUid == arrivalsMapUid)
                continue;

            // The player has successfully left arrivals and is also not on the shuttle. Remove their warp coupon.
            RemCompDeferred<PendingClockInComponent>(pUid);
            RemCompDeferred<AutoOrientComponent>(pUid);
        }
    }

    private void DumpChildren(EntityUid uid,
        ref FTLStartedEvent args,
        EntityQuery<PendingClockInComponent> pendingEntQuery,
        EntityQuery<ArrivalsBlacklistComponent> arrivalsBlacklistQuery,
        EntityQuery<MobStateComponent> mobQuery,
        EntityQuery<TransformComponent> xformQuery)
    {
        if (pendingEntQuery.HasComponent(uid))
            return;

        var xform = xformQuery.GetComponent(uid);

        if (mobQuery.HasComponent(uid) || arrivalsBlacklistQuery.HasComponent(uid))
        {
            var rotation = xform.LocalRotation;
            _transform.SetCoordinates(uid, new EntityCoordinates(args.FromMapUid!.Value, args.FTLFrom.Transform(xform.LocalPosition)));
            _transform.SetWorldRotation(uid, args.FromRotation + rotation);
            return;
        }

        var children = xform.ChildEnumerator;

        while (children.MoveNext(out var child))
        {
            DumpChildren(child.Value, ref args, pendingEntQuery, arrivalsBlacklistQuery, mobQuery, xformQuery);
        }
    }

    private void OnPlayerSpawn(PlayerSpawningEvent ev)
    {
        // Only works on latejoin even if enabled.
        if (!Enabled || _ticker.RunLevel != GameRunLevel.InRound)
            return;

        if (!HasComp<StationArrivalsComponent>(ev.Station))
            return;

        TryGetArrivals(out var arrivals);

        if (TryComp<TransformComponent>(arrivals, out var arrivalsXform))
        {
            var mapId = arrivalsXform.MapID;

            var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            var possiblePositions = new List<EntityCoordinates>();
            while ( points.MoveNext(out var uid, out var spawnPoint, out var xform))
            {
                if (spawnPoint.SpawnType != SpawnPointType.LateJoin || xform.MapID != mapId)
                    continue;

                possiblePositions.Add(xform.Coordinates);
            }

            if (possiblePositions.Count > 0)
            {
                var spawnLoc = _random.Pick(possiblePositions);
                ev.SpawnResult = _stationSpawning.SpawnPlayerMob(
                    spawnLoc,
                    ev.Job,
                    ev.HumanoidCharacterProfile,
                    ev.Station);

                EnsureComp<PendingClockInComponent>(ev.SpawnResult.Value);
                EnsureComp<AutoOrientComponent>(ev.SpawnResult.Value);
            }
        }
    }

    private bool TryTeleportToMapSpawn(EntityUid player, EntityUid stationId, TransformComponent? transform = null)
    {
        if (!Resolve(player, ref transform))
            return false;

        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new ValueList<EntityCoordinates>(32);

        // Find a spawnpoint on the same map as the player is already docked with now.
        while ( points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (spawnPoint.SpawnType == SpawnPointType.LateJoin &&
                _station.GetOwningStation(uid, xform) == stationId)
            {
                // Add to list of possible spawn locations
                possiblePositions.Add(xform.Coordinates);
            }
        }

        if (possiblePositions.Count > 0)
        {
            // Move the player to a random late-join spawnpoint.
            _transform.SetCoordinates(player, transform, _random.Pick(possiblePositions));
            return true;
        }

        return false;
    }

    private void OnShuttleStartup(EntityUid uid, ArrivalsShuttleComponent component, ComponentStartup args)
    {
        EnsureComp<PreventPilotComponent>(uid);
    }

    private void OnShuttleUnpaused(EntityUid uid, ArrivalsShuttleComponent component, ref EntityUnpausedEvent args)
    {
        component.NextTransfer += args.PausedTime;
    }

    private bool TryGetArrivals(out EntityUid uid)
    {
        var arrivalsQuery = EntityQueryEnumerator<ArrivalsSourceComponent>();

        while (arrivalsQuery.MoveNext(out uid, out _))
        {
            return true;
        }

        return false;
    }

    public TimeSpan? NextShuttleArrival()
    {
        var query = EntityQueryEnumerator<ArrivalsShuttleComponent>();
        var time = TimeSpan.MaxValue;
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextArrivalsTime < time)
                time = comp.NextArrivalsTime;
        }

        var duration = _timing.CurTime;
        return (time < duration) ? null : time - duration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ArrivalsShuttleComponent, ShuttleComponent, TransformComponent>();
        var curTime = _timing.CurTime;
        TryGetArrivals(out var arrivals);

        // TODO: FTL fucker, if on an edge tile every N seconds check for wall or w/e
        // TODO: Docking should be per-grid rather than per dock and bump off when undocking.

        // TODO: Stop dispatch if emergency shuttle has arrived.
        // TODO: Need server join message specifying shuttle wait time or smth.
        // TODO: Need maps
        // TODO: Need emergency suits on shuttle probs
        // TODO: Need some kind of comp to shunt people off if they try to get on?
        if (TryComp<TransformComponent>(arrivals, out var arrivalsXform))
        {
            while (query.MoveNext(out var uid, out var comp, out var shuttle, out var xform))
            {
                if (comp.NextTransfer > curTime || !TryComp<StationDataComponent>(comp.Station, out var data))
                    continue;

                var tripTime = ShuttleSystem.DefaultTravelTime + ShuttleSystem.DefaultStartupTime ;

                // Go back to arrivals source
                if (xform.MapUid != arrivalsXform.MapUid)
                {
                    if (arrivals.IsValid())
                        _shuttles.FTLTravel(uid, shuttle, arrivals, dock: true);

                    comp.NextArrivalsTime = _timing.CurTime + TimeSpan.FromSeconds(tripTime);
                }
                // Go to station
                else
                {
                    var targetGrid = _station.GetLargestGrid(data);

                    if (targetGrid != null)
                        _shuttles.FTLTravel(uid, shuttle, targetGrid.Value, dock: true);

                    // The ArrivalsCooldown includes the trip there, so we only need to add the time taken for
                    // the trip back.
                    comp.NextArrivalsTime = _timing.CurTime + TimeSpan.FromSeconds(
                        _cfgManager.GetCVar(CCVars.ArrivalsCooldown) + tripTime);
                }

                comp.NextTransfer += TimeSpan.FromSeconds(_cfgManager.GetCVar(CCVars.ArrivalsCooldown));
            }
        }
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        // Setup arrivals station
        if (!Enabled)
            return;

        SetupArrivalsStation();
    }

    private void SetupArrivalsStation()
    {
        var mapId = _mapManager.CreateMap();

        if (!_loader.TryLoad(mapId, _cfgManager.GetCVar(CCVars.ArrivalsMap), out var uids))
        {
            return;
        }

        foreach (var id in uids)
        {
            EnsureComp<ArrivalsSourceComponent>(id);
            EnsureComp<ProtectedGridComponent>(id);
            EnsureComp<PreventPilotComponent>(id);
        }

        // Handle roundstart stations.
        var query = AllEntityQuery<StationArrivalsComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            SetupShuttle(uid, comp);
        }
    }

    private void SetArrivals(bool obj)
    {
        Enabled = obj;

        if (Enabled)
        {
            SetupArrivalsStation();
            var query = AllEntityQuery<StationArrivalsComponent>();

            while (query.MoveNext(out var sUid, out var comp))
            {
                SetupShuttle(sUid, comp);
            }
        }
        else
        {
            var sourceQuery = AllEntityQuery<ArrivalsSourceComponent>();

            while (sourceQuery.MoveNext(out var uid, out _))
            {
                QueueDel(uid);
            }

            var shuttleQuery = AllEntityQuery<ArrivalsShuttleComponent>();

            while (shuttleQuery.MoveNext(out var uid, out _))
            {
                QueueDel(uid);
            }
        }
    }

    private void OnArrivalsStartup(EntityUid uid, StationArrivalsComponent component, ComponentStartup args)
    {
        if (!Enabled)
            return;

        // If it's a latespawn station then this will fail but that's okey
        SetupShuttle(uid, component);
    }

    private void SetupShuttle(EntityUid uid, StationArrivalsComponent component)
    {
        if (!Deleted(component.Shuttle))
            return;

        // Spawn arrivals on a dummy map then dock it to the source.
        var dummyMap = _mapManager.CreateMap();

        if (TryGetArrivals(out var arrivals) &&
            _loader.TryLoad(dummyMap, component.ShuttlePath.ToString(), out var shuttleUids))
        {
            component.Shuttle = shuttleUids[0];
            var shuttleComp = Comp<ShuttleComponent>(component.Shuttle);
            var arrivalsComp = EnsureComp<ArrivalsShuttleComponent>(component.Shuttle);
            arrivalsComp.Station = uid;
            EnsureComp<ProtectedGridComponent>(uid);
            _shuttles.FTLTravel(component.Shuttle, shuttleComp, arrivals, hyperspaceTime: 10f, dock: true);
            arrivalsComp.NextTransfer = _timing.CurTime + TimeSpan.FromSeconds(_cfgManager.GetCVar(CCVars.ArrivalsCooldown));
        }

        // Don't start the arrivals shuttle immediately docked so power has a time to stabilise?
        var timer = AddComp<TimedDespawnComponent>(_mapManager.GetMapEntityId(dummyMap));
        timer.Lifetime = 15f;
    }
}
