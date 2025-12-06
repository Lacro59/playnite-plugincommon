using CommonPlayniteShared.Common;
using CommonPlayniteShared.PluginLibrary.EpicLibrary.Models;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Epic.Models;
using CommonPluginsStores.Epic.Models.Query;
using CommonPluginsStores.Epic.Models.Response;
using CommonPluginsStores.Models;
using CommonPluginsStores.Models.Enumerations;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Epic
{
    public class EpicApi : StoreApi
    {
        #region Urls
        
        private static string UrlBase => @"https://www.epicgames.com";
        private string UrlStore => UrlBase + @"/store/{0}/p/{1}";
        private string UrlAchievements => UrlBase + @"/store/{0}/achievements/{1}";
        private string UrlLogin => UrlBase + @"/id/login?responseType=code";
        private string UrlAuthCode => UrlBase + @"/id/api/redirect?clientId=34a02cf8f4414e29b15921876da36f9a&responseType=code";

        private string UrlGraphQL => @"https://launcher.store.epicgames.com/graphql";

        private string UrlApiServiceBase => @"https://account-public-service-prod03.ol.epicgames.com";
        private string UrlAccountAuth => UrlApiServiceBase + @"/account/api/oauth/token";
        private string UrlAccount => UrlApiServiceBase + @"/account/api/public/account/{0}";

        private string UrlStoreEpic => @"https://store.epicgames.com";
        private string UrlAccountProfile => UrlStoreEpic + @"/u/{0}";
        private string UrlAccountLinkFriends => UrlStoreEpic + @"/u/{0}/friends";
        private string UrlAccountAchievements => UrlStoreEpic + @"/{0}/u/{1}/details/{2}";

        private string UrlApiFriendBase => @"https://friends-public-service-prod.ol.epicgames.com";
        private string UrlFriendsSummary => UrlApiFriendBase + @"/friends/api/v1/{0}/summary";

        private string UrlApiLibraryBase => @"https://library-service.live.use1a.on.epicgames.com";
        private string UrlPlaytimeAll => UrlApiLibraryBase + @"/library/api/public/playtime/account/{0}/all";
        private string UrlAsset => UrlApiLibraryBase + @"/library/api/public/items?includeMetadata=true&platform=Windows";

        private string UrlApiLauncherBase => @"https://launcher-public-service-prod06.ol.epicgames.com";

        private string UrlApiCatalog => @"https://catalog-public-service-prod06.ol.epicgames.com";
        
        #endregion

        private static string UserAgent => @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) EpicGamesLauncher";
        private static string AuthEncodedString => "MzRhMDJjZjhmNDQxNGUyOWIxNTkyMTg3NmRhMzZmOWE6ZGFhZmJjY2M3Mzc3NDUwMzlkZmZlNTNkOTRmYzc2Y2Y=";

        #region Paths

        private string TokensPath { get; }

        #endregion

        public EpicApi(string pluginName, ExternalPlugin pluginLibrary) : base(pluginName, pluginLibrary, "Epic")
        {
            TokensPath = Path.Combine(PathStoresData, "Epic_Tokens.dat");
            CookiesDomains = new List<string> { ".epicgames.com", ".store.epicgames.com" };
        }

        #region Cookies

        protected override List<HttpCookie> GetWebCookies(bool deleteCookies = false, IWebView webView = null)
        {
            OauthResponse tokens = LoadTokens();
            if (tokens == null)
            {
                Logger.Warn("GetWebCookies called but no tokens are available.");
                return new List<HttpCookie>();
            }

            List<HttpCookie> httpCookies = new List<HttpCookie>
            {
                new HttpCookie
                {
                    Domain = ".store.epicgames.com",
                    Name = "EPIC_EG1",
                    Value = tokens.access_token,
                    Path = "/",
                    Expires = tokens.expires_at,
                    Creation = DateTime.Now,
                    LastAccess = DateTime.Now,
                    HttpOnly = false,
                    Secure = false,
                    SameSite = CookieSameSite.LaxMode,
                    Priority = CookiePriority.Medium
                },
                new HttpCookie
                {
                    Domain = ".store.epicgames.com",
                    Name = "REFRESH_EPIC_EG1",
                    Value = tokens.refresh_token,
                    Path = "/",
                    Expires = tokens.refresh_expires_at,
                    Creation = DateTime.Now,
                    LastAccess = DateTime.Now,
                    HttpOnly = false,
                    Secure = true,
                    SameSite = CookieSameSite.NoRestriction,
                    Priority = CookiePriority.Medium
                },
                new HttpCookie
                {
                    Domain = ".store.epicgames.com",
                    Name = "refreshTokenExpires",
                    Value = tokens.refresh_expires_at.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff").Replace(":", "%3A")+ "Z",
                    Path = "/",
                    Expires = tokens.refresh_expires_at,
                    Creation = DateTime.Now,
                    LastAccess = DateTime.Now,
                    HttpOnly = false,
                    Secure = false,
                    SameSite = CookieSameSite.NoRestriction,
                    Priority = CookiePriority.Medium
                },
                new HttpCookie
                {
                    Domain = ".store.epicgames.com",
                    Name = "storeTokenExpires",
                    Value = tokens.refresh_expires_at.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff").Replace(":", "%3A")+ "Z",
                    Path = "/",
                    Expires = tokens.refresh_expires_at,
                    Creation = DateTime.Now,
                    LastAccess = DateTime.Now,
                    HttpOnly = false,
                    Secure = false,
                    SameSite = CookieSameSite.NoRestriction,
                    Priority = CookiePriority.Medium
                }
            };

            return httpCookies;
        }

        protected override List<HttpCookie> GetStoredCookies() => GetWebCookies();

        protected override bool SetStoredCookies(List<HttpCookie> httpCookies) => true;

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
                _ = LoadTokens();
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
                SetStoredCookies(GetWebCookies());

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
							EpicAccountResponse epicAccountResponse = await GetAccountInfo(accountInfos.UserId);
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

                var catalogs = GetCatalogs();
                List<PlaytimeItem> playtimeItems = GetPlaytimeItems();

                foreach (var catalog in catalogs)
                {
                    try
                    {
                        bool isCommun = false;
                        if (!accountInfos.IsCurrent)
                        {
                            isCommun = CurrentGamesInfos?.Where(y => y.Id.IsEqual(catalog.Id))?.Count() != 0;
                        }

                        string normalizedEpicName = PlayniteTools.NormalizeGameName(catalog.Title.RemoveTrademarks().Replace("'", "").Replace(",", ""));
                        string productSlug = GetProductSlug(normalizedEpicName);

                        ObservableCollection<GameAchievement> achievements = GetAchievements(catalog.Namespace, accountInfos);

                        AccountGameInfos agi = new AccountGameInfos
                        {
                            Id = catalog.Id,
                            Name = catalog.Title.RemoveTrademarks(),
                            Image = catalog.KeyImages?.First()?.Url,
                            IsCommun = isCommun,
                            Playtime = playtimeItems?.FirstOrDefault(x => x.ArtifactId == catalog.Id)?.TotalTime ?? 0,
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
                
                if (data?.Item2 == null || data.Item2.Count() == 0)
                {
                    return gameAchievements;
                }

                string productId = data.Item1;
                gameAchievements = data.Item2;

                PlayerProfileAchievementsByProductIdResponse playerProfileAchievementsByProductId = QueryPlayerProfileAchievementsByProductId(accountInfos.UserId, productId).GetAwaiter().GetResult();
                playerProfileAchievementsByProductId?.Data?.PlayerProfile?.PlayerProfileInfo?.ProductAchievements?.Data?.PlayerAchievements?.ForEach(x =>
                {
                    GameAchievement owned = gameAchievements.Where(y => y.Id.IsEqual(x.PlayerAchievement.AchievementName))?.FirstOrDefault();
                    if (owned == null)
                    {
                        Logger.Warn($"Achievement not found: {x.PlayerAchievement.AchievementName} for productId: {productId}");
                    }
                    else if (x.PlayerAchievement.Unlocked)
                    {
                        owned.DateUnlocked = DateTime.ParseExact(
                            x.PlayerAchievement.UnlockDate,
                            "yyyy-MM-ddTHH:mm:ss.fffK",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal);
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
            string localLang = CodeLang.GetEpicLang(Locale);
            string url = string.Format(UrlAchievements, localLang, id);

            return new SourceLink
            {
                GameName = name,
                Name = ClientName,
                Url = url
            };
        }

        public override ObservableCollection<AccountWishlist> GetWishlist(AccountInfos accountInfos)
        {
            if (accountInfos != null)
            {
                try
                {
                    var wishlistResponse = QueryWishList().GetAwaiter().GetResult();
                    if (wishlistResponse?.Data?.Wishlist?.WishlistItems?.Elements != null)
                    {
                        ObservableCollection<AccountWishlist> data = new ObservableCollection<AccountWishlist>();

                        foreach (var gameWishlist in wishlistResponse.Data.Wishlist.WishlistItems.Elements)
                        {
                            string id = string.Empty;
                            string name = string.Empty;
                            DateTime? released = null;
                            DateTime? added = null;
                            string image = string.Empty;
                            string link = string.Empty;

                            try
                            {
                                id = gameWishlist.OfferId + "|" + gameWishlist.Namespace;
                                name = WebUtility.HtmlDecode(gameWishlist.Offer.Title);
                                image = gameWishlist.Offer.KeyImages?.FirstOrDefault(x => x.Type.IsEqual("Thumbnail"))?.Url;
                                released = gameWishlist.Offer.EffectiveDate.ToUniversalTime();
                                added = gameWishlist.Created.ToUniversalTime();
                                link = gameWishlist.Offer?.CatalogNs?.Mappings?.FirstOrDefault()?.PageSlug;

                                data.Add(new AccountWishlist
                                {
                                    Id = id,
                                    Name = name,
                                    Link = link.IsNullOrEmpty() ? string.Empty : string.Format(UrlStore, CodeLang.GetEpicLang(Locale), link),
                                    Released = released,
                                    Added = added,
                                    Image = image
                                });
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, true, $"Error in parse {ClientName} wishlist - {name}");
                            }
                        }

                        return data;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error in parse {ClientName} wishlist", true, PluginName);
                }
            }
            return null;
        }

        // TODO Rewrite
        public override bool RemoveWishlist(string id)
        {
            if (IsUserLoggedIn)
            {
                /*
                try
                {
                    string epicOfferId = id.Split('|')[0];
                    string epicNamespace = id.Split('|')[1];

                    string query = @"mutation removeFromWishlistMutation($namespace: String!, $offerId: String!, $operation: RemoveOperation!) { Wishlist { removeFromWishlist(namespace: $namespace, offerId: $offerId, operation: $operation) { success } } }";
                    dynamic variables = new
                    {
                        @namespace = epicNamespace,
                        offerId = epicOfferId,
                        operation = "REMOVE"
                    };
                    string response = QueryWishList(query, variables).GetAwaiter().GetResult();
                    return response.IndexOf("\"success\":true") > -1;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error remove {id} in {ClientName} wishlist", true, PluginName);
                }
                */
            }

            return false;
        }

        #endregion

        #region Game

        // TODO Must be tried
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">namespace</param>
        /// <param name="accountInfos"></param>
        /// <returns></returns>
        public override GameInfos GetGameInfos(string id, AccountInfos accountInfos)
        {
            try
            {
                var assets = GetAssets();
                var asset = assets.FirstOrDefault(x => x.Namespace.IsEqual(id));
                if (asset == null)
                {
                    Logger.Warn($"No asset for {id}");
                    return null;
                }

                AddonsByNamespaceResponse addonsByNamespaceResponse = QueryAddonsByNamespace(id, "games/edition/base").GetAwaiter().GetResult();
                CatalogOffer catalogOffer = addonsByNamespaceResponse?.Data?.Catalog?.CatalogOffers?.Elements?.FirstOrDefault();
                if (catalogOffer == null)
                {
                    Logger.Warn($"No game for {id}");
                    return null;
                }

                GameInfos gameInfos = new GameInfos
                {
                    Id = catalogOffer.Id,
                    Id2 = asset.AppName,
                    Name = catalogOffer.Title,
                    Link = @"https://www.epicgames.com/store/the-escapists" + catalogOffer.ProductSlug,
                    Image = catalogOffer.KeyImages?.Find(x => x.Type.IsEqual("OfferImageWide"))?.Url?.Replace("\u002F", "/"),
                    Description = catalogOffer.Description.Trim(),
                    Released = catalogOffer.ReleaseDate
                };

                // DLC
                ObservableCollection<DlcInfos> Dlcs = GetDlcInfos(id, accountInfos);
                gameInfos.Dlcs = Dlcs;

                return gameInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override Tuple<string, ObservableCollection<GameAchievement>> GetAchievementsSchema(string @namespace)
        {
            string cacheFile = Path.Combine(PathAchievementsData, $"{@namespace}.json");
            Tuple<string, ObservableCollection<GameAchievement>> data = LoadData<Tuple<string, ObservableCollection<GameAchievement>>>(cacheFile, 1440);

			if (data?.Item2 == null || data.Item2.Count() == 0)
			{
                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
                var achievementResponse = QueryAchievement(@namespace).GetAwaiter().GetResult();
                string productId = achievementResponse.Data?.Achievement?.ProductAchievementsRecordBySandbox?.ProductId;
                achievementResponse?.Data?.Achievement?.ProductAchievementsRecordBySandbox?.Achievements?.ForEach(x =>
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
                SaveData(cacheFile, data);
            }

            return data;
        }

        // TODO Must be tried
        /// <summary>
        /// Get dlc informations for a game.
        /// </summary>
        /// <param name="id">namespace</param>
        /// <param name="accountInfos"></param>
        /// <returns></returns>
        public override ObservableCollection<DlcInfos> GetDlcInfos(string id, AccountInfos accountInfos)
        {
            try
            {
                string localLang = CodeLang.GetEpicLang(Locale);
                ObservableCollection<DlcInfos> dlcs = new ObservableCollection<DlcInfos>();

                // List DLC
                AddonsByNamespaceResponse addonsByNamespaceResponse = QueryAddonsByNamespace(id).GetAwaiter().GetResult();
                if (addonsByNamespaceResponse?.Data?.Catalog?.CatalogOffers?.Elements == null)
                {
                    Logger.Warn($"No dlc for {id}");
                    return null;
                }

                foreach (var el in addonsByNamespaceResponse?.Data?.Catalog?.CatalogOffers?.Elements)
                {
                    bool isOwned = false;
                    if (accountInfos != null && accountInfos.IsCurrent)
                    {
                        isOwned = DlcIsOwned(id, el.Id);
                    }

                    DlcInfos dlc = new DlcInfos
                    {
                        Id = el.Id,
                        Name = el.Title,
                        Description = el.Description,
                        Image = el.KeyImages?.Find(x => x.Type.IsEqual("OfferImageWide"))?.Url?.Replace("\u002F", "/"),
                        Link = string.Format(UrlStore, localLang, el.UrlSlug),
                        IsOwned = isOwned,
                        Price = el.Price?.TotalPrice?.FmtPrice?.DiscountPrice,
                        PriceBase = el.Price?.TotalPrice?.FmtPrice?.OriginalPrice,
                    };

                    dlcs.Add(dlc);
                }

                return dlcs;
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
            if (System.IO.File.Exists(TokensPath))
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
            if (accountInfos == null || accountInfos.UserId.IsNullOrEmpty())
            {
                Logger.Info($"[EpicApi] CheckIsPublic: accountInfos or UserId is null.");
                return false;
            }

            try
            {
                accountInfos.AccountStatus = AccountStatus.Checking;

				string url = string.Format(UrlAccountProfile, accountInfos.UserId);
                var pageSource = await Web.DownloadSourceDataWebView(url);
				string source = pageSource.Item1;

				// Check if the page source contains the "private-view-text" string to determine if the profile is private.
				// If the profile is private, the method will return false, otherwise it will return true.
				return !source.Contains("private-view-text");
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
            var loggedIn = false;
            var apiRedirectContent = string.Empty;
            var authorizationCode = "";

            using (IWebView webView = API.Instance.WebViews.CreateView(new WebViewSettings
            {
                WindowWidth = 580,
                WindowHeight = 700,
                // This is needed otherwise captcha won't pass
                UserAgent = UserAgent
            }))
            {
                webView.LoadingChanged += async (s, e) =>
                {
                    var address = webView.GetCurrentAddress();
                    var pageText = await webView.GetPageTextAsync();
                    if (!pageText.IsNullOrEmpty() && pageText.Contains(@"localhost") && !e.IsLoading)
                    {
                        var source = await webView.GetPageSourceAsync();
                        var matches = Regex.Matches(source, @"localhost\/launcher\/authorized\?code=([a-zA-Z0-9]+)", RegexOptions.IgnoreCase);
                        if (matches.Count > 0)
                        {
                            authorizationCode = matches[0].Groups[1].Value;
                            loggedIn = true;
                        }
                        webView.Close();
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


        private EpicAccountResponse GetEpicAccount()
        {
            OauthResponse tokens = LoadTokens();
            if (tokens != null)
            {
                return GetAccountInfo(tokens.account_id).GetAwaiter().GetResult();
            }
            return null;
        }

        public List<CatalogItem> GetCatalogs()
        {
            List<CatalogItem> catalogItems = new List<CatalogItem>();
            List<Asset> assets = GetAssets();

            foreach (var gameAsset in assets.Where(a => a.Namespace != "ue" && a.SandboxType != "PRIVATE" && !a.AppName.IsNullOrEmpty()))
            {
                try
                {
                    CatalogItem catalogItem = GetCatalogItem(gameAsset.Namespace, gameAsset.CatalogItemId);
                    if (catalogItem?.Categories?.Any(a => a.Path == "applications") != true)
                    {
                        continue;
                    }

                    if ((catalogItem?.MainGameItem != null) && (catalogItem.Categories?.Any(a => a.Path == "addons/launchable") == false))
                    {
                        continue;
                    }

                    if (catalogItem?.Categories?.Any(a => a.Path == "digitalextras" || a.Path == "plugins" || a.Path == "plugins/engine") == true)
                    {
                        continue;
                    }

                    if ((catalogItem?.CustomAttributes?.ContainsKey("ThirdPartyManagedApp") == true) && (catalogItem?.CustomAttributes["ThirdPartyManagedApp"].Value.ToLower() == "the ea app"))
                    {
                        //if (!SettingsViewModel.Settings.ImportEAGames)
                        //{
                        //    continue;
                        //}
                    }

                    if ((catalogItem?.CustomAttributes?.ContainsKey("partnerLinkType") == true) && (catalogItem.CustomAttributes["partnerLinkType"].Value == "ubisoft"))
                    {
                        //if (!SettingsViewModel.Settings.ImportUbisoftGames)
                        //{
                        //    continue;
                        //}
                    }

                    catalogItems.Add(catalogItem);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, false, PluginName);
                }
            }

            return catalogItems;
        }

        public List<PlaytimeItem> GetPlaytimeItems()
        {
            string formattedPlaytimeUrl = string.Format(UrlPlaytimeAll, CurrentAccountInfos.UserId);
            return InvokeRequest<List<PlaytimeItem>>(formattedPlaytimeUrl).GetAwaiter().GetResult().Item2;
        }

        public List<Asset> GetAssets()
        {
            string cacheFile = CommonPlayniteShared.Common.Paths.GetSafePathName($"assets.json");
            cacheFile = Path.Combine(PathAppsData, cacheFile);

            var result = LoadData<List<Asset>>(cacheFile, 10);
            if (result == null || result.Count() == 0)
            {
                var response = InvokeRequest<LibraryItemsResponse>(UrlAsset, LoadTokens()).GetAwaiter().GetResult();
                result = new List<Asset>();
                result.AddRange(response.Item2.Records);

                string nextCursor = response.Item2.ResponseMetadata?.NextCursor;
                while (nextCursor != null)
                {
                    response = InvokeRequest<LibraryItemsResponse>(
                        $"{UrlAsset}&cursor={nextCursor}",
                        LoadTokens()).GetAwaiter().GetResult();
                    result.AddRange(response.Item2.Records);
                    nextCursor = response.Item2.ResponseMetadata.NextCursor;
                }

                SaveData(cacheFile, result);
            }

            return result;
        }

        private async Task<EpicFriendsSummary> GetFriendsSummary()
        {
            string url = string.Format(UrlFriendsSummary, CurrentAccountInfos.UserId);
            Tuple<string, EpicFriendsSummary> data = await InvokeRequest<EpicFriendsSummary>(url);
            return data.Item2;
        }

        private async Task<EpicAccountResponse> GetAccountInfo(string id)
        {
            string url = string.Format(UrlAccount, id);
            Tuple<string, EpicAccountResponse> data = await InvokeRequest<EpicAccountResponse>(url, LoadTokens());
            return data.Item2;
        }

        public CatalogItem GetCatalogItem(string nameSpace, string catalogItemId)
        {
            string cacheFile = CommonPlayniteShared.Common.Paths.GetSafePathName($"{nameSpace}_{catalogItemId}.json");
            cacheFile = Path.Combine(PathAppsData, cacheFile);

            Dictionary<string, CatalogItem> result = LoadData<Dictionary<string, CatalogItem>>(cacheFile, -1);
            if (result == null)
            {
                string url = string.Format("/catalog/api/shared/namespace/{0}/bulk/items?id={1}&country={2}&locale={3}&includeMainGameDetails=true", nameSpace, catalogItemId, CodeLang.GetCountryFromLast(Locale), CodeLang.GetEpicLang(Locale));
                Tuple<string, Dictionary<string, CatalogItem>> catalogResponse = InvokeRequest<Dictionary<string, CatalogItem>>(UrlApiCatalog + url).GetAwaiter().GetResult();
                result = catalogResponse.Item2;
                SaveData(cacheFile, result);
            }

            return result.TryGetValue(catalogItemId, out CatalogItem catalogItem)
                ? catalogItem
                : throw new Exception($"Epic catalog item for {catalogItemId} {nameSpace} not found.");
        }

        public string GetProductSlug(string @namespace)
        {
            string cacheFile = CommonPlayniteShared.Common.Paths.GetSafePathName($"CatalogMappings_{@namespace}.json");
            cacheFile = Path.Combine(PathAppsData, cacheFile);

            var result = LoadData<CatalogMappingsResponse>(cacheFile, -1); 
            if (result?.Data?.Catalog?.CatalogNs?.Mappings?.FirstOrDefault()?.PageSlug == null)
            {
                result = QueryCatalogMappings(@namespace).GetAwaiter().GetResult();
                SaveData(cacheFile, result);
            }

            return result?.Data?.Catalog?.CatalogNs?.Mappings?.FirstOrDefault()?.PageSlug;
        }


        private bool DlcIsOwned(string nameSpace, string offerId)
        {
            try
            {
                EntitledOfferItemsResponse ownedDLC = QueryEntitledOfferItems(nameSpace, offerId).GetAwaiter().GetResult();
                return (ownedDLC?.Data?.Launcher?.EntitledOfferItems?.EntitledToAllItemsInOffer ?? false) && (ownedDLC?.Data?.Launcher?.EntitledOfferItems?.EntitledToAnyItemInOffer ?? false);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return false;
            }
        }


        // TODO Must be tried
        private async Task<CatalogQueryResponse> QueryCatalogOffer(string @namespace, string offerId, bool includeSubItems = true)
        {
            var query = new QueryCatalogQuery
            {
                Variables =
                {
                    Namespace = @namespace,
                    OfferId = offerId,
                    Locale = CodeLang.GetEpicLang(Locale),
                    Country = CodeLang.GetCountryFromLast(Locale),
                    IncludeSubItems = includeSubItems
                }
            };

            return await QueryGraphQL<CatalogQueryResponse>(query);
        }

        private async Task<AddonsByNamespaceResponse> QueryAddonsByNamespace(string @namespace, string categories = "addons|digitalextras")
        {
            var query = new QueryGetAddonsByNamespace
            {
                Variables =
                {
                    Categories = categories,
                    Count = 1000,
                    Country = CodeLang.GetCountryFromLast(Locale),
                    Locale = CodeLang.GetEpicLang(Locale),
                    Namespace = @namespace,
                    SortBy = "effectiveDate",
                    SortDir = "DESC"
                }
            };
            return await QueryGraphQL<AddonsByNamespaceResponse>(query);
        }

        private async Task<EntitledOfferItemsResponse> QueryEntitledOfferItems(string epic_namespace, string offerId)
        {
            var query = new QueryGetEntitledOfferItems
            {
                Variables =
                {
                    Namespace = epic_namespace,
                    OfferId = offerId
                }
            };

            return await QueryGraphQL<EntitledOfferItemsResponse>(query);
        }

        public async Task<WishlistResponse> QueryWishList(string pageType = "productHome")
        {
            var query = new QueryWishlist
            {
                Variables =
                {
                    PageType = pageType
                }
            };
            return await QueryGraphQL<WishlistResponse>(query);
        }

        private async Task<CatalogMappingsResponse> QueryCatalogMappings(string @namespace, string pageType = "productHome")
        {
            var query = new QueryGetCatalogMappings
            {
                Variables =
                {
                    Namespace = @namespace,
                    PageType = pageType
                }
            };
            return await QueryGraphQL<CatalogMappingsResponse>(query);
        }

        private async Task<AchievementResponse> QueryAchievement(string sandboxId)
        {
            var query = new QueryAchievement
            {
                Variables =
                {
                    SandboxId = sandboxId,
                    Locale = CodeLang.GetCountryFromFirst(Locale)
                }
            };
            return await QueryGraphQL<AchievementResponse>(query);
        }

        private async Task<PlayerProfileAchievementsByProductIdResponse> QueryPlayerProfileAchievementsByProductId(string accountId, string productId)
        {
            var query = new QueryPlayerProfileAchievementsByProductId
            {
                Variables =
                {
                    EpicAccountId = accountId,
                    ProductId = productId
                }
            };
            return await QueryGraphQL<PlayerProfileAchievementsByProductIdResponse>(query);
        }


        #endregion

        private async Task<Tuple<string, T>> InvokeRequest<T>(string url, OauthResponse tokens = null) where T : class
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Clear();
                if (tokens != null)
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", tokens.token_type + " " + tokens.access_token);
                }

                var response = await httpClient.GetAsync(url);
                var str = await response.Content.ReadAsStringAsync();

                if (Serialization.TryFromJson<ErrorResponse>(str, out var error) && !string.IsNullOrEmpty(error.errorCode))
                {
                    throw new Exception(error.errorCode);
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

        private async Task<T> QueryGraphQL<T>(object queryObject) where T : class
        {
            try
            {
                var queryType = queryObject.GetType();
                var operationNameProp = queryType.GetProperty("OperationName");
                var queryProp = queryType.GetProperty("Query");
                var variablesProp = queryType.GetProperty("Variables");

                string operationName = operationNameProp?.GetValue(queryObject) as string;
                string query = queryProp?.GetValue(queryObject) as string;
                object variables = variablesProp?.GetValue(queryObject);

                var payload = new
                {
                    query,
                    variables
                };

                string jsonPayload = Serialization.ToJson(payload);

                StringContent content = new StringContent(
                    jsonPayload,
                    Encoding.UTF8,
                    "application/json"
                );

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);

					bool needsAuth = (CurrentAccountInfos?.IsPrivate ?? false) || StoreSettings.UseAuth;
					if (needsAuth && AuthToken != null && !AuthToken.Token.IsNullOrEmpty())
					{
						httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + AuthToken.Token);
					}

					HttpResponseMessage response = await httpClient.PostAsync(UrlGraphQL, content);
                    string str = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            IsUserLoggedIn = false;
                            ShowNotificationUserNoAuthenticate();
                        }
                        else
                        {
                            Logger.Error($"[GraphQL] HTTP Error {response.StatusCode}: {operationName} - {str}");
                        }
                        return null;
                    }

                    if (Serialization.TryFromJson<T>(str, out T data, out Exception ex))
                    {
                        return data;
                    }
                    else if (ex != null)
                    {
                        Common.LogError(ex, false, false, PluginName, $"Failed to deserialize response - {operationName}");
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }
    }
}