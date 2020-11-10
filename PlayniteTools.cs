using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon.PlayniteResources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginCommon
{
    public class PlayniteTools
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private static List<Emulator> ListEmulators = null;
        private static JArray DisabledPlugins = null;


        #region Emulators
        public static List<Emulator> GetListEmulators(IPlayniteAPI PlayniteApi)
        {
            if (ListEmulators == null)
            {
                ListEmulators = new List<Emulator>();
                foreach (Emulator item in PlayniteApi.Database.Emulators)
                {
                    ListEmulators.Add(item);
                }
            }

            return ListEmulators;
        }

        public static bool IsGameEmulated(IPlayniteAPI PlayniteApi, Game game)
        {
            List<Emulator> ListEmulators = GetListEmulators(PlayniteApi);
            return game.PlayAction != null && game.PlayAction.EmulatorId != null && ListEmulators.FindAll(x => x.Id == game.PlayAction.EmulatorId).Count > 0;
        }
        #endregion


        public static string GetCacheFile(string ImageFileName, string PluginName)
        {
            try
            {
                if (!Directory.Exists(Path.Combine(PlaynitePaths.ImagesCachePath, PluginName.ToLower())))
                {
                    Directory.CreateDirectory(Path.Combine(PlaynitePaths.ImagesCachePath, PluginName.ToLower()));
                }
                
                string PathImageFileName = Path.Combine(PlaynitePaths.ImagesCachePath, PluginName.ToLower(), ImageFileName);

                if (File.Exists(PathImageFileName + ".png"))
                {
                    return PathImageFileName + ".png";
                }
#if DEBUG
                logger.Debug($"PluginCommon - GetCacheFile() not find - {PathImageFileName}");
#endif
            }
            catch(Exception ex)
            {
                Common.LogError(ex, "PluginCommon", "Error on GetCacheFile()");
            }

            return string.Empty;
        }


        public static bool IsDisabledPlaynitePlugins(string PluginName, string ConfigurationPath)
        {            
            JObject PlayniteConfig = new JObject();
            try
            {
                if (DisabledPlugins == null)
                {
                    string FileConfig = ConfigurationPath + "\\config.json";
                    if (File.Exists(FileConfig))
                    {
                        PlayniteConfig = JObject.Parse(File.ReadAllText(FileConfig));
                        DisabledPlugins = (JArray)PlayniteConfig["DisabledPlugins"];
                    }
                    else
                    {
                        logger.Warn($"PluginCommon - File not found {FileConfig}");
                        return false;
                    }
                }

                if (DisabledPlugins != null)
                {
                    foreach (string name in DisabledPlugins)
                    {
                        if (name.ToLower() == PluginName.ToLower())
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    logger.Warn($"PluginCommon - DisabledPlugins is null");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", "Error on IsDisabledPlaynitePlugins()");
                return false;
            }

            return false;
        }


        public static string GetSourceName(Game game, IPlayniteAPI PlayniteApi)
        {
            string SourceName = string.Empty;

            if (IsGameEmulated(PlayniteApi, game))
            {
                SourceName = "RetroAchievements";
            }
            else if (game.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
            {
                SourceName = PlayniteApi.Database.Sources.Get(game.SourceId).Name;    
            }
            else
            {
                SourceName = "Playnite";
            }

            return SourceName;
        }


        public static void SetThemeInformation(IPlayniteAPI PlayniteApi)
        {
            string defaultThemeName = "Default";
            ThemeManifest defaultTheme = new ThemeManifest()
            {
                DirectoryName = defaultThemeName,
                DirectoryPath = Path.Combine(PlaynitePaths.ThemesProgramPath, ThemeManager.GetThemeRootDir(ApplicationMode.Desktop), defaultThemeName),
                Name = defaultThemeName
            };
            ThemeManager.SetDefaultTheme(defaultTheme);

            ThemeManifest customTheme = null;
            var theme = PlayniteApi.ApplicationSettings.DesktopTheme;
            if (theme != ThemeManager.DefaultTheme.Name)
            {
                customTheme = ThemeManager.GetAvailableThemes(ApplicationMode.Desktop).SingleOrDefault(a => a.DirectoryName == theme);
                if (customTheme == null)
                {
                    ThemeManager.SetCurrentTheme(defaultTheme);
                }
                else
                {
                    ThemeManager.SetCurrentTheme(customTheme);
                }
            }
        }
    }
}
