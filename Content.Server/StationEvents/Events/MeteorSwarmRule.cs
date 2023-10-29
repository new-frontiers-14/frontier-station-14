using System.Numerics;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Spawners;

namespace Content.Server.StationEvents.Events
{
    public sealed class MeteorSwarmRule : StationEventSystem<MeteorSwarmRuleComponent>
    {
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        protected override void Started(EntityUid uid, MeteorSwarmRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, component, gameRule, args);

            var mod = Math.Sqrt(GetSeverityModifier());
            component.WaveCounter = (int) (RobustRandom.Next(component.MinimumWaves, component.MaximumWaves) * mod);
        }

        protected override void ActiveTick(EntityUid uid, MeteorSwarmRuleComponent component, GameRuleComponent gameRule, float frameTime)
        {
            if (component.WaveCounter <= 0)
            {
                ForceEndSelf(uid, gameRule);
                return;
            }

            var mod = GetSeverityModifier();

            component.Cooldown -= frameTime;

            if (component.Cooldown > 0f)
                return;

            component.WaveCounter--;

            component.Cooldown += (component.MaximumCooldown - component.MinimumCooldown) * RobustRandom.NextFloat() / mod + component.MinimumCooldown;

            Box2? playableArea = null;
            var mapId = GameTicker.DefaultMap;

            var query = AllEntityQuery<MapGridComponent, TransformComponent>();
            while (query.MoveNext(out var gridId, out _, out var xform))
            {
                if (xform.MapID != mapId)
                    continue;

                var aabb = _physics.GetWorldAABB(gridId);
                playableArea = playableArea?.Union(aabb) ?? aabb;
            }

            if (playableArea == null)
            {
                ForceEndSelf(uid, gameRule);
                return;
            }

            var minimumDistance = (playableArea.Value.TopRight - playableArea.Value.Center).Length() + 50f;
            var maximumDistance = minimumDistance + 100f;

            var center = playableArea.Value.Center;

            for (var i = 0; i < component.MeteorsPerWave; i++)
            {
                var angle = new Angle(RobustRandom.NextFloat() * MathF.Tau);
                var offset = angle.RotateVec(new Vector2((maximumDistance - minimumDistance) * RobustRandom.NextFloat() + minimumDistance, 0));
                var spawnPosition = new MapCoordinates(center + offset, mapId);
                var meteor = Spawn("MeteorLarge", spawnPosition);
                var physics = EntityManager.GetComponent<PhysicsComponent>(meteor);
                _physics.SetBodyStatus(physics, BodyStatus.InAir);
                _physics.SetLinearDamping(physics, 0f);
                _physics.SetAngularDamping(physics, 0f);
                _physics.ApplyLinearImpulse(meteor, -offset.Normalized() * component.MeteorVelocity * physics.Mass, body: physics);
                _physics.ApplyAngularImpulse(
                    meteor,
                    physics.Mass * ((component.MaxAngularVelocity - component.MinAngularVelocity) * RobustRandom.NextFloat() + component.MinAngularVelocity),
                    body: physics);

                EnsureComp<TimedDespawnComponent>(meteor).Lifetime = 120f;
            }
        }
    }
}
