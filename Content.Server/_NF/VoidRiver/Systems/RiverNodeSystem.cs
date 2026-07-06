using Content.Server._NF.VoidRiver.Components;
using Content.Shared.Traits.Assorted;
using NetCord;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._NF.VoidRiver.Systems;

public sealed partial class RiverNodeSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private readonly record struct SourceData(
        Entity<RiverNodeComponent, TransformComponent> Entity,
        Vector2 WorldPosition)
    {
        //TODO: Remove the ones that're not used after finishing coding
        public EntityUid? Uid => Entity;
        public TransformComponent Transform => Entity.Comp2;
        public float Boost => Entity.Comp1.Boost;
        public float SlowdownMultiplier => Entity.Comp1.SlowdownMultiplier;
        public Angle FlowDirection => Entity.Comp1.FlowDirection;
        public float NodeRange => Entity.Comp1.NodeRange;
    }

    private float _accumulator;
    private List<SourceData> _sources = new();
    private bool _nodesChanged = true; //Is true when any River Node has changed. Nodes won't change every frame, so this is to lower performance impact. TODO: Make this do something, or remove it.

    public override void Initialize()
    {
        base.Initialize();
        SubscribeCvars();
        SubscribeLocalEvent<RiverNodeComponent, ComponentStartup>(SetupNode);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < UpdateRate)
        {
            return;
        }

        UpdateRivers();
        _accumulator = 0f;
    }

    private void UpdateRivers()
    {
        if (_nodesChanged)
        {
            //Reset the list of nodes 
            _sources.Clear();
            _sources.EnsureCapacity(Count<RiverNodeComponent>());

            var sources = EntityQueryEnumerator<RiverNodeComponent, TransformComponent>();

            //Add all the nodes to the list of nodes
            while (sources.MoveNext(out var uid, out var source, out var xform))
            {
                var worldPos = _transform.GetWorldPosition(xform);

                _sources.Add(new((uid, source, xform), worldPos));
            }

            //_nodesChanged = false;
        }

        var targets = EntityQueryEnumerator<RiverFlowReceiverComponent, TransformComponent>();

        //Iterate through all the potential shuttles that can be influenced by the Nodes, and apply the relevant modifiers.
        while (targets.MoveNext(out var targetUid, out var target, out var targetTransform))
        {
            target.InfluencingNodes.Clear();
            var targetWorld = _transform.GetWorldPosition(targetTransform);
            var inRiver = false;
            foreach (var source in _sources)
            {
                // Are the source and target on the same Map?
                if (source.Transform.MapID != targetTransform.MapID)
                {
                    continue;
                }

                var direction = targetWorld - source.WorldPosition;
                var distance = direction.Length();

                // Are the source and target close enough?
                if (distance > source.NodeRange)
                {
                    continue;
                }

                if (source.Uid == null)
                {
                    continue;
                }

                target.InfluencingNodes.Add((EntityUid)source.Uid);
                inRiver = true;
            }
            target.InRiver = inRiver;
        }
    }

    private void SetupNode(EntityUid uid, RiverNodeComponent component, ComponentStartup args)
    {
        component.FlowDirection = _random.NextAngle();
    }

    /// <summary>
    /// Calculates the relevant modifiers the void rivers impart on the shuttle's velocity/acceleration etc.
    /// </summary>
    /// <param name="shuttlePosition">Shuttle world position</param>
    /// <param name="velocity">Velocity representing desired shuttle travel direction.</param>
    /// <param name="receiver">The shuttle's RiverFlowReceiverComponent</param>
    /// <returns></returns>
    public float ObtainVelocityModifier(Vector2 shuttlePosition, Vector2 velocity, RiverFlowReceiverComponent receiver)
    {
        var velocityMod = 1.0f;

        if (velocity.Length() != 0f)
        {
            var nodeQuery = GetEntityQuery<RiverNodeComponent>();
            var riverVector = new Vector2();
            var riverBoost = 0f;
            var riverSlowdown = 0f;
            var totalInfluence = 0f; // This collates the total amount of influence given to account for node distances.

            // Collate data for the final river flow effect.
            foreach (var entity in receiver.InfluencingNodes)
            {
                if (!nodeQuery.TryGetComponent(entity, out var node))
                {
                    continue;
                }
                // Calculates the distance between the shuttle and the river node.
                var riverDirection = shuttlePosition - _transform.GetWorldPosition(entity);
                var distanceToRiver = riverDirection.Length();
                var distanceMod = 1.0f;

                // Calculates the modifier for being away from the centre of the node.
                if (node.NodeRange != 0f && distanceToRiver < node.NodeRange)
                {
                    distanceMod = 1 - distanceToRiver / node.NodeRange;
                }

                riverVector += node.FlowDirection.ToVec() * distanceMod;
                riverBoost += node.Boost * distanceMod;
                riverSlowdown += node.SlowdownMultiplier * distanceMod;
                totalInfluence += distanceMod;
            }
            // Average the Boost and Slowdown
            if (totalInfluence != 0)
            {
                riverBoost /= totalInfluence;
                riverSlowdown /= totalInfluence;
            }
            else
            {
                // Something went wrong! This should never be 0.
            }

            var interferenceMod = 1.0f;
            if (riverVector.Length() < 1f)
            {
                interferenceMod = riverVector.Length();
            }
            riverVector = riverVector.Normalized();

            // Calculate the difference in direction between the total river flow effect and the shuttle's desired travel direction.
            var flowDiffScalar = Vector2.Dot(velocity.Normalized(), riverVector);

            if (flowDiffScalar >= 0)
            {
                // Set velocityMod somewhere between 1.0 and 1.0+Boost value
                velocityMod += flowDiffScalar * riverBoost * interferenceMod;
            }
            else
            {
                // Set velocityMod somewhere between the SlowDownMultiplier value and 1.0
                velocityMod = 1 - (riverSlowdown * -flowDiffScalar * interferenceMod);
            }
        }

        return velocityMod;
    }

}
