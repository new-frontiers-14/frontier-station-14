using Content.Server._NF.River.Components;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using System.Collections;
using System.Data;
using System.Numerics;
using System.Reflection.Metadata;

namespace Content.Server._NF.River.Systems;

public partial class RiverNodeSystem
{
    private struct Connection
    {
        public Connection(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }

        public override string ToString() => $"({Start}, {End})";
    }
    public void POIArterialRiverGen(MapId mapId, List<EntityUid> pois)
    {
        // There's a *lot* of loops in this, this can probably be optimised, but it's only run once, so this will do for now.
        var unitDistance = 3000f; //2500 or 3000? TODO: make this not a magic number.
        var validPois = FilterValidPOIs(mapId, pois);
        var poiPositions = GetPOIPositions(validPois);
        poiPositions.Add(new Vector2(0f, 0f)); // Adds Frontier Station.

        // Connect all the POIs that are in range of eachother
        List<Connection> connections = new();

        List<Vector2> handledPositions = new();
        List<Vector2> unconnectedPositions = new();
        foreach (var poiPos in poiPositions)
        {
            handledPositions.Add(poiPos);
            var connected = false;
            foreach (var otherPoiPos in poiPositions)
            {
                if (handledPositions.Contains(otherPoiPos))
                {
                    continue;
                }
                if ((poiPos - otherPoiPos).Length() < unitDistance)
                {
                    var tooCloseToPoi = false;
                    foreach (var distanceCheckPoiPos in poiPositions)
                    {
                        if (distanceCheckPoiPos == poiPos || distanceCheckPoiPos == otherPoiPos)
                        {
                            continue;
                        }
                        if (DistanceToLineSegmentSquared(distanceCheckPoiPos, poiPos, otherPoiPos, out var _, out var t) < 200f || t > 1f || t < 0f)
                        {
                            tooCloseToPoi = true;
                            break;
                        }
                    }
                    if (!tooCloseToPoi)
                    {
                        connections.Add(new Connection(poiPos, otherPoiPos));
                        connected = true;
                    }
                }
            }
            if (!connected && !ContainsPosition(poiPos, connections))
            {
                unconnectedPositions.Add(poiPos);
            }
        }

        // Cull all lines that overlap
        List<Connection> validConnections = new();
        validConnections.AddRange(connections);

        List<Connection> handledConnections = new();
        foreach (var connection in connections)
        {
            handledConnections.Add(connection);
            foreach (var otherConnection in connections)
            {
                if (handledConnections.Contains(otherConnection))
                {
                    continue;
                }
                if (CheckIntersect(connection, otherConnection))
                {
                    validConnections.Remove(connection);
                    validConnections.Remove(otherConnection);
                }
            }
        }

        // Check to make sure there is one, completely connected network, not multiple separate ones (and every valid POI is connected)
        List<List<Vector2>> networks = new();
        foreach (var position in unconnectedPositions)
        {
            List<Vector2> tinyNetwork = new();
            tinyNetwork.Add(position);
            networks.Add(tinyNetwork);
        }
        handledPositions.Clear();
        handledPositions.AddRange(unconnectedPositions);

        var safetyLimit = 100;
        foreach (var position in poiPositions)
        {
            if (handledPositions.Contains(position))
            {
                continue;
            }
            handledPositions.Add(position);
            List<Vector2> posInNetwork = new();
            posInNetwork.Add(position);
            var networkFilled = false;
            while (!networkFilled && safetyLimit > 0)
            {
                var somethingAdded = false;
                foreach (var connection in validConnections)
                {
                    if (posInNetwork.Contains(connection.Start) && !posInNetwork.Contains(connection.End))
                    {
                        posInNetwork.Add(connection.End);
                        if (!handledPositions.Contains(connection.End))
                        {
                            handledPositions.Add(connection.End);
                        }
                        somethingAdded = true;
                    }
                    else if (posInNetwork.Contains(connection.End) && !posInNetwork.Contains(connection.Start))
                    {
                        posInNetwork.Add(connection.Start);
                        if (!handledPositions.Contains(connection.Start))
                        {
                            handledPositions.Add(connection.Start);
                        }
                        somethingAdded = true;
                    }
                }
                if (!somethingAdded)
                {
                    networkFilled = true;
                }
                safetyLimit--;
            }
            networks.Add(posInNetwork);
        }

        //Handle and consolidate separate networks.
        safetyLimit = 100;
        while (networks.Count > 1 && safetyLimit > 0)
        {
            // focusNetwork should be (one of) the largest one(s) to make connections more convenient.
            var focusNetwork = new List<Vector2>();
            var largestNetworkIndex = 0;
            var largestNetworkCount = 0;
            var iterator = 0;
            List<Vector2> averagePositions = new();
            foreach (var network in networks)
            {
                if (network.Count > largestNetworkCount)
                {
                    largestNetworkIndex = iterator;
                    largestNetworkCount = network.Count;
                }
                Vector2 averagePos = new();
                foreach (var position in network)
                {
                    averagePos = averagePos + position;
                }
                averagePos = averagePos / network.Count;
                averagePositions.Add(averagePos);
                iterator++;
            }

            focusNetwork = networks[largestNetworkIndex];

            var closestNetworkIndex = -1;
            var closestNetworkDistance = -1f;

            for (var i = 0; i < networks.Count; i++)
            {
                if (largestNetworkIndex == i)
                {
                    continue;
                }
                var distance = (averagePositions[i] - averagePositions[largestNetworkIndex]).Length();
                if (closestNetworkDistance == -1f || closestNetworkDistance > distance)
                {
                    closestNetworkDistance = distance;
                    closestNetworkIndex = i;
                }
            }
            if (closestNetworkIndex == -1)
            {
                break; //Something went wrong!
            }
            var closestNetwork = networks[closestNetworkIndex];
            Vector2 closestFocusPos = new();
            Vector2 closestOtherPos = new();
            var closestDistance = -1f;
            for (var i = 0; i < focusNetwork.Count; i++)
            {
                for (var j = 0; j < closestNetwork.Count; j++)
                {
                    if (closestDistance == -1)
                    {
                        closestFocusPos = focusNetwork[i];
                        closestOtherPos = closestNetwork[j];
                        closestDistance = (closestOtherPos - closestFocusPos).Length();
                    }
                    else
                    {
                        var distance = (closestNetwork[j] - focusNetwork[i]).Length();
                        if (distance < closestDistance)
                        {
                            closestFocusPos = focusNetwork[i];
                            closestOtherPos = closestNetwork[j];
                            closestDistance = distance;
                        }
                    }
                }
            }
            // Make a connection, and merge the two networks in the "networks" list.
            validConnections.Add(new Connection(closestFocusPos, closestOtherPos));
            networks[largestNetworkIndex].AddRange(closestNetwork);
            networks.Remove(closestNetwork);

            safetyLimit--;
        }

        // Create the actual rivers.
        foreach (var connection in validConnections)
        {
            //CreateRiver(mapId, connection.Start, connection.End);
            CreateRiverPair(mapId, connection.Start, connection.End);
        }
    }

