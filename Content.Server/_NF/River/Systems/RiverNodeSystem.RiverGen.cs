using Content.Server._NF.River.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Collision.Shapes;
using System.Numerics;

namespace Content.Server._NF.River.Systems;

public partial class RiverNodeSystem
{
    public bool CalculateBoundingAngles(EntityUid well1, EntityUid well2, out Angle well1CCWise, out Angle well1CWise, out Angle well2CCWise, out Angle well2CWise)
    {
        var well1Pos = _transform.GetWorldPosition(well1);
        var well2Pos = _transform.GetWorldPosition(well2);
        var connectionAngle1 = Angle.FromWorldVec(well2Pos - well1Pos);
        var connectionAngle2 = Angle.FromWorldVec(well1Pos - well2Pos);

        well1CCWise = connectionAngle1 + Angle.FromDegrees(45d);
        well1CWise = connectionAngle1 + Angle.FromDegrees(-45d);
        well2CCWise = connectionAngle2 + Angle.FromDegrees(45d);
        well2CWise = connectionAngle2 + Angle.FromDegrees(-45d);

        if (!TryComp<RiverWellComponent>(well1, out var comp1) ||
            !TryComp<RiverWellComponent>(well2, out var comp2))
        {
            return false;
        }

        if (comp1.FlowRotation == Chirality.Clockwise)
        {
            well1CCWise = Angle.FromWorldVec(_transform.GetWorldPosition(comp1.Connections[well2].DepartureNode) - well1Pos);
            well1CWise = Angle.FromWorldVec(_transform.GetWorldPosition(comp1.Connections[well2].ArrivalNode) - well1Pos);
        }
        else
        {
            well1CCWise = Angle.FromWorldVec(_transform.GetWorldPosition(comp1.Connections[well2].ArrivalNode) - well1Pos);
            well1CWise = Angle.FromWorldVec(_transform.GetWorldPosition(comp1.Connections[well2].DepartureNode) - well1Pos);
        }

        if (comp2.FlowRotation == Chirality.Clockwise)
        {
            well2CCWise = Angle.FromWorldVec(_transform.GetWorldPosition(comp2.Connections[well1].DepartureNode) - well2Pos);
            well2CWise = Angle.FromWorldVec(_transform.GetWorldPosition(comp2.Connections[well1].ArrivalNode) - well2Pos);
        }
        else
        {
            well2CCWise = Angle.FromWorldVec(_transform.GetWorldPosition(comp2.Connections[well1].ArrivalNode) - well2Pos);
            well2CWise = Angle.FromWorldVec(_transform.GetWorldPosition(comp2.Connections[well1].DepartureNode) - well2Pos);
        }

        well1CCWise = Math.Min(Angle.FromDegrees(45d) + connectionAngle1, well1CCWise);
        well1CWise = Math.Max(Angle.FromDegrees(-45d) + connectionAngle1, well1CWise);
        well2CCWise = Math.Min(Angle.FromDegrees(45d) + connectionAngle2, well2CCWise);
        well2CWise = Math.Max(Angle.FromDegrees(-45d) + connectionAngle2, well2CWise);

        return true;
    }


    public bool PointInBounds(Vector2 point, Vector2 left1, Vector2 left2, Vector2 right1, Vector2 right2, Angle left1Angle, Angle left2Angle, Angle right1Angle, Angle right2Angle)
    {
        var angleFromL1 = Angle.FromWorldVec(point - left1);
        var angleFromL2 = Angle.FromWorldVec(point - left2);
        var angleFromR1 = Angle.FromWorldVec(point - right1);
        var angleFromR2 = Angle.FromWorldVec(point - right2);

        if (Angle.ShortestDistance(left1Angle, angleFromL1) > 0d ||
            Angle.ShortestDistance(left2Angle, angleFromL2) < 0d ||
            Angle.ShortestDistance(right1Angle, angleFromR1) < 0d ||
            Angle.ShortestDistance(right2Angle, angleFromR2) > 0d)
        {
            return false;
        }

        return true;
    }

