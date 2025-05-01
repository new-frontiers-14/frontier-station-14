using Content.Server.Administration.Logs;
using Content.Server.Audio;
using Content.Server.Emp;
using Content.Server.Power.Components;
using Content.Shared.Database;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Power.EntitySystems;

public sealed class PowerChargeSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSoundSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerChargeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PowerChargeComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<PowerChargeComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<PowerChargeComponent, AfterActivatableUIOpenEvent>(OnAfterUiOpened);
        SubscribeLocalEvent<PowerChargeComponent, AnchorStateChangedEvent>(OnAnchorStateChange);

        // This needs to be ui key agnostic
        SubscribeLocalEvent<PowerChargeComponent, SwitchChargingMachineMessage>(OnSwitchGenerator);

        SubscribeLocalEvent<PowerChargeComponent, EmpPulseEvent>(OnEmpPulse); // Frontier: emp code
        SubscribeLocalEvent<PowerChargeComponent, PowerChargeActionMessage>(OnActionAttempt); // Frontier
    }

    private void OnAnchorStateChange(EntityUid uid, PowerChargeComponent component, AnchorStateChangedEvent args)
    {
        if (args.Anchored || !TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiverComponent))
            return;

        component.Active = false;
        component.Charge = 0;
        UpdateState(new Entity<PowerChargeComponent, ApcPowerReceiverComponent>(uid, component, powerReceiverComponent));
    }

    private void OnAfterUiOpened(EntityUid uid, PowerChargeComponent component, AfterActivatableUIOpenEvent args)
    {
        if (!TryComp<ApcPowerReceiverComponent>(uid, out var apcPowerReceiver))
            return;

        UpdateUI((uid, component, apcPowerReceiver), component.ChargeRate);
    }

    private void OnSwitchGenerator(EntityUid uid, PowerChargeComponent component, SwitchChargingMachineMessage args)
    {
        SetSwitchedOn(uid, component, args.On, user: args.Actor);
    }

    private void OnUIOpenAttempt(EntityUid uid, PowerChargeComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (!component.Intact)
            args.Cancel();
    }

    private void OnComponentShutdown(EntityUid uid, PowerChargeComponent component, ComponentShutdown args)
    {
        if (!component.Active)
            return;

        component.Active = false;

        var eventArgs = new ChargedMachineDeactivatedEvent();
        RaiseLocalEvent(uid, ref eventArgs);
    }

    private void OnMapInit(Entity<PowerChargeComponent> ent, ref MapInitEvent args)
    {
        ApcPowerReceiverComponent? powerReceiver = null;
        if (!Resolve(ent, ref powerReceiver, false))
            return;

        UpdatePowerState(ent, powerReceiver);
        UpdateState((ent, ent.Comp, powerReceiver));
    }

    public void SetSwitchedOn(EntityUid uid, PowerChargeComponent component, bool on,  // Frontier: private<public for linking system in StationAnchorSystem.
        ApcPowerReceiverComponent? powerReceiver = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref powerReceiver))
            return;

        if (user is { } )
            _adminLogger.Add(LogType.Action, on ? LogImpact.Medium : LogImpact.High, $"{ToPrettyString(user):player} set ${ToPrettyString(uid):target} to {(on ? "on" : "off")}");

        component.SwitchedOn = on;
        UpdatePowerState(component, powerReceiver);
        component.NeedUIUpdate = true;
    }

    // Frontier: Added action option
    private void OnActionAttempt(EntityUid uid, PowerChargeComponent component, PowerChargeActionMessage args)
    {
        OnAction(uid, component, user: args.Actor);
    }

    private void OnAction(EntityUid uid, PowerChargeComponent component,
    ApcPowerReceiverComponent? powerReceiver = null, EntityUid? user = null)
    {
        if (component.Charge < component.ActionCharge)
            return;

        if (!Resolve(uid, ref powerReceiver))
            return;

        if (user is { })
            _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(user):player} set ${ToPrettyString(uid):target}");

        var eventActionArgs = new PowerChargeActionEvent();
        RaiseLocalEvent(uid, ref eventActionArgs);

        if (component.ActionCharge > 0)
        {
            component.Charge -= component.ActionCharge;
            component.Active = false;
            var eventDeactivatedArgs = new ChargedMachineDeactivatedEvent();
            RaiseLocalEvent(uid, ref eventDeactivatedArgs);
            component.NeedUIUpdate = true;
        }
    }
    // Frontier End

    private static void UpdatePowerState(PowerChargeComponent component, ApcPowerReceiverComponent powerReceiver)
    {
        // Frontier: update power state
        if (component.SwitchedOn)
            powerReceiver.Load = component.MaxCharge == component.Charge ? component.ActivePowerUse : component.ActiveChargingPowerUse;
        else
            powerReceiver.Load = component.IdlePowerUse;
        // End Frontier: update power state
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PowerChargeComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var chargingMachine, out var powerReceiver))
        {
            var ent = (uid, gravGen: chargingMachine, powerReceiver);
            if (!chargingMachine.Intact)
                continue;

            // Calculate charge rate based on power state and such.
            // Negative charge rate means discharging.
            float chargeRate;
            if (chargingMachine.SwitchedOn)
            {
                if (powerReceiver.Powered)
                {
                    chargeRate = chargingMachine.ChargeRate;
                }
                else
                {
                    // Scale discharge rate such that if we're at 25% active power we discharge at 75% rate.
                    var receiving = powerReceiver.PowerReceived;
                    var mainSystemPower = Math.Max(0, receiving - chargingMachine.IdlePowerUse);
                    var ratio = 1 - mainSystemPower / (chargingMachine.ActiveChargingPowerUse - chargingMachine.IdlePowerUse); // Frontier: ActivePowerUse<ActiveChargingPowerUse
                    chargeRate = -(ratio * chargingMachine.ChargeRate);
                }
            }
            else
            {
                chargeRate = -chargingMachine.ChargeRate;
            }

            var active = chargingMachine.Active;
            var lastCharge = chargingMachine.Charge;
            chargingMachine.Charge = Math.Clamp(chargingMachine.Charge + frameTime * chargeRate, 0, chargingMachine.MaxCharge);
            if (chargeRate > 0)
            {
                // Charging.
                if (MathHelper.CloseTo(chargingMachine.Charge, chargingMachine.MaxCharge) && !chargingMachine.Active)
                {
                    chargingMachine.Active = true;
                }
            }
            else
            {
                // Discharging
                if (MathHelper.CloseTo(chargingMachine.Charge, 0) && chargingMachine.Active)
                {
                    chargingMachine.Active = false;
                }
            }

            // Frontier: changing load when full
            var oldLoad = powerReceiver.Load;
            UpdatePowerState(chargingMachine, powerReceiver);
            if (oldLoad != powerReceiver.Load)
                chargingMachine.NeedUIUpdate = true;
            // End Frontier

            var updateUI = chargingMachine.NeedUIUpdate;
            if (!MathHelper.CloseTo(lastCharge, chargingMachine.Charge))
            {
                UpdateState(ent);
                updateUI = true;
            }

            if (updateUI)
                UpdateUI(ent, chargeRate);

            if (active == chargingMachine.Active)
                continue;

            if (chargingMachine.Active)
            {
                var eventArgs = new ChargedMachineActivatedEvent();
                RaiseLocalEvent(uid, ref eventArgs);
            }
            else
            {
                var eventArgs = new ChargedMachineDeactivatedEvent();
                RaiseLocalEvent(uid, ref eventArgs);
            }
        }
    }

    private void UpdateUI(Entity<PowerChargeComponent, ApcPowerReceiverComponent> ent, float chargeRate)
    {
        var (_, component, powerReceiver) = ent;
        if (!_uiSystem.IsUiOpen(ent.Owner, component.UiKey))
            return;

        var chargeTarget = chargeRate < 0 ? 0 : component.MaxCharge;
        short chargeEta;
        var atTarget = false;
        if (MathHelper.CloseTo(component.Charge, chargeTarget))
        {
            chargeEta = short.MinValue; // N/A
            atTarget = true;
        }
        else
        {
            var diff = chargeTarget - component.Charge;
            chargeEta = (short) Math.Abs(diff / chargeRate);
        }

        var status = chargeRate switch
        {
            > 0 when atTarget => PowerChargePowerStatus.FullyCharged,
            < 0 when atTarget => PowerChargePowerStatus.Off,
            > 0 => PowerChargePowerStatus.Charging,
            < 0 => PowerChargePowerStatus.Discharging,
            _ => throw new ArgumentOutOfRangeException()
        };

        var state = new PowerChargeState(
            component.SwitchedOn,
            component.Charge >= component.ActionCharge, // Frontier
            (byte) (component.Charge * 255),
            status,
            (short) Math.Round(powerReceiver.PowerReceived),
            (short) Math.Round(powerReceiver.Load),
            chargeEta
        );

        _uiSystem.SetUiState(
            ent.Owner,
            component.UiKey,
            state);

        component.NeedUIUpdate = false;
    }

    private void UpdateState(Entity<PowerChargeComponent, ApcPowerReceiverComponent> ent)
    {
        var (uid, machine, powerReceiver) = ent;
        var appearance = EntityManager.GetComponentOrNull<AppearanceComponent>(uid);
        _appearance.SetData(uid, PowerChargeVisuals.Charge, machine.Charge, appearance);
        _appearance.SetData(uid, PowerChargeVisuals.Active, machine.Active);


        if (!machine.Intact)
        {
            MakeBroken((uid, machine), appearance);
        }
        else if (powerReceiver.PowerReceived < machine.IdlePowerUse)
        {
            MakeUnpowered((uid, machine), appearance);
        }
        else if (!machine.SwitchedOn)
        {
            MakeOff((uid, machine), appearance);
        }
        else
        {
            MakeOn((uid, machine), appearance);
        }
    }

    private void MakeBroken(Entity<PowerChargeComponent> ent, AppearanceComponent? appearance)
    {
        _ambientSoundSystem.SetAmbience(ent, false);

        _appearance.SetData(ent, PowerChargeVisuals.State, PowerChargeStatus.Broken, appearance);
    }

    private void MakeUnpowered(Entity<PowerChargeComponent> ent, AppearanceComponent? appearance)
    {
        _ambientSoundSystem.SetAmbience(ent, false);

        _appearance.SetData(ent, PowerChargeVisuals.State, PowerChargeStatus.Unpowered, appearance);
    }

    private void MakeOff(Entity<PowerChargeComponent> ent, AppearanceComponent? appearance)
    {
        _ambientSoundSystem.SetAmbience(ent, false);

        _appearance.SetData(ent, PowerChargeVisuals.State, PowerChargeStatus.Off, appearance);
    }

    private void MakeOn(Entity<PowerChargeComponent> ent, AppearanceComponent? appearance)
    {
        _ambientSoundSystem.SetAmbience(ent, true);

        _appearance.SetData(ent, PowerChargeVisuals.State, PowerChargeStatus.On, appearance);
    }

    // Frontier: EMP on charge system (Upstream - #28984, MIT)
    private void OnEmpPulse(Entity<PowerChargeComponent> ent, ref EmpPulseEvent args)
    {
        ent.Comp.Active = false;
        ent.Comp.Charge = 0;
        var eventDeactivatedArgs = new ChargedMachineDeactivatedEvent();
        RaiseLocalEvent(ent.Owner, ref eventDeactivatedArgs);

        ent.Comp.NeedUIUpdate = true;

        if (!TryComp<ApcPowerReceiverComponent>(ent.Owner, out var powerReceiver))
            return;

        // update power state
        UpdateState((ent.Owner, ent.Comp, powerReceiver));
    }
    // End Frontier
}

[ByRefEvent] public record struct ChargedMachineActivatedEvent;
[ByRefEvent] public record struct ChargedMachineDeactivatedEvent;
[ByRefEvent] public record struct PowerChargeActionEvent; // Frontier
