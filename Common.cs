using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using PluginCommon.PlayniteResources;
using PluginCommon.PlayniteResources.API;
using PluginCommon.PlayniteResources.Common;
using PluginCommon.PlayniteResources.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PluginCommon
{
    public class Common
    {
        private static ILogger logger = LogManager.GetLogger();


        /// <summary>
        /// Set in application ressources the common ressources.
        /// </summary>
        /// <param name="pluginFolder"></param>
        public static void Load(string pluginFolder)
        {
            List<string> ListCommonFiles = new List<string>
            {
                Path.Combine(pluginFolder, "Resources\\Common.xaml"),
                Path.Combine(pluginFolder, "Resources\\LiveChartsCommon\\Common.xaml")
            };

            foreach (string CommonFile in ListCommonFiles)
            {
                if (File.Exists(CommonFile))
                {
#if DEBUG
                    logger.Debug($"PluginCommon - Load {CommonFile}");
#endif

                    ResourceDictionary res = null;
                    try
                    {
                        res = Xaml.FromFile<ResourceDictionary>(CommonFile);
                        res.Source = new Uri(CommonFile, UriKind.Absolute);

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
                        logger.Error(ex, $"PluginCommon - Failed to parse file {CommonFile}.");
                        return;
                    }

#if DEBUG
                    logger.Debug($"PluginCommon - res: {JsonConvert.SerializeObject(res)}");
#endif

                    Application.Current.Resources.MergedDictionaries.Add(res);
                }
                else
                {
                    logger.Warn($"PluginCommon - File {CommonFile} not found.");
                    return;
                }
            }
        }


        /// <summary>
        /// Normalize log error in Playnite.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="PluginName"></param>
        /// <param name="Message"></param>
        public static void LogError(Exception ex, string PluginName, string Message)
        {
            StackTrace Trace = new StackTrace(ex, true);
            int LineNumber = 0;
            string FileName = "???";
            
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

            logger.Error(ex, $"{PluginName} [{FileName} {LineNumber}] - {Message}\r\n");
        }
    }
}
