// SPDX-FileCopyrightText: 2025 jhrushbe <capnmerry@gmail.com>
// SPDX-FileCopyrightText: 2025 rottenheadphones <juaelwe@outlook.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: CC-BY-NC-SA-3.0

using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._FarHorizons.Power.Generation.FissionGenerator;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Radiation.Components;
using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared._FarHorizons.Materials.Systems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Construction.Components;
using Content.Shared.Popups;
using Content.Server.Popups;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Rejuvenate;
using Content.Shared.Throwing;
using Content.Shared.Damage.Systems;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Timing;

using Robust.Shared.Player; // Wayfarer

namespace Content.Server._FarHorizons.Power.Generation.FissionGenerator;

// Ported and modified from goonstation by Jhrushbe.
// CC-BY-NC-SA-3.0
// https://github.com/goonstation/goonstation/blob/ff86b044/code/obj/nuclearreactor/nuclearreactor.dm
// Performance optimizations adapted from Far-Horizons-SS14/Far-Horizons-SS14#1000
// and ss14Starlight/space-station-14#3967.

public sealed partial class NuclearReactorSystem : SharedNuclearReactorSystem
{
    // The great wall of dependencies
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly ReactorPartSystem _partSystem = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _soundSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = null!;
    [Dependency] private readonly DeviceLinkSystem _signal = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _lightSystem = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private sealed class LogData
    {
        public TimeSpan CreationTime;
        public float? SetControlRodInsertion;
    }

    private readonly Dictionary<KeyValuePair<EntityUid, EntityUid>, LogData> _logQueue = [];
    private static readonly ReactorPartComponent?[] _neighborBuffer = new ReactorPartComponent?[4];

    public override void Initialize()
    {
        base.Initialize();

        // Component events
        SubscribeLocalEvent<NuclearReactorComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<NuclearReactorComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<NuclearReactorComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<NuclearReactorComponent, RejuvenateEvent>(OnRejuvenate);

        // Atmos events
        SubscribeLocalEvent<NuclearReactorComponent, AtmosDeviceUpdateEvent>(OnUpdate);
        SubscribeLocalEvent<NuclearReactorComponent, GasAnalyzerScanEvent>(OnAnalyze);

        // Item events
        SubscribeLocalEvent<NuclearReactorComponent, EntInsertedIntoContainerMessage>(OnPartChanged);
        SubscribeLocalEvent<NuclearReactorComponent, EntRemovedFromContainerMessage>(OnPartChanged);

        // BUI events
        SubscribeLocalEvent<NuclearReactorComponent, ReactorItemActionMessage>(OnItemActionMessage);
        SubscribeLocalEvent<NuclearReactorComponent, ReactorControlRodModifyMessage>(OnControlRodMessage);

        // Signal events
        SubscribeLocalEvent<NuclearReactorComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<NuclearReactorComponent, PortDisconnectedEvent>(OnPortDisconnected);

        // Anchor events
        SubscribeLocalEvent<NuclearReactorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<NuclearReactorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
    }

    private void OnInit(EntityUid uid, NuclearReactorComponent comp, ref MapInitEvent args)
    {
        _signal.EnsureSinkPorts(uid, comp.ControlRodInsertPort, comp.ControlRodRetractPort);

        var gridWidth = comp.ReactorGridWidth;
        var gridHeight = comp.ReactorGridHeight;

        comp.ComponentGrid = new ReactorPartComponent[gridWidth, gridHeight];
        comp.FluxGrid = new List<ReactorNeutron>[gridWidth, gridHeight];
          comp.FluxGridScratch = new List<ReactorNeutron>[gridWidth, gridHeight];
        comp.TemperatureGrid = new double[gridWidth, gridHeight];
        comp.NeutronGrid = new int[gridWidth, gridHeight];

        ApplyPrefab(uid, comp);

        // I hate everything about this, but it ensures the audio doesn't just stop if you don't look at it
        comp.AlarmAudioHighThermal = SpawnAttachedTo("ReactorAlarmEntity", new(uid, 0, 0));
        comp.AlarmAudioHighTemp = SpawnAttachedTo("ReactorAlarmEntity", new(uid, 0, 0));
        _ambientSoundSystem.SetSound(comp.AlarmAudioHighTemp.Value, new SoundPathSpecifier("/Audio/_FarHorizons/Machines/reactor_alarm_2.ogg"));
        comp.AlarmAudioHighRads = SpawnAttachedTo("ReactorAlarmEntity", new(uid, 0, 0));
        _ambientSoundSystem.SetSound(comp.AlarmAudioHighRads.Value, new SoundPathSpecifier("/Audio/_FarHorizons/Machines/reactor_alarm_3.ogg"));
    }

    #region Prefab
    private void ApplyPrefab(EntityUid uid, NuclearReactorComponent comp)
    {
        var prefab = comp.Prefab == "random" ? GenerateRandomPrefab(comp) : GetPrefabFromProto(comp);
        for (var x = 0; x < comp.ReactorGridWidth; x++)
            for (var y = 0; y < comp.ReactorGridHeight; y++)
            {
                comp.ComponentGrid[x, y] = prefab.TryGetValue(new Vector2i(x, y), out var part) ? new ReactorPartComponent(part) : null;
                comp.FluxGrid[x, y] = [];
                comp.FluxGridScratch[x, y] = [];
            }

        UpdateGasVolume(comp);
        UpdateGridVisual((uid, comp));
    }

