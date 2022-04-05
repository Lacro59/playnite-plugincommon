using CommonPlayniteShared;
using CommonPluginsShared;
using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CommonPluginsStores
{
    public class SteamApi
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly string urlSteamListApp = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";
        private readonly dynamic SteamListApp = null;

        private string InstallationPath { get; set; }

        public string LoginUsersPath
        {
            get => Path.Combine(InstallationPath, "config", "loginusers.vdf");
        }


        public SteamApi()
        {

        }






        public string GetUserSteamId()
        {
            try
            {
                string PluginSteamConfigFile = Path.Combine(PlaynitePaths.ExtensionsDataPath, "CB91DFC9-B977-43BF-8E70-55F46E410FAB", "config.json");

                if (File.Exists(PluginSteamConfigFile))
                {
                    dynamic SteamConfig = Serialization.FromJsonFile<dynamic>(PluginSteamConfigFile);

                    SteamID steamID = new SteamID();
                    steamID.SetFromUInt64((ulong)SteamConfig["UserId"]);

                    return steamID.AccountID.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return string.Empty;
            }
        }


        public string GetInstallationPath()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                if (key?.GetValueNames().Contains("SteamPath") == true)
                {
                    return key.GetValue("SteamPath")?.ToString().Replace('/', '\\') ?? string.Empty;
                }
            }

            return string.Empty;
        }

        public string GetScreeshotsPath()
        {
            string PathScreeshotsFolder = string.Empty;

            if (!InstallationPath.IsNullOrEmpty())
            {
                string SteamId = GetUserSteamId();

                if (SteamId.IsNullOrEmpty())
                {
                    logger.Warn("No find SteamId");
                    return PathScreeshotsFolder;
                }


                PathScreeshotsFolder = Path.Combine(InstallationPath, "userdata", SteamId, "760", "remote");

                if (Directory.Exists(PathScreeshotsFolder))
                {
                    return PathScreeshotsFolder;
                }
                else
                {
                    logger.Warn("Folder Steam userdata not find");
                }
            }

            logger.Warn("No find Steam installation");
            return PathScreeshotsFolder;
        }

        internal List<LocalSteamUser> GetSteamUsers()
        {
            var users = new List<LocalSteamUser>();
            if (File.Exists(LoginUsersPath))
            {
                var config = new KeyValue();

                try
                {
                    config.ReadFileAsText(LoginUsersPath);
                    foreach (var user in config.Children)
                    {
                        users.Add(new LocalSteamUser()
                        {
                            Id = ulong.Parse(user.Name),
                            AccountName = user["AccountName"].Value,
                            PersonaName = user["PersonaName"].Value,
                            Recent = user["mostrecent"].AsBoolean()
                        });
                    }
                }
                catch (Exception e) 
                {

                }
            }

            return users;
        }
    }



    public class LocalSteamUser
    {
        public ulong Id
        {
            get; set;
        }

        public string AccountName
        {
            get; set;
        }

        public string PersonaName
        {
            get; set;
        }

        public bool Recent
        {
            get; set;
        }
    }
}
