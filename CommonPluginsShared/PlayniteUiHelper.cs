using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Playnite.SDK;

namespace CommonPluginsShared
{
    public class PlayniteUiHelper
    {
        /// <summary>
        /// Handles window closure with Escape key.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Key event arguments</param>
        public static void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && sender is Window window)
            {
                e.Handled = true;
                window.Close();
            }
        }

        /// <summary>
        /// Creates a Playnite extension window with the specified settings.
        /// </summary>
        /// <param name="title">Window title</param>
        /// <param name="viewExtension">User control to display</param>
        /// <param name="windowOptions">Window configuration options</param>
        /// <returns>Configured Window instance</returns>
        public static Window CreateExtensionWindow(string title, UserControl viewExtension, WindowOptions windowOptions = null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title cannot be null or empty.", nameof(title));
            }

            if (viewExtension == null)
            {
                throw new ArgumentNullException(nameof(viewExtension));
            }

            windowOptions = windowOptions ?? GetDefaultWindowOptions();

            Window windowExtension = API.Instance.Dialogs.CreateWindow(windowOptions);
            windowExtension.Title = title;
            windowExtension.ShowInTaskbar = false;
            windowExtension.ResizeMode = windowOptions.CanBeResizable ? ResizeMode.CanResize : ResizeMode.NoResize;
            windowExtension.Owner = API.Instance.Dialogs.GetCurrentAppWindow();
            windowExtension.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            windowExtension.Content = viewExtension;

            ApplyWindowDimensions(windowExtension, viewExtension, windowOptions);
            ApplyWindowConstraints(windowExtension, windowOptions);

            windowExtension.PreviewKeyDown += HandleEsc;

            return windowExtension;
        }

        /// <summary>
        /// Gets default window options configuration.
        /// </summary>
        /// <returns>Default WindowOptions instance</returns>
        private static WindowOptions GetDefaultWindowOptions()
        {
            return new WindowOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true,
                CanBeResizable = false
            };
        }

        /// <summary>
        /// Applies window dimensions independently based on available sources.
        /// Priority: WindowOptions > ViewExtension explicit size > ViewExtension min size > SizeToContent
        /// </summary>
        private static void ApplyWindowDimensions(Window window, UserControl viewExtension, WindowOptions windowOptions)
        {
            bool widthSet = false;
            bool heightSet = false;

            if (windowOptions.Width > 0)
            {
                window.Width = windowOptions.Width;
                widthSet = true;
            }
            else if (!double.IsNaN(viewExtension.Width) && viewExtension.Width > 0)
            {
                window.Width = viewExtension.Width;
                widthSet = true;
            }
            else if (!double.IsNaN(viewExtension.MinWidth) && viewExtension.MinWidth > 0)
            {
                window.Width = viewExtension.MinWidth;
                widthSet = true;
            }

            if (windowOptions.Height > 0)
            {
                window.Height = windowOptions.Height;
                heightSet = true;
            }
            else if (!double.IsNaN(viewExtension.Height) && viewExtension.Height > 0)
            {
                window.Height = viewExtension.Height + 25;
                heightSet = true;
            }
            else if (!double.IsNaN(viewExtension.MinHeight) && viewExtension.MinHeight > 0)
            {
                window.Height = viewExtension.MinHeight + 25;
                heightSet = true;
            }

            if (!widthSet && !heightSet)
            {
                window.SizeToContent = SizeToContent.WidthAndHeight;
            }
            else if (!widthSet)
            {
                window.SizeToContent = SizeToContent.Width;
            }
            else if (!heightSet)
            {
                window.SizeToContent = SizeToContent.Height;
            }
        }

        /// <summary>
        /// Applies window size constraints independently (min/max dimensions).
        /// </summary>
        private static void ApplyWindowConstraints(Window window, WindowOptions windowOptions)
        {
            if (windowOptions.MinWidth > 0)
            {
                window.MinWidth = windowOptions.MinWidth;
            }

            if (windowOptions.MinHeight > 0)
            {
                window.MinHeight = windowOptions.MinHeight;
            }

            if (windowOptions.MaxWidth > 0)
            {
                window.MaxWidth = windowOptions.MaxWidth;
            }

            if (windowOptions.MaxHeight > 0)
            {
                window.MaxHeight = windowOptions.MaxHeight;
            }
        }
    }

    public class WindowOptions : WindowCreationOptions
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double MinWidth { get; set; }
        public double MinHeight { get; set; }
        public double MaxWidth { get; set; }
        public double MaxHeight { get; set; }
        public bool CanBeResizable { get; set; } = false;
    }
}