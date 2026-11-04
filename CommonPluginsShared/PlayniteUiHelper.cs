using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using CommonPluginsShared.UI;
using Playnite.SDK;

namespace CommonPluginsShared
{
    /// <summary>
    /// Helpers for creating and configuring Playnite extension windows.
    /// </summary>
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
        /// When <see cref="WindowOptions.EnableWindowPersistence"/> is true and a
        /// <see cref="WindowOptions.WindowPersistenceKey"/> is set, size/position/state
        /// are restored from and saved to the plugin ExtensionData <c>windowPositions.json</c>.
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

            ApplyWindowDimensions(windowExtension, viewExtension, windowOptions, windowExtension.Owner);
            ApplyWindowConstraints(windowExtension, windowOptions);
            AttachWindowPersistence(windowExtension, windowOptions);

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
        private static void ApplyWindowDimensions(Window window, UserControl viewExtension, WindowOptions windowOptions, Window owner)
        {
            bool widthSet = false;
            bool heightSet = false;

            Rect workArea = GetOwnerWorkArea(owner);
            double screenWidth = workArea.Width;
            double screenHeight = workArea.Height;

            // ----- WIDTH -----
            if (windowOptions.Width > 0)
            {
                window.Width = windowOptions.Width;
                widthSet = true;
            }
            else if (windowOptions.WidthPercent > 0)
            {
                window.Width = screenWidth * (windowOptions.WidthPercent / 100d);
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

            // ----- HEIGHT -----
            if (windowOptions.Height > 0)
            {
                window.Height = windowOptions.Height;
                heightSet = true;
            }
            else if (windowOptions.HeightPercent > 0)
            {
                window.Height = screenHeight * (windowOptions.HeightPercent / 100d);
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

            // ----- SizeToContent fallback -----
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
        /// Gets work area bounds for the monitor containing the owner window.
        /// Returns dimensions in WPF DIPs to keep percent sizing DPI-aware.
        /// </summary>
        private static Rect GetOwnerWorkArea(Window owner)
        {
            Rect defaultWorkArea = SystemParameters.WorkArea;
            if (owner == null)
            {
                return defaultWorkArea;
            }

            IntPtr ownerHandle = new WindowInteropHelper(owner).Handle;
            if (ownerHandle == IntPtr.Zero)
            {
                return defaultWorkArea;
            }

            System.Windows.Forms.Screen ownerScreen = System.Windows.Forms.Screen.FromHandle(ownerHandle);
            Rectangle workingAreaPixels = ownerScreen.WorkingArea;

            PresentationSource ownerSource = PresentationSource.FromVisual(owner);
            if (ownerSource?.CompositionTarget == null)
            {
                return defaultWorkArea;
            }

            System.Windows.Point topLeftDip = ownerSource.CompositionTarget.TransformFromDevice.Transform(new System.Windows.Point(workingAreaPixels.Left, workingAreaPixels.Top));
            System.Windows.Point bottomRightDip = ownerSource.CompositionTarget.TransformFromDevice.Transform(new System.Windows.Point(workingAreaPixels.Right, workingAreaPixels.Bottom));

            return new Rect(topLeftDip, bottomRightDip);
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

        /// <summary>
        /// Restores persisted geometry when enabled and attaches save-on-close.
        /// </summary>
        private static void AttachWindowPersistence(Window window, WindowOptions windowOptions)
        {
            bool missingKey;
            if (!WindowPositionPersistence.CanPersist(windowOptions, out missingKey))
            {
                if (missingKey)
                {
                    Common.LogDebug(string.Format(
                        "[PlayniteUiHelper] Window persistence enabled but WindowPersistenceKey is empty (title=\"{0}\"); skipping.",
                        window.Title));
                }

                return;
            }

            string key = windowOptions.WindowPersistenceKey.Trim();
            Rect workArea = GetOwnerWorkArea(window.Owner);
            WindowPositionPersistence.TryRestore(window, key, workArea);

            window.Closed += (sender, args) =>
            {
                Window closedWindow = sender as Window;
                if (closedWindow != null)
                {
                    WindowPositionPersistence.Save(closedWindow, key);
                }
            };
        }
    }

    /// <summary>
    /// Extended window creation options for plugin dialogs.
    /// </summary>
    public class WindowOptions : WindowCreationOptions
    {
        /// <summary>
        /// Gets or sets the fixed window width in DIPs when greater than zero.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Gets or sets the window width as a percentage of the owner work area.
        /// </summary>
        public double WidthPercent { get; set; }

        /// <summary>
        /// Gets or sets the fixed window height in DIPs when greater than zero.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Gets or sets the window height as a percentage of the owner work area.
        /// </summary>
        public double HeightPercent { get; set; }

        /// <summary>
        /// Gets or sets the minimum window width.
        /// </summary>
        public double MinWidth { get; set; }

        /// <summary>
        /// Gets or sets the minimum window height.
        /// </summary>
        public double MinHeight { get; set; }

        /// <summary>
        /// Gets or sets the maximum window width.
        /// </summary>
        public double MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the maximum window height.
        /// </summary>
        public double MaxHeight { get; set; }

        /// <summary>
        /// Gets or sets whether the window can be resized by the user.
        /// </summary>
        public bool CanBeResizable { get; set; } = false;

        /// <summary>
        /// Gets or sets whether size, position and state are persisted under the plugin ExtensionData folder.
        /// Defaults to <c>true</c>; set to <c>false</c> for one-shot dialogs.
        /// Persistence still requires a non-empty <see cref="WindowPersistenceKey"/>.
        /// </summary>
        public bool EnableWindowPersistence { get; set; } = true;

        /// <summary>
        /// Gets or sets a stable, non-localized key used as the JSON entry id (for example <c>GameActivity.GameView</c>).
        /// Required when <see cref="EnableWindowPersistence"/> is <c>true</c>.
        /// </summary>
        public string WindowPersistenceKey { get; set; }
    }
}
