using CommonPlayniteShared.Common;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.UI;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.IO;
using System.Reflection;

namespace CommonPluginsShared.PlayniteExtended
{
    public abstract class PluginExtended<ISettings> : PlaynitePlugin<ISettings>
    {
        public PluginExtended(IPlayniteAPI playniteAPI, string pluginName) : base(playniteAPI, pluginName)
        {
        }
    }


    public abstract class PluginExtended<ISettings, TPluginDatabase> : PlaynitePlugin<ISettings> where TPluginDatabase : IPluginDatabase
    {
        public static TPluginDatabase PluginDatabase { get; set; }


        public PluginExtended(IPlayniteAPI playniteAPI, string pluginName) : base(playniteAPI, pluginName)
        {
            // Get plugin's database if used
            PluginDatabase = typeof(TPluginDatabase).CrateInstance<TPluginDatabase>(PluginSettings, this.GetPluginUserDataPath());
            PluginDatabase.InitializeDatabase();
		}
	}


    public abstract class PlaynitePlugin<ISettings> : GenericPlugin
    {
        internal static readonly ILogger Logger = LogManager.GetLogger();


        public static string PluginName { get; set; }

		public static string PluginFolder { get; set; }
		public static string PluginUserDataPath { get; set; }

		public ISettings PluginSettings { get; set; }


        protected PlaynitePlugin(IPlayniteAPI playniteAPI, string pluginName) : base(playniteAPI)
        {
            Properties = new GenericPluginProperties { HasSettings = true };

            PluginName = pluginName;

			// Get plugin's settings 
			PluginSettings = typeof(ISettings).CrateInstance<ISettings>(this);

            // Get plugin's location 
            PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			// Get plugin's data location 
			PluginUserDataPath = this.GetPluginUserDataPath();

            LoadCommon();
        }

        protected void LoadCommon()
        {
            // Set the common resourses & event
            Common.Load(PluginFolder, PlayniteApi.ApplicationSettings.Language);
        }
    }
}