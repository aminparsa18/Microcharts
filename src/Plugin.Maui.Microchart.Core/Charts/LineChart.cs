using System;
using System.Collections.Generic;
using System.Linq;
using Plugin.Maui.Microchart.Abstracts;
using Microsoft.Maui.Graphics;

namespace Plugin.Maui.Microchart
{
    /// <summary>
    /// ![chart](../images/LineSeries.png)
    ///
    /// A grouped bar chart.
    /// </summary>
    public class LineChart : PointChart
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Plugin.Maui.Microchart.LineSeriesChart"/> class.
        /// </summary>
        public LineChart() : base()
        {
        }

        #endregion

        #region Properties

        private Dictionary<ChartSerie, List<PointF>> pointsPerSerie = new Dictionary<ChartSerie, List<PointF>>();

        /// <summary>
        /// Gets or sets the size of the line.
        /// </summary>
        /// <value>The size of the line.</value>
        public float LineSize { get; set; } = 3;

        /// <summary>
        /// Gets or sets the line mode.
        /// </summary>
        /// <value>The line mode.</value>
        public LineMode LineMode { get; set; } = LineMode.Spline;

        /// <summary>
        /// Gets or sets the alpha of the line area.
        /// </summary>
        /// <value>The line area alpha.</value>
        public byte LineAreaAlpha { get; set; } = 32;

        /// <summary>
        /// Enables or disables a fade out gradient for the line area in the Y direction
        /// </summary>
        /// <value>The state of the fadeout gradient.</value>
        /// <remarks>
        /// Microsoft.Maui.Graphics has no shader-composition primitive equivalent to SkiaSharp's
        /// <c>SKShader.CreateCompose(..., SKBlendMode.SrcOut)</c>, which the original implementation used to
        /// combine the X-direction hue gradient with a Y-direction alpha fade. When this is enabled, the area
        /// fill ships a documented simplification: a single-axis vertical alpha gradient over one flat color
        /// (the serie's color, or its first entry's), rather than the true 2D X-hue &#215; Y-alpha blend. This is
        /// the one intentional behavior change called out for the 3.0 release notes.
        /// </remarks>
        public bool EnableYFadeOutGradient { get; set; } = false;

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override float CalculateHeaderHeight(Dictionary<ChartEntry, RectF> valueLabelSizes)
        {
            if(ValueLabelOption == ValueLabelOption.None || ValueLabelOption == ValueLabelOption.OverElement)
                return Margin;

            return base.CalculateHeaderHeight(valueLabelSizes);
        }

        /// <inheritdoc/>
        public override void DrawContent(ICanvas canvas, RectF dirtyRect)
        {
            pointsPerSerie.Clear();
            foreach (var s in Series)
                pointsPerSerie.Add(s, new List<PointF>());

            base.DrawContent(canvas, dirtyRect);
        }

        protected override void DrawNullPoint(ChartSerie serie, ICanvas canvas)
        {
            //Some of the drawing algorithms index into pointsPerSerie
            var point = new PointF(float.MinValue, float.MinValue);
            pointsPerSerie[serie].Add(point);
        }

        /// <inheritdoc/>
        protected override void OnDrawContentEnd(ICanvas canvas, SizeF itemSize, float origin, Dictionary<ChartEntry, RectF> valueLabelSizes)
        {
            base.OnDrawContentEnd(canvas, itemSize, origin, valueLabelSizes);

            foreach (var pps in pointsPerSerie)
            {
                DrawLineArea(canvas, pps.Key, pps.Value.ToArray(), itemSize, origin);
            }

            DrawSeriesLine(canvas, itemSize);
            DrawPoints(canvas);
            DrawValueLabels(canvas, itemSize, valueLabelSizes);
        }

        private void DrawPoints(ICanvas canvas)
        {
            if (PointMode != PointMode.None)
            {
                foreach (var pps in pointsPerSerie)
                {
                    var entries = pps.Key.Entries.ToArray();
                    for (int i = 0; i < pps.Value.Count; i++)
                    {
                        var entry = entries[i];
                        if (!entry.Value.HasValue)
                        {
                            continue;
                        }

                        var point = pps.Value.ElementAt(i);
                        canvas.DrawPoint(point, pps.Key.Color ?? entry.Color, PointSize, PointMode);
                    }
                }
            }
        }

