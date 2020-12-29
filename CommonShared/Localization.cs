using Playnite.SDK;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Windows;
using CommonPlaynite.Common;

namespace CommonShared
{
    public class PluginLocalization
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
            var langFileCommon = Path.Combine(pluginFolder, "localization\\Common\\" + language + ".xaml");


            // Load localization common
            if (File.Exists(langFileCommon))
            {
#if DEBUG
                logger.Debug($"CommonShared - Parse plugin localization file {langFileCommon}.");
#endif

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
                    logger.Error(ex, $"CommonShared - Failed to parse localization file {langFileCommon}.");
                    return;
                }

                dictionaries.Add(res);
            }
            else
            {
                logger.Warn($"CommonShared - File {langFileCommon} not found.");
            }


            // Load localization
            if (File.Exists(langFile))
            {
#if DEBUG
                logger.Debug($"CommonShared - Parse plugin localization file {langFile}.");
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
                    logger.Error(ex, $"CommonShared - Failed to parse localization file {langFile}.");
                    return;
                }

                dictionaries.Add(res);
            }
            else
            {
                logger.Warn($"CommonShared - File {langFile} not found.");
            }
        }
    }
}
