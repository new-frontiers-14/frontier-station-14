using Content.Server._NF.River.Components;
using Content.Shared.Destructible;
using Content.Shared.Shuttles.Components;
using Lidgren.Network;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Server._NF.River.Systems;

public partial class RiverNodeSystem
{
    private struct Connection
    {
        public Connection(EntityUid start, EntityUid end)
        {
            Start = start;
            End = end;
        }
        public EntityUid Start { get; set; }
        public EntityUid End { get; set; }

        public override string ToString() => $"({Start}, {End})";
    }
    public void POIArterialRiverGen(MapId mapId, List<EntityUid> pois)
    {
        // There's a *lot* of loops in this, this can probably be optimised, but it's only run once, so this will do for now.
        var unitDistance = 3000f; //2500 or 3000? TODO: make this not a magic number.
        var validPois = FilterValidPOIs(mapId, pois);
        var poiPositions = GetPOIPositions(validPois);
        poiPositions.Add(new Vector2(0f, 0f)); // Adds Frontier Station
        List<EntityUid> wells = new();

        foreach (var poi in poiPositions)
        {
            var random = _random.Next(2);
            var spin = Chirality.Counterclockwise;
            if (random == 0)
            {
                //Commented out for testing TODO: PUT BACK|spin = Chirality.Clockwise;
            }
            wells.Add(CreateWell(mapId, poi, 200f, spin));
        }

        // Connect all the POIs that are in range of eachother
        List<Connection> connections = new();

        List<EntityUid> handledWells = new();
        List<EntityUid> unconnectedWells = new();
        foreach (var well in wells)
        {
            handledWells.Add(well);
            var connected = false;
            foreach (var otherWell in wells)
            {
                if (handledWells.Contains(otherWell))
                {
                    continue;
                }
                var wellPosition = _transform.GetWorldPosition(well);
                var otherWellPosition = _transform.GetWorldPosition(otherWell);
                if ((wellPosition - otherWellPosition).Length() < unitDistance)
                {
                    var tooCloseToPoi = false;
                    foreach (var distanceCheckWell in wells)
                    {
                        if (distanceCheckWell == well || distanceCheckWell == otherWell)
                        {
                            continue;
                        }
                        var distanceCheckWellPos = _transform.GetWorldPosition(distanceCheckWell);
                        if (DistanceToLineSegmentSquared(distanceCheckWellPos, wellPosition, otherWellPosition, out var _, out var t) < 300f || t > 1f || t < 0f) //300f is pretty arbitrary, but 200 radius + 100 buffer.
                        {
                            tooCloseToPoi = true;
                            break;
                        }
                    }
                    if (!tooCloseToPoi)
                    {
                        connections.Add(new Connection(well, otherWell));
                        connected = true;
                    }
                }
            }
            if (!connected && !ContainsWell(well, connections))
            {
                unconnectedWells.Add(well);
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
        List<List<EntityUid>> networks = new();
        foreach (var well in unconnectedWells)
        {
            List<EntityUid> tinyNetwork = new();
            tinyNetwork.Add(well);
            networks.Add(tinyNetwork);
        }
        handledWells.Clear();
        handledWells.AddRange(unconnectedWells);

        var safetyLimit = 100;
        foreach (var well in wells)
        {
            if (handledWells.Contains(well))
            {
                continue;
            }
            handledWells.Add(well);
            List<EntityUid> wellInNetwork = new();
            wellInNetwork.Add(well);
            var networkFilled = false;
            while (!networkFilled && safetyLimit > 0)
            {
                var somethingAdded = false;
                foreach (var connection in validConnections)
                {
                    if (wellInNetwork.Contains(connection.Start) && !wellInNetwork.Contains(connection.End))
                    {
                        wellInNetwork.Add(connection.End);
                        if (!handledWells.Contains(connection.End))
                        {
                            handledWells.Add(connection.End);
                        }
                        somethingAdded = true;
                    }
                    else if (wellInNetwork.Contains(connection.End) && !wellInNetwork.Contains(connection.Start))
                    {
                        wellInNetwork.Add(connection.Start);
                        if (!handledWells.Contains(connection.Start))
                        {
                            handledWells.Add(connection.Start);
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
            networks.Add(wellInNetwork);
        }

        //Handle and consolidate separate networks.
        safetyLimit = 100;
        while (networks.Count > 1 && safetyLimit > 0)
        {
            // focusNetwork should be (one of) the largest one(s) to make connections more convenient.
            var focusNetwork = new List<EntityUid>();
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
                foreach (var well in network)
                {
                    averagePos = averagePos + _transform.GetWorldPosition(well);
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
            EntityUid closestFocusWell = new();
            EntityUid closestOtherWell = new();
            var closestDistance = -1f;
            for (var i = 0; i < focusNetwork.Count; i++)
            {
                for (var j = 0; j < closestNetwork.Count; j++)
                {
                    if (closestDistance == -1)
                    {
                        closestFocusWell = focusNetwork[i];
                        closestOtherWell = closestNetwork[j];
                        var focusWellPos = _transform.GetWorldPosition(closestFocusWell);
                        var otherWellPos = _transform.GetWorldPosition(closestOtherWell);
                        closestDistance = (otherWellPos - focusWellPos).Length();
                    }
                    else
                    {
                        var focusPos = _transform.GetWorldPosition(focusNetwork[i]);
                        var closestPos = _transform.GetWorldPosition(closestNetwork[j]);
                        var distance = (closestPos - focusPos).Length();
                        if (distance < closestDistance)
                        {
                            closestFocusWell = focusNetwork[i];
                            closestOtherWell = closestNetwork[j];
                            closestDistance = distance;
                        }
                    }
                }
            }
            // Make a connection, and merge the two networks in the "networks" list.
            validConnections.Add(new Connection(closestFocusWell, closestOtherWell));
            networks[largestNetworkIndex].AddRange(closestNetwork);
            networks.Remove(closestNetwork);

            safetyLimit--;
        }

        // Inform the wells of which wells they're connected to.
        foreach (var connection in validConnections)
        {
            if (!TryComp<RiverWellComponent>(connection.Start, out var startWellComp) ||
                !TryComp<RiverWellComponent>(connection.End, out var endWellComp))
            {
                continue;
            }

            if (!startWellComp.ConnectedWells.Contains(connection.End))
            {
                startWellComp.ConnectedWells.Add(connection.End);
            }
            if (!endWellComp.ConnectedWells.Contains(connection.Start))
            {
                endWellComp.ConnectedWells.Add(connection.Start);
            }
        }

        OrganiseWells(wells);

        // Create the actual rivers.
        foreach (var connection in validConnections)
        {
            CreateRiverPair(mapId, connection.Start, connection.End);
        }
    }

    public bool CreateRiverPair(MapId mapId, EntityUid well1, EntityUid well2, Angle flowDirection1 = default, Angle flowDirection2 = default, bool customFlowDirs = false)
    {
        if (!TryComp<RiverWellComponent>(well1, out var wellComp1) ||
            !TryComp<RiverWellComponent>(well2, out var wellComp2))
        {
            return false;
        }

        var crossRivers = wellComp1.FlowRotation != wellComp2.FlowRotation;

        var minDistBetweenRivers = 40f; // the default Node Range
        var maxDistance = 350f;

        var location1 = _transform.GetWorldPosition(well1);
        var location2 = _transform.GetWorldPosition(well2);

        var maxDistanceSquared = MathF.Pow(maxDistance, 2f);
        var minDistBetweenRiversSquared = MathF.Pow(minDistBetweenRivers, 2f);

        List<Vector2> nodePositionsRight = new();
        List<Vector2> nodePositionsLeft = new();

        var longTangent = Vector2.Normalize(location2 - location1);
        var longNormal = new Vector2(-longTangent.Y, longTangent.X); //Left?
        var reverseLongTangent = Vector2.Normalize(location1 - location2);
        var connectionDirection1 = longTangent;
        var connectionDirection2 = reverseLongTangent;

        // Average out connection points that are close to eachother. So if two connected wells are in roughly the same direction, have the arrival and departure be the same for them.



        Vector2 offsetLoc1Arrival = new();
        Vector2 offsetLoc2Departure = new();
        Vector2 offsetLoc1Departure = new();
        Vector2 offsetLoc2Arrival = new();

        Angle departureAngle1;
        Angle arrivalAngle1;
        if (wellComp1.FlowRotation == Chirality.Clockwise)
        {
            departureAngle1 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection1).Degrees - 45d).Reduced(); // 45 degrees clockwise
            arrivalAngle1 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection1).Degrees + 225d).Reduced(); // 180 + 45 degrees counter-clockwise
        }
        else
        {
            departureAngle1 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection1).Degrees + 45d).Reduced(); // 45 degrees counter-clockwise
            arrivalAngle1 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection1).Degrees - 225d).Reduced(); // 180 + 45 degrees clockwise
        }

