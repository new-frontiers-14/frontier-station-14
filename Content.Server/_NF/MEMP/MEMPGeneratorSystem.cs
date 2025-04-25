using Content.Server.Emp;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Popups;
using Robust.Server.GameObjects;

namespace Content.Server._NF.MEMP;

public sealed class MEMPGeneratorSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MEMPGeneratorComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<MEMPGeneratorComponent, ChargedMachineActivatedEvent>(OnActivated);
        SubscribeLocalEvent<MEMPGeneratorComponent, ChargedMachineDeactivatedEvent>(OnDeactivated);
        SubscribeLocalEvent<MEMPGeneratorComponent, ActionEvent>(OnAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MEMPGeneratorComponent, PowerChargeComponent>();
        while (query.MoveNext(out var uid, out var grav, out var charge))
        {
            if (!_lights.TryGetLight(uid, out var pointLight))
                continue;

            _lights.SetEnabled(uid, charge.Charge > 0, pointLight);
            _lights.SetRadius(uid, MathHelper.Lerp(grav.LightRadiusMin, grav.LightRadiusMax, charge.Charge),
                pointLight);
        }
    }

    private void OnActivated(Entity<MEMPGeneratorComponent> ent, ref ChargedMachineActivatedEvent args)
    {
        ent.Comp.MEMPActive = true;

        var xform = Transform(ent);

        //EmpPulse(_transform.GetMapCoordinates(uid), comp.Range, comp.EnergyConsumption, comp.DisableDuration);

        //if (TryComp(xform.ParentUid, out MEMPComponent? gravity))
        //{
        //    _gravitySystem.EnableMEMP(xform.ParentUid, gravity);
        //}
    }

    private void OnDeactivated(Entity<MEMPGeneratorComponent> ent, ref ChargedMachineDeactivatedEvent args)
    {
        ent.Comp.MEMPActive = false;

        var xform = Transform(ent);

        //if (TryComp(xform.ParentUid, out MEMPComponent? gravity))
        //{
        //    _gravitySystem.RefreshMEMP(xform.ParentUid, gravity);
        //}
    }

    private void OnAction(Entity<MEMPGeneratorComponent> ent, ref ActionEvent args)
    {
        ent.Comp.MEMPActionLocked = true;

        var xform = Transform(ent);

        _emp.EmpPulse(_transform.ToMapCoordinates(xform.Coordinates), ent.Comp.Range, ent.Comp.EnergyConsumption, ent.Comp.DisableDuration);


        if (!TryComp<ApcPowerReceiverComponent>(ent.Owner, out var powerReceiver))
            return;

        if (!TryComp<PowerChargeComponent>(ent.Owner, out var powerCharge))
            return;

        // convert from normalised energy to watts and subtract
        float maxEnergy = powerCharge.ActivePowerUse / powerCharge.ChargeRate;
        float currentEnergy = maxEnergy * powerCharge.Charge;
        currentEnergy = Math.Max(0, currentEnergy - ent.Comp.EnergyConsumption);

        // apply renormalised energy to charge variable
        powerCharge.Charge = currentEnergy / maxEnergy;

        //if (TryComp(xform.ParentUid, out MEMPComponent? gravity))
        //{
        //    _gravitySystem.EnableMEMP(xform.ParentUid, gravity);
        //}
    }

    private void OnParentChanged(EntityUid uid, MEMPGeneratorComponent component, ref EntParentChangedMessage args)
    {
        //if (component.MEMPActive && TryComp(args.OldParent, out MEMPComponent? gravity))
        //{
        //    _gravitySystem.RefreshMEMP(args.OldParent.Value, gravity);
        //}
    }
}
