using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace CommonPluginsShared.Services
{
	/// <summary>
	/// Window management interface
	/// </summary>
	public interface IPluginWindows
	{
		string PluginName { get; }

		IPluginDatabase PluginDatabase { get; }

		void ShowPluginGameDataWindow(GenericPlugin plugin, Game gameContext);

		void ShowPluginGameDataWindow(GenericPlugin plugin);

		void ShowPluginGameDataWindow(Game gameContext);

		void ShowPluginGameNoDataWindow();

		void ShowPluginDataMismatch();

		void ShowPluginTransfertData(IEnumerable<DataGame> dataGames);

		void ShowPluginDataWithoutGame(IEnumerable<DataGame> dataGames);
	}
}