using System.Linq;
using Content.Server._NF.Shipyard.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Trinary.Components;
using Content.Server.Atmos.Piping.Trinary.EntitySystems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared._NF.Shipyard.Prototypes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.Map.Components;

namespace Content.Server._NF.Shipyard.Systems;

using ShuttleGridEntity = Entity<GridAtmosphereComponent?, GasTileOverlayComponent?, MapGridComponent?>;

public sealed partial class ShipyardSystem
{
    [Dependency] private readonly GasMixerSystem _gasMixerSystem = default!;
    [Dependency] private readonly AtmosMonitorSystem _atmosMonitorSystem = default!;
    [Dependency] private readonly GasVentScrubberSystem _gasVentScrubberSystem = default!;

    /// <summary>
    /// Sets up the atmosphere of a shuttle from the given prototype.
    /// </summary>
    /// <param name="shuttleGrid">The target shuttle's grid</param>
    /// <param name="atmos">The prototype of the desired atmosphere</param>
    private void SetShuttleAtmosphere(
        ShuttleGridEntity shuttleGrid,
        ShuttleAtmospherePrototype atmos)
    {
        // GridAtmosphereComponent and GasTileOverlayComponent are internal to AtmosphereSystem and you shouldn't use
        // them directly here. We're resolving the components explicitly because we expect them to exist, the atmos code
        // silently fails if they don't, and if it would fail anyway we can skip this whole endeavor.
        if (!Resolve(shuttleGrid, ref shuttleGrid.Comp1, ref shuttleGrid.Comp2, ref shuttleGrid.Comp3))
        {
            return;
        }

        ReplaceShuttleAtmosphere(shuttleGrid, atmos);
        SetupShuttleDistroGasMixers(shuttleGrid, atmos);
        SetupShuttleDistroAtmosAlarms(shuttleGrid, atmos);
    }

    private void ReplaceShuttleAtmosphere(
        ShuttleGridEntity shuttleGrid,
        ShuttleAtmospherePrototype atmos)
    {
        var mapGrid = shuttleGrid.Comp3!;

        var mix = new GasMixture(Atmospherics.CellVolume) { Temperature = atmos.Temperature };
        foreach (var (gas, moles) in atmos.Atmosphere)
        {
            mix.SetMoles(gas, moles);
        }

        var query = GetEntityQuery<AtmosFixMarkerComponent>();

        var enumerator = _map.GetAllTilesEnumerator(shuttleGrid, mapGrid);
        while (enumerator.MoveNext(out var tileRef))
        {
            var position = tileRef.Value.GridIndices;

            // Skip any tiles without an atmosphere or with an immutable atmosphere (we can't change those)
            if (_atmosphere.GetTileMixture(shuttleGrid, null, position, true) is not { Immutable: false } air)
                continue;

            // Skip any tiles with an AtmosFixMarker; they already have the correct atmosphere from FixGridAtmosCommand
            if (_map.GetAnchoredEntities((shuttleGrid.Owner, mapGrid), position).Any(query.HasComp))
                continue;

            // Replace atmosphere
            air.CopyFrom(mix);
        }
    }

    // Assumption: There are way fewer shuttles in the game than there are top-level entities on the new grid, so
    // it's faster to look up all distro machines and check that they're on the shuttle we're setting up than going
    // through all entities on the shuttle and check that they have the atmos component

    private void SetupShuttleDistroGasMixers(
        ShuttleGridEntity shuttleGrid,
        ShuttleAtmospherePrototype atmos)
    {
        var enumerator = EntityQueryEnumerator<GasMixerAutoSetupComponent, GasMixerComponent>();
        while (enumerator.MoveNext(out var entity, out var setup, out var mixer))
        {
            if (Transform(entity).GridUid != shuttleGrid.Owner)
                continue;

            var inletOneGasAmount = atmos.Atmosphere.GetValueOrDefault(setup.InletOneGas, 0f);
            var inletTwoGasAmount = atmos.Atmosphere.GetValueOrDefault(setup.InletTwoGas, 0f);
            var enabled = inletOneGasAmount != 0 || inletTwoGasAmount != 0;
            _gasMixerSystem.SetMixerEnabled(entity, enabled, mixer);
            if (enabled)
            {
                var ratio = inletOneGasAmount / (inletOneGasAmount + inletTwoGasAmount);
                _gasMixerSystem.SetMixerRatio(entity, ratio, mixer);
            }
        }
    }