        // Get the river positions at Well 1.
        var closestDepartureAngle1 = new Angle();
        var closestArrivalAngle1 = new Angle();
        var arrivalNode1 = EntityUid.Invalid;
        var departureNode1 = EntityUid.Invalid;
        foreach (var node in wellComp1.WellNodes)
        {
            if (!TryComp<RiverNodeComponent>(node, out var nodeComp))
            {
                continue;
            }

            var departureDistance = Math.Abs(Angle.ShortestDistance(departureAngle1, nodeComp.FlowDirection.Reduced()).Degrees);
            var arrivalDistance = Math.Abs(Angle.ShortestDistance(arrivalAngle1, nodeComp.FlowDirection.Reduced()).Degrees);
            if (departureDistance < closestDepartureAngle1 || offsetLoc1Departure == new Vector2(0f, 0f))
            {
                offsetLoc1Departure = _transform.GetWorldPosition(node);
                departureNode1 = node;
                closestDepartureAngle1 = departureDistance;
            }
            else if (arrivalDistance < closestArrivalAngle1 || offsetLoc1Arrival == new Vector2(0f, 0f))
            {
                offsetLoc1Arrival = _transform.GetWorldPosition(node);
                arrivalNode1 = node;
                closestArrivalAngle1 = arrivalDistance;
            }
        }

