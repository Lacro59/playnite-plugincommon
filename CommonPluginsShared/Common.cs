using CommonPlayniteShared.Common;
using CommonPluginsShared.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CommonPluginsShared
{
    public class Common
    {
        private static ILogger Logger => LogManager.GetLogger();

        /// <summary>
        /// Load common resources (localization, styles, fonts) from the plugin directory.
        /// </summary>
        /// <param name="pluginFolder">Path to the plugin folder</param>
        /// <param name="language">Current language code</param>
        public static void Load(string pluginFolder, string language)
        {
            // Load localization
            PluginLocalization.SetPluginLanguage(pluginFolder, language);

            LoadResourceDictionaries(pluginFolder);
            LoadFontResource(pluginFolder);
        }

        /// <summary>
        /// Load and merge XAML resource dictionaries if they have been modified since the last load.
        /// </summary>
        private static void LoadResourceDictionaries(string pluginFolder)
        {
            var resourceFiles = new List<string>
            {
                Path.Combine(pluginFolder, "Resources\\Common.xaml"),
                Path.Combine(pluginFolder, "Resources\\LiveChartsCommon\\Common.xaml"),
                Path.Combine(pluginFolder, "Resources\\Controls\\ListExtendStyle.xaml")
            };

            foreach (var file in resourceFiles)
            {
                if (!File.Exists(file))
                {
                    Logger.Warn($"File {file} not found");
                    return;
                }

                string fileName = Path.GetFileName(file);
                string folderName = Path.GetFileName(Path.GetDirectoryName(file));
                string resourceKey = $"{folderName}_{fileName}";
                DateTime lastModified = File.GetLastWriteTime(file);
                DateTime lastDate = ResourceProvider.GetResource(resourceKey) as DateTime? ?? default;

                if (lastModified <= lastDate)
                    continue;

                try
                {
                    var dictionary = Xaml.FromFile<ResourceDictionary>(file);
                    dictionary.Source = new Uri(file, UriKind.Absolute);

                    // Remove empty strings (probably from localization bindings)
                    foreach (var key in dictionary.Keys.Cast<object>().ToList())
                    {
                        if (dictionary[key] is string s && s.IsNullOrEmpty())
                        {
                            dictionary.Remove(key);
                        }
                    }

                    // Track last modified time
                    Application.Current.Resources.Remove(resourceKey);
                    Application.Current.Resources.Add(resourceKey, lastModified);

                    Application.Current.Resources.MergedDictionaries.Add(dictionary);
                    LogDebug(true, $"Loaded resource {file} - {lastModified:yyyy-MM-dd HH:mm:ss}");
                }
                catch (Exception ex)
                {
                    LogError(ex, false, $"Failed to load resource dictionary: {file}");
                }
            }
        }

        /// <summary>
        /// Load a custom font from the plugin folder if it is newer or not yet loaded.
        /// </summary>
        private static void LoadFontResource(string pluginFolder)
        {
            var fontPath = Path.Combine(pluginFolder, "Resources\\font.ttf");
            if (!File.Exists(fontPath))
            {
                Logger.Warn($"Font file {fontPath} not found");
                return;
            }

            long existingSize = ResourceProvider.GetResource("CommonFontSize") as long? ?? 0;
            long newSize = new FileInfo(fontPath).Length;

            if (newSize <= existingSize)
                return;

            Application.Current.Resources.Remove("CommonFontSize");
            Application.Current.Resources.Add("CommonFontSize", newSize);

            var fontFamily = new FontFamily(new Uri(fontPath), "./#font");
            Application.Current.Resources.Remove("CommonFont");
            Application.Current.Resources.Add("CommonFont", fontFamily);
        }

        #region Logging

        /// <summary>
        /// Write a debug message (conditionally ignored in non-debug mode).
        /// </summary>
        public static void LogDebug(bool isIgnored, string message)
        {
            if (isIgnored)
            {
                message = $"[Ignored] {message}";
            }

#if DEBUG
            Logger.Debug(message);
#else
            if (!isIgnored)
            {
                Logger.Debug(message);
            }
#endif
        }

        /// <summary>
        /// Log error with various overloads.
        /// </summary>
        public static void LogError(Exception ex, bool isIgnored) =>
            LogError(ex, isIgnored, string.Empty, false, string.Empty, string.Empty);

        public static void LogError(Exception ex, bool isIgnored, string message) =>
            LogError(ex, isIgnored, message, false, string.Empty, string.Empty);

        public static void LogError(Exception ex, bool isIgnored, bool showNotification, string pluginName) =>
            LogError(ex, isIgnored, string.Empty, showNotification, pluginName, string.Empty);

        public static void LogError(Exception ex, bool isIgnored, bool showNotification, string pluginName, string notificationMessage) =>
            LogError(ex, isIgnored, string.Empty, showNotification, pluginName, notificationMessage);

        public static void LogError(Exception ex, bool isIgnored, string message, bool showNotification, string pluginName) =>
            LogError(ex, isIgnored, message, showNotification, pluginName, string.Empty);

        /// <summary>
        /// Main error logging handler.
        /// </summary>
        public static void LogError(Exception ex, bool isIgnored, string message, bool showNotification, string pluginName, string notificationMessage)
        {
            var trace = new TraceInfos(ex);

            if (message.IsNullOrEmpty())
            {
                message = !trace.InitialCaller.IsNullOrEmpty() ? $"Error on {trace.InitialCaller}()" : "Error on ???";
            }
            else if (!trace.InitialCaller.IsNullOrEmpty())
            {
                message = $"Error on {trace.InitialCaller}(): {message}";
            }

            if (isIgnored)
            {
                message = $"[Ignored] {message}";
            }

            message = $"{message}|{trace.FileName}|{trace.LineNumber}";

#if DEBUG
            Logger.Error(ex, message);
#else
            if (!isIgnored)
            {
                Logger.Error(ex, message);
            }
#endif

            if (showNotification && !isIgnored)
            {
                API.Instance.Notifications.Add(new NotificationMessage(
                    $"{pluginName}-{Guid.NewGuid()}",
                    $"{pluginName}{Environment.NewLine}{(notificationMessage.IsNullOrEmpty() ? ex.Message : notificationMessage)}",
                    NotificationType.Error,
                    () => PlayniteTools.CreateLogPackage(pluginName)
                ));
            }
        }

        #endregion

        public static void AddTextIcoFontResource(string key, string text)
        {
            if (Application.Current.Resources.Contains(key))
            {
                return;
            }

            Application.Current.Resources.Add(key, new TextBlock
            {
                Text = text,
                FontSize = 16,
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
            });
        }

        public static void AddTextIcoFontResource(Dictionary<string, string> iconsResourcesToAdd)
        {
            foreach (var iconResource in iconsResourcesToAdd)
            {
                AddTextIcoFontResource(iconResource.Key, iconResource.Value);
            }
        }
    }
}