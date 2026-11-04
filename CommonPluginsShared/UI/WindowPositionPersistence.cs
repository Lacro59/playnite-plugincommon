using CommonPlayniteShared;
using CommonPluginsShared.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace CommonPluginsShared.UI
{
    /// <summary>
    /// Loads and saves extension window geometry into the plugin ExtensionData folder
    /// using a Playnite-compatible <c>windowPositions.json</c> layout.
    /// </summary>
    public static class WindowPositionPersistence
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private static readonly object FileLock = new object();

        /// <summary>
        /// Gets or sets the plugin ExtensionData directory used to store <c>windowPositions.json</c>.
        /// Set once at plugin startup from <c>GetPluginUserDataPath()</c>.
        /// </summary>
        public static string PluginUserDataPath { get; set; }

        /// <summary>
        /// Gets the full path of the window positions file, or <c>null</c> when the data path is unset.
        /// </summary>
        public static string FilePath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PluginUserDataPath))
                {
                    return null;
                }

                return Path.Combine(PluginUserDataPath, PlaynitePaths.WindowPositionsFileName);
            }
        }

        /// <summary>
        /// Returns whether persistence can run for the given options (enabled + non-empty stable key + data path).
        /// </summary>
        /// <param name="windowOptions">Window options from the caller.</param>
        /// <param name="missingKey">True when persistence is enabled but the key is empty.</param>
        /// <returns>True when load/save should proceed.</returns>
        public static bool CanPersist(WindowOptions windowOptions, out bool missingKey)
        {
            missingKey = false;
            if (windowOptions == null || !windowOptions.EnableWindowPersistence)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(windowOptions.WindowPersistenceKey))
            {
                missingKey = true;
                return false;
            }

            if (string.IsNullOrWhiteSpace(PluginUserDataPath))
            {
                Common.LogDebug("[WindowPositionPersistence] PluginUserDataPath is unset; skipping.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to restore geometry for <paramref name="key"/> onto <paramref name="window"/>.
        /// </summary>
        /// <param name="window">Target window.</param>
        /// <param name="key">Stable persistence key.</param>
        /// <param name="workArea">Owner work area used to clamp size and position.</param>
        /// <returns>True when a stored entry was applied.</returns>
        public static bool TryRestore(Window window, string key, Rect workArea)
        {
            if (window == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            WindowPositionEntry entry;
            if (!TryGetEntry(key, out entry) || entry == null)
            {
                Common.LogDebug(string.Format(
                    "[WindowPositionPersistence] No stored entry for key={0}",
                    key));
                return false;
            }

            try
            {
                if (entry.Size != null && entry.Size.X > 0 && entry.Size.Y > 0)
                {
                    window.SizeToContent = SizeToContent.Manual;
                    window.Width = Clamp(entry.Size.X, 100d, workArea.Width > 0 ? workArea.Width : entry.Size.X);
                    window.Height = Clamp(entry.Size.Y, 100d, workArea.Height > 0 ? workArea.Height : entry.Size.Y);
                }

                if (entry.Position != null)
                {
                    window.WindowStartupLocation = WindowStartupLocation.Manual;
                    double width = !double.IsNaN(window.Width) && window.Width > 0 ? window.Width : 400d;
                    double height = !double.IsNaN(window.Height) && window.Height > 0 ? window.Height : 300d;
                    window.Left = ClampPosition(entry.Position.X, workArea.X, workArea.X + workArea.Width, width);
                    window.Top = ClampPosition(entry.Position.Y, workArea.Y, workArea.Y + workArea.Height, height);
                }

                if (Enum.IsDefined(typeof(WindowState), entry.State))
                {
                    WindowState state = (WindowState)entry.State;
                    if (state != WindowState.Minimized)
                    {
                        window.WindowState = state;
                    }
                }

                Common.LogDebug(string.Format(
                    "[WindowPositionPersistence] Restored key={0}, size={1}x{2}, pos={3},{4}, state={5}",
                    key,
                    window.Width,
                    window.Height,
                    window.Left,
                    window.Top,
                    window.WindowState));
                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "[WindowPositionPersistence] Failed to restore window geometry");
                return false;
            }
        }

        /// <summary>
        /// Persists the current geometry of <paramref name="window"/> under <paramref name="key"/>.
        /// </summary>
        /// <param name="window">Window being closed.</param>
        /// <param name="key">Stable persistence key.</param>
        public static void Save(Window window, string key)
        {
            if (window == null || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(FilePath))
            {
                return;
            }

            try
            {
                double left;
                double top;
                double width;
                double height;
                WindowState state = window.WindowState == WindowState.Minimized
                    ? WindowState.Normal
                    : window.WindowState;

                if (window.WindowState == WindowState.Normal)
                {
                    left = window.Left;
                    top = window.Top;
                    width = window.Width;
                    height = window.Height;
                    if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
                    {
                        width = window.ActualWidth;
                    }

                    if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
                    {
                        height = window.ActualHeight;
                    }
                }
                else
                {
                    Rect restoreBounds = window.RestoreBounds;
                    left = restoreBounds.Left;
                    top = restoreBounds.Top;
                    width = restoreBounds.Width;
                    height = restoreBounds.Height;
                }

                if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0
                    || double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
                {
                    Common.LogDebug(string.Format(
                        "[WindowPositionPersistence] Skip save key={0}: invalid size {1}x{2}",
                        key,
                        width,
                        height));
                    return;
                }

                WindowPositionEntry entry = new WindowPositionEntry
                {
                    Position = new WindowPoint { X = left, Y = top },
                    Size = new WindowPoint { X = width, Y = height },
                    State = (int)state
                };

                lock (FileLock)
                {
                    WindowPositionsDocument document = LoadDocument();
                    document.Positions[key] = entry;
                    string directory = Path.GetDirectoryName(FilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllText(FilePath, Serialization.ToJson(document));
                }

                Common.LogDebug(string.Format(
                    "[WindowPositionPersistence] Saved key={0}, size={1}x{2}, pos={3},{4}, state={5}",
                    key,
                    width,
                    height,
                    left,
                    top,
                    state));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "[WindowPositionPersistence] Failed to save window geometry");
            }
        }

        private static bool TryGetEntry(string key, out WindowPositionEntry entry)
        {
            entry = null;
            lock (FileLock)
            {
                WindowPositionsDocument document = LoadDocument();
                return document.Positions != null && document.Positions.TryGetValue(key, out entry);
            }
        }

        private static WindowPositionsDocument LoadDocument()
        {
            string path = FilePath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return new WindowPositionsDocument();
            }

            try
            {
                WindowPositionsDocument document = Serialization.FromJsonFile<WindowPositionsDocument>(path);
                if (document == null)
                {
                    return new WindowPositionsDocument();
                }

                if (document.Positions == null)
                {
                    document.Positions = new Dictionary<string, WindowPositionEntry>();
                }

                return document;
            }
            catch (Exception ex)
            {
                Logger.Warn(string.Format(
                    "[WindowPositionPersistence] Corrupt or unreadable {0}; using empty store. {1}",
                    PlaynitePaths.WindowPositionsFileName,
                    ex.Message));
                return new WindowPositionsDocument();
            }
        }

        private static double Clamp(double value, double min, double max)
        {
            if (max < min)
            {
                return value;
            }

            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        /// <summary>
        /// Keeps at least 40 DIP of the window inside the work area on each axis.
        /// </summary>
        private static double ClampPosition(double value, double areaMin, double areaMax, double size)
        {
            const double margin = 40d;
            double min = areaMin + margin - size;
            double max = areaMax - margin;
            if (max < min)
            {
                return areaMin;
            }

            return Clamp(value, min, max);
        }
    }
}
