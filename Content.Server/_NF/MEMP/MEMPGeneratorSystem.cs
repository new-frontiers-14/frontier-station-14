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
        // Nothing need to be done here.
    }

    private void OnDeactivated(Entity<MEMPGeneratorComponent> ent, ref ChargedMachineDeactivatedEvent args)
    {
        // Nothing need to be done here.
    }

    private void OnAction(Entity<MEMPGeneratorComponent> ent, ref ActionEvent args)
    {
        var xform = Transform(ent);
        List<EntityUid>? immuneGridList = null;
        if (xform.GridUid != null)
        {
            immuneGridList = new List<EntityUid> {
                    xform.GridUid.Value
                };
        }

        _emp.EmpPulse(_transform.ToMapCoordinates(xform.Coordinates), ent.Comp.Range, ent.Comp.EnergyConsumption, ent.Comp.DisableDuration, immuneGrids: immuneGridList);
    }

    private void OnParentChanged(EntityUid uid, MEMPGeneratorComponent component, ref EntParentChangedMessage args)
    {
        // Nothing need to be done here.
    }
}
