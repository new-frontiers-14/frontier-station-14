using Content.Shared._NF.River.Components;
using Content.Shared.Entry;
using Robust.Shared.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._NF.River;

public abstract partial class SharedRiverNodeSystem : EntitySystem
{
    public readonly struct NodeLink(
        EntityUid node,
        Vector2 controlPoint)
    {
        public EntityUid Node { get; } = node;
        public Vector2 ControlPoint { get; } = controlPoint;
    }
    /// <summary>
    /// This function calculates where two lines, defined by a location vector and a direction, intersect. If the angle is too narrow, it defaults to the middle point between the two points
    /// </summary>
    /// <param name="vector1">Location of the first line</param>
    /// <param name="direction1">Direction of the first line</param>
    /// <param name="vector2">Location of the second line</param>
    /// <param name="direction2">Direction of the second line</param>
    /// <returns>The location of the intersect point, or the halfway point if the angle between the two vectors is too small.</returns>
    public static Vector2 CalculateLinesIntersect(Vector2 vector1, Angle direction1, Vector2 vector2, Angle direction2)
    {
        var intersectPoint = new Vector2(0, 0);
        var vec1Dir = direction1.ToVec() + vector1;
        var vec2Dir = direction2.ToVec() + vector2;
        var divisor = (vector1.X - vec1Dir.X) * (vector2.Y - vec2Dir.Y) - (vector1.Y - vec1Dir.Y) * (vector2.X - vec2Dir.X);

        //If the directions are not equal, find where the direction-lines cross. This is the actual intersect point
        if (divisor != 0f)
        {
            // Line intersection math, sorry.
            intersectPoint.X = ((vector1.X * vec1Dir.Y - vector1.Y * vec1Dir.X) *
                (vector2.X - vec2Dir.X) - (vector1.X - vec1Dir.X) *
                (vector2.X * vec2Dir.Y - vector2.Y * vec2Dir.X)) / divisor;

            intersectPoint.Y = ((vector1.X * vec1Dir.Y - vector1.Y * vec1Dir.X) *
                (vector2.Y - vec2Dir.Y) - (vector1.Y - vec1Dir.Y) *
                (vector2.X * vec2Dir.Y - vector2.Y * vec2Dir.X)) / divisor;
            var test = Vector2.Dot(intersectPoint - vector1, direction1.ToVec());
            //Make sure the results are sane, if not, fall back on setting the intersect at the halfway point between the points.
            if (test < 0f)
            {
                var pointLocationDifference = vector2 - vector1;
                intersectPoint = vector1 + (pointLocationDifference / 2); // Halfway between vector1 and vector2.
            }
        }
        else
        {
            var pointLocationDifference = vector2 - vector1;
            intersectPoint = vector1 + (pointLocationDifference / 2); // Halfway between vector1 and vector2.
        }
        return (intersectPoint);
    }

    public List<EntityUid> GetNextNodes(RiverNodeComponent node)
    {
        return GetEntityList(node.NextNodes);
    }

    public List<EntityUid> GetPriorNodes(RiverNodeComponent node)
    {
        return GetEntityList(node.PriorNodes);
    }

    /// <summary>
    /// Checks if an entity (a node) is in the NetNodeLink list.
    /// </summary>
    /// <param name="entity">The node you wish to check the presence of</param>
    /// <param name="links">The list of links you wish to check</param>
    /// <returns>Returns true if the entity is in the links.</returns>
    //public bool ContainsNode(EntityUid entity, List<NetNodeLink> links)
    //{
    //    foreach (var link in links)
    //    {
    //        var localNode = GetEntity(link.Node);
    //        if (localNode == entity)
    //        {
    //            return true;
    //        }
    //    }
    //    return false;
    //}

    public static Vector2 CalculateBezierTangent(Vector2 entryPoint, Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint, float t)
    {
        var s = 1 - t;
        return MathF.Pow(s, 2) * 3 * (controlPoint1 - entryPoint) + 2 * s * t * 3 * (controlPoint2 - controlPoint1) + MathF.Pow(t, 2) * 3 * (endPoint - controlPoint2);
    }

    public static Vector2 CalculatePointOnBezier(Vector2 entryPoint, Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint, float t)
    {
        var s = 1 - t;
        return entryPoint * MathF.Pow(s, 3) + controlPoint1 * 3.0f * MathF.Pow(s, 2) * t + controlPoint2 * 3.0f * s * MathF.Pow(t, 2) + endPoint * MathF.Pow(t, 3);
    }
}
