using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPluginsPlaynite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonPlayniteShared.Manifests;

namespace CommonPluginsShared
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

        public static bool IsGameEmulated(IPlayniteAPI PlayniteApi, Guid Id)
        {
            Game game = PlayniteApi.Database.Games.Get(Id);
            return IsGameEmulated(PlayniteApi, game);
        }

        public static bool IsGameEmulated(IPlayniteAPI PlayniteApi, Game game)
        {
            if (game.GameActions == null)
            {
                return false;
            }

            List<Emulator> ListEmulators = GetListEmulators(PlayniteApi);
            GameAction PlayAction = game.GameActions.Where(x => x.IsPlayAction).FirstOrDefault();

            return PlayAction != null && PlayAction.EmulatorId != null && ListEmulators.FindAll(x => x.Id == PlayAction.EmulatorId).Count > 0;
        }

        public static bool GameUseRpcs3(IPlayniteAPI PlayniteApi, Game game)
        {
            if (game.GameActions == null)
            {
                return false;
            }

            List<Emulator> ListEmulators = GetListEmulators(PlayniteApi);
            GameAction PlayAction = game.GameActions.Where(x => x.IsPlayAction).FirstOrDefault();

            return ListEmulators.Find(x => x.Id == PlayAction.EmulatorId).Profiles[0].Executable.ToLower().Contains("rpcs3.exe");
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
            }
            catch(Exception ex)
            {
                Common.LogError(ex, false);
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
                        logger.Warn($"File not found {FileConfig}");
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
                    logger.Warn($"DisabledPlugins is null");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return false;
            }

            return false;
        }


        public static string GetSourceName(IPlayniteAPI PlayniteApi, Guid Id)
        {
            Game game = PlayniteApi.Database.Games.Get(Id);
            if (game == null)
            {
                return "Playnite";
            }
            return GetSourceName(PlayniteApi, game);
        }

        public static string GetSourceName(IPlayniteAPI PlayniteApi, Game game)
        {
            string SourceName = string.Empty;

            try
            {
                if (IsGameEmulated(PlayniteApi, game))
                {
                    SourceName = "RetroAchievements";

                    if (GameUseRpcs3(PlayniteApi, game))
                    {
                        SourceName = "Rpcs3";
                    }

                }
                else if (game.SourceId != null && game.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                {
                    SourceName = PlayniteApi.Database.Sources.Get(game.SourceId).Name;
                }
                else
                {
                    SourceName = "Playnite";
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on GetSourceName({game.Name})");
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
