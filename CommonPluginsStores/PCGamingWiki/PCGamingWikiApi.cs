using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using Playnite.SDK;
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
        internal static readonly ILogger logger = LogManager.GetLogger();
        internal static readonly IResourceProvider resources = new ResourceProvider();

        internal string PluginName { get; set; }
        internal string ClientName => "PCGamingWiki";


        #region Url
        private const string UrlBase = @"https://pcgamingwiki.com";
        private const string UrlWithSteamId = UrlBase + @"/api/appid.php?appid={0}";
        private const string UrlPCGamingWikiSearch = UrlBase + @"/w/index.php?search=";
        #endregion



        public PCGamingWikiApi(string PluginName)
        {
            this.PluginName = PluginName;
        }


        public string FindGoodUrl(Game game, int SteamId = 0)
        {
            string url = string.Empty;
            string urlMatch = string.Empty;
            string WebResponse = string.Empty;

            if (SteamId != 0)
            {
                url = string.Format(UrlWithSteamId, SteamId);

                Thread.Sleep(1000);
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
                    Thread.Sleep(1000);
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

            url = UrlPCGamingWikiSearch + WebUtility.UrlEncode(Name);

            Thread.Sleep(1000);
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


            url = UrlPCGamingWikiSearch + WebUtility.UrlEncode(game.Name);

            Thread.Sleep(1000);
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


            return string.Empty;
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
