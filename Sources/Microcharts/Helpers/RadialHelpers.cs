// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microcharts
{
    using System;
    using Microsoft.Maui.Graphics;

    internal static class RadialHelpers
    {
        #region Constants

        public const float PI = (float)Math.PI;

        private const float UprightAngle = PI / 2f;

        private const float TotalAngle = 2f * PI;

        /// <summary>
        /// Angle step (radians) used to tessellate arcs into straight <see cref="PathF.LineTo"/> segments.
        /// <see cref="ICanvas"/>/<see cref="PathF"/> has no faithful equivalent of SkiaSharp's
        /// endpoint-style <c>SKPath.ArcTo</c>/<c>AddArc</c> angle convention (verified empirically against the
        /// pinned Microsoft.Maui.Graphics 10.0.20 assembly -- its <c>PathF.AddArc(x1,y1,x2,y2,startAngle,endAngle,clockwise)</c>
        /// bounding-box overload does not map angles the same way SkiaSharp's rect+start+sweep overload does), so
        /// arcs are sampled by hand with the same trig SkiaSharp itself uses under the hood (0 degrees = positive
        /// X axis / 3 o'clock, increasing angle sweeps visually clockwise since canvas Y grows downward). This
        /// mirrors the sub-segment sampling <c>CanvasExtensions.DrawGradientLine</c> already uses for spline
        /// gradients elsewhere in this migration.
        /// </summary>
        private const float ArcStep = PI / 60f; // 3 degrees per segment

        #endregion

        #region Sectors

        public static PointF GetCirclePoint(float r, float angle)
        {
            return new PointF(r * (float)Math.Cos(angle), r * (float)Math.Sin(angle));
        }

        /// <summary>
        /// Builds a closed pie/donut sector path, centered on the origin, spanning <paramref name="start"/> to
        /// <paramref name="end"/> (each a fraction of the full circle, i.e. in [0, 1]).
        /// </summary>
        /// <remarks>
        /// Fill with <see cref="WindingMode.EvenOdd"/> -- it fills a single simple sector identically to
        /// <see cref="WindingMode.NonZero"/>, and correctly punches the hole for the full-circle special case
        /// (two overlapping circles), so callers don't need to branch on which case they're in.
        /// </remarks>
        public static PathF CreateSectorPath(float start, float end, float outerRadius, float innerRadius = 0.0f, float margin = 0.0f)
        {
            var path = new PathF();

            // if the sector has no size, then it has no path
            if (start == end)
            {
                return path;
            }

            // if the sector is a full circle, then do that
            if (end - start == 1.0f)
            {
                path.AppendCircle(PointF.Zero, outerRadius);
                if (innerRadius > 0)
                {
                    path.AppendCircle(PointF.Zero, innerRadius);
                }

                return path;
            }

            // calculate the angles
            var startAngle = (TotalAngle * start) - UprightAngle;
            var endAngle = (TotalAngle * end) - UprightAngle;

            // calculate the angle for the margins
            var offsetR = outerRadius == 0 ? 0 : ((margin / (TotalAngle * outerRadius)) * TotalAngle);
            var offsetr = innerRadius == 0 ? 0 : ((margin / (TotalAngle * innerRadius)) * TotalAngle);

            var outerStart = startAngle + offsetR;
            var outerEnd = endAngle - offsetR;
            var innerEnd = endAngle - offsetr;
            var innerStart = startAngle + offsetr;

            // add the points to the path
            path.MoveTo(GetCirclePoint(outerRadius, outerStart));
            AppendArc(path, outerRadius, outerStart, outerEnd);
            path.LineTo(GetCirclePoint(innerRadius, innerEnd));

            if (innerRadius == 0.0f)
            {
                // take a short cut -- already at the center via the line above
            }
            else
            {
                AppendArc(path, innerRadius, innerEnd, innerStart);
            }

            path.Close();

            return path;
        }

        /// <summary>
        /// Builds an open arc path (for stroking), matching SkiaSharp's <c>SKPath.AddArc(rect, startAngle, sweepAngle)</c>
        /// convention: <paramref name="startAngleDegrees"/> is measured from the positive X axis (3 o'clock),
        /// and positive <paramref name="sweepAngleDegrees"/> sweeps visually clockwise.
        /// </summary>
        public static PathF CreateArcPath(float cx, float cy, float radius, float startAngleDegrees, float sweepAngleDegrees)
        {
            var path = new PathF();
            var startRad = startAngleDegrees * PI / 180f;
            var sweepRad = sweepAngleDegrees * PI / 180f;
            var segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(sweepRad) / ArcStep));

            for (int i = 0; i <= segments; i++)
            {
                var t = startRad + (sweepRad * i / segments);
                var p = new PointF(cx + (radius * (float)Math.Cos(t)), cy + (radius * (float)Math.Sin(t)));

                if (i == 0)
                {
                    path.MoveTo(p);
                }
                else
                {
                    path.LineTo(p);
                }
            }

            return path;
        }

        /// <summary>
        /// Appends straight sub-segments approximating the arc from <paramref name="fromAngle"/> to
        /// <paramref name="toAngle"/> (radians, same convention as <see cref="GetCirclePoint"/>) onto an
        /// already-started <paramref name="path"/> (the path's current point must already be the arc's start).
        /// </summary>
        private static void AppendArc(PathF path, float radius, float fromAngle, float toAngle)
        {
            var span = toAngle - fromAngle;
            var segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(span) / ArcStep));

            for (int i = 1; i <= segments; i++)
            {
                var t = fromAngle + (span * i / segments);
                path.LineTo(GetCirclePoint(radius, t));
            }
        }

        #endregion
    }
}