    private Dictionary<Vector2i, ReactorPartComponent> GenerateRandomPrefab(NuclearReactorComponent comp)
    {
        var exportDict = new Dictionary<Vector2i, ReactorPartComponent>();
        for (var x = 0; x < comp.ReactorGridWidth; x++)
            for (var y = 0; y < comp.ReactorGridHeight; y++)
                if (_random.Prob(comp.RandomPrefabFill))
                    exportDict.Add(new Vector2i(x, y), RandomComponent());
        return exportDict;
    }

    private ReactorPartComponent RandomComponent()
    {
        var compName = Factory.GetComponentName<ReactorPartComponent>();
        var source = "NuclearReactorRandomParts";
        var protoID = _prototypes.Index<WeightedRandomPrototype>(source).Pick(_random);
        if (!_prototypes.TryIndex(protoID, out var entProto)
                || !entProto.TryGetComponent<ReactorPartComponent>(compName, out var comp))
            return new();
        comp.ProtoId = protoID;
        return comp;
    }

    private Dictionary<Vector2i, ReactorPartComponent> GetPrefabFromProto(NuclearReactorComponent comp)
    {
        var exportDict = new Dictionary<Vector2i, ReactorPartComponent>();

        if (!_prototypes.TryIndex<NuclearReactorPrefabPrototype>(comp.Prefab, out var proto) || proto.ReactorComponents == null)
            return exportDict;

        var compName = Factory.GetComponentName<ReactorPartComponent>();

        foreach (var pair in proto.ReactorComponents)
        {
            if (!_prototypes.TryIndex(pair.Value, out var entProto)
                || !entProto.TryGetComponent<ReactorPartComponent>(compName, out var reactorPart))
                continue;

            reactorPart.ProtoId = pair.Value;
            exportDict.Add(pair.Key, reactorPart);
        }

        return exportDict;
    }
    #endregion

    private void OnAnalyze(EntityUid uid, NuclearReactorComponent comp, ref GasAnalyzerScanEvent args)
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

    private void OnPartChanged(EntityUid uid, NuclearReactorComponent component, ContainerModifiedMessage args)
    {
        ReactorTryGetSlot(uid, "part_slot", out component.PartSlot!);
        UpdateUI(uid, component);
    }

    private void OnShutdown(Entity<NuclearReactorComponent> ent, ref ComponentShutdown args) => CleanUp(ent.Comp);