        var checkNode = departureNode1;
        while (checkNode != arrivalNode1)
        {
            if (!TryComp<RiverNodeComponent>(checkNode, out var checkComp))
            {
                break;
            }
            var safetyCheck = checkNode;
            if (checkComp.PriorNodes.Count > 1) // New Departure point
            {
                offsetLoc1Departure = _transform.GetWorldPosition(checkNode);
            }
            if (checkComp.NextNodes.Count > 1) // New Arrival point
            {
                offsetLoc1Arrival = _transform.GetWorldPosition(checkNode);
            }
            foreach (var node in GetEntityList(checkComp.NextNodes))
            {
                if (wellComp1.WellNodes.Contains(node))
                {
                    checkNode = node;
                }
            }

            // Make sure the loop doesn't break.
            if (checkNode == safetyCheck || checkNode == departureNode1 || !wellComp1.WellNodes.Contains(checkNode))
            {
                break;
            }
        }

        Angle departureAngle2;
        Angle arrivalAngle2;
        if (wellComp2.FlowRotation == Chirality.Clockwise)
        {
            departureAngle2 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection2).Degrees - 45d).Reduced(); // 45 degrees counter-clockwise
            arrivalAngle2 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection2).Degrees + 225d).Reduced(); // 180 + 45 degrees clockwise
        }
        else
        {
            departureAngle2 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection2).Degrees + 45d).Reduced(); // 45 degrees clockwise
            arrivalAngle2 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection2).Degrees - 225d).Reduced(); // 180 + 45 degrees counter-clockwise
        }

        // Get the river positions at Well 2.
        var closestDepartureAngle2 = new Angle();
        var closestArrivalAngle2 = new Angle();
        var arrivalNode2 = EntityUid.Invalid;
        var departureNode2 = EntityUid.Invalid;
        foreach (var node in wellComp2.WellNodes)
        {
            if (!TryComp<RiverNodeComponent>(node, out var nodeComp))
            {
                continue;
            }

            var departureDistance = Math.Abs(Angle.ShortestDistance(departureAngle2, nodeComp.FlowDirection.Reduced()).Degrees);
            var arrivalDistance = Math.Abs(Angle.ShortestDistance(arrivalAngle2, nodeComp.FlowDirection.Reduced()).Degrees);
            if (departureDistance < closestDepartureAngle2 || offsetLoc2Departure == new Vector2(0f, 0f))
            {
                offsetLoc2Departure = _transform.GetWorldPosition(node);
                departureNode2 = node;
                closestDepartureAngle2 = departureDistance;
            }
            else if (arrivalDistance < closestArrivalAngle2 || offsetLoc2Arrival == new Vector2(0f, 0f))
            {
                offsetLoc2Arrival = _transform.GetWorldPosition(node);
                arrivalNode2 = node;
                closestArrivalAngle2 = arrivalDistance;
            }
        }

        checkNode = departureNode2;
        while (checkNode != arrivalNode2)
        {
            if (!TryComp<RiverNodeComponent>(checkNode, out var checkComp))
            {
                break;
            }
            var safetyCheck = checkNode;
            if (checkComp.PriorNodes.Count > 1) // New Departure point
            {
                offsetLoc2Departure = _transform.GetWorldPosition(checkNode);
            }
            if (checkComp.NextNodes.Count > 1) // New Arrival point
            {
                offsetLoc2Arrival = _transform.GetWorldPosition(checkNode);
            }
            foreach (var node in GetEntityList(checkComp.NextNodes))
            {
                if (wellComp2.WellNodes.Contains(node))
                {
                    checkNode = node;
                }
            }

            // Make sure the loop doesn't break.
            if (checkNode == safetyCheck || checkNode == departureNode2 || !wellComp2.WellNodes.Contains(checkNode))
            {
                break;
            }
        }
        if (wellComp1.Connections.ContainsKey(well2))
        {
            offsetLoc1Departure = _transform.GetWorldPosition(wellComp1.Connections[well2].DepartureNode);
            offsetLoc1Arrival = _transform.GetWorldPosition(wellComp1.Connections[well2].ArrivalNode);
        }
        else
        {
            offsetLoc1Departure = location1;
            offsetLoc1Departure.Y += 100f;
            offsetLoc1Arrival = location1;
            offsetLoc1Arrival.Y += 100f;

        }
        if (wellComp2.Connections.ContainsKey(well1))
        {
            offsetLoc2Arrival = _transform.GetWorldPosition(wellComp2.Connections[well1].ArrivalNode);
            offsetLoc2Departure = _transform.GetWorldPosition(wellComp2.Connections[well1].DepartureNode);
        }
        else
        {
            offsetLoc2Departure = location2;
            offsetLoc2Departure.X += 100f;
            offsetLoc2Arrival = location2;
            offsetLoc2Arrival.X += 100f;
        }

        nodePositionsRight.Add(offsetLoc1Departure);
        nodePositionsRight.Add(offsetLoc2Arrival);
        nodePositionsLeft.Add(offsetLoc1Arrival);
        nodePositionsLeft.Add(offsetLoc2Departure);

        var locationDistanceSquared = (location1 - location2).LengthSquared();

        if (locationDistanceSquared < maxDistanceSquared) //Does not support tiny rivers.
        {
            return false;
        }

        int segmentNumber = (int)MathF.Sqrt(locationDistanceSquared / maxDistanceSquared);
        for (int i = 1; i <= segmentNumber; i *= 2)
        {

            var tempList1 = new List<Vector2>();
            var tempList2 = new List<Vector2>();
            for (var i0 = 0; i0 < Math.Min(nodePositionsRight.Count, nodePositionsLeft.Count) - 1; i0++) //1To2 and 2To1 should always be equal, but in case they aren't, this prevents over-running the lists.
            {
                var i1 = i0 + 1;
                var nodePos1A = nodePositionsRight[i0];
                var nodePos1B = nodePositionsRight[i1];
                var nodePos2A = nodePositionsLeft[i0];
                var nodePos2B = nodePositionsLeft[i1];

                var distance1 = (nodePos1B - nodePos1A).Length() * 0.2f;
                var distance2 = (nodePos2B - nodePos2A).Length() * 0.2f;

                var tangent1 = Vector2.Normalize(nodePos1B - nodePos1A);
                var tangent2 = Vector2.Normalize(nodePos2B - nodePos2A);
                var normal1 = new Vector2(-tangent1.Y, tangent1.X); //Left
                var normal2 = new Vector2(-tangent2.Y, tangent2.X); //Left

                var midNode1 = (nodePos1A + nodePos1B) * 0.5f;
                var midNode2 = (nodePos2A + nodePos2B) * 0.5f;

                var furthestLeft1 = minDistBetweenRivers * 2f - MathF.Sqrt(DistanceToLineSegmentSquared(midNode1, nodePos2A, nodePos2B, out var _, out var _));
                var normalOffset1 = _random.NextFloat(MathF.Max(furthestLeft1, -distance1), distance1);

                var furthestRight2 = MathF.Sqrt(DistanceToLineSegmentSquared(midNode2, nodePos1A, nodePos1B, out var _, out var _)) - minDistBetweenRivers * 2f + normalOffset1; //Make this dependent on how far left MidNode1 went.
                var normalOffset2 = _random.NextFloat(-distance2, MathF.Min(furthestRight2, distance2));

                midNode1 = midNode1 + normal1 * -normalOffset1;
                midNode2 = midNode2 + normal2 * -normalOffset2;

                tempList1.Add(nodePos1A);
                tempList1.Add(midNode1);

                tempList2.Add(nodePos2A);
                tempList2.Add(midNode2);

                if (i0 == Math.Min(nodePositionsRight.Count, nodePositionsLeft.Count) - 2)
                {
                    tempList1.Add(nodePos1B);
                    tempList2.Add(nodePos2B);
                }
            }
            nodePositionsRight.Clear();
            nodePositionsRight.AddRange(tempList1);
            nodePositionsLeft.Clear();
            nodePositionsLeft.AddRange(tempList2);
            tempList1.Clear();
            tempList2.Clear();
        }

        PopulateRiverFromVector2List(mapId, nodePositionsRight);
        nodePositionsLeft.Reverse();
        PopulateRiverFromVector2List(mapId, nodePositionsLeft);

        _nodesChanged = true;
        return true;
    }

    public void PopulateRiverFromVector2List(MapId mapId, List<Vector2> nodePositions, Angle entryFlowDirection = default, Angle exitFlowDirection = default, bool customFlowDirs = false)
    {
        //TODO: Polish the shit out of this. Currently nodes connect to nodes that don't face the right way and stuff like that.
        List<EntityUid> nodes1 = new();
        for (var i = 0; i < nodePositions.Count; i++)
        {
            Angle flowDirection;
            var priorNode = EntityUid.Invalid;

            // Handle the flowDirection of this node.
            if (i > 0)
            {
                priorNode = nodes1[i - 1];
                if (i == nodePositions.Count - 1)
                {
                    if (!customFlowDirs)
                    {
                        flowDirection = Angle.FromWorldVec(Vector2.Normalize(nodePositions[i] - nodePositions[i - 1]));
                    }
                    else
                    {
                        flowDirection = exitFlowDirection;
                    }
                }
                else
                {
                    flowDirection = Angle.FromWorldVec(Vector2.Normalize(nodePositions[i + 1] - nodePositions[i - 1]));
                }
            }
            else
            {
                if (!customFlowDirs)
                {
                    flowDirection = Angle.FromWorldVec(Vector2.Normalize(nodePositions[i + 1] - nodePositions[i]));
                }
                else
                {
                    flowDirection = entryFlowDirection;
                }
            }

            var nodeCoord = new MapCoordinates(nodePositions[i], mapId);

            // Check if nodes should be merged with nodes from other rivers.
            var nearbyNodes = _lookup.GetEntitiesInRange<RiverNodeComponent>(nodeCoord, 100f); //Magic number, TODO: KILL
            var mergeNode = EntityUid.Invalid;
            var mergeNodeDistanceSquared = -1f;
            if (i == 0 || i == nodePositions.Count - 1) // Test TODO: REMOVE
            {
                foreach (EntityUid node in nearbyNodes)
                {
                    if (node == priorNode || !TryComp<RiverNodeComponent>(node, out var nearbyComp))
                    {
                        continue;
                    }

                    var angleDelta = Math.Abs(Angle.ShortestDistance(flowDirection, nearbyComp.FlowDirection));
                    var nodeDistanceSquared = (_transform.GetWorldPosition(node) - nodePositions[i]).LengthSquared();
                    var isBetterFit = false;
                    if (mergeNodeDistanceSquared == -1f || nodeDistanceSquared < mergeNodeDistanceSquared)
                    {
                        isBetterFit = true;
                    }
                    if (angleDelta < Angle.FromDegrees(90d) && isBetterFit)
                    {
                        mergeNodeDistanceSquared = nodeDistanceSquared;
                        mergeNode = node;
                    }
                }
            }

            // If relevant, create a new node, and connect nodes to form a river segment.
            if (mergeNode == EntityUid.Invalid)
            {
                var newNode = Spawn("SpaceRiverNode", nodeCoord);

                if (!TryComp<RiverNodeComponent>(newNode, out var newComp))
                {
                    continue; //bad
                }

                newComp.FlowDirection = flowDirection;
                if (i == 0)
                {
                    newComp.ForwardControlPoint = (nodePositions[i + 1] - nodePositions[i]) * 0.25f;
                }
                mergeNode = newNode;
            }

            if (i > 0)
            {
                ConnectNodes(priorNode, mergeNode);
            }

            nodes1.Add(mergeNode);
        }
    }

    public void ConnectNodes(EntityUid sourceNode, EntityUid endNode)
    {
        if (!TryComp<RiverNodeComponent>(sourceNode, out var sourceComp) ||
            !TryComp<RiverNodeComponent>(endNode, out var endComp))
        {
            return;
        }
        ConnectNodes(sourceNode, sourceComp, endNode, endComp);
    }

    public void ConnectNodes(EntityUid sourceNode, RiverNodeComponent sourceComp, EntityUid endNode, RiverNodeComponent endComp)
    {
        var sourcePos = _transform.GetWorldPosition(sourceNode);
        var endPos = _transform.GetWorldPosition(endNode);

        var distance = (endPos - sourcePos).Length();

        if (sourceComp.ForwardControlPoint.LengthSquared() < 1f)
        {
            sourceComp.ForwardControlPoint = sourceComp.FlowDirection.ToWorldVec() * (distance * 0.25f);
        }
        if (endComp.ForwardControlPoint.LengthSquared() < 1f)
        {
            endComp.ForwardControlPoint = endComp.FlowDirection.ToWorldVec() * (distance * 0.25f);
        }

        var netSourceNode = GetNetEntity(sourceNode);
        var netEndNode = GetNetEntity(endNode);

        if (!endComp.PriorNodes.Contains(netSourceNode))
        {
            endComp.PriorNodes.Add(netSourceNode);
        }
        if (!sourceComp.NextNodes.Contains(netEndNode))
        {
            sourceComp.NextNodes.Add(netEndNode);
        }

        Dirty(sourceNode, sourceComp);
        Dirty(endNode, endComp);
    }

    /// <summary>
    /// Creates a circular river around a location
    /// </summary>
    /// <param name="mapId">The map the well should spawn on</param>
    /// <param name="location">The location around which the well should be centred</param>
    /// <param name="radius">How wide the well should be</param>
    /// <param name="clockwise">The flow is clockwise if true, or counterclockwise if false</param>
    public EntityUid CreateWell(MapId mapId, Vector2 location, float radius, Chirality spin)
    {
        var well = Spawn("SpaceRiverWell", new MapCoordinates(location, mapId));

        if (!TryComp<RiverWellComponent>(well, out var wellComp))
        {
            return EntityUid.Invalid; // bad
        }
        wellComp.FlowRotation = spin;
        wellComp.Radius = radius;

        var nodeAmount = 16;
        var randomRotation = _random.NextAngle();
        double angleDiff = 360 / nodeAmount;
        var nodes = new EntityUid[nodeAmount];
        var components = new RiverNodeComponent[nodeAmount];
        var chiralityMod = 1;
        if (spin == Chirality.Clockwise)
        {
            chiralityMod = -1;
        }
        for (var i = 0; i < nodeAmount; i++)
        {
            var nodeProto = (Angle.FromDegrees(i * angleDiff * chiralityMod) + randomRotation).Reduced();

            var nodePos = location + nodeProto.ToWorldVec() * radius;

            var nodeCoord = new MapCoordinates(nodePos, mapId);

            nodes[i] = Spawn("SpaceRiverNode", nodeCoord);
            wellComp.WellNodes.Add(nodes[i]);

            if (!TryComp<RiverNodeComponent>(nodes[i], out var comp))
            {
                return EntityUid.Invalid; //bad
            }
            components[i] = comp;


            components[i].FlowDirection = nodeProto + Angle.FromDegrees(90d * chiralityMod);

            components[i].ForwardControlPoint = components[i].FlowDirection.ToWorldVec() * radius * (2f / (float)nodeAmount);

        }

        for (var j = 0; j < nodeAmount; j++)
        {
            if (j == 0)
            {
                components[j].PriorNodes.Add(GetNetEntity(nodes[nodeAmount - 1]));
            }
            else
            {
                components[j].PriorNodes.Add(GetNetEntity(nodes[j - 1]));
            }

            if (j == nodeAmount - 1)
            {
                components[j].NextNodes.Add(GetNetEntity(nodes[0]));
            }
            else
            {
                components[j].NextNodes.Add(GetNetEntity(nodes[j + 1]));
            }

            Dirty(nodes[j], components[j]);
        }
        return well;
    }

    public void OrganiseWells(List<EntityUid> wells)
    {
        foreach (var well in wells)
        {
            //Get the angles of all the connected wells in order, handle each well with relation to its neighbouring wells, merge them if their angles are too shallow.
            if (!TryComp<RiverWellComponent>(well, out var wellComp))
            {
                continue;
            }
            var wellPos = _transform.GetWorldPosition(well);

            // Sort connections in order, from left to right/counter-clockwise to clockwise.
            List<EntityUid> sortedConnectedWells = new();
            foreach (var connection in wellComp.ConnectedWells)
            {
                var connectionDirection = Angle.FromWorldVec(_transform.GetWorldPosition(connection) - wellPos);
                if (sortedConnectedWells.Count == 0)
                {
                    sortedConnectedWells.Add(connection);
                }
                else
                {
                    var counter = 0;
                    var insert = false;
                    foreach (var otherConnection in sortedConnectedWells)
                    {
                        var otherConnectionDir = Angle.FromWorldVec(_transform.GetWorldPosition(otherConnection) - wellPos);
                        if (Angle.ShortestDistance(connectionDirection, otherConnectionDir) < 0d) //Is otherConnection further right than Connection?
                        {
                            insert = true;
                            break;
                        }
                        counter++;
                    }
                    if (insert)
                    {
                        sortedConnectedWells.Insert(counter, connection);
                        //sortedConnectedWells[counter] = connection;
                    }
                    else
                    {
                        sortedConnectedWells.Add(connection);
                    }
                }
            }

            for (var i = 0; i < sortedConnectedWells.Count; i++)
            {
                var focusConnection = sortedConnectedWells[i];
                var focusPos = _transform.GetWorldPosition(focusConnection);
                var focusTangent = Angle.FromWorldVec(focusPos - wellPos);
                Angle leftRiverAngle = new();
                Angle rightRiverAngle = new();
                if (sortedConnectedWells.Count == 1)
                {
                    leftRiverAngle = Angle.FromDegrees(focusTangent.Degrees + 90d);
                    rightRiverAngle = Angle.FromDegrees(focusTangent.Degrees - 90d);
                }
                else
                {
                    //Check against neighbouring tangents, assign good arrival and departure thingies.

                    //Left
                    var iLeft = i - 1;
                    if (iLeft < 0)
                    {
                        iLeft = sortedConnectedWells.Count - 1;
                    }
                    var leftConnection = sortedConnectedWells[iLeft];
                    var leftPos = _transform.GetWorldPosition(leftConnection);
                    var leftTangent = Angle.FromWorldVec(leftPos - wellPos);
                    leftRiverAngle = Angle.ShortestDistance(focusTangent, leftTangent) * 0.5d + focusTangent;

                    if (leftRiverAngle > Angle.FromDegrees(focusTangent.Degrees + 90d) || leftRiverAngle < focusTangent)
                    {
                        leftRiverAngle = Angle.FromDegrees(focusTangent.Degrees + 90d);
                    }

                    //Right
                    var iRight = i + 1;
                    if (iRight >= sortedConnectedWells.Count)
                    {
                        iRight = 0;
                    }

                    var rightConnection = sortedConnectedWells[iRight];
                    var rightPos = _transform.GetWorldPosition(rightConnection);
                    var rightTangent = Angle.FromWorldVec(rightPos - wellPos);
                    rightRiverAngle = Angle.ShortestDistance(focusTangent, rightTangent) * 0.5d + focusTangent;

                    if (rightRiverAngle < Angle.FromDegrees(focusTangent.Degrees - 90d) || rightRiverAngle > focusTangent)
                    {
                        rightRiverAngle = Angle.FromDegrees(focusTangent.Degrees - 90d);
                    }
                }

                // Get the nodes at the left and right RiverAngle.
                var closestLeftAngle = new Angle();
                var closestRightAngle = new Angle();
                var leftNode = EntityUid.Invalid;
                var rightNode = EntityUid.Invalid;
                foreach (var node in wellComp.WellNodes)
                {
                    var nodeAngle = Angle.FromWorldVec(_transform.GetWorldPosition(node) - wellPos);

                    var leftDistance = Math.Abs(Angle.ShortestDistance(leftRiverAngle, nodeAngle));
                    if (leftDistance < closestLeftAngle || leftNode == EntityUid.Invalid)
                    {
                        leftNode = node;
                        closestLeftAngle = leftDistance;
                    }

                    var rightDistance = Math.Abs(Angle.ShortestDistance(rightRiverAngle, nodeAngle));
                    if (rightDistance < closestRightAngle || rightNode == EntityUid.Invalid)
                    {
                        rightNode = node;
                        closestRightAngle = rightDistance;
                    }
                }

                if (wellComp.FlowRotation == Chirality.Clockwise)
                {
                    //if (!wellComp.Connections.TryAdd(leftConnection, new ArrivalDeparturePair(leftNode, EntityUid.Invalid)))
                    //{
                    //    var tempLeftConnection = wellComp.Connections[leftConnection];
                    //    tempLeftConnection.ArrivalNode = leftNode;
                    //    wellComp.Connections[leftConnection] = tempLeftConnection;
                    //}

                    wellComp.Connections.Add(focusConnection, new ArrivalDeparturePair(rightNode, leftNode));
                    //if (!wellComp.Connections.TryAdd(rightConnection, new ArrivalDeparturePair(EntityUid.Invalid, rightNode)))
                    //{
                    //    var tempRightConnection = wellComp.Connections[rightConnection];
                    //    tempRightConnection.DepartureNode = rightNode;
                    //    wellComp.Connections[rightConnection] = tempRightConnection;
                    //}
                }
                else
                {
                    //if (!wellComp.Connections.TryAdd(leftConnection, new ArrivalDeparturePair(EntityUid.Invalid, leftNode)))
                    //{
                    //    var tempLeftConnection = wellComp.Connections[leftConnection];
                    //    tempLeftConnection.DepartureNode = leftNode;
                    //    wellComp.Connections[leftConnection] = tempLeftConnection;
                    //}
                    wellComp.Connections.Add(focusConnection, new ArrivalDeparturePair(leftNode, rightNode));
                    //if (!wellComp.Connections.TryAdd(rightConnection, new ArrivalDeparturePair(rightNode, EntityUid.Invalid)))
                    //{
                    //    var tempRightConnection = wellComp.Connections[rightConnection];
                    //    tempRightConnection.ArrivalNode = rightNode;
                    //    wellComp.Connections[rightConnection] = tempRightConnection;
                    //}
                }
            }
        }
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
        var line1Start = _transform.GetWorldPosition(line1.Start);
        var line1End = _transform.GetWorldPosition(line1.End);
        var line2Start = _transform.GetWorldPosition(line2.Start);
        var line2End = _transform.GetWorldPosition(line2.End);

        var dX1 = line1End.X - line1Start.X;
        var dX2 = line2End.X - line2Start.X;
        var dY1 = line1End.Y - line1Start.Y;
        var dY2 = line2End.Y - line2Start.Y;

        var p0 = dY2 * (line2End.X - line1Start.X) - dX2 * (line2End.Y - line1Start.Y);
        var p1 = dY2 * (line2End.X - line1End.X) - dX2 * (line2End.Y - line1End.Y);
        var p2 = dY1 * (line1End.X - line2Start.X) - dX1 * (line1End.Y - line2Start.Y);
        var p3 = dY1 * (line1End.X - line2End.X) - dX1 * (line1End.Y - line2End.Y);

        return (p0 * p1 < 0 && p2 * p3 < 0);
    }

    /// <summary>
    /// Checks if a position is within a Connections list.
    /// </summary>
    /// <param name="position">The position you wish to check is in the list</param>
    /// <param name="connections">The list you wish to check</param>
    /// <returns>Whether the position is in the list (true) or not (false)</returns>
    private bool ContainsWell(EntityUid well, List<Connection> connections)
    {
        foreach (var connection in connections)
        {
            if (connection.Start == well || connection.End == well)
            {
                return true;
            }
        }

        return false;
    }
}
