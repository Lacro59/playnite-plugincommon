using Newtonsoft.Json;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonShared.Collections
{
    public abstract class PluginDataBaseGame<T> : PluginDataBaseGameBase
    {
        public abstract List<T> Items { get; set; }


        [JsonIgnore]
        public virtual bool HasData
        {
            get
            {
                return Items.Count > 0;
            }
        }
    }
}
