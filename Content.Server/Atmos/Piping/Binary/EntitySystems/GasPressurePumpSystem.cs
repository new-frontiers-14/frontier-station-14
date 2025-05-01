using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Audio;
using JetBrains.Annotations;
using Content.Server.Administration.Logs; // Frontier
using Content.Shared.Database; // Frontier

namespace Content.Server.Atmos.Piping.Binary.EntitySystems;

[UsedImplicitly]
public sealed class GasPressurePumpSystem : SharedGasPressurePumpSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!; // Frontier

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasPressurePumpComponent, AtmosDeviceUpdateEvent>(OnPumpUpdated);
    }

    private void OnPumpUpdated(EntityUid uid, GasPressurePumpComponent pump, ref AtmosDeviceUpdateEvent args)
    {
        if (!pump.Enabled
            || (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
            || !_nodeContainer.TryGetNodes(uid, pump.InletName, pump.OutletName, out PipeNode? inlet, out PipeNode? outlet))
        {
            _ambientSoundSystem.SetAmbience(uid, false);
            return;
        }

        var outputStartingPressure = outlet.Air.Pressure;

        if (outputStartingPressure >= pump.TargetPressure)
        {
            _ambientSoundSystem.SetAmbience(uid, false);
            return; // No need to pump gas if target has been reached.
        }

        if (inlet.Air.TotalMoles > 0 && inlet.Air.Temperature > 0)
        {
            // We calculate the necessary moles to transfer using our good ol' friend PV=nRT.
            var pressureDelta = pump.TargetPressure - outputStartingPressure;
            var transferMoles = (pressureDelta * outlet.Air.Volume) / (inlet.Air.Temperature * Atmospherics.R);

            var removed = inlet.Air.Remove(transferMoles);
            _atmosphereSystem.Merge(outlet.Air, removed);
            _ambientSoundSystem.SetAmbience(uid, removed.TotalMoles > 0f);
        }
    }

    // Frontier: server-side pump accessors
    public void SetPumpDirection(Entity<GasPressurePumpComponent> ent, bool inwards, EntityUid actor)
    {
        if (!ent.Comp.SettableDirection || ent.Comp.PumpingInwards == inwards)
            return;

        (ent.Comp.OutletName, ent.Comp.InletName) = (ent.Comp.InletName, ent.Comp.OutletName);

        ent.Comp.PumpingInwards = inwards;
        _adminLogger.Add(LogType.AtmosDirectionChanged,
            LogImpact.Medium,
            $"{ToPrettyString(actor):player} set the direction on {ToPrettyString(ent):device} to {(inwards ? "in" : "out")}");
        Dirty(ent);
        UpdateAppearance(ent, ent.Comp);
    }

    public void SetPumpPressure(Entity<GasPressurePumpComponent> ent, float pressure, EntityUid actor)
    {
        ent.Comp.TargetPressure = Math.Clamp(pressure, 0f, Atmospherics.MaxOutputPressure);
        _adminLogger.Add(LogType.AtmosPressureChanged,
            LogImpact.Medium,
            $"{ToPrettyString(actor):player} set the pressure on {ToPrettyString(ent):device} to {pressure}kPa");
        Dirty(ent, ent.Comp);
    }

    public void SetPumpStatus(Entity<GasPressurePumpComponent> ent, bool enabled, EntityUid actor)
    {
        ent.Comp.Enabled = enabled;
        _adminLogger.Add(LogType.AtmosPowerChanged,
            LogImpact.Medium,
            $"{ToPrettyString(actor):player} set the power on {ToPrettyString(ent):device} to {enabled}");
        Dirty(ent);
        UpdateAppearance(ent, ent.Comp);
    }
    // End Frontier: server-side pump accessors
}
