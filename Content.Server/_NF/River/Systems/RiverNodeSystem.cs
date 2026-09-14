using Content.Server._NF.River.Components;
using Content.Server.Administration.Logs.Converters;
using Content.Shared._NF.River;
using Content.Shared._NF.River.Components;
using Content.Shared._NF.River.Events;
using Content.Shared.Physics;
using Content.Shared.Traits.Assorted;
using NetCord;
using Npgsql.Internal.Postgres;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Server._NF.River.Systems;

public sealed partial class RiverNodeSystem : SharedRiverNodeSystem
{

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private float _accumulator;
    private bool _nodesChanged = true; //Is true when any River Node has changed. Nodes won't change every frame, so this is to lower performance impact.
    private List<EntityUid> _nodeList = new();
    private bool _doneOnce = false;
    private readonly HashSet<Entity<RiverFlowReceiverComponent>> _receiversInRange = new();

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


        //TODO: Remove! For testing purposes only
        if (!_doneOnce)
        {
            Spawn("SpaceRiverSource", new MapCoordinates(0f, 0f, new MapId(1)));
            _doneOnce = true;
        }
    }

    private void UpdateRivers()
    {
        if (_nodesChanged)
        {
            var netNodeList = GetNetEntityList(_nodeList);
            var transferEvent = new TransferRiverNodesEvent(netNodeList);
            RaiseNetworkEvent(transferEvent);
            _nodesChanged = false;
        }

        var flowReceivers = EntityQueryEnumerator<RiverFlowReceiverComponent>();
        while (flowReceivers.MoveNext(out var receiverUid, out var receiver))
        {
            receiver.InfluencingNodes.Clear();
            receiver.InRiver = false;
        }

        foreach (var node in _nodeList)
        {
            if (!TryComp<RiverNodeComponent>(node, out var nodeRiverComp))
            {
                continue;
            }

            var nodeCoord = _transform.GetMapCoordinates(node);

            foreach (var nodeLink in nodeRiverComp.NextNodes)
            {
                _receiversInRange.Clear();
                var nextNode = GetEntity(nodeLink.Node);
                var nextNodeCoord = _transform.GetMapCoordinates(nextNode);
                var centrePoint = (nodeCoord.Position + nextNodeCoord.Position) / 2;
                var lookUpRange = (nodeCoord.Position - centrePoint).Length() + nodeRiverComp.NodeRange;
                _lookup.GetEntitiesInRange<RiverFlowReceiverComponent>(nodeCoord.MapId, centrePoint, lookUpRange, _receiversInRange);
                foreach (var receiver in _receiversInRange)
                {
                    receiver.Comp.InfluencingNodes.Add(nextNode);
                    if (!receiver.Comp.InfluencingNodes.Contains(node))
                    {
                        receiver.Comp.InfluencingNodes.Add(node);
                    }
                    receiver.Comp.InRiver = true;
                }
            }
        }
    }

    /// <summary>
    /// Sets up the river node when it's created.
    /// </summary>
    /// <param name="uid">The node to be set up</param>
    /// <param name="component">The node's RiverNodeComponent</param>
    /// <param name="args">Event arguments.</param>
    private void SetupNode(EntityUid uid, RiverNodeComponent component, ComponentStartup args)
    {
        component.Location = _transform.GetWorldPosition(uid);

        _nodeList.Add(uid);
        _pvsOverride.AddGlobalOverride(uid);
        Dirty(uid, component);
        _nodesChanged = true;

        if (component.IsSource)
        {
            CreateRiver(uid, 10); //TODO: make the riverLength variable.
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
        CreateRiverSegment(node, riverLength);
    }


    /// <summary>
    /// Creates a new SpaceRiverNode and connects it to the input node.
    /// </summary>
    /// <param name="lastNode">The node from which you wish to create a segment.</param>
    /// <param name="riverLength">How many segments the river should have</param> //TODO: probably move this to CreateRiver.
    /// <param name="segment">An iterator that counts up the amount of segments to ensure the river becomes the appropriate length</param> //TODO: Probably move this to CreateRiver.
    /// <returns>returns true if successful, false if it failed.</returns>
    public bool CreateRiverSegment(EntityUid lastNode, int riverLength, int segment = 0)
    {
        //TODO: Make these not hard-coded.
        var distance = 200f;
        var maxAngle = 45f;

        //Ensure that the maxAngle cannot break the range-finding of segments, nor break the Bezier curve code.
        if (maxAngle > 90f)
        {
            maxAngle = 90f;
        }
        else if (maxAngle < -90f)
        {
            maxAngle = -90f;
        }

        if (!TryComp<RiverNodeComponent>(lastNode, out var lastComp))
        {
            return false;
        }
        var lastDirection = lastComp.FlowDirection;
        var directionModifier = _random.NextAngle(Angle.FromDegrees(-maxAngle), Angle.FromDegrees(maxAngle));
        var nodeDirection = lastDirection + directionModifier;
        var offset = nodeDirection.ToVec() * distance;
        var nodePos = _transform.GetMapCoordinates(lastNode);
        var spawnPos = nodePos.Offset(offset);

        var newNode = Spawn("SpaceRiverNode", spawnPos);

        if (!TryComp<RiverNodeComponent>(newNode, out var newComp))
        {
            return false;
        }
        newComp.FlowDirection = lastDirection + directionModifier * 2;

        var controlPoint = CalculateControlPoint(_transform.GetWorldPosition(lastNode),
            lastDirection,
            _transform.GetWorldPosition(newNode),
            newComp.FlowDirection);

        lastComp.NextNodes.Add(new(GetNetEntity(newNode), controlPoint));

        segment++;
        if (segment < riverLength)
        {
            CreateRiverSegment(newNode, riverLength, segment);
        }
        return true;
    }

    /// <summary>
    /// This function calculates where the control point for the Bezier curve needs to be. It does this by checking where two lines corresponding to the flow-directions,
    /// centred on the start and end points cross. 
    /// </summary>
    /// <param name="startPoint">The location where the river flow comes into the river segment.</param>
    /// <param name="startDirection">The direction in which the startPoint's flowDirection points.</param>
    /// <param name="endPoint">The location where the river flow leaves the river segment.</param>
    /// <param name="endDirection">The direction in which the endPoint's flowDirection points.</param>
    /// <returns>The location of the control point.</returns>
    public static Vector2 CalculateControlPoint(Vector2 startPoint, Angle startDirection, Vector2 endPoint, Angle endDirection)
    {
        var controlPoint = new Vector2(0, 0);
        var vecStartDir = startDirection.ToVec() + startPoint;
        var vecEndDir = endDirection.ToVec() + endPoint;

        //If the directions are not equal, find where the direction-lines cross. This is the Control Point for the Bezier Curve.
        if (vecStartDir.X != vecEndDir.X && vecStartDir.Y != vecEndDir.Y)
        {
            // Line intersection math, sorry. Basically finds the point at where the flow directions of the nodes intersect.
            controlPoint.X = ((startPoint.X * vecStartDir.Y - startPoint.Y * vecStartDir.X) *
                (endPoint.X - vecEndDir.X) - (startPoint.X - vecStartDir.X) *
                (endPoint.X * vecEndDir.Y - endPoint.Y * vecEndDir.X)) /
                ((startPoint.X - vecStartDir.X) * (endPoint.Y - vecEndDir.Y) -
                (startPoint.Y - vecStartDir.Y) * (endPoint.X - vecEndDir.X));

            controlPoint.Y = ((startPoint.X * vecStartDir.Y - startPoint.Y * vecStartDir.X) *
                (endPoint.Y - vecEndDir.Y) - (startPoint.Y - vecStartDir.Y) *
                (endPoint.X * vecEndDir.Y - endPoint.Y * vecEndDir.X)) /
                ((startPoint.X - vecStartDir.X) * (endPoint.Y - vecEndDir.Y) -
                (startPoint.Y - vecStartDir.Y) * (endPoint.X - vecEndDir.X));
        }
        else
        {
            var pointLocationDifference = endPoint - startPoint;
            controlPoint = startPoint + (pointLocationDifference * (pointLocationDifference.Length() / 2)); // Halfway between the startPoint and endPoint.
        }

        return (controlPoint);
    }
}
