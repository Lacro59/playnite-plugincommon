using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonPluginsShared.Collections
{
    public abstract class PluginDataBaseGame<T> : PluginDataBaseGameBase
    {
        public abstract List<T> Items { get; set; }

        public DateTime DateLastRefresh { get; set; } = default(DateTime);

        [DontSerialize]
        public override bool HasData
        {
            get
            {
                return Items?.Count > 0;
            }
        }
    }
}
