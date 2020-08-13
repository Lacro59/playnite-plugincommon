using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PluginCommon
{
    public class SteamApi
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly string urlSteamListApp = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";

        private string PluginCachePath;

        private JObject SteamListApp = new JObject();


        internal async Task<string> DownloadStringData(string url)
        {
            logger.Debug($"PluginCommon - Download {url}");

            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
                string responseData = await client.GetStringAsync(url).ConfigureAwait(false);
                return responseData;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", "");
                return null;
            }
        }

        public SteamApi(string PluginUserDataPath)
        {
            // Class variable
            PluginCachePath = PluginUserDataPath + "\\cache\\";
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
            string responseData = DownloadStringData(urlSteamListApp).GetAwaiter().GetResult();
            if (responseData.IsNullOrEmpty() && responseData!= "{\"applist\":{\"apps\":[]}}")
            {
                responseData = JsonConvert.SerializeObject(new JObject());
            }
            else
            {
                File.WriteAllText(PluginCacheFile, responseData);
            }
            return JObject.Parse(responseData);
        }

        private string RemoveTrademarks(string str, string remplacement = "")
        {
            if (str.IsNullOrEmpty())
            {
                return str;
            }

            return Regex.Replace(str, @"[™©®]", remplacement);
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
            newName = RemoveTrademarks(newName);
            newName = newName.Replace('’', '\'');

            newName = newName.Replace(":", "");
            newName = newName.Replace("-", "");
            newName = newName.Replace("-", "");
            newName = newName.Replace("goty", "");
            newName = newName.Replace("game of the year edition", "");
            newName = newName.Replace("  ", " ");
            newName = newName.Replace("  ", " ");
            newName = newName.Replace("  ", " ");
            newName = newName.Replace("  ", " ");
            newName = newName.Replace("  ", " ");
            newName = newName.Replace("  ", " ");
            newName = newName.Replace("  ", " ");

            return newName.Trim();
        }

        public int GetSteamId(string Name, bool IsLoop1 = false, bool IsLoop2 = false, bool IsLoop3 = false)
        {
            int SteamId = 0;

            try
            {
                foreach (JObject Game in SteamListApp["applist"]["apps"])
                {
                    string NameSteam = NormalizeGameName((string)Game["name"]);
                    string NameSearch = NormalizeGameName(Name);

                    if (NameSteam == NameSearch)
                    {
                        return (int)Game["appid"];
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", "Error on GetSteamId for {Name}");
            }

            //if (!IsLoop1)
            //{
            //    SteamId = GetSteamId(Name.Replace("  ", " "), true);
            //    if ((SteamId == 0) && (!IsLoop2))
            //    {
            //        SteamId = GetSteamId(Name.Replace(":", ""), true, true);
            //    }
            //
            //    if ((SteamId == 0) && (!IsLoop3))
            //    {
            //        SteamId = GetSteamId(Name + "™", true, true, true);
            //    }
            //
            //    return SteamId;
            //}

            if (SteamId == 0)
            {
                logger.Info($"PluginCommon - SteamId not find for {Name}");
            }

            return SteamId;
        }
    }
}
