using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.DeviceLinking.Systems;
using Content.Shared._FarHorizons.Power.Generation.FissionGenerator;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Power.Generation.FissionGenerator;

public sealed partial class GasTurbineMonitorSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly TurbineSystem _turbineSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly DeviceLinkSystem _signal = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = null!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly float _threshold = 0.5f;
    private float _accumulator = 0f;

    private sealed class LogData
    {
        public TimeSpan CreationTime;
        public NetEntity Turbine;
        public float? SetFlowRate;
        public float? SetStatorLoad;
    }

    private readonly Dictionary<KeyValuePair<EntityUid, EntityUid>, LogData> _logQueue = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasTurbineMonitorComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<GasTurbineMonitorComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<GasTurbineMonitorComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<GasTurbineMonitorComponent, TurbineChangeFlowRateMessage>(OnTurbineFlowRateChanged);
        SubscribeLocalEvent<GasTurbineMonitorComponent, TurbineChangeStatorLoadMessage>(OnTurbineStatorLoadChanged);

        SubscribeLocalEvent<GasTurbineMonitorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnMapInit(EntityUid uid, GasTurbineMonitorComponent comp, ref MapInitEvent args)
    {
        if (!_entityManager.TryGetComponent<DeviceLinkSinkComponent>(uid, out var sink))
            return;

        foreach (var source in sink.LinkedSources)
        {
            if (!HasComp<TurbineComponent>(source))
                continue;

            comp.turbine = GetNetEntity(source);
            Dirty(uid, comp);
            return; // The return is to make it behave such that the first connetion that's a turbine is the one chosen
        }
    }

    private void OnNewLink(EntityUid uid, GasTurbineMonitorComponent comp, ref NewLinkEvent args)
    {
        if (!HasComp<TurbineComponent>(args.Source))
            return;

        comp.turbine = GetNetEntity(args.Source);
        Dirty(uid, comp);
    }

    private void OnPortDisconnected(EntityUid uid, GasTurbineMonitorComponent comp, ref PortDisconnectedEvent args)
    {
        if (args.Port != comp.LinkingPort)
            return;

        comp.turbine = null;
        Dirty(uid, comp);
    }

    public bool TryGetTurbineComp(GasTurbineMonitorComponent turbineMonitor, [NotNullWhen(true)] out TurbineComponent? turbineComponent)
    {
        turbineComponent = null;
        if (!_entityManager.TryGetEntity(turbineMonitor.turbine, out var turbineUid) || turbineUid == null)
            return false;

        if (!_entityManager.TryGetComponent<TurbineComponent>(turbineUid, out var turbine))
            return false;

        turbineComponent = turbine;
        return true;
    }

    #region BUI
    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator > _threshold)
        {
            AccUpdate();
            UpdateLogs();
            _accumulator = 0;
        }

        return;

        void UpdateLogs()
        {
            var toRemove = new List<KeyValuePair<EntityUid, EntityUid>>();
            foreach (var log in _logQueue.Where(log => !((_gameTiming.RealTime - log.Value.CreationTime).TotalSeconds < 2)))
            {
                toRemove.Add(log.Key);

                if (log.Value.SetFlowRate != null)
                    _adminLogger.Add(LogType.AtmosVolumeChanged, LogImpact.Medium,
                        $"{ToPrettyString(log.Key.Key):player} set the flow rate on {ToPrettyString(log.Value.Turbine):device} to {log.Value.SetFlowRate} through {ToPrettyString(log.Key.Value):monitor}");

                if (log.Value.SetStatorLoad != null)
                    _adminLogger.Add(LogType.AtmosDeviceSetting, LogImpact.Medium,
                        $"{ToPrettyString(log.Key.Key):player} set the stator load on {ToPrettyString(log.Value.Turbine):device} to {log.Value.SetStatorLoad} through {ToPrettyString(log.Key.Value):monitor}");
            }

            foreach (var kvp in toRemove)
                _logQueue.Remove(kvp);
        }
    }

    private void AccUpdate()
    {
        var query = EntityQueryEnumerator<GasTurbineMonitorComponent>();

        while (query.MoveNext(out var uid, out var turbineMonitor))
        {
            CheckRange(uid, turbineMonitor);
            if (!TryGetTurbineComp(turbineMonitor, out var turbine))
                continue;

            _turbineSystem.UpdateUI(uid, turbine);
        }
    }

    private void OnTurbineFlowRateChanged(EntityUid uid, GasTurbineMonitorComponent comp, TurbineChangeFlowRateMessage args)
    {
        if (!TryGetTurbineComp(comp, out var turbine))
            return;

        if(TrySetFlowRate())
        {
            // Data is sent to a log queue to avoid spamming the admin log when adjusting values rapidly
            var key = new KeyValuePair<EntityUid, EntityUid>(args.Actor, uid);
            if(!_logQueue.TryGetValue(key, out var value))
                _logQueue.Add(key, new LogData
                {
                    CreationTime = _gameTiming.RealTime,
                    Turbine = comp.turbine!.Value,
                    SetFlowRate = turbine.FlowRate
                });
            else
                value.SetFlowRate = turbine.FlowRate;
        }
            
        _turbineSystem.UpdateUI(uid, turbine);

        return;

        bool TrySetFlowRate()
        {
            var newSet = Math.Clamp(args.FlowRate, 0f, turbine.FlowRateMax);
            if (turbine.FlowRate != newSet)
            {
                turbine.FlowRate = newSet;
                return true;
            }
            return false; 
        }
    }

    private void OnTurbineStatorLoadChanged(EntityUid uid, GasTurbineMonitorComponent comp, TurbineChangeStatorLoadMessage args)
    {
        if (!TryGetTurbineComp(comp, out var turbine))
            return;
        
        if (TrySetStatorLoad())
        {
            // Data is sent to a log queue to avoid spamming the admin log when adjusting values rapidly
            var key = new KeyValuePair<EntityUid, EntityUid>(args.Actor, uid);
            if (!_logQueue.TryGetValue(key, out var value))
                _logQueue.Add(key, new LogData
                {
                    CreationTime = _gameTiming.RealTime,
                    Turbine = comp.turbine!.Value,
                    SetStatorLoad = turbine.StatorLoad
                });
            else
                value.SetStatorLoad = turbine.StatorLoad;
        }

        _turbineSystem.UpdateUI(uid, turbine);

        return;

        bool TrySetStatorLoad()
        {
            var newSet = Math.Max(args.StatorLoad, 1000f);
            if (turbine.StatorLoad != newSet)
            {
                turbine.StatorLoad = newSet;
                return true;
            }
            return false; 
        }
    }
    #endregion

    private void OnAnchorChanged(EntityUid uid, GasTurbineMonitorComponent comp, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        CheckRange(uid, comp);
    }

    private void CheckRange(EntityUid uid, GasTurbineMonitorComponent comp)
    {
        if (!_entityManager.TryGetComponent<DeviceLinkSinkComponent>(uid, out var sink) || sink.LinkedSources.Count < 1)
            return;

        if (!_entityManager.TryGetEntity(comp.turbine, out var uidTurbine))
            return;

        if (!_entityManager.TryGetComponent<DeviceLinkSourceComponent>(uidTurbine, out var source))
            return;

        var xformMonitor = Transform(uid);
        var xformReactor = Transform(uidTurbine.Value);
        var posMonitor = _transformSystem.GetWorldPosition(xformMonitor);
        var posReactor = _transformSystem.GetWorldPosition(xformReactor);

        if (xformMonitor.MapID == xformReactor.MapID && (posMonitor - posReactor).Length() <= source.Range)
            return;

        _uiSystem.CloseUi(uid, TurbineUiKey.Key);
        comp.turbine = null;
        _signal.RemoveSinkFromSource(uidTurbine.Value, uid, source, sink);
        Dirty(uid, comp);
    }
}
