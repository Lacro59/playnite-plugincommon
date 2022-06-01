using CommonPlayniteShared.PluginLibrary.OriginLibrary.Models;
using CommonPlayniteShared.PluginLibrary.OriginLibrary.Services;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using CommonPluginsStores.Origin.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Origin
{
    public class OriginApi : StoreApi
    {
        #region Url
        private const string UrlBase = @"https://www.origin.com";
        private const string UrlAccountIdentity = @"https://gateway.ea.com/proxy/identity/pids/me";
        private const string UrlUserProfile = UrlBase + @"/profile/user/{0}";

        private const string UrlUserFriends = @"https://friends.gs.ea.com/friends/2/users/{0}/friends?names=true";
        private const string UrlAchievements = @"https://achievements.gameservices.ea.com/achievements/personas/{0}/{1}/all?lang={2}&metadata=true&fullset=true";
        private const string UrlStoreGame = UrlBase + @"/store{0}";

        private const string UrlDataCurrency = @"https://data3.origin.com/defaults/web-defaults/localization/currency.json";
        #endregion


        #region Url API
        private const string UrlApi1 = @"https://api1.origin.com";
        private const string UrlApi2 = @"https://api2.origin.com";
        private const string UrlApi3 = @"https://api3.origin.com";

        private const string UrlApi1EncodePair = UrlApi1 + @"/gifting/idobfuscate/users/{0}/encodePair";
        private const string UrlApi1Avatar = UrlApi1 + @"/avatar/user/{0}/avatars?size=2";
        private const string UrlApi1Price = UrlApi1 + @"/supercarp/rating/offers?country={0}&locale={1}&pid={2}&currency={3}&offerIds={4}";

        private const string UrlApi2UserInfos = UrlApi2 + @"/atom/users?userIds={0}"; 
        private const string UrlApi2GameInfo = UrlApi2 + @"/ecommerce2/public/supercat/{0}/{1}?country={2}";

        private const string UrlApi3UserGames = UrlApi3 + @"/atom/users/{0}/other/{1}/games";
        private const string UrlApi3AppsList = UrlApi3 + @"/supercat/{0}/{1}/supercat-PCWIN_MAC-{0}-{1}.json.gz";
        #endregion


        protected static OriginAccountClient _OriginAPI;
        internal static OriginAccountClient OriginAPI
        {
            get
            {
                if (_OriginAPI == null)
                {
                    _OriginAPI = new OriginAccountClient(WebViewOffscreen);
                }
                return _OriginAPI;
            }

            set => _OriginAPI = value;
        }


        private Models.AccountInfoResponse accountInfoResponse;

        private static StoreCurrency LocalCurrency { get; set; } = new StoreCurrency { country = "US", currency = "USD", symbol = "$" };


        protected List<CommonPlayniteShared.PluginLibrary.OriginLibrary.Models.GameStoreDataResponse> _AppsList;
        internal List<CommonPlayniteShared.PluginLibrary.OriginLibrary.Models.GameStoreDataResponse> AppsList
        {
            get
            {
                if (_AppsList == null)
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
                return _AppsList;
            }

            set => _AppsList = value;
        }


        #region Paths
        private readonly string AppsListPath;
        #endregion


        public OriginApi(string PluginName) : base(PluginName, ExternalPlugin.OriginLibrary, "Origin")
        {
            AppsListPath = Path.Combine(PathStoresData, "Origin_AppsList.json");
        }


        #region Configuration
        protected override bool GetIsUserLoggedIn()
        {
            bool isLogged = OriginAPI.GetIsUserLoggedIn();

            if (isLogged)
            {
                AuthTokenResponse AccessToken = OriginAPI.GetAccessToken();
                AuthToken = new StoreToken
                { 
                    Token = AccessToken.access_token,
                    Type = AccessToken.token_type
                };

                List<HttpHeader> httpHeaders = new List<HttpHeader>
                {
                    new HttpHeader { Key = "Authorization", Value = AuthToken.Type + " " + AuthToken.Token }
                };
                string WebData = Web.DownloadStringData(UrlAccountIdentity, httpHeaders).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out accountInfoResponse);
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
                    long UserId = accountInfoResponse.pid.pidId;
                    UsersInfos usersInfos = GetUsersInfos(new List<long> { UserId });

                    long.TryParse(usersInfos?.users?.First()?.personaId, out long ClientId);
                    string Avatar = GetAvatar(UserId);
                    string Pseudo = usersInfos?.users?.First()?.eaId;
                    string Link = string.Format(UrlUserProfile, GetEncoded(UserId));

                    AccountInfos userInfos = new AccountInfos
                    {
                        ClientId = ClientId,
                        UserId = UserId,
                        Avatar = Avatar,
                        Pseudo = Pseudo,
                        Link = Link,
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
                string Url = string.Format(UrlUserFriends, CurrentAccountInfos.UserId);
                string WebData = Web.DownloadStringData(Url, httpHeaders).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out FriendsResponse friendsResponse);

                if (friendsResponse?.entries != null)
                {
                    return null;
                }

                ObservableCollection<AccountInfos> accountsInfos = new ObservableCollection<AccountInfos>();
                friendsResponse?.entries.ForEach(x => 
                {
                    long UserId = x.userId;
                    long ClientId = x.personaId;
                    string Avatar = GetAvatar(UserId);
                    string Pseudo = x.displayName;
                    string Link = string.Format(UrlUserProfile, GetEncoded(UserId));
                    DateTime? DateAdded = x.dateTime;

                    AccountInfos userInfos = new AccountInfos
                    {
                        ClientId = ClientId,
                        UserId = UserId,
                        Avatar = Avatar,
                        Pseudo = Pseudo,
                        Link = Link,
                        DateAdded = DateAdded
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
                string Url = string.Format(UrlApi3UserGames, CurrentAccountInfos.UserId, accountInfos.UserId);
                string WebData = Web.DownloadStringData(Url, httpHeaders).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out ProductInfosResponse productInfosResponse);

                ObservableCollection<AccountGameInfos> accountGamesInfos = new ObservableCollection<AccountGameInfos>();
                productInfosResponse?.productInfos?.ForEach(x => 
                {
                    string Id = x.productId;
                    string Name = x.displayProductName;

                    bool IsCommun = false;
                    if (!accountInfos.IsCurrent)
                    {
                        IsCommun = CurrentGamesInfos?.Where(y => y.Id.IsEqual(Id))?.Count() != 0;
                    }

                    GameInfos gameInfos = GetGameInfos(Id, accountInfos);
                    string Link = gameInfos?.Link;

                    string achId = x?.softwares?.softwareList?.First().achievementSetOverride;
                    ObservableCollection<GameAchievement> Achievements = null;
                    if (!achId.IsNullOrEmpty())
                    {
                        Achievements = GetAchievements(achId, accountInfos);
                    }

                    AccountGameInfos accountGameInfos = new AccountGameInfos
                    {
                        Id = Id,
                        Name = Name,
                        Link = Link,
                        IsCommun = IsCommun,
                        Achievements = Achievements,
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

        public override ObservableCollection<GameAchievement> GetAchievements(string Id, AccountInfos accountInfos)
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
                string Url = string.Format(UrlAchievements, accountInfos.ClientId, Id, CodeLang.GetOriginLang(Local));
                string WebData = Web.DownloadStringData(Url, httpHeaders).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out dynamic originAchievements);

                if (originAchievements?["achievements"] == null)
                {
                    logger.Warn($"No achievements data for {Id}");
                    return null;
                }

                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
                foreach (var item in originAchievements?["achievements"])
                {
                    GameAchievement gameAchievement = new GameAchievement
                    {
                        Name = (string)item.Value["name"],
                        Description = (string)item.Value["desc"],
                        UrlUnlocked = (string)item.Value["icons"]["208"],
                        UrlLocked = string.Empty,
                        DateUnlocked = ((string)item.Value["state"]["a_st"] == "ACTIVE") ? default : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((int)item.Value["u"]).ToLocalTime(),
                        Percent = (float)item.Value["achievedPercentage"] == 0 ? 100 : (float)item.Value["achievedPercentage"]
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

        public override SourceLink GetAchievementsSourceLink(string Name, string Id, AccountInfos accountInfos)
        {
            string LangUrl = CodeLang.GetEpicLang(Local);
            return new SourceLink
            {
                GameName = Name,
                Name = ClientName,
                Url = $"https://www.origin.com/{LangUrl}/game-library/ogd/{Id}/achievements"
            };
        }
        #endregion


        #region Game
        public override GameInfos GetGameInfos(string Id, AccountInfos accountInfos)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                string Url = string.Format(UrlApi2GameInfo, Id, CodeLang.GetOriginLang(Local), CodeLang.GetOriginLangCountry(Local));
                string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out Models.GameStoreDataResponse gameStoreDataResponse);

                GameInfos gameInfos = new GameInfos
                {
                    Id = gameStoreDataResponse.offerId,
                    Id2 = gameStoreDataResponse?.platforms[0]?.achievementSetOverride?.ToString(),
                    Name = gameStoreDataResponse.i18n.displayName,
                    Link = gameStoreDataResponse?.offerPath != null ? string.Format(UrlStoreGame, gameStoreDataResponse.offerPath) : string.Empty,
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

        public override ObservableCollection<DlcInfos> GetDlcInfos(string Id, AccountInfos accountInfos)
        {
            string Url = string.Format(UrlApi2GameInfo, Id, CodeLang.GetOriginLang(Local), CodeLang.GetOriginLangCountry(Local));
            string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();
            Serialization.TryFromJson(WebData, out Models.GameStoreDataResponse gameStoreDataResponse);

            if (gameStoreDataResponse?.extraContent?.Count > 0)
            {
                return GetDlcInfos(gameStoreDataResponse.extraContent, accountInfos);
            }

            return null;
        }

        private ObservableCollection<DlcInfos> GetDlcInfos(List<string> Ids, AccountInfos accountInfos)
        {
            ObservableCollection<DlcInfos> Dlcs = new ObservableCollection<DlcInfos>();

            foreach (string Id in Ids)
            {
                try
                {
                    string Url = string.Format(UrlApi2GameInfo, Id, CodeLang.GetOriginLang(Local), CodeLang.GetOriginLangCountry(Local));
                    string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                    Serialization.TryFromJson(WebData, out Models.GameStoreDataResponse gameStoreDataResponse);

                    if (gameStoreDataResponse?.offerId == null)
                    {
                        continue;
                    }

                    bool IsOwned = false;
                    if (accountInfos != null && accountInfos.IsCurrent)
                    {
                        IsOwned = DlcIsOwned(Id);
                    }
                    
                    DlcInfos dlc = new DlcInfos
                    {
                        Id = gameStoreDataResponse.offerId,
                        Name = gameStoreDataResponse.i18n.displayName,
                        Link = gameStoreDataResponse?.offerPath != null ? string.Format(UrlStoreGame, gameStoreDataResponse.offerPath) : string.Empty,
                        Image = gameStoreDataResponse.imageServer + gameStoreDataResponse.i18n.packArtLarge,
                        Description = gameStoreDataResponse.i18n.longDescription,
                        IsOwned = IsOwned
                    };
                    
                    Dlcs.Add(dlc);
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
            if (Dlcs?.Count > 0)
            {
                try
                {
                    PriceData priceData = GetPrice(Dlcs.Select(x => x.Id).ToList(), Local, LocalCurrency);
                    if (priceData?.Price?.offer != null)
                    {
                        foreach (Offer offer in priceData.Price.offer)
                        {
                            int idx = Dlcs.ToList().FindIndex(x => x.Id.IsEqual(offer.offerId));
                            if (idx > -1 && offer.rating?.Count > 0)
                            {
                                double Price = offer.rating[0].finalTotalAmount;
                                double PriceBase = offer.rating[0].originalTotalPrice;

                                Dlcs[idx].Price = Price + " " + priceData.SymbolCurrency;
                                Dlcs[idx].PriceBase = PriceBase + " " + priceData.SymbolCurrency;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }

            return Dlcs;
        }
        #endregion


        #region Games owned
        internal override ObservableCollection<GameDlcOwned> GetGamesDlcsOwned()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                ObservableCollection<GameDlcOwned> GamesDlcsOwned = new ObservableCollection<GameDlcOwned>();
                long UserId = accountInfoResponse.pid.pidId;
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
        /// <param name="UserId"></param>
        /// <returns></returns>
        private string GetEncoded(long UserId)
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
                string Url = string.Format(UrlApi1EncodePair, UserId);
                string WebData = Web.DownloadStringData(Url, httpHeaders).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out Encoded encoded);

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
        /// <param name="UserId"></param>
        /// <returns></returns>
        private string GetAvatar(long UserId)
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
                string Url = string.Format(UrlApi1Avatar, UserId);
                string WebData = Web.DownloadStringData(Url, httpHeaders).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out AvatarResponse avatarResponse);

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
        /// <param name="UserIds"></param>
        /// <returns></returns>
        private UsersInfos GetUsersInfos(List<long> UserIds)
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
                string Url = string.Format(UrlApi2UserInfos, string.Join(",", UserIds));
                string WebData = Web.DownloadStringData(Url, httpHeaders).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out UsersInfos usersInfos);

                return usersInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
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
                    Url = string.Format(UrlApi3AppsList, CodeLang.GetOriginLangCountry(Local), CodeLang.GetOriginLang(Local));
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
                    logger.Warn($"appsListResponse is empty");
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
        /// <param name="Name"></param>
        /// <param name="byItemName"></param>
        /// <returns></returns>
        public string GetOriginId(string Name, bool byItemName = false)
        {
            CommonPlayniteShared.PluginLibrary.OriginLibrary.Models.GameStoreDataResponse finded = null;
            if (!byItemName)
            {
                finded = AppsList.Find(x => x.masterTitle.IsEqual(Name));
            }
            else
            {
                finded = AppsList.Find(x => x.itemName.IsEqual(Name));
            }

            if (finded != null)
            {
                Common.LogDebug(true, $"Find Origin data for {Name} - {Serialization.ToJson(finded)}");
                return finded.offerId ?? string.Empty;
            }
            else if (!byItemName)
            {
                return GetOriginId(Name, true);
            }

            return string.Empty;
        }

        private PriceData GetPrice(List<string> ids, string Local, StoreCurrency LocalCurrency)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                long UserId = accountInfoResponse.pid.pidId;
                string joined = string.Join(",", ids);
                string UrlPrice = string.Format(UrlApi1Price, LocalCurrency.country, Local, UserId, LocalCurrency.currency, joined);

                List<HttpHeader> httpHeaders = new List<HttpHeader>
                {
                    new HttpHeader { Key = "authtoken", Value = AuthToken.Token },
                    new HttpHeader { Key = "accept", Value = "application/json" },
                };
                string DataPrice = Web.DownloadStringData(UrlPrice, httpHeaders).GetAwaiter().GetResult();

                Serialization.TryFromJson<PriceResult>(DataPrice, out PriceResult priceResult);

                string CodeCurrency = LocalCurrency.currency;
                string SymbolCurrency = LocalCurrency.symbol;

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
