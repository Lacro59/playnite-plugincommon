using Playnite.Common;
using Playnite.SDK;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Windows;

namespace PluginCommon
{
    public class Localization
    {
        private static ILogger logger = LogManager.GetLogger();

        public static void SetPluginLanguage(string pluginFolder, string PlayniteConfigurationPath)
        {
            string language = GetPluginLanguageConfiguration(PlayniteConfigurationPath);

            var dictionaries = Application.Current.Resources.MergedDictionaries;

            if (language == "english")
            {
                language = "LocSource";
            }

            var langFile = Path.Combine(pluginFolder, "localization\\" + language + ".xaml");

            logger.Debug($"PluginCommon - Parse plugin localization file {langFile}");

            if (!File.Exists(langFile))
            {
                language = "LocSource";
                langFile = Path.Combine(pluginFolder, "localization\\" + language + ".xaml");
            }

            logger.Debug($"PluginCommon - Parse plugin localization file {langFile}");

            if (File.Exists(langFile))
            {
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
                catch (Exception e)
                {
                    logger.Error(e, $"PluginCommon - Failed to parse localization file {langFile}");
                    return;
                }

                dictionaries.Add(res);
            }
        }

        internal static string GetPluginLanguageConfiguration(string PlayniteConfigurationPath)
        {
            string path = Path.Combine(PlayniteConfigurationPath, "config.json");

            try
            {
                if (File.Exists(path))
                {
                    return ((dynamic)JsonConvert.DeserializeObject(File.ReadAllText(path))).Language;
                }
            }
            catch (Exception e)
            {
                logger.Error(e, $"PluginCommon - Failed to load {path} setting file.");
            }

            return "english";
        }
    }
}

