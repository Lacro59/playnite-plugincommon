using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonPlayniteShared.Common;
using CommonPluginsStores.Steam;
using Microsoft.Win32;
using static CommonPluginsShared.PlayniteTools;
using CommonPluginsShared;

namespace CommonPluginsStores
{
    public class PlayniteTools
    {
        private static string SteamId = "null";
        private static string SteamAccountId = "null";
        private static string SteamInstallDir = "null";
        private static string SteamScreenshotsDir = "null";

        private static string UbisoftInstallDir = "null";
        private static string UbisoftScreenshotsDir = "null";

        private static string GogScreenshotDir = "null";
        private static string XboxGamebarScreenshotsDir = "null";

        private const string XboxCapturesShellFolderValueName = "{EDC0FE71-98D8-4F4A-B920-C8DC133CB165}";



        public static List<string> ListVariables = new List<string>
        {
            "{InstallDir}", "{InstallDirName}", "{ImagePath}", "{ImageName}", "{ImageNameNoExt}", "{PlayniteDir}", "{Name}",
            "{Platform}", "{GameId}", "{DatabaseId}", "{PluginId}", "{Version}", "{EmulatorDir}",

            "{DropboxFolder}", "{OneDriveFolder}",

            "{SteamId}", "{SteamAccountId}", "{SteamInstallDir}", "{SteamScreenshotsDir}",
            "{UbisoftInstallDir}", "{UbisoftScreenshotsDir}",
            "{GogScreenshotDir}", "{XboxGamebarScreenshotsDir}",
            "{RetroArchScreenshotsDir}",

            "{WinDir}", "{AllUsersProfile}", "{AppData}", "{HomePath}", "{UserName}", "{ComputerName}", "{UserProfile}",
            "{HomeDrive}", "{SystemDrive}", "{SystemRoot}", "{Public}", "{CommonProgramW6432}", "{CommonProgramFiles}",
            "{ProgramFiles}", "{CommonProgramFiles(x86)}", "{ProgramFiles(x86)}"
        };

        public static string StringExpandWithStores(Game game, string inputString, bool fixSeparators = false)
        {
            if (string.IsNullOrEmpty(inputString) || !inputString.Contains('{'))
            {
                return inputString;
            }

            string result = inputString;
            result = StringExpandWithoutStore(game, result, fixSeparators);

            // Steam
            if (result.Contains("{Steam"))
            {
                SteamApi steamApi = new SteamApi("PlayniteTools", ExternalPlugin.None);

                if (SteamId == "null")
                {
                    SteamId = steamApi.CurrentAccountInfos?.UserId.ToString() ?? string.Empty;
                }
                if (SteamAccountId == "null")
                {
                    if (steamApi.CurrentAccountInfos != null)
                    {
                        SteamAccountId = SteamApi.GetAccountId(ulong.Parse(steamApi.CurrentAccountInfos.UserId)).ToString() ?? string.Empty;
                    }
                }
                if (SteamInstallDir == "null")
                {
                    SteamInstallDir = steamApi.GetInstallationPath();
                }
                if (SteamScreenshotsDir == "null")
                {
                    SteamScreenshotsDir = steamApi.GetScreeshotsPath();
                }

                result = SteamId.IsNullOrEmpty() ? result : result.Replace("{SteamId}", SteamId);
                result = SteamInstallDir.IsNullOrEmpty() ? result : result.Replace("{SteamInstallDir}", SteamInstallDir);
                result = SteamScreenshotsDir.IsNullOrEmpty() ? result : result.Replace("{SteamScreenshotsDir}", SteamScreenshotsDir);
            }

            // Ubisoft Connect
            if (result.Contains("{Ubisoft"))
            {
                UplayAPI ubisoftAPI = null;

                if (UbisoftInstallDir == "null")
                {
                    ubisoftAPI = ubisoftAPI ?? new UplayAPI();
                    UbisoftInstallDir = ubisoftAPI.GetInstallationPath();
                }
                if (UbisoftScreenshotsDir == "null")
                {
                    ubisoftAPI = ubisoftAPI ?? new UplayAPI();
                    UbisoftScreenshotsDir = ubisoftAPI.GetScreeshotsPath();
                }

                result = UbisoftInstallDir.IsNullOrEmpty() ? result : result.Replace("{UbisoftInstallDir}", UbisoftInstallDir);
                result = UbisoftScreenshotsDir.IsNullOrEmpty() ? result : result.Replace("{UbisoftScreenshotsDir}", UbisoftScreenshotsDir);
            }

            // GOG Galaxy
            if (result.Contains("{GogScreenshotDir"))
            {
                if (GogScreenshotDir == "null")
                {
                    GogScreenshotDir = GetGogScreenshotDir();
                }

                result = GogScreenshotDir.IsNullOrEmpty() ? result : result.Replace("{GogScreenshotDir}", GogScreenshotDir);
            }

            // Xbox Game Bar
            if (result.Contains("{XboxGamebarScreenshotsDir"))
            {
                if (XboxGamebarScreenshotsDir == "null")
                {
                    XboxGamebarScreenshotsDir = GetXboxGamebarScreenshotsDir();
                }

                result = XboxGamebarScreenshotsDir.IsNullOrEmpty() ? result : result.Replace("{XboxGamebarScreenshotsDir}", XboxGamebarScreenshotsDir);
            }

            return fixSeparators ? Paths.FixSeparators(result) : result;
        }

