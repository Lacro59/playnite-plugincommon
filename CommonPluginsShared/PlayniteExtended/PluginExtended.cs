using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsShared.PlayniteExtended
{
    public abstract class PluginExtended : Plugin
    {
        public PluginExtended(IPlayniteAPI api) : base(api)
        {
           
        }
    }
}