    #region Main Loop
    private void OnUpdate(Entity<NuclearReactorComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        var comp = ent.Comp;
        var uid = ent.Owner;

        _appearance.SetData(uid, ReactorVisuals.Sprite, comp.Melted ? Reactors.Melted : Reactors.Normal);

        ProcessCaseRadiation(ent);

        if (comp.Melted)
            return;

        if(!GetPipes(uid, comp, out var inlet, out var outlet))
            return;

        var gridWidth = comp.ReactorGridWidth;
        var gridHeight = comp.ReactorGridHeight;

        if (comp.ApplyPrefab)
        {
            ApplyPrefab(uid, comp);
            comp.ApplyPrefab = false;
        }

        _appearance.SetData(uid, ReactorVisuals.Input, inlet.Air.TotalMoles > 20);
        _appearance.SetData(uid, ReactorVisuals.Output, outlet.Air.TotalMoles > 20);


        var TempRads = 0;
        var ControlRods = 0;
        var AvgControlRodInsertion = 0f;
        var TempChange = 0f;

        // Debug Vars
        var NeutronCount = 0;
        var MeltedComps = 0;
        var TotalNRads = 0f;
        var TotalRads = 0f;
        var TotalSpent = 0f;

        var transferVolume = CalculateTransferVolume(inlet.Air.Volume, inlet, outlet, args.dt);
        var GasInput = inlet.Air.RemoveVolume(transferVolume);

        GasInput.Volume = inlet.Volume;

        // Update control rod insertion based on device network
        if (comp.InsertPortState != SignalState.Low)
            AdjustControlRods(comp, 0.1f);
        if (comp.RetractPortState != SignalState.Low)
            AdjustControlRods(comp, -0.1f);

        if (comp.InsertPortState == SignalState.Momentary)
            comp.InsertPortState = SignalState.Low;
        if (comp.RetractPortState == SignalState.Momentary)
            comp.RetractPortState = SignalState.Low;

        comp.SimTime.Restart();
        var scratch = comp.FluxGridScratch;
        for (var x = 0; x < gridWidth; x++)
        {
            for (var y = 0; y < gridHeight; y++)
            {
                var ReactorComp = comp.ComponentGrid[x, y];

                if (ReactorComp == null)
                {
                    comp.TemperatureGrid[x, y] = 0;
                }
                else
                {
                    var gas = _partSystem.ProcessGas(ReactorComp, ent, GasInput);
                    GasInput.Volume -= ReactorComp.GasVolume;

                    if (gas != null)
                        _atmosphereSystem.Merge(outlet.Air, gas);

                    _partSystem.ProcessHeat(ReactorComp, ent, GetGridNeighbors(comp, x, y, _neighborBuffer), this);
                    comp.TemperatureGrid[x, y] = ReactorComp.Temperature;

                    if (ReactorComp.HasRodType(ReactorPartComponent.RodTypes.ControlRod) && ReactorComp.IsControlRod)
                    {
                        ReactorComp.ConfiguredInsertionLevel = comp.ControlRodInsertion;
                        ControlRods++;
                    }

                    if (ReactorComp.Melted)
                        MeltedComps++;

                    comp.FluxGrid[x, y] = _partSystem.ProcessNeutrons(ReactorComp, comp.FluxGrid[x, y], out var deltaT);
                    TempChange += deltaT;

                    // Second check so that AvgControlRodInsertion represents the present instead of 1 tick in the past
                    if (ReactorComp.HasRodType(ReactorPartComponent.RodTypes.ControlRod) && ReactorComp.IsControlRod)
                        AvgControlRodInsertion += ReactorComp.NeutronCrossSection;

                    TotalNRads += ReactorComp.Properties.NeutronRadioactivity;
                    TotalRads += ReactorComp.Properties.Radioactivity;
                    TotalSpent += ReactorComp.Properties.FissileIsotopes;
                }

                foreach (var neutron in comp.FluxGrid[x, y])
                {
                    NeutronCount++;

                    var dir = (byte)neutron.dir.AsFlag();
                    // Bit abuse
                    var xmod = ((dir >> 1) % 2) - ((dir >> 3) % 2);
                    var ymod = ((dir >> 2) % 2) - (dir % 2);

                    if (x + xmod >= 0 && y + ymod >= 0 && x + xmod <= gridWidth - 1
                        && y + ymod <= gridHeight - 1)
                    {
                        scratch[x + xmod, y + ymod].Add(neutron);
                    }
                    else
                    {
                        TempRads++;
                    }
                }

                comp.NeutronGrid[x, y] = comp.FluxGrid[x, y].Count;

                if (comp.SimTime.Elapsed.TotalMilliseconds > 500)
                {
                    QueueDel(uid);
                    _adminLog.Add(LogType.EntityDelete, LogImpact.Extreme, $"{ToPrettyString(uid):reactor} simulation took too long ({comp.SimTime.Elapsed.TotalMilliseconds} ms).");
                    return;
                }
            }
        }

        (comp.FluxGrid, comp.FluxGridScratch) = (comp.FluxGridScratch, comp.FluxGrid);
        for (var x = 0; x < gridWidth; x++)
        {
            for (var y = 0; y < gridHeight; y++)
            {
                comp.FluxGridScratch[x, y].Clear();
            }
        }

        AvgControlRodInsertion /= ControlRods;

        // Sound for the control rods moving, basically an audio cue that the reactor's doing something important
        if (ControlRods > 0 && !MathHelper.CloseTo(comp.AvgInsertion, AvgControlRodInsertion))
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_FarHorizons/Machines/relay_click.ogg"), uid);

        var CasingGas = ProcessCasingGas(comp, GasInput);
        if (CasingGas != null)
            _atmosphereSystem.Merge(outlet.Air, CasingGas);

        // If there's still input gas left over
        _atmosphereSystem.Merge(outlet.Air, GasInput);

        comp.RadiationLevel = Math.Max(comp.RadiationLevel + TempRads, 0);

        comp.NeutronCount = NeutronCount;
        comp.MeltedParts = MeltedComps;
        comp.DetectedControlRods = ControlRods;
        comp.AvgInsertion = AvgControlRodInsertion;
        comp.TotalNRads = TotalNRads;
        comp.TotalRads = TotalRads;
        comp.TotalSpent = TotalSpent;

        if (comp.ThermalPowerCount < comp.ThermalPowerPrecision)
            comp.ThermalPowerCount++;
        comp.ThermalPower += (TempChange - comp.ThermalPower) / Math.Min(comp.ThermalPowerCount, comp.ThermalPowerPrecision);

        if (comp.Temperature > comp.ReactorMeltdownTemp) // Disabled the explode if over 1000 rads thing, hope the server survives
        {
            CatastrophicOverload(ent);
        }

        UpdateVisuals(ent);
        UpdateAudio(ent);
        // UpdateRadio(ent); // Frontier: Disabled radio callouts for reactors.
        UpdateTempIndicators(ent);

        UpdateUI(uid, comp);
    }

    private void ProcessCaseRadiation(Entity<NuclearReactorComponent> ent)
    {
        var reactor = ent.Comp;
        var comp = EnsureComp<RadiationSourceComponent>(ent.Owner);

        // Linear scaling up to maximum, logarithmic beyond that
        comp.Intensity = (float)Math.Max(reactor.RadiationLevel <= reactor.MaximumRadiation ? reactor.RadiationLevel : reactor.MaximumRadiation + Math.Log(reactor.RadiationLevel - reactor.MaximumRadiation + 1), reactor.Melted ? reactor.MeltdownRadiation : 0);
        reactor.RadiationLevel /= Math.Max(reactor.RadiationStability, 1);
    }

    private static ReactorPartComponent?[] GetGridNeighbors(NuclearReactorComponent reactor, int x, int y, ReactorPartComponent?[] buffer)
    {
        buffer[0] = x - 1 < 0 ? null : reactor.ComponentGrid[x - 1, y];
        buffer[1] = x + 1 >= reactor.ReactorGridWidth ? null : reactor.ComponentGrid[x + 1, y];
        buffer[2] = y - 1 < 0 ? null : reactor.ComponentGrid[x, y - 1];
        buffer[3] = y + 1 >= reactor.ReactorGridHeight ? null : reactor.ComponentGrid[x, y + 1];
        return buffer;
    }

    private void UpdateGasVolume(NuclearReactorComponent reactor)
    {
        if (reactor.InletEnt == null || reactor.OutletEnt == null)
            return;

        if (!_nodeContainer.TryGetNode(reactor.InletEnt.Value, reactor.PipeName, out PipeNode? inlet) || !_nodeContainer.TryGetNode(reactor.OutletEnt.Value, reactor.PipeName, out PipeNode? outlet))
            return;

        var totalGasVolume = reactor.ReactorVesselGasVolume;

        for (var x = 0; x < reactor.ReactorGridWidth; x++)
            for (var y = 0; y < reactor.ReactorGridHeight; y++)
                if (reactor.ComponentGrid![x, y] != null)
                    totalGasVolume += reactor.ComponentGrid[x, y]!.GasVolume;
        inlet.Volume = totalGasVolume;
        outlet.Volume = totalGasVolume;
    }

