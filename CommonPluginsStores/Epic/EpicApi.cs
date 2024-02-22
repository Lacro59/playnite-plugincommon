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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static CommonPlayniteShared.PluginLibrary.EpicLibrary.Models.WebStoreModels.QuerySearchResponse.Data.CatalogItem.SearchStore;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Epic
{
    public class EpicApi : StoreApi
    {
        #region Url
        private string UrlBase => @"https://www.epicgames.com";

        private string UrlStore => UrlBase + @"/store/{0}/p/{1}";
        private string UrlAchievements => UrlBase + @"/store/{0}/achievements/{1}";

        private string UrlGraphQL => @"https://graphql.epicgames.com/graphql";
        #endregion

        protected static EpicAccountClient _EpicAccountClient;
        internal static EpicAccountClient EpicAccountClient
        {
            get
            {
                if (_EpicAccountClient == null)
                {
                    _EpicAccountClient = new EpicAccountClient(
                        API.Instance,
                        PlaynitePaths.ExtensionsDataPath + "\\00000002-DBD1-46C6-B5D0-B1BA559D10E4\\tokens.json"
                    );
                }
                return _EpicAccountClient;
            }

            set => _EpicAccountClient = value;
        }

        public bool Forced { get; set; } = false;


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
            bool isLogged = EpicAccountClient.GetIsUserLoggedIn();

            if (isLogged)
            {
                OauthResponse tokens = EpicAccountClient.loadTokens();
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
                AccountInfos userInfos = new AccountInfos
                {
                    //UserId = long.Parse(player.steamid),
                    //Avatar = player.avatarfull,
                    //Pseudo = player.personaname,
                    //Link = player.profileurl,
                    IsCurrent = true
                };
                return userInfos;
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
                        Logger.Warn($"No ProductSlug for {Id}");
                        return null;
                    }

                    List<HttpCookie> Cookies = GetStoredCookies();
                    Url = string.Format(UrlAchievements, LocalLang, ProductSlug);
                    using (IWebView WebViews = API.Instance.WebViews.CreateOffscreenView())
                    {
                        _ = WebViews.CanExecuteJavascriptInMainFrame;
                        Cookies.ForEach(x => { WebViews.SetCookies(Url, x); });
                        WebViews.NavigateAndWait(Url);
                        ResultWeb = WebViews.GetPageSource();

                        if (ResultWeb.Contains("data-translate=\"please_wait\"", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Common.LogDebug(true, "Checking browser...");
                            ResetIsUserLoggedIn();
                            return null;
                        }
                    }

                    if (!ResultWeb.Contains("lang=\"" + LocalLang + "\"", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Url = string.Format(UrlAchievements, LocalLangShort, ProductSlug);
                        using (var WebViews = API.Instance.WebViews.CreateOffscreenView())
                        {
                            _ = WebViews.CanExecuteJavascriptInMainFrame;
                            Cookies.ForEach(x => { WebViews.SetCookies(Url, x); });
                            WebViews.NavigateAndWait(Url);
                            ResultWeb = WebViews.GetPageSource();

                            if (ResultWeb.Contains("data-translate=\"please_wait\"", StringComparison.InvariantCultureIgnoreCase))
                            {
                                Common.LogDebug(true, "Checking browser...");
                                ResetIsUserLoggedIn();
                                return null;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404"))
                    {
                        Logger.Warn($"Error 404 for {Id}");
                    }
                    else
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    return null;
                }

                if (!ResultWeb.Contains("\"achievements\":[{\"achievement\""))
                {
                    if (!Forced)
                    {
                        Forced = true;
                        ObservableCollection<GameAchievement> data = GetAchievements(Id, accountInfos);
                        Forced = false;
                        return data;
                    }

                    Logger.Warn($"Error 404 for {Id}");
                    return null;
                }

                if (ResultWeb != string.Empty && !ResultWeb.Contains("<span>404</span>", StringComparison.InvariantCultureIgnoreCase))
                {
                    try
                    {
                        string JsonDataString = Tools.GetJsonInString(ResultWeb, "window.__REACT_QUERY_INITIAL_QUERIES__ =", "window.server_rendered", "}]};");
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
                    Logger.Warn($"Error 404 for {Id}");
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
                Logger.Warn($"No ProductSlug for {Name}");
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

        public override ObservableCollection<AccountWishlist> GetWishlist(AccountInfos accountInfos)
        {
            if (accountInfos != null)
            {
                try
                {
                    string query = "query wishlistQuery { Wishlist { wishlistItems { elements { id order created offerId updated namespace offer {id title offerType effectiveDate expiryDate status isCodeRedemptionOnly keyImages { type url width height }catalogNs { mappings(pageType: \"productHome\") { pageSlug pageType } } offerMappings { pageSlug pageType } } } } } }";
                    dynamic variables = new { };
                    string response = QueryWishList(query, variables).GetAwaiter().GetResult();

                    if (!response.IsNullOrEmpty() && Serialization.TryFromJson(response, out EpicWishlistData epicWishlistData))
                    {
                        if (epicWishlistData?.data?.Wishlist?.wishlistItems?.elements != null)
                        {
                            ObservableCollection<AccountWishlist> data = new ObservableCollection<AccountWishlist>();

                            foreach (WishlistElement gameWishlist in epicWishlistData.data.Wishlist.wishlistItems.elements)
                            {
                                string Id = string.Empty;
                                string Name = string.Empty;
                                DateTime? Released = null;
                                DateTime? Added = null;
                                string Image = string.Empty;
                                string Link = string.Empty;

                                try
                                {
                                    Id = gameWishlist.offerId + "|" + gameWishlist.@namespace;
                                    Name = WebUtility.HtmlDecode(gameWishlist.offer.title);
                                    Image = gameWishlist.offer.keyImages?.FirstOrDefault(x => x.type.IsEqual("Thumbnail"))?.url;
                                    Released = gameWishlist.offer.effectiveDate.ToUniversalTime();
                                    Added = gameWishlist.created.ToUniversalTime();
                                    Link = gameWishlist.offer?.catalogNs?.mappings?.FirstOrDefault()?.pageSlug;

                                    data.Add(new AccountWishlist
                                    {
                                        Id = Id,
                                        Name = Name,
                                        Link = Link.IsNullOrEmpty() ? string.Empty : string.Format(UrlStore, CodeLang.GetEpicLang(Local), Link),
                                        Released = Released,
                                        Added = Added,
                                        Image = Image
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Common.LogError(ex, true, $"Error in parse {ClientName} wishlist - {Name}");
                                }
                            }

                            return data;
                        }

                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error in parse {ClientName} wishlist", true, PluginName);
                }
            }

            return null;
        }

        public override bool RemoveWishlist(string Id)
        {
            if (IsUserLoggedIn)
            {
                try
                {
                    string EpicOfferId = Id.Split('|')[0];
                    string EpicNamespace = Id.Split('|')[1];

                    string query = @"mutation removeFromWishlistMutation($namespace: String!, $offerId: String!, $operation: RemoveOperation!) { Wishlist { removeFromWishlist(namespace: $namespace, offerId: $offerId, operation: $operation) { success } } }";
                    dynamic variables = new
                    {
                        @namespace = EpicNamespace,
                        offerId = EpicOfferId,
                        operation = "REMOVE"
                    };
                    string ResultWeb = QueryWishList(query, variables).GetAwaiter().GetResult();
                    return ResultWeb.IndexOf("\"success\":true") > -1;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error remove {Id} in {ClientName} wishlist", true, PluginName);
                }
            }

            return false;
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
                    Logger.Warn($"No dlc for {Id}");
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
                        Image = el.keyImages?.Find(x => x.type.IsEqual("OfferImageWide"))?.url?.Replace("\u002F", "/"),
                        Link = string.Format(UrlStore, LocalLang, el.urlSlug),
                        IsOwned = IsOwned,
                        Price = el.price?.totalPrice?.fmtPrice?.discountPrice,
                        PriceBase = el.price?.totalPrice?.fmtPrice?.originalPrice,
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
            if (Name.IsEqual("warhammer 40 000 mechanicus"))
            {
                Name = "warhammer mechanicus";
            }
            else if (Name.IsEqual("death stranding"))
            {
                return "death-stranding";
            }

            string ProductSlug = string.Empty;

            try
            {
                using (WebStoreClient client = new WebStoreClient())
                {
                    List<SearchStoreElement> catalogs = client.QuerySearch(Name).GetAwaiter().GetResult();
                    if (catalogs.HasItems())
                    {
                        catalogs = catalogs.OrderBy(x => x.title.Length).ToList();
                        SearchStoreElement catalog = catalogs.FirstOrDefault(a => a.title.IsEqual(Name, true));
                        if (catalog == null)
                        {
                            catalog = catalogs[0];
                        }

                        if (catalog.productSlug.IsNullOrEmpty())
                        {
                            SearchStoreElement.CatalogNs.Mappings mapping = catalog.catalogNs.mappings.FirstOrDefault(b => b.pageType.Equals("productHome", StringComparison.InvariantCultureIgnoreCase));
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

            try
            {
                using (WebStoreClient client = new WebStoreClient())
                {
                    List<SearchStoreElement> catalogs = client.QuerySearch(Name).GetAwaiter().GetResult();
                    if (catalogs.HasItems())
                    {
                        catalogs = catalogs.OrderBy(x => x.title.Length).ToList();
                        SearchStoreElement catalog = catalogs.FirstOrDefault(a => a.title.IsEqual(Name, true));
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
                EpicEntitledOfferItems ownedDLC = GetEntitledOfferItems(productNameSpace, Id).GetAwaiter().GetResult();
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
                QueryAddonsByNamespace query = new QueryAddonsByNamespace();
                query.variables.epic_namespace = epic_namespace;
                query.variables.locale = CodeLang.GetEpicLang(Local);
                query.variables.country = CodeLang.GetOriginLangCountry(Local);
                StringContent content = new StringContent(Serialization.ToJson(query), Encoding.UTF8, "application/json");
                HttpClient httpClient = new HttpClient();
                HttpResponseMessage response = await httpClient.PostAsync(UrlGraphQL, content);
                string str = await response.Content.ReadAsStringAsync();
                EpicAddonsByNamespace data = Serialization.FromJson<EpicAddonsByNamespace>(str);
                return data;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }

        private async Task<EpicEntitledOfferItems> GetEntitledOfferItems(string productNameSpace, string offerId)
        {
            try
            {
                QueryEntitledOfferItems query = new QueryEntitledOfferItems();
                query.variables.productNameSpace = productNameSpace;
                query.variables.offerId = offerId;
                StringContent content = new StringContent(Serialization.ToJson(query), Encoding.UTF8, "application/json");
                string str = await Web.PostStringData(UrlGraphQL, AuthToken.Token, content);
                EpicEntitledOfferItems data = Serialization.FromJson<EpicEntitledOfferItems>(str);
                return data;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }

        public async Task<string> QueryWishList(string query, dynamic variables)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AuthToken.Token);

                var queryObject = new
                {
                    query = query,
                    variables = variables
                };
                StringContent content = new StringContent(Serialization.ToJson(queryObject), Encoding.UTF8, "application/json");
                string str = await Web.PostStringData(UrlGraphQL, AuthToken.Token, content);

                return str;
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
