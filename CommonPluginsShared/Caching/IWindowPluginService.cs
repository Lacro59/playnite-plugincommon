using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace CommonPluginsShared.Interfaces
{
	/// <summary>
	/// Window management interface
	/// </summary>
	public interface IWindowPluginService
	{
		string PluginName { get; }
		IPluginDatabase PluginDatabase { get; }

		void ShowPluginGameDataWindow(Game gameContext);

		void ShowPluginGameNoDataWindow();
	}
}