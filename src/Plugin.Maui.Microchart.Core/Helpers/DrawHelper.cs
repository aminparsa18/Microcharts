using System;
using System.Collections.Generic;
using System.Linq;
using Plugin.Maui.Microchart.Abstracts;
using Microsoft.Maui.Graphics;
// Both Microsoft.Maui and Microsoft.Maui.Graphics (implicit global usings under UseMaui=true) declare a
// "Font" type; alias to the one this file means.
using Font = Microsoft.Maui.Graphics.Font;

namespace Plugin.Maui.Microchart
{
    internal enum YPositionBehavior
    {
        None,
        UpToElementHeight,
        UpToElementMiddle,
        DownToElementMiddle
    }

    internal static class DrawHelper
    {
        internal static void DrawLabel(ICanvas canvas, ITextMetricsProvider textMetrics, Orientation orientation, YPositionBehavior yPositionBehavior, SizeF itemSize, PointF point, Color color, RectF bounds, string text, float textSize, IFont font)
        {
            font ??= Font.Default;

            canvas.SaveState();
            try
            {
                if (orientation == Orientation.Vertical)
                {
                    var y = point.Y;

                    switch (yPositionBehavior)
                    {
                        case YPositionBehavior.UpToElementHeight:
                            y -= bounds.Width;
                            break;
                        case YPositionBehavior.UpToElementMiddle:
                            y -= bounds.Width / 2;
                            break;
                        case YPositionBehavior.DownToElementMiddle:
                            y += bounds.Width / 2;
                            break;
                        case YPositionBehavior.None:
                        default:
                            break;
                    }

                    canvas.Rotate(90);
                    canvas.Translate(y, -point.X + (bounds.Height / 2));

                    DrawBaselineAnchoredString(canvas, textMetrics, text, font, textSize, color, 0, 0);
                }
                else
                {
                    if (bounds.Width > itemSize.Width)
                    {
                        text = text.Substring(0, Math.Min(3, text.Length));
                        bounds = textMetrics.MeasureInkBounds(canvas, text, font, textSize);
                    }

                    if (bounds.Width > itemSize.Width)
                    {
                        text = text.Substring(0, Math.Min(1, text.Length));
                        bounds = textMetrics.MeasureInkBounds(canvas, text, font, textSize);
                    }

                    var y = point.Y;

                    switch (yPositionBehavior)
                    {
                        case YPositionBehavior.UpToElementHeight:
                            y -= bounds.Height;
                            break;
                        case YPositionBehavior.UpToElementMiddle:
                            y -= bounds.Height / 2;
                            break;
                        case YPositionBehavior.DownToElementMiddle:
                            y += bounds.Height / 2;
                            break;
                        case YPositionBehavior.None:
                        default:
                            break;
                    }

                    canvas.Translate(point.X - (bounds.Width / 2), y);

                    DrawBaselineAnchoredString(canvas, textMetrics, text, font, textSize, color, 0, 0);
                }
            }
            finally
            {
                canvas.RestoreState();
            }
        }

        /// <summary>
        /// Extra width added to the box passed to <c>ICanvas.DrawString</c>'s boxed overload beyond the
        /// measured ink width, everywhere that overload is used for what's meant to be single-line text.
        /// Internal (not private) so <see cref="CanvasExtensions"/>'s equivalent call sites share the same
        /// value instead of duplicating it.
        /// </summary>
        /// <remarks>
        /// Works around a Microsoft.Maui.Graphics issue on Android: its <c>PlatformCanvas.DrawString(x, y,
        /// width, height, ha, va, textFlow, ...)</c> always lays the text out in a <c>StaticLayout</c>
        /// constrained to <c>width</c> -- it silently ignores the <c>textFlow</c> argument entirely (confirmed
        /// against Microsoft.Maui.Graphics' own Android source), so passing <see cref="TextFlow.OverflowBounds"/>
        /// (as every call site here does) has no effect there. <see cref="PortableTextMetricsProvider"/>'s own
        /// doc comments already note its ink-bounds width is advance-based and "under-reports true ink-bounds
        /// width by a small, roughly size-independent amount (~1-3px)" -- for glyph runs with a wide leading
        /// capital (e.g. "Tue", "Wed"), that shortfall was enough to push the real rendered width past the box,
        /// and with no space character to break on, StaticLayout hard-wrapped mid-word ("Tu"/"e", "We"/"d")
        /// instead of drawing (or even overflowing) on one line. This margin only pads the box handed to
        /// DrawString -- callers keep using the real (unpadded) ink bounds for centering/positioning, so this
        /// doesn't shift where text is anchored, only gives the wrap constraint enough slack to never trigger
        /// for what was measured as fitting.
        /// </remarks>
        internal const float TextWidthSafetyMargin = 6f;

