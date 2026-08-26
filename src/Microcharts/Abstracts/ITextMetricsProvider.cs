// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Maui.Graphics;

namespace Microcharts.Abstracts
{
    /// <summary>
    /// Provides the text metrics that label, legend, and axis layout depend on: the ink bounds of a measured
    /// string, and a font's ascent/descent.
    /// </summary>
    /// <remarks>
    /// SkiaSharp's <c>SKFont.MeasureText</c> returns an ink-bounds glyph rectangle, and
    /// <c>SKFontMetrics.Ascent</c>/<c>Descent</c> give exact font metrics; nearly all of Microcharts' label
    /// positioning math is built on those two numbers. Microsoft.Maui.Graphics has no portable equivalent —
    /// <see cref="ICanvas.GetStringSize(string, IFont, float)"/> only returns an advance-based
    /// <see cref="SizeF"/>. This abstraction lets a platform that can answer precisely (iOS via
    /// <c>UIFont.Ascender</c>/<c>Descender</c> + <c>NSString.BoundingRect</c>, Android via
    /// <c>Paint.FontMetrics</c> + <c>Paint.GetTextBounds</c>) supply an exact implementation, while a portable,
    /// ratio-based approximation (<c>PortableTextMetricsProvider</c>) covers everywhere else.
    /// </remarks>
    public interface ITextMetricsProvider
    {
        /// <summary>
        /// Measures the ink bounds of <paramref name="text"/> when drawn with <paramref name="font"/> at
        /// <paramref name="fontSize"/>, relative to the text's drawing origin (baseline at y = 0, matching
        /// <c>SKFont.MeasureText</c>'s bounds convention: negative <c>Y</c> is above the baseline).
        /// </summary>
        /// <param name="canvas">
        /// The canvas the text will be drawn on. Portable implementations need this to call
        /// <see cref="ICanvas.GetStringSize(string, IFont, float)"/>; platform-precise implementations ignore it.
        /// </param>
        /// <param name="text">The text to measure.</param>
        /// <param name="font">The font the text will be drawn with.</param>
        /// <param name="fontSize">The font size, in points.</param>
        RectF MeasureInkBounds(ICanvas canvas, string text, IFont font, float fontSize);

        /// <summary>
        /// Returns <paramref name="font"/>'s ascent (distance from the baseline to the top of the font,
        /// positive) and descent (distance from the baseline to the bottom of the font, positive) at
        /// <paramref name="fontSize"/>.
        /// </summary>
        (float Ascent, float Descent) GetFontMetrics(IFont font, float fontSize);
    }
}
