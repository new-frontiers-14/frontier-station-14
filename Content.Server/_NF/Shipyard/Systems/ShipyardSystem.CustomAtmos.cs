using System.Linq;
using Content.Server.Atmos.Components;
using Content.Shared._NF.Shipyard.Prototypes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;

namespace Content.Server._NF.Shipyard.Systems;

using ShuttleGridEntity = Entity<GridAtmosphereComponent?, GasTileOverlayComponent?, MapGridComponent?>;

public sealed partial class ShipyardSystem
{
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
}
