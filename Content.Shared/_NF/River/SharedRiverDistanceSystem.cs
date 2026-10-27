//Copyright(c) < 2024 > < Felix Potthast >
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software
//and associated documentation files (the "Software"), to deal in the Software without
//restriction, including without limitation the rights to use, copy, modify, merge, publish,
//distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
//Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies
//or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
//DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
//ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.
//Modified by Nawichusea (Blame me for any messes =P)

using System.Numerics;

namespace Content.Shared._NF.River;

public abstract partial class SharedRiverNodeSystem
{
    static private float Length2(Vector2 v)
    {
        return Vector2.Dot(v, v);
    }

    static public float DistanceToLineSegmentSquared(Vector2 p, Vector2 a, Vector2 b, out Vector2 closestPoint, out float outH)
    {
        Vector2 pa = p - a, ba = b - a;
        var h = Math.Clamp(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba), 0.0f, 1.0f);
        closestPoint = ba * h;
        outH = h;
        var result = Length2(pa - closestPoint);
        if (result == float.NaN)
        {
            result = 0;
        }
        return result;
    }

    /// <summary>
    /// Ascertains the approximate distance of a point to the Cubic Bezier curve. Uses linear segments for performance and ease of implementation reasons.
    /// </summary>
    /// <param name="point">The point whose distance to the curve you wish to ascertain</param>
    /// <param name="entryPoint">The entrypoint of the Bezier</param>
    /// <param name="controlPoint1">The control point which indicates the direction in which the flow enters the segment. Should already be modified to include the magnitude.</param>
    /// <param name="controlPoint2">The control point which indicates the inverse to the direction in which the flow leaves the segment. Should already be modified to be inversed and to include the magnitude.</param>
    /// <param name="exitPoint">The exitpoint of the Bezier</param>
    /// <param name="outQ">The approximate location of the closest point.</param>
    /// <param name="resolution">Determines the amount of sample points taken. Higher numbers can affect performance but increase accuracy.</param>
    /// <param name="comparative">True: will return a squared value exclusively to be used for comparing against other squared distance values. False: returns the true distance</param>
    /// <returns>Depending on the comparative setting, will either return the approximate distance to the Bezier, or the squared distance to the Bezier.</returns>
    public static float CubicDistanceToBezier(Vector2 point, Vector2 entryPoint, Vector2 controlPoint1, Vector2 controlPoint2, Vector2 exitPoint, out Vector2 outQ, out float outT, int resolution, bool comparative = false)
    {
        if (resolution < 2) //Safety measure to prevent the resolution from going low enough to break things.
        {
            resolution = 2;
        }
        outQ = new Vector2(0f, 0f);
        outT = 0f;
        float d0 = float.MaxValue;
        Vector2 a = entryPoint;
        for (int i = 1; i < resolution; i++)
        {
            var t = i / (float)(resolution - 1);
            var s = 1.0f - t;
            Vector2 b = entryPoint * s * s * s + controlPoint1 * 3.0f * s * s * t + controlPoint2 * 3.0f * s * t * t + exitPoint * t * t * t;
            var distance = DistanceToLineSegmentSquared(point, a, b, out var comparePoint, out var h);
            if (d0 > distance)
            {
                d0 = distance;
                outQ = comparePoint;
                outT = t + h / (float)(resolution - 1);
            }
            //d0 = Math.Min(d0, DistanceSquared(p, a, b));
            a = b;
        }
        if (!comparative)
        {
            d0 = MathF.Sqrt(d0);
        }
        return d0;
    }
}
