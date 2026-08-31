using System.Collections.Generic;

namespace Plugin.Maui.Microchart
{
    /// <summary>
    /// Base class of simple chart
    /// </summary>
    public abstract class SimpleChart : Chart
    {
        /// <summary>
        /// Gets or Sets Entries
        /// </summary>
        /// <value>IEnumerable of <seealso cref="T:Plugin.Maui.Microchart.ChartEntry"/></value>
        public IEnumerable<ChartEntry> Entries
        {
            get => entries;
            set => UpdateEntries(value);
        }
    }
}
