﻿using Playnite.SDK;
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
                SteamApi steamApi = new SteamApi();

                result = result.Replace("{SteamId}", steamApi.GetUserSteamId());
                result = result.Replace("{SteamInstallDir}", steamApi.GetInstallationPath());
                result = result.Replace("{SteamScreenshotsDir}", steamApi.GetScreeshotsPath());
            }


            // Ubisoft Connect
            if (result.Contains("{Ubisoft"))
            {
                UbisoftAPI ubisoftAPI = new UbisoftAPI();

                result = result.Replace("{UbisoftInstallDir}", ubisoftAPI.GetInstallationPath());
                result = result.Replace("{UbisoftScreenshotsDir}", ubisoftAPI.GetScreeshotsPath());
            }


            return fixSeparators ? Paths.FixSeparators(result) : result;
        }
    }
}
