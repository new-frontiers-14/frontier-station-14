using System.Numerics;
using Content.Server._NF.Roadkill.Components;
using Content.Server.Database;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Roadkill.Systems;

public sealed class RoadkillSystem : EntitySystem
{
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private readonly ProtoId<DamageTypePrototype> _bluntDamageType = "Blunt";
    private readonly FixedPoint2 _extraDamage = 20;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoadkillComponent, StartCollideEvent>(OnRoadkillCollide);
    }

    private void OnRoadkillCollide(Entity<RoadkillComponent> ent, ref StartCollideEvent args)
    {
        var ourXform = Transform(ent);
        var otherXform = Transform(args.OtherEntity);

        // Roadkill collision: roadkillable thing might not be on a grid (e.g. it flew in onto a lattice grid but slams into a wall at high speed)
        // but the thing it collides with should be on a grid (not space) and not be an item
        if (ourXform.MapUid == null
            || ourXform.MapUid != otherXform.MapUid
            || otherXform.GridUid == null
            || HasComp<ProjectileComponent>(args.OtherEntity)
            || HasComp<ItemComponent>(args.OtherEntity))
            return;

        var ourVelocity = _physics.GetMapLinearVelocity(ent, args.OurBody, ourXform);
        var otherVelocity = _physics.GetMapLinearVelocity(args.OtherEntity, args.OtherBody, otherXform);
        var jungleDiff = (ourVelocity - otherVelocity).Length();

        if (jungleDiff >= ent.Comp.DestroySpeed)
        {
            // Play audio following the colliding entity (presumably more stable for doppler than a static position)
            if (ent.Comp.DestroySound != null)
                _audio.PlayPvs(_audio.ResolveSound(ent.Comp.DestroySound), args.OtherEntity);
            QueueDel(ent);
        }
        else if (jungleDiff >= ent.Comp.KillSpeed)
        {
            if (_mobState.IsDead(ent))
                return;

            // Try to apply damage if this thing can take damage.
            if (_mobThreshold.TryGetThresholdForState(ent, MobState.Dead, out var threshold) &&
                TryComp<DamageableComponent>(ent, out var damageableComponent) &&
                damageableComponent.TotalDamage < threshold)
            {
                var damage = new DamageSpecifier();
                damage.DamageDict[_bluntDamageType] = threshold.Value - damageableComponent.TotalDamage + _extraDamage;
                _damageable.TryChangeDamage(ent, damage, ignoreResistances: true);
            }
            _mobState.ChangeMobState(ent, MobState.Dead);
        }
    }
}