    public bool CreateRiverPair(MapId mapId, Vector2 location1, Vector2 location2, Angle flowDirection1 = default, Angle flowDirection2 = default, bool customFlowDirs = false)
    {
        // TODO: Fix whatever it is that is broken here. Still creates too many river segments I guess? (How though? I don't quite get it, it should be far *fewer* than previous attempts)
        var maxDistanceSquared = MathF.Pow(200f, 2f); //Magic number, TODO: Kill
        var minDistBetweenRiversSquared = MathF.Pow(60f, 2f);

        List<Vector2> nodePositions1 = new(); //Can insert, woo!
        List<Vector2> nodePositions2 = new(); //Both should be generated from 1 to 2, but the order of flow should go the other way. Just so that they are able to remain separate.

        nodePositions1.Add(location1);
        nodePositions1.Add(location2);
        nodePositions2.Add(location1);
        nodePositions2.Add(location2);

        var locationDistanceSquared = (location1 - location2).LengthSquared();
        var longTangent = Vector2.Normalize(location2 - location1);

        if (locationDistanceSquared < maxDistanceSquared) //Does not support tiny rivers.
        {
            return false;
        }

        int segmentNumber = (int)MathF.Sqrt(locationDistanceSquared / maxDistanceSquared);
        var isDone = false;
        var safetyLimit = 100;
        //while (!isDone && safetyLimit > 0)
        for (int i = 1; i <= segmentNumber; i *= 2)
        {
            isDone = true;

            var tempList1 = new List<Vector2>();
            var tempList2 = new List<Vector2>();
            for (var i0 = 0; i0 < Math.Min(nodePositions1.Count, nodePositions2.Count) - 1; i0++) //1To2 and 2To1 should always be equal, but in case they aren't, this prevents over-running the lists.
            {
                var i1 = i0 + 1;
                // Pick the halfway point between nodePositionsNToM[i] and nodePositionsNToM[i + 1], perturb it along the normal, make sure it doesn't cross over its sibling,
                var nodePos1A = nodePositions1[i0];
                var nodePos1B = nodePositions1[i1];
                var nodePos2A = nodePositions2[i0];
                var nodePos2B = nodePositions2[i1];

                var distance1 = (nodePos1B - nodePos1A).Length() * 0.2f;
                var distance2 = (nodePos2B - nodePos2A).Length() * 0.2f;

                var tangent1 = Vector2.Normalize(nodePos1B - nodePos1A);
                var tangent2 = Vector2.Normalize(nodePos2B - nodePos2A);
                var normal1 = new Vector2(-tangent1.Y, tangent1.X); //Left
                var normal2 = new Vector2(-tangent2.Y, tangent2.X); //Left

                //Testing
                //normal1 = new Vector2(-longTangent.Y, longTangent.X);
                //normal2 = normal1;
                //Testing

                var midNode1 = (nodePos1A + nodePos1B) * 0.5f;
                var midNode2 = (nodePos2A + nodePos2B) * 0.5f;

                var furthestLeft1 = MathF.Sqrt(minDistBetweenRiversSquared) - MathF.Sqrt(DistanceToLineSegmentSquared(midNode1, nodePos2A, nodePos2B, out var _, out var _));
                var normalOffset1 = _random.NextFloat(MathF.Max(furthestLeft1, -distance1), distance1);

                var furthestRight2 = MathF.Sqrt(DistanceToLineSegmentSquared(midNode2, nodePos1A, nodePos1B, out var _, out var _)) - MathF.Sqrt(minDistBetweenRiversSquared) + normalOffset1; //Make this dependent on how far left MidNode1 went.
                var normalOffset2 = _random.NextFloat(-distance2, MathF.Min(furthestRight2, distance2));

                midNode1 = midNode1 + normal1 * -normalOffset1;
                midNode2 = midNode2 + normal2 * -normalOffset2;

                tempList1.Add(nodePos1A);
                tempList1.Add(midNode1);

                tempList2.Add(nodePos2A);
                tempList2.Add(midNode2);

                if (i0 == Math.Min(nodePositions1.Count, nodePositions2.Count) - 2)
                {
                    tempList1.Add(nodePos1B);
                    tempList2.Add(nodePos2B);
                }

                if ((midNode1 - nodePos1A).LengthSquared() > maxDistanceSquared ||
                    (midNode1 - nodePos1B).LengthSquared() > maxDistanceSquared ||
                    (midNode2 - nodePos2A).LengthSquared() > maxDistanceSquared ||
                    (midNode2 - nodePos2B).LengthSquared() > maxDistanceSquared)
                {
                    isDone = false;
                }
            }
            nodePositions1.Clear();
            nodePositions1.AddRange(tempList1);
            nodePositions2.Clear();
            nodePositions2.AddRange(tempList2);
            tempList1.Clear();
            tempList2.Clear();

            safetyLimit--;
        }
        if (safetyLimit <= 0)
        {
            bool safetyLimitHit = true; //For breakpoints
        }

        PopulateRiverFromVector2List(mapId, nodePositions1);
        nodePositions2.Reverse();
        PopulateRiverFromVector2List(mapId, nodePositions2);

        _nodesChanged = true;
        return true;
    }

