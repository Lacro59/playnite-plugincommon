using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPluginsPlaynite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonPlayniteShared.Manifests;
using CommonPluginsPlaynite.Common;
using System.Text.RegularExpressions;
using CommonPluginsShared.Extensions;

namespace CommonPluginsShared
{
    public class PlayniteTools
    {
        private static HashSet<string> _disabledPlugins;
        private static readonly ILogger logger = LogManager.GetLogger();

        private static List<Emulator> ListEmulators = null;
        private static HashSet<string> DisabledPlugins
        {
            get { return _disabledPlugins ?? (_disabledPlugins = GetDisabledPlugins()); }
        }


        private enum ExternalPlugin
        {
            None,
            BattleNetLibrary,
            GogLibrary,
            OriginLibrary,
            PSNLibrary,
            SteamLibrary,
            XboxLibrary,
            IndiegalaLibrary,
            AmazonGamesLibrary,
            BethesdaLibrary,
            EpicLibrary,
            HumbleLibrary,
            ItchioLibrary,
            RockstarLibrary,
            TwitchLibrary,
            UplayLibrary
        }

        private static readonly Dictionary<Guid, ExternalPlugin> PluginsById = new Dictionary<Guid, ExternalPlugin>
        {
            { new Guid("e3c26a3d-d695-4cb7-a769-5ff7612c7edd"), ExternalPlugin.BattleNetLibrary },
            { new Guid("aebe8b7c-6dc3-4a66-af31-e7375c6b5e9e"), ExternalPlugin.GogLibrary },
            { new Guid("85dd7072-2f20-4e76-a007-41035e390724"), ExternalPlugin.OriginLibrary },
            { new Guid("e4ac81cb-1b1a-4ec9-8639-9a9633989a71"), ExternalPlugin.PSNLibrary },
            { new Guid("cb91dfc9-b977-43bf-8e70-55f46e410fab"), ExternalPlugin.SteamLibrary },
            { new Guid("7e4fbb5e-2ae3-48d4-8ba0-6b30e7a4e287"), ExternalPlugin.XboxLibrary },
            { new Guid("f7da6eb0-17d7-497c-92fd-347050914954"), ExternalPlugin.IndiegalaLibrary },
            { new Guid("402674cd-4af6-4886-b6ec-0e695bfa0688"), ExternalPlugin.AmazonGamesLibrary },
            { new Guid("0E2E793E-E0DD-4447-835C-C44A1FD506EC"), ExternalPlugin.BethesdaLibrary },
            { new Guid("00000002-DBD1-46C6-B5D0-B1BA559D10E4"), ExternalPlugin.EpicLibrary },
            { new Guid("96e8c4bc-ec5c-4c8b-87e7-18ee5a690626"), ExternalPlugin.HumbleLibrary },
            { new Guid("00000001-EBB2-4EEC-ABCB-7C89937A42BB"), ExternalPlugin.ItchioLibrary },
            { new Guid("88409022-088a-4de8-805a-fdbac291f00a"), ExternalPlugin.RockstarLibrary },
            { new Guid("E2A7D494-C138-489D-BB3F-1D786BEEB675"), ExternalPlugin.TwitchLibrary },
            { new Guid("C2F038E5-8B92-4877-91F1-DA9094155FC5"), ExternalPlugin.UplayLibrary }
        };


        #region Emulators
        /// <summary>
        /// Get configured emulators list
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Check if the game used an emulator
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static bool IsGameEmulated(IPlayniteAPI PlayniteApi, Guid Id)
        {
            Game game = PlayniteApi.Database.Games.Get(Id);
            return IsGameEmulated(PlayniteApi, game);
        }

        /// <summary>
        /// Check if the game used an emulator
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        public static bool IsGameEmulated(IPlayniteAPI PlayniteApi, Game game)
        {
            if (game?.GameActions == null)
            {
                return false;
            }

            List<Emulator> ListEmulators = GetListEmulators(PlayniteApi);
            return game.GameActions.Where(x => x.IsPlayAction && ListEmulators.Any(y => y.Id == x?.EmulatorId)).Count() > 0;
        }

        /// <summary>
        /// Check if a game used RPCS3 emulator
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        public static bool GameUseRpcs3(IPlayniteAPI PlayniteApi, Game game)
        {
            if (game?.GameActions == null)
            {
                return false;
            }

            List<Emulator> ListEmulators = GetListEmulators(PlayniteApi);
            foreach (var action in game.GameActions)
            {
                if (action == null || !action.IsPlayAction || action.EmulatorId == Guid.Empty)
                {
                    continue;
                }

                var emulator = ListEmulators.FirstOrDefault(e => e.Id == action.EmulatorId);
                if (emulator == null || emulator.BuiltInConfigId.IsNullOrEmpty())
                {
                    continue;
                }

                if (emulator.BuiltInConfigId == "rpcs3")
                {
                    return true;
                }
            }
            return false;
        }
        #endregion


