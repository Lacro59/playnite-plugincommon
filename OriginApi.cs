using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PluginCommon
{
    public class OriginApi
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly string urlOriginListApp = @"https://api3.origin.com/supercat/FR/fr_FR/supercat-PCWIN_MAC-FR-fr_FR.json.gz";
        private readonly List<GameStoreDataResponseAppsList> OriginListApp = new List<GameStoreDataResponseAppsList>();


        public OriginApi(string PluginUserDataPath)
        {
            // Class variable
            string PluginCachePath = PluginUserDataPath + "\\cache\\";
            string PluginCacheFile = PluginCachePath + "\\OriginListApp.json";

            // Load Origin list app
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
                            logger.Info("PluginCommon - GetOriginAppListFromCache");
                            OriginListApp = JsonConvert.DeserializeObject<List<GameStoreDataResponseAppsList>>(File.ReadAllText(PluginCacheFile));
                        }
                        else
                        {
                            OriginListApp = GetOriginAppListFromWeb(PluginCacheFile);
                        }
                    }
                    // From web
                    else
                    {
                        OriginListApp = GetOriginAppListFromWeb(PluginCacheFile);
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

        private List<GameStoreDataResponseAppsList> GetOriginAppListFromWeb(string PluginCacheFile)
        {
            logger.Info("PluginCommon - GetOriginAppListFromWeb");

            string responseData = string.Empty;
            try
            {
                string result = Web.DownloadStringDataWithGz(urlOriginListApp).GetAwaiter().GetResult();
                JObject resultObject = JObject.Parse(result);
                responseData = JsonConvert.SerializeObject(resultObject["offers"]);

                // Write file for cache usage
                File.WriteAllText(PluginCacheFile, responseData);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Failed to load from {urlOriginListApp}");
            }

            return JsonConvert.DeserializeObject<List<GameStoreDataResponseAppsList>>(responseData);
        }

        public string GetOriginId(string Name)
        {
            GameStoreDataResponseAppsList findGame = OriginListApp.Find(x => x.masterTitle.ToLower() == Name.ToLower());

#if DEBUG
            logger.Debug($"PluginCommon - Find Origin data for {Name} - {JsonConvert.SerializeObject(findGame)}");
#endif

            if (findGame != null)
            {
                return findGame.offerId ?? string.Empty;
            }

            return string.Empty;
        }
    }

    public class GameStoreDataResponseAppsList
    {
        public string offerId;
        public string offerType;
        public string masterTitleId;
        public string publisherFacetKey;
        public string developerFacetKey;
        public string genreFacetKey;
        public string imageServer;
        public string itemName;
        public string itemType;
        public string itemId;
        public string offerPath;
        public string masterTitle;
    }
}
