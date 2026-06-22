using CommonPluginsShared;
using Playnite.SDK;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsShared.Commands
{
    /// <summary>
    /// Provides static relay commands for window-level operations.
    /// All commands accept a <see cref="FrameworkElement"/> parameter so they can be
    /// declared once and reused from any UserControl without creating per-instance commands.
    /// </summary>
    public static class CommandsWindows
    {
        /// <summary>
        /// Closes the host <see cref="Window"/> of the element passed as CommandParameter.
        /// </summary>
        /// <remarks>
        /// XAML usage:
        /// <code>
        /// Command="{x:Static ui:CommandsWindows.CloseHostWindow}"
        /// CommandParameter="{Binding RelativeSource={RelativeSource AncestorType=UserControl}}"
        /// </code>
        /// </remarks>
        public static RelayCommand<FrameworkElement> CloseHostWindow => new RelayCommand<FrameworkElement>((element) =>
        {
            try
            {
                Window.GetWindow(element)?.Close();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "CloseHostWindow failed");
            }
        });
    }
}