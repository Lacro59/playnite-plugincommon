using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace CommonPluginsShared
{
    /// <summary>
    /// Provides common commands used across plugins (navigation, restart flags, game selection).
    /// </summary>
    public static class Commands
    {
        /// <summary>
        /// Gets a command that opens an URL in the system browser.
        /// </summary>
        /// <remarks>
        /// This member is obsolete. Use <c>CommonPlayniteShared.Commands.NavigateUrlCommand</c> instead.
        /// </remarks>
        [Obsolete("Use CommonPlayniteShared.Commands.NavigateUrlCommand", true)]
        public static RelayCommand<object> NavigateUrl { get; } = new RelayCommand<object>((url) =>
       {
           try
           {
               if (url is string stringUrl)
               {
                   Process.Start(stringUrl);
               }
               else if (url is Uri uriUrl)
               {
                   Process.Start(uriUrl.AbsoluteUri);
               }
               else
               {
                   throw new Exception("Unsupported URL format.");
               }
           }
           catch (Exception ex)
           {
               Common.LogError(ex, false, "Failed to open url.");
           }
       });

        /// <summary>
        /// Gets a command that marks a view model as requiring a Playnite restart.
        /// </summary>
        /// <remarks>
        /// The command expects the originating control as the command parameter so that
        /// the parent window can be resolved and its <c>IsRestartRequired</c> property set to <c>true</c>.
        /// </remarks>
        public static RelayCommand<object> RestartRequired => new RelayCommand<object>((sender) =>
        {
            try
            {
                Window WinParent = UI.FindParent<Window>((FrameworkElement)sender);
                if (WinParent.DataContext?.GetType().GetProperty("IsRestartRequired") != null)
                {
                    ((dynamic)WinParent.DataContext).IsRestartRequired = true;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        });

        /// <summary>
        /// Gets a command that selects a game in the library view and switches the main view to the library.
        /// </summary>
        public static RelayCommand<Guid> GoToGame => new RelayCommand<Guid>((id) =>
       {
           API.Instance.MainView.SelectGame(id);
           API.Instance.MainView.SwitchToLibraryView();
       });
    }
}