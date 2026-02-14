using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace CommonPluginsShared.Services
{
	/// <summary>
	/// Window management interface
	/// </summary>
	public interface IWindowPluginService
	{
		string PluginName { get; }

		void ShowPluginGameDataWindow(Game gameContext);
	}
}