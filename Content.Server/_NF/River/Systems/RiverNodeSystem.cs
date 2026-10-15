using Content.Server._NF.River.Components;
using Content.Shared._NF.River;
using Content.Shared._NF.River.Components;
using Content.Shared._NF.River.Events;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;
using System.ComponentModel;
using System.Numerics;

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
            Spawn("SpaceRiverSourceEast", new MapCoordinates(-500f, 0f, new MapId(1)));
            Spawn("SpaceRiverSourceSouthEast", new MapCoordinates(-200f, 700f, new MapId(1)));
            Spawn("SpaceRiverSourceSouthEast", new MapCoordinates(400f, 700f, new MapId(1)));
            Spawn("SpaceRiverSourceSouthEast", new MapCoordinates(1000f, 700f, new MapId(1)));
            Spawn("SpaceRiverSourceSouthEast", new MapCoordinates(1600f, 700f, new MapId(1)));
            Spawn("SpaceRiverSourceNorthEast", new MapCoordinates(100f, -700f, new MapId(1)));
            Spawn("SpaceRiverSourceNorthEast", new MapCoordinates(700f, -700f, new MapId(1)));
            Spawn("SpaceRiverSourceNorthEast", new MapCoordinates(1300f, -700f, new MapId(1)));
            Spawn("SpaceRiverSourceNorthEast", new MapCoordinates(1900f, -700f, new MapId(1)));
            _doneOnce = true;
        }
        //END TODO Testing
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

            foreach (var nextNetNode in nodeRiverComp.NextNodes)
            {
                _receiversInRange.Clear();
                var nextNode = GetEntity(nextNetNode);
                var nextNodeCoord = _transform.GetMapCoordinates(nextNode);
                var centrePoint = (nodeCoord.Position + nextNodeCoord.Position) / 2;
                var lookUpRange = (nodeCoord.Position - centrePoint).Length() + nodeRiverComp.NodeRange;
                _lookup.GetEntitiesInRange<RiverFlowReceiverComponent>(nodeCoord.MapId, centrePoint, lookUpRange, _receiversInRange);
                foreach (var receiver in _receiversInRange)
                {
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
        // Ensures the node doesn't get parented to a grid.        
        if (_transform.GetMap(uid) is { } currentMap) //If the uid's map is not null, assign the map to currentMap
        {
            _transform.SetParent(uid, currentMap);
        }

        component.Location = _transform.GetWorldPosition(uid);

        _nodeList.Add(uid);
        _pvsOverride.AddGlobalOverride(uid);

        // TODO: Separate this towards River Generation when I get to that.
        if (component.IsSource)
        {
            component.FlowDirection = component.CardinalFlow.GetClockwise90Degrees().ToAngle(); // Point in desired direction. For some reason North is not North. So rotate it 90 Degrees.
            CreateRiver(uid, 1000); //TODO: make the riverLength variable.
        }
        // END TODO separate River Gen
        _nodesChanged = true;
        Dirty(uid, component);
    }

    /// <summary>
    /// Calculates the relevant modifiers the void rivers impart on the shuttle's velocity/acceleration etc.
    /// </summary>
    /// <param name="shuttlePosition">Shuttle world position</param>
    /// <param name="velocity">Velocity representing desired shuttle travel direction.</param>
    /// <param name="receiver">The shuttle's RiverFlowReceiverComponent</param>
    /// <returns></returns>
    public float ObtainVelocityModifier(EntityUid shuttleUid, Vector2 velocity, RiverFlowReceiverComponent receiver)
    {
        var velocityMod = 1.0f;

        if (velocity.Length() != 0f)
        {
            var nodeQuery = GetEntityQuery<RiverNodeComponent>();
            var riverBoost = 0f;
            var riverSlowdown = 0f;

            // Collate data for the final river flow effect.
            foreach (var entity in receiver.InfluencingNodes)
            {
                if (!nodeQuery.TryGetComponent(entity, out var node))
                {
                    continue;
                }
                foreach (var segment in node.NextNodes)
                {
                    // Calculates the distance between the shuttle and the river segment.
                    var nextNode = GetEntity(segment);
                    if (!TryComp<RiverNodeComponent>(nextNode, out var nextComp))
                    {
                        continue;
                    }
                    var shuttleLocation = _transform.GetWorldPosition(shuttleUid);
                    var nodeLocation = _transform.GetWorldPosition(entity);
                    var nextNodeLocation = _transform.GetWorldPosition(nextNode);

                    var controlPoint1 = nodeLocation + node.ForwardControlPoint;
                    var controlPoint2 = nextNodeLocation + -nextComp.ForwardControlPoint;
                    var distanceToRiver = CubicDistanceToBezier(shuttleLocation, nodeLocation, controlPoint1, controlPoint2, nextNodeLocation, out var closestPoint, out var distAlongCurve, SegmentResolution);
                    var flowDirection = Vector2.Normalize(CalculateBezierTangent(nodeLocation, controlPoint1, controlPoint2, nextNodeLocation, distAlongCurve));
                    var distanceMod = 1.0f;

                    // Calculates the modifier for being away from the river segment.
                    if (node.NodeRange != 0f && distanceToRiver < node.NodeRange)
                    {
                        distanceMod = 1 - distanceToRiver / node.NodeRange;
                    }
                    else
                    {
                        continue;
                    }

                    // Calculate the difference in direction between the river flow effect and the shuttle's desired travel direction.
                    var flowDiffScalar = Vector2.Dot(velocity.Normalized(), flowDirection);
                    if (flowDiffScalar >= 0) //If going along the flow, stores only the biggest boost modifier.
                    {
                        var tempRiverBoost = flowDiffScalar * node.Boost * distanceMod;
                        if (tempRiverBoost > riverBoost)
                        {
                            riverBoost = tempRiverBoost;
                        }
                    }
                    else //If going against the flow, stores only the biggest slowdown modifier.
                    {
                        var tempRiverSlowdown = node.SlowdownMultiplier * -flowDiffScalar * distanceMod;

                        if (tempRiverSlowdown > riverSlowdown)
                        {
                            riverSlowdown = tempRiverSlowdown;
                        }
                    }
                }
            }
            //Colate the final velocity modifier.
            velocityMod = 1 + riverBoost - riverSlowdown;
        }
        return velocityMod;
    }

    //TODO: Move to river-gen code when I get to that and implement something proper.
    public void CreateRiver(EntityUid node, int riverLength)
    {
        CreateRiverSegment(node, riverLength);
    }
    //END TODO River gen.

    //TODO: Probably move this to river gen once I get to it Possibly overhaul the system when I get to that..
    /// <summary>
    /// Creates a new SpaceRiverNode and connects it to the input node.
    /// </summary>
    /// <param name="lastNode">The node from which you wish to create a segment.</param>
    /// <param name="riverLength">How many segments the river should have</param> //TODO: probably move this to CreateRiver.
    /// <param name="segment">An iterator that counts up the amount of segments to ensure the river becomes the appropriate length</param> //TODO: Probably move this to CreateRiver.
    /// <param name="branchNumber">Which branch is this? Needs to be no higher than totalBranches</param>
    /// <param name="totalBranches">The total number of branches stemming from the lastNode. 1 means there is no fork in the river.</param>
    /// <returns>returns true if successful, false if it failed.</returns>
    public bool CreateRiverSegment(EntityUid lastNode, int riverLength, int segment = 0, int branchNumber = 1, int totalBranches = 1)
    {
        //TODO: Make these not hard-coded.
        var distance = 200f;
        var maxAngle = 20f;
        var splitIterate = 5;
        //END TODO hard-coded.

        var nodePos = _transform.GetMapCoordinates(lastNode);

        //Ensure that the maxAngle cannot break the range-finding of segments, nor break the Bezier curve code. (No longer necessary,
        //but still keeps things nice-ish. Although, would have to test the range-finding bit)
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

        Angle minBranchDifference = MathF.Asin(lastComp.NodeRange / distance) * 2 * 1.1f; // 10% leway
        // Make sure all the branches can fit. If not, set the total branches to a number that can fit.
        if (totalBranches * minBranchDifference.Degrees > 180f)
        {
            totalBranches = (int)(180f / minBranchDifference.Degrees);
        }

        // There can be no more branches than the totalBranches
        if (branchNumber > totalBranches)
        {
            return false;
        }

        var wiggleRoom = 180f - (minBranchDifference * totalBranches);
        if (maxAngle * 2 > wiggleRoom / totalBranches)
        {
            maxAngle = (float)wiggleRoom / totalBranches;
        }


        //Angle directionOffset = (180f / (totalBranches * 2)) * (branchNumber * 2 - 1) - 90f;
        Angle minLimiter = maxAngle + minBranchDifference;
        Angle directionOffset = branchNumber * 2 * minLimiter - ((totalBranches + 1) * minLimiter);

        //Check for other nodes that the segment could connect to.
        //List<EntityUid> connectableNodes = new();
        //List<EntityUid> repulsiveNodes = new();
        //var nearbyRiverNodes = _lookup.GetEntitiesInRange<RiverNodeComponent>(nodePos, distance * 1.5f);
        //foreach (var nearbyNode in nearbyRiverNodes)
        //{
        //    var nearNodePos = _transform.GetWorldPosition(nearbyNode);
        //    Angle nearNodeDirection = Vector2.Normalize(nearNodePos - nodePos.Position).ToAngle();
        //
        //    // Is the nearby node not already connected to this node and ahead of the flow?
        //    var angleTest = Angle.ShortestDistance(nearNodeDirection, lastDirection).Degrees;
        //    if (!ContainsNode(nearbyNode, lastComp.NextNodes) && Math.Abs(angleTest) < 90f && (nearNodePos - nodePos.Position).Length() > distance * 0.5f)
        //    {
        //        if (!TryComp<RiverNodeComponent>(nearbyNode, out var nearbyComp))
        //        {
        //            continue;
        //        }
        //
        //        // Does the nearbyNode's flow not run counter to ours?
        //        var angleTest2 = Angle.ShortestDistance(nearbyComp.FlowDirection, lastDirection).Degrees;
        //        if ((angleTest >= 0 && angleTest2 >= 0) || (angleTest <= 0 && angleTest2 <= 0))
        //        {
        //            connectableNodes.Add(nearbyNode);
        //        }
        //        else
        //        {
        //            repulsiveNodes.Add(nearbyNode);
        //            //var leftRightIndicator = 1; // - if left, + if right
        //            //if (directionOffset < 0)
        //            //{
        //            //    leftRightIndicator = -1;
        //            //}
        //            //if (Angle.ShortestDistance(nearNodeDirection, lastDirection) < maxAngle + directionOffset * leftRightIndicator)
        //            //{
        //            //}
        //            //else
        //            //{
        //            //    repulsiveNodes.Add(nearbyNode);
        //            //}
        //        }
        //    }
        //}

        // Temporary?
        EntityUid newNode;
        Angle newFlowDirection = new();

        if (true)//connectableNodes.Count == 0)
        {

            var directionModifier = _random.NextAngle(Angle.FromDegrees(-maxAngle + directionOffset), Angle.FromDegrees(maxAngle + directionOffset));
            var nodeDirection = lastDirection + directionModifier;
            var offset = nodeDirection.ToVec() * distance;
            var spawnPos = nodePos.Offset(offset);
            newFlowDirection = lastDirection + directionModifier * 2;
            newFlowDirection = newFlowDirection.Reduced();

            newNode = Spawn("SpaceRiverNode", spawnPos);

        }
        else
        {
            //newNode = connectableNodes[0];
        }

        if (!TryComp<RiverNodeComponent>(newNode, out var newComp))
        {
            return false;
        }
        // There's so many magic numbers in this class... TODO: get rid of as many magic numbers as possible.
        lastComp.ForwardControlPoint = lastComp.FlowDirection.ToVec() * lastComp.Boost * 50f;
        newComp.ForwardControlPoint = newFlowDirection.ToVec() * newComp.Boost * 50f;
        //END TODO magic number cleanup

        if (true)//connectableNodes.Count == 0)
        {
            newComp.FlowDirection = newFlowDirection;
        }

        lastComp.NextNodes.Add(GetNetEntity(newNode));
        newComp.PriorNodes.Add(GetNetEntity(lastNode));
        Dirty(lastNode, lastComp);
        Dirty(newNode, newComp);

        segment++;
        if (segment < riverLength && true)//connectableNodes.Count == 0)
        {
            if (false)//0 == segment % splitIterate)
            {
                CreateRiverSegment(newNode, riverLength, segment, 1, 2);
                CreateRiverSegment(newNode, riverLength, segment, 2, 2);
                //CreateRiverSegment(newNode, (int)(riverLength * 0.5f), segment);
            }
            else
            {
                CreateRiverSegment(newNode, riverLength, segment);
            }
        }
        return true;
    }
    //End TODO move to river gen
}
