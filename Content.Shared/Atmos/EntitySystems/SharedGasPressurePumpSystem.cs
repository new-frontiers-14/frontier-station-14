using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared._NF.Atmos.Piping.Binary.Messages; // Frontier

namespace Content.Shared.Atmos.EntitySystems;

public abstract class SharedGasPressurePumpSystem : EntitySystem
{
    [Dependency] private   readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
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

    private void OnExamined(EntityUid uid, GasPressurePumpComponent pump, ExaminedEvent args)
    {
        if (!Transform(uid).Anchored)
            return;

        if (Loc.TryGetString("gas-pressure-pump-system-examined", out var str,
                ("statusColor", "lightblue"), // TODO: change with pressure?
                ("pressure", pump.TargetPressure)
            ))
        {
            args.PushMarkup(str);
        }
    }

    private void OnMapInit(EntityUid uid, GasPressurePumpComponent pump, MapInitEvent args) // Frontier - Init on map
    {
        if (pump.StartOnMapInit)
        {
            pump.Enabled = true;
        }
        UpdateAppearance(uid, pump);
    }

    private void OnInit(EntityUid uid, GasPressurePumpComponent pump, ComponentInit args)
    {
        UpdateAppearance(uid, pump);
    }

    private void OnPowerChanged(EntityUid uid, GasPressurePumpComponent component, ref PowerChangedEvent args)
    {
        UpdateAppearance(uid, component);
    }

    protected void UpdateAppearance(EntityUid uid, GasPressurePumpComponent? pump = null, AppearanceComponent? appearance = null) // Frontier: private<protected
    {
        if (!Resolve(uid, ref pump, ref appearance, false))
            return;

        var pumpOn = pump.Enabled && _receiver.IsPowered(uid);
        Appearance.SetData(uid, PumpVisuals.Enabled, pumpOn, appearance);
        Appearance.SetData(uid, PumpVisuals.PumpingInwards, pump.PumpingInwards, appearance); // Frontier
    }

    private void OnToggleStatusMessage(EntityUid uid, GasPressurePumpComponent pump, GasPressurePumpToggleStatusMessage args)
    {
        pump.Enabled = args.Enabled;
        _adminLogger.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(uid):device} to {args.Enabled}");
        Dirty(uid, pump);
        UpdateAppearance(uid, pump);
    }

    private void OnOutputPressureChangeMessage(EntityUid uid, GasPressurePumpComponent pump, GasPressurePumpChangeOutputPressureMessage args)
    {
        pump.TargetPressure = Math.Clamp(args.Pressure, 0f, Atmospherics.MaxOutputPressure);
        _adminLogger.Add(LogType.AtmosPressureChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the pressure on {ToPrettyString(uid):device} to {args.Pressure}kPa");
        Dirty(uid, pump);
    }

    private void OnPumpLeaveAtmosphere(EntityUid uid, GasPressurePumpComponent pump, ref AtmosDeviceDisabledEvent args)
    {
        pump.Enabled = false;
        Dirty(uid, pump);
        UpdateAppearance(uid, pump);

        UserInterfaceSystem.CloseUi(uid, GasPressurePumpUiKey.Key);
    }

    // Frontier - bidirectional pumps
    public void OnPumpSetDirectionMessage(EntityUid uid, GasPressurePumpComponent pump, GasPressurePumpChangePumpDirectionMessage args)
    {
        if (!pump.SettableDirection || pump.PumpingInwards == args.Inwards)
            return;

        var temp = pump.OutletName;
        pump.OutletName = pump.InletName;
        pump.InletName = temp;

        pump.PumpingInwards = args.Inwards;
        _adminLogger.Add(LogType.AtmosDirectionChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the direction on {ToPrettyString(uid):device} to {(args.Inwards ? "in" : "out")}");
        Dirty(uid, pump);
        UpdateAppearance(uid, pump);
    }
    // End Frontier
}
