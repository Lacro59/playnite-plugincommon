using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace CommonPluginsShared
{
    public static class Commands
    {
        private static ILogger logger = LogManager.GetLogger();

        public static RelayCommand<object> RestartRequired
        {
            get => new RelayCommand<object>((sender) =>
            {
                try
                {
                    var WinParent = UI.FindParent<Window>((FrameworkElement)sender);

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
        }
    }
}
