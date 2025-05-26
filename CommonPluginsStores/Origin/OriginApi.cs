using CommonPlayniteShared.Common.Web;
using CommonPlayniteShared.PluginLibrary.OriginLibrary.Models;
using CommonPlayniteShared.PluginLibrary.OriginLibrary.Services;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using CommonPluginsStores.Origin.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Origin
{
    public class OriginApi : StoreApi
    {
        #region Urls

        private static string UrlBase => @"https://www.ea.com";
        private static string UrlAccountIdentity => @"https://gateway.ea.com/proxy/identity/pids/me";
        private static string UrlUserProfile => UrlBase + @"/profile/user/{0}";

        private static string UrlUserFriends => @"https://friends.gs.ea.com/friends/2/users/{0}/friends?names=true";
        private static string UrlAchievements => @"https://achievements.gameservices.ea.com/achievements/personas/{0}/{1}/all?lang={2}&metadata=true&fullset=true";
        private static string UrlStoreGame => UrlBase + @"/games{0}";

        private static string UrlDataCurrency => @"https://data3.origin.com/defaults/web-defaults/localization/currency.json";

        #endregion

        #region Urls API

        private static string UrlApi1 => @"https://api1.origin.com";
        private static string UrlApi2 => @"https://api2.origin.com";
        private static string UrlApi3 => @"https://api3.origin.com";

        private static string UrlApi1EncodePair => UrlApi1 + @"/gifting/idobfuscate/users/{0}/encodePair";
        private static string UrlApi1Avatar => UrlApi1 + @"/avatar/user/{0}/avatars?size=2";
        private static string UrlApi1Price => UrlApi1 + @"/supercarp/rating/offers?country={0}&locale={1}&pid={2}&currency={3}&offerIds={4}";

        private static string UrlApi2UserInfos => UrlApi2 + @"/atom/users?userIds={0}"; 
        private static string UrlApi2GameInfo => UrlApi2 + @"/ecommerce2/public/supercat/{0}/{1}?country={2}";
        private static string UrlApi2Wishlist => UrlApi2 + @"/gifting/users/{0}/wishlist";
        private static string UrlApi2WishlistDelete => UrlApi2 + @"/gifting/users/{0}/wishlist?offerId={1}";

        private static string UrlApi3UserGames => UrlApi3 + @"/atom/users/{0}/other/{1}/games";
        private static string UrlApi3AppsList => UrlApi3 + @"/supercat/{0}/{1}/supercat-PCWIN_MAC-{0}-{1}.json.gz";

        #endregion

        private static readonly Lazy<OriginAccountClient> _originAPI = new Lazy<OriginAccountClient>(() => new OriginAccountClient(API.Instance.WebViews.CreateOffscreenView()));
        private static OriginAccountClient OriginAPI => _originAPI.Value;

        private Models.AccountInfoResponse accountInfoResponse;

        private static StoreCurrency LocalCurrency { get; set; } = new StoreCurrency { country = "US", currency = "USD", symbol = "$" };

        private List<CommonPlayniteShared.PluginLibrary.OriginLibrary.Models.GameStoreDataResponse> _appsList;
        private List<CommonPlayniteShared.PluginLibrary.OriginLibrary.Models.GameStoreDataResponse> AppsList
        {
            get
            {
                if (_appsList == null)
                {
                    // From cache if exists & not expired
                    if (File.Exists(AppsListPath) && File.GetLastWriteTime(AppsListPath).AddDays(3) > DateTime.Now)
                    {
                        Common.LogDebug(true, "GetOriginAppListFromCache");
                        AppsList = Serialization.FromJsonFile<List<CommonPlayniteShared.PluginLibrary.OriginLibrary.Models.GameStoreDataResponse>>(AppsListPath);
                    }
                    // From web
                    else
                    {
                        Common.LogDebug(true, "GetOriginAppListFromWeb");
                        AppsList = GetOriginAppsListFromWeb();
                    }
                }
                return _appsList;
            }

            set => _appsList = value;
        }

        #region Paths

        private string AppsListPath { get; }

        #endregion

        public OriginApi(string pluginName) : base(pluginName, ExternalPlugin.OriginLibrary, "EA")
        {
            AppsListPath = Path.Combine(PathStoresData, "Origin_AppsList.json");
        }

        #region Configuration

        protected override bool GetIsUserLoggedIn()
        {
            bool isLogged = OriginAPI.GetIsUserLoggedIn();
            if (isLogged)
            {
                AuthTokenResponse accessToken = OriginAPI.GetAccessToken();
                AuthToken = new StoreToken
                { 
                    Token = accessToken.access_token,
                    Type = accessToken.token_type
                };

                List<HttpHeader> httpHeaders = new List<HttpHeader>
                {
                    new HttpHeader { Key = "Authorization", Value = AuthToken.Type + " " + AuthToken.Token }
                };
                string response = Web.DownloadStringData(UrlAccountIdentity, httpHeaders).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(response, out accountInfoResponse);
            }
            else
            {
                AuthToken = null;
            }

            return isLogged;
        }

        /// <summary>
        /// Set currency.
        /// </summary>
        /// <param name="currency"></param>
        public void SetCurrency(StoreCurrency currency)
        {
            LocalCurrency = currency;
        }

        #endregion

        #region Current user

        protected override AccountInfos GetCurrentAccountInfos()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                if (accountInfoResponse != null)
                {
                    string userId = accountInfoResponse.pid.pidId;
                    UsersInfos usersInfos = GetUsersInfos(new List<string> { userId });

                    string clientId = usersInfos?.users?.First()?.personaId;
                    string avatar = GetAvatar(userId);
                    string pseudo = usersInfos?.users?.First()?.eaId;
                    string link = string.Format(UrlUserProfile, GetEncoded(userId));

                    AccountInfos userInfos = new AccountInfos
                    {
                        ClientId = clientId,
                        UserId = userId,
                        Avatar = avatar,
                        Pseudo = pseudo,
                        Link = link,
                        IsCurrent = true
                    };
                    return userInfos;
                }
            }
            catch (Exception ex) 
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        protected override ObservableCollection<AccountInfos> GetCurrentFriendsInfos()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                List<HttpHeader> httpHeaders = new List<HttpHeader> 
                {
                    new HttpHeader { Key = "AuthToken", Value = AuthToken.Token },
                    new HttpHeader { Key = "X-Api-Version", Value = "2" },
                    new HttpHeader { Key = "X-Application-Key", Value = "Origin" }
                };
                string url = string.Format(UrlUserFriends, CurrentAccountInfos.UserId);
                string response = Web.DownloadStringData(url, httpHeaders).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(response, out FriendsResponse friendsResponse);

                if (friendsResponse?.entries == null)
                {
                    return null;
                }

                ObservableCollection<AccountInfos> accountsInfos = new ObservableCollection<AccountInfos>();
                friendsResponse?.entries.ForEach(x => 
                {
                    string userId = x.userId;
                    string clientId = x.personaId;
                    string avatar = GetAvatar(userId);
                    string pseudo = x.displayName;
                    string link = string.Format(UrlUserProfile, GetEncoded(userId));
                    DateTime? dateAdded = x.dateTime;

                    AccountInfos userInfos = new AccountInfos
                    {
                        ClientId = clientId,
                        UserId = userId,
                        Avatar = avatar,
                        Pseudo = pseudo,
                        Link = link,
                        DateAdded = dateAdded
                    };
                    accountsInfos.Add(userInfos);
                });

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
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                List<HttpHeader> httpHeaders = new List<HttpHeader>
                {
                    new HttpHeader { Key = "AuthToken", Value =  AuthToken.Token },
                    new HttpHeader { Key = "Accept", Value = "application/json" }
                };
                string url = string.Format(UrlApi3UserGames, CurrentAccountInfos.UserId, accountInfos.UserId);
                string response = Web.DownloadStringData(url, httpHeaders).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(response, out ProductInfosResponse productInfosResponse);

                ObservableCollection<AccountGameInfos> accountGamesInfos = new ObservableCollection<AccountGameInfos>();
                productInfosResponse?.productInfos?.ForEach(x => 
                {
                    string id = x.productId;
                    string name = x.displayProductName;

                    bool isCommun = false;
                    if (!accountInfos.IsCurrent)
                    {
                        isCommun = CurrentGamesInfos?.Where(y => y.Id.IsEqual(id))?.Count() != 0;
                    }

                    GameInfos gameInfos = GetGameInfos(id, accountInfos);
                    string link = gameInfos?.Link;

                    string achId = x?.softwares?.softwareList?.First().achievementSetOverride;
                    ObservableCollection<GameAchievement> achievements = null;
                    if (!achId.IsNullOrEmpty())
                    {
                        achievements = GetAchievements(achId, accountInfos);
                    }

                    AccountGameInfos accountGameInfos = new AccountGameInfos
                    {
                        Id = id,
                        Name = name,
                        Link = link,
                        IsCommun = isCommun,
                        Achievements = achievements,
                        Playtime = 0
                    };
                    accountGamesInfos.Add(accountGameInfos);
                });

                return accountGamesInfos;
            }
            catch (Exception ex)
            {
                // Error 403 when no data
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override ObservableCollection<GameAchievement> GetAchievements(string id, AccountInfos accountInfos)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                List<HttpHeader> httpHeaders = new List<HttpHeader>
                {
                    new HttpHeader { Key = "AuthToken", Value =  AuthToken.Token },
                    new HttpHeader { Key = "Accept", Value = "application/json" }
                };
                string url = string.Format(UrlAchievements, accountInfos.ClientId, id, CodeLang.GetOriginLang(Locale));
                string response = Web.DownloadStringData(url, httpHeaders).GetAwaiter().GetResult();
                Serialization.TryFromJson(response, out dynamic originAchievements);

                if (originAchievements?["achievements"] == null)
                {
                    Logger.Warn($"No achievements data for {id}");
                    return null;
                }

                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
                foreach (var item in originAchievements?["achievements"])
                {
                    GameAchievement gameAchievement = new GameAchievement
                    {
                        Name = ((string)item.Value["name"]).Trim(),
                        Description = ((string)item.Value["desc"]).Trim(),
                        UrlUnlocked = (string)item.Value["icons"]["208"],
                        UrlLocked = string.Empty,
                        DateUnlocked = ((string)item.Value["state"]["a_st"] == "ACTIVE") ? default : new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((int)item.Value["u"]),
                        Percent = (float)item.Value["achievedPercentage"] == 0 ? 100 : (float)item.Value["achievedPercentage"],
                        GamerScore = (float)item.Value["xp"]
                    };
                    gameAchievements.Add(gameAchievement);
                }

                return gameAchievements;
            }
            catch (Exception ex)
            {
                // Error 403 when no data
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override SourceLink GetAchievementsSourceLink(string name, string id, AccountInfos accountInfos)
        {
            return new SourceLink
            {
                GameName = name,
                Name = ClientName,
                Url = $"{UrlBase}/{CodeLang.GetEpicLang(Locale)}/game-library/ogd/{id}/achievements"
            };
        }
        
        public override ObservableCollection<AccountWishlist> GetWishlist(AccountInfos accountInfos)
        {
            if (accountInfos != null)
            {
                try
                {
                    ObservableCollection<AccountWishlist> data = new ObservableCollection<AccountWishlist>();

                    // Get informations from Origin plugin.
                    string accessToken = OriginAPI.GetAccessToken().access_token;
                    long userId = OriginAPI.GetAccountInfo(OriginAPI.GetAccessToken()).pid.pidId;

                    using (WebClient webClient = new WebClient { Encoding = Encoding.UTF8 })
                    {
                        webClient.Headers.Add("authToken", accessToken);
                        webClient.Headers.Add("accept", "application/vnd.origin.v3+json; x-cache/force-write");

                        string response = webClient.DownloadString(string.Format(UrlApi2Wishlist, userId));
                        Wishlists WishlistData = Serialization.FromJson<Wishlists>(response);

                        foreach (Wishlist item in WishlistData.wishlist)
                        {
                            string offerId = item.offerId;
                            GameInfos gameInfos = GetGameInfos(offerId, null);

                            DateTime? Added = null;
                            if (int.TryParse(item.addedAt.ToString().Substring(0, 10), out int int_addedAt))
                            {
                                Added = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(int_addedAt);
                            }

                            if (gameInfos != null)
                            {
                                data.Add(new AccountWishlist
                                {
                                    Id = gameInfos.Id,
                                    Name = gameInfos.Name,
                                    Link = gameInfos.Link,
                                    Released = gameInfos.Released,
                                    Added = Added,
                                    Image = gameInfos.Image
                                });
                            }
                        }
                    }

                    return data;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error in {ClientName} wishlist", true, PluginName);
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
                    string accessToken = OriginAPI.GetAccessToken().access_token;
                    long userId = OriginAPI.GetAccountInfo(OriginAPI.GetAccessToken()).pid.pidId;

                    using (WebClient webClient = new WebClient { Encoding = Encoding.UTF8 })
                    {
                        webClient.Headers.Add("authToken", accessToken);
                        webClient.Headers.Add("accept", "application/vnd.origin.v3+json; x-cache/force-write");

                        string stringData = Encoding.UTF8.GetString(webClient.UploadValues(string.Format(UrlApi2WishlistDelete, userId, id), "DELETE", new NameValueCollection()));
                        return stringData.Contains("\"ok\"");
                    }
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

        public override GameInfos GetGameInfos(string id, AccountInfos accountInfos)
        {
            try
            {
                Models.GameStoreDataResponse gameStoreDataResponse = GetStoreData(id);
                if (gameStoreDataResponse == null)
                {
                    return null;
                }

                GameInfos gameInfos = new GameInfos
                {
                    Id = gameStoreDataResponse.offerId,
                    Id2 = gameStoreDataResponse?.platforms[0]?.achievementSetOverride?.ToString(),
                    Name = gameStoreDataResponse.i18n.displayName,
                    Link = gameStoreDataResponse?.offerPath != null ? string.Format(UrlStoreGame, gameStoreDataResponse.gdpPath) : string.Empty,
                    Image = gameStoreDataResponse.imageServer + gameStoreDataResponse.i18n.packArtLarge,
                    Description = gameStoreDataResponse.i18n.longDescription
                };

                // DLC
                if (gameStoreDataResponse?.extraContent?.Count > 0)
                {
                    ObservableCollection<DlcInfos> Dlcs = GetDlcInfos(gameStoreDataResponse.extraContent, accountInfos);
                    gameInfos.Dlcs = Dlcs;
                }

                return gameInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override ObservableCollection<DlcInfos> GetDlcInfos(string id, AccountInfos accountInfos)
        {
            Models.GameStoreDataResponse gameStoreDataResponse = GetStoreData(id);
            if (gameStoreDataResponse?.extraContent?.Count > 0)
            {
                return GetDlcInfos(gameStoreDataResponse.extraContent, accountInfos);
            }

            return null;
        }

        private ObservableCollection<DlcInfos> GetDlcInfos(List<string> ids, AccountInfos accountInfos)
        {
            ObservableCollection<DlcInfos> dlcs = new ObservableCollection<DlcInfos>();

            foreach (string id in ids)
            {
                try
                {
                    Models.GameStoreDataResponse gameStoreDataResponse = GetStoreData(id);
                    if (gameStoreDataResponse?.offerId == null)
                    {
                        continue;
                    }

                    bool isOwned = false;
                    if (accountInfos != null && accountInfos.IsCurrent)
                    {
                        isOwned = IsDlcOwned(id);
                    }
                    
                    DlcInfos dlc = new DlcInfos
                    {
                        Id = gameStoreDataResponse.offerId,
                        Name = gameStoreDataResponse.i18n.displayName,
                        Link = gameStoreDataResponse?.offerPath != null ? string.Format(UrlStoreGame, gameStoreDataResponse.offerPath) : string.Empty,
                        Image = gameStoreDataResponse.imageServer + gameStoreDataResponse.i18n.packArtLarge,
                        Description = gameStoreDataResponse.i18n.longDescription,
                        IsOwned = isOwned
                    };
                    
                    dlcs.Add(dlc);
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("404"))
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }
                }
            }

            // Price
            if (dlcs?.Count > 0)
            {
                try
                {
                    PriceData priceData = GetPrice(dlcs.Select(x => x.Id).ToList(), Locale, LocalCurrency);
                    if (priceData?.Price?.offer != null)
                    {
                        foreach (Offer offer in priceData.Price.offer)
                        {
                            int idx = dlcs.ToList().FindIndex(x => x.Id.IsEqual(offer.offerId));
                            if (idx > -1 && offer.rating?.Count > 0)
                            {
                                double Price = offer.rating[0].finalTotalAmount;
                                double PriceBase = offer.rating[0].originalTotalPrice;

                                dlcs[idx].Price = Price + " " + priceData.SymbolCurrency;
                                dlcs[idx].PriceBase = PriceBase + " " + priceData.SymbolCurrency;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }

            return dlcs;
        }

        #endregion

        #region Games owned

        protected override ObservableCollection<GameDlcOwned> GetGamesDlcsOwned()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                ObservableCollection<GameDlcOwned> GamesDlcsOwned = new ObservableCollection<GameDlcOwned>();
                long UserId = long.Parse(accountInfoResponse.pid.pidId);
                List<AccountEntitlementsResponse.Entitlement> UserDataOwned = OriginAPI.GetOwnedGames(UserId, OriginAPI.GetAccessToken());

                UserDataOwned?.ForEach(x =>
                {
                    GamesDlcsOwned.Add(new GameDlcOwned { Id = x.offerId });
                });
                return GamesDlcsOwned;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }

        #endregion

        #region Origin

        /// <summary>
        /// Get UserId encoded.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private string GetEncoded(string userId)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                List<HttpHeader> httpHeaders = new List<HttpHeader>
                {
                    new HttpHeader { Key = "AuthToken", Value =  AuthToken.Token },
                    new HttpHeader { Key = "Accept", Value = "application/json" }
                };
                string Url = string.Format(UrlApi1EncodePair, userId);
                string WebData = Web.DownloadStringData(Url, httpHeaders).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(WebData, out Encoded encoded);

                return encoded?.id;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }


        /// <summary>
        /// Get the avatar link for a user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private string GetAvatar(string userId)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                List<HttpHeader> httpHeaders = new List<HttpHeader>
                {
                    new HttpHeader { Key = "AuthToken", Value =  AuthToken.Token },
                    new HttpHeader { Key = "Accept", Value = "application/json" }
                };
                string Url = string.Format(UrlApi1Avatar, userId);
                string WebData = Web.DownloadStringData(Url, httpHeaders).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(WebData, out AvatarResponse avatarResponse);

                if (avatarResponse != null)
                {
                    return avatarResponse?.users?.First()?.avatar?.link;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        /// <summary>
        /// Get basic users infos from a list of Users.
        /// </summary>
        /// <param name="userIds"></param>
        /// <returns></returns>
        private UsersInfos GetUsersInfos(List<string> userIds)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                List<HttpHeader> httpHeaders = new List<HttpHeader>
                {
                    new HttpHeader { Key = "AuthToken", Value =  AuthToken.Token },
                    new HttpHeader { Key = "Accept", Value = "application/json" }
                };
                string Url = string.Format(UrlApi2UserInfos, string.Join(",", userIds));
                string WebData = Web.DownloadStringData(Url, httpHeaders).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(WebData, out UsersInfos usersInfos);

                return usersInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public Models.GameStoreDataResponse GetStoreData(string id)
        {
            string cachePath = Path.Combine(PathAppsData, $"{id}.json");
            Models.GameStoreDataResponse gameStoreDataResponse = LoadData<Models.GameStoreDataResponse>(cachePath, 1440);

            if (gameStoreDataResponse == null)
            {
                string url = string.Format(UrlApi2GameInfo, id, CodeLang.GetOriginLang(Locale), CodeLang.GetOriginLangCountry(Locale));
                string response = Encoding.UTF8.GetString(HttpDownloader.DownloadData(url));
                Serialization.TryFromJson(response, out gameStoreDataResponse);
            }

            return gameStoreDataResponse;
        }

        /// <summary>
        /// Get the list of all games from the Origin store.
        /// </summary>
        /// <returns></returns>
        private List<CommonPlayniteShared.PluginLibrary.OriginLibrary.Models.GameStoreDataResponse> GetOriginAppsListFromWeb(bool forceEnglish = false)
        {
            string Url = string.Empty;
            List<CommonPlayniteShared.PluginLibrary.OriginLibrary.Models.GameStoreDataResponse> AppsList = null;
            try
            {
                if (forceEnglish)
                {
                    Url = string.Format(UrlApi3AppsList, CodeLang.GetOriginLangCountry("en_US"), CodeLang.GetOriginLang("en_US"));
                }
                else
                {
                    Url = string.Format(UrlApi3AppsList, CodeLang.GetOriginLangCountry(Locale), CodeLang.GetOriginLang(Locale));
                }

                string WebData = Web.DownloadStringDataWithGz(Url).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out AppsListResponse appsListResponse);

                // Write file for cache
                if (appsListResponse != null)
                {
                    AppsList = appsListResponse.offers.Where(x => x.offerType.IsEqual("Base Game")).ToList(); 
                    File.WriteAllText(AppsListPath, Serialization.ToJson(AppsList), Encoding.UTF8);
                }
                else
                {
                    Logger.Warn($"appsListResponse is empty");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("404"))
                {
                    return GetOriginAppsListFromWeb(true);
                }

                Common.LogError(ex, false, $"Failed to load from {Url}");
            }

            return AppsList;
        }

        /// <summary>
        /// Get Id from Origin store with a game name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="byItemName"></param>
        /// <returns></returns>
        public string GetOriginId(string name, bool byItemName = false)
        {
            CommonPlayniteShared.PluginLibrary.OriginLibrary.Models.GameStoreDataResponse found = null;
            if (!byItemName)
            {
                found = AppsList.Find(x => x.masterTitle.IsEqual(name));
            }
            else
            {
                found = AppsList.Find(x => x.itemName.IsEqual(name));
            }

            if (found != null)
            {
                Common.LogDebug(true, $"Found Origin data for {name} - {Serialization.ToJson(found)}");
                return found.offerId ?? string.Empty;
            }
            else if (!byItemName)
            {
                return GetOriginId(name, true);
            }

            return string.Empty;
        }

        private PriceData GetPrice(List<string> ids, string local, StoreCurrency localCurrency)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                string UserId = accountInfoResponse.pid.pidId;
                string joined = string.Join(",", ids);
                string UrlPrice = string.Format(UrlApi1Price, localCurrency.country, local, UserId, localCurrency.currency, joined);

                List<HttpHeader> httpHeaders = new List<HttpHeader>
                {
                    new HttpHeader { Key = "authtoken", Value = AuthToken.Token },
                    new HttpHeader { Key = "accept", Value = "application/json" },
                };
                string DataPrice = Web.DownloadStringData(UrlPrice, httpHeaders).GetAwaiter().GetResult();

                Serialization.TryFromJson<PriceResult>(DataPrice, out PriceResult priceResult);

                string CodeCurrency = localCurrency.currency;
                string SymbolCurrency = localCurrency.symbol;

                if (priceResult != null)
                {
                    return new PriceData
                    {
                        Price = priceResult,
                        CodeCurrency = CodeCurrency,
                        SymbolCurrency = SymbolCurrency
                    };
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        /// <summary>
        /// Get list of actual currencies used.
        /// </summary>
        /// <returns></returns>
        public List<StoreCurrency> GetCurrencies()
        {
            return new List<StoreCurrency>
            {
                new StoreCurrency { country = "AU", currency = "AUD", symbol = "$" },
                new StoreCurrency { country = "BE", currency = "EUR", symbol = "€" },
                new StoreCurrency { country = "CA", currency = "CAD", symbol = "$" },
                new StoreCurrency { country = "DE", currency = "EUR", symbol = "€" },
                new StoreCurrency { country = "DK", currency = "DKK", symbol = "kr." },
                new StoreCurrency { country = "ES", currency = "EUR", symbol = "€" },
                new StoreCurrency { country = "FI", currency = "EUR", symbol = "€" },
                new StoreCurrency { country = "FR", currency = "EUR", symbol = "€" },
                new StoreCurrency { country = "GB", currency = "GBP", symbol = "£" },
                new StoreCurrency { country = "HK", currency = "HKD", symbol = "HK$" },
                new StoreCurrency { country = "IE", currency = "EUR", symbol = "€" },
                new StoreCurrency { country = "IN", currency = "INR", symbol = "Rs." },
                new StoreCurrency { country = "IT", currency = "EUR", symbol = "€" },
                new StoreCurrency { country = "JP", currency = "JPY", symbol = "¥" },
                new StoreCurrency { country = "KR", currency = "KRW", symbol = "₩" },
                new StoreCurrency { country = "MX", currency = "USD", symbol = "$" },
                new StoreCurrency { country = "NL", currency = "EUR", symbol = "€" },
                new StoreCurrency { country = "NO", currency = "NOK", symbol = "kr." },
                new StoreCurrency { country = "NZ", currency = "NZD", symbol = "$" },
                new StoreCurrency { country = "PL", currency = "PLN", symbol = "zł" },
                new StoreCurrency { country = "RU", currency = "RUB", symbol = "руб" },
                new StoreCurrency { country = "SE", currency = "SEK", symbol = "" },
                new StoreCurrency { country = "SG", currency = "SGD", symbol = "$" },
                new StoreCurrency { country = "TH", currency = "THB", symbol = "฿" },
                new StoreCurrency { country = "TW", currency = "TWD", symbol = "NT$" },
                new StoreCurrency { country = "US", currency = "USD", symbol = "$" },
                new StoreCurrency { country = "ZA", currency = "ZAR", symbol = "R" },
                new StoreCurrency { country = "MY", currency = "MYR", symbol = "RM" },
                new StoreCurrency { country = "AR", currency = "ARS", symbol = "$" },
                new StoreCurrency { country = "CL", currency = "CLP", symbol = "" },
                new StoreCurrency { country = "CO", currency = "COP", symbol = "$" },
                new StoreCurrency { country = "EG", currency = "EGP", symbol = "ج.م" },
                new StoreCurrency { country = "ID", currency = "IDR", symbol = "Rp" },
                new StoreCurrency { country = "PH", currency = "PHP", symbol = "₱" },
                new StoreCurrency { country = "SA", currency = "USD", symbol = "$" },
                new StoreCurrency { country = "TR", currency = "TRY", symbol = "" },
                new StoreCurrency { country = "VN", currency = "VND", symbol = "₫" }
            };
        }

        #endregion
    }
}