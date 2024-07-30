using CommonPlayniteShared;
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

        protected static EpicAccountClient epicAccountClient;
        internal static EpicAccountClient EpicAccountClient
        {
            get
            {
                if (epicAccountClient == null)
                {
                    epicAccountClient = new EpicAccountClient(
                        API.Instance,
                        PlaynitePaths.ExtensionsDataPath + "\\" + GetPluginId(ExternalPlugin.EpicLibrary) + "\\tokens.json"
                    );
                }
                return epicAccountClient;
            }

            set => epicAccountClient = value;
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
                OauthResponse tokens = EpicAccountClient.loadTokens();
                AccountInfos userInfos = new AccountInfos
                {
                    UserId = tokens?.account_id,
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

        /// <summary>
        /// Get a list of a game's achievements with a user's possessions.
        /// </summary>
        /// <param name="id">nameSpace</param>
        /// <param name="accountInfos"></param>
        /// <returns></returns>
        public override ObservableCollection<GameAchievement> GetAchievements(string id, AccountInfos accountInfos)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();

                string url = string.Empty;
                string resultWeb = string.Empty;
                string localLang = CodeLang.GetEpicLang(Local);
                string localLangShort = CodeLang.GetEpicLangCountry(Local);


                EpicAchievementResponse epicAchievementResponse = QueryAchievement(id, localLangShort).GetAwaiter().GetResult();
                epicAchievementResponse.Data.Achievement?.ProductAchievementsRecordBySandbox?.Achievements?.ForEach(x =>
                {
                    GameAchievement gameAchievement = new GameAchievement
                    {
                        Id = x.Achievement.Name,
                        Name = x.Achievement.UnlockedDisplayName,
                        Description = x.Achievement.UnlockedDescription,
                        UrlUnlocked = x.Achievement.UnlockedIconLink,
                        UrlLocked = x.Achievement.LockedIconLink,
                        DateUnlocked = default,
                        Percent = x.Achievement.Rarity.Percent
                    };
                    gameAchievements.Add(gameAchievement);
                });

                EpicPlayerAchievementResponse epicPlayerAchievementResponse = QueryPlayerAchievement(accountInfos.UserId, id).GetAwaiter().GetResult();
                epicPlayerAchievementResponse?.Data?.PlayerAchievement?.PlayerAchievementGameRecordsBySandbox?.Records?.FirstOrDefault().PlayerAchievements.ForEach(x =>
                {
                    GameAchievement owned = gameAchievements.Where(y => y.Id.IsEqual(x.PlayerAchievement.AchievementName))?.FirstOrDefault();
                    if (owned != null)
                    {
                        owned.DateUnlocked = x.PlayerAchievement?.UnlockDate ?? default;
                    }
                });

                return gameAchievements;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }


        /// <summary>
        /// Get achievements SourceLink.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id">productSlug</param>
        /// <param name="accountInfos"></param>
        /// <returns></returns>
        public override SourceLink GetAchievementsSourceLink(string name, string id, AccountInfos accountInfos)
        {
            string LocalLang = CodeLang.GetEpicLang(Local);
            string Url = string.Format(UrlAchievements, LocalLang, id);

            return new SourceLink
            {
                GameName = name,
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

        public override bool RemoveWishlist(string id)
        {
            if (IsUserLoggedIn)
            {
                try
                {
                    string EpicOfferId = id.Split('|')[0];
                    string EpicNamespace = id.Split('|')[1];

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
                    Common.LogError(ex, false, $"Error remove {id} in {ClientName} wishlist", true, PluginName);
                }
            }

            return false;
        }
        #endregion

        #region Game
        // TODO
        public override GameInfos GetGameInfos(string id, AccountInfos accountInfos)
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

        /// <summary>
        /// Get dlc informations for a game.
        /// </summary>
        /// <param name="id">epic_namespace</param>
        /// <param name="accountInfos"></param>
        /// <returns></returns>
        public override ObservableCollection<DlcInfos> GetDlcInfos(string id, AccountInfos accountInfos)
        {
            try
            {
                string LocalLang = CodeLang.GetEpicLang(Local);
                ObservableCollection<DlcInfos> Dlcs = new ObservableCollection<DlcInfos>();

                // List DLC
                EpicAddonsByNamespace dataDLC = GetAddonsByNamespace(id).GetAwaiter().GetResult();
                if (dataDLC?.data?.Catalog?.catalogOffers?.elements == null)
                {
                    Logger.Warn($"No dlc for {id}");
                    return null;
                }

                foreach (Element el in dataDLC?.data?.Catalog?.catalogOffers?.elements)
                {
                    bool IsOwned = false;
                    if (accountInfos != null && accountInfos.IsCurrent)
                    {
                        IsOwned = DlcIsOwned(id, el.id);
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
        public string GetProductSlug(string name)
        {
            if (name.IsEqual("warhammer 40 000 mechanicus"))
            {
                name = "warhammer mechanicus";
            }
            else if (name.IsEqual("death stranding"))
            {
                return "death-stranding";
            }

            string ProductSlug = string.Empty;

            try
            {
                using (WebStoreClient client = new WebStoreClient())
                {
                    List<SearchStoreElement> catalogs = client.QuerySearch(name).GetAwaiter().GetResult();
                    if (catalogs.HasItems())
                    {
                        catalogs = catalogs.OrderBy(x => x.title.Length).ToList();
                        SearchStoreElement catalog = catalogs.FirstOrDefault(a => a.title.IsEqual(name, true));
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

        public string GetProductSlugByUrl(string url)
        {
            string ProductSlug = string.Empty;

            try
            {
                if (url.Contains(".epicgames.com/", StringComparison.InvariantCultureIgnoreCase))
                {
                    ProductSlug = url.Replace("/home", string.Empty).Split('/').Last();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return ProductSlug;
        }

        public string GetNameSpace(string name)
        {
            return GetNameSpace(name, string.Empty);
        }

        public string GetNameSpace(string name, string productSlug)
        {
            string nameSpace = string.Empty;

            try
            {
                using (WebStoreClient client = new WebStoreClient())
                {
                    List<SearchStoreElement> catalogs = client.QuerySearch(name).GetAwaiter().GetResult();
                    if (catalogs.HasItems())
                    {
                        catalogs = catalogs.OrderBy(x => x.title.Length).ToList();

                        SearchStoreElement catalog = null;
                        if (productSlug.IsNullOrEmpty())
                        { 
                            catalog = catalogs.FirstOrDefault(a => a.title.IsEqual(name, true)); }
                        else 
                        { 
                            catalog = catalogs.FirstOrDefault(a => a.productSlug.IsEqual(productSlug, true));
                            if (catalog == null)
                            {
                                catalogs.ForEach(x =>
                                {
                                    if (catalog == null)
                                    {
                                        var finded = x.catalogNs.mappings.FirstOrDefault(b => b.pageSlug.IsEqual(productSlug));
                                        if (finded != null)
                                        {
                                            catalog = x;
                                        }
                                    }
                                });
                            }
                        }

                        if (catalog == null)
                        {
                            Logger.Warn($"Not find nameSpace for {name} - {productSlug}");
                        }

                        nameSpace = catalog?.nameSpace;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return nameSpace;
        }


        private bool DlcIsOwned(string productNameSpace, string id)
        {
            try
            {
                EpicEntitledOfferItems ownedDLC = GetEntitledOfferItems(productNameSpace, id).GetAwaiter().GetResult();
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

        private async Task<EpicAchievementResponse> QueryAchievement(string sandboxId, string locale)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AuthToken.Token);

                var queryObject = new
                {
                    operationName = "Achievement",
                    variables = new { sandboxId, locale },
                    extensions = new
                    {
                        persistedQuery = new
                        {
                            version = 1,
                            sha256Hash = "7d54399ad8b8b5538bc2d93ee66b07014432b5488945cda35fe0b1fc70eea83a"
                        }
                    }
                };

                StringContent content = new StringContent(Serialization.ToJson(queryObject), Encoding.UTF8, "application/json");
                string str = await Web.PostStringData(UrlGraphQL, AuthToken.Token, content);
                EpicAchievementResponse data = Serialization.FromJson<EpicAchievementResponse>(str);
                return data;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }

        private async Task<EpicPlayerAchievementResponse> QueryPlayerAchievement(string epicAccountId, string sandboxId)
        {
            try
            {
                QueryPlayerAchievement query = new QueryPlayerAchievement();
                query.variables.epicAccountId = epicAccountId;
                query.variables.sandboxId = sandboxId;
                StringContent content = new StringContent(Serialization.ToJson(query), Encoding.UTF8, "application/json");
                string str = await Web.PostStringData(UrlGraphQL, AuthToken.Token, content);
                EpicPlayerAchievementResponse data = Serialization.FromJson<EpicPlayerAchievementResponse>(str.Replace("\"unlockDate\":\"N/A\",", string.Empty));
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
