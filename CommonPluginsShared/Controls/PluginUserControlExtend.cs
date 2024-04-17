using CommonPluginsShared.Collections;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace CommonPluginsShared.Controls
{
    public class PluginUserControlExtend : PluginUserControlExtendBase
    {
        internal virtual IPluginDatabase _PluginDatabase { get; }


        public virtual void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
        }

        public override async Task UpdateDataAsync()
        {
            _updateDataTimer.Stop();
            Visibility = MustDisplay ? Visibility.Visible : Visibility.Collapsed;

            if (GameContext is null)
            {
                return;
            }

            Game contextGame = GameContext;
            if (GameContext is null || GameContext.Id != contextGame.Id)
            {
                return;
            }

            PluginDataBaseGameBase PluginGameData = _PluginDatabase.Get(GameContext, true);
            if (GameContext is null || GameContext.Id != contextGame.Id || (!PluginGameData?.HasData ?? true))
            {
                Visibility = AlwaysShow ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            await Task.Run(() => Application.Current.Dispatcher?.Invoke(() => SetData(GameContext, PluginGameData), DispatcherPriority.Render));
        }
    }
}
