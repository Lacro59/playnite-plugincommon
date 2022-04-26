using CommonPlayniteShared.PluginLibrary.OriginLibrary.Models;
using CommonPlayniteShared.PluginLibrary.OriginLibrary.Services;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsStores.Models;
using CommonPluginsStores.Origin.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

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
        #endregion


        #region Url API
        private const string UrlApi1 = @"https://api1.origin.com";
        private const string UrlApi2 = @"https://api2.origin.com";
        private const string UrlApi3 = @"https://api3.origin.com";

        private const string UrlApi1EncodePair = UrlApi1 + @"/gifting/idobfuscate/users/{0}/encodePair";
        private const string UrlApi1Avatar = UrlApi1 + @"/avatar/user/{0}/avatars?size=2";

        private const string UrlApi2UserInfos = UrlApi2 + @"/atom/users?userIds={0}"; 
        private const string UrlApi2GameInfo = UrlApi2 + @"/ecommerce2/public/supercat/{0}/{1}?country={2}";

        private const string UrlApi3UserGames = UrlApi3 + @"/atom/users/{0}/other/{1}/games";
        private const string UrlApi3AppsList = UrlApi3 + @"/supercat/{0}/{1}/supercat-PCWIN_MAC-{0}-{1}.json.gz";
        #endregion


        protected OriginAccountClient _OriginAPI;
        internal OriginAccountClient OriginAPI
        {
            get
            {
                if (_OriginAPI == null)
                {
                    _OriginAPI = new OriginAccountClient(WebViewOffscreen);
                }
                return _OriginAPI;
            }

            set
            {
                _OriginAPI = value;
            }
        }


        protected List<GameStoreDataResponse> _AppsList;
        internal List<GameStoreDataResponse> AppsList
        {
            get
            {
                if (_AppsList == null)
                {
                    // From cache if exists & not expired
                    if (File.Exists(AppsListPath) && File.GetLastWriteTime(AppsListPath).AddDays(3) > DateTime.Now)
                    {
                        Common.LogDebug(true, "GetOriginAppListFromCache");
                        AppsList = Serialization.FromJsonFile<List<GameStoreDataResponse>>(AppsListPath);
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

            set
            {
                _AppsList = value;
            }
        }


        #region Paths
        private string AppsListPath;
        #endregion


        public OriginApi() : base("Origin")
        {
            AppsListPath = Path.Combine(PathStoresData, "OriginAppsList.json");
        }


        #region Cookies
        internal override List<HttpCookie> GetWebCookies()
        {
            throw new NotImplementedException();
        }
        #endregion


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
            }
            else
            {
                AuthToken = null;
            }

            return isLogged;
        }
        #endregion


        #region Current user
        protected override AccountInfos GetCurrentAccountInfos()
        {
            try
            {
                List<HttpHeader> httpHeaders = new List<HttpHeader> 
                { 
                    new HttpHeader { Key = "Authorization", Value = AuthToken.Type + " " + AuthToken.Token }
                };
                string WebData = Web.DownloadStringData(UrlAccountIdentity, httpHeaders).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out Models.AccountInfoResponse accountInfoResponse);

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
                Common.LogError(ex, false);
            }

            return null;
        }

        protected override ObservableCollection<AccountInfos> GetCurrentFriendsInfos()
        {
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
                Common.LogError(ex, false);
            }

            return null;
        }
        #endregion


        #region User details
        public override ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos)
        {
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
                Common.LogError(ex, false);
            }

            return null;
        }

        public override ObservableCollection<GameAchievement> GetAchievements(string Id, AccountInfos accountInfos)
        {
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
                Common.LogError(ex, false);
            }

            return null;
        }
        #endregion


        #region Game
        public override GameInfos GetGameInfos(string Id, AccountInfos accountInfos)
        {
            try
            {
                string Url = string.Format(UrlApi2GameInfo, Id, CodeLang.GetOriginLang(Local), CodeLang.GetOriginLangCountry(Local));
                string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out GameStoreDataResponse gameStoreDataResponse);

                GameInfos gameInfos = new GameInfos
                {
                    Id = gameStoreDataResponse.offerId,
                    Name = gameStoreDataResponse.i18n.displayName,
                    Link = gameStoreDataResponse?.offerPath != null ? string.Format(UrlStoreGame, gameStoreDataResponse.offerPath) : string.Empty,
                    Description = gameStoreDataResponse.i18n.longDescription
                };
                return gameInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
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
                Common.LogError(ex, false);
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
                Common.LogError(ex, false);
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
                Common.LogError(ex, false);
            }

            return null;
        }

        /// <summary>
        /// Get the list of all games from the Origin store.
        /// </summary>
        /// <returns></returns>
        private List<GameStoreDataResponse> GetOriginAppsListFromWeb(bool forceEnglish = false)
        {
            string Url = string.Empty;
            List<GameStoreDataResponse> AppsList = null;
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
            GameStoreDataResponse finded = null;
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
        #endregion
    }
}
