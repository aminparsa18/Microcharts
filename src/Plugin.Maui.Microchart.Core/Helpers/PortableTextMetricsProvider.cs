// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Plugin.Maui.Microchart.Abstracts;
using Microsoft.Maui.Graphics;

namespace Plugin.Maui.Microchart
{
    /// <summary>
    /// A portable <see cref="ITextMetricsProvider"/>, backed by <see cref="ICanvas.GetStringSize(string, IFont, float)"/>
    /// for width and calibrated ascent/descent ratios for vertical extent, since Microsoft.Maui.Graphics has no
    /// portable ink-bounds or font-metrics API. This is the default provider used by <see cref="Chart"/>
    /// (and so by <c>Plugin.Maui.Microchart</c> and the Windows target), and the fallback for the native
    /// <c>Plugin.Maui.Microchart.iOS</c>/<c>Plugin.Maui.Microchart.Droid</c> packages when no platform-precise provider is supplied.
    /// </summary>
    /// <remarks>
    /// The ascent/descent ratios below were calibrated against <c>SKFontMetrics.Ascent</c>/<c>Descent</c> and
    /// <c>SKFont.MeasureText</c>'s ink bounds for <c>SKTypeface.Default</c> at 10/12/16/24pt, over strings
    /// representative of Plugin.Maui.Microchart label/legend/axis text (month names, signed numeric labels, single
    /// glyphs — see the Milestone-1 measurement spike). Findings:
    /// <list type="bullet">
    /// <item><description>Ascent:descent split measured a consistent ~0.77:0.23 (not the round 0.75:0.25 first
    /// assumed) across all four sizes for that typeface, and ascent+descent tracked <c>fontSize</c> almost
    /// exactly — so the *sum* is a safe stand-in for total vertical extent, only the split needed adjusting.</description></item>
    /// <item><description>Per-string ink-bounds height (what <c>SKFont.MeasureText</c> actually reports) is
    /// meaningfully smaller than <c>fontSize</c> for text without ascenders/descenders — e.g. "400" at 24pt
    /// measured 20px of ink vs. this provider's 24px — so this approximation is conservative (more computed
    /// vertical space than SkiaSharp would use), never tighter. That's the safe direction: extra whitespace,
    /// not clipped text.</description></item>
    /// <item><description>Width (advance, via <c>GetStringSize</c>) under-reports true ink-bounds width by a
    /// small, roughly size-independent amount (~1-3px for the strings measured) rather than a multiplicative
    /// error — acceptable for a v1 approximation, worth re-checking if legend/label text visually clips in
    /// later milestones.</description></item>
    /// </list>
    /// These ratios are specific to one (default, macOS-hosted) typeface; other platforms' default typefaces
    /// will differ somewhat, which is exactly why the native iOS/Android packages get a platform-precise
    /// <see cref="ITextMetricsProvider"/> instead of this one.
    /// </remarks>
    public sealed class PortableTextMetricsProvider : ITextMetricsProvider
    {
        private const float AscentRatio = 0.77f;
        private const float DescentRatio = 0.23f;

        /// <inheritdoc/>
        public RectF MeasureInkBounds(ICanvas canvas, string text, IFont font, float fontSize)
        {
            if (canvas is null || string.IsNullOrEmpty(text))
            {
                return RectF.Zero;
            }

            var size = canvas.GetStringSize(text, font, fontSize);
            var (ascent, descent) = GetFontMetrics(font, fontSize);

            // The advance-based width stands in for true ink-bounds width (close for most Latin text, but
            // doesn't account for glyph side-bearings); top/bottom follow the calibrated ascent/descent ratios
            // rather than the string's actual glyph extent (e.g. a string with no descenders will report more
            // bottom space than SkiaSharp would).
            return new RectF(0, -ascent, size.Width, ascent + descent);
        }

        /// <inheritdoc/>
        public (float Ascent, float Descent) GetFontMetrics(IFont font, float fontSize)
        {
            return (fontSize * AscentRatio, fontSize * DescentRatio);
        }
    }
}
