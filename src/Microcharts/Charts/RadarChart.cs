// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using Microsoft.Maui.Graphics;

namespace Microcharts
{
    /// <summary>
    /// ![chart](../images/Radar.png)
    ///
    /// A radar chart.
    /// </summary>
    public class RadarChart : SimpleChart
    {
        #region Constants

        private const float Epsilon = 0.01f;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the size of the line.
        /// </summary>
        /// <value>The size of the line.</value>
        public float LineSize { get; set; } = 3;

        /// <summary>
        /// Gets or sets the color of the border line.
        /// </summary>
        /// <value>The color of the border line.</value>
        public Color BorderLineColor { get; set; } = Colors.LightGray.WithAlpha(110 / 255f);

        /// <summary>
        /// Gets or sets the size of the border line.
        /// </summary>
        /// <value>The size of the border line.</value>
        public float BorderLineSize { get; set; } = 2;

        /// <summary>
        /// Gets or sets the point mode.
        /// </summary>
        /// <value>The point mode.</value>
        public PointMode PointMode { get; set; } = PointMode.Circle;

        /// <summary>
        /// Gets or sets the size of the points.
        /// </summary>
        /// <value>The size of the point.</value>
        public float PointSize { get; set; } = 14;

        private float AbsoluteMinimum => Entries.Where( x=>x.Value.HasValue).Select(x => x.Value.Value).Concat(new[] { MaxValue, MinValue, InternalMinValue ?? 0 }).Min(x => Math.Abs(x));

        private float AbsoluteMaximum => Entries.Where(x => x.Value.HasValue).Select(x => x.Value.Value).Concat(new[] { MaxValue, MinValue, InternalMinValue ?? 0 }).Max(x => Math.Abs(x));

        /// <inheritdoc />
        protected override float ValueRange => AbsoluteMaximum - AbsoluteMinimum;

        #endregion

        #region Methods