    private void SetupShuttleDistroAtmosAlarms(
        ShuttleGridEntity shuttleGrid,
        ShuttleAtmospherePrototype atmos)
    {
        if (atmos.Alarms == null && atmos.FilterGases == null)
        {
            // Nothing to do
            return;
        }

        // Prepare alarm thresholds, if set
        var pressureThreshold = atmos.Alarms?.PressureThreshold;
        AtmosAlarmThresholdPrototype? thresholdPrototype;
        if (pressureThreshold == null && _prototypeManager.TryIndex(atmos.Alarms?.PressureThresholdId, out thresholdPrototype))
        {
            pressureThreshold = new AtmosAlarmThreshold(thresholdPrototype);
        }

        var temperatureThreshold = atmos.Alarms?.TemperatureThreshold;
        if (temperatureThreshold == null && _prototypeManager.TryIndex(atmos.Alarms?.TemperatureThresholdId, out thresholdPrototype))
        {
            temperatureThreshold = new AtmosAlarmThreshold(thresholdPrototype);
        }

        var gasThresholds = atmos.Alarms?.GasThresholds;
        if (gasThresholds == null)
        {
            gasThresholds = new Dictionary<Gas, AtmosAlarmThreshold>();
            foreach (var (gas, protoId) in atmos.Alarms?.GasThresholdPrototypes ?? [])
            {
                if (_prototypeManager.Resolve(protoId, out var gasThresholdPrototype))
                {
                    gasThresholds.Add(gas, new AtmosAlarmThreshold(gasThresholdPrototype));
                }
            }
        }

        // Go through every air alarm on the new shuttle and set the thresholds on all atmos monitors that are linked
        // to it.
        // Note: The UI uses AirAlarmComponent.SensorData to get a list of known devices and decide where to copy
        // settings to. We can't use this here because at this point, no game tick has happened yet, so no sensor data
        // is available.
        var enumerator = EntityQueryEnumerator<AirAlarmComponent, DeviceListComponent>();
        while (enumerator.MoveNext(out var entity, out var alarm, out var deviceList))
        {
            if (Transform(entity).GridUid != shuttleGrid.Owner)
                continue;

            if (atmos.FilterGases != null)
            {
                // Air alarms use a hard-coded gas list in automatic mode, which is very stupid and will discard our
                // overrides
                alarm.AutoMode = false;
            }

            foreach (var device in deviceList.Devices)
            {
                if (HasComp<AtmosMonitorComponent>(device))
                {
                    if (temperatureThreshold != null)
                    {
                        _atmosMonitorSystem.SetThreshold(
                            device,
                            AtmosMonitorThresholdType.Temperature,
                            temperatureThreshold
                        );
                    }

                    if (pressureThreshold != null)
                    {
                        _atmosMonitorSystem.SetThreshold(
                            device,
                            AtmosMonitorThresholdType.Pressure,
                            pressureThreshold
                        );
                    }

                    foreach (var (gas, threshold) in gasThresholds)
                    {
                        _atmosMonitorSystem.SetThreshold(device, AtmosMonitorThresholdType.Gas, threshold, gas);
                    }
                }

                if (atmos.FilterGases != null && TryComp<GasVentScrubberComponent>(device, out var comp))
                {
                    _gasVentScrubberSystem.SetFilterGases(device, atmos.FilterGases, comp);
                }
            }
        }
    }
}
