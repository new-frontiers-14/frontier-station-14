using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;

namespace Content.Server._NF.MEMP;

public sealed class MEMPGeneratorSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MEMPGeneratorComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<MEMPGeneratorComponent, ChargedMachineActivatedEvent>(OnActivated);
        SubscribeLocalEvent<MEMPGeneratorComponent, ChargedMachineDeactivatedEvent>(OnDeactivated);
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

    private void OnParentChanged(EntityUid uid, MEMPGeneratorComponent component, ref EntParentChangedMessage args)
    {
        //if (component.MEMPActive && TryComp(args.OldParent, out MEMPComponent? gravity))
        //{
        //    _gravitySystem.RefreshMEMP(args.OldParent.Value, gravity);
        //}
    }
}
