using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPlayniteShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonPlayniteShared.Manifests;
using CommonPlayniteShared.Common;
using System.Text.RegularExpressions;
using CommonPluginsShared.Extensions;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Diagnostics;
using Microsoft.Win32;
using Playnite.SDK.Plugins;

namespace CommonPluginsShared
{
    public class PlayniteTools
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private static List<Emulator> ListEmulators = null;

        private static HashSet<string> _disabledPlugins;
        private static HashSet<string> DisabledPlugins
        {
            get { return _disabledPlugins ?? (_disabledPlugins = GetDisabledPlugins()); }
        }


        #region External plugin
        public enum ExternalPlugin
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
            OculusLibrary,
            RiotLibrary,
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
            { new Guid("C2F038E5-8B92-4877-91F1-DA9094155FC5"), ExternalPlugin.UplayLibrary },
            { new Guid("77346DD6-B0CC-4F7D-80F0-C1D138CCAE58"), ExternalPlugin.OculusLibrary },
            { new Guid("317a5e2e-eac1-48bc-adb3-fb9e321afd3f"), ExternalPlugin.RiotLibrary }
        };

        public static ExternalPlugin GetPluginType(Guid PluginId)
        {
            PluginsById.TryGetValue(PluginId, out ExternalPlugin PluginSource);
            return PluginSource;
        }

        public static Guid GetPluginId(ExternalPlugin externalPlugin)
        {
            return PluginsById.FirstOrDefault(x => x.Value == externalPlugin).Key;
        }
        #endregion


        #region Emulators
        /// <summary>
        /// Get configured emulators list
        /// </summary>
        /// <returns></returns>
        public static List<Emulator> GetListEmulators()
        {
            if (ListEmulators == null)
            {
                ListEmulators = new List<Emulator>();
                foreach (Emulator item in API.Instance.Database.Emulators)
                {
                    ListEmulators.Add(item);
                }
            }

            return ListEmulators;
        }

        /// <summary>
        /// Check if the game used an emulator
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static bool IsGameEmulated(Guid Id)
        {
            Game game = API.Instance.Database.Games.Get(Id);
            return IsGameEmulated(game);
        }

        /// <summary>
        /// Check if the game used an emulator
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        // TODO can be better
        public static bool IsGameEmulated(Game game)
        {
            if (game?.GameActions == null)
            {
                return false;
            }

            List<Emulator> ListEmulators = GetListEmulators();
            return game.GameActions.Where(x => x.IsPlayAction && ListEmulators.Any(y => y.Id == x?.EmulatorId)).Count() > 0;
        }

        /// <summary>
        /// Check if a game used RPCS3 emulator
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public static bool GameUseRpcs3(Game game)
        {
            if (game?.GameActions == null)
            {
                return false;
            }

            foreach (var action in game.GameActions)
            {
                var emulator = API.Instance.Database.Emulators?.FirstOrDefault(e => e.Id == action?.EmulatorId);

                if (emulator == null)
                {
                    logger.Warn($"No emulator find for {game.Name}");
                    return false;
                }

                string BuiltInConfigId = string.Empty;
                if (emulator.BuiltInConfigId == null)
                {
                    //logger.Warn($"No BuiltInConfigId find for {emulator.Name}");
                }
                else
                {
                    BuiltInConfigId = emulator.BuiltInConfigId;
                }

                if (BuiltInConfigId.Contains("rpcs3", StringComparison.OrdinalIgnoreCase)
                    || emulator.Name.Contains("rpcs3", StringComparison.OrdinalIgnoreCase)
                    || emulator.InstallDir.Contains("rpcs3", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool GameUseRetroArch(Game game)
        {
            if (game?.GameActions == null)
            {
                return false;
            }

            foreach (var action in game.GameActions)
            {
                var emulator = API.Instance.Database.Emulators?.FirstOrDefault(e => e.Id == action?.EmulatorId);

                if (emulator == null)
                {
                    logger.Warn($"No emulator find for {game.Name}");
                    return false;
                }

                string BuiltInConfigId = string.Empty;
                if (emulator.BuiltInConfigId == null)
                {
                    logger.Warn($"No BuiltInConfigId find for {emulator.Name}");
                }
                else
                {
                    BuiltInConfigId = emulator.BuiltInConfigId;
                }

                if (BuiltInConfigId.Contains("RetroArch", StringComparison.OrdinalIgnoreCase)
                    || emulator.Name.Contains("RetroArch", StringComparison.OrdinalIgnoreCase)
                    || emulator.InstallDir.Contains("RetroArch", StringComparison.OrdinalIgnoreCase))
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
        public static string GetCacheFile(string FileName, string PluginName, dynamic Options = null)
        {
            PluginName = PluginName.ToLower();
            FileName = CommonPlayniteShared.Common.Paths.GetSafePathName(FileName);

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
                else
                {
                    if (!FileName.IsNullOrEmpty() && Options?.CachedFileIfMissing ?? false)
                    {
                        Task.Run(() =>
                        {
                            Common.LogDebug(true, $"DownloadFileImage is missing - {FileName}");
                            Web.DownloadFileImage(FileName, Options.Url, PlaynitePaths.DataCachePath, PluginName).GetAwaiter().GetResult();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on GetCacheFile({FileName})");
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
        /// <param name="Id"></param>
        /// <returns></returns>
        public static string GetSourceName(Guid Id)
        {
            Game game = API.Instance.Database.Games.Get(Id);
            if (game == null)
            {
                return "Playnite";
            }
            return GetSourceName(game);
        }

        /// <summary>
        /// Get normalized source name
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public static string GetSourceName(Game game)
        {
            string SourceName = GetSourceByPluginId(game.PluginId);
            if (!SourceName.IsNullOrEmpty())
            {
                return SourceName;
            }

            try
            {
                if (IsGameEmulated( game))
                {
                    SourceName = "RetroAchievements";
                    if (GameUseRpcs3(game))
                    {
                        SourceName = "Rpcs3";
                    }
                }
                else if (API.Instance.Database.Sources.Get(game.SourceId)?.Name.IsEqual("Xbox Game Pass") ?? false)
                {
                    SourceName = "Xbox";
                }
                else if (game.SourceId != null && game.SourceId != default(Guid))
                {
                    SourceName = API.Instance.Database.Sources.Get(game.SourceId)?.Name;
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
                case ExternalPlugin.IndiegalaLibrary:
                    return "Indiegala";
                case ExternalPlugin.RockstarLibrary:
                    return "Rockstar";
                case ExternalPlugin.OculusLibrary:
                    return "Oculus";
                case ExternalPlugin.RiotLibrary:
                    return "Riot Games";
            }

            return string.Empty;
        }

        public static string GetSourceBySourceIdOrPlatformId(Guid SourceId, List<Guid> PlatformsIds)
        {
            string SourceName = "Playnite";

            if (SourceId != default(Guid))
            {
                try
                {
                    var Source = API.Instance.Database.Sources.Get(SourceId);
                    if (Source == null)
                    {
                        logger.Warn($"SourceName not find for {SourceId.ToString()}");
                        return "Playnite";
                    }
                    return Source.Name;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"SourceId: {SourceId.ToString()}");
                    return "Playnite";
                }
            }

            if (PlatformsIds == null)
            {
                logger.Warn($"No PlatformsIds for {Serialization.ToJson(PlatformsIds)}");
                return SourceName;
            }

            foreach (Guid PlatformID in PlatformsIds)
            {
                if (PlatformID !=default(Guid))
                {
                    var platform = API.Instance.Database.Platforms.Get(PlatformID);
                    if (platform != null)
                    {
                        switch (platform.Name.ToLower())
                        {
                            case "pc":
                            case "pc (windows)":
                            case "pc (mac)":
                            case "pc (linux)":
                                return "Playnite";
                            default:
                                return platform.Name;
                        }
                    }
                }
            }

            return SourceName;
        }


        /// <summary>
        /// Get platform icon if defined
        /// </summary>
        /// <param name="PlatformName"></param>
        /// <returns></returns>
        public static string GetPlatformIcon(string PlatformName)
        {
            Platform PlatformFinded = API.Instance.Database.Platforms?.Where(x => x.Name.IsEqual(PlatformName)).FirstOrDefault();
            if (!(PlatformFinded?.Icon).IsNullOrEmpty())
            {
                return API.Instance.Database.GetFullFilePath(PlatformFinded.Icon);
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
            newName = newName.Replace(" (CD)", string.Empty);

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


        public static void SetThemeInformation()
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
            var theme = API.Instance.ApplicationSettings.DesktopTheme;
            if (theme != ThemeManager.DefaultTheme.Name)
            {
                customTheme = ThemeManager.GetAvailableThemes(ApplicationMode.Desktop).FirstOrDefault(a => a.Id == theme);
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
        public static string StringExpandWithoutStore(Game game, string inputString, bool fixSeparators = false)
        {
            if (string.IsNullOrEmpty(inputString) || !inputString.Contains('{'))
            {
                return inputString;
            }

            string result = inputString;

            // Playnite variables
            if (game == null)
            {
                game = new Game();
            }
            result = API.Instance.ExpandGameVariables(game, inputString);
            


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

            // OneDrive
            if (result.Contains("{OneDrive"))
            {
                result = result.Replace("{OneDriveFolder}", GetOneDriveInstallationPath());
            }

            //RetroArchScreenshotsDir
            if (result.Contains("{RetroArchScreenshotsDir"))
            {
                string RetroarchScreenshots = string.Empty;
                var emulator = API.Instance.Database.Emulators.Where(x => x.Name.Contains("RetroArch", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (emulator != null)
                {
                    string cfg = Path.Combine(emulator.InstallDir, "retroarch.cfg");
                    if (File.Exists(cfg))
                    {
                        string line = string.Empty;
                        string Name = string.Empty;
                        StreamReader file = new StreamReader(cfg);
                        while ((line = file.ReadLine()) != null)
                        {
                            if (line.Contains("screenshot_directory", StringComparison.OrdinalIgnoreCase))
                            {
                                RetroarchScreenshots = line.Replace("screenshot_directory = ", string.Empty)
                                                            .Replace("\"", string.Empty)
                                                            .Trim()
                                                            .Replace(":", emulator.InstallDir);
                            }
                        }
                        file.Close();
                    }
                }

                result = result.Replace("{RetroArchScreenshotsDir}", RetroarchScreenshots);
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


            return fixSeparators ? CommonPlayniteShared.Common.Paths.FixSeparators(result) : result;
        }

        public static string PathToRelativeWithoutStores(Game game, string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                return inputString;
            }

            string result = inputString;


            string DropboxFolder = StringExpandWithoutStore(game, "{DropboxFolder}");
            if (!DropboxFolder.IsNullOrEmpty())
            {
                result = result.Replace(DropboxFolder, "{DropboxFolder}");
            }

            string OneDriveFolder = StringExpandWithoutStore(game, "{OneDriveFolder}");
            if (!OneDriveFolder.IsNullOrEmpty())
            {
                result = result.Replace(OneDriveFolder, "{OneDriveFolder}");
            }

            string RetroArchScreenshotsDir = StringExpandWithoutStore(game, "{RetroArchScreenshotsDir}");
            if (!RetroArchScreenshotsDir.IsNullOrEmpty())
            {
                result = result.Replace(RetroArchScreenshotsDir, "{RetroArchScreenshotsDir}");
            }


            string AppData = StringExpandWithoutStore(game, "{AppData}");
            result = result.Replace(AppData, "{AppData}");

            string AllUsersProfile = StringExpandWithoutStore(game, "{AllUsersProfile}");
            result = result.Replace(AllUsersProfile, "{AllUsersProfile}");

            string CommonProgramFiles = StringExpandWithoutStore(game, "{CommonProgramFiles}");
            result = result.Replace(CommonProgramFiles, "{CommonProgramFiles}");

            string CommonProgramFiles_x86 = StringExpandWithoutStore(game, "{CommonProgramFiles(x86)}");
            result = result.Replace(CommonProgramFiles_x86, "{CommonProgramFiles(x86)}");

            string CommonProgramW6432 = StringExpandWithoutStore(game, "{CommonProgramW6432}");
            result = result.Replace(CommonProgramW6432, "{CommonProgramW6432}");

            string ProgramFiles = StringExpandWithoutStore(game, "{ProgramFiles}");
            result = result.Replace(ProgramFiles, "{ProgramFiles}");

            string ProgramFiles_x86 = StringExpandWithoutStore(game, "{ProgramFiles(x86)}");
            result = result.Replace(ProgramFiles_x86, "{ProgramFiles(x86)}");

            string Public = StringExpandWithoutStore(game, "{Public}");
            result = result.Replace(Public, "{Public}");

            string WinDir = StringExpandWithoutStore(game, "{WinDir}");
            result = result.Replace(WinDir, "{WinDir}");

            string UserProfile = StringExpandWithoutStore(game, "{UserProfile}");
            result = result.Replace(UserProfile, "{UserProfile}");

            string SystemRoot = StringExpandWithoutStore(game, "{SystemRoot}");
            result = result.Replace(SystemRoot, "{SystemRoot}");

            string HomePath = StringExpandWithoutStore(game, "{HomePath}");
            result = result.Replace(HomePath, "{HomePath}");

            string SystemDrive = StringExpandWithoutStore(game, "{SystemDrive}");
            result = result.Replace(SystemDrive, "{SystemDrive}");

            string HomeDrive = StringExpandWithoutStore(game, "{HomeDrive}");
            result = result.Replace(HomeDrive, "{HomeDrive}");

            string UserName = StringExpandWithoutStore(game, "{UserName}");
            result = result.Replace(UserName, "{UserName}");

            string ComputerName = StringExpandWithoutStore(game, "{ComputerName}");
            result = result.Replace(ComputerName, "{ComputerName}");

            return result;
        }

        private static string GetOneDriveInstallationPath()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\OneDrive"))
            {
                if (key?.GetValueNames().Contains("UserFolder") == true)
                {
                    return key.GetValue("UserFolder")?.ToString().Replace('/', '\\') ?? string.Empty;
                }
            }

            return string.Empty;
        }


        public static void CreateLogPackage(string PluginName)
        {
            var response = API.Instance.Dialogs.ShowMessage(resources.GetString("LOCCommonCreateLog"), PluginName, System.Windows.MessageBoxButton.YesNo);

            if (response == System.Windows.MessageBoxResult.Yes)
            {
                string path = Path.Combine(PlaynitePaths.DataCachePath, PluginName + "_" + DateTime.Now.ToString("yyyy-MM-dd") + ".zip");

                FileSystem.DeleteFile(path);
                using (FileStream zipToOpen = new FileStream(path, FileMode.Create))
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    foreach (var logFile in Directory.GetFiles(PlaynitePaths.ConfigRootPath, "*.log", SearchOption.TopDirectoryOnly))
                    {
                        if (Path.GetFileName(logFile) == "cef.log" || Path.GetFileName(logFile) == "debug.log")
                        {
                            continue;
                        }
                        else
                        {
                            archive.CreateEntryFromFile(logFile, Path.GetFileName(logFile));
                        }
                    }
                }

                Process.Start(PlaynitePaths.DataCachePath);
            }
        }


        public static void ShowPluginSettings(ExternalPlugin externalPlugin)
        {
            try
            {
                Guid PluginId = GetPluginId(externalPlugin);
                Plugin plugin = API.Instance.Addons.Plugins.FirstOrDefault(x => x.Id == PluginId);
                if (plugin != null)
                {
                    plugin.OpenSettingsView();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }
    }
}
