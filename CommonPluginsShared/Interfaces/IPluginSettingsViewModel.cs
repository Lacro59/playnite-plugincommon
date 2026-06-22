using CommonPluginsShared.Plugins;
using Playnite.SDK;

namespace CommonPluginsShared.Interfaces
{
	/// <summary>
	/// Contract that all plugin settings view-models must satisfy.
	/// Exposes the inner <see cref="PluginSettings"/> model without reflection.
	/// </summary>
	public interface IPluginSettingsViewModel: ISettings
	{
		/// <summary>Gets the inner settings model instance.</summary>
		IPluginSettings Settings { get; }
	}
}