        /// <summary>
        /// Draws <paramref name="text"/> such that its baseline sits at (<paramref name="x"/>, <paramref name="y"/>)
        /// with left alignment, matching SkiaSharp's <c>SKCanvas.DrawText(text, x, y, SKTextAlign.Left, font, paint)</c>
        /// baseline convention.
        /// </summary>
        /// <remarks>
        /// Microsoft.Maui.Graphics' <c>ICanvas.DrawString</c> has no baseline-anchored overload -- only a box
        /// with <see cref="VerticalAlignment"/>. This reconstructs baseline placement by Top-aligning a box whose
        /// top edge sits at the glyphs' measured top (<c>ink.Top</c>, i.e. <c>-ascent</c>, above the baseline);
        /// used here (rather than <see cref="CanvasExtensions.DrawTextCenteredVertically"/>'s center-based
        /// approach) because <see cref="DrawLabel"/>'s <see cref="YPositionBehavior"/> offsets are computed as
        /// precise baseline shifts, not "center around a point" -- preserving them exactly requires preserving
        /// the baseline anchor itself.
        /// </remarks>
        private static void DrawBaselineAnchoredString(ICanvas canvas, ITextMetricsProvider textMetrics, string text, IFont font, float textSize, Color color, float x, float y)
        {
            var ink = textMetrics.MeasureInkBounds(canvas, text, font, textSize);

            canvas.Font = font;
            canvas.FontSize = textSize;
            canvas.FontColor = color;
            canvas.DrawString(text, x, y + ink.Top, Math.Max(ink.Width, 0) + TextWidthSafetyMargin, ink.Height, HorizontalAlignment.Left, VerticalAlignment.Top, TextFlow.OverflowBounds);
        }

        internal static void DrawYAxis(bool showYAxisText, bool showYAxisLines, Position yAxisPosition, ICanvas canvas, ITextMetricsProvider textMetrics, IFont yAxisTextFont, float yAxisTextSize, Color yAxisTextColor, Color yAxisLinesColor, float yAxisLinesSize, float margin, float animationProgress, float maxValue, float valueRange, int width, float yAxisXShift, List<float> yAxisIntervalLabels, float headerHeight, SizeF itemSize, float origin)
        {
            if (showYAxisText || showYAxisLines)
            {
                int cnt = 0;
                var intervals = yAxisIntervalLabels
                    .Select(t => new ValueTuple<string, PointF>
                    (
                        t.ToString(),
                        new PointF(yAxisPosition == Position.Left ? yAxisXShift : width, MeasureHelper.CalculatePoint(margin, animationProgress, maxValue, valueRange, t, cnt++, itemSize, origin, headerHeight).Y)
                    ))
                    .ToList();

                if (showYAxisText)
                {
                    DrawYAxisText(canvas, textMetrics, yAxisTextFont, yAxisTextSize, yAxisTextColor, yAxisPosition, intervals);
                }

                if (showYAxisLines)
                {
                    var lines = intervals.Select(tup =>
                    {
                        (_, PointF pt) = tup;

                        return yAxisPosition == Position.Right ?
                            new RectF(0, pt.Y, width, 0) :
                            new RectF(yAxisXShift, pt.Y, width, 0);
                    });

                    DrawYAxisLines(margin, yAxisLinesColor, yAxisLinesSize, canvas, lines);
                }
            }
        }

        /// <summary>
        /// Shows a Y axis
        /// </summary>
        private static void DrawYAxisText(ICanvas canvas, ITextMetricsProvider textMetrics, IFont yAxisTextFont, float yAxisTextSize, Color yAxisTextColor, Position yAxisPosition, IEnumerable<(string Label, PointF Point)> intervals)
        {
            var textAlign = yAxisPosition == Position.Left ? HorizontalAlignment.Right : HorizontalAlignment.Left;

            foreach (var @int in intervals)
                canvas.DrawTextCenteredVertically(textMetrics, @int.Label, textAlign, yAxisTextFont, yAxisTextSize, yAxisTextColor, @int.Point.X, @int.Point.Y);
        }

        /// <summary>
        /// Draws interval lines
        /// </summary>
        private static void DrawYAxisLines(float margin, Color yAxisLinesColor, float yAxisLinesSize, ICanvas canvas, IEnumerable<RectF> intervals)
        {
            canvas.StrokeColor = yAxisLinesColor;
            canvas.StrokeSize = yAxisLinesSize;

            foreach (var @int in intervals)
            {
                canvas.DrawLine(margin / 2 + @int.Left, @int.Top, @int.Right - margin / 2, @int.Bottom);
            }
        }
    }
}
