using Content.Server.Administration.Logs;
using Content.Server.Damage.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Mobs.Components;
using Content.Shared.Throwing;
using Content.Shared.Wires;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server.Damage.Systems
{
    public sealed class DamageOtherOnHitSystem : EntitySystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly GunSystem _guns = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly DamageExamineSystem _damageExamine = default!;
        [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
        [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<DamageOtherOnHitComponent, ThrowDoHitEvent>(OnDoHit);
            SubscribeLocalEvent<DamageOtherOnHitComponent, DamageExamineEvent>(OnDamageExamine);
            SubscribeLocalEvent<DamageOtherOnHitComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow);
        }

        private void OnDoHit(EntityUid uid, DamageOtherOnHitComponent component, ThrowDoHitEvent args)
        {
            if (TerminatingOrDeleted(args.Target))
                return;

            // Frontier begin
            var damage = component.Damage;
            if (args.Component.Thrower is { } thrower)
            {
                var ev = new ApplyClothingDamageModifierEvent(thrower, DamageContext.Thrown, damage);
                RaiseLocalEvent(thrower, ref ev);
                damage = ev.Damage;
            }
            // Frontier end

            var dmg = _damageable.TryChangeDamage(args.Target, damage * _damageable.UniversalThrownDamageModifier, component.IgnoreResistances, origin: args.Component.Thrower); // Frontier component.Damage<damage

            // Log damage only for mobs. Useful for when people throw spears at each other, but also avoids log-spam when explosions send glass shards flying.
            if (dmg != null && HasComp<MobStateComponent>(args.Target))
                _adminLogger.Add(LogType.ThrowHit, $"{ToPrettyString(args.Target):target} received {dmg.GetTotal():damage} damage from collision");

            if (dmg is { Empty: false })
            {
                _color.RaiseEffect(Color.Red, new List<EntityUid>() { args.Target }, Filter.Pvs(args.Target, entityManager: EntityManager));
            }

            _guns.PlayImpactSound(args.Target, dmg, null, false);
            if (TryComp<PhysicsComponent>(uid, out var body) && body.LinearVelocity.LengthSquared() > 0f)
            {
                var direction = body.LinearVelocity.Normalized();
                _sharedCameraRecoil.KickCamera(args.Target, direction);
            }
        }

        private void OnDamageExamine(EntityUid uid, DamageOtherOnHitComponent component, ref DamageExamineEvent args)
        {
            // Frontier begin
            if (args.User is { } user)
            {
                var damage = component.Damage;
                var ev = new ApplyClothingDamageModifierEvent(user, DamageContext.Thrown, damage);
                RaiseLocalEvent(user, ref ev);
                damage = ev.Damage;

                _damageExamine.AddDamageExamine(args.Message, _damageable.ApplyUniversalAllModifiers(damage * _damageable.UniversalThrownDamageModifier), Loc.GetString("damage-throw"));
                return;
            }
            // Frontier end
            _damageExamine.AddDamageExamine(args.Message, _damageable.ApplyUniversalAllModifiers(component.Damage * _damageable.UniversalThrownDamageModifier), Loc.GetString("damage-throw"));
        }

        /// <summary>
        /// Prevent players with the Pacified status effect from throwing things that deal damage.
        /// </summary>
        private void OnAttemptPacifiedThrow(Entity<DamageOtherOnHitComponent> ent, ref AttemptPacifiedThrowEvent args)
        {
            args.Cancel("pacified-cannot-throw");
        }
    }
}
