// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Plugin.Maui.Microchart.Abstracts;
using Microsoft.Maui.Graphics;
// Both Microsoft.Maui and Microsoft.Maui.Graphics (implicit global usings under UseMaui=true) declare a
// "Font" type; alias to the one this file means.
using Font = Microsoft.Maui.Graphics.Font;

namespace Plugin.Maui.Microchart
{
    internal static class CanvasExtensions
    {
        /// <summary>
        /// Draws a caption label (and optional bold value label) vertically centered around <paramref name="point"/>.
        /// When both are present, the label is centered above <paramref name="point"/> and the value below it
        /// (or vice versa for the horizontal offset direction), each offset by 60% of <paramref name="textSize"/>.
        /// </summary>
        /// <remarks>
        /// SkiaSharp's version computed a baseline y from <c>(bounds.Top + bounds.Bottom) / 2</c> because
        /// <c>SKCanvas.DrawText</c> only draws from a baseline. Microsoft.Maui.Graphics' <c>DrawString</c> can
        /// center text within an explicit box (<see cref="VerticalAlignment.Center"/>), which is a more direct
        /// expression of the original "center this text at a point" intent and doesn't depend on
        /// <see cref="ITextMetricsProvider"/>'s ascent/descent split being exactly right (Maui.Graphics' own
        /// renderer centers the actual glyphs within the box) -- only the measured width matters here, and only
        /// for horizontal alignment and the returned bounds.
        /// </remarks>
        public static void DrawCaptionLabels(this ICanvas canvas, ITextMetricsProvider textMetrics, string label, Color labelColor, string value, Color valueColor, float textSize, PointF point, HorizontalAlignment horizontalAlignment, IFont font, out RectF totalBounds)
        {
            var hasLabel = !string.IsNullOrEmpty(label);
            var hasValueLabel = !string.IsNullOrEmpty(value);

            totalBounds = RectF.Zero;

            if (!hasLabel && !hasValueLabel)
            {
                return;
            }

            var hasOffset = hasLabel && hasValueLabel;
            var captionMargin = textSize * 0.60f;
            var space = hasOffset ? captionMargin : 0;

            if (hasLabel)
            {
                totalBounds = DrawCenteredLine(canvas, textMetrics, label, labelColor, textSize, point, -space, horizontalAlignment, font, bold: false);
            }

            if (hasValueLabel)
            {
                var valueBounds = DrawCenteredLine(canvas, textMetrics, value, valueColor, textSize, point, space, horizontalAlignment, font, bold: true);
                totalBounds = hasLabel ? totalBounds.Union(valueBounds) : valueBounds;
            }
        }

        private static RectF DrawCenteredLine(ICanvas canvas, ITextMetricsProvider textMetrics, string text, Color color, float textSize, PointF point, float yOffset, HorizontalAlignment horizontalAlignment, IFont font, bool bold)
        {
            var drawFont = bold ? Bolden(font) : (font ?? Font.Default);
            var ink = textMetrics.MeasureInkBounds(canvas, text, drawFont, textSize);
            var boxHeight = ink.Height > 0 ? ink.Height : textSize;
            var boxLeft = GetBoxLeft(point.X, ink.Width, horizontalAlignment);
            var boxTop = point.Y + yOffset - (boxHeight / 2);

            canvas.Font = drawFont;
            canvas.FontSize = textSize;
            canvas.FontColor = color;
            canvas.DrawString(text, boxLeft, boxTop, ink.Width, boxHeight, horizontalAlignment, VerticalAlignment.Center, TextFlow.OverflowBounds);

            return new RectF(boxLeft, boxTop, ink.Width, boxHeight);
        }

        private static Font Bolden(IFont font)
        {
            var basis = font ?? Font.Default;
            return new Font(basis.Name, FontWeights.Bold, basis.StyleType);
        }

        /// <summary>
        /// Draws the given point.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="point">The point.</param>
        /// <param name="color">The fill color.</param>
        /// <param name="size">The point size.</param>
        /// <param name="mode">The point mode.</param>
        public static void DrawPoint(this ICanvas canvas, PointF point, Color color, float size, PointMode mode)
        {
            canvas.FillColor = color;

            switch (mode)
            {
                case PointMode.Square:
                    canvas.FillRectangle(point.X - (size / 2), point.Y - (size / 2), size, size);
                    break;

                case PointMode.Circle:
                    canvas.FillEllipse(point.X - (size / 2), point.Y - (size / 2), size, size);
                    break;
            }
        }

