// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Plugin.Maui.Microchart
{
    using Microsoft.Maui.Graphics;

    /// <summary>
    /// A data entry for a chart.
    /// </summary>
    public class ChartEntry
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Plugin.Maui.Microchart.ChartEntry"/> class.
        /// </summary>
        /// <param name="value">The entry value.</param>
        public ChartEntry(float? value)
        {
            this.Value = value;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public float? Value { get; }

        /// <summary>
        /// Gets or sets the caption label.
        /// </summary>
        /// <value>The label.</value>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the label associated to the value.
        /// </summary>
        /// <value>The value label.</value>
        public string ValueLabel { get; set; }

        /// <summary>
        /// Gets or sets the color of the fill.
        /// </summary>
        /// <value>The color of the fill.</value>
        public Color Color { get; set; } = Colors.Black;

        /// <summary>
        /// Gets or sets the color of the rest part. <c>null</c> means unset (no "rest" color drawn).
        /// </summary>
        /// <value>The color of the rest part.</value>
        /// <remarks>
        /// Was a non-nullable <c>SKColor</c> compared against the <c>SKColor.Empty</c> sentinel to mean "unset";
        /// Microsoft.Maui.Graphics' <see cref="Color"/> has no equivalent empty sentinel, so this is now a
        /// nullable <see cref="Color"/> with <c>null</c> meaning unset.
        /// </remarks>
        public Color OtherColor { get; set; }

        /// <summary>
        /// Gets or sets the color of the text (for the caption label).
        /// </summary>
        /// <value>The color of the text.</value>
        public Color TextColor { get; set; } = Colors.Gray;

        /// <summary>
        /// Gets or sets the color of the value label
        /// </summary>
        /// <value>The color of the value label.</value>
        public Color ValueLabelColor { get; set; } = Colors.Black;

        #endregion
    }
}
