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
            if (game.GameActions == null)
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
            if (game.GameActions == null)
                return false;

            List<Emulator> ListEmulators = GetListEmulators(PlayniteApi);
            foreach (var action in game.GameActions)
            {
                if (action == null || !action.IsPlayAction || action.EmulatorId == Guid.Empty)
                    continue;

                var emulator = ListEmulators.FirstOrDefault(e => e.Id == action.EmulatorId);
                if (emulator == null)
                    continue;

                if (emulator.BuiltInConfigId == "rpcs3")
                    return true;
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
            BuiltinExtension PluginSource = Playnite.SDK.BuiltinExtensions.GetExtensionFromId(game.PluginId);
            switch (PluginSource)
            {
                case BuiltinExtension.AmazonGamesLibrary:
                    return "Amazon Games";
                case BuiltinExtension.BattleNetLibrary:
                    return "Battle.NET";
                case BuiltinExtension.BethesdaLibrary:
                    return "Bethesda";
                case BuiltinExtension.EpicLibrary:
                    return "Epic";
                case BuiltinExtension.GogLibrary:
                    return "GOG";
                case BuiltinExtension.HumbleLibrary:
                    return "Humble";
                case BuiltinExtension.ItchioLibrary:
                    return "itch.io";
                case BuiltinExtension.OriginLibrary:
                    return "Origin";
                case BuiltinExtension.SteamLibrary:
                    return "Steam";
                case BuiltinExtension.TwitchLibrary:
                    return "Twitch";
                case BuiltinExtension.UplayLibrary:
                    return "Ubisoft Connect";
                case BuiltinExtension.XboxLibrary:
                    return "Xbox";
                case BuiltinExtension.PSNLibrary:
                    return "Playstation";
            }


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