        /// <summary>
        /// Draws a line with a gradient stroke.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="startPoint">The starting point.</param>
        /// <param name="startColor">The starting color.</param>
        /// <param name="endPoint">The end point.</param>
        /// <param name="endColor">The end color.</param>
        /// <param name="size">The stroke size.</param>
        /// <remarks>
        /// <see cref="ICanvas"/> has no gradient-stroke primitive (<see cref="ICanvas.SetFillPaint"/> only
        /// applies to fills) -- unlike SkiaSharp, where a gradient <c>SKShader</c> could be assigned directly to
        /// a stroke <c>SKPaint</c>. This reconstructs a stroke by filling a thin quadrilateral strip running
        /// along the line with a two-stop <see cref="LinearGradientPaint"/>.
        /// </remarks>
        public static void DrawGradientLine(this ICanvas canvas, PointF startPoint, Color startColor, PointF endPoint, Color endColor, float size)
        {
            var dx = endPoint.X - startPoint.X;
            var dy = endPoint.Y - startPoint.Y;
            var length = MathF.Sqrt((dx * dx) + (dy * dy));

            if (length <= 0 || size <= 0)
            {
                return;
            }

            var halfSize = size / 2;
            var nx = -dy / length * halfSize;
            var ny = dx / length * halfSize;

            var strip = new PathF();
            strip.MoveTo(startPoint.X + nx, startPoint.Y + ny);
            strip.LineTo(endPoint.X + nx, endPoint.Y + ny);
            strip.LineTo(endPoint.X - nx, endPoint.Y - ny);
            strip.LineTo(startPoint.X - nx, startPoint.Y - ny);
            strip.Close();

            var gradient = new LinearGradientPaint(startPoint, endPoint)
            {
                StartColor = startColor,
                EndColor = endColor,
            };

            canvas.SaveState();
            canvas.SetFillPaint(gradient, strip.Bounds);
            canvas.FillPath(strip, WindingMode.NonZero);
            canvas.RestoreState();
        }

        /// <summary>
        /// Draws text vertically centered on <paramref name="point"/>.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="textMetrics">The text metrics provider used to measure <paramref name="text"/>.</param>
        /// <param name="text">The text to display</param>
        /// <param name="textAlign">The text alignment</param>
        /// <param name="font">The font to use for text and calculations</param>
        /// <param name="fontSize">The font size, in points.</param>
        /// <param name="color">The text color.</param>
        /// <param name="point">The point to vertically center the text on.</param>
        public static void DrawTextCenteredVertically(this ICanvas canvas, ITextMetricsProvider textMetrics, string text, HorizontalAlignment textAlign, IFont font, float fontSize, Color color, PointF point)
            => canvas.DrawTextCenteredVertically(textMetrics, text, textAlign, font, fontSize, color, point.X, point.Y);

        /// <summary>
        /// Draws text vertically centered on (<paramref name="x"/>, <paramref name="y"/>).
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="textMetrics">The text metrics provider used to measure <paramref name="text"/>.</param>
        /// <param name="text">The text to display</param>
        /// <param name="textAlign">The text alignment</param>
        /// <param name="font">The font to use for text and calculations</param>
        /// <param name="fontSize">The font size, in points.</param>
        /// <param name="color">The text color.</param>
        /// <param name="x">The x position to align the text at.</param>
        /// <param name="y">The y position to vertically center the text on.</param>
        public static void DrawTextCenteredVertically(this ICanvas canvas, ITextMetricsProvider textMetrics, string text, HorizontalAlignment textAlign, IFont font, float fontSize, Color color, float x, float y)
        {
            var drawFont = font ?? Font.Default;
            var ink = textMetrics.MeasureInkBounds(canvas, text, drawFont, fontSize);
            var boxHeight = ink.Height > 0 ? ink.Height : fontSize;
            var boxLeft = GetBoxLeft(x, ink.Width, textAlign);
            var boxTop = y - (boxHeight / 2);

            canvas.Font = drawFont;
            canvas.FontSize = fontSize;
            canvas.FontColor = color;
            canvas.DrawString(text, boxLeft, boxTop, ink.Width, boxHeight, textAlign, VerticalAlignment.Center, TextFlow.OverflowBounds);
        }

        /// <summary>
        /// Computes the left edge of a text box of the given <paramref name="width"/>, anchored at
        /// <paramref name="x"/> according to <paramref name="horizontalAlignment"/> -- the box-based equivalent
        /// of SkiaSharp's point + <c>SKTextAlign</c> anchoring.
        /// </summary>
        private static float GetBoxLeft(float x, float width, HorizontalAlignment horizontalAlignment) => horizontalAlignment switch
        {
            HorizontalAlignment.Center => x - (width / 2),
            HorizontalAlignment.Right => x - width,
            _ => x,
        };
    }
}
