using CommonPlayniteShared.Database;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Plugins;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CommonPluginsShared.Collections
{
	/// <summary>
	/// Provides common context objects (settings and database) that can be used
	/// to build Playnite menu integrations for a plugin (main menu, game menu, etc.).
	/// </summary>
	public abstract class PluginMenus
	{
		protected readonly PluginSettings _settings;
		protected readonly IPluginDatabase _database;

		protected PluginMenus(PluginSettings settings, IPluginDatabase database)
		{
			_settings = settings;
			_database = database;
		}

		public abstract IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args);
		public abstract IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args);
	}
}