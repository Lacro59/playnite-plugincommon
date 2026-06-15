namespace CommonPluginsShared.Interfaces
{
	/// <summary>
	/// Defines how library-wide source filtering is applied.
	/// </summary>
	public enum SourceFilterMode
	{
		/// <summary>No source filter — all sources are included.</summary>
		All,

		/// <summary>Only sources listed in <see cref="IPluginSettings.EnabledSources"/> are included.</summary>
		Whitelist,

		/// <summary>All sources except those listed in <see cref="IPluginSettings.ExcludedSources"/> are included.</summary>
		Blacklist
	}
}