        public override void DrawContent(ICanvas canvas, RectF dirtyRect)
        {
            int width = (int)dirtyRect.Width;
            int height = (int)dirtyRect.Height;

            var total = Entries?.Count() ?? 0;

            if (total > 0)
            {
                var captionHeight = Entries.Max(x =>
                {
                    var result = 0.0f;

                    var hasLabel = !string.IsNullOrEmpty(x.Label);
                    var hasValueLabel = !string.IsNullOrEmpty(x.ValueLabel);

                    if (hasLabel || hasValueLabel)
                    {
                        var hasOffset = hasLabel && hasValueLabel;
                        var captionMargin = LabelTextSize * 0.60f;
                        var space = hasOffset ? captionMargin : 0;

                        if (hasLabel)
                        {
                            result += LabelTextSize;
                        }

                        if (hasValueLabel)
                        {
                            result += LabelTextSize;
                        }
                    }

                    return result;
                });

                var center = new PointF(width / 2, height / 2);
                var radius = ((Math.Min(width, height) - (2 * Margin)) / 2) - captionHeight;
                var rangeAngle = (float)((Math.PI * 2) / total);
                var startAngle = (float)Math.PI;

                DrawBorder(canvas, center, radius);

                var clip = new PathF();
                clip.AppendCircle(center, radius);

                for (int i = 0; i < total; i++)
                {
                    var angle = startAngle + (rangeAngle * i);
                    var entry = Entries.ElementAt(i);

                    int nextIndex = (i + 1) % total;
                    var nextAngle = startAngle + (rangeAngle * nextIndex);
                    var nextEntry = Entries.ElementAt(nextIndex);
                    while (!nextEntry.Value.HasValue)
                    {
                        nextIndex = (nextIndex + 1) % total;
                        nextAngle = startAngle + (rangeAngle * nextIndex);
                        nextEntry = Entries.ElementAt(nextIndex);
                    }

                    canvas.SaveState();
                    if (entry.Value.HasValue)
                    {
                        var point = GetPoint(entry.Value.Value * AnimationProgress, center, angle, radius);
                        var nextPoint = GetPoint(nextEntry.Value.Value * AnimationProgress, center, nextAngle, radius);

                        canvas.ClipPath(clip);

                        // Border center bars
                        canvas.StrokeColor = BorderLineColor;
                        canvas.StrokeSize = BorderLineSize;
                        canvas.Antialias = true;
                        var borderPoint = GetPoint(MaxValue, center, angle, radius);
                        canvas.DrawLine(point.X, point.Y, borderPoint.X, borderPoint.Y);

                        // Values points and lines
                        canvas.StrokeColor = entry.Color.MultiplyAlpha(0.75f * AnimationProgress);
                        canvas.StrokeSize = BorderLineSize;
                        canvas.StrokeDashPattern = new[] { BorderLineSize, BorderLineSize * 2 };
                        canvas.StrokeDashOffset = 0;
                        canvas.Antialias = true;
                        var amount = Math.Abs(entry.Value.Value - AbsoluteMinimum) / ValueRange;
                        canvas.DrawEllipse(center.X - (radius * amount), center.Y - (radius * amount), 2 * radius * amount, 2 * radius * amount);
                        canvas.StrokeDashPattern = null;

                        canvas.DrawGradientLine(center, entry.Color.WithAlpha(0), point, entry.Color.MultiplyAlpha(0.75f), LineSize);
                        canvas.DrawGradientLine(point, entry.Color, nextPoint, nextEntry.Color, LineSize);
                        canvas.DrawPoint(point, entry.Color, PointSize, PointMode);
                    }
                    canvas.RestoreState();

                    // Labels
                    var labelPoint = RotatePoint(new PointF(0, radius + LabelTextSize + (PointSize / 2)), angle);
                    labelPoint = new PointF(center.X + labelPoint.X, center.Y + labelPoint.Y);
                    var alignment = HorizontalAlignment.Left;

                    if ((Math.Abs(angle - (startAngle + Math.PI)) < Epsilon) || (Math.Abs(angle - Math.PI) < Epsilon))
                    {
                        alignment = HorizontalAlignment.Center;
                    }
                    else if (angle > (float)(startAngle + Math.PI))
                    {
                        alignment = HorizontalAlignment.Right;
                    }

                    canvas.DrawCaptionLabels(TextMetricsProvider, entry.Label, entry.TextColor, entry.ValueLabel, entry.Color.WithAlpha(AnimationProgress), LabelTextSize, labelPoint, alignment, Typeface, out var _);
                }
            }
        }

        /// <summary>
        /// Finds point coordinates of an entry.
        /// </summary>
        /// <returns>The point.</returns>
        /// <param name="value">The value.</param>
        /// <param name="center">The center.</param>
        /// <param name="angle">The entry angle.</param>
        /// <param name="radius">The radius.</param>
        private PointF GetPoint(float value, PointF center, float angle, float radius)
        {
            var amount = Math.Abs(value - AbsoluteMinimum) / ValueRange;
            var point = RotatePoint(new PointF(0, radius * amount), angle);
            return new PointF(center.X + point.X, center.Y + point.Y);
        }

        /// <summary>
        /// Rotates <paramref name="point"/> around the origin by <paramref name="angleRadians"/>, matching
        /// SkiaSharp's <c>SKMatrix.CreateRotation(angle).MapPoint(point)</c> (the standard rotation matrix --
        /// unlike arc sweep angles, this has one unambiguous convention, so no equivalent of
        /// <see cref="RadialHelpers"/>'s arc-tessellation workaround is needed here).
        /// </summary>
        private static PointF RotatePoint(PointF point, float angleRadians)
        {
            var cos = (float)Math.Cos(angleRadians);
            var sin = (float)Math.Sin(angleRadians);
            return new PointF((point.X * cos) - (point.Y * sin), (point.X * sin) + (point.Y * cos));
        }

        private void DrawBorder(ICanvas canvas, PointF center, float radius)
        {
            canvas.SaveState();
            canvas.StrokeColor = BorderLineColor;
            canvas.StrokeSize = BorderLineSize;
            canvas.Antialias = true;
            canvas.DrawEllipse(center.X - radius, center.Y - radius, 2 * radius, 2 * radius);
            canvas.RestoreState();
        }

        #endregion
    }
}
