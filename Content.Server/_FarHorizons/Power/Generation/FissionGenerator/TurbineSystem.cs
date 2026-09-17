// SPDX-FileCopyrightText: 2025 jhrushbe <capnmerry@gmail.com>
// SPDX-FileCopyrightText: 2025 rottenheadphones <juaelwe@outlook.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: CC-BY-NC-SA-3.0


using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._FarHorizons.Power.Generation.FissionGenerator;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Repairable;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Construction.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork;
using Content.Shared.Damage;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Damage.Systems;
using Content.Server.Audio;
using Content.Shared.Audio;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._FarHorizons.Power.Generation.FissionGenerator;

// Ported and modified from goonstation by Jhrushbe.
// CC-BY-NC-SA-3.0
// https://github.com/goonstation/goonstation/blob/ff86b044/code/obj/nuclearreactor/turbine.dm

public sealed class TurbineSystem : SharedTurbineSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = null!;
    [Dependency] private readonly DeviceLinkSystem _signal = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly List<string> _damageSoundList = [
        "/Audio/_FarHorizons/Effects/engine_grump1.ogg",
        "/Audio/_FarHorizons/Effects/engine_grump2.ogg",
        "/Audio/_FarHorizons/Effects/engine_grump3.ogg",
        "/Audio/Effects/metal_slam5.ogg",
        "/Audio/Effects/metal_scrape2.ogg"
    ];

    private sealed class LogData
    {
        public TimeSpan CreationTime;
        public float? SetFlowRate;
        public float? SetStatorLoad;
    }

    private readonly Dictionary<KeyValuePair<EntityUid, EntityUid>, LogData> _logQueue = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TurbineComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<TurbineComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<TurbineComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<TurbineComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<TurbineComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<TurbineComponent, ItemSlotEjectAttemptEvent>(OnEjectAttempt);
        SubscribeLocalEvent<TurbineComponent, EntInsertedIntoContainerMessage>(OnPartInserted);
        SubscribeLocalEvent<TurbineComponent, EntRemovedFromContainerMessage>(OnPartEjected);

        SubscribeLocalEvent<TurbineComponent, AtmosDeviceUpdateEvent>(OnUpdate);
        SubscribeLocalEvent<TurbineComponent, GasAnalyzerScanEvent>(OnAnalyze);

        SubscribeLocalEvent<TurbineComponent, TurbineChangeFlowRateMessage>(OnTurbineFlowRateChanged);
        SubscribeLocalEvent<TurbineComponent, TurbineChangeStatorLoadMessage>(OnTurbineStatorLoadChanged);

        SubscribeLocalEvent<TurbineComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<TurbineComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<TurbineComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<TurbineComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
    }

    private const string BladeContainer = "blade_slot";
    private const string StatorContainer = "stator_slot";

    private void OnInit(EntityUid uid, TurbineComponent comp, ref MapInitEvent args)
    {
        _signal.EnsureSourcePorts(uid, comp.SpeedHighPort, comp.SpeedLowPort, comp.TurbineDataPort);
        _signal.EnsureSinkPorts(uid, comp.StatorLoadIncreasePort, comp.StatorLoadDecreasePort);

        TryGetPart(uid, BladeContainer, out comp.CurrentBlade);
        TryGetPart(uid, StatorContainer, out comp.CurrentStator);

        UpdatePartValues(comp);

        comp.AlarmAudioOvertemp = SpawnAttachedTo("GasTurbineAlarmEntity", new(uid, 0, 0));
        comp.AlarmAudioUnderspeed = SpawnAttachedTo("GasTurbineAlarmEntity", new(uid, 0, 0));
        _ambientSoundSystem.SetSound(comp.AlarmAudioUnderspeed.Value, new SoundPathSpecifier("/Audio/_FarHorizons/Machines/alarm_beep.ogg"));
        _ambientSoundSystem.SetVolume(comp.AlarmAudioUnderspeed.Value, -4);
    }

    private bool TryGetPart(EntityUid uid, string slot, [NotNullWhen(true)] out EntityUid? part)
    {
        part = null;

        if (!_containerSystem.TryGetContainer(uid, slot, out var container) || container.ContainedEntities.Count == 0)
            return false;

        part = container.ContainedEntities[0];

        return true;
    }

    private void OnAnalyze(EntityUid uid, TurbineComponent comp, ref GasAnalyzerScanEvent args)
    {
        if (!comp.InletEnt.HasValue || !comp.OutletEnt.HasValue)
            return;

        args.GasMixtures ??= [];

        if (_nodeContainer.TryGetNode(comp.InletEnt.Value, comp.PipeName, out PipeNode? inlet) && inlet.Air.Volume != 0f)
        {
            var inletAirLocal = inlet.Air.Clone();
            inletAirLocal.Multiply(inlet.Volume / inlet.Air.Volume);
            inletAirLocal.Volume = inlet.Volume;
            args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-inlet"), inletAirLocal));
        }

        if (_nodeContainer.TryGetNode(comp.OutletEnt.Value, comp.PipeName, out PipeNode? outlet) && outlet.Air.Volume != 0f)
        {
            var outletAirLocal = outlet.Air.Clone();
            outletAirLocal.Multiply(outlet.Volume / outlet.Air.Volume);
            outletAirLocal.Volume = outlet.Volume;
            args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-outlet"), outletAirLocal));
        }
    }

    private void OnShutdown(EntityUid uid, TurbineComponent comp, ref ComponentShutdown args)
    {
        QueueDel(comp.InletEnt);
        QueueDel(comp.OutletEnt);
    }

    #region Main Loop
    private void OnUpdate(EntityUid uid, TurbineComponent comp, ref AtmosDeviceUpdateEvent args)
    {
        var supplier = Comp<PowerSupplierComponent>(uid);
        comp.SupplierMaxSupply = supplier.MaxSupply;
        comp.SupplierLastSupply = supplier.CurrentSupply;

        supplier.MaxSupply = comp.LastGen;

        if(!GetPipes(uid, comp, out var inlet, out var outlet))
            return;

        if (comp.CurrentBlade == null || comp.CurrentStator == null)
            comp.Ruined = true;

        UpdateAppearance(uid, comp);

        var transferVolume = CalculateTransferVolume(comp, inlet, outlet, args.dt);

        var AirContents = inlet.Air.RemoveVolume(transferVolume) ?? new GasMixture();

        comp.LastVolumeTransfer = transferVolume;
        comp.LastGen = 0;
        comp.Overtemp = AirContents.Temperature >= comp.MaxTemp - 500;
        comp.Undertemp = AirContents.Temperature <= comp.MinTemp;

        // Dump gas into atmosphere
        if (comp.Ruined || AirContents.Temperature >= comp.MaxTemp)
        {
            var tile = _atmosphereSystem.GetTileMixture(uid, excite: true);

            if (tile != null)
            {
                _atmosphereSystem.Merge(tile, AirContents);
            }

            // This does rely on the alarm existing, but if it doesn't then there are bigger problems
            if (!comp.Ruined && _entityManager.TryGetComponent<AmbientSoundComponent>(comp.AlarmAudioOvertemp, out var ambience) && !ambience.Enabled)
                _popupSystem.PopupEntity(Loc.GetString("turbine-overheat", ("owner", uid)), uid, PopupType.LargeCaution);

            // Prevent power from being generated by residual gasses
            AirContents.Clear();
        }

        if(Exists(comp.AlarmAudioOvertemp))
            _ambientSoundSystem.SetAmbience(comp.AlarmAudioOvertemp.Value, !comp.Ruined && AirContents.Temperature >= comp.MaxTemp);

        // Update stator load based on device network
        if (comp.IncreasePortState != SignalState.Low)
            AdjustStatorLoad(comp, 1000);
        if (comp.DecreasePortState != SignalState.Low)
            AdjustStatorLoad(comp, -1000);

        if (comp.IncreasePortState == SignalState.Momentary)
            comp.IncreasePortState = SignalState.Low;
        if (comp.DecreasePortState == SignalState.Momentary)
            comp.DecreasePortState = SignalState.Low;

        if (!comp.Ruined && AirContents != null)
        {
            var InputStartingEnergy = _atmosphereSystem.GetThermalEnergy(AirContents);
            var InputHeatCap = _atmosphereSystem.GetHeatCapacity(AirContents, true);

            // Prevents div by 0 if it would come up
            if (InputStartingEnergy <= 0)
            {
                InputStartingEnergy = 1;
            }
            if (InputHeatCap <= 0)
            {
                InputHeatCap = 1;
            }

            if (AirContents.Temperature > comp.MinTemp)
            {
                AirContents.Temperature = (float)Math.Max((InputStartingEnergy - ((InputStartingEnergy - (InputHeatCap * Atmospherics.T20C)) * 0.8)) / InputHeatCap, Atmospherics.T20C);
            }

            var OutputStartingEnergy = _atmosphereSystem.GetThermalEnergy(AirContents);
            var EnergyGenerated = comp.StatorLoad * (comp.RPM / 60);

            var DeltaE = InputStartingEnergy - OutputStartingEnergy;
            float NewRPM;

            if (DeltaE - EnergyGenerated > 0)
            {
                NewRPM = comp.RPM + (float)Math.Sqrt(2 * (Math.Max(DeltaE - EnergyGenerated, 0) / comp.TurbineMass));
            }
            else
            {
                NewRPM = comp.RPM - (float)Math.Sqrt(2 * (Math.Max(EnergyGenerated - DeltaE, 0) / comp.TurbineMass));
            }

            var NextGen = comp.StatorLoad * (Math.Max(NewRPM, 0) / 60);
            float NextRPM;

            if (DeltaE - NextGen > 0)
            {
                NextRPM = comp.RPM + (float)Math.Sqrt(2 * (Math.Max(DeltaE - NextGen, 0) / comp.TurbineMass));
            }
            else
            {
                NextRPM = comp.RPM - (float)Math.Sqrt(2 * (Math.Max(NextGen - DeltaE, 0) / comp.TurbineMass));
            }

            if (NewRPM < 0 || NextRPM < 0)
            {
                // Stator load is too high
                comp.Stalling = true;
                comp.RPM = 0;
            }
            else
            {
                comp.Stalling = false;
                comp.RPM = NextRPM;
            }

            if(Exists(comp.AlarmAudioUnderspeed))
                _ambientSoundSystem.SetAmbience(comp.AlarmAudioUnderspeed.Value, !comp.Ruined && comp.Stalling && !comp.Undertemp && comp.FlowRate > 0);

            if (comp.RPM > 10)
            {
                // Sacrifices must be made to have a smooth ramp up:
                // This will generate 2 audio streams every second with up to 4 of them playing at once... surely this can't go wrong :clueless:
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/_FarHorizons/Ambience/Objects/turbine_room.ogg"), uid, AudioParams.Default.WithPitchScale(comp.RPM / comp.BestRPM).WithVolume(-2));
            }

            // Calculate power generation
            comp.LastGen = comp.PowerMultiplier * comp.StatorLoad * (comp.RPM / 30) * (float)(1 / Math.Cosh(0.01 * (comp.RPM - comp.BestRPM)));

            if (float.IsNaN(comp.LastGen))
                throw new NotFiniteNumberException("Turbine made NaN power");

            comp.Overspeed = comp.RPM > comp.BestRPM * 1.2;

            // Damage the turbines during overspeed, linear increase from 18% to 45% then stays at 45%
            if (comp.Overspeed && _random.NextFloat() < 0.15 * Math.Min(comp.RPM / comp.BestRPM, 3))
            {
                // TODO: damage flash
                _audio.PlayPvs(new SoundPathSpecifier(_damageSoundList[_random.Next(0, _damageSoundList.Count - 1)]), uid, AudioParams.Default.WithVariation(0.25f).WithVolume(-1));
                comp.BladeHealth--;
                UpdateHealthIndicators(uid, comp);
            }

            _atmosphereSystem.Merge(outlet.Air, AirContents);
        }

        // Explode
        if (!comp.Ruined && (comp.BladeHealth <= 0|| comp.RPM>comp.BestRPM*4))
        {
            TearApart(uid, comp);
        }

        // Send signals to device network
        _signal.SendSignal(uid, comp.SpeedHighPort, comp.RPM > comp.BestRPM * 1.05);
        _signal.SendSignal(uid, comp.SpeedLowPort, comp.RPM < comp.BestRPM * 0.95);

        Dirty(uid, comp);
        UpdateUI(uid, comp);
    }

    private float CalculateTransferVolume(TurbineComponent comp, PipeNode inlet, PipeNode outlet, float dt)
    {
        var wantToTransfer = comp.FlowRate * _atmosphereSystem.PumpSpeedup() * dt;
        var transferVolume = Math.Min(inlet.Air.Volume, wantToTransfer);
        var transferMoles = inlet.Air.Pressure * transferVolume / (inlet.Air.Temperature * Atmospherics.R);
        var molesSpaceLeft = (comp.OutputPressure - outlet.Air.Pressure) * outlet.Air.Volume / (outlet.Air.Temperature * Atmospherics.R);
        var actualMolesTransfered = Math.Clamp(transferMoles, 0, Math.Max(0, molesSpaceLeft));
        return Math.Max(0, actualMolesTransfered * inlet.Air.Temperature * Atmospherics.R / inlet.Air.Pressure);
    }

    private void TearApart(EntityUid uid, TurbineComponent comp)
    {
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/metal_break5.ogg"), uid, AudioParams.Default);
        _popupSystem.PopupEntity(Loc.GetString("turbine-explode", ("owner", uid)), uid, PopupType.LargeCaution);

        _explosion.QueueExplosion(uid, "Default", comp.RPM / 10, 15, 5, 0, canCreateVacuum: false);

        if (comp.RPM > comp.BestRPM / 6) // If it's barely moving then there's not really reason it would throw shrapnel
            ShootShrapnel(uid);

        _adminLogger.Add(LogType.Explosion, LogImpact.High, $"{ToPrettyString(uid)} destroyed by overspeeding for too long");

        comp.Ruined = true;
        comp.RPM = 0;
        _entityManager.QueueDeleteEntity(comp.CurrentBlade);
        comp.CurrentBlade = null;

        UpdateAppearance(uid, comp);
    }

    private void ShootShrapnel(EntityUid uid)
    {
        var ShrapnelCount = _random.Next(5, 20);
        for (var i=0;i< ShrapnelCount; i++)
        {
            _gun.ShootProjectile(Spawn("TurbineBladeShrapnel", _transformSystem.GetMapCoordinates(uid)), _random.NextAngle().ToVec().Normalized(), _random.NextVector2(2, 6), uid, uid);
        }
    }
    #endregion

    #region BUI
    public void UpdateUI(EntityUid uid, TurbineComponent turbine)
    {
        if (!_uiSystem.IsUiOpen(uid, TurbineUiKey.Key))
            return;

        _uiSystem.SetUiState(uid, TurbineUiKey.Key,
           new TurbineBuiState
           {
               Overspeed = turbine.Overspeed,
               Stalling = turbine.Stalling,
               Overtemp = turbine.Overtemp,
               Undertemp = turbine.Undertemp,

               RPM = turbine.RPM,
               BestRPM = turbine.BestRPM,

               FlowRateMin = 0,
               FlowRateMax = turbine.FlowRateMax,
               FlowRate = turbine.FlowRate,

               StatorLoadMin = 1000,
               StatorLoad = turbine.StatorLoad,

               PowerGeneration = turbine.SupplierMaxSupply,
               PowerSupply = turbine.SupplierLastSupply,

               Health = turbine.BladeHealth,
               HealthMax = turbine.BladeHealthMax,

               Blade = _entityManager.GetNetEntity(turbine.CurrentBlade),
               Stator = _entityManager.GetNetEntity(turbine.CurrentStator),
           });
    }

    private void OnTurbineFlowRateChanged(EntityUid uid, TurbineComponent turbine, TurbineChangeFlowRateMessage args)
    {
        if(TrySetFlowRate())
        {
            // Data is sent to a log queue to avoid spamming the admin log when adjusting values rapidly
            var key = new KeyValuePair<EntityUid, EntityUid>(args.Actor, uid);
            if(!_logQueue.TryGetValue(key, out var value))
                _logQueue.Add(key, new LogData
                {
                    CreationTime = _gameTiming.RealTime,
                    SetFlowRate = turbine.FlowRate
                });
            else
                value.SetFlowRate = turbine.FlowRate;
        }

        UpdateUI(uid, turbine);

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

    private void OnTurbineStatorLoadChanged(EntityUid uid, TurbineComponent turbine, TurbineChangeStatorLoadMessage args)
    {
        if (TrySetStatorLoad())
        {
            // Data is sent to a log queue to avoid spamming the admin log when adjusting values rapidly
            var key = new KeyValuePair<EntityUid, EntityUid>(args.Actor, uid);
            if (!_logQueue.TryGetValue(key, out var value))
                _logQueue.Add(key, new LogData
                {
                    CreationTime = _gameTiming.RealTime,
                    SetStatorLoad = turbine.StatorLoad
                });
            else
                value.SetStatorLoad = turbine.StatorLoad;
        }

        UpdateUI(uid, turbine);

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

    private float _accumulator = 0f;
    private readonly float _threshold = 0.5f;

    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator > _threshold)
        {
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
                        $"{ToPrettyString(log.Key.Key):player} set the flow rate on {ToPrettyString(log.Key.Value):device} to {log.Value.SetFlowRate}");

                if (log.Value.SetStatorLoad != null)
                    _adminLogger.Add(LogType.AtmosDeviceSetting, LogImpact.Medium,
                        $"{ToPrettyString(log.Key.Key):player} set the stator load on {ToPrettyString(log.Key.Value):device} to {log.Value.SetStatorLoad}");
            }

            foreach (var kvp in toRemove)
                _logQueue.Remove(kvp);
        }
    }
    #endregion

    private void OnSignalReceived(EntityUid uid, TurbineComponent comp, ref SignalReceivedEvent args)
    {
        var state = SignalState.Momentary;
        args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

        if (args.Port == comp.StatorLoadIncreasePort)
            comp.IncreasePortState = state;
        else if (args.Port == comp.StatorLoadDecreasePort)
            comp.DecreasePortState = state;

        var logtext = "maintain";
        if (comp.IncreasePortState != SignalState.Low && comp.DecreasePortState == SignalState.Low)
            logtext = "increase";
        else if (comp.DecreasePortState != SignalState.Low && comp.IncreasePortState == SignalState.Low)
            logtext = "decrease";

        _adminLogger.Add(LogType.Action, $"{ToPrettyString(args.Trigger):trigger} set the stator load on {ToPrettyString(uid):target} to {logtext}");
    }

    private void OnPortDisconnected(EntityUid uid, TurbineComponent comp, ref PortDisconnectedEvent args)
    {
        if (args.Port == comp.StatorLoadIncreasePort)
            comp.IncreasePortState = SignalState.Low;
        if (args.Port == comp.StatorLoadDecreasePort)
            comp.DecreasePortState = SignalState.Low;
    }

    #region Anchoring
    private void OnAnchorChanged(EntityUid uid, TurbineComponent comp, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
        {
            CleanUp(comp);
            return;
        }
    }

    private void OnUnanchorAttempt(EntityUid uid, TurbineComponent comp, ref UnanchorAttemptEvent args)
    {
        if (comp.RPM>1)
        {
            _popupSystem.PopupEntity(Loc.GetString("turbine-unanchor-warning"), args.User, args.User, PopupType.LargeCaution);
            args.Cancel();
        }
    }

    private bool GetPipes(EntityUid uid, TurbineComponent comp, [NotNullWhen(true)] out PipeNode? inlet, [NotNullWhen(true)] out PipeNode? outlet)
    {
        inlet = null;
        outlet = null;

        if (!comp.InletEnt.HasValue || EntityManager.Deleted(comp.InletEnt.Value))
            comp.InletEnt = SpawnAttachedTo(comp.PipePrototype, new(uid, comp.InletPos), rotation: Angle.FromDegrees(comp.InletRot));
        if (!comp.OutletEnt.HasValue || EntityManager.Deleted(comp.OutletEnt.Value))
            comp.OutletEnt = SpawnAttachedTo(comp.PipePrototype, new(uid, comp.OutletPos), rotation: Angle.FromDegrees(comp.OutletRot));

        if (comp.InletEnt == null || comp.OutletEnt == null)
            return false;

        if (!Transform(comp.InletEnt.Value).Anchored || !Transform(comp.OutletEnt.Value).Anchored)
        {
            _popupSystem.PopupEntity(Loc.GetString("turbine-anchor-warning"), uid, PopupType.MediumCaution);
            CleanUp(comp);
            _transform.Unanchor(uid);
            return false;
        }

        if (!_nodeContainer.TryGetNode(comp.InletEnt.Value, comp.PipeName, out inlet))
            return false;
        if (!_nodeContainer.TryGetNode(comp.OutletEnt.Value, comp.PipeName, out outlet))
            return false;

        return true;
    }
    #endregion

    private void CleanUp(TurbineComponent comp)
    {
        QueueDel(comp.InletEnt);
        QueueDel(comp.OutletEnt);
    }

    private void OnDamaged(EntityUid uid, TurbineComponent comp, ref DamageChangedEvent args)
    {
        if (comp.Ruined)
            return;

        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        var damage = (float)args.DamageDelta.GetTotal();
        var threshold = 50;
        var ratio = damage / threshold;

        if(ratio < 1)
        {
            comp.BladeHealth -= _random.Next(1, (int)(3f * ratio) + 1);
            UpdateHealthIndicators(uid, comp);
            return;
        }

        if (comp.RPM > comp.BestRPM / 6)
            TearApart(uid, comp);
        _entityManager.QueueDeleteEntity(comp.CurrentBlade);
        comp.CurrentBlade = null;
        if (_random.Prob(Math.Clamp(ratio - 1f, 0, 1)))
        {
            _entityManager.QueueDeleteEntity(comp.CurrentStator);
            comp.CurrentStator = null;
        }
        comp.Ruined = true;
    }

    private void OnRejuvenate(EntityUid uid, TurbineComponent comp, ref RejuvenateEvent args)
    {
        comp.RPM = 0;
        comp.CurrentBlade ??= SpawnInContainerOrDrop("SteelGasTurbineBlade", uid, BladeContainer);
        comp.CurrentStator ??= SpawnInContainerOrDrop("SteelGasTurbineStator", uid, StatorContainer);
        UpdatePartValues(comp);
        comp.Ruined = false;
        comp.FlowRate = 200;
        comp.StatorLoad = 35000;
        comp.IsSmoking = false;
        comp.IsSparking = false;
    }

    private void OnEjectAttempt(EntityUid uid, TurbineComponent comp, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (comp.RPM < 1)
            return;

        args.Cancelled = true;
    }

    private void OnInsertAttempt(EntityUid uid, TurbineComponent comp, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (comp.RPM < 1)
            return;

        args.Cancelled = true;
    }

    private void OnPartInserted(EntityUid uid, TurbineComponent comp, ref EntInsertedIntoContainerMessage args)
    {
        switch (args.Container.ID)
        {
            case BladeContainer:
                comp.CurrentBlade = args.Container.ContainedEntities[0];
                break;
            case StatorContainer:
                comp.CurrentStator = args.Container.ContainedEntities[0];
                break;
            default:
                return;
        }
        UpdatePartValues(comp);
    }

    private void OnPartEjected(EntityUid uid, TurbineComponent comp, ref EntRemovedFromContainerMessage args)
    {
        switch (args.Container.ID)
        {
            case BladeContainer:
                comp.CurrentBlade = null;
                break;
            case StatorContainer:
                comp.CurrentStator = null;
                break;
            default:
                return;
        }
        UpdatePartValues(comp);
    }

    private void UpdatePartValues(TurbineComponent comp)
    {
        _entityManager.TryGetComponent<GasTurbineBladeComponent>(comp.CurrentBlade, out var bladeComp);
        _entityManager.TryGetComponent<GasTurbineStatorComponent>(comp.CurrentStator, out var statorComp);

        if (bladeComp != null)
        {
            comp.TurbineMass = Math.Max(200, 200 * bladeComp.Properties.Density);
            comp.BladeHealthMax = (int)Math.Max(1, 5 * bladeComp.Properties.Hardness);
            comp.BladeHealth = comp.BladeHealthMax;
        }

        if (statorComp != null)
        {
            comp.PowerMultiplier = (float)Math.Max(0.2, 0.2 * statorComp.Properties.ElectricalConductivity);
        }
    }
}
