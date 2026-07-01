using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Robust.Shared.Configuration;
using Content.Server._NF.VoidRiver.Components;
using System.Numerics;
using Robust.Server.GameObjects;

namespace Content.Server._NF.VoidRiver.Systems;

public sealed partial class RiverNodeSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
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
            foreach (var entity in receiver.InfluencingNodes)
            {
                if (!nodeQuery.TryGetComponent(entity, out var node))
                {
                    continue;
                }
                var flowDiffScalar = Vector2.Dot(velocity.Normalized(), node.FlowDirection.ToVec());
                var riverDirection = shuttlePosition - _transform.GetWorldPosition(entity);
                var distanceToRiver = riverDirection.Length();
                var distanceMod = 1.0f;

                if (node.NodeRange != 0f && distanceToRiver < node.NodeRange)
                {
                    distanceMod = 1 - distanceToRiver / node.NodeRange;
                }

                if (flowDiffScalar >= 0)
                {
                    // Set velocityMod somewhere between 1.0 and 1.0+Boost value
                    velocityMod += flowDiffScalar * node.Boost * distanceMod;
                }
                else
                {
                    // Set velocityMod somewhere between the SlowDownMultiplier value and 1.0
                    velocityMod = 1 - (node.SlowdownMultiplier * -flowDiffScalar * distanceMod);
                }
            }
        }

        return velocityMod;
    }

}
