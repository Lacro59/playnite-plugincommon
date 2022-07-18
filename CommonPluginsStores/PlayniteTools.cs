using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using CommonPlayniteShared.Common;
using CommonPluginsStores.Steam;

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



        public static List<string> ListVariables = new List<string>
        {
            "{InstallDir}", "{InstallDirName}", "{ImagePath}", "{ImageName}", "{ImageNameNoExt}", "{PlayniteDir}", "{Name}",
            "{Platform}", "{GameId}", "{DatabaseId}", "{PluginId}", "{Version}", "{EmulatorDir}",

            "{DropboxFolder}", "{OneDriveFolder}",

            "{SteamId}", "{SteamAccountId}", "{SteamInstallDir}", "{SteamScreenshotsDir}",
            "{UbisoftInstallDir}", "{UbisoftScreenshotsDir}",
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
            result = CommonPluginsShared.PlayniteTools.StringExpandWithoutStore(game, result, fixSeparators);


            // Steam
            if (result.Contains("{Steam"))
            {
                SteamApi steamApi = new SteamApi("PlayniteTools");

                if (SteamId =="null")
                {
                    SteamId = steamApi.CurrentUser.SteamId.ToString();
                }
                if (SteamAccountId == "null")
                {
                    SteamAccountId = steamApi.CurrentUser.AccountId.ToString();
                }
                if (SteamInstallDir == "null")
                {
                    SteamInstallDir = steamApi.GetInstallationPath();
                }
                if (SteamScreenshotsDir == "null")
                {
                    SteamScreenshotsDir = steamApi.GetScreeshotsPath();
                }

                result = result.Replace("{SteamId}", SteamId);
                result = result.Replace("{SteamInstallDir}", SteamInstallDir);
                result = result.Replace("{SteamScreenshotsDir}", SteamScreenshotsDir);
            }


            // Ubisoft Connect
            if (result.Contains("{Ubisoft"))
            {
                UbisoftAPI ubisoftAPI = null;

                if (UbisoftInstallDir == "null")
                {
                    ubisoftAPI = ubisoftAPI ?? new UbisoftAPI();
                    UbisoftInstallDir = ubisoftAPI.GetInstallationPath();
                }
                if (UbisoftScreenshotsDir == "null")
                {
                    ubisoftAPI = ubisoftAPI ?? new UbisoftAPI();
                    UbisoftScreenshotsDir = ubisoftAPI.GetScreeshotsPath();
                }

                result = result.Replace("{UbisoftInstallDir}", UbisoftInstallDir);
                result = result.Replace("{UbisoftScreenshotsDir}", UbisoftScreenshotsDir);
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
            if (!SteamId.IsNullOrEmpty())
            {
                result = result.Replace(SteamId, "{SteamId}");
            }

            string SteamInstallDir = StringExpandWithStores(game, "{SteamInstallDir}");
            if (!SteamInstallDir.IsNullOrEmpty())
            {
                result = result.Replace(SteamInstallDir, "{SteamInstallDir}");
            }

            string SteamScreenshotsDir = StringExpandWithStores(game, "{SteamScreenshotsDir}");
            if (!SteamScreenshotsDir.IsNullOrEmpty())
            {
                result = result.Replace(SteamScreenshotsDir, "{SteamScreenshotsDir}");
            }

            string UbisoftInstallDir = StringExpandWithStores(game, "{UbisoftInstallDir}");
            if (!UbisoftInstallDir.IsNullOrEmpty())
            {
                result = result.Replace(UbisoftInstallDir, "{UbisoftInstallDir}");
            }

            string UbisoftScreenshotsDir = StringExpandWithStores(game, "{UbisoftScreenshotsDir}");
            if (!UbisoftScreenshotsDir.IsNullOrEmpty())
            {
                result = result.Replace(UbisoftScreenshotsDir, "{UbisoftScreenshotsDir}");
            }


            result = CommonPluginsShared.PlayniteTools.PathToRelativeWithoutStores(game, result);
            return result;
        }

        public static string NormalizeGameName(string name, bool removeEditions = false)
        {
            return CommonPluginsShared.PlayniteTools.NormalizeGameName(name, removeEditions);
        }
    }
}
