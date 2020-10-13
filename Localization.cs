using Playnite.SDK;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Windows;
using PluginCommon.PlayniteResources.Common;

namespace PluginCommon
{
    public class Localization
    {
        private static ILogger logger = LogManager.GetLogger();


        public static void SetPluginLanguage(string pluginFolder, string language, bool DefaultLoad = false)
        {
            // Load default for missing
            if (!DefaultLoad)
            {
                SetPluginLanguage(pluginFolder, "LocSource", true);
            }


            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var langFile = Path.Combine(pluginFolder, "localization\\" + language + ".xaml");

            // Load localization
            if (File.Exists(langFile))
            {
#if DEBUG
                logger.Debug($"PluginCommon - Parse plugin localization file {langFile}.");
#endif

                ResourceDictionary res = null;
                try
                {
                    res = Xaml.FromFile<ResourceDictionary>(langFile);
                    res.Source = new Uri(langFile, UriKind.Absolute);

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
                    logger.Error(ex, $"PluginCommon - Failed to parse localization file {langFile}.");
                    return;
                }

                dictionaries.Add(res);
            }
            else
            {
                logger.Warn($"PluginCommon - File {langFile} not found.");
            }
        }

        /// <summary>
        /// Get language defined in Playnite settings; default "english".
        /// </summary>
        /// <param name="PlayniteConfigurationPath"></param>
        /// <returns></returns>
        [Obsolete("Use PlayniteApi.ApplicationSettings.Language")]
        public static string GetPlayniteLanguageConfiguration(string PlayniteConfigurationPath)
        {
            string path = Path.Combine(PlayniteConfigurationPath, "config.json");

            try
            {
                if (File.Exists(path))
                {
                    return ((dynamic)JsonConvert.DeserializeObject(File.ReadAllText(path))).Language;
                }
                else
                {
                    logger.Warn($"PluginCommon - Not find config file {path}");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"PluginCommon - Failed to load config file {path}");
            }

            return "english";
        }
    }
}