    private GasMixture? ProcessCasingGas(NuclearReactorComponent reactor, GasMixture inGas)
    {
        GasMixture? ProcessedGas = null;
        if (reactor.AirContents != null)
        {
            var DeltaT = reactor.Temperature - reactor.AirContents.Temperature;
            var DeltaTr = Math.Pow(reactor.Temperature, 4) - Math.Pow(reactor.AirContents.Temperature, 4);

            var k = MaterialSystem.CalculateHeatTransferCoefficient(_prototypes.Index(reactor.Material).Properties, null);
            var A = 1 * (0.4 * 8);

            var ThermalEnergy = _atmosphereSystem.GetThermalEnergy(reactor.AirContents);

            var Hottest = Math.Max(reactor.AirContents.Temperature, reactor.Temperature);
            var Coldest = Math.Min(reactor.AirContents.Temperature, reactor.Temperature);

            var MaxDeltaE = Math.Clamp((k * A * DeltaT) + (5.67037442e-8 * A * DeltaTr),
                (reactor.Temperature * reactor.ThermalMass) - (Hottest * reactor.ThermalMass),
                (reactor.Temperature * reactor.ThermalMass) - (Coldest * reactor.ThermalMass));

            reactor.AirContents.Temperature = (float)Math.Clamp(reactor.AirContents.Temperature +
                (MaxDeltaE / _atmosphereSystem.GetHeatCapacity(reactor.AirContents, true)), Coldest, Hottest);

            reactor.Temperature = (float)Math.Clamp(reactor.Temperature -
                ((_atmosphereSystem.GetThermalEnergy(reactor.AirContents) - ThermalEnergy) / reactor.ThermalMass), Coldest, Hottest);

            if (reactor.AirContents.Temperature < 0 || reactor.Temperature < 0)
                throw new Exception("Reactor casing temperature calculation resulted in sub-zero value.");

            ProcessedGas = reactor.AirContents;
        }

        if (inGas != null && _atmosphereSystem.GetThermalEnergy(inGas) > 0)
        {
            reactor.AirContents = inGas.RemoveVolume(reactor.ReactorVesselGasVolume);

            if (reactor.AirContents != null && reactor.AirContents.TotalMoles < 1)
            {
                if (ProcessedGas != null)
                {
                    _atmosphereSystem.Merge(ProcessedGas, reactor.AirContents);
                    reactor.AirContents.Clear();
                }
                else
                {
                    ProcessedGas = reactor.AirContents;
                    reactor.AirContents.Clear();
                }
            }
        }
        return ProcessedGas;
    }

    private float CalculateTransferVolume(float volume, PipeNode inlet, PipeNode outlet, float dt)
    {
        var wantToTransfer = volume * _atmosphereSystem.PumpSpeedup() * dt;
        var transferVolume = Math.Min(inlet.Air.Volume, wantToTransfer);
        var transferMoles = inlet.Air.Pressure * transferVolume / (inlet.Air.Temperature * Atmospherics.R);
        var molesSpaceLeft = ((Atmospherics.MaxOutputPressure * 3) - outlet.Air.Pressure) * outlet.Air.Volume / (outlet.Air.Temperature * Atmospherics.R);
        var actualMolesTransfered = Math.Clamp(transferMoles, 0, Math.Max(0, molesSpaceLeft));
        return Math.Max(0, actualMolesTransfered * inlet.Air.Temperature * Atmospherics.R / inlet.Air.Pressure);
    }

