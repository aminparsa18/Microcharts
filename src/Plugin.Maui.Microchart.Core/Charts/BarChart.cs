// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Maui.Graphics;

namespace Plugin.Maui.Microchart
{
    /// <summary>
    /// ![chart](../images/BarSeries.png)
    ///
    /// A grouped bar chart.
    /// </summary>
    public class BarChart : AxisBasedChart
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Plugin.Maui.Microchart.BarSeriesChart"/> class.
        /// </summary>
        public BarChart() : base()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the bar background area alpha.
        /// </summary>
        /// <value>The bar area alpha.</value>
        public byte BarAreaAlpha { get; set; } = DefaultValues.BarAreaAlpha;

        /// <summary>
        /// Get or sets the minimum height for a bar
        /// </summary>
        /// <value>The minium height of a bar.</value>
        public float MinBarHeight { get; set; } = DefaultValues.MinBarHeight;

        /// <summary>
        /// Get or sets the corner radius for a bar
        /// </summary>
        /// <value>The corner radius of a bar.</value>
        public float CornerRadius { get; set; } = DefaultValues.CornerRadius;

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override float CalculateHeaderHeight(Dictionary<ChartEntry, RectF> valueLabelSizes)
        {
            if (ValueLabelOption == ValueLabelOption.None || ValueLabelOption == ValueLabelOption.OverElement)
                return Margin;

            return base.CalculateHeaderHeight(valueLabelSizes);
        }

        /// <inheritdoc/>
        protected override void DrawValueLabel(ICanvas canvas, Dictionary<ChartEntry, RectF> valueLabelSizes, float headerWithLegendHeight, SizeF itemSize, SizeF barSize, ChartEntry entry, float barX, float barY, float itemX, float origin)
        {
            if (string.IsNullOrEmpty(entry?.ValueLabel))
                return;

            (PointF location, SizeF size) = GetBarDrawingProperties(headerWithLegendHeight, itemSize, barSize, 0, barX, barY);
            if(ValueLabelOption == ValueLabelOption.TopOfChart)
                base.DrawValueLabel(canvas, valueLabelSizes, headerWithLegendHeight, itemSize, barSize, entry, barX, barY, itemX, origin);
            else if(ValueLabelOption == ValueLabelOption.TopOfElement)
                DrawHelper.DrawLabel(canvas, TextMetricsProvider, ValueLabelOrientation, ValueLabelOrientation == Orientation.Vertical ? YPositionBehavior.UpToElementHeight : YPositionBehavior.None, barSize, new PointF(location.X + size.Width / 2, barY - Margin), entry.ValueLabelColor.MultiplyAlpha(AnimationProgress), valueLabelSizes[entry], entry.ValueLabel, ValueLabelTextSize, Typeface);
            else if(ValueLabelOption == ValueLabelOption.OverElement)
                DrawHelper.DrawLabel(canvas, TextMetricsProvider, ValueLabelOrientation, ValueLabelOrientation == Orientation.Vertical ? YPositionBehavior.UpToElementMiddle : YPositionBehavior.DownToElementMiddle, barSize, new PointF(location.X + size.Width / 2, barY + (origin - barY) / 2), entry.ValueLabelColor.MultiplyAlpha(AnimationProgress), valueLabelSizes[entry], entry.ValueLabel, ValueLabelTextSize, Typeface);
        }

        /// <inheritdoc />
        protected override void DrawBar(ChartSerie serie, ICanvas canvas, float headerHeight, float itemX, SizeF itemSize, SizeF barSize, float origin, float barX, float barY, Color color)
        {
            canvas.SaveState();
            canvas.FillColor = color;

            (PointF location, SizeF size) = GetBarDrawingProperties(headerHeight, itemSize, barSize, origin, barX, barY);
            canvas.FillRoundedRectangle(location.X, location.Y, size.Width, size.Height, CornerRadius);

            // If bar was drawn with corners, cover the bottom corners with a rectangle to give a "rounded top" look.
            if (CornerRadius > 0)
            {
                float coverRectHeight = size.Height / 2;
                float coverRectY = location.Y + size.Height - coverRectHeight;
                canvas.FillRectangle(location.X, coverRectY, size.Width, coverRectHeight);
            }

            canvas.RestoreState();
        }

        private (PointF location, SizeF size) GetBarDrawingProperties(float headerHeight, SizeF itemSize, SizeF barSize, float origin, float barX, float barY)
        {
            var x = barX - (itemSize.Width / 2);
            var y = Math.Min(origin, barY);
            var height = Math.Abs(origin - barY);
            // Enforce a minimum height only for non-zero bars, so a zero-value bar stays empty (issue #263).
            if (height > 0 && height < MinBarHeight)
            {
                height = MinBarHeight;
                // Keep the bar anchored to the axis origin: a positive bar grows up from origin
                // (a negative bar's top is already at origin and grows down from it).
                if (barY < origin)
                {
                    y = origin - height;
                }
            }

            return (new PointF(x, y), new SizeF(barSize.Width, height));
        }

        /// <inheritdoc />
        protected override void DrawBarArea(ICanvas canvas, float headerHeight, SizeF itemSize, SizeF barSize, Color color, Color otherColor, float origin, float value, float barX, float barY)
        {
            Color fillColor = null;
            if(otherColor != null)
            {
                fillColor = otherColor;
            }else if(BarAreaAlpha > 0)
            {
                fillColor = color.MultiplyAlpha((this.BarAreaAlpha / 255f) * this.AnimationProgress);
            }


            if (fillColor != null)
            {
                canvas.SaveState();
                canvas.FillColor = fillColor;

                var max = value > 0 ? headerHeight : headerHeight + itemSize.Height;
                var height = Math.Abs(max - barY) + Math.Min(origin - barY, CornerRadius);
                var y = Math.Min(max, barY);
                canvas.FillRectangle(barX - (itemSize.Width / 2), y, barSize.Width, height);

                canvas.RestoreState();
            }
        }

        #endregion
    }
}