    public void PopulateRiverFromVector2List(MapId mapId, List<Vector2> nodePositions, Angle entryFlowDirection = default, Angle exitFlowDirection = default, bool customFlowDirs = false)
    {
        List<EntityUid> nodes1 = new();
        for (var i = 0; i < nodePositions.Count; i++)
        {
            if (i == 0)
            {
                if (!customFlowDirs)
                {
                    entryFlowDirection = Vector2.Normalize(nodePositions[i + 1] - nodePositions[i]).ToAngle();
                }
                var nodeCoord = new MapCoordinates(nodePositions[i], mapId);
                var newNode = Spawn("SpaceRiverNode", nodeCoord);

                if (!TryComp<RiverNodeComponent>(newNode, out var newComp))
                {
                    continue; //bad
                }

                newComp.FlowDirection = entryFlowDirection;
                newComp.ForwardControlPoint = entryFlowDirection.ToVec() * 50f; //Magic number TODO: Kill
                nodes1.Add(newNode);
            }
            else if (i == nodePositions.Count - 1)
            {
                if (!customFlowDirs)
                {
                    exitFlowDirection = Vector2.Normalize(nodePositions[i] - nodePositions[i - 1]).ToAngle();
                }
                var nodeCoord = new MapCoordinates(nodePositions[i], mapId);
                var newNode = Spawn("SpaceRiverNode", nodeCoord);

                if (!TryComp<RiverNodeComponent>(newNode, out var newComp) ||
                    !TryComp<RiverNodeComponent>(nodes1[i - 1], out var priorComp))
                {
                    continue; //bad
                }

                newComp.FlowDirection = exitFlowDirection;
                newComp.ForwardControlPoint = exitFlowDirection.ToVec() * 50f; //Magic number TODO: Kill
                newComp.PriorNodes.Add(GetNetEntity(nodes1[i - 1]));
                priorComp.NextNodes.Add(GetNetEntity(newNode));

                nodes1.Add(newNode);
            }
            else
            {
                var flowDirection = Vector2.Normalize(nodePositions[i + 1] - nodePositions[i - 1]);
                var nodeCoord = new MapCoordinates(nodePositions[i], mapId);
                var newNode = Spawn("SpaceRiverNode", nodeCoord);

                if (!TryComp<RiverNodeComponent>(newNode, out var newComp) ||
                    !TryComp<RiverNodeComponent>(nodes1[i - 1], out var priorComp))
                {
                    continue; //bad
                }

                newComp.FlowDirection = flowDirection.ToAngle();
                newComp.ForwardControlPoint = flowDirection * 50f; //Magic number TODO: Kill
                newComp.PriorNodes.Add(GetNetEntity(nodes1[i - 1]));
                priorComp.NextNodes.Add(GetNetEntity(newNode));

                nodes1.Add(newNode);
            }
        }
        foreach (var node in nodes1)
        {
            if (!TryComp<RiverNodeComponent>(node, out var comp))
            {
                continue;
            }
            Dirty(node, comp);
        }
    }

