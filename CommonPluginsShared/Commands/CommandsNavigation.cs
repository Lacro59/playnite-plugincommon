using CommonPlayniteShared.Commands;
using CommonPluginsShared;
using Playnite.SDK;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsShared.Commands
{
    /// <summary>
    /// Provides static relay commands for UI navigation within Playnite.
    /// </summary>
    public static class CommandsNavigation
    {
        /// <summary>Opens a URL in the default browser.</summary>
        public static RelayCommand<object> NavigateUrl => GlobalCommands.NavigateUrlCommand;

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