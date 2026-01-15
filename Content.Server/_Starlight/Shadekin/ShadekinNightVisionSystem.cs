using Content.Shared._Starlight.Shadekin;
using System.Linq;
using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Examine;
using Content.Shared.Tag;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Shadekin;

public sealed class ShadekinNightVisionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> ShadekinEyesTag = "ShadekinEyes";
    private static readonly ProtoId<TagPrototype> TheDarkTag = "TheDark";

    private sealed class LightCone
    {
        public float Direction { get; init; }
        public float InnerWidth { get; init; }
        public float OuterWidth { get; init; }
    }

    private readonly Dictionary<string, List<LightCone>> _lightMasks = new()
    {
        ["/Textures/Effects/LightMasks/cone.png"] = new List<LightCone>
        {
            new() { Direction = 0, InnerWidth = 30, OuterWidth = 60 }
        },
        ["/Textures/Effects/LightMasks/double_cone.png"] = new List<LightCone>
        {
            new() { Direction = 0, InnerWidth = 30, OuterWidth = 60 },
            new() { Direction = 180, InnerWidth = 30, OuterWidth = 60 }
        }
    };

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShadekinComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.NextUpdate)
                continue;

            component.NextUpdate = _timing.CurTime + component.UpdateCooldown;

            var lightExposure = 0f;
            if (!AreWeInTheDark(uid) && !_container.IsEntityInContainer(uid))
                lightExposure = GetLightExposure(uid);

            CheckThresholds(uid, component, lightExposure);
            UpdateAlert(uid, component, (short) component.CurrentState);

            ToggleNightVision(uid, component.CurrentState);
        }
    }

    private void UpdateAlert(EntityUid uid, ShadekinComponent component, short state)
    {
        _alerts.ShowAlert(uid, component.ShadekinAlert, state);
    }

    private void CheckThresholds(EntityUid uid, ShadekinComponent component, float lightExposure)
    {
        foreach (var (threshold, shadekinState) in component.Thresholds.Reverse())
        {
            var selectedState = shadekinState;
            if (lightExposure < threshold)
            {
                if (selectedState == ShadekinState.Low)
                    selectedState = ShadekinState.Dark;
                else
                    continue;
            }

            component.CurrentState = selectedState;
            Dirty(uid, component);
            break;
        }
    }

    private void ToggleNightVision(EntityUid uid, ShadekinState shadekinState)
    {
        if (!HasShadekinEyes(uid))
        {
            RemComp<NightVisionComponent>(uid);
            return;
        }

        if (shadekinState == ShadekinState.Dark)
            EnsureComp<NightVisionComponent>(uid);
        else
            RemComp<NightVisionComponent>(uid);
    }

    private bool HasShadekinEyes(EntityUid uid)
    {
        if (!TryComp<BodyComponent>(uid, out var body))
            return false;

        var organs = _bodySystem.GetBodyOrganEntityComps<OrganComponent>((uid, body));
        foreach (var organ in organs)
        {
            if (_tag.HasTag(organ.Owner, ShadekinEyesTag))
                return true;
        }

        return false;
    }

    private Angle GetAngle(EntityUid lightUid, SharedPointLightComponent lightComp, EntityUid targetUid)
    {
        var (lightPos, lightRot) = _transform.GetWorldPositionRotation(lightUid);
        lightPos += lightRot.RotateVec(lightComp.Offset);

        var (targetPos, _) = _transform.GetWorldPositionRotation(targetUid);
        var mapDiff = targetPos - lightPos;
        var oppositeMapDiff = (-lightRot).RotateVec(mapDiff);
        var angle = oppositeMapDiff.ToWorldAngle();

        if (angle == double.NaN && _transform.ContainsEntity(targetUid, lightUid) || _transform.ContainsEntity(lightUid, targetUid))
            angle = 0f;

        return angle;
    }

    private float GetLightExposure(EntityUid uid)
    {
        var illumination = 0f;

        var lightQuery = _lookup.GetEntitiesInRange<PointLightComponent>(Transform(uid).Coordinates, 20, LookupFlags.Uncontained);
        foreach (var light in lightQuery)
        {
            if (HasComp<DarkLightComponent>(light))
                continue;

            if (!light.Comp.Enabled || light.Comp.Radius < 1 || light.Comp.Energy <= 0)
                continue;

            var (lightPos, lightRot) = _transform.GetWorldPositionRotation(light);
            lightPos += lightRot.RotateVec(light.Comp.Offset);

            if (!_examine.InRangeUnOccluded(light, uid, light.Comp.Radius, null))
                continue;

            Transform(uid).Coordinates.TryDistance(EntityManager, Transform(light).Coordinates, out var dist);
            var denom = dist / light.Comp.Radius;
            var attenuation = 1 - (denom * denom);
            var calculatedLight = 0f;

            if (light.Comp.MaskPath is not null && _lightMasks.TryGetValue(light.Comp.MaskPath, out var cones))
            {
                var angleToTarget = GetAngle(light, light.Comp, uid);
                foreach (var cone in cones)
                {
                    var coneLight = 0f;
                    var angleAttenuation =
                        (float) Math.Min(Math.Max(cone.OuterWidth - angleToTarget, 0f), cone.InnerWidth) / cone.OuterWidth;

                    if (angleToTarget.Degrees - cone.Direction > cone.OuterWidth)
                        continue;
                    else if (angleToTarget.Degrees - cone.Direction > cone.InnerWidth
                             && angleToTarget.Degrees - cone.Direction < cone.OuterWidth)
                        coneLight = light.Comp.Energy * attenuation * attenuation * angleAttenuation;
                    else
                        coneLight = light.Comp.Energy * attenuation * attenuation;

                    calculatedLight = Math.Max(calculatedLight, coneLight);
                }
            }
            else
            {
                calculatedLight = light.Comp.Energy * attenuation * attenuation;
            }

            illumination += calculatedLight;
        }

        return illumination;
    }

    private bool AreWeInTheDark(EntityUid uid)
    {
        var mapUid = Transform(uid).MapUid;
        if (mapUid is not null && _tag.HasTag(mapUid.Value, TheDarkTag))
            return true;

        return false;
    }
}
