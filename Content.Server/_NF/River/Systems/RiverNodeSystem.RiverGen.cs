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

        Angle cCWiseAngle1 = Angle.FromDegrees(90d);
        Angle cCWiseDifference1 = 90d;
        Angle clockWiseAngle1 = Angle.FromDegrees(-90d);
        Angle clockWiseDifference1 = -90d;
        foreach (var linkedWell in comp1.ConnectedWells)
        {
            if (linkedWell == well2)
            {
                continue;
            }

            var linkedPos = _transform.GetWorldPosition(linkedWell);
            var linkedAngle = Angle.FromWorldVec(linkedPos - well1Pos);
            var angleDifference = Angle.ShortestDistance(connectionAngle1, linkedAngle);

            if (cCWiseDifference1 > angleDifference)
            {
                cCWiseAngle1 = linkedAngle;
                cCWiseDifference1 = angleDifference;
            }
            if (clockWiseDifference1 < angleDifference)
            {
                clockWiseAngle1 = linkedAngle;
                clockWiseDifference1 = angleDifference;
            }
        }

        Angle cCWiseAngle2 = Angle.FromDegrees(90d);
        Angle cCWiseDifference2 = 90d;
        Angle clockWiseAngle2 = Angle.FromDegrees(-90d);
        Angle clockWiseDifference2 = -90d;
        foreach (var linkedWell in comp2.ConnectedWells)
        {
            if (linkedWell == well1)
            {
                continue;
            }

            var linkedPos = _transform.GetWorldPosition(linkedWell);
            var linkedAngle = Angle.FromWorldVec(linkedPos - well2Pos);
            var angleDifference = Angle.ShortestDistance(connectionAngle2, linkedAngle);

            if (cCWiseDifference2 > angleDifference)
            {
                cCWiseAngle2 = linkedAngle;
                cCWiseDifference2 = angleDifference;
            }
            if (clockWiseDifference2 < angleDifference)
            {
                clockWiseAngle2 = linkedAngle;
                clockWiseDifference2 = angleDifference;
            }
        }

        well1CCWise = (cCWiseAngle1 + connectionAngle1) * 0.5f;
        well1CWise = (clockWiseAngle1 + connectionAngle1) * 0.5f;

        well2CCWise = (cCWiseAngle2 + connectionAngle2) * 0.5f;
        well2CWise = (clockWiseAngle2 + connectionAngle2) * 0.5f;

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
        var segmentLength = 300f;
        var maxSegmentDrift = 200f; //How much side-to-side offset can there be per segment? This affects the sharpness of the river bends.

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

        //TODO: Divide length in even pieces, and set the max offset for those
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
                crossNode = (int)((_random.Next(1, nodeAmount) + _random.Next(1, nodeAmount)) * 0.5f);
            }
            var midOffset = (leftOffsets[crossNode] + rightOffsets[crossNode]) * 0.3f; // Averages between the two random offsets, but also pulls it all towards the middle.
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
                leftOffsets[crossNode] = midOffset;
                rightOffsets[crossNode] = midOffset - crossingLength;
            }
            if (midOffset < -crossingLength)
            {
                leftOffsets[crossNode] = midOffset + crossingLength;
                rightOffsets[crossNode] = midOffset;
            }
            else
            {
                leftOffsets[crossNode] = midOffset + crossingLength * 0.5f;
                rightOffsets[crossNode] = midOffset - crossingLength * 0.5f;
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
                else if (i == crossNode)
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

        PopulateRiverFromVector2List(mapId, river1To2);
        PopulateRiverFromVector2List(mapId, river2To1);

        // Skip to the middle/crossing point, then fill out the inbetween spots. 

        //var maxDistanceSquared = MathF.Pow(maxDistance, 2f);
        //var minDistBetweenRiversSquared = MathF.Pow(minDistBetweenRivers, 2f);
        //
        //List<Vector2> nodePositionsRight = new();
        //List<Vector2> nodePositionsLeft = new();
        //
        //var longTangent = Vector2.Normalize(location2 - location1);
        //var longNormal = new Vector2(-longTangent.Y, longTangent.X); //Left?
        //var reverseLongTangent = Vector2.Normalize(location1 - location2);
        //var connectionDirection1 = longTangent;
        //var connectionDirection2 = reverseLongTangent;
        //
        //// Average out connection points that are close to eachother. So if two connected wells are in roughly the same direction, have the arrival and departure be the same for them.
        //
        //
        //
        //Vector2 offsetLoc1Arrival = new();
        //Vector2 offsetLoc2Departure = new();
        //Vector2 offsetLoc1Departure = new();
        //Vector2 offsetLoc2Arrival = new();
        //
        //Angle departureAngle1;
        //Angle arrivalAngle1;
        //if (wellComp1.FlowRotation == Chirality.Clockwise)
        //{
        //    departureAngle1 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection1).Degrees - 45d).Reduced(); // 45 degrees clockwise
        //    arrivalAngle1 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection1).Degrees + 225d).Reduced(); // 180 + 45 degrees counter-clockwise
        //}
        //else
        //{
        //    departureAngle1 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection1).Degrees + 45d).Reduced(); // 45 degrees counter-clockwise
        //    arrivalAngle1 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection1).Degrees - 225d).Reduced(); // 180 + 45 degrees clockwise
        //}
        //
        //// Get the river positions at Well 1.
        //var closestDepartureAngle1 = new Angle();
        //var closestArrivalAngle1 = new Angle();
        //var arrivalNode1 = EntityUid.Invalid;
        //var departureNode1 = EntityUid.Invalid;
        //foreach (var node in wellComp1.WellNodes)
        //{
        //    if (!TryComp<RiverNodeComponent>(node, out var nodeComp))
        //    {
        //        continue;
        //    }
        //
        //    var departureDistance = Math.Abs(Angle.ShortestDistance(departureAngle1, nodeComp.FlowDirection.Reduced()).Degrees);
        //    var arrivalDistance = Math.Abs(Angle.ShortestDistance(arrivalAngle1, nodeComp.FlowDirection.Reduced()).Degrees);
        //    if (departureDistance < closestDepartureAngle1 || offsetLoc1Departure == new Vector2(0f, 0f))
        //    {
        //        offsetLoc1Departure = _transform.GetWorldPosition(node);
        //        departureNode1 = node;
        //        closestDepartureAngle1 = departureDistance;
        //    }
        //    else if (arrivalDistance < closestArrivalAngle1 || offsetLoc1Arrival == new Vector2(0f, 0f))
        //    {
        //        offsetLoc1Arrival = _transform.GetWorldPosition(node);
        //        arrivalNode1 = node;
        //        closestArrivalAngle1 = arrivalDistance;
        //    }
        //}
        //
        //var checkNode = departureNode1;
        //while (checkNode != arrivalNode1)
        //{
        //    if (!TryComp<RiverNodeComponent>(checkNode, out var checkComp))
        //    {
        //        break;
        //    }
        //    var safetyCheck = checkNode;
        //    if (checkComp.PriorNodes.Count > 1) // New Departure point
        //    {
        //        offsetLoc1Departure = _transform.GetWorldPosition(checkNode);
        //    }
        //    if (checkComp.NextNodes.Count > 1) // New Arrival point
        //    {
        //        offsetLoc1Arrival = _transform.GetWorldPosition(checkNode);
        //    }
        //    foreach (var node in GetEntityList(checkComp.NextNodes))
        //    {
        //        if (wellComp1.WellNodes.Contains(node))
        //        {
        //            checkNode = node;
        //        }
        //    }
        //
        //    // Make sure the loop doesn't break.
        //    if (checkNode == safetyCheck || checkNode == departureNode1 || !wellComp1.WellNodes.Contains(checkNode))
        //    {
        //        break;
        //    }
        //}
        //
        //Angle departureAngle2;
        //Angle arrivalAngle2;
        //if (wellComp2.FlowRotation == Chirality.Clockwise)
        //{
        //    departureAngle2 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection2).Degrees - 45d).Reduced(); // 45 degrees counter-clockwise
        //    arrivalAngle2 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection2).Degrees + 225d).Reduced(); // 180 + 45 degrees clockwise
        //}
        //else
        //{
        //    departureAngle2 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection2).Degrees + 45d).Reduced(); // 45 degrees clockwise
        //    arrivalAngle2 = Angle.FromDegrees(Angle.FromWorldVec(connectionDirection2).Degrees - 225d).Reduced(); // 180 + 45 degrees counter-clockwise
        //}
        //
        //// Get the river positions at Well 2.
        //var closestDepartureAngle2 = new Angle();
        //var closestArrivalAngle2 = new Angle();
        //var arrivalNode2 = EntityUid.Invalid;
        //var departureNode2 = EntityUid.Invalid;
        //foreach (var node in wellComp2.WellNodes)
        //{
        //    if (!TryComp<RiverNodeComponent>(node, out var nodeComp))
        //    {
        //        continue;
        //    }
        //
        //    var departureDistance = Math.Abs(Angle.ShortestDistance(departureAngle2, nodeComp.FlowDirection.Reduced()).Degrees);
        //    var arrivalDistance = Math.Abs(Angle.ShortestDistance(arrivalAngle2, nodeComp.FlowDirection.Reduced()).Degrees);
        //    if (departureDistance < closestDepartureAngle2 || offsetLoc2Departure == new Vector2(0f, 0f))
        //    {
        //        offsetLoc2Departure = _transform.GetWorldPosition(node);
        //        departureNode2 = node;
        //        closestDepartureAngle2 = departureDistance;
        //    }
        //    else if (arrivalDistance < closestArrivalAngle2 || offsetLoc2Arrival == new Vector2(0f, 0f))
        //    {
        //        offsetLoc2Arrival = _transform.GetWorldPosition(node);
        //        arrivalNode2 = node;
        //        closestArrivalAngle2 = arrivalDistance;
        //    }
        //}
        //
        //checkNode = departureNode2;
        //while (checkNode != arrivalNode2)
        //{
        //    if (!TryComp<RiverNodeComponent>(checkNode, out var checkComp))
        //    {
        //        break;
        //    }
        //    var safetyCheck = checkNode;
        //    if (checkComp.PriorNodes.Count > 1) // New Departure point
        //    {
        //        offsetLoc2Departure = _transform.GetWorldPosition(checkNode);
        //    }
        //    if (checkComp.NextNodes.Count > 1) // New Arrival point
        //    {
        //        offsetLoc2Arrival = _transform.GetWorldPosition(checkNode);
        //    }
        //    foreach (var node in GetEntityList(checkComp.NextNodes))
        //    {
        //        if (wellComp2.WellNodes.Contains(node))
        //        {
        //            checkNode = node;
        //        }
        //    }
        //
        //    // Make sure the loop doesn't break.
        //    if (checkNode == safetyCheck || checkNode == departureNode2 || !wellComp2.WellNodes.Contains(checkNode))
        //    {
        //        break;
        //    }
        //}
        //if (wellComp1.Connections.ContainsKey(well2))
        //{
        //    offsetLoc1Departure = _transform.GetWorldPosition(wellComp1.Connections[well2].DepartureNode);
        //    offsetLoc1Arrival = _transform.GetWorldPosition(wellComp1.Connections[well2].ArrivalNode);
        //}
        //else
        //{
        //    offsetLoc1Departure = location1;
        //    offsetLoc1Departure.Y += 100f;
        //    offsetLoc1Arrival = location1;
        //    offsetLoc1Arrival.Y += 100f;
        //
        //}
        //if (wellComp2.Connections.ContainsKey(well1))
        //{
        //    offsetLoc2Arrival = _transform.GetWorldPosition(wellComp2.Connections[well1].ArrivalNode);
        //    offsetLoc2Departure = _transform.GetWorldPosition(wellComp2.Connections[well1].DepartureNode);
        //}
        //else
        //{
        //    offsetLoc2Departure = location2;
        //    offsetLoc2Departure.X += 100f;
        //    offsetLoc2Arrival = location2;
        //    offsetLoc2Arrival.X += 100f;
        //}
        //
        //nodePositionsRight.Add(offsetLoc1Departure);
        //nodePositionsRight.Add(offsetLoc2Arrival);
        //nodePositionsLeft.Add(offsetLoc1Arrival);
        //nodePositionsLeft.Add(offsetLoc2Departure);
        //
        //var locationDistanceSquared = (location1 - location2).LengthSquared();
        //
        //if (locationDistanceSquared < maxDistanceSquared) //Does not support tiny rivers.
        //{
        //    return false;
        //}
        //
        //int segmentNumber = (int)MathF.Sqrt(locationDistanceSquared / maxDistanceSquared);
        //for (int i = 1; i <= segmentNumber; i *= 2)
        //{
        //
        //    var tempList1 = new List<Vector2>();
        //    var tempList2 = new List<Vector2>();
        //    for (var i0 = 0; i0 < Math.Min(nodePositionsRight.Count, nodePositionsLeft.Count) - 1; i0++) //1To2 and 2To1 should always be equal, but in case they aren't, this prevents over-running the lists.
        //    {
        //        var i1 = i0 + 1;
        //        var nodePos1A = nodePositionsRight[i0];
        //        var nodePos1B = nodePositionsRight[i1];
        //        var nodePos2A = nodePositionsLeft[i0];
        //        var nodePos2B = nodePositionsLeft[i1];
        //
        //        var distance1 = (nodePos1B - nodePos1A).Length() * 0.2f;
        //        var distance2 = (nodePos2B - nodePos2A).Length() * 0.2f;
        //
        //        var tangent1 = Vector2.Normalize(nodePos1B - nodePos1A);
        //        var tangent2 = Vector2.Normalize(nodePos2B - nodePos2A);
        //        var normal1 = new Vector2(-tangent1.Y, tangent1.X); //Left
        //        var normal2 = new Vector2(-tangent2.Y, tangent2.X); //Left
        //
        //        var midNode1 = (nodePos1A + nodePos1B) * 0.5f;
        //        var midNode2 = (nodePos2A + nodePos2B) * 0.5f;
        //
        //        var furthestLeft1 = minDistBetweenRivers * 2f - MathF.Sqrt(DistanceToLineSegmentSquared(midNode1, nodePos2A, nodePos2B, out var _, out var _));
        //        var normalOffset1 = _random.NextFloat(MathF.Max(furthestLeft1, -distance1), distance1);
        //
        //        var furthestRight2 = MathF.Sqrt(DistanceToLineSegmentSquared(midNode2, nodePos1A, nodePos1B, out var _, out var _)) - minDistBetweenRivers * 2f + normalOffset1; //Make this dependent on how far left MidNode1 went.
        //        var normalOffset2 = _random.NextFloat(-distance2, MathF.Min(furthestRight2, distance2));
        //
        //        midNode1 = midNode1 + normal1 * -normalOffset1;
        //        midNode2 = midNode2 + normal2 * -normalOffset2;
        //
        //        tempList1.Add(nodePos1A);
        //        tempList1.Add(midNode1);
        //
        //        tempList2.Add(nodePos2A);
        //        tempList2.Add(midNode2);
        //
        //        if (i0 == Math.Min(nodePositionsRight.Count, nodePositionsLeft.Count) - 2)
        //        {
        //            tempList1.Add(nodePos1B);
        //            tempList2.Add(nodePos2B);
        //        }
        //    }
        //    nodePositionsRight.Clear();
        //    nodePositionsRight.AddRange(tempList1);
        //    nodePositionsLeft.Clear();
        //    nodePositionsLeft.AddRange(tempList2);
        //    tempList1.Clear();
        //    tempList2.Clear();
        //}
        //
        //PopulateRiverFromVector2List(mapId, nodePositionsRight);
        //nodePositionsLeft.Reverse();
        //PopulateRiverFromVector2List(mapId, nodePositionsLeft);

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
