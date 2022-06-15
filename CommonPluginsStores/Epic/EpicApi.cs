using CommonPlayniteShared;
using CommonPlayniteShared.PluginLibrary.EpicLibrary;
using CommonPlayniteShared.PluginLibrary.EpicLibrary.Models;
using CommonPlayniteShared.PluginLibrary.EpicLibrary.Services;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Epic.Models;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Epic
{
    public class EpicApi : StoreApi
    {
        #region Url
        private const string UrlBase = @"https://www.epicgames.com";

        private string UrlStore = UrlBase + @"/store/{0}/p/{1}";
        private string UrlAchievements = UrlBase + @"/store/{0}/achievements/{1}";

        private string UrlGraphQL = @"https://graphql.epicgames.com/graphql";
        #endregion


        protected static EpicAccountClient _EpicAPI;
        internal static EpicAccountClient EpicAPI
        {
            get
            {
                if (_EpicAPI == null)
                {
                    _EpicAPI = new EpicAccountClient(
                        API.Instance,
                        PlaynitePaths.ExtensionsDataPath + "\\00000002-DBD1-46C6-B5D0-B1BA559D10E4\\tokens.json"
                    );
                }
                return _EpicAPI;
            }

            set => _EpicAPI = value;
        }


        public bool forced { get; set; } = false;


        public EpicApi(string PluginName) : base(PluginName, ExternalPlugin.EpicLibrary, "Epic")
        {

        }


        #region Cookies
        internal override List<HttpCookie> GetWebCookies()
        {
            string LocalLangShort = CodeLang.GetGogLang(Local);
            List<HttpCookie> httpCookies = new List<HttpCookie>
            {
                new HttpCookie
                {
                    Domain = ".www.epicgames.com",
                    Name = "EPIC_LOCALE_COOKIE",
                    Value = LocalLangShort
                },
                new HttpCookie
                {
                    Domain = ".www.epicgames.com",
                    Name = "EPIC_EG1",
                    Value = AuthToken.Token
                },
                new HttpCookie
                {
                    Domain = "store.epicgames.com",
                    Name = "EPIC_LOCALE_COOKIE",
                    Value = LocalLangShort
                },
                new HttpCookie
                {
                    Domain = "store.epicgames.com",
                    Name = "EPIC_EG1",
                    Value = AuthToken.Token
                }
            };
            return httpCookies;
        }
        #endregion


        #region Configuration
        protected override bool GetIsUserLoggedIn()
        {
            bool isLogged = EpicAPI.GetIsUserLoggedIn();

            if (isLogged)
            {
                OauthResponse tokens = EpicAPI.loadTokens();
                AuthToken = new StoreToken
                {
                    Token = tokens.access_token,
                    Type = tokens.token_type
                };
            }
            else
            {
                AuthToken = null;
            }

            return isLogged;
        }
        #endregion


        #region Current user
        // TODO 
        protected override AccountInfos GetCurrentAccountInfos()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                AccountInfos accountInfos = new AccountInfos{ IsCurrent = true };
                return accountInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }
        #endregion


        #region User details
        // TODO
        public override ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                ObservableCollection<AccountGameInfos> accountGamesInfos = new ObservableCollection<AccountGameInfos>();
                return accountGamesInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override ObservableCollection<GameAchievement> GetAchievements(string Id, AccountInfos accountInfos)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            { 
                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();

                string Url = string.Empty;
                string ResultWeb = string.Empty;
                string LocalLang = CodeLang.GetEpicLang(Local);
                string LocalLangShort = CodeLang.GetGogLang(Local);

                try
                {
                    string ProductSlug = GetProductSlug(PlayniteTools.NormalizeGameName(Id));
                    if (ProductSlug.IsNullOrEmpty())
                    {
                        logger.Warn($"No ProductSlug for {Id}");
                        return null;
                    }

                    List<HttpCookie> Cookies = GetStoredCookies();
                    Url = string.Format(UrlAchievements, LocalLang, ProductSlug);
                    //ResultWeb = Web.DownloadStringData(Url, Cookies).GetAwaiter().GetResult();

                    using (var WebViews = API.Instance.WebViews.CreateOffscreenView())
                    {
                        Cookies.ForEach(x => { WebViews.SetCookies(Url, x); });
                        WebViews.NavigateAndWait(Url);
                        ResultWeb = WebViews.GetPageSource();
                    }

                    if (!ResultWeb.Contains("lang=\"" + LocalLang + "\"", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Url = string.Format(UrlAchievements, LocalLangShort, ProductSlug);
                        ResultWeb = Web.DownloadStringData(Url, Cookies).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404"))
                    {
                        logger.Warn($"Error 404 for {Id}");
                    }
                    else
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    return null;
                }

                if (!ResultWeb.Contains("\"achievements\":[{\"achievement\""))
                {
                    if (!forced)
                    {
                        forced = true;
                        ObservableCollection<GameAchievement> data = GetAchievements(Id, accountInfos);
                        forced = false;
                        return data;
                    }

                    logger.Warn($"Error 404 for {Id}");
                    return null;
                }

                if (ResultWeb != string.Empty && !ResultWeb.Contains("<span>404</span>", StringComparison.InvariantCultureIgnoreCase))
                {
                    try
                    {
                        int indexStart = ResultWeb.IndexOf("window.__REACT_QUERY_INITIAL_QUERIES__ =");
                        int indexEnd = ResultWeb.IndexOf("window.server_rendered");

                        string stringStart = ResultWeb.Substring(0, indexStart + "window.__REACT_QUERY_INITIAL_QUERIES__ =".Length);
                        string stringEnd = ResultWeb.Substring(indexEnd);

                        int length = ResultWeb.Length - stringStart.Length - stringEnd.Length;

                        string JsonDataString = ResultWeb.Substring(
                            indexStart + "window.__REACT_QUERY_INITIAL_QUERIES__ =".Length,
                            length
                        );

                        indexEnd = JsonDataString.IndexOf("}]};");
                        length = JsonDataString.Length - (JsonDataString.Length - indexEnd - 3);
                        JsonDataString = JsonDataString.Substring(0, length);

                        EpicData epicData = Serialization.FromJson<EpicData>(JsonDataString);

                        // Achievements data
                        Query achievemenstData = epicData.queries
                                .Where(x => (Serialization.ToJson(x.state.data)).Contains("\"achievements\":[{\"achievement\"", StringComparison.InvariantCultureIgnoreCase))
                                .FirstOrDefault();

                        EpicAchievementsData epicAchievementsData = Serialization.FromJson<EpicAchievementsData>(Serialization.ToJson(achievemenstData.state.data));

                        if (epicAchievementsData != null && epicAchievementsData.Achievement.productAchievementsRecordBySandbox.achievements?.Count > 0)
                        {
                            foreach (var ach in epicAchievementsData.Achievement.productAchievementsRecordBySandbox.achievements)
                            {
                                GameAchievement gameAchievement = new GameAchievement
                                {
                                    Id = ach.achievement.name,
                                    Name = ach.achievement.unlockedDisplayName,
                                    Description = ach.achievement.unlockedDescription,
                                    UrlUnlocked = ach.achievement.unlockedIconLink,
                                    UrlLocked = ach.achievement.lockedIconLink,
                                    DateUnlocked = default(DateTime),
                                    Percent = ach.achievement.rarity.percent
                                };
                                gameAchievements.Add(gameAchievement);
                            }
                        }

                        // Owned achievement
                        var achievemenstOwnedData = epicData.queries
                            .Where(x => (Serialization.ToJson(x.state.data)).Contains("\"playerAchievements\":[{\"playerAchievement\"", StringComparison.InvariantCultureIgnoreCase))
                            .FirstOrDefault();

                        if (achievemenstOwnedData != null)
                        {
                            string dataAch = Serialization.ToJson(achievemenstOwnedData.state.data).Replace("\"N/A\"", "null");
                            EpicAchievementsOwnedData epicAchievementsOwnedData = Serialization.FromJson<EpicAchievementsOwnedData>(dataAch);

                            if (epicAchievementsOwnedData != null && epicAchievementsOwnedData.PlayerAchievement.playerAchievementGameRecordsBySandbox.records.FirstOrDefault()?.playerAchievements?.Count() > 0)
                            {
                                foreach (var ach in epicAchievementsOwnedData.PlayerAchievement.playerAchievementGameRecordsBySandbox.records.FirstOrDefault().playerAchievements)
                                {
                                    GameAchievement owned = gameAchievements.Where(x => x.Id.IsEqual(ach.playerAchievement.achievementName))?.FirstOrDefault();
                                    if (owned != null)
                                    {
                                        owned.DateUnlocked = ach.playerAchievement?.unlockDate ?? default(DateTime);
                                    }
                                    else
                                    {
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                        return null;
                    }
                }
                else
                {
                    logger.Warn($"Error 404 for {Id}");
                }

                return gameAchievements;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override SourceLink GetAchievementsSourceLink(string Name, string Id, AccountInfos accountInfos)
        {
            string ProductSlug = GetProductSlug(PlayniteTools.NormalizeGameName(Name));
            if (ProductSlug.IsNullOrEmpty())
            {
                logger.Warn($"No ProductSlug for {Name}");
                return null;
            }

            string LocalLang = CodeLang.GetEpicLang(Local);
            string Url = string.Format(UrlAchievements, LocalLang, ProductSlug);

            return new SourceLink
            {
                GameName = Name,
                Name = ClientName,
                Url = Url
            };
        }
        #endregion


        #region Game
        // TODO
        public override GameInfos GetGameInfos(string Id, AccountInfos accountInfos)
        {
            try
            {

            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override ObservableCollection<DlcInfos> GetDlcInfos(string Id, AccountInfos accountInfos)
        {
            try
            {
                string LocalLang = CodeLang.GetEpicLang(Local);
                ObservableCollection<DlcInfos> Dlcs = new ObservableCollection<DlcInfos>();

                // List DLC
                EpicAddonsByNamespace dataDLC = GetAddonsByNamespace(Id).GetAwaiter().GetResult();
                if (dataDLC?.data?.Catalog?.catalogOffers?.elements == null)
                {
                    logger.Warn($"No dlc for {Id}");
                    return null;
                }

                foreach (Element el in dataDLC?.data?.Catalog?.catalogOffers?.elements)
                {
                    bool IsOwned = false;
                    if (accountInfos != null && accountInfos.IsCurrent)
                    {
                        IsOwned = DlcIsOwned(Id, el.id);
                    }
                    
                    DlcInfos dlc = new DlcInfos
                    {
                        Id = el.id,
                        Name = el.title,
                        Description = el.description,
                        Image = el.keyImages.Find(x => x.type.IsEqual("OfferImageWide")).url.Replace("\u002F", "/"),
                        Link = string.Format(UrlStore, LocalLang, el.urlSlug),
                        IsOwned = IsOwned,
                        Price = el.price.totalPrice.fmtPrice.discountPrice,
                        PriceBase = el.price.totalPrice.fmtPrice.originalPrice,
                    };

                    Dlcs.Add(dlc);
                }

                return Dlcs;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }
        #endregion


        #region Epic
        public string GetProductSlug(string Name)
        {
            string ProductSlug = string.Empty;

            try
            {
                using (WebStoreClient client = new WebStoreClient())
                {
                    var catalogs = client.QuerySearch(Name).GetAwaiter().GetResult();
                    if (catalogs.HasItems())
                    {
                        catalogs = catalogs.OrderBy(x => x.title.Length).ToList();
                        var catalog = catalogs.FirstOrDefault(a => a.title.IsEqual(Name, true));
                        if (catalog == null)
                        {
                            catalog = catalogs[0];
                        }

                        if (catalog.productSlug.IsNullOrEmpty())
                        {
                            var mapping = catalog.catalogNs.mappings.FirstOrDefault(b => b.pageType.Equals("productHome", StringComparison.InvariantCultureIgnoreCase));
                            catalog.productSlug = mapping.pageSlug;
                        }

                        ProductSlug = catalog?.productSlug?.Replace("/home", string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return ProductSlug;
        }

        public string GetNameSpace(string Name)
        {
            string NameSpace = string.Empty;

            try {
                using (WebStoreClient client = new WebStoreClient())
                {
                    var catalogs = client.QuerySearch(Name).GetAwaiter().GetResult();
                    if (catalogs.HasItems())
                    {
                        catalogs = catalogs.OrderBy(x => x.title.Length).ToList();
                        var catalog = catalogs.FirstOrDefault(a => a.title.IsEqual(Name, true));
                        if (catalog == null)
                        {
                            catalog = catalogs[0];
                        }

                        NameSpace = catalog?.nameSpace;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return NameSpace;
        }


        private bool DlcIsOwned(string productNameSpace, string Id)
        {
            try
            {
                EpicEntitledOfferItems ownedDLC = GetEntitledOfferItems(productNameSpace, Id, AuthToken.Token).GetAwaiter().GetResult();
                return (ownedDLC?.data?.Launcher?.entitledOfferItems?.entitledToAllItemsInOffer ?? false) && (ownedDLC?.data?.Launcher?.entitledOfferItems?.entitledToAnyItemInOffer ?? false);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return false;
            }
        }


        private async Task<EpicAddonsByNamespace> GetAddonsByNamespace(string epic_namespace)
        {
            try
            {
                var query = new QueryAddonsByNamespace();
                query.variables.epic_namespace = epic_namespace;
                query.variables.locale = CodeLang.GetEpicLang(Local); ;
                query.variables.country = CodeLang.GetOriginLangCountry(Local);
                var content = new StringContent(Serialization.ToJson(query), Encoding.UTF8, "application/json");
                HttpClient httpClient = new HttpClient();
                var response = await httpClient.PostAsync(UrlGraphQL, content);
                var str = await response.Content.ReadAsStringAsync();
                var data = Serialization.FromJson<EpicAddonsByNamespace>(str);
                return data;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }

        private async Task<EpicEntitledOfferItems> GetEntitledOfferItems(string productNameSpace, string offerId, string token)
        {
            try
            {
                var query = new QueryEntitledOfferItems();
                query.variables.productNameSpace = productNameSpace;
                query.variables.offerId = offerId;
                var content = new StringContent(Serialization.ToJson(query), Encoding.UTF8, "application/json");
                string str = await Web.PostStringData(UrlGraphQL, token, content);
                var data = Serialization.FromJson<EpicEntitledOfferItems>(str);
                return data;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }
        #endregion
    }
}