        /// <summary>
        /// Get file from cache
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="PluginName"></param>
        /// <returns></returns>
        public static string GetCacheFile(string FileName, string PluginName)
        {
            PluginName = PluginName.ToLower();

            try
            {
                if (!Directory.Exists(Path.Combine(PlaynitePaths.DataCachePath, PluginName)))
                {
                    Directory.CreateDirectory(Path.Combine(PlaynitePaths.DataCachePath, PluginName));
                }

                string PathImageFileName = Path.Combine(PlaynitePaths.DataCachePath, PluginName, FileName);

                if (File.Exists(PathImageFileName))
                {
                    return PathImageFileName;
                }
                // TODO If used, must be changed
                if (File.Exists(PathImageFileName + ".png"))
                {
                    return PathImageFileName + ".png";
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return string.Empty;
        }


        /// <summary>
        /// Check if a plugin is disabled
        /// </summary>
        /// <param name="PluginName"></param>
        /// <returns></returns>
        public static bool IsDisabledPlaynitePlugins(string PluginName)
        {
            return DisabledPlugins?.Contains(PluginName) ?? false;
        }

        private static HashSet<string> GetDisabledPlugins()
        {
            try
            {
                string FileConfig = PlaynitePaths.ConfigFilePath;
                if (File.Exists(FileConfig))
                {
                    dynamic playniteConfig = Serialization.FromJsonFile<dynamic>(FileConfig);
                    dynamic disabledPlugins = playniteConfig.DisabledPlugins;
                    var output = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (disabledPlugins != null)
                    {
                        foreach (var pluginName in disabledPlugins)
                        {
                            output.Add(pluginName.ToString());
                        }
                    }
                    return output;
                }
                else
                {
                    logger.Warn($"File not found {FileConfig}");
                    return new HashSet<string>();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }
        }


        #region Game informations
        /// <summary>
        /// Get normalized source name
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static string GetSourceName(IPlayniteAPI PlayniteApi, Guid Id)
        {
            Game game = PlayniteApi.Database.Games.Get(Id);
            if (game == null)
            {
                return "Playnite";
            }
            return GetSourceName(PlayniteApi, game);
        }

        /// <summary>
        /// Get normalized source name
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        public static string GetSourceName(IPlayniteAPI PlayniteApi, Game game)
        {
            string SourceName = GetSourceByPluginId(game.PluginId);
            if (!SourceName.IsNullOrEmpty())
            {
                return SourceName;
            }

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
                else if (PlayniteApi.Database.Sources.Get(game.SourceId)?.Name.ToLower() == "xbox game pass")
                {
                    SourceName = "Xbox";
                }
                else if (game.SourceId != null && game.SourceId != default(Guid))
                {
                    SourceName = PlayniteApi.Database.Sources.Get(game.SourceId)?.Name;
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

        public static string GetSourceByPluginId(Guid PluginId)
        {
            PluginsById.TryGetValue(PluginId, out ExternalPlugin PluginSource);
            switch (PluginSource)
            {
                case ExternalPlugin.AmazonGamesLibrary:
                    return "Amazon Games";
                case ExternalPlugin.BattleNetLibrary:
                    return "Battle.NET";
                case ExternalPlugin.BethesdaLibrary:
                    return "Bethesda";
                case ExternalPlugin.EpicLibrary:
                    return "Epic";
                case ExternalPlugin.GogLibrary:
                    return "GOG";
                case ExternalPlugin.HumbleLibrary:
                    return "Humble";
                case ExternalPlugin.ItchioLibrary:
                    return "itch.io";
                case ExternalPlugin.OriginLibrary:
                    return "Origin";
                case ExternalPlugin.SteamLibrary:
                    return "Steam";
                case ExternalPlugin.TwitchLibrary:
                    return "Twitch";
                case ExternalPlugin.UplayLibrary:
                    return "Ubisoft Connect";
                case ExternalPlugin.XboxLibrary:
                    return "Xbox";
                case ExternalPlugin.PSNLibrary:
                    return "Playstation";
            }

            return string.Empty;
        }


        /// <summary>
        /// Get platform icon if defined
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="PlatformName"></param>
        /// <returns></returns>
        public static string GetPlatformIcon(IPlayniteAPI PlayniteApi, string PlatformName)
        {
            Platform PlatformFinded = PlayniteApi.Database.Platforms?.Where(x => x.Name.ToLower() == PlatformName.ToLower()).FirstOrDefault();
            if (!(PlatformFinded?.Icon).IsNullOrEmpty())
            {
                return PlayniteApi.Database.GetFullFilePath(PlatformFinded.Icon);
            }
            return string.Empty;
        }

        private static Regex NonWordCharactersAndTrimmableWhitespace = new Regex(@"(?<start>^[\W_]+)|(?<end>[\W_]+$)|(?<middle>[\W_]+)", RegexOptions.Compiled);
        private static Regex EditionInGameName = new Regex(@"\b(goty|game of the year|standard|deluxe|definitive|ultimate|platinum|gold|extended|complete|special|anniversary|enhanced)( edition)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Remove all non-letter and non-number characters from a string, remove diacritics, make lowercase. For use when comparing game titles.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="removeEditions">Remove "game of the year", "complete edition" and the like from the string too</param>
        /// <returns></returns>
        public static string NormalizeGameName(string name, bool removeEditions = false)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            string newName = name;
            if (removeEditions)
                newName = EditionInGameName.Replace(newName, string.Empty);

            MatchEvaluator matchEvaluator = (Match match) =>
            {
                if (match.Groups["middle"].Success) //if the match group is the last one in the regex (non-word characters, including whitespace, in the middle of a string)
                    return " "; //replace (multiple) non-word character(s) in the middle of the string with a space
                else
                    return string.Empty; //remove non-word characters (including white space) at the start and end of the string
            };
            newName = NonWordCharactersAndTrimmableWhitespace.Replace(newName, matchEvaluator).RemoveDiacritics();

            return newName.ToLowerInvariant();
        }
        #endregion


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
                customTheme = ThemeManager.GetAvailableThemes(ApplicationMode.Desktop).SingleOrDefault(a => a.Id == theme);
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


        /// <summary>
        /// Remplace Playnite & Windows variables
        /// </summary>
        /// <param name="game"></param>
        /// <param name="inputString"></param>
        /// <param name="fixSeparators"></param>
        /// <returns></returns>
        public static string StringExpandWithoutStore(IPlayniteAPI PlayniteAPI, Game game, string inputString, bool fixSeparators = false)
        {
            if (string.IsNullOrEmpty(inputString) || !inputString.Contains('{'))
            {
                return inputString;
            }

            string result = inputString;

            // Playnite variables
            result = PlayniteAPI.ExpandGameVariables(game, inputString);


            // Dropbox
            if (result.Contains("{Dropbox"))
            {
                string DropboxInfoFile = Path.Combine(Environment.GetEnvironmentVariable("AppData"), "..", "Local", "Dropbox", "info.json");
                if (File.Exists(DropboxInfoFile))
                {
                    dynamic DropboxInfo = Serialization.FromJsonFile<dynamic>(DropboxInfoFile);
                    result = result.Replace("{DropboxFolder}", ((dynamic)DropboxInfo["personal"]["path"]).Value);
                }
            }


            // Windows variables
            result = result.Replace("{WinDir}", Environment.GetEnvironmentVariable("WinDir"));
            result = result.Replace("{AllUsersProfile}", Environment.GetEnvironmentVariable("AllUsersProfile"));
            result = result.Replace("{AppData}", Environment.GetEnvironmentVariable("AppData"));
            result = result.Replace("{HomePath}", Environment.GetEnvironmentVariable("HomePath"));
            result = result.Replace("{UserName}", Environment.GetEnvironmentVariable("UserName"));
            result = result.Replace("{ComputerName}", Environment.GetEnvironmentVariable("ComputerName"));
            result = result.Replace("{UserProfile}", Environment.GetEnvironmentVariable("UserProfile"));
            result = result.Replace("{HomeDrive}", Environment.GetEnvironmentVariable("HomeDrive"));
            result = result.Replace("{SystemDrive}", Environment.GetEnvironmentVariable("SystemDrive"));
            result = result.Replace("{SystemRoot}", Environment.GetEnvironmentVariable("SystemRoot"));
            result = result.Replace("{Public}", Environment.GetEnvironmentVariable("Public"));
            result = result.Replace("{ProgramFiles}", Environment.GetEnvironmentVariable("ProgramFiles"));
            result = result.Replace("{CommonProgramFiles}", Environment.GetEnvironmentVariable("CommonProgramFiles"));
            result = result.Replace("{CommonProgramFiles(x86)}", Environment.GetEnvironmentVariable("CommonProgramFiles(x86)"));
            result = result.Replace("{CommonProgramW6432}", Environment.GetEnvironmentVariable("CommonProgramW6432"));
            result = result.Replace("{ProgramFiles(x86)}", Environment.GetEnvironmentVariable("ProgramFiles(x86)"));


            return fixSeparators ? Paths.FixSeparators(result) : result;
        }
    }
}
