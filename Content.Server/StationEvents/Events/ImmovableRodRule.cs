using System.Numerics;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.ImmovableRod;
using Content.Server.StationEvents.Components;
using Content.Server.Weapons.Ranged.Systems;
using Robust.Shared.Spawners;
using Robust.Shared.Prototypes;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Server.StationEvents.Events;

public sealed class ImmovableRodRule : StationEventSystem<ImmovableRodRuleComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    protected override void Started(EntityUid uid, ImmovableRodRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var proto = _prototypeManager.Index<EntityPrototype>(component.RodPrototype);
        if (proto.TryGetComponent<ImmovableRodComponent>(out var rod) && proto.TryGetComponent<TimedDespawnComponent>(out var despawn))
        {
            TryFindRandomTile(out _, out _, out _, out var targetCoords);
            var speed = RobustRandom.NextFloat(rod.MinSpeed, rod.MaxSpeed);
            var angle = RobustRandom.NextAngle();
            var direction = angle.ToVec();
            var spawnCoords = targetCoords.ToMap(EntityManager, _transform).Offset(-direction * speed * despawn.Lifetime / 2);
            var ent = Spawn(component.RodPrototype, spawnCoords);
            _gun.ShootProjectile(ent, direction, Vector2.Zero, uid, speed: speed);
        }
        else
        {
            Sawmill.Error($"Invalid immovable rod prototype: {component.RodPrototype}");
        }
    }
}
