using Playnite.SDK;
using System;
using System.IO;
using System.Windows;
using CommonPlayniteShared.Common;
using System.Collections.ObjectModel;
using System.Linq;

namespace CommonPluginsShared
{
    public class PluginLocalization
    {
        private static ILogger Logger => LogManager.GetLogger();

        /// <summary>
        /// Load plugin localization resources from XAML files.
        /// </summary>
        /// <param name="pluginFolder">Plugin directory path</param>
        /// <param name="language">Language code (e.g., "en", "fr")</param>
        /// <param name="defaultLoad">If true, load default localization (used in debug mode)</param>
        internal static void SetPluginLanguage(string pluginFolder, string language, bool defaultLoad = false)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;

#if DEBUG
            // In development mode, force loading LocSource if not defaultLoad
            if (!defaultLoad)
            {
                SetPluginLanguage(pluginFolder, "LocSource", true);
            }

            // Load plugin-specific localization (not "Common") only if defaultLoad is requested
            if (defaultLoad)
            {
                string pluginLangFile = Path.Combine(pluginFolder, $"localization\\{language}.xaml");
                LoadResourceDictionary(pluginLangFile, dictionaries, logAs: "Plugin");
            }
#endif

            // Always try to load common localization
            string commonLangFile = Path.Combine(pluginFolder, $"localization\\Common\\{language}.xaml");
            LoadResourceDictionary(commonLangFile, dictionaries, "Common");
        }

        /// <summary>
        /// Load a localization resource dictionary if file exists and has changed.
        /// </summary>
        /// <param name="filePath">Full path to the XAML localization file</param>
        /// <param name="dictionaries">The merged dictionaries collection to update</param>
        /// <param name="logAs">Prefix to use for logging/tracking resource name</param>
        private static void LoadResourceDictionary(string filePath, Collection<ResourceDictionary> dictionaries, string logAs)
        {
            if (!File.Exists(filePath))
            {
                Logger.Warn($"[{logAs}] Localization file not found: {filePath}");
                return;
            }

            string fileKey = $"{logAs}_{Path.GetFileName(filePath)}";
            DateTime lastKnownDate = ResourceProvider.GetResource(fileKey) as DateTime? ?? default;
            DateTime fileModifiedDate = File.GetLastWriteTime(filePath);

            if (fileModifiedDate <= lastKnownDate)
                return;

            try
            {
                var res = Xaml.FromFile<ResourceDictionary>(filePath);
                res.Source = new Uri(filePath, UriKind.Absolute);

                // Remove empty string entries
                foreach (var key in res.Keys.Cast<object>().ToList())
                {
                    if (res[key] is string s && s.IsNullOrEmpty())
                    {
                        res.Remove(key);
                    }
                }

                // Replace existing dictionary if same source
                var existing = dictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    dictionaries.Remove(existing);
                }

                dictionaries.Add(res);

                Application.Current.Resources.Remove(fileKey);
                Application.Current.Resources.Add(fileKey, fileModifiedDate);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to load {logAs} localization file: {filePath}");
            }
        }
    }
}
