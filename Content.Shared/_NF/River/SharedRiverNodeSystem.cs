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
