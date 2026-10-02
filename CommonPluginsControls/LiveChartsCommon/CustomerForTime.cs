using CommonPluginsControls.Controls;
using System;

namespace CommonPluginsControls.LiveChartsCommon
{
    public class CustomerForTime
    {
        public object Icon { get; set; }
        public string IconText { get; set; }

        public bool HideIsZero { get; set; }

        public string Name { get; set; }
        public string SecondaryName { get; set; }
        public long Values { get; set; }
        /// <summary>Local session timestamp when the point maps to a single session (click selection).</summary>
        public DateTime SessionDate { get; set; }
        public string ValuesFormat => (int)TimeSpan.FromSeconds(Values).TotalHours + "h " + TimeSpan.FromSeconds(Values).ToString(@"mm") + "min";
    }
}
