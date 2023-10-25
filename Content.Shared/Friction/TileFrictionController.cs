using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Gravity;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Friction
{
    public sealed class TileFrictionController : VirtualController
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly SharedGravitySystem _gravity = default!;
        [Dependency] private readonly SharedMoverController _mover = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        private float _stopSpeed;
        private float _frictionModifier;
        public const float DefaultFriction = 0.3f;

        public override void Initialize()
        {
            base.Initialize();

            _configManager.OnValueChanged(CCVars.TileFrictionModifier, SetFrictionModifier, true);
            _configManager.OnValueChanged(CCVars.StopSpeed, SetStopSpeed, true);
        }

        private void SetStopSpeed(float value) => _stopSpeed = value;

        private void SetFrictionModifier(float value) => _frictionModifier = value;

        public override void Shutdown()
        {
            base.Shutdown();

            _configManager.UnsubValueChanged(CCVars.TileFrictionModifier, SetFrictionModifier);
            _configManager.UnsubValueChanged(CCVars.StopSpeed, SetStopSpeed);
        }

        public override void UpdateBeforeMapSolve(bool prediction, PhysicsMapComponent mapComponent, float frameTime)
        {
            base.UpdateBeforeMapSolve(prediction, mapComponent, frameTime);

            var frictionQuery = GetEntityQuery<TileFrictionModifierComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var pullerQuery = GetEntityQuery<SharedPullerComponent>();
            var pullableQuery = GetEntityQuery<SharedPullableComponent>();
            var gridQuery = GetEntityQuery<MapGridComponent>();

            foreach (var body in mapComponent.AwakeBodies)
            {
                var uid = body.Owner;

                // Only apply friction when it's not a mob (or the mob doesn't have control)
                if (prediction && !body.Predict ||
                    body.BodyStatus == BodyStatus.InAir ||
                    _mover.UseMobMovement(uid))
                {
                    continue;
                }

                if (body.LinearVelocity.Equals(Vector2.Zero) && body.AngularVelocity.Equals(0f))
                    continue;

                if (!xformQuery.TryGetComponent(uid, out var xform))
                {
                    Log.Error($"Unable to get transform for {ToPrettyString(uid)} in tilefrictioncontroller");
                    continue;
                }

                var surfaceFriction = GetTileFriction(uid, body, xform, gridQuery, frictionQuery);
                var bodyModifier = 1f;

                if (frictionQuery.TryGetComponent(uid, out var frictionComp))
                {
                    bodyModifier = frictionComp.Modifier;
                }

                var ev = new TileFrictionEvent(bodyModifier);

                RaiseLocalEvent(uid, ref ev);
                bodyModifier = ev.Modifier;

                // If we're sandwiched between 2 pullers reduce friction
                // Might be better to make this dynamic and check how many are in the pull chain?
                // Either way should be much faster for now.
                if (pullerQuery.TryGetComponent(uid, out var puller) && puller.Pulling != null &&
                    pullableQuery.TryGetComponent(uid, out var pullable) && pullable.BeingPulled)
                {
                    bodyModifier *= 0.2f;
                }

                var friction = _frictionModifier * surfaceFriction * bodyModifier;

                ReduceLinearVelocity(uid, prediction, body, friction, frameTime);
                ReduceAngularVelocity(uid, prediction, body, friction, frameTime);
            }
        }

        private void ReduceLinearVelocity(EntityUid uid, bool prediction, PhysicsComponent body, float friction, float frameTime)
        {
            var speed = body.LinearVelocity.Length();

            if (speed <= 0.0f)
                return;

            // This is the *actual* amount that speed will drop by, we just do some multiplication around it to be easier.
            var drop = 0.0f;
            float control;

            if (friction > 0.0f)
            {
                // TBH I can't really tell if this makes a difference.
                if (!prediction)
                {
                    control = speed < _stopSpeed ? _stopSpeed : speed;
                }
                else
                {
                    control = speed;
                }

                drop += control * friction * frameTime;
            }

            var newSpeed = MathF.Max(0.0f, speed - drop);

            newSpeed /= speed;
            _physics.SetLinearVelocity(uid, body.LinearVelocity * newSpeed, body: body);
        }

        private void ReduceAngularVelocity(EntityUid uid, bool prediction, PhysicsComponent body, float friction, float frameTime)
        {
            var speed = MathF.Abs(body.AngularVelocity);

            if (speed <= 0.0f)
                return;

            // This is the *actual* amount that speed will drop by, we just do some multiplication around it to be easier.
            var drop = 0.0f;
            float control;

            if (friction > 0.0f)
            {
                // TBH I can't really tell if this makes a difference.
                if (!prediction)
                {
                    control = speed < _stopSpeed ? _stopSpeed : speed;
                }
                else
                {
                    control = speed;
                }

                drop += control * friction * frameTime;
            }

            var newSpeed = MathF.Max(0.0f, speed - drop);

            newSpeed /= speed;
            _physics.SetAngularVelocity(uid, body.AngularVelocity * newSpeed, body: body);
        }

        [Pure]
        private float GetTileFriction(
            EntityUid uid,
            PhysicsComponent body,
            TransformComponent xform,
            EntityQuery<MapGridComponent> gridQuery,
            EntityQuery<TileFrictionModifierComponent> frictionQuery)
        {
            // TODO: Make IsWeightless event-based; we already have grid traversals tracked so just raise events
            if (_gravity.IsWeightless(uid, body, xform))
                return 0.0f;

            if (!xform.Coordinates.IsValid(EntityManager))
                return 0.0f;

            // If not on a grid then return the map's friction.
            if (!gridQuery.TryGetComponent(xform.GridUid, out var grid))
            {
                return frictionQuery.TryGetComponent(xform.MapUid, out var friction)
                    ? friction.Modifier
                    : DefaultFriction;
            }

            var tile = grid.GetTileRef(xform.Coordinates);

            // If it's a map but on an empty tile then just assume it has gravity.
            if (tile.Tile.IsEmpty &&
                HasComp<MapComponent>(xform.GridUid) &&
                (!TryComp<GravityComponent>(xform.GridUid, out var gravity) || gravity.Enabled))
            {
                return DefaultFriction;
            }

            // If there's an anchored ent that modifies friction then fallback to that instead.
            var anc = grid.GetAnchoredEntitiesEnumerator(tile.GridIndices);

            while (anc.MoveNext(out var tileEnt))
            {
                if (frictionQuery.TryGetComponent(tileEnt, out var friction))
                    return friction.Modifier;
            }

            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
            return tileDef.Friction;
        }

        public void SetModifier(EntityUid entityUid, float value, TileFrictionModifierComponent? friction = null)
        {
            if (!Resolve(entityUid, ref friction) || value.Equals(friction.Modifier))
                return;

            friction.Modifier = value;
            Dirty(friction);
        }
    }
}
