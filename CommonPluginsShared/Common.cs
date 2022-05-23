using Playnite.SDK;
using CommonPlayniteShared.Common;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using CommonPluginsShared.Models;
using System.Windows.Automation;
using System.Windows.Media;
using Playnite.SDK.Data;

namespace CommonPluginsShared
{
    public class Common
    {
        private static ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();


        /// <summary>
        /// Load the common ressources
        /// </summary>
        /// <param name="pluginFolder"></param>
        public static void Load(string pluginFolder, string language)
        {
            // Common localization
            PluginLocalization.SetPluginLanguage(pluginFolder, language);

            #region Common xaml
            List<string> ListCommonFiles = new List<string>
            {
                Path.Combine(pluginFolder, "Resources\\Common.xaml"),
                Path.Combine(pluginFolder, "Resources\\LiveChartsCommon\\Common.xaml"),
                Path.Combine(pluginFolder, "Resources\\Controls\\ListExtendStyle.xaml")
            };

            foreach (string CommonFile in ListCommonFiles)
            {
                if (File.Exists(CommonFile))
                {
                    DateTime LastDate = default;
                    string FileName = Path.GetFileName(CommonFile);
                    if (resources.GetResource(FileName) != null)
                    {
                        LastDate = (DateTime)resources.GetResource(FileName);
                    }

                    DateTime lastModified = File.GetLastWriteTime(CommonFile);
                    if (lastModified > LastDate)
                    {
                        Application.Current.Resources.Remove(FileName);
                        Application.Current.Resources.Add(FileName, lastModified);

                        Common.LogDebug(true, $"Load {CommonFile} - {lastModified.ToString("yyyy-MM-dd HH:mm:ss")}");

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

                        Common.LogDebug(true, $"res: {Serialization.ToJson(res)}");

                        Application.Current.Resources.MergedDictionaries.Add(res);
                    }
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
                if (resources.GetResource("CommonFontSize") != null)
                {
                    fileSize = (long)resources.GetResource("CommonFontSize");
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


        #region Common event
        /// <summary>
        /// Load common event
        /// </summary>
        /// <param name="PlayniteAPI"></param>
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
            string WinName = string.Empty;

            try
            {
                WinIdProperty = ((Window)sender).GetValue(AutomationProperties.AutomationIdProperty).ToString();
                WinName = ((Window)sender).Name;

                if (WinIdProperty == "WindowSettings")
                {
                    ((Window)sender).Width = 860;
                }
                else if (WinIdProperty == "WindowExtensions")
                {
                    ((Window)sender).Height = 700;
                }
                else if (((Window)sender).DataContext != null)
                {
                    if (((Window)sender).DataContext.GetType().GetProperty("SettingsView") != null
                        && (((dynamic)(Window)sender).DataContext).SettingsView.DataContext is ISettings)
                    {
                        ((Window)sender).Width = 700;
                        ((Window)sender).Height = 500;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on WindowBase_LoadedEvent for {WinName} - {WinIdProperty}");
            }
        }
        #endregion


        #region Logs
        /// <summary>
        /// Debug log with ignore when no debug mode
        /// </summary>
        /// <param name="IsIgnored"></param>
        /// <param name="Message"></param>
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
            LogError(ex, IsIgnored, string.Empty, false, string.Empty, string.Empty);
        }

        public static void LogError(Exception ex, bool IsIgnored, string Message)
        {
            LogError(ex, IsIgnored, Message, false, string.Empty, string.Empty);
        }

        public static void LogError(Exception ex, bool IsIgnored, bool ShowNotification, string PluginName)
        {
            LogError(ex, IsIgnored, string.Empty, ShowNotification, PluginName, string.Empty);
        }

        public static void LogError(Exception ex, bool IsIgnored, bool ShowNotification, string PluginName, string NotificationMessage)
        {
            LogError(ex, IsIgnored, string.Empty, ShowNotification, PluginName, NotificationMessage);
        }

        public static void LogError(Exception ex, bool IsIgnored, string Message, bool ShowNotification, string PluginName)
        {
            LogError(ex, IsIgnored, string.Empty, ShowNotification, PluginName, string.Empty);
        }

        public static void LogError(Exception ex, bool IsIgnored, string Message, bool ShowNotification, string PluginName, string NotificationMessage)
        {
            TraceInfos traceInfos = new TraceInfos(ex);

            if (Message.IsNullOrEmpty())
            {
                if (!traceInfos.InitialCaller.IsNullOrEmpty())
                {
                    Message += $"Error on {traceInfos.InitialCaller}()";
                }
            }

            if (IsIgnored)
            {
                Message = $"[Ignored] {Message}";
            }
            Message = $"{Message}|{traceInfos.FileName}|{traceInfos.LineNumber}";

#if DEBUG
            logger.Error(ex, $"{Message}");
#else
            if (!IsIgnored) 
            {
                logger.Error(ex, $"{Message}");
            }
#endif

            if (ShowNotification)
            {
                API.Instance.Notifications.Add(new NotificationMessage(
                     $"{PluginName}-{new Guid()}",
                     $"{PluginName}" + System.Environment.NewLine + (NotificationMessage.IsNullOrEmpty() ? $"{ex.Message}" : NotificationMessage),
                     NotificationType.Error,
                     () => PlayniteTools.CreateLogPackage(PluginName)
                 ));
            }
        }
        #endregion
    }
}
