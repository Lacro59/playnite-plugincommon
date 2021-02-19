using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace CommonPluginsShared.PlayniteExtended
{
    public abstract class PluginExtended<ISettings, TPluginDatabase> : Plugin
        where TPluginDatabase : IPluginDatabase
    {
        internal static readonly ILogger logger = LogManager.GetLogger();
        internal static IResourceProvider resources = new ResourceProvider();

        public ISettings PluginSettings { get; set; }

        public string PluginFolder { get; set; }
        public static TPluginDatabase PluginDatabase { get; set; }


        public PluginExtended(IPlayniteAPI api, bool WithDatabase = false) : base(api)
        {
            // Get plugin's settings 
            PluginSettings = typeof(ISettings).CrateInstance<ISettings>(this);

            // Get plugin's location 
            PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Get plugin's database if used
            if (WithDatabase)
            {
                PluginDatabase = typeof(TPluginDatabase).CrateInstance<TPluginDatabase>(PlayniteApi, PluginSettings, this.GetPluginUserDataPath());
                PluginDatabase.InitializeDatabase();
            }

            LoadCommon();
        }

        protected void LoadCommon()
        {
            // Set the common resourses & event
            Common.Load(PluginFolder, PlayniteApi.ApplicationSettings.Language);
            Common.SetEvent(PlayniteApi);
        }
    }
}
