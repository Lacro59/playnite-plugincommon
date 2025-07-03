using CommonPluginsShared.Collections;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace CommonPluginsShared.Controls
{
    /// <summary>
    /// Extended user control base class that integrates with a plugin database.
    /// Automatically handles data retrieval and UI updates based on the current game context.
    /// </summary>
    public class PluginUserControlExtend : PluginUserControlExtendBase
    {
        /// <summary>
        /// The plugin's database interface for accessing game-specific data.
        /// </summary>
        protected virtual IPluginDatabase pluginDatabase { get; }

        /// <summary>
        /// Sets the control data using the provided game and associated plugin data.
        /// This method should be overridden by derived classes to implement custom logic.
        /// </summary>
        /// <param name="newContext">The current selected game context.</param>
        /// <param name="pluginGameData">The plugin-specific data associated with the game.</param>
        public virtual void SetData(Game newContext, PluginDataBaseGameBase pluginGameData)
        {
        }

        /// <summary>
        /// Updates the control's content asynchronously using game context and plugin data.
        /// </summary>
        public override async Task UpdateDataAsync()
        {
            UpdateDataTimer.Stop();
            Visibility = MustDisplay ? Visibility.Visible : Visibility.Collapsed;

            if (GameContext is null)
            {
                return;
            }

            if (GameContext is null || GameContext.Id != CurrentGame.Id)
            {
                return;
            }

            PluginDataBaseGameBase PluginGameData = pluginDatabase.Get(GameContext, true);
            if (GameContext is null || GameContext.Id != CurrentGame.Id || (!PluginGameData?.HasData ?? true))
            {
                Visibility = AlwaysShow ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            await Application.Current.Dispatcher?.InvokeAsync(
                () => SetData(GameContext, PluginGameData),
                DispatcherPriority.Render
            );
        }
    }
}