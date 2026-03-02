using System;

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
		/// Gets or sets a value indicating whether the library updated event should be
		/// suppressed on application start.
		/// <para>Not serialized — runtime state only.</para>
		/// </summary>
		bool PreventLibraryUpdatedOnStart { get; set; }
	}
}