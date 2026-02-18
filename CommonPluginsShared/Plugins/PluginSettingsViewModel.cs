using AngleSharp.Network;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.UI;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommonPluginsShared.Plugins
{
	public class PluginSettingsViewModel: ObservableObject
	{
		protected static readonly ILogger Logger = LogManager.GetLogger();

		#region Commands

		/// <summary>
		/// Adds a tag to all games in the library based on their system check result.
		/// Replaces the former ButtonAddTag_Click code-behind handler.
		/// </summary>
		public RelayCommand CmdAddTag { get; private set; }

		/// <summary>
		/// Removes the system checker tag from all games in the library.
		/// Replaces the former ButtonRemoveTag_Click code-behind handler.
		/// </summary>
		public RelayCommand CmdRemoveTag { get; private set; }

		/// <summary>
		/// Clears all plugin data from the database.
		/// Replaces the former Button_Click code-behind handler.
		/// </summary>
		public RelayCommand CmdClearAll { get; private set; }

		#endregion

        /// <summary>
        /// Initializes all RelayCommands for the settings view.
        /// Keeping command wiring in a dedicated method avoids constructor bloat.
        /// </summary>
        public void InitializeCommands(string pluginName, IPluginDatabase pluginDatabase)
		{
			try
			{
				var commandsSettings = new CommandsSettings(pluginName, pluginDatabase);
				CmdAddTag = commandsSettings.CmdAddTag;
				CmdRemoveTag = commandsSettings.CmdRemoveTag;
				CmdClearAll = commandsSettings.CmdClearAll;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "Failed to initialize plugin settings commands.", false, pluginName);
			}
		}
	}
}