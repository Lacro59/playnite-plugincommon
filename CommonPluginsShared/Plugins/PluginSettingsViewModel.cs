using CommonPluginsShared.Commands;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using System;
using System.Collections.Generic;

namespace CommonPluginsShared.Plugins
{
	public class PluginSettingsViewModel: ObservableObject
	{
		protected static readonly ILogger Logger = LogManager.GetLogger();

		#region Commands

		/// <summary>
		/// Adds a tag to all games in the library based on their system check result.
		/// </summary>
		public RelayCommand CmdAddTag { get; private set; }

		/// <summary>
		/// Removes the system checker tag from all games in the library.
		/// </summary>
		public RelayCommand CmdRemoveTag { get; private set; }

		/// <summary>
		/// Clears all plugin data from the database.
		/// </summary>
		public RelayCommand CmdClearAll { get; private set; }

		/// <summary>
		/// Deletes all temporary cache files stored by the plugin.
		/// Does not affect plugin data or the Playnite library.
		/// </summary>
		public RelayCommand CmdClearCache { get; private set; }

		#endregion

		/// <summary>
		/// Initializes all RelayCommands for the settings view.
		/// </summary>
		public void InitializeCommands(string pluginName, IPluginDatabase pluginDatabase)
		{
			try
			{
				var commandsPlugin = new CommandsPlugin(pluginName, pluginDatabase);
				CmdAddTag = commandsPlugin.CmdAddTag;
				CmdRemoveTag = commandsPlugin.CmdRemoveTag;
				CmdClearAll = commandsPlugin.CmdClearAll;
				CmdClearCache = commandsPlugin.CmdClearCache;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "Failed to initialize plugin settings commands.", false, pluginName);
			}
		}
	}
}