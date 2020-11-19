using Newtonsoft.Json;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginCommon.Collections
{
    public abstract class PluginDataBaseGameDetails<T, Y> : PluginDataBaseGameBase
    {
        public abstract List<T> Items { get; set; }

        public abstract Y ItemsDetails { get; set; }
    }
}
