using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPlayniteShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonPlayniteShared.Manifests;
using CommonPlayniteShared.Common;

namespace CommonPluginsStores
{
    public class PlayniteTools
    {
        private static string SteamId = "null";
        private static string SteamInstallDir = "null";
        private static string SteamScreenshotsDir = "null";

        private static string UbisoftInstallDir = "null";
        private static string UbisoftScreenshotsDir = "null";



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
                SteamApi steamApi = null;

                if (SteamId =="null")
                {
                    steamApi = steamApi ?? new SteamApi();
                    SteamId = steamApi.GetUserSteamId();
                }
                if (SteamInstallDir == "null")
                {
                    steamApi = steamApi ?? new SteamApi();
                    SteamInstallDir = steamApi.GetInstallationPath();
                }
                if (SteamScreenshotsDir == "null")
                {
                    steamApi = steamApi ?? new SteamApi();
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
    }
}
