using Content.Server._NF.River.Components;
using Content.Shared.Destructible;
using Robust.Shared.Map;
using Robust.Shared.Physics.Collision.Shapes;
using System.Numerics;

namespace Content.Server._NF.River.Systems;

public partial class RiverNodeSystem
{
    /// <summary>
    /// Calculates angles which forms the outer bounds beyond which no part of the river should generate. This system only takes into account two wells at a time,
    /// and does not account for overlapping angles from other connections.
    /// </summary>
    /// <param name="well1">One of the two wells between which the rivers are to be generated.</param>
    /// <param name="well2">The other of two wells between which the rivers are to be generated.</param>
    /// <param name="well1CCWise">Returns the counter-clockwise (positive) Angle of well 1.</param>
    /// <param name="well1CWise">Returns the clockwise (negative) Angle of well 1.</param>
    /// <param name="well2CCWise">Returns the counter-clockwise (positive) Angle of well 2.</param>
    /// <param name="well2CWise">Returns the clockwise (negative) Angle of well 2.</param>
    /// <returns>Returns true if the action was successful. If false, then all angles returned are at the maximum angle (+/-45 degrees by default).</returns>
    public bool CalculateBoundingAngles(EntityUid well1, EntityUid well2, out Angle well1CCWise, out Angle well1CWise, out Angle well2CCWise, out Angle well2CWise)
    {
        var maxAngle = Angle.FromDegrees(45d);
        var well1Pos = _transform.GetWorldPosition(well1);
        var well2Pos = _transform.GetWorldPosition(well2);
        var connectionAngle1 = Angle.FromWorldVec(well2Pos - well1Pos);
        var connectionAngle2 = Angle.FromWorldVec(well1Pos - well2Pos);

        well1CCWise = connectionAngle1 + maxAngle;
        well1CWise = connectionAngle1 - maxAngle;
        well2CCWise = connectionAngle2 + maxAngle;
        well2CWise = connectionAngle2 - maxAngle;

        if (!TryComp<RiverWellComponent>(well1, out var comp1) ||
            !TryComp<RiverWellComponent>(well2, out var comp2))
        {
            return false;
        }

        if (comp1.FlowRotation == Chirality.Clockwise)
        {
            var cCWiseAngle = Angle.FromWorldVec(_transform.GetWorldPosition(comp1.Connections[well2].DepartureNode) - well1Pos);
            if (Math.Abs(Angle.ShortestDistance(cCWiseAngle, connectionAngle1)) < maxAngle)
            {
                well1CCWise = cCWiseAngle;
            }
            var clockWiseAngle = Angle.FromWorldVec(_transform.GetWorldPosition(comp1.Connections[well2].ArrivalNode) - well1Pos);
            if (Math.Abs(Angle.ShortestDistance(clockWiseAngle, connectionAngle1)) < maxAngle)
            {
                well1CWise = clockWiseAngle;
            }
        }
        else
        {
            var cCWiseAngle = Angle.FromWorldVec(_transform.GetWorldPosition(comp1.Connections[well2].ArrivalNode) - well1Pos);
            if (Math.Abs(Angle.ShortestDistance(cCWiseAngle, connectionAngle1)) < maxAngle)
            {
                well1CCWise = cCWiseAngle;
            }
            var clockWiseAngle = Angle.FromWorldVec(_transform.GetWorldPosition(comp1.Connections[well2].DepartureNode) - well1Pos);
            if (Math.Abs(Angle.ShortestDistance(clockWiseAngle, connectionAngle1)) < maxAngle)
            {
                well1CWise = clockWiseAngle;
            }
        }

        if (comp2.FlowRotation == Chirality.Clockwise)
        {
            var cCWiseAngle = Angle.FromWorldVec(_transform.GetWorldPosition(comp2.Connections[well1].DepartureNode) - well2Pos);
            if (Math.Abs(Angle.ShortestDistance(cCWiseAngle, connectionAngle2)) < maxAngle)
            {
                well2CCWise = cCWiseAngle;
            }
            var clockWiseAngle = Angle.FromWorldVec(_transform.GetWorldPosition(comp2.Connections[well1].ArrivalNode) - well2Pos);
            if (Math.Abs(Angle.ShortestDistance(clockWiseAngle, connectionAngle2)) < maxAngle)
            {
                well2CWise = clockWiseAngle;
            }
        }
        else
        {
            var cCWiseAngle = Angle.FromWorldVec(_transform.GetWorldPosition(comp2.Connections[well1].ArrivalNode) - well2Pos);
            if (Math.Abs(Angle.ShortestDistance(cCWiseAngle, connectionAngle2)) < maxAngle)
            {
                well2CCWise = cCWiseAngle;
            }
            var clockWiseAngle = Angle.FromWorldVec(_transform.GetWorldPosition(comp2.Connections[well1].DepartureNode) - well2Pos);
            if (Math.Abs(Angle.ShortestDistance(clockWiseAngle, connectionAngle2)) < maxAngle)
            {
                well2CWise = clockWiseAngle;
            }
        }

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

    //TODO: Think I fixed it? But double check please.
    public float DistanceToBoundary(Vector2 posAlongConnection, Angle connectionAngle, Vector2 boundaryStart, Vector2 boundaryEnd, Angle boundAngleStart, Angle boundAngleEnd)
    {
        //Reconstructing a triangle to do trigenometry.
        Angle connectionNormal;
        bool leftNormal;
        if (Angle.ShortestDistance(connectionAngle, boundAngleStart) > 0d)
        {
            connectionNormal = connectionAngle + Angle.FromDegrees(90d);
            leftNormal = true;
        }
        else
        {
            connectionNormal = connectionAngle - Angle.FromDegrees(90d);
            leftNormal = false;
        }
        connectionNormal = connectionNormal.Reduced();

        Angle angleStartTheta = Math.Abs(Angle.ShortestDistance(connectionAngle, boundAngleStart));
        var startToPosAlongConnect = Angle.FromWorldVec(boundaryStart - posAlongConnection);
        Angle angleA = Math.Abs(Angle.ShortestDistance(startToPosAlongConnect, connectionNormal));

        angleA.Reduced();
        Angle angleC;
        if (leftNormal && Angle.ShortestDistance(startToPosAlongConnect, connectionNormal) < 0d)
        {
            angleC = Angle.FromDegrees(90d) - angleStartTheta;
        }
        else if (leftNormal)
        {
            angleC = Angle.FromDegrees(90d) + angleStartTheta;
        }
        else if (Angle.ShortestDistance(startToPosAlongConnect, connectionNormal) < 0d)
        {
            angleC = Angle.FromDegrees(90d) + angleStartTheta;
        }
        else
        {
            angleC = Angle.FromDegrees(90d) - angleStartTheta;
        }
        angleC = Math.Abs(angleC);
        var distanceC = (boundaryStart - posAlongConnection).Length();

        var angleB = Angle.FromDegrees(180d) - angleC - angleA;

        var distanceToStartAngle = Math.Abs(distanceC * Math.Sin(angleB) / Math.Sin(angleC));

        //Doing the same for the end.
        Angle connectionNormalEnd;
        bool leftNormalEnd;
        var endConnectionAngle = connectionAngle.Opposite();
        if (Angle.ShortestDistance(endConnectionAngle, boundAngleEnd) > 0d)
        {
            connectionNormalEnd = endConnectionAngle + Angle.FromDegrees(90d);
            leftNormalEnd = true;
        }
        else
        {
            connectionNormalEnd = endConnectionAngle - Angle.FromDegrees(90d);
            leftNormalEnd = false;
        }
        connectionNormalEnd = connectionNormalEnd.Reduced();

        Angle angleEndTheta = Math.Abs(Angle.ShortestDistance(endConnectionAngle, boundAngleEnd));
        var endToPosAlongConnect = Angle.FromWorldVec(boundaryEnd - posAlongConnection);
        Angle angleD = Math.Abs(Angle.ShortestDistance(endToPosAlongConnect, connectionNormalEnd));
        Angle angleF;
        if (leftNormalEnd && Angle.ShortestDistance(endToPosAlongConnect, connectionNormalEnd) < 0d)
        {
            angleF = Angle.FromDegrees(90d) - angleEndTheta;
        }
        else if (leftNormalEnd)
        {
            angleF = Angle.FromDegrees(90d) + angleEndTheta;
        }
        else if (Angle.ShortestDistance(endToPosAlongConnect, connectionNormalEnd) < 0d)
        {
            angleF = Angle.FromDegrees(90d) + angleEndTheta;
        }
        else
        {
            angleF = Angle.FromDegrees(90d) - angleEndTheta;
        }

        angleF = Math.Abs(angleF);
        var distanceF = (boundaryEnd - posAlongConnection).Length();

        var angleE = Angle.FromDegrees(180d) - angleF - angleD;

        var distanceToEndAngle = Math.Abs(distanceF * Math.Sin(angleE) / Math.Sin(angleF));

        //DEBUG
        //Log.Warning("AngleStart: " + boundAngleStart.Degrees.ToString());
        //Log.Warning("ConnectionAngle: " + connectionAngle.Degrees.ToString());
        //Log.Warning("ConnectionAngleEnd: " + endConnectionAngle.Degrees.ToString());
        //Log.Warning("AngleEnd: " + boundAngleEnd.Degrees.ToString());
        //
        //if (Math.Abs(Angle.ShortestDistance(boundAngleStart, boundAngleEnd)) < Angle.FromDegrees(90d))
        //{
        //    Log.Error("Angle between Start and End too sharp! " + new Angle(Math.Abs(Angle.ShortestDistance(boundAngleStart, boundAngleEnd))).Degrees.ToString());
        //}
        //
        //var totalAngleABC = (angleA + angleB + angleC).Degrees;
        //var totalAngleDEF = (angleD + angleE + angleF).Degrees;
        //if (totalAngleABC < 179 || totalAngleABC > 181 || angleC < 0d)
        //{
        //    Log.Warning("Normal: " + connectionNormal.Degrees.ToString());
        //    Log.Warning("startToPosAlongConnect:" + startToPosAlongConnect.Degrees.ToString());
        //
        //    Log.Warning("AngleA: " + angleA.Degrees.ToString());
        //    Log.Warning("AngleB: " + angleB.Degrees.ToString());
        //    Log.Warning("AngleC: " + angleC.Degrees.ToString());
        //    Log.Warning("DistanceToStartAngle: " + distanceToStartAngle.ToString());
        //    Log.Warning("TotalAngle: " + totalAngleABC.ToString());
        //}
        //if (totalAngleABC < 179 || totalAngleABC > 181 || angleF < 0d)
        //{
        //    Log.Warning("Normal: " + connectionNormalEnd.Degrees.ToString());
        //    Log.Warning("endToPosAlongConnect:" + endToPosAlongConnect.Degrees.ToString());
        //    Log.Warning("AngleD: " + angleD.Degrees.ToString());
        //    Log.Warning("AngleE: " + angleE.Degrees.ToString());
        //    Log.Warning("AngleF: " + angleF.Degrees.ToString());
        //    Log.Warning("DistanceToEndAngle: " + distanceToEndAngle.ToString());
        //    Log.Warning("TotalAngle: " + totalAngleDEF.ToString());
        //}
        //DEBUG END

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
        var crossingLength = 100f;
        var crossingBevel = 100f;
        var crossMidOffset = 0f;

        var nodeRange = 40f; // the default Node Range
        var segmentLength = 250f;
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

        //DEBUG
        //var angle1to2 = Angle.FromWorldVec(_transform.GetWorldPosition(well2) - _transform.GetWorldPosition(well1));
        //var angle2to1 = Angle.FromWorldVec(_transform.GetWorldPosition(well1) - _transform.GetWorldPosition(well2));
        //Log.Warning("Well1: " + well1.ToString());
        //Log.Warning("angle1To2: " + angle1to2.Degrees.ToString());
        //Log.Error("Well2: " + well2.ToString());
        //Log.Error("angle2To1: " + angle2to1.Degrees.ToString());
        //Log.Warning("well1LeftBoundingAngle: " + well1LeftBoundingAngle.Degrees.ToString());
        //Log.Warning("well1 left to connecting angle: " + Angle.ShortestDistance(angle1to2, well1LeftBoundingAngle).Degrees);
        //Log.Warning("well1RightBoundingAngle: " + well1RightBoundingAngle.Degrees.ToString());
        //Log.Warning("well1 right to connecting angle: " + Angle.ShortestDistance(angle1to2, well1RightBoundingAngle).Degrees);
        //Log.Error("well2RightBoundingAngle: " + well2RightBoundingAngle.Degrees.ToString());
        //Log.Error("well2 right to connecting angle: " + Angle.ShortestDistance(angle2to1, well2RightBoundingAngle).Degrees);
        //Log.Error("well2LeftBoundingAngle: " + well2LeftBoundingAngle.Degrees.ToString());
        //Log.Error("well2 left to connecting angle: " + Angle.ShortestDistance(angle2to1, well2LeftBoundingAngle).Degrees);
        //Log.Info("Left difference: " + Angle.ShortestDistance(well1LeftBoundingAngle, well2LeftBoundingAngle).Degrees);
        //Log.Info("Right difference: " + Angle.ShortestDistance(well1RightBoundingAngle, well2RightBoundingAngle).Degrees);

        //DEBUG END

        var interwellTangent = location2 - location1;
        var interwellDistance = (interwellTangent).Length();
        var interwellNormal = Vector2.Normalize(new Vector2(-interwellTangent.Y, interwellTangent.X));
        var scalar1 = MathF.Max(Vector2.Dot(left1 - location1, interwellTangent.Normalized()), Vector2.Dot(right1 - location1, interwellTangent.Normalized()));
        var scalar2 = MathF.Max(Vector2.Dot(left2 - location2, -interwellTangent.Normalized()), Vector2.Dot(right2 - location2, -interwellTangent.Normalized()));
        var mid1 = location1 + interwellTangent.Normalized() * scalar1;
        var mid2 = location2 + -interwellTangent.Normalized() * scalar2;

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
            var midPoint = Vector2.Lerp(mid1, mid2, nodeDivisor * i);
            var leftPoint = Vector2.Lerp(left1, left2, nodeDivisor * i);
            var rightPoint = Vector2.Lerp(right1, right2, nodeDivisor * i);
            maxLeftDistances[i] = DistanceToBoundary(midPoint, Angle.FromWorldVec(interwellTangent), left1, left2, well1LeftBoundingAngle, well2LeftBoundingAngle) - nodeRange;
            maxRightDistances[i] = DistanceToBoundary(midPoint, Angle.FromWorldVec(interwellTangent), right1, right2, well1RightBoundingAngle, well2RightBoundingAngle) - nodeRange;
            var randomOffset1 = _random.NextFloat(-maxRightDistances[i], maxLeftDistances[i]);
            var randomOffset2 = _random.NextFloat(-maxRightDistances[i], maxLeftDistances[i]);
            var leftOffset = MathF.Max(randomOffset1, randomOffset2);
            var rightOffset = MathF.Min(randomOffset1, randomOffset2);

            // Ensure they're not too close.
            if (MathF.Abs(leftOffset - rightOffset) < nodeRange * 2)
            {
                if (leftOffset + nodeRange < maxLeftDistances[i])
                {
                    leftOffset += nodeRange;
                }
                else
                {
                    rightOffset -= nodeRange;
                }

                if (rightOffset - nodeRange > -maxRightDistances[i])
                {
                    rightOffset -= nodeRange;
                }
                else
                {
                    leftOffset += nodeRange;
                }
            }

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

        //TODO: add smoothing here? (Secondary points going along the flow basically)
        leftSidePoints[0] = left1;
        leftSidePoints[lastNodeIndex] = left2;

        rightSidePoints[0] = right1;
        rightSidePoints[lastNodeIndex] = right2;

        // Won't be 100% accurate, but close enough.
        leftOffsets[0] = (mid1 - left1).Length();
        rightOffsets[0] = -(mid1 - right1).Length();
        leftOffsets[lastNodeIndex] = (mid2 - left2).Length();
        rightOffsets[lastNodeIndex] = -(mid2 - right2).Length();

        var left1Tangent = new Vector2(-(left1 - location1).Y, (left1 - location1).X);
        var right1Tangent = -new Vector2(-(right1 - location1).Y, (right1 - location1).X);
        var left2Tangent = new Vector2(-(left2 - location2).Y, (left2 - location2).X);
        var right2Tangent = -new Vector2(-(right2 - location2).Y, (right2 - location2).X);

        // Math such that the further from the interwell tangent the angle is, the smaller the offset.

        if (crossRivers)
        {
            if (nodeAmount > 3)
            {
                if (nodeAmount > 5)
                {
                    crossNode = _random.Next(2, lastNodeIndex - 1);
                }
                else
                {
                    crossNode = _random.Next(1, lastNodeIndex);
                }
            }
            crossMidOffset = (leftOffsets[crossNode] + rightOffsets[crossNode]) * 0.5f;
        }

        // This is supposed to make the rivers curve gently going into and coming out of wells. Doesn't work, TODO: Fix this.
        var peninitialL1Mod = Math.Abs(Angle.ShortestDistance(Angle.FromWorldVec(left1Tangent), Angle.FromWorldVec(interwellTangent))) / Math.PI;
        var peninitialR1Mod = Math.Abs(Angle.ShortestDistance(Angle.FromWorldVec(right1Tangent), Angle.FromWorldVec(interwellTangent))) / Math.PI;
        var penultimateL2Mod = Math.Abs(Angle.ShortestDistance(Angle.FromWorldVec(left2Tangent), Angle.FromWorldVec(interwellTangent))) / Math.PI;
        var penultimateR2Mod = Math.Abs(Angle.ShortestDistance(Angle.FromWorldVec(right2Tangent), Angle.FromWorldVec(interwellTangent))) / Math.PI;

        leftOffsets[1] = MathF.Min((float)peninitialL1Mod * wellComp1.Radius, maxLeftDistances[1]);
        rightOffsets[1] = MathF.Max((float)peninitialR1Mod * -wellComp1.Radius, -maxRightDistances[1]);
        leftOffsets[lastNodeIndex - 1] = MathF.Min((float)penultimateL2Mod * wellComp2.Radius, maxLeftDistances[lastNodeIndex - 1]);
        rightOffsets[lastNodeIndex - 1] = MathF.Max((float)penultimateR2Mod * -wellComp2.Radius, -maxRightDistances[lastNodeIndex - 1]);

        // River smoothing and ensuring no illegal positions happen.
        var isSmoothed = false;
        while (!isSmoothed)
        {
            isSmoothed = true;
            //Run through the river length and check the left and right points to see if they need corrections.
            for (int i = 1; i < lastNodeIndex; i++)
            {
                var priorIndex = i - 1;
                var nextIndex = i + 1;

                var halfCrossLength = crossingLength * 0.5f;

                // If a right node is to the left of the left node, just swaps them for ease.
                if (leftOffsets[i] < rightOffsets[i])
                {
                    var tempLeft = leftOffsets[i];
                    var tempRight = rightOffsets[i];
                    leftOffsets[i] = tempRight;
                    rightOffsets[i] = tempLeft;
                }

                // Ensures Rivers (usually) don't overlap. (This doesn't account for some overlaps, where the rivers are close, and the perturbations are steep.)
                if (MathF.Abs(leftOffsets[i] - rightOffsets[i]) < nodeRange * 2f)
                {
                    if (rightOffsets[i] > 0f)
                    {
                        rightOffsets[i] = rightOffsets[i] - nodeRange * 2f;
                    }
                    else if (leftOffsets[i] < 0f)
                    {
                        leftOffsets[i] = leftOffsets[i] + nodeRange * 2f;
                    }
                    else
                    {
                        rightOffsets[i] = rightOffsets[i] - nodeRange;
                        leftOffsets[i] = leftOffsets[i] + nodeRange;
                    }
                }

                if (crossRivers)
                {
                    var closestMiddleRight = MathF.Max(rightOffsets[crossNode + 1], rightOffsets[crossNode - 1]);
                    var closestMiddleLeft = MathF.Min(leftOffsets[crossNode + 1], leftOffsets[crossNode - 1]);
                    var doubleBevel = crossingBevel * 2f;

                    if (closestMiddleRight > 0f)
                    {
                        if (MathF.Abs(rightOffsets[crossNode + 1] - closestMiddleLeft) < crossingLength + doubleBevel)
                        {
                            rightOffsets[crossNode + 1] = closestMiddleLeft - crossingLength - doubleBevel;
                        }
                        if (MathF.Abs(rightOffsets[crossNode - 1] - closestMiddleLeft) < crossingLength + doubleBevel)
                        {
                            rightOffsets[crossNode - 1] = closestMiddleLeft - crossingLength - doubleBevel;
                        }
                    }
                    else if (closestMiddleLeft < 0f)
                    {
                        if (MathF.Abs(leftOffsets[crossNode + 1] - closestMiddleRight) < crossingLength + doubleBevel)
                        {
                            leftOffsets[crossNode + 1] = closestMiddleRight + crossingLength + doubleBevel;
                        }
                        if (MathF.Abs(leftOffsets[crossNode - 1] - closestMiddleRight) < crossingLength + doubleBevel)
                        {
                            leftOffsets[crossNode - 1] = closestMiddleRight + crossingLength + doubleBevel;
                        }
                    }
                    else
                    {
                        var middle = (closestMiddleRight + closestMiddleLeft) * 0.5f;
                        if (MathF.Abs(rightOffsets[crossNode + 1] - closestMiddleLeft) < crossingLength + doubleBevel)
                        {
                            rightOffsets[crossNode + 1] = middle - halfCrossLength - crossingBevel;
                        }
                        if (MathF.Abs(rightOffsets[crossNode - 1] - closestMiddleLeft) < crossingLength + doubleBevel)
                        {
                            rightOffsets[crossNode - 1] = middle - halfCrossLength - crossingBevel;
                        }
                        if (MathF.Abs(leftOffsets[crossNode + 1] - closestMiddleRight) < crossingLength + doubleBevel)
                        {
                            leftOffsets[crossNode + 1] = middle + halfCrossLength + crossingBevel;
                        }
                        if (MathF.Abs(leftOffsets[crossNode - 1] - closestMiddleRight) < crossingLength + doubleBevel)
                        {
                            leftOffsets[crossNode - 1] = middle + halfCrossLength + crossingBevel;
                        }
                    }
                }

                if (crossRivers && i == crossNode)
                {
                    var leftLimit = crossMidOffset + halfCrossLength + crossingBevel;
                    var rightLimit = crossMidOffset - halfCrossLength - crossingBevel;
                    if (leftLimit > MathF.Min(leftOffsets[priorIndex], leftOffsets[nextIndex]) ||
                        rightLimit < MathF.Max(rightOffsets[priorIndex], rightOffsets[nextIndex]))
                    {
                        crossMidOffset = _random.NextFloat(MathF.Max(rightOffsets[priorIndex], rightOffsets[nextIndex]) + crossingBevel + halfCrossLength, MathF.Min(leftOffsets[priorIndex], leftOffsets[nextIndex]) - crossingBevel - halfCrossLength);
                        leftOffsets[i] = crossMidOffset + halfCrossLength;
                        rightOffsets[i] = crossMidOffset - halfCrossLength;
                        isSmoothed = false;
                    }
                }
                else
                {
                    if (Math.Abs(leftOffsets[i] - leftOffsets[priorIndex]) > maxSegmentDrift ||
                        Math.Abs(leftOffsets[i] - leftOffsets[nextIndex]) > maxSegmentDrift)
                    {
                        var minOffset = MathF.Min(leftOffsets[priorIndex], leftOffsets[nextIndex]);
                        minOffset = MathF.Max(minOffset, rightOffsets[i] + nodeRange * 2f);

                        var maxOffset = MathF.Max(leftOffsets[priorIndex], leftOffsets[nextIndex]);
                        maxOffset = MathF.Min(maxOffset, maxLeftDistances[i]);

                        leftOffsets[i] = _random.NextFloat(minOffset, maxOffset);
                        isSmoothed = false;
                    }
                    if (Math.Abs(rightOffsets[i] - rightOffsets[priorIndex]) > maxSegmentDrift ||
                        Math.Abs(rightOffsets[i] - rightOffsets[nextIndex]) > maxSegmentDrift)
                    {
                        var minOffset = MathF.Min(rightOffsets[priorIndex], rightOffsets[nextIndex]);
                        minOffset = MathF.Max(minOffset, -maxRightDistances[i]);

                        var maxOffset = MathF.Max(rightOffsets[priorIndex], rightOffsets[nextIndex]);
                        maxOffset = MathF.Min(maxOffset, leftOffsets[i] - nodeRange * 2f);

                        rightOffsets[i] = _random.NextFloat(minOffset, maxOffset);
                        isSmoothed = false;
                    }
                }

                if (!rightOffsets[i].IsValid() || !leftOffsets[i].IsValid())
                {
                    Log.Error("Offsets invalid! leftOffsets[" + i.ToString() + "] = " + leftOffsets[i].ToString() + " | rightOffsets[" + i.ToString() + "] = " + rightOffsets[i].ToString());
                }
            }
        }

        //DEBUG
        //List<Vector2> maxLeft = new();
        //List<Vector2> maxRight = new();
        //for (int i = 0; i < nodeAmount; i++)
        //{
        //    var midNode = Vector2.Lerp(mid1, mid2, i * nodeDivisor);
        //    maxLeft.Add(midNode + interwellNormal * maxLeftDistances[i]);
        //    maxRight.Add(midNode + -interwellNormal * maxRightDistances[i]);
        //}
        //END DEBUG

        for (int i = 1; i < lastNodeIndex; i++)
        {
            var midNode = Vector2.Lerp(mid1, mid2, i * nodeDivisor);
            var leftNode = Vector2.Lerp(left1, left2, i * nodeDivisor);
            var rightNode = Vector2.Lerp(right1, right2, i * nodeDivisor);

            leftSidePoints[i] = midNode + interwellNormal * leftOffsets[i];
            rightSidePoints[i] = midNode + interwellNormal * rightOffsets[i];
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

        //DEBUG STUFF
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
        //PopulateRiverFromVector2List(mapId, maxLeft);
        //PopulateRiverFromVector2List(mapId, maxRight);
        //END OF DEBUG STUFF                

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
                var nearbyPos = _transform.GetWorldPosition(node);
                var nodeDistanceSquared = (nearbyPos - nodePositions[i]).LengthSquared();
                var isBetterFit = false;
                if (mergeNodeDistanceSquared == -1f || nodeDistanceSquared < mergeNodeDistanceSquared)
                {
                    isBetterFit = true;
                }
                if ((angleDelta < Angle.FromDegrees(100d) && isBetterFit) || nearbyPos == nodePositions[i])
                {
                    mergeNodeDistanceSquared = nodeDistanceSquared;
                    mergeNode = node;
                }
            }

            if (double.IsNaN(flowDirection))
            {
                Log.Error("flowDirection is NaN!");
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

        var netSourceNode = GetNetEntity(sourceNode);
        var netEndNode = GetNetEntity(endNode);

        if (!endComp.PriorNodes.Contains(netSourceNode))
        {
            endComp.PriorNodes.Add(netSourceNode);
        }
        if (!sourceComp.NextNodes.Contains(netEndNode))
        {
            sourceComp.NextNodes.Add(netEndNode);
            sourceComp.SegmentControlPointMultiplier.Add(netEndNode, distance * 0.25f);
        }

        // Base it off the prior one(s) instead?
        if (!sourceComp.LockFlowDirection && sourceComp.NextNodes.Count > 1)
        {
            if (sourceComp.PriorNodes.Count == 1)
            {
                var priorNode = GetEntity(sourceComp.PriorNodes[0]);
                var priorPos = _transform.GetWorldPosition(priorNode);
                sourceComp.FlowDirection = Angle.FromWorldVec(sourcePos - priorPos);
            }
            else
            {
                var nextNodes = GetEntityList(sourceComp.NextNodes);
                Vector2 averageFlowDirection = new();
                foreach (var segment in nextNodes)
                {
                    var nextNodePos = _transform.GetWorldPosition(segment);
                    averageFlowDirection += nextNodePos - sourcePos;
                }
                sourceComp.FlowDirection = Angle.FromWorldVec(averageFlowDirection / sourceComp.NextNodes.Count);
            }
        }

        if (!endComp.LockFlowDirection && endComp.PriorNodes.Count > 1)
        {
            if (endComp.NextNodes.Count == 1)
            {
                var nextNode = GetEntity(endComp.NextNodes[0]);
                var nextPos = _transform.GetWorldPosition(nextNode);
                endComp.FlowDirection = Angle.FromWorldVec(nextPos - endPos);
            }
            else
            {
                var priorNodes = GetEntityList(endComp.PriorNodes);
                Vector2 averageFlowDirection = new();
                foreach (var segment in priorNodes)
                {
                    var priorNodePos = _transform.GetWorldPosition(segment);
                    averageFlowDirection += priorNodePos - sourcePos;
                }
                sourceComp.FlowDirection = Angle.FromWorldVec(averageFlowDirection / sourceComp.NextNodes.Count);
            }
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
                var netNextNode = GetNetEntity(nodes[0]);
                components[j].NextNodes.Add(netNextNode);
                var mod = radius * (2f / (float)nodeAmount);
                components[j].SegmentControlPointMultiplier.Add(netNextNode, mod);
            }
            else
            {
                var netNextNode = GetNetEntity(nodes[j + 1]);
                components[j].NextNodes.Add(netNextNode);
                var mod = radius * (2f / (float)nodeAmount);
                components[j].SegmentControlPointMultiplier.Add(netNextNode, mod);
            }
            components[j].LockFlowDirection = true;
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
                        var shortestDistance = Angle.ShortestDistance(connectionDirection, otherConnectionDir);
                        if (Math.Max(connectionDirection, otherConnectionDir) == connectionDirection) //Is otherConnection further right than Connection?
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
