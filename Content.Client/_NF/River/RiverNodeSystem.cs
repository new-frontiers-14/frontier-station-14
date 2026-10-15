//using Content.Client._NF.River.Components;
using Content.Shared._NF.River;
using Content.Shared._NF.River.Components;
using Content.Shared._NF.River.Events;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Client.GameObjects;
using Robust.Shared.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Client._NF.River;

public sealed partial class RiverNodeSystem : SharedRiverNodeSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public readonly Dictionary<EntityUid, Vector2[]> MeshData = new();
    private List<EntityUid> _nodeList = new();

    // Testing
    public List<Vector2?> MiddleLine = new();
    // End Testing

    public override void Initialize()
    {
        base.Initialize();
        SubscribeCvars();
        SubscribeNetworkEvent<TransferRiverNodesEvent>(TransferNodes);
    }

    private void TransferNodes(TransferRiverNodesEvent transferEvent)
    {
        if (transferEvent?.NodeList == null)
        {
            _nodeList.Clear();
            return;
        }
        _nodeList = GetEntityList(transferEvent.NodeList);
        UpdateAllNodes();
    }
    private void UpdateAllNodes()
    {
        foreach (var node in _nodeList)
        {
            UpdateRiverSegmentMesh(node, SegmentRenderResolution);
        }
    }
    public List<EntityUid> GetNodeList()
    {
        return _nodeList;
    }

    public bool TryGetNodeData(EntityUid node, [NotNullWhen(true)] out RiverNodeComponent? nodeComp)
    {
        var returnValue = TryComp<RiverNodeComponent>(node, out var tempComp);
        nodeComp = tempComp;
        return returnValue;
    }

    public static List<Vector2> CalcCubicCurveSections(Vector2 entryPoint, Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint, int resolution)
    {
        List<Vector2> result = new();
        float increment = 1f / resolution;
        for (var i = 0; i <= resolution; i++)
        {
            var section = i * increment;
            var newPoint = entryPoint * MathF.Pow(1 - section, 3) + controlPoint1 * 3.0f * MathF.Pow(1 - section, 2) * section + controlPoint2 * 3.0f * (1 - section) * MathF.Pow(section, 2) + endPoint * MathF.Pow(section, 3);
            result.Add(newPoint);
        }
        return result;
    }

    public bool UpdateRiverBoundingBox(EntityUid entryNode)
    {
        if (!TryComp<RiverNodeComponent>(entryNode, out var entryComp))
        {
            return false;
        }
        var nextNodes = GetNextNodes(entryComp);
        foreach (var endNode in nextNodes)
        {
            if (!TryComp<RiverNodeComponent>(endNode, out var endComp))
            {
                continue;
            }
            //Check for culling, makes a bounding box around the segment, and checks if it intersects the viewport.
            //TODO: Figure out a tighter fit, less naive.
            var entryXForm = entryComp.Location;
            var exitXForm = _transform.GetWorldPosition(endNode);
            var control1XForm = entryXForm + entryComp.ForwardControlPoint;
            var control2XForm = entryXForm + -endComp.ForwardControlPoint;
            var centrePoint = (entryXForm + control1XForm + control2XForm + exitXForm) / 4f;

            var maxX = MathF.Max(MathF.Max(entryXForm.X, control1XForm.X), MathF.Max(control2XForm.X, exitXForm.X)) + entryComp.NodeRange;
            var maxY = MathF.Max(MathF.Max(entryXForm.Y, control1XForm.Y), MathF.Max(control2XForm.Y, exitXForm.Y)) + entryComp.NodeRange;
            var minX = MathF.Min(MathF.Min(entryXForm.X, control1XForm.X), MathF.Min(control2XForm.X, exitXForm.X)) - entryComp.NodeRange;
            var minY = MathF.Min(MathF.Min(entryXForm.Y, control1XForm.Y), MathF.Min(control2XForm.Y, exitXForm.Y)) - entryComp.NodeRange;

            var boxHeight = maxY - minY;
            var boxWidth = maxX - minX;
            var boxSize = new Vector2(boxWidth, boxHeight);

            var boundingBox = Box2.CenteredAround(centrePoint, boxSize);
        }
        return true;
    }

    /// <summary>
    /// Creates a Triangle List for the river segment
    /// </summary>
    /// <param name="entryNode">The node which indicates the entry of the segment.</param>
    /// <param name="resolution">The amount of segments the river segment(s) should be divided in for the mesh</param>
    /// <returns>true if successful, false if failed</returns>
    public bool UpdateRiverSegmentMesh(EntityUid entryNode, int resolution)
    {
        if (!TryComp<RiverNodeComponent>(entryNode, out var nodeComp))
        {
            return false;
        }

        if (nodeComp.NextNodes.Count == 1)
        {
            var exitNode = GetNextNodes(nodeComp)[0];
            if (!TryComp<RiverNodeComponent>(exitNode, out var exitComp))
            {
                return false;
            }

            if (exitComp.PriorNodes.Count <= 1)
            {
                return HandleLoneSegment(entryNode, nodeComp, exitNode, exitComp, resolution);
            }
            else
            {
                //return HandleLoneSegment(entryNode, nodeComp, nodeLink, exitComp, resolution);
                return false;// HandleConvergingSegments(entryNode,/* nodeComp, */node, exitComp, resolution);
            }
        }
        else if (nodeComp.NextNodes.Count != 0)
        {
            // If a Sprouting Segment is also Converging... Yea, It should throw an error somewhere.
            return false;// HandleSproutingSegments(entryNode, nodeComp, resolution);
        }
        else
        {
            //Handle endcaps.
            return true;
        }
    }

    /// <summary>
    /// A method only to be used in UpdateRiverSegmentMesh. Primarily made to make it easier to work on this and parse for myself.
    /// This method handles Mesh-building and updating for a River Segment that has nothing converging into it, or sprouting from it.
    /// </summary>
    /// <param name="entryNode">The node from which the flow enters</param>
    /// <param name="nodeComp">The RiverNodeComponent of the entryNode</param>
    /// <param name="nodeLink">the NodeLink connecting the entryNode to the exit node</param>
    /// <param name="exitComp">the RiverNodeComponent of the exit node</param>
    /// <param name="resolution">The amount of segments the river segment should be divided in for the mesh</param>
    /// <returns>Whether the method was successful</returns>
    private bool HandleLoneSegment(EntityUid entryNode, RiverNodeComponent nodeComp, EntityUid exitNode, RiverNodeComponent exitComp, int resolution)
    {
        if (resolution < 2) //Safety measure to prevent breaking things. TODO: Ensure a resolution of 2 doesn't break things. (Do not remove TODO until code is finished)
        {
            resolution = 2;
        }
        //There's a lot of mention of "Left" and "Right" in this code... I do not actually know for sure if it *is* left and right, or the other way around.
        List<Vector2> protoMesh = new();
        var nextNodes = GetNextNodes(nodeComp);
        Vector2 entryPoint = _transform.GetWorldPosition(entryNode);
        Vector2 endPoint = _transform.GetWorldPosition(exitNode);

        //Vector2 controlPoint = nodeLink.ControlPoint;
        Vector2 controlPoint1 = entryPoint + nodeComp.ForwardControlPoint;
        Vector2 controlPoint2 = endPoint + -exitComp.ForwardControlPoint;

        float increment = 1f / resolution;
        for (var i = 0; i < resolution; i++)
        {
            Vector2 point1;
            Vector2 tangent1;
            Vector2 normal1;
            Vector2 point2;
            Vector2 tangent2;
            Vector2 normal2;
            // Figure out the points along the line from which the Vertices will be extruded, as well as their tangents.
            var t1 = i * increment;
            var t2 = (i + 1) * increment;
            if (i == 0)
            {
                point1 = entryPoint;
                point2 = CalculatePointOnBezier(entryPoint, controlPoint1, controlPoint2, endPoint, t2);// entryPoint * MathF.Pow(s2, 3) + controlPoint1 * 3.0f * MathF.Pow(s2, 2) * t2 + controlPoint2 * 3.0f * s2 * MathF.Pow(t2, 2) + endPoint * MathF.Pow(t2, 3);
                tangent1 = nodeComp.FlowDirection.ToVec();
                tangent2 = CalculateBezierTangent(entryPoint, controlPoint1, controlPoint2, endPoint, t2);
            }
            else if (i == resolution - 1)
            {
                point1 = CalculatePointOnBezier(entryPoint, controlPoint1, controlPoint2, endPoint, t1);
                point2 = endPoint;
                tangent1 = CalculateBezierTangent(entryPoint, controlPoint1, controlPoint2, endPoint, t1);
                tangent2 = exitComp.FlowDirection.ToVec();
            }
            else
            {
                point1 = CalculatePointOnBezier(entryPoint, controlPoint1, controlPoint2, endPoint, t1);
                point2 = CalculatePointOnBezier(entryPoint, controlPoint1, controlPoint2, endPoint, t2);
                tangent1 = CalculateBezierTangent(entryPoint, controlPoint1, controlPoint2, endPoint, t1);
                tangent2 = CalculateBezierTangent(entryPoint, controlPoint1, controlPoint2, endPoint, t2);
            }
            // Find Normals (Left of the tangent)
            normal1 = Vector2.Normalize(new Vector2(-tangent1.Y, tangent1.X));
            normal2 = Vector2.Normalize(new Vector2(-tangent2.Y, tangent2.X));

            //Construct the vertices of a section, extruding along and counter to the normals at the range of the river's influence.
            var right1 = point1 + normal1 * nodeComp.NodeRange;
            var left1 = point1 - normal1 * nodeComp.NodeRange;
            var right2 = point2 + normal2 * nodeComp.NodeRange;
            var left2 = point2 - normal2 * nodeComp.NodeRange;

            //Form the polygons.
            protoMesh.Add(right1);
            protoMesh.Add(left1);
            protoMesh.Add(right2);

            protoMesh.Add(left1);
            protoMesh.Add(right2);
            protoMesh.Add(left2);
        }

        if (MeshData.ContainsKey(entryNode))
        {
            MeshData[entryNode] = protoMesh.ToArray();
        }
        else
        {
            MeshData.Add(entryNode, protoMesh.ToArray());
        }
        return true;
    }

    /// <summary>
    /// A method only to be used in UpdateRiverSegmentMesh. Primarily made to make it easier to work on this and parse for myself.
    /// This method handles Mesh-Building for a river segment which has other segments converging into it.
    /// </summary>
    /// <param name="entryNode">The node from which the flow enters</param>
    /// <param name="nodeComp">The RiverNodeComponent of the entryNode</param>
    /// <param name="resolution">The amount of segments the river segment should be divided in for the mesh</param>
    /// <returns>Whether the method was successful</returns>
    //private bool HandleConvergingSegments(EntityUid currentNode,/* RiverNodeComponent nodeComp,*/ EntityUid exitNode, RiverNodeComponent exitComp, int resolution)
    //{
    //    if (resolution< 2) //Safety measure to prevent breaking things. TODO: Ensure a resolution of 2 doesn't break things. (Do not remove TODO until code is finished)
    //    {
    //        resolution = 2;
    //    }
    //    List<Vector2> protoMesh = new();
    //    var nodeLinks = GetPriorNodes(exitComp);
    //    Vector2[,] segmentSections = new Vector2[nodeLinks.Count, resolution + 1]; // Holds all the sections of each segment so they can be easily compared.
    //    Vector2[,] sectionNormals = new Vector2[nodeLinks.Count, resolution + 1]; // Holds all the normals for each section of each segment.
    //    Vector2[] controlPoints = new Vector2[nodeLinks.Count]; // Holds all the control points of each segment.
    //    Vector2?[,] mergePoints = new Vector2?[2, resolution + 1]; // Stores the points where the two rivers should, [0, #] is left, [1, #] is right of the current segment.
    //    const int left = 0; // For the mergePoints indices.
    //    const int right = 1; // For the mergePoints indices.
    //    int[] orderLToR = new int[nodeLinks.Count]; // An ordered array of indices showing what order from left to right the segments diverge. 1\ 2| 3/ etc.
    //    int currentNodeSegment = -1; // The identifier for the currentNode to allow it to survive the shift to using orderLToR.
    //
    //    // These three variables are to ensure that we focus on the currentNode, and don't do all the calculations all the time.
    //    int leftSegmentFromLeft = -1;
    //    int currentSegmentFromLeft = -1;
    //    int rightSegmentFromLeft = -1;
    //
    //    var exitPoint = _transform.GetWorldPosition(exitNode);
    //
    //    float increment = 1f / resolution;
    //    for (var i = 0; i < resolution; i++)
    //    {
    //        var i2 = i + 1;
    //
    //        var segmentCounter = 0;
    //        var t1 = i * increment;
    //        var t2 = (i + 1) * increment;
    //        // calculates where the points are which will extrude into vertices.
    //        foreach (var node in nodeLinks)
    //        {
    //            Vector2 point1;
    //            Vector2 tangent1;
    //            Vector2 point2;
    //            Vector2 tangent2;
    //            //var controlPoint = node.ControlPoint;
    //            var entryPoint = _transform.GetWorldPosition(node);
    //
    //            if (node == currentNode)
    //            {
    //                currentNodeSegment = segmentCounter;
    //            }
    //
    //            if (!TryComp<RiverNodeComponent>(node, out var entryComp))
    //            {
    //                return false;
    //            }
    //            if (entryComp.NextNodes.Count != 1)
    //            {
    //                Log.Warning("A river segment should never have both sprouting and converging segments coming in and out of it!");
    //            }
    //
    //            if (i == 0)
    //            {
    //                point1 = entryPoint;
    //                //point2 = MathF.Pow((1 - t2), 2) * entryPoint + 2 * (1 - t2) * t2 * controlPoint + MathF.Pow(t2, 2) * exitPoint;
    //                tangent1 = entryComp.FlowDirection.ToVec();
    //                //tangent2 = 2 * (1 - t2) * (controlPoint - entryPoint) + 2 * t2 * (exitPoint - controlPoint);
    //            }
    //            else if (i == resolution - 1)
    //            {
    //                //point1 = MathF.Pow((1 - t1), 2) * entryPoint + 2 * (1 - t1) * t1 * controlPoint + MathF.Pow(t1, 2) * exitPoint;
    //                point2 = exitPoint;
    //                //tangent1 = 2 * (1 - t1) * (controlPoint - entryPoint) + 2 * t1 * (exitPoint - controlPoint);
    //                tangent2 = exitComp.FlowDirection.ToVec();
    //            }
    //            else
    //            {
    //                //point1 = MathF.Pow((1 - t1), 2) * entryPoint + 2 * (1 - t1) * t1 * controlPoint + MathF.Pow(t1, 2) * exitPoint;
    //                //point2 = MathF.Pow((1 - t2), 2) * entryPoint + 2 * (1 - t2) * t2 * controlPoint + MathF.Pow(t2, 2) * exitPoint;
    //                //tangent1 = 2 * (1 - t1) * (controlPoint - entryPoint) + 2 * t1 * (exitPoint - controlPoint);
    //                //tangent2 = 2 * (1 - t2) * (controlPoint - entryPoint) + 2 * t2 * (exitPoint - controlPoint);
    //            }
    //            segmentSections[segmentCounter, i] = point1;
    //            segmentSections[segmentCounter, i2] = point2;
    //
    //            sectionNormals[segmentCounter, i] = Vector2.Normalize(new Vector2(-tangent1.Y, tangent1.X));
    //            sectionNormals[segmentCounter, i2] = Vector2.Normalize(new Vector2(-tangent2.Y, tangent2.X));
    //
    //            controlPoints[segmentCounter] = controlPoint;
    //
    //            segmentCounter++;
    //        }
    //
    //        if (currentNodeSegment == -1)
    //        {
    //            return false;
    //        }
    //
    //        //Sorts the segments from left to right so that the following processes can run more efficiently.
    //        if (i == 0)
    //        {
    //            var flowDirVec = exitComp.FlowDirection.ToVec();
    //            var exitPointNormal = Vector2.Normalize(new Vector2(-flowDirVec.Y, flowDirVec.X));
    //            for (var segment = 0; segment < nodeLinks.Count; segment++)
    //            {
    //                var divergence = Vector2.Dot(segmentSections[segment, i], exitPointNormal); //How far along the normal does it project? Used for discovering leftness.
    //                var amountMoreLeft = 0;
    //                for (var otherSegment = 0; otherSegment < nodeLinks.Count; otherSegment++)
    //                {
    //                    if (segment == otherSegment)
    //                    {
    //                        continue;
    //                    }
    //                    var otherDivergence = Vector2.Dot(segmentSections[otherSegment, i], exitPointNormal); //How far along the normal does it project? Used for discovering leftness.
    //                    if (otherDivergence > divergence)
    //                    {
    //                        amountMoreLeft++;
    //                    }
    //                }
    //                orderLToR[amountMoreLeft] = segment;
    //                if (segment == currentNodeSegment)
    //                {
    //                    currentSegmentFromLeft = amountMoreLeft;
    //                    if (amountMoreLeft != 0)
    //                    {
    //                        leftSegmentFromLeft = amountMoreLeft - 1;
    //                    }
    //                    if (amountMoreLeft != nodeLinks.Count - 1)
    //                    {
    //                        rightSegmentFromLeft = amountMoreLeft + 1;
    //                    }
    //                }
    //            }
    //        }
    //        // Makes the Mesh for one section
    //        /*for (var segmentsFromLeft = leftSegmentIndex; segmentsFromLeft < nodeLinks.Count; segmentsFromLeft++)
    //        {*/
    //        var segmentCurrentIndex = orderLToR[currentSegmentFromLeft];// orderLToR[segmentsFromLeft];
    //        var segmentRightIndex = -1;
    //        if (rightSegmentFromLeft != -1)
    //        {
    //            segmentRightIndex = orderLToR[rightSegmentFromLeft];
    //        }
    //        var segmentLeftIndex = -1;
    //        if (leftSegmentFromLeft != -1)
    //        {
    //            segmentLeftIndex = orderLToR[leftSegmentFromLeft];
    //        }
    //
    //        bool needsSmoothing = false;
    //
    //        Vector2 smoothRight1 = new Vector2(0f, 0f);
    //        Vector2 smoothLeft1 = new Vector2(0f, 0f);
    //        Vector2 smoothRight2 = new Vector2(0f, 0f);
    //        Vector2 smoothLeft2 = new Vector2(0f, 0f);
    //
    //        Vector2 rightVert1 = new Vector2(0f, 0f);
    //        Vector2 leftVert1 = new Vector2(0f, 0f);
    //        Vector2 rightVert2 = new Vector2(0f, 0f);
    //        Vector2 leftVert2 = new Vector2(0f, 0f);
    //        var nodeRange = exitComp.NodeRange; // TODO: Make this more consistent, so that the NodeRange is always propagated downstream.
    //
    //        //Handle Right vertices
    //        if (segmentRightIndex == -1) //Rightmost segment
    //        {
    //            rightVert1 = segmentSections[segmentCurrentIndex, i] + sectionNormals[segmentCurrentIndex, i] * nodeRange;
    //            rightVert2 = segmentSections[segmentCurrentIndex, i2] + sectionNormals[segmentCurrentIndex, i2] * nodeRange;
    //        }
    //        else
    //        {
    //            var section1Distance = DistanceToSegment(segmentSections[segmentCurrentIndex, i], segmentSections[segmentRightIndex, resolution], controlPoints[segmentRightIndex], exitPoint, out var closestPoint1);
    //            var section2Distance = DistanceToSegment(segmentSections[segmentCurrentIndex, i2], segmentSections[segmentRightIndex, resolution], controlPoints[segmentRightIndex], exitPoint, out var closestPoint2);
    //            if (section2Distance < nodeRange * 2f)
    //            {
    //                var section2 = segmentSections[segmentCurrentIndex, i2];
    //                var midPoint2 = (section2 + closestPoint2) * 0.5f;
    //
    //                // If the previously checked section's ranges don't touch
    //                if (section1Distance >= nodeRange * 2f)
    //                {
    //                    if ((midPoint2 - segmentSections[segmentRightIndex, i2]).Length() < nodeRange)
    //                    {
    //                        midPoint2 = (section2 + segmentSections[segmentRightIndex, i2]) * 0.5f;
    //                        section2Distance = (section2 - segmentSections[segmentRightIndex, i2]).Length();
    //                    }
    //
    //                    var distanceToCircleIntersect = MathF.Sqrt(MathF.Pow(nodeRange, 2) - MathF.Pow(section2Distance * 0.5f, 2));
    //                    var sectionToMidPoint = midPoint2 - section2;
    //                    var midPointNormal = Vector2.Normalize(new Vector2(-sectionToMidPoint.Y, sectionToMidPoint.X));
    //                    var offset = midPointNormal * distanceToCircleIntersect;
    //                    rightVert2 = midPoint2 + offset;
    //
    //                    rightVert2 = closestPoint2; //TEST
    //
    //                    rightVert1 = segmentSections[segmentCurrentIndex, i2] + sectionNormals[segmentCurrentIndex, i2] * nodeRange;
    //
    //                    mergePoints[right, i] = null;
    //                    mergePoints[right, i2] = rightVert2;
    //
    //                    needsSmoothing = true;
    //                    smoothRight2 = rightVert1;
    //                    smoothRight1 = segmentSections[segmentCurrentIndex, i] + sectionNormals[segmentCurrentIndex, i] * nodeRange;
    //                    smoothLeft2 = segmentSections[segmentCurrentIndex, i] - sectionNormals[segmentCurrentIndex, i] * nodeRange;
    //                    smoothLeft1 = smoothLeft2;
    //                }
    //                else
    //                {
    //                    rightVert2 = midPoint2;
    //                    rightVert2 = closestPoint2; //TEST
    //
    //                    var section1 = segmentSections[segmentCurrentIndex, i];
    //                    var midPoint1 = (section1 + closestPoint1) * 0.5f;
    //
    //                    // If it's not the first loop, and the previously checked section's ranges don't touch
    //                    if (i == 0)
    //                    {
    //                        rightVert1 = segmentSections[segmentCurrentIndex, i] + sectionNormals[segmentCurrentIndex, i] * nodeRange;
    //                    }
    //                    else if (mergePoints[right, i + 1] == null)
    //                    {
    //                        var mergePoint = mergePoints[right, i];
    //                        if (mergePoint != null)
    //                        {
    //                            rightVert1 = (Vector2)mergePoint;
    //                            rightVert1 = closestPoint1;//TEST
    //                        }
    //                        else
    //                        {
    //                            //This shouldn't get hit, but just in case, here's a failsafe.
    //                            rightVert1 = midPoint1;
    //                            Log.Error("a RiverNodeSystem branch mergePoint was null when it shouldn't be in section " + i.ToString() + "of entry node " + currentNode.ToString());
    //                        }
    //                    }
    //                    else
    //                    {
    //                        rightVert1 = midPoint1;
    //                        rightVert1 = closestPoint1;//Test
    //                    }
    //                    mergePoints[right, i] = rightVert1;
    //                    mergePoints[right, i2] = rightVert2;
    //                }
    //            }
    //            else
    //            {
    //                rightVert2 = segmentSections[segmentCurrentIndex, i2] + sectionNormals[segmentCurrentIndex, i2] * nodeRange;
    //
    //                rightVert1 = segmentSections[segmentCurrentIndex, i] + sectionNormals[segmentCurrentIndex, i] * nodeRange;
    //                mergePoints[right, i] = null;
    //            }
    //        }
    //
    //        //Handle left vertices
    //        if (leftSegmentFromLeft == -1) //Leftmost segment
    //        {
    //            leftVert1 = segmentSections[segmentCurrentIndex, i] - sectionNormals[segmentCurrentIndex, i] * nodeRange;
    //            leftVert2 = segmentSections[segmentCurrentIndex, i2] - sectionNormals[segmentCurrentIndex, i2] * nodeRange;
    //        }
    //        else
    //        {
    //            var section1Distance = DistanceToSegment(segmentSections[segmentCurrentIndex, i], segmentSections[segmentLeftIndex, resolution], controlPoints[segmentLeftIndex], exitPoint, out var closestPoint1);
    //            var section2Distance = DistanceToSegment(segmentSections[segmentCurrentIndex, i2], segmentSections[segmentLeftIndex, resolution], controlPoints[segmentLeftIndex], exitPoint, out var closestPoint2);
    //            if (section2Distance < nodeRange * 2f)
    //            {
    //                var section2 = segmentSections[segmentCurrentIndex, i2];
    //                var midPoint2 = (section2 + closestPoint2) * 0.5f;
    //
    //                // If the previously checked section's ranges don't touch
    //                if (section1Distance >= nodeRange * 2f)
    //                {
    //                    if ((midPoint2 - segmentSections[segmentLeftIndex, i2]).Length() < nodeRange)
    //                    {
    //                        midPoint2 = (section2 + segmentSections[segmentLeftIndex, i2]) * 0.5f;
    //                        section2Distance = (section2 - segmentSections[segmentLeftIndex, i2]).Length();
    //                    }
    //
    //                    var distanceToCircleIntersect = MathF.Sqrt(MathF.Pow(nodeRange, 2) - MathF.Pow(section2Distance * 0.5f, 2));
    //                    var sectionToMidPoint = midPoint2 - section2;
    //                    var midPointNormal = Vector2.Normalize(new Vector2(-sectionToMidPoint.Y, sectionToMidPoint.X));
    //                    var offset = midPointNormal * distanceToCircleIntersect;
    //                    leftVert2 = midPoint2 + offset;
    //                    leftVert2 = closestPoint2;//Test
    //
    //                    leftVert1 = segmentSections[segmentCurrentIndex, i2] + sectionNormals[segmentCurrentIndex, i2] * nodeRange;
    //
    //                    mergePoints[left, i] = null;
    //                    mergePoints[left, i2] = leftVert2;
    //
    //                    needsSmoothing = true;
    //                    smoothLeft2 = leftVert1;
    //                    smoothLeft1 = segmentSections[segmentCurrentIndex, i] + sectionNormals[segmentCurrentIndex, i] * nodeRange;
    //                    smoothRight2 = segmentSections[segmentCurrentIndex, i] - sectionNormals[segmentCurrentIndex, i] * nodeRange;
    //                    smoothRight1 = smoothRight2;
    //                }
    //                else
    //                {
    //                    leftVert2 = midPoint2;
    //                    leftVert2 = closestPoint2;//Test
    //
    //                    var section1 = segmentSections[segmentCurrentIndex, i];
    //                    var midPoint1 = (section1 + closestPoint1) * 0.5f;
    //
    //                    // If it's not the first loop, and the previously checked section's ranges don't touch
    //                    if (i == 0)
    //                    {
    //                        leftVert1 = segmentSections[segmentCurrentIndex, i] + sectionNormals[segmentCurrentIndex, i] * nodeRange;
    //                    }
    //                    else if (mergePoints[left, i + 1] == null)
    //                    {
    //                        var mergePoint = mergePoints[left, i];
    //                        if (mergePoint != null)
    //                        {
    //                            leftVert1 = (Vector2)mergePoint;
    //                            leftVert1 = closestPoint1;//Test
    //                        }
    //                        else
    //                        {
    //                            //This shouldn't get hit, but just in case, here's a failsafe.
    //                            leftVert1 = midPoint1;
    //                            leftVert1 = closestPoint1;//Test
    //                            Log.Error("a RiverNodeSystem branch mergePoint was null when it shouldn't be in section " + i.ToString() + "of entry node " + currentNode.ToString());
    //                        }
    //                    }
    //                    else
    //                    {
    //                        leftVert1 = midPoint1;
    //                        leftVert1 = closestPoint1;//Test
    //                    }
    //                    mergePoints[left, i] = leftVert1;
    //                    mergePoints[left, i2] = leftVert2;
    //                }
    //            }
    //            else
    //            {
    //                leftVert2 = segmentSections[segmentCurrentIndex, i2] + sectionNormals[segmentCurrentIndex, i2] * nodeRange;
    //
    //                leftVert1 = segmentSections[segmentCurrentIndex, i] + sectionNormals[segmentCurrentIndex, i] * nodeRange;
    //                mergePoints[left, i] = null;
    //            }
    //        }
    //
    //        //Form the polygons.
    //        if (needsSmoothing)
    //        {
    //            protoMesh.Add(smoothRight1);
    //            protoMesh.Add(smoothLeft1);
    //            protoMesh.Add(smoothRight2);
    //
    //            protoMesh.Add(smoothLeft1);
    //            protoMesh.Add(smoothRight2);
    //            protoMesh.Add(smoothLeft2);
    //        }
    //
    //        protoMesh.Add(rightVert1);
    //        protoMesh.Add(leftVert1);
    //        protoMesh.Add(rightVert2);
    //
    //        protoMesh.Add(leftVert1);
    //        protoMesh.Add(rightVert2);
    //        protoMesh.Add(leftVert2);
    //    }
    //    //}
    //    if (!MeshData.ContainsKey(currentNode))
    //    {
    //        MeshData.Add(currentNode, protoMesh.ToArray());
    //    }
    //    else
    //    {
    //        MeshData[currentNode] = protoMesh.ToArray();
    //    }
    //
    //    // Testing
    //    for (var i = 0; i < mergePoints.Length / 2 - 1; i++)
    //    {
    //        var left1 = mergePoints[0, i];
    //        var left2 = mergePoints[0, i + 1];
    //        var right1 = mergePoints[1, i];
    //        var right2 = mergePoints[1, i + 1];
    //        MiddleLine.Add(left1);
    //        MiddleLine.Add(left2);
    //        MiddleLine.Add(right1);
    //        MiddleLine.Add(right2);
    //    }
    //    // End Testing
    //
    //    return true;
    //}

    /// <summary>
    /// A method only to be used in UpdateRiverSegmentMesh. Primarily made to make it easier to work on this and parse for myself.
    /// This method handles Mesh-Building for a river segment which has other segments sprouting out of it.
    /// </summary>
    /// <param name="entryNode">The node from which the flow enters</param>
    /// <param name="nodeComp">The RiverNodeComponent of the entryNode</param>
    /// <param name="resolution">The amount of segments the river segment should be divided in for the mesh</param>
    /// <returns>Whether the method was successful</returns>
    //private bool HandleSproutingSegments(EntityUid entryNode, RiverNodeComponent nodeComp, int resolution)
    //{
    //    if (resolution< 2) //Safety measure to prevent breaking things. TODO: Ensure a resolution of 2 doesn't break things. (Do not remove TODO until code is finished)
    //    {
    //        resolution = 2;
    //    }
    //    List<Vector2> protoMesh = new();
    //    var nodeLinks = GetNextNodes(nodeComp);
    //    Vector2[,] segmentSections = new Vector2[nodeLinks.Count, resolution + 1]; // Holds all the sections of each segment so they can be easily compared.
    //    Vector2[,] sectionNormals = new Vector2[nodeLinks.Count, resolution + 1]; // Holds all the normals for each section of each segment.
    //    Vector2[] controlPoints = new Vector2[nodeLinks.Count]; // Holds all the control points of each segment.
    //    Vector2?[,] mergePointsRight = new Vector2?[nodeLinks.Count - 1, resolution + 1]; // Stores the points where the two rivers should merge.
    //    int[] orderLToR = new int[nodeLinks.Count]; // An ordered array of indices showing what order from left to right the segments diverge. 1\ 2| 3/ etc.
    //
    //    var entryPoint = _transform.GetWorldPosition(entryNode);
    //
    //    float increment = 1f / resolution;
    //    for (var i = resolution; i > 0; i--)
    //    {
    //        var i2 = i - 1;
    //
    //        var segmentCounter = 0;
    //        var t1 = i * increment;
    //        var t2 = (i - 1) * increment;
    //        // calculates where the points are which will extrude into vertices.
    //        foreach (var node in nodeLinks)
    //        {
    //            Vector2 point1;
    //            Vector2 tangent1;
    //            Vector2 point2;
    //            Vector2 tangent2;
    //            var controlPoint = node.ControlPoint;
    //            var endPoint = _transform.GetWorldPosition(node.Node);
    //
    //            if (!TryComp<RiverNodeComponent>(node.Node, out var exitComp))
    //            {
    //                return false;
    //            }
    //            if (exitComp.PriorNodes.Count != 1)
    //            {
    //                Log.Warning("A river segment should never have both sprouting and converging segments coming in and out of it!");
    //            }
    //
    //            if (i == resolution)
    //            {
    //                point1 = endPoint;
    //                point2 = MathF.Pow((1 - t2), 2) * entryPoint + 2 * (1 - t2) * t2 * controlPoint + MathF.Pow(t2, 2) * endPoint;
    //                tangent1 = exitComp.FlowDirection.ToVec();
    //                tangent2 = 2 * (1 - t2) * (controlPoint - entryPoint) + 2 * t2 * (endPoint - controlPoint);
    //            }
    //            else if (i == 1)
    //            {
    //                point1 = MathF.Pow((1 - t1), 2) * entryPoint + 2 * (1 - t1) * t1 * controlPoint + MathF.Pow(t1, 2) * endPoint;
    //                point2 = entryPoint;
    //                tangent1 = 2 * (1 - t1) * (controlPoint - entryPoint) + 2 * t1 * (endPoint - controlPoint);
    //                tangent2 = nodeComp.FlowDirection.ToVec();
    //            }
    //            else
    //            {
    //                point1 = MathF.Pow((1 - t1), 2) * entryPoint + 2 * (1 - t1) * t1 * controlPoint + MathF.Pow(t1, 2) * endPoint;
    //                point2 = MathF.Pow((1 - t2), 2) * entryPoint + 2 * (1 - t2) * t2 * controlPoint + MathF.Pow(t2, 2) * endPoint;
    //                tangent1 = 2 * (1 - t1) * (controlPoint - entryPoint) + 2 * t1 * (endPoint - controlPoint);
    //                tangent2 = 2 * (1 - t2) * (controlPoint - entryPoint) + 2 * t2 * (endPoint - controlPoint);
    //            }
    //            segmentSections[segmentCounter, i] = point1;
    //            segmentSections[segmentCounter, i2] = point2;
    //
    //            sectionNormals[segmentCounter, i] = -Vector2.Normalize(new Vector2(-tangent1.Y, tangent1.X));
    //            sectionNormals[segmentCounter, i2] = -Vector2.Normalize(new Vector2(-tangent2.Y, tangent2.X));
    //
    //            controlPoints[segmentCounter] = controlPoint;
    //
    //            segmentCounter++;
    //        }
    //
    //        //Sorts the segments from left to right so that the following processes can run more efficiently.
    //        if (i == resolution)
    //        {
    //            var flowDirVec = nodeComp.FlowDirection.ToVec();
    //            var entryPointNormal = Vector2.Normalize(new Vector2(-flowDirVec.Y, flowDirVec.X));
    //            for (var segment = 0; segment < nodeLinks.Count; segment++)
    //            {
    //                var divergence = Vector2.Dot(segmentSections[segment, i], entryPointNormal); //How far along the normal does it project? Used for discovering leftness.
    //                var amountMoreLeft = 0;
    //                for (var otherSegment = 0; otherSegment < nodeLinks.Count; otherSegment++)
    //                {
    //                    if (segment == otherSegment)
    //                    {
    //                        continue;
    //                    }
    //                    var otherDivergence = Vector2.Dot(segmentSections[otherSegment, i], entryPointNormal); //How far along the normal does it project? Used for discovering leftness.
    //                    if (otherDivergence > divergence)
    //                    {
    //                        amountMoreLeft++;
    //                    }
    //                }
    //                orderLToR[amountMoreLeft] = segment;
    //            }
    //        }
    //
    //        // Makes the Mesh for one section of every segment
    //        for (var segmentsFromLeft = 0; segmentsFromLeft < nodeLinks.Count; segmentsFromLeft++)
    //        {
    //            var segmentIndex = orderLToR[segmentsFromLeft];
    //
    //            bool needsSmoothing = false;
    //
    //            Vector2 smoothRight1 = new Vector2(0f, 0f);
    //            Vector2 smoothLeft1 = new Vector2(0f, 0f);
    //            Vector2 smoothRight2 = new Vector2(0f, 0f);
    //            Vector2 smoothLeft2 = new Vector2(0f, 0f);
    //
    //            Vector2 right1;
    //            Vector2 left1;
    //            Vector2 right2;
    //            Vector2 left2;
    //            var nodeRange = nodeComp.NodeRange;
    //
    //            //Handle Right vertices
    //            if (segmentsFromLeft == nodeLinks.Count - 1) //Rightmost segment
    //            {
    //                right1 = segmentSections[segmentIndex, i] + sectionNormals[segmentIndex, i] * nodeRange;
    //                right2 = segmentSections[segmentIndex, i2] + sectionNormals[segmentIndex, i2] * nodeRange;
    //            }
    //            else
    //            {
    //                var segmentRightIndex = orderLToR[segmentsFromLeft + 1];
    //                var section1Distance = DistanceToSegment(segmentSections[segmentIndex, i], entryPoint, controlPoints[segmentRightIndex], segmentSections[segmentRightIndex, resolution], out var closestPoint1);
    //                var section2Distance = DistanceToSegment(segmentSections[segmentIndex, i2], entryPoint, controlPoints[segmentRightIndex], segmentSections[segmentRightIndex, resolution], out var closestPoint2);
    //                if (section2Distance < nodeRange * 2f)
    //                {
    //                    var section2 = segmentSections[segmentIndex, i2];
    //                    var midPoint2 = (section2 + closestPoint2) * 0.5f;
    //
    //                    // If the previously checked section's ranges don't touch
    //                    if (section1Distance >= nodeRange * 2f)
    //                    {
    //                        if ((midPoint2 - segmentSections[segmentRightIndex, i2]).Length() < nodeRange)
    //                        {
    //                            midPoint2 = (section2 + segmentSections[segmentRightIndex, i2]) * 0.5f;
    //                            section2Distance = (section2 - segmentSections[segmentRightIndex, i2]).Length();
    //                        }
    //
    //                        var distanceToCircleIntersect = MathF.Sqrt(MathF.Pow(nodeRange, 2) - MathF.Pow(section2Distance * 0.5f, 2));
    //                        var sectionToMidPoint = midPoint2 - section2;
    //                        var midPointNormal = Vector2.Normalize(new Vector2(-sectionToMidPoint.Y, sectionToMidPoint.X));
    //                        var offset = midPointNormal * distanceToCircleIntersect;
    //                        right2 = midPoint2 + offset;
    //
    //                        right1 = segmentSections[segmentIndex, i2] + sectionNormals[segmentIndex, i2] * nodeRange; ;
    //
    //                        mergePointsRight[segmentsFromLeft, i] = null;
    //                        mergePointsRight[segmentsFromLeft, i2] = right2;
    //
    //                        needsSmoothing = true;
    //                        smoothRight2 = right1;
    //                        smoothRight1 = segmentSections[segmentIndex, i] + sectionNormals[segmentIndex, i] * nodeRange;
    //                        smoothLeft2 = segmentSections[segmentIndex, i] - sectionNormals[segmentIndex, i] * nodeRange;
    //                        smoothLeft1 = smoothLeft2;
    //                    }
    //                    else
    //                    {
    //                        right2 = midPoint2;
    //
    //                        var section1 = segmentSections[segmentIndex, i];
    //                        var midPoint1 = (section1 + closestPoint1) * 0.5f;
    //
    //                        // If it's not the first loop, and the previously checked section's ranges don't touch
    //                        if (i == resolution)
    //                        {
    //                            right1 = segmentSections[segmentIndex, i] + sectionNormals[segmentIndex, i] * nodeRange;
    //                        }
    //                        else if (mergePointsRight[segmentsFromLeft, i + 1] == null)
    //                        {
    //                            var mergePoint = mergePointsRight[segmentsFromLeft, i];
    //                            if (mergePoint != null)
    //                            {
    //                                right1 = (Vector2)mergePoint;
    //                            }
    //                            else
    //                            {
    //                                //This shouldn't get hit, but just in case, here's a failsafe.
    //                                right1 = midPoint1;
    //                                Log.Error("a RiverNodeSystem branch mergePoint was null when it shouldn't be in section " + i.ToString() + "of entry node " + entryNode.ToString());
    //                            }
    //                        }
    //                        else
    //                        {
    //                            right1 = midPoint1;
    //                        }
    //                        mergePointsRight[segmentsFromLeft, i] = right1;
    //                        mergePointsRight[segmentsFromLeft, i2] = right2;
    //                    }
    //                }
    //                else
    //                {
    //                    right2 = segmentSections[segmentIndex, i2] + sectionNormals[segmentIndex, i2] * nodeRange;
    //
    //                    right1 = segmentSections[segmentIndex, i] + sectionNormals[segmentIndex, i] * nodeRange;
    //                    mergePointsRight[segmentsFromLeft, i] = null;
    //                }
    //            }
    //
    //            //Handle left vertices
    //            if (segmentsFromLeft == 0) //Leftmost segment
    //            {
    //                left1 = segmentSections[segmentIndex, i] - sectionNormals[segmentIndex, i] * nodeRange;
    //                left2 = segmentSections[segmentIndex, i2] - sectionNormals[segmentIndex, i2] * nodeRange;
    //            }
    //            else
    //            {
    //                var segmentLeft = segmentsFromLeft - 1;
    //                var mergePoint1 = mergePointsRight[segmentLeft, i];
    //                var mergePoint2 = mergePointsRight[segmentLeft, i2];
    //                if (mergePoint2 != null)
    //                {
    //                    left2 = (Vector2)mergePoint2;
    //
    //                    if (i == resolution)
    //                    {
    //                        left1 = segmentSections[segmentIndex, i] - sectionNormals[segmentIndex, i] * nodeRange;
    //                    }
    //                    else if (mergePoint1 != null)
    //                    {
    //                        left1 = (Vector2)mergePoint1;
    //                    }
    //                    else
    //                    {
    //                        left1 = segmentSections[segmentIndex, i2] - sectionNormals[segmentIndex, i2] * nodeRange;
    //
    //                        needsSmoothing = true;
    //                        smoothLeft2 = left1;
    //                        smoothLeft1 = segmentSections[segmentIndex, i] - sectionNormals[segmentIndex, i] * nodeRange;
    //                        smoothRight2 = segmentSections[segmentIndex, i] + sectionNormals[segmentIndex, i] * nodeRange;
    //                        smoothRight1 = smoothRight2;
    //                    }
    //                }
    //                else
    //                {
    //                    left2 = segmentSections[segmentIndex, i2] - sectionNormals[segmentIndex, i2] * nodeRange;
    //                    left1 = segmentSections[segmentIndex, i] - sectionNormals[segmentIndex, i] * nodeRange;
    //                }
    //            }
    //
    //            //Form the polygons.
    //            if (needsSmoothing)
    //            {
    //                protoMesh.Add(smoothRight1);
    //                protoMesh.Add(smoothLeft1);
    //                protoMesh.Add(smoothRight2);
    //
    //                protoMesh.Add(smoothLeft1);
    //                protoMesh.Add(smoothRight2);
    //                protoMesh.Add(smoothLeft2);
    //            }
    //
    //            protoMesh.Add(right1);
    //            protoMesh.Add(left1);
    //            protoMesh.Add(right2);
    //
    //            protoMesh.Add(left1);
    //            protoMesh.Add(right2);
    //            protoMesh.Add(left2);
    //        }
    //    }
    //    if (MeshData.ContainsKey(entryNode))
    //    {
    //        MeshData[entryNode] = protoMesh.ToArray();
    //    }
    //    else
    //    {
    //        MeshData.Add(entryNode, protoMesh.ToArray());
    //    }
    //    return true;
    //}
}