        private void DrawValueLabels(ICanvas canvas, SizeF itemSize, Dictionary<ChartEntry, RectF> valueLabelSizes)
        {
            ValueLabelOption valueLabelOption = ValueLabelOption;
            if (ValueLabelOption == ValueLabelOption.TopOfChart && Series.Count() > 1)
                valueLabelOption = ValueLabelOption.TopOfElement;

            if (valueLabelOption == ValueLabelOption.TopOfElement || valueLabelOption == ValueLabelOption.OverElement)
            {
                foreach (var pps in pointsPerSerie)
                {
                    var entries = pps.Key.Entries.ToArray();
                    for (int i = 0; i < pps.Value.Count; i++)
                    {
                        var entry = entries[i];
                        string label = entry.ValueLabel;
                        if (!string.IsNullOrEmpty(label))
                        {
                          var drawedPoint = pps.Value.ElementAt(i);
                          PointF point;
                          YPositionBehavior yPositionBehavior = YPositionBehavior.None;

                            if (!valueLabelSizes.ContainsKey(entry))
                            {
                                continue;
                            }

                          var valueLabelSize = valueLabelSizes[entry];
                          if (valueLabelOption == ValueLabelOption.TopOfElement)
                          {
                              point = new PointF(drawedPoint.X, drawedPoint.Y - (PointSize / 2) - (Margin / 2));
                              if (ValueLabelOrientation == Orientation.Vertical)
                                  yPositionBehavior = YPositionBehavior.UpToElementHeight;
                          }
                          else
                          {
                              if (ValueLabelOrientation == Orientation.Vertical)
                                  yPositionBehavior = YPositionBehavior.UpToElementMiddle;
                              else
                                  yPositionBehavior = YPositionBehavior.DownToElementMiddle;

                              point = new PointF(drawedPoint.X, drawedPoint.Y);

                          }

                          DrawHelper.DrawLabel(canvas, TextMetricsProvider, ValueLabelOrientation, yPositionBehavior, itemSize, point, entry.ValueLabelColor.MultiplyAlpha(AnimationProgress), valueLabelSize, label, ValueLabelTextSize, Typeface);
                        } else
                        {
                            continue;
                        }
                    }
                }
            }
        }

        private void DrawSeriesLine(ICanvas canvas, SizeF itemSize)
        {
            if (pointsPerSerie.Any() && pointsPerSerie.Values.First().Count > 1 && LineMode != LineMode.None)
            {
                foreach (var s in Series)
                {
                    var points = pointsPerSerie[s].ToArray();
                    var entries = s.Entries.ToArray();
                    var lineMode = LineMode;
                    var last = (lineMode == LineMode.Spline) ? points.Length - 1 : points.Length;

                    canvas.SaveState();
                    canvas.Antialias = true;
                    canvas.StrokeSize = LineSize;

                    if (s.Color != null)
                    {
                        // Solid stroke: a single color, no gradient needed -- direct 1:1 port.
                        canvas.StrokeColor = s.Color;
                        var path = BuildLinePath(points, entries, lineMode, last, itemSize);
                        if (path != null)
                            canvas.DrawPath(path);
                    }
                    else
                    {
                        // ICanvas has no gradient-stroke primitive (see CanvasExtensions.DrawGradientLine's remarks) --
                        // unlike SkiaSharp, a Shader can't be assigned to a stroke Paint here. The per-entry X-color
                        // gradient is reconstructed by stroking short two-color gradient segments instead of one
                        // shader spanning the whole path. Straight segments are drawn exactly (they *are* the real
                        // path segments); each spline (cubic) segment is sampled into short straight sub-segments --
                        // the gradient is a pure function of X, not of the path's shape, so this only samples color
                        // fidelity, not curve fidelity, and is visually equivalent to the original at typical LineSize.
                        DrawGradientSeriesLine(canvas, points, entries, last, lineMode, itemSize);
                    }

                    canvas.RestoreState();
                }
            }
        }

        private PathF BuildLinePath(PointF[] points, ChartEntry[] entries, LineMode lineMode, int last, SizeF itemSize)
        {
            var path = new PathF();
            var isFirst = true;

            for (int i = 0; i < last; i++)
            {
                if (!entries[i].Value.HasValue) continue;
                if (isFirst)
                {
                    path.MoveTo(points[i]);
                    isFirst = false;
                }

                if (lineMode == LineMode.Spline)
                {
                    int next = i + 1;
                    while (next < last && !entries[next].Value.HasValue)
                    {
                        next++;
                    }

                    if (next == last && !entries[next].Value.HasValue)
                    {
                        break;
                    }

                    var cubicInfo = CalculateCubicInfo(points, i, next, itemSize);
                    path.CurveTo(cubicInfo.control, cubicInfo.nextControl, cubicInfo.nextPoint);
                }
                else if (lineMode == LineMode.Straight)
                {
                    path.LineTo(points[i]);
                }
            }

            return isFirst ? null : path;
        }

