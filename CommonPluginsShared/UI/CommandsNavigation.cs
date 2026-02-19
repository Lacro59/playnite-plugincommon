using CommonPluginsShared;
using Playnite.SDK;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsShared.UI
{
    /// <summary>
    /// Provides static relay commands for UI navigation within Playnite.
    /// </summary>
    public static class CommandsNavigation
    {
        /// <summary>
        /// Marks the parent window's view model as requiring a Playnite restart.
        /// Sets the <c>IsRestartRequired</c> property on the window's DataContext via reflection.
        /// </summary>
        /// <remarks>
        /// The command parameter must be the originating <see cref="FrameworkElement"/>.
        /// </remarks>
        public static RelayCommand<FrameworkElement> RestartRequired => new RelayCommand<FrameworkElement>((sender) =>
        {
            try
            {
                Window parentWindow = UIHelper.FindParent<Window>(sender);
                if (parentWindow?.DataContext?.GetType().GetProperty("IsRestartRequired") != null)
                {
                    ((dynamic)parentWindow.DataContext).IsRestartRequired = true;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "Failed to set restart required flag");
            }
        });

        /// <summary>
        /// Selects a game in the Playnite library view by its ID and switches to library view.
        /// </summary>
        /// <remarks>
        /// The command parameter must be the target game's <see cref="Guid"/>.
        /// </remarks>
        public static RelayCommand<Guid> GoToGame => new RelayCommand<Guid>((id) =>
        {
            API.Instance.MainView.SelectGame(id);
            API.Instance.MainView.SwitchToLibraryView();
        });
    }
}