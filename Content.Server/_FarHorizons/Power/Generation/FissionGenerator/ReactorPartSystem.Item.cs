using Content.Server.Atmos.EntitySystems;
using Content.Shared._FarHorizons.Power.Generation.FissionGenerator;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Nutrition;
using Content.Shared.Radiation.Components;

namespace Content.Server._FarHorizons.Power.Generation.FissionGenerator;

public sealed partial class ReactorPartSystem
{
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;

    private void OnAtmosExposed(EntityUid uid, ReactorPartComponent component, ref AtmosExposedUpdateEvent args)
    {
        // Stops it from cooking the room while in the reactor
        if(!TryComp(uid, out MetaDataComponent? metaData) || (metaData.Flags & MetaDataFlags.InContainer) == MetaDataFlags.InContainer)
            return;

        // Can't use args.GasMixture because then it wouldn't excite the tile
        var gasMix = _atmosphereSystem.GetContainingMixture(uid, false, true) ?? GasMixture.SpaceGas;
        if(gasMix.TotalMoles < Atmospherics.GasMinMoles)
            gasMix = GasMixture.SpaceGas;

        var DeltaT = (component.Temperature - gasMix.Temperature) * 0.01f;

        if (Math.Abs(DeltaT) < 0.1)
            return;

        component.Temperature -= DeltaT;
        if (!gasMix.Immutable) // This prevents it from heating up space itself
            // This viloates COE, but if energy is conserved, then pulling out a hot rod will instantly turn the room into an oven
            gasMix.Temperature += 0.1f * DeltaT * component.ThermalMass / _atmosphereSystem.GetHeatCapacity(gasMix, false);

        var burncomp = EnsureComp<DamageOnInteractComponent>(uid);

        burncomp.IsDamageActive = component.Temperature > Atmospherics.T0C + _hotTemp;

        if (burncomp.IsDamageActive)
        {
            var damage = Math.Max((component.Temperature - Atmospherics.T0C - _hotTemp) / _burnDiv, 0);

            // Giant string of if/else that makes sure it will interfere only as much as it needs to
            if (burncomp.Damage == null)
                burncomp.Damage = new() { DamageDict = new() { { "Heat", damage } } };
            else if (burncomp.Damage.DamageDict == null)
                burncomp.Damage.DamageDict = new() { { "Heat", damage } };
            else if (!burncomp.Damage.DamageDict.ContainsKey("Heat"))
                burncomp.Damage.DamageDict.Add("Heat", damage);
            else
                burncomp.Damage.DamageDict["Heat"] = damage;
        }

        Dirty(uid, burncomp);
    }
}