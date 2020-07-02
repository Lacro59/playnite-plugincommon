using System;

namespace PluginCommon.LiveChartsCommon
{
    public class CustomerForTime
    {
        public string Name { get; set; }
        public long Values { get; set; }
        public string ValuesFormat => (int)TimeSpan.FromSeconds(Values).TotalHours + "h " + TimeSpan.FromSeconds(Values).ToString(@"mm") + "min";
    }
}
