using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;

namespace CommonPluginsShared
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
                            logger.Info("GetSteamAppListFromCache");
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
                Common.LogError(ex, false, "Error on load SteamListApp");
            }
        }

        // TODO transform to task and identified object and saved in playnite temp
        private JObject GetSteamAppListFromWeb(string PluginCacheFile)
        {
            Common.LogDebug(true, "GetSteamAppListFromWeb");

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
                Common.LogError(ex, false, $"Failed to load from {urlSteamListApp}");
                responseData = "{\"applist\":{\"apps\":[]}}";
            }

            return JObject.Parse(responseData);
        }

        public int GetSteamId(string Name)
        {
            int SteamId = 0;
        
            try
            {
                if (SteamListApp != null && SteamListApp["applist"] != null && SteamListApp["applist"]["apps"] != null)
                {
                    string SteamAppsListString = JsonConvert.SerializeObject(SteamListApp["applist"]["apps"]);
                    var SteamAppsList = JsonConvert.DeserializeObject<List<SteamApps>>(SteamAppsListString);
                    SteamAppsList.Sort((x, y) => y.AppId.CompareTo(x.AppId));

                    foreach (SteamApps Game in SteamAppsList)
                    {
                        string NameSteam = Common.NormalizeGameName(Game.Name);
                        string NameSearch = Common.NormalizeGameName(Name);

                        if (NameSteam == NameSearch)
                        {
                            return Game.AppId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error with {Name}");
            }
        
            if (SteamId == 0)
            {
                logger.Warn($"SteamId not find for {Name}");
            }
        
            return SteamId;
        }
    }

    public class SteamApps
    {
        [JsonProperty("appid")]
        public int AppId { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
