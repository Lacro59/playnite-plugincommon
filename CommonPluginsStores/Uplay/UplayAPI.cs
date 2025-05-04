using CommonPlayniteShared.Common;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace CommonPluginsStores
{
    public class UplayAPI
    {
        private static ILogger Logger => LogManager.GetLogger();


        public UplayAPI()
        {
        }


        public string GetInstallationPath()
        {
            var progs = Programs.GetUnistallProgramsList().FirstOrDefault(a => a.DisplayName == "Ubisoft Connect");
            if (progs == null)
            {
                return string.Empty;
            }
            else
            {
                return progs.InstallLocation;
            }
        }

        public string GetScreeshotsPath()
        {
            string configPath = Path.Combine(Environment.GetEnvironmentVariable("AppData"), "..", "Local", "Ubisoft Game Launcher", "settings.yaml");
            if (File.Exists(configPath))
            {
                dynamic SettingsData = Serialization.FromYamlFile<dynamic>(configPath);
                return ((string)SettingsData["misc"]["screenshot_root_path"]).Replace('/', Path.DirectorySeparatorChar);
            }

            return string.Empty;
        }
    }
}