        public static string PathToRelativeWithStores(Game game, string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                return inputString;
            }

            string result = inputString;

            string SteamId = StringExpandWithStores(game, "{SteamId}");
            result = SteamId.IsNullOrEmpty() ? result : result.Replace(SteamId, "{SteamId}");

            string SteamInstallDir = StringExpandWithStores(game, "{SteamInstallDir}");
            result = SteamInstallDir.IsNullOrEmpty() ? result : result.Replace(SteamInstallDir, "{SteamInstallDir}");

            string SteamScreenshotsDir = StringExpandWithStores(game, "{SteamScreenshotsDir}");
            result = SteamScreenshotsDir.IsNullOrEmpty() ? result : result.Replace(SteamScreenshotsDir, "{SteamScreenshotsDir}");

            string UbisoftInstallDir = StringExpandWithStores(game, "{UbisoftInstallDir}");
            result = UbisoftInstallDir.IsNullOrEmpty() ? result : result.Replace(UbisoftInstallDir, "{UbisoftInstallDir}");

            string UbisoftScreenshotsDir = StringExpandWithStores(game, "{UbisoftScreenshotsDir}");
            result = UbisoftScreenshotsDir.IsNullOrEmpty() ? result : result.Replace(UbisoftScreenshotsDir, "{UbisoftScreenshotsDir}");

            string GogScreenshotDir = StringExpandWithStores(game, "{GogScreenshotDir}");
            result = GogScreenshotDir.IsNullOrEmpty() ? result : result.Replace(GogScreenshotDir, "{GogScreenshotDir}");

            string XboxGamebarScreenshotsDir = StringExpandWithStores(game, "{XboxGamebarScreenshotsDir}");
            result = XboxGamebarScreenshotsDir.IsNullOrEmpty() ? result : result.Replace(XboxGamebarScreenshotsDir, "{XboxGamebarScreenshotsDir}");

            result = CommonPluginsShared.PlayniteTools.PathToRelativeWithoutStores(game, result);
            return result;
        }

        /// <summary>
        /// Gets the GOG Galaxy screenshots directory under the user Documents folder (including OneDrive redirection).
        /// </summary>
        /// <returns>The canonical screenshots folder path.</returns>
        private static string GetGogScreenshotDir()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "GOG Galaxy",
                "Screenshots");
        }

        /// <summary>
        /// Gets the Xbox Game Bar captures directory from the Windows shell folder registry value,
        /// falling back to the default Videos\Captures folder when the registry value is absent.
        /// </summary>
        /// <returns>The captures folder path, or an empty string when it cannot be resolved.</returns>
        private static string GetXboxGamebarScreenshotsDir()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders"))
                {
                    if (key != null)
                    {
                        string[] valueNames = key.GetValueNames();
                        if (valueNames != null && valueNames.Contains(XboxCapturesShellFolderValueName))
                        {
                            object registryValue = key.GetValue(XboxCapturesShellFolderValueName);
                            if (registryValue != null)
                            {
                                return registryValue.ToString().Replace('/', '\\');
                            }
                        }
                    }
                }

                string fallbackDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Videos",
                    "Captures");

                return Directory.Exists(fallbackDir) ? fallbackDir : string.Empty;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "Error resolving Xbox Game Bar screenshots directory.");
                return string.Empty;
            }
        }

        public static string NormalizeGameName(string name, bool removeEditions = false)
        {
            return CommonPluginsShared.PlayniteTools.NormalizeGameName(name, removeEditions);
        }
    }
}
