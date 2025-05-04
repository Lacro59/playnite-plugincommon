using CommonPluginsShared;
using CommonPluginsStores.Steam;
using FuzzySharp;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.PCGamingWiki
{
    public class PCGamingWikiApi
    {
        internal static ILogger Logger => LogManager.GetLogger();

        internal string PluginName { get; }
        internal string ClientName => "PCGamingWiki";
        internal ExternalPlugin PluginLibrary { get; }


        #region Urls
        private string UrlBase => @"https://pcgamingwiki.com";
        private string UrlWithSteamId => UrlBase + @"/api/appid.php?appid={0}";
        private string UrlPCGamingWikiSearch => UrlBase + @"/w/index.php?search=";
        private string UrlPCGamingWikiSearchWithApi => UrlBase + @"/w/api.php?action=opensearch&format=json&formatversion=2&search={0}&namespace=0&limit=10";
        #endregion


        public PCGamingWikiApi(string pluginName, ExternalPlugin pluginLibrary)
        {
            PluginName = pluginName;
            PluginLibrary = pluginLibrary;
        }


        /// <summary>
        /// Get the url for PCGamingWiki from url on Game or with Steam appId of with a search on website.
        /// </summary>
        /// <returns></returns>
        public string FindGoodUrl(Game game)
        {
            try
            {
                string url = string.Empty;

                #region With Steam appId
                uint appId = 0;

                if (game.PluginId == GetPluginId(ExternalPlugin.SteamLibrary))
                {
                    appId = uint.Parse(game.GameId);

                }
                else
                {
                    SteamApi steamApi = new SteamApi(PluginName, ExternalPlugin.CheckLocalizations);
                    appId = steamApi.GetAppId(game);
                }

                if (appId != 0)
                {
                    url = string.Format(UrlWithSteamId, appId);
                    Thread.Sleep(500);
                    string response = Web.DownloadStringData(url).GetAwaiter().GetResult();
                    if (!response.Contains("search results", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Info($"Url for PCGamingWiki find for {game.Name} - {url}");
                        return url;
                    }
                }
                #endregion

                #region With game links
                url = game.Links?
                    .FirstOrDefault(link => link.Url.Contains("pcgamingwiki", StringComparison.OrdinalIgnoreCase) &&
                                            !link.Url.StartsWith(UrlPCGamingWikiSearch, StringComparison.OrdinalIgnoreCase))
                    ?.Url;

                if (!url.IsNullOrEmpty())
                {
                    Logger.Info($"Url for PCGamingWiki find for {game.Name} - {url}");
                    return url;
                }
                #endregion

                #region With PCGamingWiki search
                string name = PlayniteTools.NormalizeGameName(game.Name);

                // Search with release date
                if (game.ReleaseDate != null)
                {
                    url = string.Format(UrlPCGamingWikiSearchWithApi, WebUtility.UrlEncode(name + $" ({((ReleaseDate)game.ReleaseDate).Year})"));
                    url = GetWithSearchApi(url);
                    if (!url.IsNullOrEmpty())
                    {
                        Logger.Info($"Url for PCGamingWiki find for {game.Name} - {url}");
                        return url;
                    }
                }

                // Normal search
                url = string.Format(UrlPCGamingWikiSearchWithApi, WebUtility.UrlEncode(name));
                url = GetWithSearchApi(url);
                if (!url.IsNullOrEmpty())
                {
                    Logger.Info($"Url for PCGamingWiki find for {game.Name} - {url}");
                    return url;
                }
                #endregion
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            Logger.Warn($"Url for PCGamingWiki not find for {game.Name}");
            return string.Empty;
        }

        private string GetWithSearchApi(string url)
        {
            string urlFound = string.Empty;

            try
            {
                string response = Web.DownloadStringData(url).GetAwaiter().GetResult();
                if (Serialization.TryFromJson(response, out dynamic data) && data[3]?.Count > 0)
                {
                    List<string> listName = Serialization.FromJson<List<string>>(Serialization.ToJson(data[1]));
                    List<string> listUrl = Serialization.FromJson<List<string>>(Serialization.ToJson(data[3]));

                    Dictionary<string, string> dataFound = new Dictionary<string, string>();
                    for (int i = 0; i < listName.Count; i++)
                    {
                        dataFound.Add(listName[i], listUrl[i]);
                    }

                    var fuzzList = dataFound.Select(x => new { MatchPercent = Fuzz.Ratio(data[0].ToString().ToLower(), x.Key.ToLower()), Data = x })
                        .OrderByDescending(x => x.MatchPercent)
                        .ToList();

                    urlFound = fuzzList.First().MatchPercent >= 95 ? fuzzList.First().Data.Value : string.Empty;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return urlFound;
        }
    }
}