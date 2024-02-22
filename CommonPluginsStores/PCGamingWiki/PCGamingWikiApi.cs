using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CommonPluginsStores.PCGamingWiki
{
    public class PCGamingWikiApi
    {
        internal static ILogger Logger => LogManager.GetLogger();
        internal static IResourceProvider ResourceProvider => new ResourceProvider();

        internal string PluginName { get; set; }
        internal string ClientName => "PCGamingWiki";


        #region Url
        private string UrlBase => @"https://pcgamingwiki.com";
        private string UrlWithSteamId => UrlBase + @"/api/appid.php?appid={0}";
        private string UrlPCGamingWikiSearch => UrlBase + @"/w/index.php?search=";
        private string UrlPCGamingWikiSearchWithApi => UrlBase + @"/w/api.php?action=opensearch&format=json&formatversion=2&search={0}&namespace=0&limit=10";
        #endregion


        public PCGamingWikiApi(string pluginName)
        {
            PluginName = pluginName;
        }


        public string FindGoodUrl(Game game, int SteamId = 0)
        {
            string url = string.Empty;
            string urlMatch = string.Empty;
            string WebResponse = string.Empty;

            if (SteamId != 0)
            {
                url = string.Format(UrlWithSteamId, SteamId);

                Thread.Sleep(500);
                WebResponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                if (!WebResponse.ToLower().Contains("search results"))
                {
                    return url;
                }
            }


            url = string.Empty;
            if (game.Links != null)
            {
                foreach (Link link in game.Links)
                {
                    if (link.Url.ToLower().Contains("pcgamingwiki"))
                    {
                        url = link.Url;

                        if (url.ToLower().Contains(@"http://pcgamingwiki.com/w/index.php?search="))
                        {
                            url = UrlPCGamingWikiSearch + WebUtility.UrlEncode(url.Replace(@"http://pcgamingwiki.com/w/index.php?search=", string.Empty));
                        }
                        if (url.Length == UrlPCGamingWikiSearch.Length)
                        {
                            url = string.Empty;
                        }
                    }
                }

                if (!url.IsNullOrEmpty())
                {
                    Thread.Sleep(500);
                    WebResponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                    if (!WebResponse.ToLower().Contains("search results"))
                    {
                        return url;
                    }
                    else
                    {
                        urlMatch = GetUrlIsOneResult(WebResponse);
                        if (!urlMatch.IsNullOrEmpty())
                        {
                            return urlMatch;
                        }
                    }
                }
            }


            string Name = Regex.Replace(game.Name, @"([ ]demo\b)", string.Empty, RegexOptions.IgnoreCase);
            Name = Regex.Replace(Name, @"(demo[ ])", string.Empty, RegexOptions.IgnoreCase);
            Name = CommonPluginsShared.PlayniteTools.NormalizeGameName(Name);


            // Search with release date
            if (game.ReleaseDate != null) 
            {
                url = string.Format(UrlPCGamingWikiSearchWithApi, WebUtility.UrlEncode(Name + $" ({((ReleaseDate)game.ReleaseDate).Year})"));
                urlMatch = GetWithSearchApi(url);
                if (!urlMatch.IsNullOrEmpty())
                {
                    return urlMatch;
                }
            }

            // Normal search
            url = string.Format(UrlPCGamingWikiSearchWithApi, WebUtility.UrlEncode(Name));
            urlMatch = GetWithSearchApi(url);
            if (!urlMatch.IsNullOrEmpty())
            {
                return urlMatch;
            }


            // old method
            url = UrlPCGamingWikiSearch + WebUtility.UrlEncode(Name);
            Thread.Sleep(500);
            WebResponse = Web.DownloadStringData(url).GetAwaiter().GetResult(); 
            if (WebResponse.ToLower().Contains("database query error has occurred"))
            {
                Logger.Warn($"Error on PCGamingWiki search with {Name}");
            }
            else
            {
                if (!WebResponse.ToLower().Contains("search results"))
                {
                    return url;
                }
                else
                {
                    urlMatch = GetUrlIsOneResult(WebResponse);
                    if (!urlMatch.IsNullOrEmpty())
                    {
                        return urlMatch;
                    }
                }
            }


            url = UrlPCGamingWikiSearch + WebUtility.UrlEncode(game.Name);
            Thread.Sleep(500);
            WebResponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
            if (WebResponse.ToLower().Contains("database query error has occurred"))
            {
                Logger.Warn($"Error on PCGamingWiki search with {game.Name}");
            }
            else
            {
                if (!WebResponse.ToLower().Contains("search results"))
                {
                    return url;
                }
                else
                {
                    urlMatch = GetUrlIsOneResult(WebResponse);
                    if (!urlMatch.IsNullOrEmpty())
                    {
                        return urlMatch;
                    }
                }
            }


            return string.Empty;
        }


        private string GetWithSearchApi(string url)
        {
            string urlFound = string.Empty;

            try
            {
                string WebResponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebResponse, out dynamic data);

                if (data != null && data[3]?.Count > 0)
                {
                    urlFound = data[3][0];
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return urlFound;
        }


        private string GetUrlIsOneResult(string WebResponse)
        {
            string url = string.Empty;

            try
            {
                if (!WebResponse.Contains("There were no results matching the query"))
                {
                    HtmlParser parser = new HtmlParser();
                    IHtmlDocument HtmlDocument = parser.Parse(WebResponse);

                    if (HtmlDocument.QuerySelectorAll("ul.mw-search-results")?.Count() == 2)
                    {
                        IHtmlCollection<IElement> TitleMatches = HtmlDocument.QuerySelectorAll("ul.mw-search-results")[0].QuerySelectorAll("li");
                        if (TitleMatches?.Count() == 1)
                        {
                            url = UrlBase + TitleMatches[0].QuerySelector("a").GetAttribute("href");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return url;
        }
    }
}
