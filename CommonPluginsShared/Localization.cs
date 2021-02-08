using Playnite.SDK;
using System;
using System.IO;
using System.Windows;
using CommonPluginsPlaynite.Common;

namespace CommonPluginsShared
{
    public class PluginLocalization
    {
        private static ILogger logger = LogManager.GetLogger();


        /// <summary>
        /// Load common localization.
        /// </summary>
        /// <param name="pluginFolder"></param>
        /// <param name="language"></param>
        /// <param name="DefaultLoad"></param>
        internal static void SetPluginLanguage(string pluginFolder, string language, bool DefaultLoad = false)
        {
            // Load default for missing
            if (!DefaultLoad)
            {
                SetPluginLanguage(pluginFolder, "LocSource", true);
            }


            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var langFile = Path.Combine(pluginFolder, "localization\\" + language + ".xaml");
            var langFileCommon = Path.Combine(pluginFolder, "localization\\Common\\" + language + ".xaml");

            // Load common localization 
            if (File.Exists(langFileCommon))
            {
                ResourceDictionary res = null;
                try
                {
                    res = Xaml.FromFile<ResourceDictionary>(langFileCommon);
                    res.Source = new Uri(langFileCommon, UriKind.Absolute);

                    foreach (var key in res.Keys)
                    {
                        if (res[key] is string locString && locString.IsNullOrEmpty())
                        {
                            res.Remove(key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"CommonPluginsShared - Failed to integrate localization file {langFileCommon}.");
                    return;
                }

                dictionaries.Add(res);
            }
            else
            {
                logger.Warn($"CommonPluginsShared - File {langFileCommon} not found.");
            }
        }
    }
}
