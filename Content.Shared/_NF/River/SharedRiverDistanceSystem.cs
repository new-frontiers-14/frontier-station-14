// The MIT License
// Copyright © 2018 Inigo Quilez
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// (Modified by Nawichusea(So any jank with it? Blame me) Separated from rest of the code for licensing reasons.)

using System.Numerics;

namespace Content.Shared._NF.River;

public abstract partial class SharedRiverNodeSystem
{
    private static float Cro(Vector2 a, Vector2 b) { return a.X * b.Y - a.Y * b.X; }

    // This method provides just an approximation, and is only usable in
    // the very close neighborhood of the curve. Taken and adapted from
    // http://research.microsoft.com/en-us/um/people/hoppe/ravg.pdf
    /// <summary>
    /// Approximately calculates the distance from the nearest point on a Bezier curve and identifies that point.
    /// </summary>
    /// <param name="p">The point between which and the curve you wish to calculate the distance.</param>
    /// <param name="v0">The starting point of the curve.</param>
    /// <param name="v1">The control point of the curve.</param>
    /// <param name="v2">The end point of the curve.</param>
    /// <param name="outQ">The location on the curve from which the distance is measured.</param>
    /// <returns></returns>
    public static float distanceToSegment(Vector2 p, Vector2 v0, Vector2 v1, Vector2 v2, out Vector2 outQ)
    {
        float distance;
        // Tests to see if it is too straight for the bezier code.
        var testVec1 = v0 - v2;
        var testVec2 = v1 - v2;
        var testAngle1 = testVec1.ToAngle();
        var testAngle2 = testVec2.ToAngle();

        if (!testAngle1.EqualsApprox(testAngle2)) // Not too straight, can do bezier code.
        {
            // Decided to be explicit about the types for the sake of readability.
            Vector2 i = v0 - v2;
            Vector2 j = v2 - v1;
            Vector2 k = v1 - v0;
            Vector2 w = j - k;

            v0 -= p; v1 -= p; v2 -= p;

            float x = Cro(v0, v2);
            float y = Cro(v1, v0);
            float z = Cro(v2, v1);

            Vector2 s = 2.0f * (y * j + z * k) - x * i;

            float r = (y * z - x * x * 0.25f) / Vector2.Dot(s, s);
            float t = Math.Clamp((0.5f * x + y + r * Vector2.Dot(s, w)) / (x + y + z), 0.0f, 1.0f);

            Vector2 d = v0 + t * (k + k + t * w);
            outQ = d + p;
            distance = d.Length();
        }
        else // Too straight, calculate from a straight line between v0 and v2 instead.
        {
            var perpendicular = new Vector2(v2.Y - v0.Y, -(v2.X - v0.X));
            perpendicular = Vector2.Normalize(perpendicular);
            var v0ToPDiff = v0 - p;


            var v2ToOrigin = v2 - v0;
            var pToOrigin = p - v0;

            var pProjection = Vector2.Dot(pToOrigin, Vector2.Normalize(v2ToOrigin));
            if (pProjection < 0) // If the closest point is v0, ensure the closets point is v0
            {
                distance = pToOrigin.Length();
                outQ = v0;
            }
            else if (pProjection > v2ToOrigin.Length()) // If the closest point is v2, ensure the closest point is v2
            {
                var pToV2 = v2 - p;
                distance = pToV2.Length();
                outQ = v2;
            }
            else // If the closest point is on the segment, return the distance and where on the segment.
            {
                distance = Math.Abs(Vector2.Dot(perpendicular, v0ToPDiff));
                outQ = v0 + Vector2.Normalize(v2ToOrigin) * pProjection;
            }
        }
        return distance;
    }
}