    public bool CreateRiver(MapId mapId, Vector2 startLocation, Vector2 endLocation)
    {
        //TODO: Make this something that's not horrible. *Very* placeholder, just draws long as lines for now.

        var startCoord = new MapCoordinates(startLocation, mapId);
        var endCoord = new MapCoordinates(endLocation, mapId);
        var startNode = Spawn("SpaceRiverNode", startCoord);
        var endNode = Spawn("SpaceRiverNode", endCoord);

        var flowDirection = Vector2.Normalize(endLocation - startLocation).ToAngle();
        if (!TryComp<RiverNodeComponent>(startNode, out var startComp)
            || !TryComp<RiverNodeComponent>(endNode, out var endComp))
        {
            return false;
        }

        startComp.FlowDirection = flowDirection;
        endComp.FlowDirection = flowDirection;

        startComp.ForwardControlPoint = startComp.FlowDirection.ToVec() * startComp.Boost * 50f;
        endComp.ForwardControlPoint = endComp.FlowDirection.ToVec() * endComp.Boost * 50f;

        startComp.NextNodes.Add(GetNetEntity(endNode));
        endComp.PriorNodes.Add(GetNetEntity(startNode));
        Dirty(startNode, startComp);
        Dirty(endNode, endComp);

        return true;
    }