        private const int SplineGradientSamplesPerSegment = 16;

        private void DrawGradientSeriesLine(ICanvas canvas, PointF[] points, ChartEntry[] entries, int last, LineMode lineMode, SizeF itemSize)
        {
            var isFirst = true;
            PointF previousPoint = default;

            for (int i = 0; i < last; i++)
            {
                if (!entries[i].Value.HasValue) continue;

                if (isFirst)
                {
                    previousPoint = points[i];
                    isFirst = false;
                }

                if (lineMode == LineMode.Spline)
                {
                    int next = i + 1;
                    while (next < last && !entries[next].Value.HasValue)
                    {
                        next++;
                    }

                    if (next == last && !entries[next].Value.HasValue)
                    {
                        break;
                    }

                    var cubicInfo = CalculateCubicInfo(points, i, next, itemSize);
                    var segmentStart = points[i];
                    for (int step = 1; step <= SplineGradientSamplesPerSegment; step++)
                    {
                        var t = (float)step / SplineGradientSamplesPerSegment;
                        var sample = EvaluateCubic(segmentStart, cubicInfo.control, cubicInfo.nextControl, cubicInfo.nextPoint, t);
                        canvas.DrawGradientLine(previousPoint, ColorAt(previousPoint.X, points, entries), sample, ColorAt(sample.X, points, entries), LineSize);
                        previousPoint = sample;
                    }
                }
                else if (lineMode == LineMode.Straight)
                {
                    canvas.DrawGradientLine(previousPoint, ColorAt(previousPoint.X, points, entries), points[i], ColorAt(points[i].X, points, entries), LineSize);
                    previousPoint = points[i];
                }
            }
        }

        private void DrawLineArea(ICanvas canvas, ChartSerie serie, PointF[] points, SizeF itemSize, float origin)
        {
            if (LineAreaAlpha > 0 && points.Length > 1)
            {
                var entries = serie.Entries.ToArray();
                var lineMode = LineMode;
                var last = (lineMode == LineMode.Spline) ? points.Length - 1 : points.Length;

                var path = new PathF();
                var isFirst = true;
                PointF lastPoint = points.First();
                for (int i = 0; i < last; i++)
                {
                    if (!entries[i].Value.HasValue) continue;

                    if (isFirst)
                    {
                        path.MoveTo(points[i].X, origin);
                        path.LineTo(points[i]);
                        isFirst = false;
                    }

                    if (lineMode == LineMode.Spline)
                    {
                        int next = i + 1;
                        while (next < last && !entries[next].Value.HasValue)
                        {
                            next++;
                        }

                        if (next == last && !entries[next].Value.HasValue)
                        {
                            lastPoint = points[i];
                            break;
                        }

                        var cubicInfo = CalculateCubicInfo(points, i, next, itemSize);
                        path.CurveTo(cubicInfo.control, cubicInfo.nextControl, cubicInfo.nextPoint);
                        lastPoint = cubicInfo.nextPoint;
                    }
                    else if (lineMode == LineMode.Straight)
                    {
                        path.LineTo(points[i]);
                        lastPoint = points[i];
                    }
                }

                // Every entry was null, so no point was ever added to the path and
                // lastPoint still holds the placeholder used for null points.
                if (isFirst)
                {
                    return;
                }

                path.LineTo(lastPoint.X, origin);
                path.Close();

                var alpha = (LineAreaAlpha / 255f) * AnimationProgress;

                canvas.SaveState();
                canvas.Antialias = true;

                LinearGradientPaint gradient;
                if (EnableYFadeOutGradient)
                {
                    // Documented simplification (see EnableYFadeOutGradient remarks): a flat base color with a
                    // vertical alpha fade, instead of the true X-hue x Y-alpha blend SkiaSharp produced via shader
                    // composition.
                    var baseColor = serie.Color ?? entries.FirstOrDefault()?.Color ?? Colors.White;
                    gradient = CreateYFadeGradient(points, baseColor, alpha);
                }
                else
                {
                    gradient = CreateXGradient(points, entries, serie.Color, alpha);
                }

                canvas.SetFillPaint(gradient, path.Bounds);
                canvas.FillPath(path, WindingMode.NonZero);
                canvas.RestoreState();
            }
        }

