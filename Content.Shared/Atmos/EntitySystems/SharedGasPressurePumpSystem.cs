using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared._NF.Atmos.Piping.Binary.Messages; // Frontier

namespace Content.Shared.Atmos.EntitySystems;

public abstract class SharedGasPressurePumpSystem : EntitySystem
{
    [Dependency] private   readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private   readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private   readonly SharedPowerReceiverSystem _receiver = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UserInterfaceSystem = default!;

    // TODO: Check enabled for activatableUI
    // TODO: Add activatableUI to it.

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasPressurePumpComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GasPressurePumpComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<GasPressurePumpComponent, GasPressurePumpChangeOutputPressureMessage>(OnOutputPressureChangeMessage);
        SubscribeLocalEvent<GasPressurePumpComponent, GasPressurePumpToggleStatusMessage>(OnToggleStatusMessage);
        SubscribeLocalEvent<GasPressurePumpComponent, GasPressurePumpChangePumpDirectionMessage>(OnPumpSetDirectionMessage); // Frontier

        SubscribeLocalEvent<GasPressurePumpComponent, AtmosDeviceDisabledEvent>(OnPumpLeaveAtmosphere);
        SubscribeLocalEvent<GasPressurePumpComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<GasPressurePumpComponent, MapInitEvent>(OnMapInit); // Frontier
    }

    private void OnExamined(Entity<GasPressurePumpComponent> ent, ref ExaminedEvent args)
    {
        if (!Transform(ent).Anchored)
            return;

        if (Loc.TryGetString("gas-pressure-pump-system-examined",
                out var str,
                ("statusColor", "lightblue"), // TODO: change with pressure?
                ("pressure", ent.Comp.TargetPressure)
            ))
        {
            args.PushMarkup(str);
        }
    }

    // Frontier: run on start pumps
    private void OnMapInit(Entity<GasPressurePumpComponent> ent, ref MapInitEvent args) // Frontier - Init on map
    {
        if (ent.Comp.StartOnMapInit)
        {
            ent.Comp.Enabled = true;
        }
        UpdateAppearance(ent);
    }
    // End Frontier: run on start pumps

    private void OnInit(Entity<GasPressurePumpComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent);
    }

    private void OnPowerChanged(Entity<GasPressurePumpComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<GasPressurePumpComponent, AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        var pumpOn = ent.Comp1.Enabled && _receiver.IsPowered(ent.Owner);
        _appearance.SetData(ent, PumpVisuals.Enabled, pumpOn, ent.Comp2);
        _appearance.SetData(ent, PumpVisuals.PumpingInwards, ent.Comp1.PumpingInwards); // Frontier
    }

    private void OnToggleStatusMessage(Entity<GasPressurePumpComponent> ent, ref GasPressurePumpToggleStatusMessage args)
    {
        ent.Comp.Enabled = args.Enabled;
        _adminLogger.Add(LogType.AtmosPowerChanged,
            LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(ent):device} to {args.Enabled}");
        Dirty(ent);
        UpdateAppearance(ent);
        UpdateUi(ent);
    }

    private void OnOutputPressureChangeMessage(Entity<GasPressurePumpComponent> ent, ref GasPressurePumpChangeOutputPressureMessage args)
    {
        ent.Comp.TargetPressure = Math.Clamp(args.Pressure, 0f, Atmospherics.MaxOutputPressure);
        _adminLogger.Add(LogType.AtmosPressureChanged,
            LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the pressure on {ToPrettyString(ent):device} to {args.Pressure}kPa");
        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnPumpLeaveAtmosphere(Entity<GasPressurePumpComponent> ent, ref AtmosDeviceDisabledEvent args)
    {
        ent.Comp.Enabled = false;
        Dirty(ent);
        UpdateAppearance(ent);

        UserInterfaceSystem.CloseUi(ent.Owner, GasPressurePumpUiKey.Key);
    }

    protected virtual void UpdateUi(Entity<GasPressurePumpComponent> ent)
    {
    }

    // Frontier - bidirectional pumps
    public void OnPumpSetDirectionMessage(Entity<GasPressurePumpComponent> ent, ref GasPressurePumpChangePumpDirectionMessage args)
    {
        if (!ent.Comp.SettableDirection || ent.Comp.PumpingInwards == args.Inwards)
            return;

        var temp = ent.Comp.OutletName;
        ent.Comp.OutletName = ent.Comp.InletName;
        ent.Comp.InletName = temp;

        ent.Comp.PumpingInwards = args.Inwards;
        _adminLogger.Add(LogType.AtmosDirectionChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the direction on {ToPrettyString(ent):device} to {(args.Inwards ? "in" : "out")}");
        Dirty(ent);
        UpdateAppearance(ent);
    }
    // End Frontier
}
