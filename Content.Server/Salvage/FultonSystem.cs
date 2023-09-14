using Content.Shared.Salvage.Fulton;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Salvage;

public sealed class FultonSystem : SharedFultonSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FultonedComponent, ComponentStartup>(OnFultonedStartup);
        SubscribeLocalEvent<FultonedComponent, ComponentShutdown>(OnFultonedShutdown);
    }

    private void OnFultonedShutdown(EntityUid uid, FultonedComponent component, ComponentShutdown args)
    {
        Del(component.Effect);
        component.Effect = EntityUid.Invalid;
    }

    private void OnFultonedStartup(EntityUid uid, FultonedComponent component, ComponentStartup args)
    {
        if (Exists(component.Effect))
            return;

        component.Effect = Spawn(EffectProto, new EntityCoordinates(uid, EffectOffset));
        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FultonedComponent>();
        var curTime = Timing.CurTime;

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextFulton > curTime)
                continue;

            Fulton(uid, comp);
        }
    }

    private void Fulton(EntityUid uid, FultonedComponent component)
    {
        if (!Deleted(component.Beacon) &&
            TryComp<TransformComponent>(component.Beacon, out var beaconXform) &&
            !_container.IsEntityOrParentInContainer(component.Beacon.Value, xform: beaconXform))
        {
            var xform = Transform(uid);
            var metadata = MetaData(uid);
            var oldCoords = xform.Coordinates;
            var offset = _random.NextVector2(1.5f);
            var localPos = TransformSystem.GetInvWorldMatrix(beaconXform.ParentUid)
                .Transform(TransformSystem.GetWorldPosition(beaconXform)) + offset;

            TransformSystem.SetCoordinates(uid, new EntityCoordinates(beaconXform.ParentUid, localPos));

            RaiseNetworkEvent(new FultonAnimationMessage()
            {
                Entity = GetNetEntity(uid, metadata),
                Coordinates = GetNetCoordinates(oldCoords),
            });
        }

        Audio.PlayPvs(component.Sound, uid);
        RemCompDeferred<FultonedComponent>(uid);
    }
}