    /// <summary>
    /// Filters out any POI with a [Hide] flag on its IFFComponent or on a different map. This is so that the Cove and Listening Point Bravo cannot be found through meta-knowledge
    /// of river generation, as well as automatically making future POIs that probably don't want to be found, not connect automatically to rivers.
    /// Hidden POIs with the ForceRiverConnectionComponent will still be added.
    /// </summary>
    /// <param name="mapId">The ID of the map on which the rivers should be generated</param>
    /// <param name="pois">A list of all POIs</param>
    /// <returns>A list of all POIs that don't have the [Hide] IFFFlag.</returns>
    private List<EntityUid> FilterValidPOIs(MapId mapId, List<EntityUid> pois)
    {
        List<EntityUid> validPois = new();
        foreach (var poi in pois)
        {
            if (!TryComp<IFFComponent>(poi, out var poiIFFComp))
            {
                continue;
            }
            var iffFlag = poiIFFComp.Flags;
            if ((!iffFlag.HasFlag(IFFFlags.Hide) || TryComp<ForceRiverConnectionComponent>(poi, out var _)) && _transform.GetMapId(poi) == mapId)
            {
                validPois.Add(poi);
            }
        }
        return validPois;
    }

    /// <summary>
    /// Gets the world positions of each POI(entity) in a list.
    /// </summary>
    /// <param name="pois">The list of POI EntityUids whose positions are to be retrieved.</param>
    /// <returns>A list of WorldPosition vectors of the inserted POIs</returns>
    private List<Vector2> GetPOIPositions(List<EntityUid> pois)
    {
        List<Vector2> poiCoordinates = new();
        foreach (var poi in pois)
        {
            poiCoordinates.Add(_transform.GetWorldPosition(poi));
        }
        return poiCoordinates;
    }

    /// <summary>
    /// Checks if two Connections (Segments) intersect anywhere other than at their vertex points.
    /// </summary>
    /// <param name="line1">First line</param>
    /// <param name="line2">Second line</param>
    /// <returns>True if the Connections intersect anywhere other than their vertex points, else returns false.</returns>
    private bool CheckIntersect(Connection line1, Connection line2)
    {
        var dX1 = line1.End.X - line1.Start.X;
        var dX2 = line2.End.X - line2.Start.X;
        var dY1 = line1.End.Y - line1.Start.Y;
        var dY2 = line2.End.Y - line2.Start.Y;

        var p0 = dY2 * (line2.End.X - line1.Start.X) - dX2 * (line2.End.Y - line1.Start.Y);
        var p1 = dY2 * (line2.End.X - line1.End.X) - dX2 * (line2.End.Y - line1.End.Y);
        var p2 = dY1 * (line1.End.X - line2.Start.X) - dX1 * (line1.End.Y - line2.Start.Y);
        var p3 = dY1 * (line1.End.X - line2.End.X) - dX1 * (line1.End.Y - line2.End.Y);

        return (p0 * p1 < 0 && p2 * p3 < 0);
    }

    /// <summary>
    /// Checks if a position is within a Connections list.
    /// </summary>
    /// <param name="position">The position you wish to check is in the list</param>
    /// <param name="connections">The list you wish to check</param>
    /// <returns>Whether the position is in the list (true) or not (false)</returns>
    private bool ContainsPosition(Vector2 position, List<Connection> connections)
    {
        foreach (var connection in connections)
        {
            if (connection.Start == position || connection.End == position)
            {
                return true;
            }
        }

        return false;
    }
}
