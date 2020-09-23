using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using System;
using System.IO;
using PluginCommon.PlayniteResources.Common.Extensions;

namespace PluginCommon
{
    public class SteamApi
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly string urlSteamListApp = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";
        private readonly JObject SteamListApp = new JObject();


        public SteamApi(string PluginUserDataPath)
        {
            // Class variable
            string PluginCachePath = PluginUserDataPath + "\\cache\\";
            string PluginCacheFile = PluginCachePath + "\\SteamListApp.json";

            // Load Steam list app
            try
            {
                if (Directory.Exists(PluginCachePath))
                {
                    // From cache if it exists
                    if (File.Exists(PluginCacheFile))
                    {
                        // If not expired
                        if (File.GetLastWriteTime(PluginCacheFile).AddDays(3) > DateTime.Now)
                        {
                            logger.Info("PluginCommon - GetSteamAppListFromCache");
                            SteamListApp = JObject.Parse(File.ReadAllText(PluginCacheFile));
                        }
                        else
                        {
                            SteamListApp = GetSteamAppListFromWeb(PluginCacheFile);
                        }
                    }
                    // From web
                    else
                    {
                        SteamListApp = GetSteamAppListFromWeb(PluginCacheFile);
                    }
                }
                else
                {
                    Directory.CreateDirectory(PluginCachePath);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", "Error on load SteamListApp");
            }
        }

        private JObject GetSteamAppListFromWeb(string PluginCacheFile)
        {
            logger.Info("PluginCommon - GetSteamAppListFromWeb");

            string responseData = string.Empty;
            try
            {
                responseData = Web.DownloadStringData(urlSteamListApp).GetAwaiter().GetResult();
                if (responseData.IsNullOrEmpty() || responseData == "{\"applist\":{\"apps\":[]}}")
                {
                    responseData = JsonConvert.SerializeObject(new JObject());
                }
                else
                {
                    // Write file for cache usage
                    File.WriteAllText(PluginCacheFile, responseData);
                }
            }
            catch(Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Failed to load from {urlSteamListApp}");
                responseData = "{\"applist\":{\"apps\":[]}}";
            }

            return JObject.Parse(responseData);
        }

        private string NormalizeGameName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var newName = name.ToLower();
            newName = newName.RemoveTrademarks();
            newName = newName.Replace("_", "");
            newName = newName.Replace(".", "");
            newName = newName.Replace('’', '\'');
            newName = newName.Replace(":", "");
            newName = newName.Replace("-", "");
            newName = newName.Replace("goty", "");
            newName = newName.Replace("game of the year edition", "");
            newName = newName.Replace("  ", " ");

            return newName.Trim();
        }

        public int GetSteamId(string Name)
        {
            int SteamId = 0;
        
            try
            {
                foreach (JObject Game in SteamListApp["applist"]["apps"])
                {
                    string NameSteam = NormalizeGameName((string)Game["name"]);
                    string NameSearch = NormalizeGameName(Name);

#if DEBUG
            logger.Debug($"PluginCommon - GetSteamId() - Search: {Name} => {NameSearch} - Steam: {Game["name"]} => {NameSteam}");
#endif

                    if (NameSteam == NameSearch)
                    {
                        return (int)Game["appid"];
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error with {Name}");
            }
        
            if (SteamId == 0)
            {
                logger.Warn($"PluginCommon - SteamId not find for {Name}");
            }
        
            return SteamId;
        }
    }
}
