using CommonPlayniteShared.Common;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Plugins;
using CommonPluginsShared.UI;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.IO;
using System.Reflection;
using SystemChecker.Services;

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

		protected PluginMenus _menus;

		public PluginExtended(IPlayniteAPI playniteAPI, string pluginName) : base(playniteAPI, pluginName)
        {
            // Get plugin's database if used
            PluginDatabase = typeof(TPluginDatabase).CrateInstance<TPluginDatabase>(PluginSettingsViewModel, this.GetPluginUserDataPath());
            PluginDatabase.InitializeDatabase();
		}
	}


    public abstract class PlaynitePlugin<ISettings> : GenericPlugin
    {
        protected static readonly ILogger Logger = LogManager.GetLogger();

        public static string PluginName { get; set; }

		public static string PluginFolder { get; set; }
		public static string PluginUserDataPath { get; set; }

		public ISettings PluginSettingsViewModel { get; set; }


        protected PlaynitePlugin(IPlayniteAPI playniteAPI, string pluginName) : base(playniteAPI)
        {
            Properties = new GenericPluginProperties { HasSettings = true };

            PluginName = pluginName;

			// Get plugin's settings 
			PluginSettingsViewModel = typeof(ISettings).CrateInstance<ISettings>(this);

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