using System;
using System.Collections.Generic;

namespace CommonPluginsShared.Interfaces
{
	/// <summary>
	/// Defines the base contract for all plugin settings.
	/// </summary>
	public interface IPluginSettings
	{
		/// <summary>
		/// Gets or sets a value indicating whether the plugin menu is displayed in the Extensions menu.
		/// </summary>
		bool MenuInExtensions { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether tag features are enabled.
		/// </summary>
		bool EnableTag { get; set; }

		/// <summary>
		/// Gets or sets the date of the last automatic library update assets download.
		/// Defaults to <see cref="DateTime.MinValue"/> when no download has occurred yet.
		/// </summary>
		DateTime LastAutoLibUpdateAssetsDownload { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether automatic data import is enabled
		/// when the library is updated.
		/// </summary>
		bool AutoImport { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether automatic data import is enabled
		/// when a game is installed.
		/// </summary>
		bool AutoImportOnInstalled { get; set; }

		/// <summary>
		/// Gets or sets the number of backup files retained for the plugin database.
		/// </summary>
		int DatabaseBackupMaxCount { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the library updated event should be
		/// suppressed on application start.
		/// <para>Not serialized — runtime state only.</para>
		/// </summary>
		bool PreventLibraryUpdatedOnStart { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether emulated games are included in plugin
		/// library operations (refresh, import, tags, selection dialogs).
		/// </summary>
		bool IncludeEmulatedGames { get; set; }

		/// <summary>
		/// Gets or sets how source-based library filtering is applied.
		/// </summary>
		SourceFilterMode LibrarySourceFilterMode { get; set; }

		/// <summary>
		/// Gets or sets normalized source names included when <see cref="LibrarySourceFilterMode"/> is <see cref="SourceFilterMode.Whitelist"/>.
		/// Values are compared against <c>PlayniteTools.GetSourceName</c>.
		/// </summary>
		List<string> EnabledSources { get; set; }

		/// <summary>
		/// Gets or sets normalized source names excluded when <see cref="LibrarySourceFilterMode"/> is <see cref="SourceFilterMode.Blacklist"/>.
		/// Values are compared against <c>PlayniteTools.GetSourceName</c>.
		/// </summary>
		List<string> ExcludedSources { get; set; }
	}
}