    private void CatastrophicOverload(Entity<NuclearReactorComponent> ent)
    {
        var comp = ent.Comp;
        var uid = ent.Owner;

        var stationUid = _station.GetStationInMap(Transform(uid).MapID);
        if (stationUid != null)
            _alertLevel.SetLevel(stationUid.Value, comp.MeltdownAlertLevel, true, true, true);

        string stationName = GetReactorLocation(uid); // Wayfarer
        //var announcement = Loc.GetString("reactor-meltdown-announcement"); // Wayfarer: Edited for the one below
        var announcement = Loc.GetString("reactor-meltdown-announcement-wf", ("station", stationName));
        var sender = Loc.GetString("reactor-meltdown-announcement-sender");
        _chatSystem.DispatchStationAnnouncement(stationUid ?? uid, announcement, sender, false, null, Color.Orange);

        //_soundSystem.PlayGlobalOnStation(uid, _audio.ResolveSound(comp.MeltdownSound)); // Wayfarer: Commented for the one below
        _audio.PlayGlobal(comp.MeltdownSound, Filter.Broadcast(), true);

        comp.Melted = true;
        var MeltdownBadness = 0f;
        comp.AirContents ??= new();

        for (var x = 0; x < comp.ReactorGridWidth; x++)
        {
            for (var y = 0; y < comp.ReactorGridHeight; y++)
            {
                if (comp.ComponentGrid[x, y] != null)
                {
                    var RC = comp.ComponentGrid[x, y];
                    if (RC == null)
                        return;
                    MeltdownBadness += ((RC.Properties.Radioactivity * 2) + (RC.Properties.NeutronRadioactivity * 5) + (RC.Properties.FissileIsotopes * 10)) * (RC.Melted ? 2 : 1);
                    if (RC.HasRodType(ReactorPartComponent.RodTypes.GasChannel))
                    {
                        _atmosphereSystem.Merge(comp.AirContents, RC.AirContents ?? new());
                        (RC.AirContents ?? new()).Clear();
                    }

                    comp.ComponentGrid[x, y] = null;
                    comp.NeutronGrid[x, y] = 0;
                    comp.FluxGrid[x, y] = [];
                    if (comp.GridEntities.TryGetValue(new(x, y), out var partEntity))
                    {
                        QueueDel(partEntity);
                        comp.GridEntities.Remove(new(x, y));
                    }
                }
            }
        }
        comp.RadiationLevel = Math.Clamp(comp.RadiationLevel + MeltdownBadness, 0, 200);
        comp.AirContents.AdjustMoles(Gas.Tritium, MeltdownBadness * 15);
        comp.AirContents.Temperature = Math.Max(comp.Temperature, comp.AirContents.Temperature);

        var T = _atmosphereSystem.GetTileMixture(ent.Owner, excite: true);
        if (T != null)
            _atmosphereSystem.Merge(T, comp.AirContents);

        _adminLog.Add(LogType.Explosion, LogImpact.Extreme, $"{ToPrettyString(ent):reactor} catastrophically overloads, meltdown badness: {MeltdownBadness}");

        // You did not see graphite on the roof. You're in shock. Report to medical.
        for (var i = 0; i < _random.Next(10, 30); i++)
            _throwingSystem.TryThrow(Spawn("NuclearDebrisChunk", _transformSystem.GetMapCoordinates(uid)), _random.NextAngle().ToVec().Normalized(), _random.NextFloat(8, 16), uid);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/metal_break5.ogg"), uid);
        _explosionSystem.QueueExplosion(ent.Owner, "Radioactive", Math.Max(100, MeltdownBadness * 5), 1, 5, 0, canCreateVacuum: false);

        var lightcomp = _lightSystem.EnsureLight(uid);
        _lightSystem.SetEnergy(uid, 0.1f, lightcomp);
        _lightSystem.SetFalloff(uid, 2, lightcomp);
        _lightSystem.SetRadius(uid, (comp.ReactorGridWidth + comp.ReactorGridHeight) / 4, lightcomp);
        _lightSystem.SetColor(uid, Color.FromHex("#FFAAAAFF"), lightcomp);

        // Reset grids
        comp.ComponentGrid = new ReactorPartComponent[comp.ReactorGridWidth, comp.ReactorGridHeight]; // Not Array.Clear due to ammonia
        Array.Clear(comp.NeutronGrid);
        Array.Clear(comp.TemperatureGrid);
        Array.Clear(comp.FluxGrid);
        Array.Clear(comp.FluxGridScratch);

        // This will Dirty() the reactor, so no need to declare it explicitly
        UpdateGridVisual(ent);
    }

    private void UpdateVisuals(Entity<NuclearReactorComponent> ent)
    {
        var comp = ent.Comp;
        var uid = ent.Owner;

        if (comp.Melted)
        {
            _appearance.SetData(uid, ReactorVisuals.Lights, ReactorWarningLights.LightsOff);
            _appearance.SetData(uid, ReactorVisuals.Status, ReactorStatusLights.Off);
            _appearance.SetData(uid, ReactorVisuals.Input, false);
            _appearance.SetData(uid, ReactorVisuals.Output, false);
            return;
        }

        // Temperature & radiation warning
        if (comp.Temperature >= comp.ReactorOverheatTemp || comp.RadiationLevel > comp.MaximumRadiation * 0.5)
            if (comp.Temperature >= comp.ReactorFireTemp || comp.RadiationLevel > comp.MaximumRadiation)
                _appearance.SetData(uid, ReactorVisuals.Lights, ReactorWarningLights.LightsMeltdown);
            else
                _appearance.SetData(uid, ReactorVisuals.Lights, ReactorWarningLights.LightsWarning);
        else
            _appearance.SetData(uid, ReactorVisuals.Lights, ReactorWarningLights.LightsOff);

        // Status screen / side lights
        switch (comp.Temperature)
        {
            case float n when n is <= Atmospherics.T20C:
                _appearance.SetData(uid, ReactorVisuals.Status, ReactorStatusLights.Off);
                break;
            case float n when n > Atmospherics.T20C && n <= comp.ReactorOverheatTemp:
                _appearance.SetData(uid, ReactorVisuals.Status, ReactorStatusLights.Active);
                break;
            case float n when n > comp.ReactorOverheatTemp && n <= comp.ReactorFireTemp:
                _appearance.SetData(uid, ReactorVisuals.Status, ReactorStatusLights.Overheat);
                break;
            case float n when n > comp.ReactorFireTemp && n <= float.PositiveInfinity:
                _appearance.SetData(uid, ReactorVisuals.Status, ReactorStatusLights.Meltdown);
                break;
            default:
                _appearance.SetData(uid, ReactorVisuals.Status, ReactorStatusLights.Off);
                break;
        }
    }

    private void UpdateAudio(Entity<NuclearReactorComponent> ent)
    {
        var comp = ent.Comp;

        if (Exists(comp.AlarmAudioHighThermal))
            _ambientSoundSystem.SetAmbience(comp.AlarmAudioHighThermal.Value, !comp.Melted && comp.ThermalPower > comp.MaximumThermalPower);
        if (Exists(comp.AlarmAudioHighTemp))
            _ambientSoundSystem.SetAmbience(comp.AlarmAudioHighTemp.Value, !comp.Melted && comp.Temperature > comp.ReactorOverheatTemp);
        if (Exists(comp.AlarmAudioHighRads))
            _ambientSoundSystem.SetAmbience(comp.AlarmAudioHighRads.Value, !comp.Melted && comp.RadiationLevel > comp.MaximumRadiation * 0.5);
    }

