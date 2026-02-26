using CommonPluginsShared.Interfaces;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace CommonPluginsShared.Plugins
{
	/// <summary>
	/// Base implementation of <see cref="IPluginSettings"/>.
	/// Inherit from this class to add plugin-specific settings.
	/// </summary>
	public class PluginSettings : ObservableObject, IPluginSettings
	{
		#region UI

		private bool _menuInExtensions = true;

		/// <inheritdoc/>
		public bool MenuInExtensions
		{
			get => _menuInExtensions;
			set => SetValue(ref _menuInExtensions, value);
		}

		#endregion

		#region Tag

		private bool _enableTag = false;

		/// <inheritdoc/>
		public bool EnableTag
		{
			get => _enableTag;
			set => SetValue(ref _enableTag, value);
		}

		#endregion

		#region Automatic update when updating library

		/// <inheritdoc/>
		public DateTime LastAutoLibUpdateAssetsDownload { get; set; } = DateTime.MinValue;

		private bool _autoImport = false;

		/// <inheritdoc/>
		public bool AutoImport
		{
			get => _autoImport;
			set => SetValue(ref _autoImport, value);
		}

		#endregion

		#region Automatic update when game is installed

		private bool _autoImportOnInstalled = false;

		/// <inheritdoc/>
		public bool AutoImportOnInstalled
		{
			get => _autoImportOnInstalled;
			set => SetValue(ref _autoImportOnInstalled, value);
		}

		#endregion

		#region Runtime state

		/// <inheritdoc/>
		[DontSerialize]
		public bool PreventLibraryUpdatedOnStart { get; set; } = true;

		private bool _hasData = false;

		/// <summary>
		/// Gets or sets a value indicating whether the plugin has data available for the current game.
		/// <para>Not serialized — runtime state only, exposed for custom theme bindings.</para>
		/// </summary>
		[DontSerialize]
		public bool HasData
		{
			get => _hasData;
			set => SetValue(ref _hasData, value);
		}

		/// <summary>
		/// Gets the current state of external library plugins.
		/// <para>Not serialized — runtime state only, exposed for custom theme bindings.</para>
		/// </summary>
		[DontSerialize]
		public PluginState PluginState => new PluginState();

		#endregion
	}

	/// <summary>
	/// Provides runtime availability state for known external library plugins.
	/// Consumed by custom themes via bindings.
	/// </summary>
	public class PluginState
	{
		/// <summary>Gets a value indicating whether the Steam library plugin is enabled.</summary>
		public bool SteamIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.SteamLibrary));

		/// <summary>Gets a value indicating whether an Epic Games library plugin is enabled (Epic or Legendary).</summary>
		public bool EpicIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.EpicLibrary))
								  || PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.LegendaryLibrary));

		/// <summary>Gets a value indicating whether a GOG library plugin is enabled (GOG or GogOss).</summary>
		public bool GogIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.GogLibrary))
								 || PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.GogOssLibrary));

		/// <summary>Gets a value indicating whether the Origin/EA library plugin is enabled.</summary>
		public bool OriginIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.OriginLibrary));

		/// <summary>Gets a value indicating whether the Xbox library plugin is enabled.</summary>
		public bool XboxIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.XboxLibrary));

		/// <summary>Gets a value indicating whether the PlayStation Network library plugin is enabled.</summary>
		public bool PsnIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.PSNLibrary));

		/// <summary>Gets a value indicating whether the Nintendo library plugin is enabled.</summary>
		public bool NintendoIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.NintendoLibrary));

		/// <summary>Gets a value indicating whether the Battle.net library plugin is enabled.</summary>
		public bool BattleNetIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.BattleNetLibrary));

		/// <summary>Gets a value indicating whether the Game Jolt library plugin is enabled.</summary>
		public bool GameJoltIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.GameJoltLibrary));
	}

	/// <summary>
	/// Defines scheduling options for automatic plugin data updates.
	/// </summary>
	public class PluginUpdate
	{
		/// <summary>
		/// Gets or sets a value indicating whether the update should run on application start.
		/// </summary>
		public bool OnStart { get; set; } = false;

		/// <summary>
		/// Gets or sets a value indicating whether the update should run on a recurring hourly schedule.
		/// </summary>
		public bool EveryHours { get; set; } = false;

		/// <summary>
		/// Gets or sets the interval in hours between automatic updates.
		/// Only relevant when <see cref="EveryHours"/> is <c>true</c>.
		/// </summary>
		public uint Hours { get; set; } = 3;
	}
}