using System;
using System.Collections.Generic;
using Plugin.Maui.Microchart.Abstracts;
using Microsoft.Maui.Graphics;

namespace Plugin.Maui.Microchart
{
    /// <summary>
    /// ![chart](../images/BarSeries.png)
    ///
    /// A grouped bar chart.
    /// </summary>
    public class PointChart : AxisBasedChart
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Plugin.Maui.Microchart.PointSeriesChart"/> class.
        /// </summary>
        public PointChart() : base()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the size of the point.
        /// </summary>
        /// <value>The size of the point.</value>
        public float PointSize { get; set; } = 14;

        /// <summary>
        /// Gets or sets the point mode.
        /// </summary>
        /// <value>The point mode.</value>
        public PointMode PointMode { get; set; } = PointMode.Circle;

        /// <summary>
        /// Gets or sets the point area alpha.
        /// </summary>
        /// <value>The point area alpha.</value>
        public byte PointAreaAlpha { get; set; } = 100;

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override void DrawValueLabel(ICanvas canvas, Dictionary<ChartEntry, RectF> valueLabelSizes, float headerWithLegendHeight, SizeF itemSize, SizeF barSize, ChartEntry entry, float barX, float barY, float itemX, float origin)
        {
            string label = entry?.ValueLabel;
            if (string.IsNullOrEmpty(label))
                return;

            var drawedPoint = new PointF(barX - (itemSize.Width / 2) + (barSize.Width / 2), barY);
            if (ValueLabelOption == ValueLabelOption.TopOfChart)
                base.DrawValueLabel(canvas, valueLabelSizes, headerWithLegendHeight, itemSize, barSize, entry, barX, barY, itemX, origin);
            else if (ValueLabelOption == ValueLabelOption.TopOfElement)
                DrawHelper.DrawLabel(canvas, TextMetricsProvider, ValueLabelOrientation, ValueLabelOrientation == Orientation.Vertical ? YPositionBehavior.UpToElementHeight : YPositionBehavior.None, barSize, new PointF(drawedPoint.X, drawedPoint.Y - (PointSize / 2) - (Margin / 2)), entry.ValueLabelColor.MultiplyAlpha(AnimationProgress), valueLabelSizes[entry], label, ValueLabelTextSize, Typeface);
            else if (ValueLabelOption == ValueLabelOption.OverElement)
                DrawHelper.DrawLabel(canvas, TextMetricsProvider, ValueLabelOrientation, ValueLabelOrientation == Orientation.Vertical ? YPositionBehavior.UpToElementMiddle : YPositionBehavior.DownToElementMiddle, barSize, new PointF(drawedPoint.X, drawedPoint.Y), entry.ValueLabelColor.MultiplyAlpha(AnimationProgress), valueLabelSizes[entry], label, ValueLabelTextSize, Typeface);
        }

        /// <inheritdoc />
        protected override void DrawBar(ChartSerie serie, ICanvas canvas, float headerHeight, float itemX, SizeF itemSize, SizeF barSize, float origin, float barX, float barY, Color color)
        {
            if (PointMode != PointMode.None)
            {
                var point = new PointF(barX - (itemSize.Width / 2) + (barSize.Width / 2), barY);
                canvas.DrawPoint(point, color, PointSize, PointMode);
            }
        }

        /// <inheritdoc />
        protected override void DrawBarArea(ICanvas canvas, float headerHeight, SizeF itemSize, SizeF barSize, Color color, Color otherColor, float origin, float value, float barX, float barY)
        {
            Color fillColor = null;
            Color startColor = null, endColor = null;
            if (otherColor != null)
            {
                fillColor = otherColor;
                startColor = otherColor;
                // Matches the original literal `(byte)(100 / 3)` -- a hardcoded alpha, not derived from PointAreaAlpha.
                endColor = otherColor.WithAlpha((100 / 3) / 255f);
            }
            else if (PointAreaAlpha > 0)
            {
                fillColor = color.WithAlpha(PointAreaAlpha / 255f);
                startColor = fillColor;
                endColor = color.WithAlpha((PointAreaAlpha / 3) / 255f);
            }

            if (fillColor != null)
            {
                var y = Math.Min(origin, barY);
                var height = Math.Max(2, Math.Abs(origin - barY));
                var rect = new RectF(barX - (itemSize.Width / 2) + (barSize.Width / 2) - (PointSize / 2), y, PointSize, height);

                // ICanvas has no gradient-stroke primitive, but this is a fill, so the two-stop vertical
                // gradient ports directly onto SetFillPaint + FillRectangle.
                var gradient = new LinearGradientPaint(new PointF(0, origin), new PointF(0, barY))
                {
                    StartColor = startColor,
                    EndColor = endColor,
                };

                canvas.SaveState();
                canvas.SetFillPaint(gradient, rect);
                canvas.FillRectangle(rect);
                canvas.RestoreState();
            }
        }

        #endregion
    }
}