    // Frontier: Disables the radio alerts for reactor
    /*     private void UpdateRadio(Entity<NuclearReactorComponent> ent)
    {
        var comp = ent.Comp;
        var uid = ent.Owner;

        if (comp.Melted)
            return;

        var engi = _prototypes.Index<RadioChannelPrototype>(ent.Comp.EngineeringChannel);

        var shortband = _prototypes.Index<RadioChannelPrototype>(ent.Comp.ShortbandChannel); // Wayfarer: We'll use this so we only send the BIG critical alerts to.
        string stationName = GetReactorLocation(uid); // Wayfarer


        if (comp.Temperature >= comp.ReactorOverheatTemp)
        {
            if (!comp.IsSmoking)
            {
                _adminLog.Add(LogType.Damaged, $"{ToPrettyString(ent):reactor} is at {comp.Temperature}K and may meltdown");
                //_radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-smoke-start-message", ("owner", uid), ("temperature", Math.Round(comp.Temperature))), engi, ent); // Wayfarer: Edited for the one below
                _radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-smoke-start-message-wf", ("owner", uid), ("station", stationName), ("temperature", Math.Round(comp.Temperature))), shortband, ent);
                comp.LastSendTemperature = comp.Temperature;
            }
            if (comp.Temperature >= comp.ReactorFireTemp && !comp.IsBurning)
            {
                _adminLog.Add(LogType.Damaged, $"{ToPrettyString(ent):reactor} is at {comp.Temperature}K and is likely to meltdown");
                //_radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-fire-start-message", ("owner", uid), ("temperature", Math.Round(comp.Temperature))), engi, ent); // Wayfarer: Edited for the one below
                _radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-fire-start-message-wf", ("owner", uid), ("station", stationName), ("temperature", Math.Round(comp.Temperature))), engi, ent); // This one is critical, so we send it to both channels.
                _radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-fire-start-message-wf", ("owner", uid), ("station", stationName), ("temperature", Math.Round(comp.Temperature))), shortband, ent);
                comp.LastSendTemperature = comp.Temperature;
            }
            else if (comp.Temperature < comp.ReactorFireTemp && comp.IsBurning)
            {
                _adminLog.Add(LogType.Healed, $"{ToPrettyString(ent):reactor} is cooling from {comp.ReactorFireTemp}K");
                //_radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-fire-stop-message", ("owner", uid)), engi, ent); // Wayfarer: Edited for the one below
                _radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-fire-stop-message-wf", ("owner", uid), ("station", stationName)), shortband, ent);
                comp.LastSendTemperature = comp.Temperature;
            }
        }
        else
        {
            if (comp.IsSmoking)
            {
                _adminLog.Add(LogType.Healed, $"{ToPrettyString(ent):reactor} is cooling from {comp.ReactorOverheatTemp}K");
                //_radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-smoke-stop-message", ("owner", uid)), engi, ent); // Wayfarer: Edited for the one below
                _radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-smoke-stop-message-wf", ("owner", uid), ("station", stationName)), shortband, ent);
                comp.LastSendTemperature = comp.Temperature;
                comp.HasSentWarning = false;
            }
        }

        if (comp.Temperature >= (comp.ReactorFireTemp + comp.ReactorMeltdownTemp) >> 1 && !comp.HasSentWarning)
        {
            var stationUid = _station.GetStationInMap(Transform(uid).MapID);
            //var announcement = Loc.GetString("reactor-melting-announcement"); // Wayfarer: Edited for the one below
            var announcement = Loc.GetString("reactor-melting-announcement-wf", ("station", stationName));
            var sender = Loc.GetString("reactor-melting-announcement-sender");
            _chatSystem.DispatchStationAnnouncement(stationUid ?? uid, announcement, sender, false, null, Color.Orange);
            //_soundSystem.PlayGlobalOnStation(uid, _audio.ResolveSound(new SoundPathSpecifier("/Audio/Misc/delta_alt.ogg"))); // Wayfarer: Commented for the one below
            _audio.PlayGlobal(_audio.ResolveSound(new SoundPathSpecifier("/Audio/Misc/delta_alt.ogg")), Filter.Broadcast(), true);
            comp.HasSentWarning = true;
        }

        if (Math.Max(comp.LastSendTemperature, comp.Temperature) < comp.ReactorOverheatTemp)
            return;

        var step = comp.ReactorMeltdownTemp * 0.05;

        if (Math.Abs(comp.Temperature - comp.LastSendTemperature) < step)
            return;

        if (comp.LastSendTemperature > comp.Temperature)
        {
            //_radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-temperature-cooling-message", ("owner", uid), ("temperature", Math.Round(comp.Temperature))), engi, ent); // Wayfarer: Edited for the one below
            _radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-temperature-cooling-message-wf", ("owner", uid), ("station", stationName), ("temperature", Math.Round(comp.Temperature))), shortband, ent);
        }
        else
        {
            if (comp.Temperature >= comp.ReactorFireTemp)
            {
                //_radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-temperature-critical-message", ("owner", uid), ("temperature", Math.Round(comp.Temperature))), engi, ent); // Wayfarer: Edited for the one below
                _radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-temperature-critical-message-wf", ("owner", uid), ("station", stationName), ("temperature", Math.Round(comp.Temperature))), shortband, ent);
            }
            else if (comp.Temperature >= comp.ReactorOverheatTemp)
            {
                //_radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-temperature-dangerous-message", ("owner", uid), ("temperature", Math.Round(comp.Temperature))), engi, ent); // Wayfarer: Edited for the one below
                _radioSystem.SendRadioMessage(uid, Loc.GetString("reactor-temperature-dangerous-message-wf", ("owner", uid), ("station", stationName), ("temperature", Math.Round(comp.Temperature))), shortband, ent);
            }
        }

        comp.LastSendTemperature = comp.Temperature;
    }
 */
    // End Frontier
    #endregion

