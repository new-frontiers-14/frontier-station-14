using Content.Server.Cargo.Systems;
using Content.Server.Emp;
using Content.Shared.Emp; // Frontier: Upstream - #28984
using Content.Server.Power.Components;
using Content.Shared.Examine;
using Content.Shared.Rejuvenate;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    public sealed class BatterySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ExaminableBatteryComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<PowerNetworkBatteryComponent, RejuvenateEvent>(OnNetBatteryRejuvenate);
            SubscribeLocalEvent<BatteryComponent, RejuvenateEvent>(OnBatteryRejuvenate);
            SubscribeLocalEvent<BatteryComponent, PriceCalculationEvent>(CalculateBatteryPrice);
            SubscribeLocalEvent<BatteryComponent, EmpPulseEvent>(OnEmpPulse);
            SubscribeLocalEvent<BatteryComponent, EmpDisabledRemoved>(OnEmpDisabledRemoved); // Frontier: Upstream - #28984

            SubscribeLocalEvent<NetworkBatteryPreSync>(PreSync);
            SubscribeLocalEvent<NetworkBatteryPostSync>(PostSync);
        }

        private void OnNetBatteryRejuvenate(EntityUid uid, PowerNetworkBatteryComponent component, RejuvenateEvent args)
        {
            component.NetworkBattery.CurrentStorage = component.NetworkBattery.Capacity;
        }

        private void OnBatteryRejuvenate(EntityUid uid, BatteryComponent component, RejuvenateEvent args)
        {
            SetCharge(uid, component.MaxCharge, component);
        }

        private void OnExamine(EntityUid uid, ExaminableBatteryComponent component, ExaminedEvent args)
        {
            if (!TryComp<BatteryComponent>(uid, out var batteryComponent))
                return;
            if (args.IsInDetailsRange)
            {
                var effectiveMax = batteryComponent.MaxCharge;
                if (effectiveMax == 0)
                    effectiveMax = 1;
                var chargeFraction = batteryComponent.CurrentCharge / effectiveMax;
                var chargePercentRounded = (int) (chargeFraction * 100);
                args.PushMarkup(
                    Loc.GetString(
                        "examinable-battery-component-examine-detail",
                        ("percent", chargePercentRounded),
                        ("markupPercentColor", "green")
                    )
                );
            }
        }

        private void PreSync(NetworkBatteryPreSync ev)
        {
            // Ignoring entity pausing. If the entity was paused, neither component's data should have been changed.
            var enumerator = AllEntityQuery<PowerNetworkBatteryComponent, BatteryComponent>();
            while (enumerator.MoveNext(out var netBat, out var bat))
            {
                DebugTools.Assert(bat.CurrentCharge <= bat.MaxCharge && bat.CurrentCharge >= 0);
                netBat.NetworkBattery.Capacity = bat.MaxCharge;
                netBat.NetworkBattery.CurrentStorage = bat.CurrentCharge;
            }
        }

        private void PostSync(NetworkBatteryPostSync ev)
        {
            // Ignoring entity pausing. If the entity was paused, neither component's data should have been changed.
            var enumerator = AllEntityQuery<PowerNetworkBatteryComponent, BatteryComponent>();
            while (enumerator.MoveNext(out var uid, out var netBat, out var bat))
            {
                SetCharge(uid, netBat.NetworkBattery.CurrentStorage, bat);
            }
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<BatterySelfRechargerComponent, BatteryComponent>();
            while (query.MoveNext(out var uid, out var comp, out var batt))
            {
                if (!comp.AutoRecharge) continue;
                if (batt.IsFullyCharged) continue;
                TrySetCharge(uid, batt.CurrentCharge + comp.AutoRechargeRate * frameTime, batt); // Frontier: Upstream - #28984
            }
        }

        /// <summary>
        /// Gets the price for the power contained in an entity's battery.
        /// </summary>
        private void CalculateBatteryPrice(EntityUid uid, BatteryComponent component, ref PriceCalculationEvent args)
        {
            args.Price += component.CurrentCharge * component.PricePerJoule;
        }

        private void OnEmpPulse(EntityUid uid, BatteryComponent component, ref EmpPulseEvent args)
        {
            args.Affected = true;
            args.Disabled = true; // Frontier: Upstream - #28984
            UseCharge(uid, args.EnergyConsumption, component);
        }

        /// <summary>
        /// if a disabled battery is put into a recharged, allow the recharger to start recharging again after the disable ends.
        /// </summary>
        private void OnEmpDisabledRemoved(EntityUid uid, BatteryComponent component, ref EmpDisabledRemoved args) // Frontier: Upstream - #28984
        {
            if (!TryComp<ChargingComponent>(uid, out var charging))
                return;

            var ev = new ChargerUpdateStatusEvent();
            RaiseLocalEvent(charging.ChargerUid, ref ev);
        }

        public float UseCharge(EntityUid uid, float value, BatteryComponent? battery = null)
        {
            if (value <= 0 ||  !Resolve(uid, ref battery) || battery.CurrentCharge == 0)
                return 0;

            var newValue = Math.Clamp(0, battery.CurrentCharge - value, battery.MaxCharge);
            var delta = newValue - battery.CurrentCharge;
            battery.CurrentCharge = newValue;
            var ev = new ChargeChangedEvent(battery.CurrentCharge, battery.MaxCharge);
            RaiseLocalEvent(uid, ref ev);
            return delta;
        }

        public void SetMaxCharge(EntityUid uid, float value, BatteryComponent? battery = null)
        {
            if (!Resolve(uid, ref battery))
                return;

            var old = battery.MaxCharge;
            battery.MaxCharge = Math.Max(value, 0);
            battery.CurrentCharge = Math.Min(battery.CurrentCharge, battery.MaxCharge);
            if (MathHelper.CloseTo(battery.MaxCharge, old))
                return;

            var ev = new ChargeChangedEvent(battery.CurrentCharge, battery.MaxCharge);
            RaiseLocalEvent(uid, ref ev);
        }

        public void SetCharge(EntityUid uid, float value, BatteryComponent? battery = null)
        {
            if (!Resolve(uid, ref battery))
                return;

            var old = battery.CurrentCharge;
            battery.CurrentCharge = MathHelper.Clamp(value, 0, battery.MaxCharge);
            if (MathHelper.CloseTo(battery.CurrentCharge, old))
                return;

            var ev = new ChargeChangedEvent(battery.CurrentCharge, battery.MaxCharge);
            RaiseLocalEvent(uid, ref ev);
        }

        /// <summary>
        ///     If sufficient charge is available on the battery, use it. Otherwise, don't.
        /// </summary>
        public bool TryUseCharge(EntityUid uid, float value, BatteryComponent? battery = null)
        {
            if (!Resolve(uid, ref battery, false) || value > battery.CurrentCharge)
                return false;

            UseCharge(uid, value, battery);
            return true;
        }

        /// <summary>
        ///     Like SetCharge, but checks for conditions like EmpDisabled before executing
        /// </summary>
        public bool TrySetCharge(EntityUid uid, float value, BatteryComponent? battery = null) // Frontier: Upstream - #28984
        {
            if (!Resolve(uid, ref battery, false) || HasComp<EmpDisabledComponent>(uid))
                return false;

            SetCharge(uid, value, battery);
            return true;
        }

        /// <summary>
        /// Returns whether the battery is at least 99% charged, basically full.
        /// </summary>
        public bool IsFull(EntityUid uid, BatteryComponent? battery = null)
        {
            if (!Resolve(uid, ref battery))
                return false;

            return battery.CurrentCharge / battery.MaxCharge >= 0.99f;
        }
    }
}