    public float DistanceToBoundary(Vector2 posAlongConnection, Angle connectionAngle, Vector2 boundaryStart, Vector2 boundaryEnd, Angle boundAngleStart, Angle boundAngleEnd)
    {
        var connectionNormal = connectionAngle + Angle.FromDegrees(90d);

        //Reconstructing a triangle to do trigenometry.
        var startToPosAlongConnect = Angle.FromWorldVec(boundaryStart - posAlongConnection);
        Angle angleA = Math.Abs(Angle.ShortestDistance(startToPosAlongConnect, connectionNormal));
        var angleB = Math.Abs(Angle.ShortestDistance(connectionAngle, boundAngleStart)) + angleA - Angle.FromDegrees(90d);
        var distanceC = (boundaryStart - posAlongConnection).Length();

        var angleC = Angle.FromDegrees(180d) - angleB - angleA;

        var distanceToStartAngle = Math.Abs(distanceC * Math.Sin(angleB) / Math.Sin(angleC));

        //Doing the same for the end.
        var endToPosAlongConnect = Angle.FromWorldVec(boundaryEnd - posAlongConnection);
        Angle angleD = Math.Abs(Angle.ShortestDistance(endToPosAlongConnect, connectionNormal));
        var angleE = Math.Abs(Angle.ShortestDistance(connectionAngle + Angle.FromDegrees(180d), boundAngleEnd)) + angleD - Angle.FromDegrees(90d);
        var distanceF = (boundaryEnd - posAlongConnection).Length();

        var angleF = Angle.FromDegrees(180d) - angleE - angleD;

        var distanceToEndAngle = Math.Abs(distanceF * Math.Sin(angleE) / Math.Sin(angleF));

        return (float)Math.Min(distanceToStartAngle, distanceToEndAngle);
    }

