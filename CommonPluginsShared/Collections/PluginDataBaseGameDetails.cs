using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonPluginsShared.Collections
{
    public abstract class PluginDataBaseGameDetails<T, Y> : PluginDataBaseGame<T>
    {
        public abstract Y ItemsDetails { get; set; }
    }
}
