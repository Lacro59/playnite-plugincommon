using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using System;
using System.Threading;
using System.Windows;

namespace CommonPluginsShared.UI { 	
	public class CommandsSettings
	{
		private static readonly ILogger Logger = LogManager.GetLogger();

		public string PluginName { get; private set; }

		public IPluginDatabase PluginDatabase { get; private set; }

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

		public CommandsSettings(string pluginName, IPluginDatabase pluginDatabase)
		{
			PluginName = pluginName;
			PluginDatabase = pluginDatabase;

			// Add tag command: delegates to the plugin database helper.
			// Wrapped in try/catch to surface errors via Playnite notifications
			// rather than crashing the settings dialog.
			CmdAddTag = new RelayCommand(() =>
			{
				try
				{
					PluginDatabase.AddTagAllGames();
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, "CmdAddTag failed", true, PluginName, ResourceProvider.GetString("LOCCommonAddTagError"));
				}
			});

			// Remove tag command: delegates to the plugin database helper.
			CmdRemoveTag = new RelayCommand(() =>
			{
				try
				{
					PluginDatabase.RemoveTagAllGames();
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, "CmdRemoveTag failed", true, PluginName, ResourceProvider.GetString("LOCCommonTagError"));
				}
			});

			// Clear all data command: asks for confirmation before wiping data.
			// Uses Playnite's built-in dialog API to stay consistent with the host UI.
			CmdClearAll = new RelayCommand(() =>
			{
				try
				{
					MessageBoxResult result = API.Instance.Dialogs.ShowMessage(
						ResourceProvider.GetString("LOCCommonClearAllConfirm"),
						ResourceProvider.GetString($"{PluginName}"),
						MessageBoxButton.YesNo,
						MessageBoxImage.Warning);

					if (result == MessageBoxResult.Yes)
					{
						PluginDatabase.ClearDatabase();
					}
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, "CmdClearAll failed", true, PluginName, ResourceProvider.GetString("LOCCommonClearAllError"));
				}
			});
		}
	}
}