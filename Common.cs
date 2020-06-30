using Playnite.Common;
using Playnite.SDK;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PluginCommon
{
    public class Common
    {
        private static ILogger logger = LogManager.GetLogger();


        /// <summary>
        /// Set in application ressources the common ressource.
        /// </summary>
        /// <param name="pluginFolder"></param>
        public static void Load(string pluginFolder)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;

            var LiveChartsCommonFile = Path.Combine(pluginFolder, "Resources\\LiveChartsCommon\\Common.xaml");
            if (File.Exists(LiveChartsCommonFile))
            {
                ResourceDictionary res = null;
                try
                {
                    res = Xaml.FromFile<ResourceDictionary>(LiveChartsCommonFile);
                    res.Source = new Uri(LiveChartsCommonFile, UriKind.Absolute);

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
                    logger.Error(ex, $"PluginCommon - Failed to parse file {LiveChartsCommonFile}.");
                    return;
                }

                dictionaries.Add(res);
            }
            else
            {
                logger.Error($"PluginCommon - File {LiveChartsCommonFile} not found.");
                return;
            }
        }


        public static void LogError(Exception ex, string PluginName, string Message)
        {
            StackTrace Trace = new StackTrace(ex, true);
            int LineNumber = 0;
            string FileName = "";
            
            foreach (var frame in Trace.GetFrames())
            {
                if (!string.IsNullOrEmpty(frame.GetFileName()) && (string.IsNullOrEmpty(FileName)))
                {
                    FileName = frame.GetFileName();
                }
                if ((frame.GetFileLineNumber() > 0) && (LineNumber == 0))
                {
                    LineNumber = frame.GetFileLineNumber();
                }
            }

            logger.Error(ex, $"{PluginName} [{FileName} {LineNumber}] - {Message} ");
        }


    }
}
