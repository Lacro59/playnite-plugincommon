using CommonPluginsShared.Commands;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Plugins;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace CommonPluginsShared.Collections
{
	/// <summary>
	/// Provides common context objects (settings and database) that can be used
	/// to build Playnite menu integrations for a plugin (main menu, game menu, etc.).
	/// </summary>
	public abstract class PluginMenus
	{
		protected static readonly ILogger Logger = LogManager.GetLogger();

		protected readonly IPluginSettings _settings;
		protected readonly IPluginDatabase _database;
		protected readonly CommandsPlugin _commands;

		protected PluginMenus(IPluginSettings settings, IPluginDatabase database)
		{
			_settings = settings;
			_database = database;

			_commands = new CommandsPlugin(_database.PluginName, database);
		}

		/// <inheritdoc cref="Plugin.GetGameMenuItems(GetGameMenuItemsArgs)"/>
		public abstract IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args);

		/// <inheritdoc cref="Plugin.GetMainMenuItems(GetMainMenuItemsArgs)"/>
		public abstract IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args);

	}
}