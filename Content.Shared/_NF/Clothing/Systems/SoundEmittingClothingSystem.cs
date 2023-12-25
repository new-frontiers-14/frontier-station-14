using System.Numerics;
using Content.Shared._NF.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared._NF.Clothing.Systems;

public sealed class SoundEmittingClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _grid = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<InputMoverComponent> _moverQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        _moverQuery = GetEntityQuery<InputMoverComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<SoundEmittingClothingComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<SoundEmittingClothingComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(EntityUid uid, SoundEmittingClothingComponent component, GotEquippedEvent args)
    {
        var comp = EnsureComp<SoundEmittingEntityComponent>(args.Equipee);
        comp.SoundCollection = component.SoundCollection;
        comp.RequiresGravity = component.RequiresGravity;
    }

    private void OnUnequipped(EntityUid uid, SoundEmittingClothingComponent component, GotUnequippedEvent args)
    {
        if (TryComp<SoundEmittingEntityComponent>(args.Equipee, out var comp) &&
            comp.SoundCollection == component.SoundCollection)
        {
            RemComp<SoundEmittingEntityComponent>(args.Equipee);
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SoundEmittingEntityComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateSound(uid, comp);
        }
        query.Dispose();
    }

    private void UpdateSound(EntityUid uid, SoundEmittingEntityComponent component)
    {
        if (!_xformQuery.TryGetComponent(uid, out var xform) ||
            !_physicsQuery.TryGetComponent(uid, out var physics))
            return;

        if (!physics.Awake || physics.LinearVelocity.EqualsApprox(Vector2.Zero))
            return;

        // Space does not transmit sound
        if (xform.GridUid == null)
            return;

        if (component.RequiresGravity && _gravity.IsWeightless(uid, physics, xform))
            return;

        // The below is shamelessly copied from SharedMoverController
        var coordinates = xform.Coordinates;
        var distanceNeeded = (_moverQuery.TryGetComponent(uid, out var mover) && mover.Sprinting) ? 2f : 1.5f;

        if (!coordinates.TryDistance(EntityManager, component.LastPosition, out var distance) || distance > distanceNeeded)
            component.SoundDistance = distanceNeeded;
        else
            component.SoundDistance += distance;

        component.LastPosition = coordinates;
        if (component.SoundDistance < distanceNeeded)
            return;
        component.SoundDistance -= distanceNeeded;

        var sound = component.SoundCollection;
        var audioParams = sound.Params
            .WithVolume(sound.Params.Volume)
            .WithVariation(sound.Params.Variation ?? 0f);

        _audio.PlayPredicted(sound, uid, uid, audioParams);
    }
}