    #region BUI
    public void UpdateUI(EntityUid uid, NuclearReactorComponent reactor)
    {
        if (!_uiSystem.IsUiOpen(uid, NuclearReactorUiKey.Key))
            return;

        if(reactor.Melted)
        {
            _uiSystem.CloseUi(uid, NuclearReactorUiKey.Key);
            return;
        }

        var gridWidth = reactor.ReactorGridWidth;
        var gridHeight = reactor.ReactorGridHeight;

        var dict = new Dictionary<Vector2i, ReactorSlotBUIData>();

        for (var x = 0; x < gridWidth; x++)
        {
            for (var y = 0; y < gridHeight; y++)
            {
                var reactorPart = reactor.ComponentGrid[x, y];
                if (reactorPart == null)
                {
                    if(reactor.NeutronGrid[x, y] > 0)
                        dict.Add(new(x,y), new ReactorSlotBUIData { NeutronCount = reactor.NeutronGrid[x, y] });
                    continue;
                }

                dict.Add(new(x, y), new ReactorSlotBUIData
                {
                    Temperature = reactor.TemperatureGrid[x, y],
                    NeutronCount = reactor.NeutronGrid[x, y],
                    IconName = reactorPart.IconStateInserted,
                    PartName = _prototypes.Index(reactorPart.ProtoId).Name,
                    NeutronRadioactivity = reactorPart.Properties.NeutronRadioactivity,
                    Radioactivity = reactorPart.Properties.Radioactivity,
                    SpentFuel = reactorPart.Properties.FissileIsotopes
                });
            }
        }

        // This is transmitting close to 2.3KB of data every time it's called... ouch
        _uiSystem.SetUiState(uid, NuclearReactorUiKey.Key,
           new NuclearReactorBuiState
           {
               SlotData = dict,

               ItemName = reactor.PartSlot.Item != null ? Identity.Name(reactor.PartSlot.Item.Value, _entityManager) : null,

               ReactorTemp = reactor.Temperature,
               ReactorRads = reactor.RadiationLevel,
               ReactorRadsMax = reactor.MaximumRadiation,
               ReactorTherm = reactor.ThermalPower,

               ControlRodActual = reactor.AvgInsertion,
               ControlRodSet = reactor.ControlRodInsertion,

               GridWidth = gridWidth,
               GridHeight = gridHeight,
           });
    }

    private void OnItemActionMessage(Entity<NuclearReactorComponent> ent, ref ReactorItemActionMessage args)
    {
        var comp = ent.Comp;
        var pos = args.Position;
        var part = comp.ComponentGrid[(int)pos.X, (int)pos.Y];

        if (comp.PartSlot.Item == null == (part == null))
            return;

        if (comp.PartSlot.Item == null)
        {
            if (part!.Melted) // No removing a part if it's melted
            {
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg"), ent.Owner);
                return;
            }

            var item = SpawnInContainerOrDrop(part!.ProtoId, ent.Owner, "part_slot");
            _entityManager.RemoveComponent<ReactorPartComponent>(item);
            _entityManager.AddComponent(item, new ReactorPartComponent(part!));

            _adminLog.Add(LogType.Action, $"{ToPrettyString(args.Actor):actor} removed {ToPrettyString(item):item} from position {pos.Y},{pos.X} in {ToPrettyString(ent):target}");
            comp.ComponentGrid[(int)pos.X, (int)pos.Y] = null;
        }
        else
        {
            if (TryComp(comp.PartSlot.Item, out ReactorPartComponent? reactorPart))
                comp.ComponentGrid[(int)pos.X, (int)pos.Y] = new ReactorPartComponent(reactorPart);
            else
                return;

            _adminLog.Add(LogType.Action, $"{ToPrettyString(args.Actor):actor} added {ToPrettyString(comp.PartSlot.Item):item} to position {pos.Y},{pos.X} in {ToPrettyString(ent):target}");
            var proto = _entityManager.GetComponent<MetaDataComponent>(comp.PartSlot.Item.Value).EntityPrototype;
            comp.ComponentGrid[(int)pos.X, (int)pos.Y]!.ProtoId = proto != null ? proto.ID : "BaseReactorPart";
            _entityManager.DeleteEntity(comp.PartSlot.Item);
        }

        UpdateGridVisual(ent);
        UpdateGasVolume(comp);
        UpdateUI(ent.Owner, comp);
    }

