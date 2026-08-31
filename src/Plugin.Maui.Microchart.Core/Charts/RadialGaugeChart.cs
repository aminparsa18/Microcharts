// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using Microsoft.Maui.Graphics;

namespace Plugin.Maui.Microchart
{
    /// <summary>
    /// ![chart](../images/RadialGauge.png)
    ///
    /// Radial gauge chart.
    /// </summary>
    public class RadialGaugeChart : SimpleChart
    {
        #region Properties

        /// <summary>
        /// Gets or sets the size of each gauge. If negative, then its will be calculated from the available space.
        /// </summary>
        /// <value>The size of the line.</value>
        public float LineSize { get; set; } = -1;

        /// <summary>
        /// Gets or sets the gauge background area alpha.
        /// </summary>
        /// <value>The line area alpha.</value>
        public byte LineAreaAlpha { get; set; } = 52;

        /// <summary>
        /// Gets or sets the start angle.
        /// </summary>
        /// <value>The start angle.</value>
        public float StartAngle { get; set; } = -90;

        private float AbsoluteMinimum => Entries?.Where(x=>x.Value.HasValue).Select(x => x.Value.Value).Concat(new[] { MaxValue, MinValue, InternalMinValue ?? 0 }).Min(x => Math.Abs(x)) ?? 0;

        private float AbsoluteMaximum => Entries?.Where(x => x.Value.HasValue).Select(x => x.Value.Value).Concat(new[] { MaxValue, MinValue, InternalMinValue ?? 0 }).Max(x => Math.Abs(x)) ?? 0;

        /// <inheritdoc />
        protected override float ValueRange => AbsoluteMaximum - AbsoluteMinimum;

        #endregion

        #region Methods

        public void DrawGaugeArea(ICanvas canvas, ChartEntry entry, float radius, int cx, int cy, float strokeWidth)
        {
            canvas.SaveState();
            canvas.StrokeColor = entry.Color.WithAlpha(LineAreaAlpha / 255f);
            canvas.StrokeSize = strokeWidth;
            canvas.Antialias = true;
            canvas.DrawEllipse(cx - radius, cy - radius, 2 * radius, 2 * radius);
            canvas.RestoreState();
        }

        public void DrawGauge(ICanvas canvas, Color color, float value, float radius, int cx, int cy, float strokeWidth)
        {
            canvas.SaveState();
            canvas.StrokeColor = color;
            canvas.StrokeSize = strokeWidth;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.Antialias = true;

            var sweepAngle = AnimationProgress * 360 * (Math.Abs(value) - AbsoluteMinimum) / ValueRange;
            var path = RadialHelpers.CreateArcPath(cx, cy, radius, StartAngle, sweepAngle);
            canvas.DrawPath(path);

            canvas.RestoreState();
        }

        public override void DrawContent(ICanvas canvas, RectF dirtyRect)
        {
            int width = (int)dirtyRect.Width;
            int height = (int)dirtyRect.Height;

            if (Entries != null)
            {
                var sumValue = Entries.Where( x=>x.Value.HasValue).Sum(x => Math.Abs(x.Value.Value));
                var radius = (Math.Min(width, height) - (2 * Margin)) / 2;
                var cx = width / 2;
                var cy = height / 2;
                var lineWidth = (LineSize < 0) ? (radius / ((Entries.Count() + 1) * 2)) : LineSize;
                // Space the rings by the available radius rather than the stroke width, so a custom
                // LineSize changes only the line thickness, not the overall gauge size (issue #138).
                // In the auto case (LineSize < 0) this equals lineWidth * 2, so behaviour is unchanged.
                var radiusSpace = radius / (Entries.Count() + 1);

                for (int i = 0; i < Entries.Count(); i++)
                {
                    var entry = Entries.ElementAt(i);

                    //Skip the ring if it has a null value
                    if (!entry.Value.HasValue) continue;

                    var entryRadius = (i + 1) * radiusSpace;
                    DrawGaugeArea(canvas, entry, entryRadius, cx, cy, lineWidth);
                    DrawGauge(canvas, entry.Color, entry.Value.Value, entryRadius, cx, cy, lineWidth);
                }

                //Make sure captions draw on top of chart
                DrawCaption(canvas, width, height);
            }
        }

        private void DrawCaption(ICanvas canvas, int width, int height)
        {
            var rightValues = Entries.Take(Entries.Count() / 2).ToList();
            var leftValues = Entries.Skip(rightValues.Count()).ToList();

            leftValues.Reverse();

            DrawCaptionElements(canvas, width, height, rightValues, false, false);
            DrawCaptionElements(canvas, width, height, leftValues, true, false);
        }

        #endregion
    }
}
