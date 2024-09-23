<<<<<<< HEAD
using Content.Server.Administration.Logs;
using Content.Server.Audio;
using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Server.Emp; // Frontier: Upstream - #28984
using Content.Shared.Database;
=======
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
>>>>>>> a7e29f2878a63d62c9c23326e2b8f2dc64d40cc4
using Content.Shared.Gravity;

namespace Content.Server.Gravity;

public sealed class GravityGeneratorSystem : EntitySystem
{
    [Dependency] private readonly GravitySystem _gravitySystem = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravityGeneratorComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineActivatedEvent>(OnActivated);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineDeactivatedEvent>(OnDeactivated);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<GravityGeneratorComponent, PowerChargeComponent>();
        while (query.MoveNext(out var uid, out var grav, out var charge))
        {
            if (!_lights.TryGetLight(uid, out var pointLight))
                continue;

<<<<<<< HEAD
            SubscribeLocalEvent<GravityGeneratorComponent, ComponentInit>(OnCompInit);
            SubscribeLocalEvent<GravityGeneratorComponent, ComponentShutdown>(OnComponentShutdown);
            SubscribeLocalEvent<GravityGeneratorComponent, EntParentChangedMessage>(OnParentChanged); // Or just anchor changed?
            SubscribeLocalEvent<GravityGeneratorComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<GravityGeneratorComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<GravityGeneratorComponent, SharedGravityGeneratorComponent.SwitchGeneratorMessage>(
                OnSwitchGenerator);

            SubscribeLocalEvent<GravityGeneratorComponent, EmpPulseEvent>(OnEmpPulse); // Frontier: Upstream - #28984
=======
            _lights.SetEnabled(uid, charge.Charge > 0, pointLight);
            _lights.SetRadius(uid, MathHelper.Lerp(grav.LightRadiusMin, grav.LightRadiusMax, charge.Charge),
                pointLight);
>>>>>>> a7e29f2878a63d62c9c23326e2b8f2dc64d40cc4
        }
    }

    private void OnActivated(Entity<GravityGeneratorComponent> ent, ref ChargedMachineActivatedEvent args)
    {
        ent.Comp.GravityActive = true;

        var xform = Transform(ent);

        if (TryComp(xform.ParentUid, out GravityComponent? gravity))
        {
            _gravitySystem.EnableGravity(xform.ParentUid, gravity);
        }
    }

    private void OnDeactivated(Entity<GravityGeneratorComponent> ent, ref ChargedMachineDeactivatedEvent args)
    {
        ent.Comp.GravityActive = false;

        var xform = Transform(ent);

        if (TryComp(xform.ParentUid, out GravityComponent? gravity))
        {
            _gravitySystem.RefreshGravity(xform.ParentUid, gravity);
        }
    }

    private void OnParentChanged(EntityUid uid, GravityGeneratorComponent component, ref EntParentChangedMessage args)
    {
        if (component.GravityActive && TryComp(args.OldParent, out GravityComponent? gravity))
        {
<<<<<<< HEAD
            base.Update(frameTime);

            var query = EntityQueryEnumerator<GravityGeneratorComponent, ApcPowerReceiverComponent>();
            while (query.MoveNext(out var uid, out var gravGen, out var powerReceiver))
            {
                var ent = (uid, gravGen, powerReceiver);
                if (!gravGen.Intact)
                    continue;

                // Calculate charge rate based on power state and such.
                // Negative charge rate means discharging.
                float chargeRate;
                if (gravGen.SwitchedOn)
                {
                    if (powerReceiver.Powered)
                    {
                        chargeRate = gravGen.ChargeRate;
                    }
                    else
                    {
                        // Scale discharge rate such that if we're at 25% active power we discharge at 75% rate.
                        var receiving = powerReceiver.PowerReceived;
                        var mainSystemPower = Math.Max(0, receiving - gravGen.IdlePowerUse);
                        var ratio = 1 - mainSystemPower / (gravGen.ActivePowerUse - gravGen.IdlePowerUse);
                        chargeRate = -(ratio * gravGen.ChargeRate);
                    }
                }
                else
                {
                    chargeRate = -gravGen.ChargeRate;
                }

                var active = gravGen.GravityActive;
                var lastCharge = gravGen.Charge;
                gravGen.Charge = Math.Clamp(gravGen.Charge + frameTime * chargeRate, 0, gravGen.MaxCharge);
                if (chargeRate > 0)
                {
                    // Charging.
                    if (MathHelper.CloseTo(gravGen.Charge, gravGen.MaxCharge) && !gravGen.GravityActive)
                    {
                        gravGen.GravityActive = true;
                    }
                }
                else
                {
                    // Discharging
                    if (MathHelper.CloseTo(gravGen.Charge, 0) && gravGen.GravityActive)
                    {
                        gravGen.GravityActive = false;
                    }
                }

                var updateUI = gravGen.NeedUIUpdate;
                if (!MathHelper.CloseTo(lastCharge, gravGen.Charge))
                {
                    UpdateState(ent);
                    updateUI = true;
                }

                if (updateUI)
                    UpdateUI(ent, chargeRate);

                if (active != gravGen.GravityActive &&
                    TryComp(uid, out TransformComponent? xform) &&
                    TryComp<GravityComponent>(xform.ParentUid, out var gravity))
                {
                    // Force it on in the faster path.
                    if (gravGen.GravityActive)
                    {
                        _gravitySystem.EnableGravity(xform.ParentUid, gravity);
                    }
                    else
                    {
                        _gravitySystem.RefreshGravity(xform.ParentUid, gravity);
                    }
                }
            }
        }

        private void SetSwitchedOn(EntityUid uid, GravityGeneratorComponent component, bool on,
            ApcPowerReceiverComponent? powerReceiver = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref powerReceiver))
                return;

            if (user != null)
                _adminLogger.Add(LogType.Action, on ? LogImpact.Medium : LogImpact.High, $"{ToPrettyString(user)} set ${ToPrettyString(uid):target} to {(on ? "on" : "off")}");

            component.SwitchedOn = on;
            UpdatePowerState(component, powerReceiver);
            component.NeedUIUpdate = true;
        }

        private static void UpdatePowerState(
            GravityGeneratorComponent component,
            ApcPowerReceiverComponent powerReceiver)
        {
            powerReceiver.Load = component.SwitchedOn ? component.ActivePowerUse : component.IdlePowerUse;
        }

        private void UpdateUI(Entity<GravityGeneratorComponent, ApcPowerReceiverComponent> ent, float chargeRate)
        {
            var (_, component, powerReceiver) = ent;
            if (!_uiSystem.IsUiOpen(ent.Owner, SharedGravityGeneratorComponent.GravityGeneratorUiKey.Key))
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
                > 0 when atTarget => GravityGeneratorPowerStatus.FullyCharged,
                < 0 when atTarget => GravityGeneratorPowerStatus.Off,
                > 0 => GravityGeneratorPowerStatus.Charging,
                < 0 => GravityGeneratorPowerStatus.Discharging,
                _ => throw new ArgumentOutOfRangeException()
            };

            var state = new SharedGravityGeneratorComponent.GeneratorState(
                component.SwitchedOn,
                (byte) (component.Charge * 255),
                status,
                (short) Math.Round(powerReceiver.PowerReceived),
                (short) Math.Round(powerReceiver.Load),
                chargeEta
            );

            _uiSystem.SetUiState(
                ent.Owner,
                SharedGravityGeneratorComponent.GravityGeneratorUiKey.Key,
                state);

            component.NeedUIUpdate = false;
        }

        private void OnCompInit(Entity<GravityGeneratorComponent> ent, ref ComponentInit args)
        {
            ApcPowerReceiverComponent? powerReceiver = null;
            if (!Resolve(ent, ref powerReceiver, false))
                return;

            UpdatePowerState(ent, powerReceiver);
            UpdateState((ent, ent.Comp, powerReceiver));
        }

        private void OnInteractHand(EntityUid uid, GravityGeneratorComponent component, InteractHandEvent args)
        {
            ApcPowerReceiverComponent? powerReceiver = default!;
            if (!Resolve(uid, ref powerReceiver))
                return;

            // Do not allow opening UI if broken or unpowered.
            if (!component.Intact || powerReceiver.PowerReceived < component.IdlePowerUse)
                return;

            _uiSystem.OpenUi(uid, SharedGravityGeneratorComponent.GravityGeneratorUiKey.Key, args.User);
            component.NeedUIUpdate = true;
        }

        public void UpdateState(Entity<GravityGeneratorComponent, ApcPowerReceiverComponent> ent)
        {
            var (uid, grav, powerReceiver) = ent;
            var appearance = EntityManager.GetComponentOrNull<AppearanceComponent>(uid);
            _appearance.SetData(uid, GravityGeneratorVisuals.Charge, grav.Charge, appearance);

            if (_lights.TryGetLight(uid, out var pointLight))
            {
                _lights.SetEnabled(uid, grav.Charge > 0, pointLight);
                _lights.SetRadius(uid, MathHelper.Lerp(grav.LightRadiusMin, grav.LightRadiusMax, grav.Charge), pointLight);
            }

            if (!grav.Intact)
            {
                MakeBroken((uid, grav), appearance);
            }
            else if (powerReceiver.PowerReceived < grav.IdlePowerUse)
            {
                MakeUnpowered((uid, grav), appearance);
            }
            else if (!grav.SwitchedOn)
            {
                MakeOff((uid, grav), appearance);
            }
            else
            {
                MakeOn((uid, grav), appearance);
            }
        }

        private void OnRefreshParts(EntityUid uid, GravityGeneratorComponent component, RefreshPartsEvent args)
        {
            var maxChargeMultipler = args.PartRatings[component.MachinePartMaxChargeMultiplier];
            component.MaxCharge = maxChargeMultipler * 1;
        }

        private void MakeBroken(Entity<GravityGeneratorComponent> ent, AppearanceComponent? appearance)
        {
            _ambientSoundSystem.SetAmbience(ent, false);

            _appearance.SetData(ent, GravityGeneratorVisuals.State, GravityGeneratorStatus.Broken);
        }

        private void MakeUnpowered(Entity<GravityGeneratorComponent> ent, AppearanceComponent? appearance)
        {
            _ambientSoundSystem.SetAmbience(ent, false);

            _appearance.SetData(ent, GravityGeneratorVisuals.State, GravityGeneratorStatus.Unpowered, appearance);
        }

        private void MakeOff(Entity<GravityGeneratorComponent> ent, AppearanceComponent? appearance)
        {
            _ambientSoundSystem.SetAmbience(ent, false);

            _appearance.SetData(ent, GravityGeneratorVisuals.State, GravityGeneratorStatus.Off, appearance);
        }

        private void MakeOn(Entity<GravityGeneratorComponent> ent, AppearanceComponent? appearance)
        {
            _ambientSoundSystem.SetAmbience(ent, true);

            _appearance.SetData(ent, GravityGeneratorVisuals.State, GravityGeneratorStatus.On, appearance);
        }

        private void OnSwitchGenerator(
            EntityUid uid,
            GravityGeneratorComponent component,
            SharedGravityGeneratorComponent.SwitchGeneratorMessage args)
        {
            SetSwitchedOn(uid, component, args.On, user: args.Actor);
=======
            _gravitySystem.RefreshGravity(args.OldParent.Value, gravity);
>>>>>>> a7e29f2878a63d62c9c23326e2b8f2dc64d40cc4
        }

        private void OnEmpPulse(EntityUid uid, GravityGeneratorComponent component, EmpPulseEvent args) // Frontier: Upstream - #28984
        {
            /// i really don't think that the gravity generator should use normalised 0-1 charge
            /// as opposed to watts charge that every other battery uses

            if (!TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver))
                return;

            var ent = (uid, component, powerReceiver);

            // convert from normalised energy to watts and subtract
            float maxEnergy = component.ActivePowerUse / component.ChargeRate;
            float currentEnergy = maxEnergy * component.Charge;
            currentEnergy = Math.Max(0, currentEnergy - args.EnergyConsumption);

            // apply renormalised energy to charge variable
            component.Charge = currentEnergy / maxEnergy;

            // update power state
            UpdateState(ent);
        }
    }
}
