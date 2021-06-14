using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores
{
    public class PlayniteTools
    {
        public static string StringExpandWithStores(Game game, string inputString, bool fixSeparators = false, bool SafeName = true)
        {
            if (string.IsNullOrEmpty(inputString) || !inputString.Contains('{'))
            {
                return inputString;
            }


            // Playnite variables
            var result = inputString;
            if (!game.InstallDirectory.IsNullOrWhiteSpace())
            {
                result = result.Replace(ExpandableVariables.InstallationDirectory, game.InstallDirectory);
                result = result.Replace(ExpandableVariables.InstallationDirName, Path.GetFileName(Path.GetDirectoryName(game.InstallDirectory)));
            }

            if (!game.GameImagePath.IsNullOrWhiteSpace())
            {
                result = result.Replace(ExpandableVariables.ImagePath, game.GameImagePath);
                result = result.Replace(ExpandableVariables.ImageNameNoExtension, Path.GetFileNameWithoutExtension(game.GameImagePath));
                result = result.Replace(ExpandableVariables.ImageName, Path.GetFileName(game.GameImagePath));
            }

            result = result.Replace(ExpandableVariables.PlayniteDirectory, PlaynitePaths.ProgramPath);

            if (SafeName)
            {
                result = result.Replace(ExpandableVariables.Name, Paths.GetSafeFilename(game.Name));
                result = result.Replace(ExpandableVariables.Platform, Paths.GetSafeFilename(game.Platform?.Name));
            }
            else
            {
                result = result.Replace(ExpandableVariables.Name, game.Name);
                result = result.Replace(ExpandableVariables.Platform, game.Platform?.Name);
            }

            result = result.Replace(ExpandableVariables.PluginId, game.PluginId.ToString());
            result = result.Replace(ExpandableVariables.GameId, game.GameId);
            result = result.Replace(ExpandableVariables.DatabaseId, game.Id.ToString());
            result = result.Replace(ExpandableVariables.Version, game.Version);


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
