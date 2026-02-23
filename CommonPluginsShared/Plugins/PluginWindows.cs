using CommonPluginsControls.Views;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Windows;
using Playnite.SDK.Plugins;
using System.Collections.Generic;
using CommonPluginsShared.Models;
using System.Linq;

namespace CommonPluginsShared.Plugins
{
	public class PluginWindows : IPluginWindows
	{
		protected static readonly ILogger Logger = LogManager.GetLogger();

		public string PluginName { get; private set; }

		public IPluginDatabase PluginDatabase { get; private set; }

		public PluginWindows(string pluginName, IPluginDatabase pluginDatabase)
		{
			PluginName = pluginName;
			PluginDatabase = pluginDatabase;

			if (PluginDatabase == null)
			{
				Logger.Warn("WindowPluginService created with a null PluginDatabase instance.");
			}
		}

		public virtual void ShowPluginGameDataWindow(GenericPlugin plugin, Game gameContext)
		{
			throw new System.NotImplementedException();
		}

		public virtual void ShowPluginGameDataWindow(GenericPlugin plugin)
		{
			throw new System.NotImplementedException();
		}

		public virtual void ShowPluginGameDataWindow(Game gameContext)
		{
			throw new System.NotImplementedException();
		}

		public virtual void ShowPluginGameNoDataWindow()
		{
			throw new System.NotImplementedException();
		}

		public void ShowPluginDataWithoutGame(IEnumerable<DataGame> dataGames)
		{
			WindowOptions windowOptions = new WindowOptions
			{
				ShowMinimizeButton = false,
				ShowMaximizeButton = false,
				ShowCloseButton = true,
				CanBeResizable = false,
				Height = 700,
				Width = 1000
			};

			var viewExtension = new ListWithNoData(PluginDatabase);
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
				PluginName,
				viewExtension,
				windowOptions);
			windowExtension.ShowDialog();
		}

		public void ShowPluginTransfertData(IEnumerable<DataGame> dataGames)
		{
			WindowOptions windowOptions = new WindowOptions
			{
				ShowMinimizeButton = false,
				ShowMaximizeButton = false,
				ShowCloseButton = true,
				Height = 200,
				Width = 1000
			};

			TransfertData ViewExtension = new TransfertData(dataGames.ToList(), PluginDatabase);
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
				ResourceProvider.GetString("LOCCommonSelectTransferData"),
				ViewExtension,
				windowOptions);
			_ = windowExtension.ShowDialog();
		}

		public virtual void ShowPluginDataMismatch()
		{
			throw new System.NotImplementedException();
		}
	}
}