        /// <inheritdoc/>
        protected override void DrawValueLabel(ICanvas canvas, Dictionary<ChartEntry, RectF> valueLabelSizes, float headerWithLegendHeight, SizeF itemSize, SizeF barSize, ChartEntry entry, float barX, float barY, float itemX, float origin)
        {
            if(Series.Count() == 1 && ValueLabelOption == ValueLabelOption.TopOfChart)
                base.DrawValueLabel(canvas, valueLabelSizes, headerWithLegendHeight, itemSize, barSize, entry, barX, barY, itemX, origin);
        }

        /// <inheritdoc/>
        protected override void DrawBar(ChartSerie serie, ICanvas canvas, float headerHeight, float itemX, SizeF itemSize, SizeF barSize, float origin, float barX, float barY, Color color)
        {
            //Drawing entry point at center of the item (label) part
            var point = new PointF(itemX, barY);
            pointsPerSerie[serie].Add(point);
        }

        /// <inheritdoc/>
        protected override void DrawBarArea(ICanvas canvas, float headerHeight, SizeF itemSize, SizeF barSize, Color color, Color otherColor, float origin, float value, float barX, float barY)
        {
            //Area is draw on the OnDrawContentEnd
        }

        private (PointF control, PointF nextPoint, PointF nextControl) CalculateCubicInfo(PointF[] points, int i, int next, SizeF itemSize)
        {
            var point = points[i];
            var nextPoint = points[next];
            var controlOffset = new SizeF(itemSize.Width * 0.8f, 0);
            var currentControl = point + controlOffset;
            var nextControl = nextPoint - controlOffset;
            return (currentControl, nextPoint, nextControl);
        }

        private static PointF EvaluateCubic(PointF p0, PointF c1, PointF c2, PointF p3, float t)
        {
            var mt = 1 - t;
            var a = mt * mt * mt;
            var b = 3 * mt * mt * t;
            var c = 3 * mt * t * t;
            var d = t * t * t;
            return new PointF(
                (a * p0.X) + (b * c1.X) + (c * c2.X) + (d * p3.X),
                (a * p0.Y) + (b * c1.Y) + (c * c2.Y) + (d * p3.Y));
        }

        /// <summary>
        /// The color of the auto-generated (null serie color) X-gradient at canvas x-position <paramref name="x"/>,
        /// used to color the short straight sub-segments <see cref="DrawGradientSeriesLine"/> strokes.
        /// </summary>
        private static Color ColorAt(float x, PointF[] points, ChartEntry[] entries)
        {
            var startX = points.First().X;
            var endX = points.Last().X;

            if (entries.Length == 1 || endX <= startX)
                return entries[0].Color;

            var t = Math.Clamp((x - startX) / (endX - startX), 0f, 1f) * (entries.Length - 1);
            var i0 = (int)MathF.Floor(t);
            var i1 = Math.Min(i0 + 1, entries.Length - 1);
            var frac = t - i0;
            return LerpColor(entries[i0].Color, entries[i1].Color, frac);
        }

        private static Color LerpColor(Color a, Color b, float t) => new Color(
            a.Red + ((b.Red - a.Red) * t),
            a.Green + ((b.Green - a.Green) * t),
            a.Blue + ((b.Blue - a.Blue) * t),
            a.Alpha + ((b.Alpha - a.Alpha) * t));

        private LinearGradientPaint CreateXGradient(PointF[] points, ChartEntry[] entries, Color serieColor, float alpha)
        {
            var startX = points.First().X;
            var endX = points.Last().X;
            var count = entries.Length;
            var stops = new PaintGradientStop[count];
            for (int i = 0; i < count; i++)
            {
                // Matches SkiaSharp's SKShader.CreateLinearGradient(..., positions: null) behavior: with no
                // explicit stop positions, colors are distributed evenly across [0, 1], not by each point's
                // actual X (the two coincide here anyway, since entries are laid out at even X intervals).
                var offset = count > 1 ? (float)i / (count - 1) : 0f;
                var color = (serieColor ?? entries[i].Color).WithAlpha(alpha);
                stops[i] = new PaintGradientStop(offset, color);
            }

            return new LinearGradientPaint(stops, new PointF(startX, 0), new PointF(endX, 0));
        }

        private LinearGradientPaint CreateYFadeGradient(PointF[] points, Color baseColor, float alpha)
        {
            var startY = points.Max(p => p.Y);
            var endY = 0f;

            return new LinearGradientPaint(new PointF(0, startY), new PointF(0, endY))
            {
                StartColor = baseColor.WithAlpha(0),
                EndColor = baseColor.WithAlpha(alpha),
            };
        }

        #endregion
    }
}
