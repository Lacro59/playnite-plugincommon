using Newtonsoft.Json;
using Playnite.SDK;
using CommonPluginsPlaynite.Common;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommonPluginsShared.Models;
using System.Windows.Automation;
using System.Windows.Media;

namespace CommonPluginsShared
{
    public class Common
    {
        private static ILogger logger = LogManager.GetLogger();


        /// <summary>
        /// Load the common ressources.
        /// </summary>
        /// <param name="pluginFolder"></param>
        public static void Load(string pluginFolder, string language)
        {
            #region Common localization
            PluginLocalization.SetPluginLanguage(pluginFolder, language);
            #endregion

            #region Common xaml
            List<string> ListCommonFiles = new List<string>
            {
                Path.Combine(pluginFolder, "Resources\\Common.xaml"),
                Path.Combine(pluginFolder, "Resources\\LiveChartsCommon\\Common.xaml")
            };

            foreach (string CommonFile in ListCommonFiles)
            {
                if (File.Exists(CommonFile))
                {
                    Common.LogDebug(true, $"Load {CommonFile}");

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
                        LogError(ex, false, $"Failed to integrate file {CommonFile}");
                        return;
                    }

                    Common.LogDebug(true, $"res: {JsonConvert.SerializeObject(res)}");

                    Application.Current.Resources.MergedDictionaries.Add(res);
                }
                else
                {
                    logger.Warn($"File {CommonFile} not find");
                    return;
                }
            }
            #endregion

            #region Common font
            string FontFile = Path.Combine(pluginFolder, "Resources\\font.ttf");
            if (File.Exists(FontFile))
            {
                long fileSize = 0;
                if (Application.Current.Resources.FindName("CommonFontSize") != null)
                {
                    fileSize = (long)Application.Current.Resources.FindName("CommonFontSize");
                }

                // Load only the newest
                if (fileSize <= new FileInfo(FontFile).Length)
                {
                    Application.Current.Resources.Remove("CommonFontSize");
                    Application.Current.Resources.Add("CommonFontSize", new FileInfo(FontFile).Length);

                    FontFamily fontFamily = new FontFamily(new Uri(FontFile), "./#font");
                    Application.Current.Resources.Remove("CommonFont");
                    Application.Current.Resources.Add("CommonFont", fontFamily);
                }
            }
            else
            {
                logger.Warn($"File {FontFile} not find");
            }
            #endregion
        }


        public static void SetEvent(IPlayniteAPI PlayniteAPI)
        {
            if (PlayniteAPI.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(WindowBase_LoadedEvent));
            }
        }

        private static void WindowBase_LoadedEvent(object sender, System.EventArgs e)
        {
            string WinIdProperty = string.Empty;

            try
            {
                WinIdProperty = ((Window)sender).GetValue(AutomationProperties.AutomationIdProperty).ToString();

                if (WinIdProperty == "WindowSettings")
                {
                    ((Window)sender).Width = 860;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on WindowBase_LoadedEvent for {WinIdProperty}");
            }
        }




        #region Logs
        public static void LogDebug(bool IsIgnored, string Message)
        {
            if (IsIgnored)
            {
                Message = $"[Ignored] {Message}";
            }

#if DEBUG
            logger.Debug(Message);
#else
            if (!IsIgnored) 
            {            
                logger.Debug(Message); 
            }
#endif
        }

        public static void LogError(Exception ex, bool IsIgnored)
        {
            TraceInfos traceInfos = new TraceInfos(ex);
            string Message = string.Empty;

            if (IsIgnored)
            {
                Message = $"[Ignored] ";
            }

            Message += $"[{traceInfos.FileName} {traceInfos.LineNumber}]";

            if (!traceInfos.InitialCaller.IsNullOrEmpty())
            {
                Message += $" - Error on {traceInfos.InitialCaller}()";
            }

#if DEBUG
            logger.Error(ex, $"{Message}");
#else
            if (!IsIgnored) 
            {
                logger.Error(ex, $"{Message}");
            }
#endif
        }

        public static void LogError(Exception ex, bool IsIgnored, string Message)
        {
            TraceInfos traceInfos = new TraceInfos(ex);
            Message = $"[{traceInfos.FileName} {traceInfos.LineNumber}] - {Message}";

            if (IsIgnored)
            {
                Message = $"[Ignored] {Message}";
            }

#if DEBUG
            logger.Error(ex, $"{Message}");
#else
            if (!IsIgnored) 
            {
                logger.Error(ex, $"{Message}");
            }
#endif
        }
        #endregion


        public static string NormalizeGameName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var newName = name.ToLower();
            newName = newName.RemoveTrademarks();
            newName = newName.Replace("_", "");
            newName = newName.Replace(".", "");
            newName = newName.Replace('’', '\'');
            newName = newName.Replace(":", "");
            newName = newName.Replace("-", "");
            newName = newName.Replace("goty", "");
            newName = newName.Replace("game of the year edition", "");
            newName = newName.Replace("  ", " ");

            return newName.Trim();
        }
    }
}
