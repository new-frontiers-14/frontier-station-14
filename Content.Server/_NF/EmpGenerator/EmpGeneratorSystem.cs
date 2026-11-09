using Content.Server.Emp;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._NF.BindToStation;
using Content.Shared._NF.EmpGenerator;
using Robust.Server.GameObjects;

namespace Content.Server._NF.EmpGenerator;

public sealed class EmpGeneratorSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpGeneratorComponent, PowerChargeActionEvent>(OnAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<EmpGeneratorComponent, PowerChargeComponent>();
        while (query.MoveNext(out var uid, out var grav, out var charge))
        {
            if (!_lights.TryGetLight(uid, out var pointLight))
                continue;

            _lights.SetEnabled(uid, charge.Charge > 0, pointLight);
            _lights.SetRadius(uid, MathHelper.Lerp(grav.LightRadiusMin, grav.LightRadiusMax, charge.Charge),
                pointLight);
        }
    }

    private void OnAction(Entity<EmpGeneratorComponent> ent, ref PowerChargeActionEvent args)
    {
        if (TryComp<StationBoundObjectComponent>(ent, out var stationBound)
            && _station.GetOwningStation(ent) != stationBound.BoundStation)
            return;

        if (!TryComp(ent, out TransformComponent? xform))
            return;

        List<EntityUid>? immuneGridList = null;
        if (xform.GridUid != null)
            immuneGridList = [xform.GridUid.Value];

        _emp.EmpPulse(_transform.ToMapCoordinates(xform.Coordinates), ent.Comp.Range, ent.Comp.EnergyConsumption, ent.Comp.DisableDuration, immuneGrids: immuneGridList);
    }
}
