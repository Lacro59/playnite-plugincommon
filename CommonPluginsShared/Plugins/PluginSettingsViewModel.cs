using CommonPluginsShared.Commands;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CommonPluginsShared.Plugins
{
	/// <summary>
	/// Shared helpers for plugin settings view-models (commands, clone helpers, persist + verbose sync).
	/// </summary>
	public class PluginSettingsViewModel : ObservableObject
	{
		protected static readonly ILogger Logger = LogManager.GetLogger();

		/// <summary>
		/// Restores settings values in-place without swapping the settings instance reference.
		/// This preserves existing bindings and <see cref="ObservableObject.PropertyChanged"/> subscribers.
		/// </summary>
		/// <typeparam name="TSettings">Concrete settings type.</typeparam>
		/// <param name="source">Snapshot source (typically captured in BeginEdit).</param>
		/// <param name="target">Current runtime settings instance to mutate.</param>
		protected static void CopySettingsValues<TSettings>(TSettings source, TSettings target) where TSettings : class
		{
			if (source == null || target == null)
			{
				return;
			}

			PropertyInfo[] properties = typeof(TSettings).GetProperties(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
			foreach (PropertyInfo property in properties)
			{
				if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length != 0)
				{
					continue;
				}

				object value = property.GetValue(source, null);
				property.SetValue(target, value, null);
			}
		}

		/// <summary>
		/// Persists settings through Playnite and synchronizes verbose logging (Info state log included).
		/// Prefer this over a bare <c>SavePluginSettings</c> in <c>EndEdit</c> so every plugin gets the same sync.
		/// </summary>
		/// <param name="plugin">Plugin instance used by Playnite to locate the settings file.</param>
		/// <param name="settings">Settings model to persist.</param>
		protected void PersistSettings(Plugin plugin, IPluginSettings settings)
		{
			if (plugin == null || settings == null)
			{
				return;
			}

			plugin.SavePluginSettings(settings);
			Common.SyncVerboseLoggingFromSettings(settings, "settings-saved");
		}

		/// <summary>
		/// Synchronizes verbose logging after settings were already persisted (for example by <c>PersistSettingsAction</c>).
		/// </summary>
		/// <param name="settings">Settings model currently in memory.</param>
		protected void SyncVerboseLoggingAfterSave(IPluginSettings settings)
		{
			Common.SyncVerboseLoggingFromSettings(settings, "settings-saved");
		}

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
