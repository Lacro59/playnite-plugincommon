using CommonPlayniteShared.Common;
using CommonPlayniteShared.PluginLibrary.EpicLibrary.Models;
using CommonPlayniteShared.PluginLibrary.EpicLibrary.Services;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Epic.Models;
using CommonPluginsStores.Epic.Models.Query;
using CommonPluginsStores.Models;
using CommonPluginsStores.Models.Enumerations;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static CommonPlayniteShared.PluginLibrary.EpicLibrary.Models.WebStoreModels.QuerySearchResponse.Data.CatalogItem.SearchStore;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Epic
{
    // https://gist.github.com/woctezuma/8ca464a276b15d7dfad475fd6b6cbee9
    // https://github.com/nmrugg/UE4Launcher/blob/master/libs/epicApi.js
    // https://github.com/pepeizq/pepeizqs-deals-web/blob/master/pepeizqs%20deals%20web/APIs/EpicGames/Juego.cs
    public class EpicApi : StoreApi
    {
        #region Urls

        private string UrlBase => @"https://www.epicgames.com";
        private string UrlStore => UrlBase + @"/store/{0}/p/{1}";
        private string UrlAchievements => UrlBase + @"/store/{0}/achievements/{1}";
        private string UrlLogin => UrlBase + @"/id/login";
        private string UrlAuthCode => UrlBase + @"/id/api/redirect?clientId=34a02cf8f4414e29b15921876da36f9a&responseType=code";

        private string UrlGraphQL => @"https://graphql.epicgames.com/graphql";

        private string UrlApiServiceBase => @"https://account-public-service-prod03.ol.epicgames.com";
        private string UrlAccountAuth => UrlApiServiceBase + @"/account/api/oauth/token";
        private string UrlAccount => UrlApiServiceBase + @"/account/api/public/account/{0}";
        private string UrlStoreEpic => @"https://store.epicgames.com";
        private string UrlAccountProfile => UrlStoreEpic + @"/u/{0}";
        private string UrlAccountLinkFriends => UrlStoreEpic + @"/u/{0}/friends";
        private string UrlAccountAchievementss => UrlStoreEpic + @"/{0}/u/{1}/details/{2}";

        private string UrlApiFriendBase => @"https://friends-public-service-prod.ol.epicgames.com";
        private string UrlFriendsSummary => UrlApiFriendBase + @"/friends/api/v1/{0}/summary";

        private string UrlApiLibraryBase => @"https://library-service.live.use1a.on.epicgames.com";
        private string UrlPlaytimeAll => UrlApiLibraryBase + @"/library/api/public/playtime/account/{0}/all";

        private string UrlApiLauncherBase => @"https://launcher-public-service-prod06.ol.epicgames.com";
        private string UrlAsset => UrlApiLauncherBase + @"/launcher/api/public/assets/Windows?label=Live";

        private string UrlApiCatalog => @"https://catalog-public-service-prod06.ol.epicgames.com";
        
        #endregion

        private static string AuthEncodedString => "MzRhMDJjZjhmNDQxNGUyOWIxNTkyMTg3NmRhMzZmOWE6ZGFhZmJjY2M3Mzc3NDUwMzlkZmZlNTNkOTRmYzc2Y2Y=";

        #region Paths

        private string TokensPath { get; }

        #endregion

        public EpicApi(string pluginName, ExternalPlugin pluginLibrary) : base(pluginName, pluginLibrary, "Epic")
        {
            TokensPath = Path.Combine(PathStoresData, "Epic_Tokens.dat");

            CookiesDomains = new List<string> { ".epicgames.com" };
        }

        #region Cookies

        protected override List<HttpCookie> GetWebCookies(bool deleteCookies = false, IWebView webView = null)
        {
            string localLangShort = CodeLang.GetEpicLangCountry(Locale);
            List<HttpCookie> httpCookies = new List<HttpCookie>
            {
                new HttpCookie
                {
                    Domain = ".www.epicgames.com",
                    Name = "EPIC_LOCALE_COOKIE",
                    Value = localLangShort,
                    Creation = DateTime.Now,
                    LastAccess = DateTime.Now
                },
                new HttpCookie
                {
                    Domain = ".www.epicgames.com",
                    Name = "EPIC_EG1",
                    Value = AuthToken?.Token ?? string.Empty,
                    Creation = DateTime.Now,
                    LastAccess = DateTime.Now
                },
                new HttpCookie
                {
                    Domain = "store.epicgames.com",
                    Name = "EPIC_LOCALE_COOKIE",
                    Value = localLangShort,
                    Creation = DateTime.Now,
                    LastAccess = DateTime.Now
                },
                new HttpCookie
                {
                    Domain = "store.epicgames.com",
                    Name = "EPIC_EG1",
                    Value = AuthToken?.Token ?? string.Empty,
                    Creation = DateTime.Now,
                    LastAccess = DateTime.Now
                }
            };
            return httpCookies;
        }

        #endregion

        #region Configuration

        protected override bool GetIsUserLoggedIn()
        {
            if (CurrentAccountInfos == null)
            {
                return false;
            }

            if (!CurrentAccountInfos.IsPrivate && !StoreSettings.UseAuth)
            {
                return !CurrentAccountInfos.UserId.IsNullOrEmpty();
            }

            bool isLogged = CheckIsUserLoggedIn();
            if (isLogged)
            {
                OauthResponse tokens = LoadTokens();
            }
            else
            {
                AuthToken = null;
            }

            return isLogged;
        }

        public override void Login()
        {
            try
            {
                ResetIsUserLoggedIn();
                EpicLogin();
                SetUserAfterLogin();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, false, PluginName);
            }
        }

        public override void LoginAlternative()
        {
            try
            {
                ResetIsUserLoggedIn();
                EpicLoginAlternative();
                SetUserAfterLogin();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, false, PluginName);
            }
        }

        private void SetUserAfterLogin()
        {
            OauthResponse tokens = LoadTokens();
            if (tokens != null)
            {
                AccountInfos accountInfos = new AccountInfos
                {
                    UserId = tokens.account_id,
                    Pseudo = tokens.account_id == CurrentAccountInfos.UserId ? CurrentAccountInfos.Pseudo : string.Empty,
                    Link = string.Format(UrlAccountProfile, tokens.account_id),
                    IsPrivate = true,
                    IsCurrent = true
                };
                CurrentAccountInfos = accountInfos;

                SaveCurrentUser();
                _ = GetCurrentAccountInfos();

                Logger.Info($"{ClientName} logged");
            }
        }

        #endregion

        #region Current user

        protected override AccountInfos GetCurrentAccountInfos()
        {
            AccountInfos accountInfos = LoadCurrentUser();
            if (!accountInfos?.UserId?.IsNullOrEmpty() ?? false)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(1000);
                        CurrentAccountInfos.IsPrivate = !await CheckIsPublic(accountInfos);
                        CurrentAccountInfos.AccountStatus = CurrentAccountInfos.IsPrivate ? AccountStatus.Private : AccountStatus.Public;

                        if (CurrentAccountInfos.Pseudo.IsNullOrEmpty())
                        {
                            OauthResponse tokens = LoadTokens();
                            EpicAccountResponse epicAccountResponse = await GetAccountInfo(tokens.account_id);
                            CurrentAccountInfos.Pseudo = epicAccountResponse?.DisplayName;
                            SaveCurrentUser();
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }
                });

                return accountInfos;
            }
            return new AccountInfos { IsCurrent = true };
        }

        protected override ObservableCollection<AccountInfos> GetCurrentFriendsInfos()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                ObservableCollection<AccountInfos> accountsInfos = new ObservableCollection<AccountInfos>();

                EpicFriendsSummary epicFriendsSummary = GetFriendsSummary().GetAwaiter().GetResult();
                var tasks = epicFriendsSummary.Friends.Select(async x =>
                {
                    EpicAccountResponse epicAccountResponsea = await GetAccountInfo(x.AccountId);
                    return new AccountInfos
                    {
                        DateAdded = null,
                        UserId = x.AccountId,
                        Avatar = string.Empty,
                        Pseudo = epicAccountResponsea.DisplayName,
                        Link = string.Format(UrlAccountProfile, x.AccountId)
                    };
                }).ToList();

                var results = Task.WhenAll(tasks).GetAwaiter().GetResult();
                foreach (var userInfos in results)
                {
                    accountsInfos.Add(userInfos);
                }

                return accountsInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        #endregion

        #region User details

        public override ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos)
        {
            if (!IsUserLoggedIn || !accountInfos.IsCurrent)
            {
                return null;
            }

            try
            {
                ObservableCollection<AccountGameInfos> accountGamesInfos = new ObservableCollection<AccountGameInfos>();

                List<Asset> assets = GetAssets();
                List<PlaytimeItem> playtimeItems = GetPlaytimeItems();

                foreach (Asset gameAsset in assets.Where(a => a.@namespace != "ue"))
                {
                    try
                    {
                        string cacheFile = CommonPlayniteShared.Common.Paths.GetSafePathName($"{gameAsset.@namespace}_{gameAsset.catalogItemId}_{gameAsset.buildVersion}.json");
                        cacheFile = Path.Combine(PathAppsData, cacheFile);
                        CatalogItem catalogItem = GetCatalogItem(gameAsset.@namespace, gameAsset.catalogItemId, cacheFile);
                        if (catalogItem?.categories?.Any(a => a.path == "applications") != true)
                        {
                            continue;
                        }

                        if ((catalogItem?.mainGameItem != null) && (catalogItem.categories?.Any(a => a.path == "addons/launchable") == false))
                        {
                            continue;
                        }

                        if (catalogItem?.categories?.Any(a => a.path == "digitalextras" || a.path == "plugins" || a.path == "plugins/engine") == true)
                        {
                            continue;
                        }

                        if ((catalogItem?.customAttributes?.ContainsKey("ThirdPartyManagedApp") == true) && (catalogItem?.customAttributes["ThirdPartyManagedApp"].value.ToLower() == "the ea app"))
                        {
                            //if (!SettingsViewModel.Settings.ImportEAGames)
                            //{
                            //    continue;
                            //}
                        }

                        if ((catalogItem?.customAttributes?.ContainsKey("partnerLinkType") == true) && (catalogItem.customAttributes["partnerLinkType"].value == "ubisoft"))
                        {
                            //if (!SettingsViewModel.Settings.ImportUbisoftGames)
                            //{
                            //    continue;
                            //}
                        }

                        bool isCommun = false;
                        if (!accountInfos.IsCurrent)
                        {
                            isCommun = CurrentGamesInfos?.Where(y => y.Id.IsEqual(gameAsset.appName))?.Count() != 0;
                        }

                        ObservableCollection<GameAchievement> achievements = GetAchievements(gameAsset.@namespace, accountInfos);

                        AccountGameInfos agi = new AccountGameInfos
                        {
                            Id = gameAsset.appName,
                            Name = catalogItem.title.RemoveTrademarks(),
                            Image = catalogItem.keyImages?.First()?.url,
                            IsCommun = isCommun,
                            Playtime = playtimeItems?.FirstOrDefault(x => x.artifactId == gameAsset.appName)?.totalTime ?? 0,
                            Achievements = achievements,
                            Link = string.Empty,
                            Released = null
                        };

                        accountGamesInfos.Add(agi);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, false, PluginName);
                    }
                }

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
                Tuple<string, ObservableCollection<GameAchievement>> data = GetAchievementsSchema(id);
                
                if (data?.Item2?.Count() == 0)
                {
                    return gameAchievements;
                }

                string productId = data.Item1;
                gameAchievements = data.Item2;

                if (!accountInfos.IsPrivate && !StoreSettings.UseAuth)
                {
                    EpicPlayerProfileAchievementsByProductIdResponse playerProfileAchievementsByProductId = QueryPlayerProfileAchievementsByProductId(accountInfos.UserId, productId).GetAwaiter().GetResult();
                    playerProfileAchievementsByProductId?.Data?.PlayerProfile?.PlayerProfile2?.ProductAchievements?.Data?.PlayerAchievements?.ForEach(x =>
                    {
                        GameAchievement owned = gameAchievements.Where(y => y.Id.IsEqual(x.PlayerAchievement.AchievementName))?.FirstOrDefault();
                        if (owned != null)
                        {
                            owned.DateUnlocked = x?.PlayerAchievement.UnlockDate ?? default;
                        }
                    });
                }
                else
                {
                    EpicPlayerAchievementResponse epicPlayerAchievementResponse = QueryPlayerAchievement(accountInfos.UserId, id).GetAwaiter().GetResult();
                    epicPlayerAchievementResponse?.Data?.PlayerAchievement?.PlayerAchievementGameRecordsBySandbox?.Records?.FirstOrDefault().PlayerAchievements.ForEach(x =>
                    {
                        GameAchievement owned = gameAchievements.Where(y => y.Id.IsEqual(x.PlayerAchievement.AchievementName))?.FirstOrDefault();
                        if (owned != null)
                        {
                            owned.DateUnlocked = x.PlayerAchievement?.UnlockDate ?? default;
                        }
                    });
                }

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
            string LocalLang = CodeLang.GetEpicLang(Locale);
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
                                        Link = Link.IsNullOrEmpty() ? string.Empty : string.Format(UrlStore, CodeLang.GetEpicLang(Locale), Link),
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

        public override Tuple<string, ObservableCollection<GameAchievement>> GetAchievementsSchema(string id)
        {
            string cachePath = Path.Combine(PathAchievementsData, $"{id}.json");
            Tuple<string, ObservableCollection<GameAchievement>> data = LoadData<Tuple<string, ObservableCollection<GameAchievement>>>(cachePath, 1440);

            if (data?.Item2 == null)
            {
                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
                EpicAchievementResponse epicAchievementResponse = QueryAchievement(id).GetAwaiter().GetResult();
                string productId = epicAchievementResponse.Data?.Achievement?.ProductAchievementsRecordBySandbox?.ProductId;
                epicAchievementResponse?.Data?.Achievement?.ProductAchievementsRecordBySandbox?.Achievements?.ForEach(x =>
                {
                    GameAchievement gameAchievement = new GameAchievement
                    {
                        Id = x.Achievement.Name,
                        Name = x.Achievement.UnlockedDisplayName.Trim(),
                        Description = x.Achievement.UnlockedDescription.Trim(),
                        UrlUnlocked = x.Achievement.UnlockedIconLink,
                        UrlLocked = x.Achievement.LockedIconLink,
                        DateUnlocked = default,
                        Percent = x.Achievement.Rarity.Percent,
                        GamerScore = x.Achievement.XP
                    };
                    gameAchievements.Add(gameAchievement);
                });

                data = new Tuple<string, ObservableCollection<GameAchievement>>(productId, gameAchievements);
                FileSystem.WriteStringToFile(cachePath, Serialization.ToJson(data));
            }

            return data;
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
                string LocalLang = CodeLang.GetEpicLang(Locale);
                ObservableCollection<DlcInfos> Dlcs = new ObservableCollection<DlcInfos>();

                // List DLC
                EpicAddonsByNamespace dataDLC = QueryAddonsByNamespace(id).GetAwaiter().GetResult();
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

        private OauthResponse LoadTokens()
        {
            if (File.Exists(TokensPath))
            {
                try
                {
                    OauthResponse tokens = Serialization.FromJson<OauthResponse>(
                        Encryption.DecryptFromFile(
                            TokensPath,
                            Encoding.UTF8,
                            WindowsIdentity.GetCurrent().User.Value));

                    AuthToken = new StoreToken
                    {
                        Token = tokens.access_token,
                        Type = tokens.token_type
                    };

                    return tokens;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, false, PluginName);
                }
            }

            return null;
        }

        private void AuthenticateUsingAuthCode(string authorizationCode)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", "basic " + AuthEncodedString);
                using (StringContent content = new StringContent($"grant_type=authorization_code&code={authorizationCode}&token_type=eg1"))
                {
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    HttpResponseMessage response = httpClient.PostAsync(UrlAccountAuth, content).GetAwaiter().GetResult();
                    string respContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    FileSystem.CreateDirectory(Path.GetDirectoryName(TokensPath));
                    Encryption.EncryptToFile(
                        TokensPath,
                        respContent,
                        Encoding.UTF8,
                        WindowsIdentity.GetCurrent().User.Value);
                }
            }
        }

        private EpicAccountResponse GetEpicAccount()
        {
            OauthResponse tokens = LoadTokens();
            if (tokens != null)
            {
                return GetAccountInfo(tokens.account_id).GetAwaiter().GetResult();
            }
            return null;
        }

        /// <summary>
        /// Checks if the account profile is public by navigating to the profile page and inspecting the HTML content.
        /// </summary>
        /// <param name="accountInfos">The account information containing the user ID of the profile to check.</param>
        /// <returns>
        /// Returns true if the profile is public (i.e., the page does not contain "private-view-text").
        /// Returns false if the profile is private or if an error occurs during the operation.
        /// </returns>
        public async Task<bool> CheckIsPublic(AccountInfos accountInfos)
        {
            try
            {
                accountInfos.AccountStatus = AccountStatus.Checking;

                WebViewSettings webViewSettings = new WebViewSettings
                {
                    JavaScriptEnabled = true
                };

                using (IWebView webView = API.Instance.WebViews.CreateOffscreenView(webViewSettings))
                {
                    string url = string.Format(UrlAccountProfile, accountInfos.UserId);
                    webView.NavigateAndWait(url);
                    string source = await webView.GetPageSourceAsync();

                    // Check if the page source contains the "private-view-text" string to determine if the profile is private.
                    // If the profile is private, the method will return false, otherwise it will return true.
                    return !source.Contains("private-view-text");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return false;
        }

        private bool CheckIsUserLoggedIn()
        {
            OauthResponse tokens = LoadTokens();
            if (tokens == null)
            {
                return false;
            }

            try
            {
                EpicAccountResponse account = GetAccountInfo(tokens.account_id).GetAwaiter().GetResult();
                if (account == null)
                {
                    RenewTokens(tokens.refresh_token);
                    tokens = LoadTokens();
                    if (tokens.account_id.IsNullOrEmpty() || tokens.access_token.IsNullOrEmpty())
                    {
                        return false;
                    }

                    account = GetEpicAccount();
                }

                if (CurrentAccountInfos.Pseudo.IsNullOrEmpty() && account.Id == tokens.account_id)
                {
                    CurrentAccountInfos.Pseudo = account?.DisplayName;
                    CurrentAccountInfos.Link = string.Format(UrlAccountProfile, account.Id);
                }
                return account != null && account.Id == tokens.account_id;
            }
            catch
            {
                try
                {
                    RenewTokens(tokens.refresh_token);
                    tokens = LoadTokens();
                    if (tokens.account_id.IsNullOrEmpty() || tokens.access_token.IsNullOrEmpty())
                    {
                        return false;
                    }

                    EpicAccountResponse account = GetAccountInfo(tokens.account_id).GetAwaiter().GetResult()    ;
                    if (CurrentAccountInfos.Pseudo.IsNullOrEmpty() && account.Id == tokens.account_id)
                    {
                        CurrentAccountInfos.Pseudo = account?.DisplayName;
                        CurrentAccountInfos.Link = string.Format(UrlAccountProfile, account.Id);
                    }
                    return account.Id == tokens.account_id;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "Failed to validation Epic authentication.", false, PluginName);
                    return false;
                }
            }
        }

        private void EpicLogin()
        {
            bool loggedIn = false;
            string apiRedirectContent = string.Empty;

            using (IWebView webView = API.Instance.WebViews.CreateView(new WebViewSettings
            {
                WindowWidth = 580,
                WindowHeight = 700,
                // This is needed otherwise captcha won't pass
                UserAgent = Web.UserAgent
            }))
            {
                webView.LoadingChanged += async (s, e) =>
                {
                    string address = webView.GetCurrentAddress();
                    if (address.Contains(@"id/api/redirect?clientId=") && !e.IsLoading)
                    {
                        apiRedirectContent = await webView.GetPageTextAsync();
                        loggedIn = true;
                        CookiesDomains.ForEach(x => { webView.DeleteDomainCookies(x); });
                        webView.Close();
                    }

                    if (address.EndsWith(@"epicgames.com/account/personal") && !e.IsLoading)
                    {
                        webView.Navigate(UrlAuthCode);
                    }
                };

                CookiesDomains.ForEach(x => { webView.DeleteDomainCookies(x); });
                webView.Navigate(UrlLogin);
                _ = webView.OpenDialog();
            }

            if (!loggedIn)
            {
                return;
            }

            if (apiRedirectContent.IsNullOrEmpty())
            {
                return;
            }

            string authorizationCode = Serialization.FromJson<ApiRedirectResponse>(apiRedirectContent).authorizationCode;
            FileSystem.DeleteFile(TokensPath);
            if (string.IsNullOrEmpty(authorizationCode))
            {
                Logger.Error("Failed to get login exchange key for Epic account.");
                return;
            }

            AuthenticateUsingAuthCode(authorizationCode);
        }

        private void EpicLoginAlternative()
        {
            _ = API.Instance.Dialogs.ShowMessage(
                ResourceProvider.GetString("LOCEpicAlternativeAuthInstructions"), "",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.None);
            _ = ProcessStarter.StartUrl(UrlAuthCode);
            StringSelectionDialogResult res = API.Instance.Dialogs.SelectString("LOCEpicAuthCodeInputMessage", "", "");
            if (!res.Result || res.SelectedString.IsNullOrWhiteSpace())
            {
                return;
            }

            AuthenticateUsingAuthCode(res.SelectedString.Trim().Trim('"'));
        }

        private void RenewTokens(string refreshToken)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", "basic " + AuthEncodedString);
                using (StringContent content = new StringContent($"grant_type=refresh_token&refresh_token={refreshToken}&token_type=eg1"))
                {
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    HttpResponseMessage response = httpClient.PostAsync(UrlAccountAuth, content).GetAwaiter().GetResult();
                    string respContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    FileSystem.CreateDirectory(Path.GetDirectoryName(TokensPath));
                    Encryption.EncryptToFile(
                        TokensPath,
                        respContent,
                        Encoding.UTF8,
                        WindowsIdentity.GetCurrent().User.Value);
                }
            }
        }

        public string GetProducSlug(Game game)
        {
            string productSlug = string.Empty;
            string normalizedEpicName = PlayniteTools.NormalizeGameName(game.Name.Replace("'", "").Replace(",", ""));
            game.Links?.ForEach(x =>
            {
                productSlug = GetProductSlugByUrl(x.Url, normalizedEpicName).IsNullOrEmpty() ? productSlug : GetProductSlugByUrl(x.Url, normalizedEpicName);
            });

            if (productSlug.IsNullOrEmpty())
            {
                productSlug = GetProductSlug(normalizedEpicName);
            }

            if (productSlug.IsNullOrEmpty())
            {
                Logger.Warn($"No ProductSlug for {game.Name}");
            }

            return productSlug;
        }

        private string GetProductSlug(string name)
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

        private string GetProductSlugByUrl(string url, string gameName)
        {
            string ProductSlug = string.Empty;

            if (url.Contains("store.epicgames.com", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    string[] urlSplit = url.Split('/');
                    foreach (string slug in urlSplit)
                    {
                        if (slug.ContainsInvariantCulture(gameName.ToLower(), System.Globalization.CompareOptions.IgnoreSymbols))
                        {
                            ProductSlug = slug;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }

            return ProductSlug;
        }

        private string GetNameSpace(string name)
        {
            return GetNameSpace(name, string.Empty);
        }

        private string GetNameSpace(string name, string productSlug)
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
                                        SearchStoreElement.CatalogNs.Mappings found = x?.catalogNs?.mappings?.FirstOrDefault(b => b?.pageSlug?.IsEqual(productSlug) ?? false);
                                        if (found != null)
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

        public string GetNameSpace(Game game)
        {
            string productSlug = GetProducSlug(game);
            string normalizedEpicName = PlayniteTools.NormalizeGameName(game.Name.Replace("'", "").Replace(",", ""));

            // The search don't find the classic game
            if (productSlug == "death-stranding")
            {
                return "f4a904fcef2447439c35c4e6457f3027";
            }
            return productSlug.IsNullOrEmpty() ? GetNameSpace(normalizedEpicName) : GetNameSpace(normalizedEpicName, productSlug);
        }

        private bool DlcIsOwned(string productNameSpace, string id)
        {
            try
            {
                EpicEntitledOfferItems ownedDLC = QueryEntitledOfferItems(productNameSpace, id).GetAwaiter().GetResult();
                return (ownedDLC?.data?.Launcher?.entitledOfferItems?.entitledToAllItemsInOffer ?? false) && (ownedDLC?.data?.Launcher?.entitledOfferItems?.entitledToAnyItemInOffer ?? false);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return false;
            }
        }

        private async Task<EpicAddonsByNamespace> QueryAddonsByNamespace(string epic_namespace)
        {
            try
            {
                string cachePath = Path.Combine(PathAppsData, $"{epic_namespace}.json");
                EpicAddonsByNamespace data = LoadData<EpicAddonsByNamespace>(cachePath, 1440);

                if (data == null)
                {
                    QueryAddonsByNamespace query = new QueryAddonsByNamespace();
                    query.variables.epic_namespace = epic_namespace;
                    query.variables.locale = CodeLang.GetEpicLang(Locale);
                    query.variables.country = CodeLang.GetOriginLangCountry(Locale);
                    StringContent content = new StringContent(Serialization.ToJson(query), Encoding.UTF8, "application/json");
                    HttpClient httpClient = new HttpClient();
                    HttpResponseMessage response = await httpClient.PostAsync(UrlGraphQL, content);
                    string str = await response.Content.ReadAsStringAsync();
                    data = Serialization.FromJson<EpicAddonsByNamespace>(str);
                }

                return data;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }

        private async Task<EpicEntitledOfferItems> QueryEntitledOfferItems(string productNameSpace, string offerId)
        {
            try
            {
                QueryEntitledOfferItems query = new QueryEntitledOfferItems();
                query.variables.productNameSpace = productNameSpace;
                query.variables.offerId = offerId;
                StringContent content = new StringContent(Serialization.ToJson(query), Encoding.UTF8, "application/json");
                string str = await Web.PostStringData(UrlGraphQL, AuthToken?.Token, content);
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

        private async Task<EpicAchievementResponse> QueryAchievement(string sandboxId)
        {
            try
            {
                string queryParams = $"?operationName=Achievement&variables={{\"sandboxId\":\"{sandboxId}\",\"locale\":\"{CodeLang.GetEpicLang(Locale)}\"}}&extensions={{\"persistedQuery\":{{\"version\":1,\"sha256Hash\":\"9284d2fe200e351d1496feda728db23bb52bfd379b236fc3ceca746c1f1b33f2\"}}}}";
                string response = await Web.DownloadStringData(UrlGraphQL + queryParams);
                _ = Serialization.TryFromJson(response, out EpicAchievementResponse data);
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
                if (AuthToken?.Token?.IsNullOrEmpty() ?? true)
                {
                    IsUserLoggedIn = false;
                    return null;
                }

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

        private async Task<EpicPlayerProfileAchievementsByProductIdResponse> QueryPlayerProfileAchievementsByProductId(string epicAccountId, string productId)
        {
            try
            {
                QueryPlayerProfileAchievementsByProductId query = new QueryPlayerProfileAchievementsByProductId();
                query.variables.epicAccountId = epicAccountId;
                query.variables.productId = productId;
                StringContent content = new StringContent(Serialization.ToJson(query), Encoding.UTF8, "application/json");
                string str = await Web.PostStringData(UrlGraphQL, content);
                EpicPlayerProfileAchievementsByProductIdResponse data = Serialization.FromJson<EpicPlayerProfileAchievementsByProductIdResponse>(str.Replace("\"unlockDate\":\"N/A\",", string.Empty));
                return data;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }

        public List<PlaytimeItem> GetPlaytimeItems()
        {
            string formattedPlaytimeUrl = string.Format(UrlPlaytimeAll, CurrentAccountInfos.UserId);
            return InvokeRequest<List<PlaytimeItem>>(formattedPlaytimeUrl).GetAwaiter().GetResult().Item2;
        }

        public List<Asset> GetAssets()
        {
            return InvokeRequest<List<Asset>>(UrlAsset).GetAwaiter().GetResult().Item2;
        }

        public async Task<EpicFriendsSummary> GetFriendsSummary()
        {
            string url = string.Format(UrlFriendsSummary, CurrentAccountInfos.UserId);
            Tuple<string, EpicFriendsSummary> data = await InvokeRequest<EpicFriendsSummary>(url);
            return data.Item2;
        }

        public async Task<EpicAccountResponse> GetAccountInfo(string id)
        {
            string url = string.Format(UrlAccount, id);
            Tuple<string, EpicAccountResponse> data = await InvokeRequest<EpicAccountResponse>(url);
            return data.Item2;
        }

        public CatalogItem GetCatalogItem(string nameSpace, string id, string cachePath)
        {
            Dictionary<string, CatalogItem> result = LoadData<Dictionary<string, CatalogItem>>(cachePath, -1);

            if (result == null)
            {
                string url = string.Format("/catalog/api/shared/bulk/items?id={0}&country={1}&locale={2}&includeMainGameDetails=true", id, CodeLang.GetEpicLangCountry(Locale), CodeLang.GetEpicLang(Locale));
                Tuple<string, Dictionary<string, CatalogItem>> catalogResponse = InvokeRequest<Dictionary<string, CatalogItem>>(UrlApiCatalog + url).GetAwaiter().GetResult();
                result = catalogResponse.Item2;
                FileSystem.WriteStringToFile(cachePath, catalogResponse.Item1);
            }

            return result.TryGetValue(id, out CatalogItem catalogItem)
                ? catalogItem
                : throw new Exception($"Epic catalog item for {id} {nameSpace} not found.");
        }

        #endregion

        private async Task<Tuple<string, T>> InvokeRequest<T>(string url) where T : class
        {
            if (!(AuthToken?.Type?.IsNullOrEmpty() ?? true) && !(AuthToken?.Token?.IsNullOrEmpty() ?? true) )
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Add("Authorization", AuthToken.Type + " " + AuthToken.Token);
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    string str = await response.Content.ReadAsStringAsync();

                    if (Serialization.TryFromJson(str, out ErrorResponse error) && !string.IsNullOrEmpty(error.errorCode))
                    {
                        throw new TokenException(error.errorCode);
                    }
                    else
                    {
                        try
                        {
                            return new Tuple<string, T>(str, Serialization.FromJson<T>(str));
                        }
                        catch
                        {
                            // For cases like #134, where the entire service is down and doesn't even return valid error messages.
                            Logger.Error(str);
                            throw new Exception("Failed to get data from Epic service.");
                        }
                    }
                }
            }
            return new Tuple<string, T>(string.Empty, null);
        }
    }
}