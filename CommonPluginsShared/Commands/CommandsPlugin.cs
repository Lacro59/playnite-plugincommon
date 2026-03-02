using CommonPluginsShared.Interfaces;
using CommonPluginsShared.UI;
using Playnite.SDK;
using System;
using System.Threading;
using System.Windows;

namespace CommonPluginsShared.Commands
{
	public class CommandsPlugin
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
		/// Clears all plugin data from the database, then clears the cache.
		/// </summary>
		public RelayCommand CmdClearAll { get; private set; }

		/// <summary>
		/// Deletes all temporary cache files stored by the plugin.
		/// Does not affect plugin data or the Playnite library.
		/// </summary>
		public RelayCommand CmdClearCache { get; private set; }

		/// <summary>
		/// Marks the parent window's view model as requiring a Playnite restart.
		/// Sets the <c>IsRestartRequired</c> property on the window's DataContext via reflection.
		/// </summary>
		/// <remarks>
		/// The command parameter must be the originating <see cref="FrameworkElement"/>.
		/// </remarks>
		public static RelayCommand<FrameworkElement> CmdRestartRequired => new RelayCommand<FrameworkElement>((sender) =>
		{
			try
			{
				Window parentWindow = UIHelper.FindParent<Window>(sender);
				if (parentWindow?.DataContext?.GetType().GetProperty("IsRestartRequired") != null)
				{
					((dynamic)parentWindow.DataContext).IsRestartRequired = true;
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "Failed to set restart required flag");
			}
		});

		public CommandsPlugin(string pluginName, IPluginDatabase pluginDatabase)
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

			// Clear all data command: asks for confirmation before wiping data and cache.
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

			// Clear cache command: no confirmation needed, cache files are non-destructive for user data.
			CmdClearCache = new RelayCommand(() =>
			{
				try
				{
					MessageBoxResult result = API.Instance.Dialogs.ShowMessage(
						ResourceProvider.GetString("LOCCommonClearCacheConfirm"),
						ResourceProvider.GetString($"{PluginName}"),
						MessageBoxButton.YesNo,
						MessageBoxImage.Warning);

					if (result == MessageBoxResult.Yes)
					{
						PluginDatabase.ClearCache();
					}
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, "CmdClearCache failed", true, PluginName, ResourceProvider.GetString("LOCCommonClearCacheError"));
				}
			});
		}
	}
}