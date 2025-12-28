using System.Linq;
using Content.Server._NF.Shipyard.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Trinary.Components;
using Content.Server.Atmos.Piping.Trinary.EntitySystems;
using Content.Shared._NF.Shipyard.Prototypes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;

namespace Content.Server._NF.Shipyard.Systems;

using ShuttleGridEntity = Entity<GridAtmosphereComponent?, GasTileOverlayComponent?, MapGridComponent?>;

public sealed partial class ShipyardSystem
{
    [Dependency] private readonly GasMixerSystem _gasMixerSystem = default!;

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
}
