using Content.Server._NF.River.Components;
using Content.Server.Administration.Logs.Converters;
using Content.Shared._NF.River;
using Content.Shared._NF.River.Components;
using Content.Shared._NF.River.Events;
using Content.Shared.Traits.Assorted;
using NetCord;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._NF.River.Systems;

public sealed partial class RiverNodeSystem : SharedRiverNodeSystem
{

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    private float _accumulator;
    private bool _nodesChanged = true; //Is true when any River Node has changed. Nodes won't change every frame, so this is to lower performance impact.
    private List<EntityUid> _nodeList = new();

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
            ////Reset the list of nodes 
            //NodeList.Clear();
            //NodeList.EnsureCapacity(Count<RiverNodeComponent>());
            //
            //var sources = EntityQueryEnumerator<RiverNodeComponent>();
            //
            ////Add all the nodes to the list of nodes
            //while (sources.MoveNext(out var uid, out var source))
            //{
            //    NodeList.Add(uid);
            //}

            var netNodeList = GetNetEntityList(_nodeList);
            var transferEvent = new TransferRiverNodesEvent(netNodeList);
            RaiseNetworkEvent(transferEvent);
            _nodesChanged = false;
        }

        var flowReceivers = EntityQueryEnumerator<RiverFlowReceiverComponent, TransformComponent>();

        //Iterate through all the potential shuttles that can be influenced by the Nodes, and apply the relevant modifiers.
        while (flowReceivers.MoveNext(out var receiverUid, out var receiver, out var receiverTransform))
        {
            receiver.InfluencingNodes.Clear();
            var targetWorld = _transform.GetWorldPosition(receiverTransform);
            var inRiver = false;
            foreach (var node in _nodeList)
            {
                if (!TryComp(node, out TransformComponent? nodeTransform))
                {
                    continue;
                }

                if (!TryComp<RiverNodeComponent>(node, out var nodeRiverComp))
                {
                    continue;
                }

                // Are the node and flow receiver on the same Map?
                if (nodeTransform.MapID != receiverTransform.MapID)
                {
                    continue;
                }

                var direction = targetWorld - _transform.GetWorldPosition(node);
                var distance = direction.Length();

                // Are the node and flow receiver close enough?
                if (distance > nodeRiverComp.NodeRange)
                {
                    continue;
                }

                receiver.InfluencingNodes.Add(node);
                inRiver = true;
            }
            receiver.InRiver = inRiver;
        }
    }

    private void SetupNode(EntityUid uid, /*Shared*/RiverNodeComponent component, ComponentStartup args)
    {
        component.Location = _transform.GetWorldPosition(uid);
        //component.FlowDirection = _random.NextAngle();

        _nodeList.Add(uid);
        _pvsOverride.AddGlobalOverride(uid);
        Dirty(uid, component);
        _nodesChanged = true;

        if (component.IsSource)
        {
            CreateRiver(uid, 1000); //TODO: make the riverLength variable.
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

    public void CreateRiver(EntityUid node, int riverLength)
    {
        CreateRiverSegment(node, 0, riverLength);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="lastNode"></param>
    /// <param name="segment"></param>
    /// <param name="riverLength"></param>
    /// <returns>returns true if successful, false if it failed.</returns>
    public bool CreateRiverSegment(EntityUid lastNode, int segment, int riverLength)
    {
        if (!TryComp<RiverNodeComponent>(lastNode, out var lastComp))
        {
            return false;
        }
        var lastDirection = lastComp.FlowDirection;
        var directionModifier = _random.NextAngle(Angle.FromDegrees(-5f), Angle.FromDegrees(5f));
        var nodeDirection = lastDirection + directionModifier;
        var offset = nodeDirection.ToVec() * 70f;
        var nodePos = _transform.GetMapCoordinates(lastNode);
        var spawnPos = nodePos.Offset(offset);

        var newNode = Spawn("SpaceRiverNode", spawnPos);

        if (!TryComp<RiverNodeComponent>(newNode, out var newComp))
        {
            return false;
        }
        newComp.FlowDirection = lastDirection + directionModifier * 2;

        segment++;
        if (segment < riverLength)
        {
            CreateRiverSegment(newNode, segment, riverLength);
        }
        return true;
    }
}
