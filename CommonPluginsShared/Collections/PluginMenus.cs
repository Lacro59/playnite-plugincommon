using CommonPlayniteShared.Database;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Plugins;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Threading;

namespace CommonPluginsShared.Collections
{
	/// <summary>
	/// Provides common context objects (settings and database) that can be used
	/// to build Playnite menu integrations for a plugin (main menu, game menu, etc.).
	/// </summary>
	public class PluginMenus
	{
		internal readonly PluginSettings _settings;
		internal readonly IPluginDatabase _database;

		/// <summary>
		/// Initializes a new instance of the <see cref="PluginMenus"/> class.
		/// </summary>
		/// <param name="pluginSettings">The plugin settings view model.</param>
		/// <param name="database">The plugin database service.</param>
		public PluginMenus(PluginSettings pluginSettings, IPluginDatabase database)
		{
			_settings = pluginSettings;
			_database = database;
		}
	}
}