    private void OnControlRodMessage(Entity<NuclearReactorComponent> ent, ref ReactorControlRodModifyMessage args)
    {
        if(AdjustControlRods(ent.Comp, args.Change))
            // Data is sent to a log queue to avoid spamming the admin log when adjusting values rapidly
            if(!_logQueue.TryGetValue(new(args.Actor, ent.Owner), out var value))
                _logQueue.Add(new(args.Actor, ent.Owner), new LogData {
                    CreationTime = _gameTiming.RealTime,
                    SetControlRodInsertion = ent.Comp.ControlRodInsertion
                });
            else
                value.SetControlRodInsertion = ent.Comp.ControlRodInsertion;

        UpdateUI(ent.Owner, ent.Comp);
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

                if (log.Value.SetControlRodInsertion != null)
                    _adminLog.Add(LogType.Action, $"{ToPrettyString(log.Key.Key):actor} set control rod insertion of {ToPrettyString(log.Key.Value):target} to {log.Value.SetControlRodInsertion}");
            }

            foreach (var kvp in toRemove)
                _logQueue.Remove(kvp);
        }
    }
    #endregion

    private void OnSignalReceived(EntityUid uid, NuclearReactorComponent comp, ref SignalReceivedEvent args)
    {
        var state = SignalState.Momentary;
        args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

        if (args.Port == comp.ControlRodInsertPort)
            comp.InsertPortState = state;
        else if (args.Port == comp.ControlRodRetractPort)
            comp.RetractPortState = state;

        var logtext = "maintain";
        if (comp.InsertPortState != SignalState.Low && comp.RetractPortState == SignalState.Low)
            logtext = "insert";
        else if (comp.RetractPortState != SignalState.Low && comp.InsertPortState == SignalState.Low)
            logtext = "retract";

        _adminLog.Add(LogType.Action, $"{ToPrettyString(args.Trigger):trigger} set control rod insertion of {ToPrettyString(uid):target} to {logtext}");
    }

    private void OnPortDisconnected(EntityUid uid, NuclearReactorComponent comp, ref PortDisconnectedEvent args)
    {
        if (args.Port == comp.ControlRodInsertPort)
            comp.InsertPortState = SignalState.Low;
        if (args.Port == comp.ControlRodRetractPort)
            comp.RetractPortState = SignalState.Low;
    }

    #region Anchoring
    private void OnAnchorChanged(EntityUid uid, NuclearReactorComponent comp, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
        {
            CleanUp(comp);
            return;
        }
    }

    private void OnUnanchorAttempt(EntityUid uid, NuclearReactorComponent comp, ref UnanchorAttemptEvent args)
    {
        // One does not simply move a reactor that has welded itself in place
        if (comp.Melted)
        {
            _popupSystem.PopupEntity(Loc.GetString("reactor-unanchor-melted"), args.User, args.User, PopupType.LargeCaution);
            args.Cancel();
            return;
        }

        if (comp.Temperature >= Atmospherics.T0C + 80 || !CheckEmpty(comp))
        {
            _popupSystem.PopupEntity(Loc.GetString("reactor-unanchor-warning"), args.User, args.User, PopupType.LargeCaution);
            args.Cancel();
        }
    }

    private static bool CheckEmpty(NuclearReactorComponent comp)
    {
        for (var x = 0; x < comp.ReactorGridWidth; x++)
            for (var y = 0; y < comp.ReactorGridHeight; y++)
                if (comp.ComponentGrid[x, y] != null)
                    return false;
        return true;
    }

    private bool GetPipes(EntityUid uid, NuclearReactorComponent comp, [NotNullWhen(true)] out PipeNode? inlet, [NotNullWhen(true)] out PipeNode? outlet)
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
            _popupSystem.PopupEntity(Loc.GetString("reactor-unanchor-warning"), uid, PopupType.MediumCaution);
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

    private void CleanUp(NuclearReactorComponent comp)
    {
        QueueDel(comp.InletEnt);
        QueueDel(comp.OutletEnt);
    }

    private void OnDamaged(EntityUid uid, NuclearReactorComponent comp, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        var damage = (float)args.DamageDelta.GetTotal();
        var destruction = 100;

        var throwProb = Math.Clamp(damage / destruction, 0, 1);
        var coords = _transformSystem.GetMapCoordinates(uid);
        for (var x = 0; x < comp.ReactorGridWidth; x++)
            for (var y = 0; y < comp.ReactorGridHeight; y++)
                if (comp.ComponentGrid[x, y] != null && _random.Prob(throwProb))
                {
                    var reactorPart = comp.ComponentGrid[x, y];
                    if (reactorPart == null)
                        continue;

                    EntityUid item;
                    if (_random.Prob(0.5f) || reactorPart.Melted)
                        item = Spawn("NuclearDebrisChunk", coords);
                    else
                    {
                        item = Spawn(reactorPart.ProtoId, coords);
                        _entityManager.RemoveComponent<ReactorPartComponent>(item);
                        _entityManager.AddComponent(item, new ReactorPartComponent(reactorPart));
                    }

                    _throwingSystem.TryThrow(item, _random.NextAngle().ToVec().Normalized(), _random.NextFloat(8, 16), uid);
                    _adminLog.Add(LogType.Action, $"Damage by {ToPrettyString(args.Origin):actor} removed {ToPrettyString(item):item} from position {x},{y} in {ToPrettyString(uid):target}");

                    comp.ComponentGrid[x, y] = null;

                    UpdateGridVisual((uid, comp));
                    UpdateGasVolume(comp);
                }
    }

    private void OnRejuvenate(EntityUid uid, NuclearReactorComponent comp, ref RejuvenateEvent args)
    {
        comp.Temperature = Atmospherics.T20C;
        comp.LastSendTemperature = comp.Temperature;
        comp.Melted = false;
        comp.IsBurning = false;
        comp.IsSmoking = false;
        comp.RadiationLevel = 0;
        comp.ThermalPower = 0;
        comp.ControlRodInsertion = 2;
        comp.ApplyPrefab = true;
    }
}