    public bool CreateConnectingRiverPair(MapId mapId, EntityUid well1, EntityUid well2, Angle flowDirection1 = default, Angle flowDirection2 = default, bool customFlowDirs = false)
    {
        if (!TryComp<RiverWellComponent>(well1, out var wellComp1) ||
            !TryComp<RiverWellComponent>(well2, out var wellComp2))
        {
            return false;
        }

        var crossRivers = wellComp1.FlowRotation != wellComp2.FlowRotation;
        int crossNode = 0;
        var crossingLength = 150f;

        var nodeRange = 40f; // the default Node Range
        var segmentLength = 200f;
        var maxSegmentDrift = segmentLength;//200f; //How much side-to-side offset can there be per segment? This affects the sharpness of the river bends.

        var location1 = _transform.GetWorldPosition(well1);
        var location2 = _transform.GetWorldPosition(well2);

        var arrival1 = _transform.GetWorldPosition(wellComp1.Connections[well2].ArrivalNode);
        var departure1 = _transform.GetWorldPosition(wellComp1.Connections[well2].DepartureNode);

        var arrival2 = _transform.GetWorldPosition(wellComp2.Connections[well1].ArrivalNode);
        var departure2 = _transform.GetWorldPosition(wellComp2.Connections[well1].DepartureNode);

        Vector2 left1;
        Vector2 left2;
        Vector2 right1;
        Vector2 right2;

        if (wellComp1.FlowRotation == Chirality.Clockwise)
        {
            left1 = departure1;
            right1 = arrival1;
        }
        else
        {
            left1 = arrival1;
            right1 = departure1;
        }

        if (wellComp2.FlowRotation == Chirality.Clockwise)
        {
            left2 = arrival2;
            right2 = departure2;
        }
        else
        {
            left2 = departure2;
            right2 = arrival2;
        }

        CalculateBoundingAngles(well1, well2, out var well1LeftBoundingAngle, out var well1RightBoundingAngle, out var well2RightBoundingAngle, out var well2LeftBoundingAngle);

        var interwellTangent = location2 - location1;
        var interwellDistance = (interwellTangent).Length();
        var interwellNormal = Vector2.Normalize(new Vector2(-interwellTangent.Y, interwellTangent.X));

        interwellNormal.Normalize();

        int nodeAmount = (int)(interwellDistance / segmentLength + 0.5f); // Rounding rather than truncating.
        if (!crossRivers && nodeAmount < 2)
        {
            nodeAmount = 2;
        }
        else if (crossRivers && nodeAmount < 3)
        {
            nodeAmount = 3;
            segmentLength = (interwellDistance - wellComp1.Radius - wellComp2.Radius) * 0.5f;
            crossNode = 1;
        }
        else if (crossRivers && nodeAmount == 3)
        {
            crossNode = 1;
        }

        var leftSidePoints = new Vector2[nodeAmount];
        var rightSidePoints = new Vector2[nodeAmount];

        var maxLeftDistances = new float[nodeAmount];
        var maxRightDistances = new float[nodeAmount];

        var leftOffsets = new float[nodeAmount];
        var rightOffsets = new float[nodeAmount];

        var lastNodeIndex = nodeAmount - 1;
        float nodeDivisor = 1f / (float)lastNodeIndex; // Multiply by index to get the correct node percentage. Then Lerp when relevant to get the right index position.
        // lastNodeIndex - index to get river2To1 nodes when building the rivers parallel.

        for (int i = 0; i < nodeAmount; i++)
        {
            var leftPoint = Vector2.Lerp(left1, left2, nodeDivisor * i);
            var rightPoint = Vector2.Lerp(right1, right2, nodeDivisor * i);
            maxLeftDistances[i] = DistanceToBoundary(leftPoint, Angle.FromWorldVec(interwellTangent), left1, left2, well1LeftBoundingAngle, well2LeftBoundingAngle) - nodeRange;
            maxRightDistances[i] = DistanceToBoundary(rightPoint, Angle.FromWorldVec(interwellTangent), right1, right2, well1RightBoundingAngle, well2RightBoundingAngle) - nodeRange;
            var randomOffset1 = _random.NextFloat(-maxRightDistances[i], maxLeftDistances[i]);
            var randomOffset2 = _random.NextFloat(-maxRightDistances[i], maxLeftDistances[i]);
            var leftOffset = MathF.Max(randomOffset1, randomOffset2);
            var rightOffset = MathF.Min(randomOffset1, randomOffset2);

            if (leftOffset < rightOffset + nodeRange * 2f)
            {
                if (leftOffset < 0f)
                {
                    leftOffset += nodeRange * 2f;
                }
                else
                {
                    rightOffset -= nodeRange * 2f;
                }
            }
            leftOffsets[i] = leftOffset;
            rightOffsets[i] = rightOffset;
        }

        leftSidePoints[0] = left1;
        leftSidePoints[lastNodeIndex] = left2;

        rightSidePoints[0] = right1;
        rightSidePoints[lastNodeIndex] = right2;

        // Just setting these as narrow as possible so that the smoothing will work alright-ish, with a slight bias for keeping everything in the middle.
        leftOffsets[0] = nodeRange;
        rightOffsets[0] = -nodeRange;
        leftOffsets[lastNodeIndex] = nodeRange;
        rightOffsets[lastNodeIndex] = -nodeRange;

        if (crossRivers)
        {
            if (nodeAmount > 3)
            {
                crossNode = _random.Next(1, lastNodeIndex);
            }
            //The offsets start at a width already!!!
            var midOffset = (leftOffsets[crossNode] + rightOffsets[crossNode]) * 0.3f; // Averages between the two random offsets, but also pulls it all towards the middle.

            var leftCrossPos = Vector2.Lerp(left1, left2, crossNode * nodeDivisor);
            var rightCrossPos = Vector2.Lerp(right1, right2, crossNode * nodeDivisor);
            var midPos = Vector2.Lerp(leftCrossPos, rightCrossPos, 0.5f);
            var sideToMidDistance = (leftCrossPos - midPos).Length();

            if (MathF.Abs(midOffset) + crossingLength * 0.5f > maxSegmentDrift * crossNode ||
                MathF.Abs(midOffset) + crossingLength * 0.5f > maxSegmentDrift * (nodeAmount - crossNode))
            {
                if (midOffset > 0f)
                {
                    midOffset = MathF.Min(maxSegmentDrift * crossNode - crossingLength * 0.5f, maxSegmentDrift * (lastNodeIndex - crossNode) - crossingLength * 0.5f);
                }
                else
                {
                    midOffset = MathF.Max(-maxSegmentDrift * crossNode + crossingLength * 0.5f, -maxSegmentDrift * (lastNodeIndex - crossNode) + crossingLength * 0.5f);
                }
            }
            midOffset = 0f;
            if (midOffset > crossingLength)
            {
                leftOffsets[crossNode] = midOffset - sideToMidDistance;
                rightOffsets[crossNode] = midOffset + sideToMidDistance - crossingLength;
            }
            if (midOffset < -crossingLength)
            {
                leftOffsets[crossNode] = midOffset - sideToMidDistance + crossingLength;
                rightOffsets[crossNode] = midOffset + sideToMidDistance;
            }
            else
            {
                leftOffsets[crossNode] = midOffset + crossingLength * 0.5f - sideToMidDistance;
                rightOffsets[crossNode] = midOffset - crossingLength * 0.5f + sideToMidDistance;
            }
        }

        var isSmoothed = false;
        while (!isSmoothed)
        {
            isSmoothed = true;
            for (int i = 1; i < lastNodeIndex; i++)
            {
                var priorIndex = i - 1;
                var nextIndex = i + 1;
                if (i == crossNode)
                {
                    continue;
                }
                if (Math.Abs(leftOffsets[i] - leftOffsets[priorIndex]) > maxSegmentDrift ||
                    Math.Abs(leftOffsets[i] - leftOffsets[nextIndex]) > maxSegmentDrift)
                {
                    leftOffsets[i] = (leftOffsets[i] + leftOffsets[priorIndex] + leftOffsets[nextIndex]) * 0.3f;
                    isSmoothed = false;
                }
                if (Math.Abs(rightOffsets[i] - rightOffsets[priorIndex]) > maxSegmentDrift ||
                    Math.Abs(rightOffsets[i] - rightOffsets[nextIndex]) > maxSegmentDrift)
                {
                    rightOffsets[i] = (rightOffsets[i] + rightOffsets[priorIndex] + rightOffsets[nextIndex]) * 0.3f;
                    isSmoothed = false;
                }
            }
        }

        for (int i = 1; i < lastNodeIndex; i++)
        {
            var leftNode = Vector2.Lerp(left1, left2, i * nodeDivisor);
            var rightNode = Vector2.Lerp(right1, right2, i * nodeDivisor);

            leftSidePoints[i] = leftNode + interwellNormal * leftOffsets[i];
            rightSidePoints[i] = rightNode + interwellNormal * rightOffsets[i];
        }

        List<Vector2> river1To2 = new();
        List<Vector2> river2To1 = new();
        for (int i = 0; i < nodeAmount; i++)
        {
            if (wellComp1.FlowRotation == Chirality.Clockwise)
            {
                if (!crossRivers || i < crossNode)
                {
                    river1To2.Add(leftSidePoints[i]);

                    river2To1.Insert(0, rightSidePoints[i]);
                }
                else if (i == crossNode)
                {
                    river1To2.Add(leftSidePoints[i]);
                    river1To2.Add(rightSidePoints[i]);

                    river2To1.Insert(0, rightSidePoints[i]);
                    river2To1.Insert(0, leftSidePoints[i]);
                }
                else
                {
                    river1To2.Add(rightSidePoints[i]);

                    river2To1.Insert(0, leftSidePoints[i]);
                }
            }
            else
            {
                if (!crossRivers || i < crossNode)
                {
                    river1To2.Add(rightSidePoints[i]);

                    river2To1.Insert(0, leftSidePoints[i]);
                }
                else if (i == crossNode && crossRivers)
                {
                    river1To2.Add(rightSidePoints[i]);
                    river1To2.Add(leftSidePoints[i]);

                    river2To1.Insert(0, leftSidePoints[i]);
                    river2To1.Insert(0, rightSidePoints[i]);
                }
                else
                {
                    river1To2.Add(leftSidePoints[i]);

                    river2To1.Insert(0, rightSidePoints[i]);
                }
            }
        }

        var outOfBoundsCounter = 0;
        foreach (var node in river1To2)
        {
            if (!PointInBounds(node, left1, left2, right1, right2, well1LeftBoundingAngle, well2LeftBoundingAngle, well1RightBoundingAngle, well2RightBoundingAngle))
            {
                outOfBoundsCounter++;
            }
        }
        foreach (var node in river2To1)
        {
            if (!PointInBounds(node, left1, left2, right1, right2, well1LeftBoundingAngle, well2LeftBoundingAngle, well1RightBoundingAngle, well2RightBoundingAngle))
            {
                outOfBoundsCounter++;
            }
        }

        Log.Warning("Well1: " + well1.ToString() + " Well2: " + well2.ToString() + " Out Of Bounds Count: " + outOfBoundsCounter.ToString());

        List<Vector2> leftBound1 = new List<Vector2>();
        leftBound1.Add(left1);
        leftBound1.Add(left1 + well1LeftBoundingAngle.ToWorldVec() * 200f);
        List<Vector2> leftBound2 = new List<Vector2>();
        leftBound2.Add(left2);
        leftBound2.Add(left2 + well2LeftBoundingAngle.ToWorldVec() * 200f);
        List<Vector2> rightBound1 = new List<Vector2>();
        rightBound1.Add(right1);
        rightBound1.Add(right1 + well1RightBoundingAngle.ToWorldVec() * 200f);
        List<Vector2> rightBound2 = new List<Vector2>();
        rightBound2.Add(right2);
        rightBound2.Add(right2 + well2RightBoundingAngle.ToWorldVec() * 200f);
        //
        //PopulateRiverFromVector2List(mapId, leftBound1);
        //PopulateRiverFromVector2List(mapId, leftBound2);
        //PopulateRiverFromVector2List(mapId, rightBound1);
        //PopulateRiverFromVector2List(mapId, rightBound2);

        if (crossRivers)
        {
            Log.Info("CrossNode: " + crossNode.ToString());
        }

        PopulateRiverFromVector2List(mapId, river1To2);
        PopulateRiverFromVector2List(mapId, river2To1);

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
            var nearbyNodes = _lookup.GetEntitiesInRange<RiverNodeComponent>(nodeCoord, 80f); //Magic number, TODO: KILL
            var mergeNode = EntityUid.Invalid;
            var mergeNodeDistanceSquared = -1f;
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
                if (angleDelta < Angle.FromDegrees(100d) && isBetterFit)
                {
                    mergeNodeDistanceSquared = nodeDistanceSquared;
                    mergeNode = node;
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

    /// <summary>
    /// Figures out where the arrival and departure points along the well's flow should be, and stores those in the well's component.
    /// </summary>
    /// <param name="wells">A list of wells to organise.</param>
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
                    wellComp.Connections.Add(focusConnection, new ArrivalDeparturePair(rightNode, leftNode));
                }
                else
                {
                    wellComp.Connections.Add(focusConnection, new ArrivalDeparturePair(leftNode, rightNode));
                }
            }
        }
    }
}
