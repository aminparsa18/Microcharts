using System.Collections.Generic;
using System.Linq;
using Microcharts.Abstracts;
using Microsoft.Maui.Graphics;
// Both Microsoft.Maui and Microsoft.Maui.Graphics (implicit global usings under UseMaui=true) declare a
// "Font" type; alias to the one this file means.
using Font = Microsoft.Maui.Graphics.Font;

namespace Microcharts
{
    internal static class MeasureHelper
    {
        /// <summary>
        /// Measures the ink bounds of the text values.
        /// </summary>
        /// <returns>The texts bounds.</returns>
        internal static RectF[] MeasureTexts(ICanvas canvas, ITextMetricsProvider textMetrics, string[] texts, float textSize, IFont font = null)
        {
            font ??= Font.Default;
            return texts.Select(text => string.IsNullOrEmpty(text)
                ? RectF.Zero
                : textMetrics.MeasureInkBounds(canvas, text, font, textSize)).ToArray();
        }

        /// <summary>
        /// Calculates the height of the header or footer.
        /// </summary>
        /// <returns>The header or footer height.</returns>
        /// <param name="margin">the global margin of chart</param>
        /// <param name="textSize">the text size</param>
        /// <param name="textSizes">text sizes</param>
        /// <param name="orientation">orientation of content</param>
        internal static float CalculateFooterHeaderHeight(float margin, float textSize, RectF[] textSizes, Orientation orientation)
        {
            var result = margin;
            if (textSizes.Any(l => !l.IsEmpty))
            {
                if (orientation == Orientation.Vertical)
                {
                    var maxValueWidth = textSizes.Max(x => x.Width);
                    if (maxValueWidth > 0)
                    {
                        result += maxValueWidth + margin;
                    }
                }
                else
                {
                    result += textSize + margin;
                }
            }

            return result;
        }

        internal static int CalculateYAxis(bool showYAxisText, bool showYAxisLines, IEnumerable<ChartEntry> entries, int yAxisMaxTicks, ICanvas canvas, ITextMetricsProvider textMetrics, IFont yAxisTextFont, float yAxisTextSize, Position yAxisPosition, int width, bool fixedRange, ref float maxValue, ref float minValue, out float yAxisXShift, out List<float> yAxisIntervalLabels)
        {
            yAxisXShift = 0.0f;
            yAxisIntervalLabels = new List<float>();
            if (showYAxisText || showYAxisLines)
            {
                var yAxisWidth = width;
                double range, niceMin, niceMax, tickSpacing;
                int ticks;

                if (!fixedRange)
                {
                    //var enumerable = entries.ToList(); // to avoid double enumeration
                    if (minValue == maxValue)
                    {
                        if (minValue >= 0)
                            maxValue += 100;
                        else
                            maxValue = 0;
                    }

                    NiceScale.Calculate(minValue, maxValue, yAxisMaxTicks, out range, out tickSpacing, out niceMin, out niceMax);
                    // Count ticks across the actual [niceMin, niceMax] axis span, not the nice data range, so the
                    // axis never overshoots below niceMin (issue #304) or drops the final tick.
                    ticks = (int)((niceMax - niceMin) / tickSpacing) + 1;
                }
                else
                {
                    niceMin = minValue;
                    niceMax = maxValue;
                    range = niceMax - niceMin;
                    tickSpacing = range / (yAxisMaxTicks-1);
                    ticks = yAxisMaxTicks;
                }

                yAxisIntervalLabels = Enumerable.Range(0, ticks)
                    .Select(i => (float)(niceMax - (i * tickSpacing)))
                    .ToList();

                var longestYAxisLabel = yAxisIntervalLabels.Aggregate(string.Empty, (max, cur) => max.Length > cur.ToString().Length ? max : cur.ToString());
                var longestYAxisLabelWidth = MeasureTexts(canvas, textMetrics, new[] { longestYAxisLabel }, yAxisTextSize, yAxisTextFont).Select(b => b.Width).FirstOrDefault();
                yAxisWidth = (int)(width - longestYAxisLabelWidth) - 10;
                if (yAxisPosition == Position.Left)
                {
                    yAxisXShift = longestYAxisLabelWidth;
                }

                // to reduce chart width
                width = yAxisWidth;
                maxValue = (float)niceMax;
                minValue = (float)niceMin;
            }

            return width;
        }

        internal static PointF CalculatePoint(float margin, float animationProgress, float maxValue, float valueRange, float value, int i, SizeF itemSize, float origin, float headerHeight, float originX = 0)
        {
            var x = originX + margin + (itemSize.Width / 2) + (i * (itemSize.Width + margin));
            var y = headerHeight + ((1 - animationProgress) * (origin - headerHeight) + (((maxValue - value) / valueRange) * itemSize.Height) * animationProgress);

            return new PointF(x, y);
        }
    }
}
