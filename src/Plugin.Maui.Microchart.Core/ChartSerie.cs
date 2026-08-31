using System.Collections.Generic;
using Microsoft.Maui.Graphics;

namespace Plugin.Maui.Microchart
{
    /// <summary>
    /// A serie of data entries for chart
    /// </summary>
    public class ChartSerie
    {
        /// <summary>
        /// Gets or sets the name of the serie
        /// </summary>
        /// <value>Name of the serie</value>
        public string Name { get; set; } = "Default";

        /// <summary>
        /// Gets or sets the color of the fill
        /// </summary>
        /// <value>The color of the fill.</value>
        public Color? Color { get; set; } = Colors.Black;

        /// <summary>
        /// Gets or sets the color of the rest part. <c>null</c> (the default) means unset, i.e. fall back to
        /// each entry's own <see cref="ChartEntry.OtherColor"/>.
        /// </summary>
        /// <value>The color of the rest part.</value>
        /// <remarks>
        /// Was <c>SKColor?</c> defaulting to the non-null <c>SKColors.Empty</c> sentinel. Since a non-null
        /// nullable never falls through a <c>??</c> fallback, that default made the "fall back to
        /// <c>ChartEntry.OtherColor</c>" half of <c>serie.OtherColor ?? entry.OtherColor</c> call sites (e.g.
        /// <c>AxisBasedChart</c>) unreachable whenever a serie's <c>OtherColor</c> was left at its default --
        /// the coalesced result was always the serie's empty sentinel, even when the entry had a real
        /// <c>OtherColor</c> set. Defaulting to an actual <c>null</c> here fixes that: an un-set serie
        /// <c>OtherColor</c> now genuinely defers to the entry's.
        /// </remarks>
        public Color? OtherColor { get; set; }

        /// <summary>
        /// Gets or sets the entries value for the serie
        /// </summary>
        public IEnumerable<ChartEntry> Entries { get; set; }
    }
}
