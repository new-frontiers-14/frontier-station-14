using Content.Server.Atmos.Monitor.Components;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Pinpointer;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Consoles;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._NF.Atmos.Components; // Frontier
using Content.Server.Atmos.Piping.Binary.Components; // Frontier
using Content.Server.Atmos.Piping.Binary.EntitySystems; // Frontier
using Content.Server.NodeContainer.EntitySystems; // Frontier
using Content.Server.NodeContainer.Nodes; // Frontier
using Content.Shared._NF.Atmos.BUI; // Frontier
using Content.Shared._NF.Atmos.Piping.Binary.Messages; // Frontier
using Content.Shared.Atmos.Piping.Binary.Components; // Frontier
using Content.Shared.Shuttles.Events; // Frontier
using Content.Server.Shuttles.Systems;
using Content.Server.Shuttles.Components; // Frontier

namespace Content.Server.Atmos.Monitor.Systems;

public sealed class AtmosAlertsComputerSystem : SharedAtmosAlertsComputerSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly AirAlarmSystem _airAlarmSystem = default!;
    [Dependency] private readonly AtmosDeviceNetworkSystem _atmosDevNet = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly NavMapSystem _navMapSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceListSystem _deviceListSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!; // Frontier
    [Dependency] private readonly GasPressurePumpSystem _pressurePump = default!; // Frontier
    [Dependency] private readonly DockingSystem _docking = default!; // Frontier

    private const float UpdateTime = 1.0f;

    // Note: this data does not need to be saved
    private float _updateTimer = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        // Console events
        SubscribeLocalEvent<AtmosAlertsComputerComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<AtmosAlertsComputerComponent, EntParentChangedMessage>(OnConsoleParentChanged);
        SubscribeLocalEvent<AtmosAlertsComputerComponent, AtmosAlertsComputerFocusChangeMessage>(OnFocusChangedMessage);

        // Grid events
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);

        // Alarm events
        SubscribeLocalEvent<AtmosAlertsDeviceComponent, EntityTerminatingEvent>(OnDeviceTerminatingEvent);
        SubscribeLocalEvent<AtmosAlertsDeviceComponent, AnchorStateChangedEvent>(OnDeviceAnchorChanged);

        // Frontier: gaslock event handlers
        SubscribeLocalEvent<AtmosAlertsComputerComponent, UndockRequestMessage>(OnUndockRequestMessage);
        SubscribeLocalEvent<AtmosAlertsComputerComponent, RemoteGasPressurePumpChangePumpDirectionMessage>(OnPumpDirectionMessage);
        SubscribeLocalEvent<AtmosAlertsComputerComponent, RemoteGasPressurePumpChangeOutputPressureMessage>(OnPumpPressureMessage);
        SubscribeLocalEvent<AtmosAlertsComputerComponent, RemoteGasPressurePumpToggleStatusMessage>(OnPumpStatusMessage);
    }

    #region Event handling

    private void OnConsoleInit(EntityUid uid, AtmosAlertsComputerComponent component, ComponentInit args)
    {
        InitalizeConsole(uid, component);
    }

    private void OnConsoleParentChanged(EntityUid uid, AtmosAlertsComputerComponent component, EntParentChangedMessage args)
    {
        InitalizeConsole(uid, component);
    }

    private void OnFocusChangedMessage(EntityUid uid, AtmosAlertsComputerComponent component, AtmosAlertsComputerFocusChangeMessage args)
    {
        component.FocusDevice = args.FocusDevice;
    }

    private void OnGridSplit(ref GridSplitEvent args)
    {
        // Collect grids
        var allGrids = args.NewGrids.ToList();

        if (!allGrids.Contains(args.Grid))
            allGrids.Add(args.Grid);

        // Update atmos monitoring consoles that stand upon an updated grid
        var query = AllEntityQuery<AtmosAlertsComputerComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (entXform.GridUid == null)
                continue;

            if (!allGrids.Contains(entXform.GridUid.Value))
                continue;

            InitalizeConsole(ent, entConsole);
        }
    }

    private void OnDeviceAnchorChanged(EntityUid uid, AtmosAlertsDeviceComponent component, AnchorStateChangedEvent args)
    {
        OnDeviceAdditionOrRemoval(uid, component, args.Anchored);
    }

    private void OnDeviceTerminatingEvent(EntityUid uid, AtmosAlertsDeviceComponent component, ref EntityTerminatingEvent args)
    {
        OnDeviceAdditionOrRemoval(uid, component, false);
    }

    private void OnDeviceAdditionOrRemoval(EntityUid uid, AtmosAlertsDeviceComponent component, bool isAdding)
    {
        var xform = Transform(uid);
        var gridUid = xform.GridUid;

        if (gridUid == null)
            return;

        if (!TryComp<NavMapComponent>(xform.GridUid, out var navMap))
            return;

        if (!TryGetAtmosDeviceNavMapData(uid, component, xform, out var data))
            return;

        var netEntity = GetNetEntity(uid);

        var query = AllEntityQuery<AtmosAlertsComputerComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (gridUid != entXform.GridUid)
                continue;

            if (isAdding)
            {
                entConsole.AtmosDevices.Add(data.Value);
            }

            else
            {
                entConsole.AtmosDevices.RemoveWhere(x => x.NetEntity == netEntity);
                _navMapSystem.RemoveNavMapRegion(gridUid.Value, navMap, netEntity);
            }

            Dirty(ent, entConsole);
        }
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            // Keep a list of UI entries for each gridUid, in case multiple consoles stand on the same grid
            var airAlarmEntriesForEachGrid = new Dictionary<EntityUid, AtmosAlertsComputerEntry[]>();
            var fireAlarmEntriesForEachGrid = new Dictionary<EntityUid, AtmosAlertsComputerEntry[]>();
            var gaslockEntriesForEachGrid = new Dictionary<EntityUid, AtmosAlertsComputerEntry[]>(); // Frontier

            var query = AllEntityQuery<AtmosAlertsComputerComponent, TransformComponent>();
            while (query.MoveNext(out var ent, out var entConsole, out var entXform))
            {
                if (entXform?.GridUid == null)
                    continue;

                // Make a list of alarm state data for all the air and fire alarms on the grid
                if (!airAlarmEntriesForEachGrid.TryGetValue(entXform.GridUid.Value, out var airAlarmEntries))
                {
                    airAlarmEntries = GetAlarmStateData(entXform.GridUid.Value, AtmosAlertsComputerGroup.AirAlarm).ToArray();
                    airAlarmEntriesForEachGrid[entXform.GridUid.Value] = airAlarmEntries;
                }

                if (!fireAlarmEntriesForEachGrid.TryGetValue(entXform.GridUid.Value, out var fireAlarmEntries))
                {
                    fireAlarmEntries = GetAlarmStateData(entXform.GridUid.Value, AtmosAlertsComputerGroup.FireAlarm).ToArray();
                    fireAlarmEntriesForEachGrid[entXform.GridUid.Value] = fireAlarmEntries;
                }

                // Frontier: gaslocks (note: no alarm state)
                if (!gaslockEntriesForEachGrid.TryGetValue(entXform.GridUid.Value, out var gaslockEntries))
                {
                    gaslockEntries = GetAlarmStateData(entXform.GridUid.Value, AtmosAlertsComputerGroup.Gaslock).ToArray();
                    gaslockEntriesForEachGrid[entXform.GridUid.Value] = gaslockEntries;
                }
                // End Frontier

                // Determine the highest level of alert for the console (based on non-silenced alarms)
                var highestAlert = AtmosAlarmType.Invalid;

                foreach (var entry in airAlarmEntries)
                {
                    if (entry.AlarmState > highestAlert && !entConsole.SilencedDevices.Contains(entry.NetEntity))
                        highestAlert = entry.AlarmState;
                }

                foreach (var entry in fireAlarmEntries)
                {
                    if (entry.AlarmState > highestAlert && !entConsole.SilencedDevices.Contains(entry.NetEntity))
                        highestAlert = entry.AlarmState;
                }

                // Update the appearance of the console based on the highest recorded level of alert
                if (TryComp<AppearanceComponent>(ent, out var entAppearance))
                    _appearance.SetData(ent, AtmosAlertsComputerVisuals.ComputerLayerScreen, (int) highestAlert, entAppearance);

                // If the console UI is open, send UI data to each subscribed session
                UpdateUIState(ent, airAlarmEntries, fireAlarmEntries, gaslockEntries, entConsole, entXform); // Frontier: add gaslockEntries
            }
        }
    }

    public void UpdateUIState
        (EntityUid uid,
        AtmosAlertsComputerEntry[] airAlarmStateData,
        AtmosAlertsComputerEntry[] fireAlarmStateData,
        AtmosAlertsComputerEntry[] gaslockStateData, // Frontier
        AtmosAlertsComputerComponent component,
        TransformComponent xform)
    {
        if (!_userInterfaceSystem.IsUiOpen(uid, AtmosAlertsComputerUiKey.Key))
            return;

        var gridUid = xform.GridUid!.Value;

        if (!HasComp<MapGridComponent>(gridUid))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        EnsureComp<NavMapComponent>(gridUid);

        // Gathering remaining data to be send to the client
        var focusAlarmData = GetFocusAlarmData(uid, GetEntity(component.FocusDevice), gridUid);

        var focusGaslockData = GetFocusGaslockData(uid, GetEntity(component.FocusDevice), gridUid); // Frontier

        // Set the UI state
        _userInterfaceSystem.SetUiState(uid, AtmosAlertsComputerUiKey.Key,
            new AtmosAlertsComputerBoundInterfaceState(airAlarmStateData, fireAlarmStateData, focusAlarmData, gaslockStateData, focusGaslockData)); // Frontier: add gaslockStateData, focusGaslockData
    }

    private List<AtmosAlertsComputerEntry> GetAlarmStateData(EntityUid gridUid, AtmosAlertsComputerGroup group)
    {
        var alarmStateData = new List<AtmosAlertsComputerEntry>();

        var queryAlarms = AllEntityQuery<AtmosAlertsDeviceComponent, AtmosAlarmableComponent, DeviceNetworkComponent, TransformComponent>();
        while (queryAlarms.MoveNext(out var ent, out var entDevice, out var entAtmosAlarmable, out var entDeviceNetwork, out var entXform))
        {
            if (entXform.GridUid != gridUid)
                continue;

            if (!entXform.Anchored)
                continue;

            if (entDevice.Group != group)
                continue;

            if (!TryComp<MapGridComponent>(entXform.GridUid, out var mapGrid))
                continue;

            if (!TryComp<NavMapComponent>(entXform.GridUid, out var navMap))
                continue;

            // If emagged, change the alarm type to normal
            var alarmState = (entAtmosAlarmable.LastAlarmState == AtmosAlarmType.Emagged) ? AtmosAlarmType.Normal : entAtmosAlarmable.LastAlarmState;

            // Unpowered alarms can't sound
            if (TryComp<ApcPowerReceiverComponent>(ent, out var entAPCPower) && !entAPCPower.Powered)
                alarmState = AtmosAlarmType.Invalid;

            // Create entry
            var netEnt = GetNetEntity(ent);

            var entry = new AtmosAlertsComputerEntry
                (netEnt,
                GetNetCoordinates(entXform.Coordinates),
                entDevice.Group,
                alarmState,
                MetaData(ent).EntityName,
                entDeviceNetwork.Address);

            // Get the list of sensors attached to the alarm
            var sensorList = TryComp<DeviceListComponent>(ent, out var entDeviceList) ? _deviceListSystem.GetDeviceList(ent, entDeviceList) : null;

            if (sensorList?.Any() == true)
            {
                var alarmRegionSeeds = new HashSet<Vector2i>();

                // If valid and anchored, use the position of sensors as seeds for the region
                foreach (var (address, sensorEnt) in sensorList)
                {
                    if (!sensorEnt.IsValid() || !HasComp<AtmosMonitorComponent>(sensorEnt))
                        continue;

                    var sensorXform = Transform(sensorEnt);

                    if (sensorXform.Anchored && sensorXform.GridUid == entXform.GridUid)
                        alarmRegionSeeds.Add(_mapSystem.CoordinatesToTile(entXform.GridUid.Value, mapGrid, _transformSystem.GetMapCoordinates(sensorEnt, sensorXform)));
                }

                var regionProperties = new SharedNavMapSystem.NavMapRegionProperties(netEnt, AtmosAlertsComputerUiKey.Key, alarmRegionSeeds);
                _navMapSystem.AddOrUpdateNavMapRegion(gridUid, navMap, netEnt, regionProperties);
            }

            else
            {
                _navMapSystem.RemoveNavMapRegion(entXform.GridUid.Value, navMap, netEnt);
            }

            alarmStateData.Add(entry);
        }

        return alarmStateData;
    }

    private AtmosAlertsFocusDeviceData? GetFocusAlarmData(EntityUid uid, EntityUid? focusDevice, EntityUid gridUid)
    {
        if (focusDevice == null)
            return null;

        var focusDeviceXform = Transform(focusDevice.Value);

        if (!focusDeviceXform.Anchored ||
            focusDeviceXform.GridUid != gridUid ||
            !TryComp<AirAlarmComponent>(focusDevice.Value, out var focusDeviceAirAlarm))
        {
            return null;
        }

        // Force update the sensors attached to the alarm
        if (!_userInterfaceSystem.IsUiOpen(focusDevice.Value, SharedAirAlarmInterfaceKey.Key))
        {
            _atmosDevNet.Register(focusDevice.Value, null);
            _atmosDevNet.Sync(focusDevice.Value, null);

            foreach ((var address, var _) in focusDeviceAirAlarm.SensorData)
                _atmosDevNet.Register(uid, null);
        }

        // Get the sensor data
        var temperatureData = (_airAlarmSystem.CalculateTemperatureAverage(focusDeviceAirAlarm), AtmosAlarmType.Normal);
        var pressureData = (_airAlarmSystem.CalculatePressureAverage(focusDeviceAirAlarm), AtmosAlarmType.Normal);
        var gasData = new Dictionary<Gas, (float, float, AtmosAlarmType)>();

        foreach ((var address, var sensorData) in focusDeviceAirAlarm.SensorData)
        {
            if (sensorData.TemperatureThreshold.CheckThreshold(sensorData.Temperature, out var temperatureState) &&
                (int) temperatureState > (int) temperatureData.Item2)
            {
                temperatureData = (temperatureData.Item1, temperatureState);
            }

            if (sensorData.PressureThreshold.CheckThreshold(sensorData.Pressure, out var pressureState) &&
                (int) pressureState > (int) pressureData.Item2)
            {
                pressureData = (pressureData.Item1, pressureState);
            }

            if (focusDeviceAirAlarm.SensorData.Sum(g => g.Value.TotalMoles) > 1e-8)
            {
                foreach ((var gas, var threshold) in sensorData.GasThresholds)
                {
                    if (!gasData.ContainsKey(gas))
                    {
                        float mol = _airAlarmSystem.CalculateGasMolarConcentrationAverage(focusDeviceAirAlarm, gas, out var percentage);

                        if (mol < 1e-8)
                            continue;

                        gasData[gas] = (mol, percentage, AtmosAlarmType.Normal);
                    }

                    if (threshold.CheckThreshold(gasData[gas].Item2, out var gasState) &&
                        (int) gasState > (int) gasData[gas].Item3)
                    {
                        gasData[gas] = (gasData[gas].Item1, gasData[gas].Item2, gasState);
                    }
                }
            }
        }

        return new AtmosAlertsFocusDeviceData(GetNetEntity(focusDevice.Value), temperatureData, pressureData, gasData);
    }

    private HashSet<AtmosAlertsDeviceNavMapData> GetAllAtmosDeviceNavMapData(EntityUid gridUid)
    {
        var atmosDeviceNavMapData = new HashSet<AtmosAlertsDeviceNavMapData>();

        var query = AllEntityQuery<AtmosAlertsDeviceComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entComponent, out var entXform))
        {
            if (entXform.GridUid != gridUid)
                continue;

            if (TryGetAtmosDeviceNavMapData(ent, entComponent, entXform, out var data))
                atmosDeviceNavMapData.Add(data.Value);
        }

        return atmosDeviceNavMapData;
    }

    private bool TryGetAtmosDeviceNavMapData
        (EntityUid uid,
        AtmosAlertsDeviceComponent component,
        TransformComponent xform,
        [NotNullWhen(true)] out AtmosAlertsDeviceNavMapData? output)
    {
        output = null;

        if (!xform.Anchored)
            return false;

        output = new AtmosAlertsDeviceNavMapData(GetNetEntity(uid), GetNetCoordinates(xform.Coordinates), component.Group);

        return true;
    }

    private void InitalizeConsole(EntityUid uid, AtmosAlertsComputerComponent component)
    {
        var xform = Transform(uid);

        if (xform.GridUid == null)
            return;

        var grid = xform.GridUid.Value;
        component.AtmosDevices = GetAllAtmosDeviceNavMapData(grid);

        Dirty(uid, component);
    }

    // Frontier: gets gaslock BUI state for a particular entity
    private AtmosAlertsFocusGaslockData? GetFocusGaslockData(EntityUid uid, EntityUid? focusDevice, EntityUid gridUid)
    {
        if (focusDevice == null)
            return null;

        var focusDeviceXform = Transform(focusDevice.Value);

        // Hack: assuming all pumps are dockable pumps, pressure pumps, and have one non-dockable node
        if (!focusDeviceXform.Anchored ||
            focusDeviceXform.GridUid != gridUid ||
            !TryComp<GasPressurePumpComponent>(focusDevice.Value, out var pressurePump) ||
            !TryComp<DockingComponent>(focusDevice.Value, out var docking))
        {
            return null;
        }

        var gasData = new Dictionary<Gas, (float, float)>();
        if (TryComp<DockablePipeComponent>(focusDevice.Value, out var dockablePump) &&
        _nodeContainer.TryGetNode(focusDevice.Value, dockablePump.InternalNodeName, out PipeNode? port))
        {
            if (port.Air.TotalMoles > 1e-8)
            {
                var totalMoles = port.Air.TotalMoles;
                for (int i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                {
                    var moles = port.Air.GetMoles(i);
                    if (moles < 1e-8)
                        continue;
                    gasData[(Gas)i] = (moles, moles / totalMoles);
                }
            }
        }

        if (!TryGetNetEntity(docking.DockedWith, out var dockingNetEnt))
            dockingNetEnt = NetEntity.Invalid;

        return new AtmosAlertsFocusGaslockData(GetNetEntity(focusDevice.Value),
        pressurePump.TargetPressure, // float pressure,
        pressurePump.PumpingInwards, // bool pumpingInwards,
        pressurePump.Enabled, // bool enabled,
        dockingNetEnt.Value, // NetEntity dockedEntity,
        gasData); // Dictionary<Gas, (float, float)> gasData)
    }

    // Frontier: message handlers for gaslock state
    private void OnUndockRequestMessage(EntityUid gridUid, AtmosAlertsComputerComponent comp, UndockRequestMessage args)
    {
        var dockUid = GetEntity(args.DockEntity);
        if (!HasComp<DockablePipeComponent>(dockUid) ||
            !TryComp<DockingComponent>(dockUid, out var dockComp))
            return;
        _docking.Undock((dockUid, dockComp));
    }

    // We want this to be doing whatever the pressure pump is doing, so we're hijacking the GasPressurePumpSystem interface.
    private void OnPumpDirectionMessage(EntityUid gridUid, AtmosAlertsComputerComponent comp, RemoteGasPressurePumpChangePumpDirectionMessage args)
    {
        var pumpUid = GetEntity(args.Pump);
        if (!TryComp<GasPressurePumpComponent>(pumpUid, out var pumpComp) || !pumpComp.SettableDirection)
            return;
        _pressurePump.SetPumpDirection((pumpUid, pumpComp), args.Inwards, args.Actor);
    }

    private void OnPumpPressureMessage(EntityUid gridUid, AtmosAlertsComputerComponent comp, RemoteGasPressurePumpChangeOutputPressureMessage args)
    {
        var pumpUid = GetEntity(args.Pump);
        if (!TryComp<GasPressurePumpComponent>(pumpUid, out var pumpComp))
            return;
        _pressurePump.SetPumpPressure((pumpUid, pumpComp), args.Pressure, args.Actor);
    }

    private void OnPumpStatusMessage(EntityUid gridUid, AtmosAlertsComputerComponent comp, RemoteGasPressurePumpToggleStatusMessage args)
    {
        var pumpUid = GetEntity(args.Pump);
        if (!TryComp<GasPressurePumpComponent>(pumpUid, out var pumpComp))
            return;
        _pressurePump.SetPumpStatus((pumpUid, pumpComp), args.Enabled, args.Actor);
    }
    // End Frontier
}
