using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsStores.Models;
using CommonPluginsStores.Steam.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace CommonPluginsStores.Steam
{
    public class SteamApi : StoreApi
    {
        #region Url

        #endregion


        #region Url API
        private const string UrlApi = @"https://api.steampowered.com";

        private const string UrlApiAppsList = UrlApi +  @"/ISteamApps/GetAppList/v2/";
        private const string UrlApiFriendList = UrlApi + @"/ISteamUser/GetFriendList/v1/?key={0}&steamid={1}";
        #endregion


        protected List<App> _AppsList;
        internal List<App> AppsList
        {
            get
            {
                if (_AppsList == null)
                {
                    // From cache if exists & not expired
                    if (File.Exists(PathAppsList) && File.GetLastWriteTime(PathAppsList).AddDays(3) > DateTime.Now)
                    {
                        Common.LogDebug(true, "GetSteamAppListFromCache");
                        AppsList = Serialization.FromJsonFile<List<App>>(PathAppsList);
                    }
                    // From web
                    else
                    {
                        Common.LogDebug(true, "GetSteamAppsListFromWeb");
                        AppsList = GetSteamAppsListFromWeb();
                    }
                }
                return _AppsList;
            }

            set
            {
                _AppsList = value;
            }
        }


        protected bool? _IsPrivate;
        public bool IsPrivate
        {
            get
            {
                if (_IsPrivate == null)
                {
                    _IsPrivate = GetIsPrivate();
                }
                return (bool)_IsPrivate;
            }

            set => SetValue(ref _IsPrivate, value);
        }


        private string ApiKey;
        private int SteamId;

        private string PathAppsList;


        public SteamApi() : base("Steam")
        {
            PathAppsList = Path.Combine(PathStoresData, "SteamAppsList.json");
        }


        #region Cookies
        internal override List<HttpCookie> GetWebCookies()
        {
            throw new NotImplementedException();
        }
        #endregion


        #region Configuration
        internal override bool GetIsUserLoggedIn()
        {
            //bool isLogged = OriginAPI.GetIsUserLoggedIn();
            //
            //if (isLogged)
            //{
            //    AuthTokenResponse AccessToken = OriginAPI.GetAccessToken();
            //    AuthToken = new StoreToken
            //    {
            //        Token = AccessToken.access_token,
            //        Type = AccessToken.token_type
            //    };
            //}
            //else
            //{
            //    AuthToken = null;
            //}
            //
            //return isLogged;
            return false;
        }
        #endregion


        #region Current user
        internal override AccountInfos GetCurrentAccountInfos()
        {
            try
            {
                //List<HttpHeader> httpHeaders = new List<HttpHeader>
                //{
                //    new HttpHeader { Key = "Authorization", Value = AuthToken.Type + " " + AuthToken.Token }
                //};
                //string WebData = Web.DownloadStringData(UrlAccountIdentity, httpHeaders).GetAwaiter().GetResult();
                //Serialization.TryFromJson(WebData, out Models.AccountInfoResponse accountInfoResponse);
                //
                //if (accountInfoResponse != null)
                //{
                //    long UserId = accountInfoResponse.pid.pidId;
                //    UsersInfos usersInfos = GetUsersInfos(new List<long> { UserId });
                //
                //    long.TryParse(usersInfos?.users?.First()?.personaId, out long ClientId);
                //    string Avatar = GetAvatar(UserId);
                //    string Pseudo = usersInfos?.users?.First()?.eaId;
                //    string Link = string.Format(UrlUserProfile, GetEncoded(UserId));
                //
                //    AccountInfos userInfos = new AccountInfos
                //    {
                //        ClientId = ClientId,
                //        UserId = UserId,
                //        Avatar = Avatar,
                //        Pseudo = Pseudo,
                //        Link = Link,
                //        IsCurrent = true
                //    };
                //    return userInfos;
                //}
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }

        internal override ObservableCollection<AccountInfos> GetCurrentFriendsInfos()
        {
            try
            {
                //List<HttpHeader> httpHeaders = new List<HttpHeader>
                //{
                //    new HttpHeader { Key = "AuthToken", Value = AuthToken.Token },
                //    new HttpHeader { Key = "X-Api-Version", Value = "2" },
                //    new HttpHeader { Key = "X-Application-Key", Value = "Origin" }
                //};
                //string Url = string.Format(UrlUserFriends, CurrentAccountInfos.UserId);
                //string WebData = Web.DownloadStringData(Url, httpHeaders).GetAwaiter().GetResult();
                //Serialization.TryFromJson(WebData, out FriendsResponse friendsResponse);
                //
                //ObservableCollection<AccountInfos> accountsInfos = new ObservableCollection<AccountInfos>();
                //friendsResponse?.entries.ForEach(x =>
                //{
                //    long UserId = x.userId;
                //    long ClientId = x.personaId;
                //    string Avatar = GetAvatar(UserId);
                //    string Pseudo = x.displayName;
                //    string Link = string.Format(UrlUserProfile, GetEncoded(UserId));
                //    DateTime? DateAdded = x.dateTime;
                //
                //    AccountInfos userInfos = new AccountInfos
                //    {
                //        ClientId = ClientId,
                //        UserId = UserId,
                //        Avatar = Avatar,
                //        Pseudo = Pseudo,
                //        Link = Link
                //    };
                //    accountsInfos.Add(userInfos);
                //});
                //
                //return accountsInfos;
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
                //List<HttpHeader> httpHeaders = new List<HttpHeader>
                //{
                //    new HttpHeader { Key = "AuthToken", Value =  AuthToken.Token },
                //    new HttpHeader { Key = "Accept", Value = "application/json" }
                //};
                //string Url = string.Format(UrlApi3UserGames, CurrentAccountInfos.UserId, accountInfos.UserId);
                //string WebData = Web.DownloadStringData(Url, httpHeaders).GetAwaiter().GetResult();
                //Serialization.TryFromJson(WebData, out ProductInfosResponse productInfosResponse);
                //
                //ObservableCollection<AccountGameInfos> accountGamesInfos = new ObservableCollection<AccountGameInfos>();
                //productInfosResponse?.productInfos?.ForEach(x =>
                //{
                //    string Id = x.productId;
                //    string Name = x.displayProductName;
                //
                //    bool IsCommun = false;
                //    if (!accountInfos.IsCurrent)
                //    {
                //        IsCommun = CurrentGamesInfos?.Where(y => y.Id.IsEqual(Id))?.Count() != 0;
                //    }
                //
                //    GameInfos gameInfos = GetGameInfos(Id, Local);
                //    string Link = gameInfos?.Link;
                //
                //    string achId = x?.softwares?.softwareList?.First().achievementSetOverride;
                //    ObservableCollection<GameAchievement> Achievements = null;
                //    if (!achId.IsNullOrEmpty())
                //    {
                //        Achievements = GetAchievements(achId, accountInfos);
                //    }
                //
                //    AccountGameInfos accountGameInfos = new AccountGameInfos
                //    {
                //        Id = Id,
                //        Name = Name,
                //        Link = Link,
                //        IsCommun = IsCommun,
                //        Achievements = Achievements,
                //        HoursPlayed = 0
                //    };
                //    accountGamesInfos.Add(accountGameInfos);
                //});
                //
                //return accountGamesInfos;
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
                //List<HttpHeader> httpHeaders = new List<HttpHeader>
                //{
                //    new HttpHeader { Key = "AuthToken", Value =  AuthToken.Token },
                //    new HttpHeader { Key = "Accept", Value = "application/json" }
                //};
                //string Url = string.Format(UrlAchievements, accountInfos.ClientId, Id, CodeLang.GetOriginLang(Local));
                //string WebData = Web.DownloadStringData(Url, httpHeaders).GetAwaiter().GetResult();
                //Serialization.TryFromJson(WebData, out dynamic originAchievements);
                //
                //ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
                //foreach (var item in originAchievements?["achievements"])
                //{
                //    GameAchievement gameAchievement = new GameAchievement
                //    {
                //        Name = (string)item.Value["name"],
                //        Description = (string)item.Value["desc"],
                //        UrlUnlocked = (string)item.Value["icons"]["208"],
                //        UrlLocked = string.Empty,
                //        DateUnlocked = ((string)item.Value["state"]["a_st"] == "ACTIVE") ? default : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((int)item.Value["u"]).ToLocalTime(),
                //        Percent = (float)item.Value["achievedPercentage"] == 0 ? 100 : (float)item.Value["achievedPercentage"]
                //    };
                //    gameAchievements.Add(gameAchievement);
                //}
                //
                //return gameAchievements;
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
        public static new GameInfos GetGameInfos(string Id, string Local)
        {
            try
            {
                //string Url = string.Format(UrlApi2GameInfo, Id, CodeLang.GetOriginLang(Local), CodeLang.GetOriginLangCountry(Local));
                //string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                //Serialization.TryFromJson(WebData, out GameStoreDataResponse gameStoreDataResponse);
                //
                //GameInfos gameInfos = new GameInfos
                //{
                //    Id = gameStoreDataResponse.offerId,
                //    Name = gameStoreDataResponse.i18n.displayName,
                //    Link = gameStoreDataResponse?.offerPath != null ? string.Format(UrlStoreGame, gameStoreDataResponse.offerPath) : string.Empty,
                //    Description = gameStoreDataResponse.i18n.longDescription
                //};
                //return gameInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }
        #endregion


        #region Steam
        /// <summary>
        /// Get the list of all games from the Origin store.
        /// </summary>
        /// <returns></returns>
        private List<App> GetSteamAppsListFromWeb()
        {
            string Url = string.Empty;
            List<App> AppsList = null;
            try
            {
                Url = UrlApiAppsList;
                string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                if (WebData.IsNullOrEmpty() || WebData == "{\"applist\":{\"apps\":[]}}")
                {
                    WebData = "{}";
                }
                Serialization.TryFromJson(WebData, out SteamApps appsListResponse);

                // Write file for cache
                if (appsListResponse != null)
                {
                    AppsList = appsListResponse.applist.apps;
                    File.WriteAllText(PathAppsList, Serialization.ToJson(AppsList), Encoding.UTF8);
                }
                else
                {
                    logger.Warn($"appsListResponse is empty");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to load from {Url}");
            }

            return AppsList;
        }

        /// <summary>
        /// Get Id from Steam store with a game name.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public int GetSteamId(string Name)
        {
            AppsList.Sort((x, y) => x.appid.CompareTo(y.appid));
            App finded = AppsList.Find(x => x.name.IsEqual(Name)); ;

            if (finded != null)
            {
                Common.LogDebug(true, $"Find Steam data for {Name} - {Serialization.ToJson(finded)}");
                return finded.appid;
            }

            return 0;
        }

        /// <summary>
        /// Get name from Steam store with a game Id.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public string GetGameName(int Id)
        {
            AppsList.Sort((x, y) => x.appid.CompareTo(y.appid));
            App finded = AppsList.Find(x => x.appid == Id); ;

            if (finded != null)
            {
                Common.LogDebug(true, $"Find Steam data for {Id} - {Serialization.ToJson(finded)}");
                return finded.name;
            }

            return string.Empty;
        }

        private ObservableCollection<AccountInfos> GetCurrentFriendsInfos_Api()
        {
            try
            {
                string Url = string.Format(UrlApiFriendList, ApiKey, SteamId);
                string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out FriendsList friendsList);

                ObservableCollection<AccountInfos> accountsInfos = new ObservableCollection<AccountInfos>();
                friendsList?.friendslist?.friends?.ForEach(x =>
                {
                    long.TryParse(x.steamid, out long UserId);
                    string Avatar = "";
                    string Pseudo = "";
                    string Link = "";
                    DateTime? DateAdded = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(x.friend_since);

                    AccountInfos userInfos = new AccountInfos
                    {
                        UserId = UserId,
                        Avatar = Avatar,
                        Pseudo = Pseudo,
                        Link = Link
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

        private bool IsProfilePublic(string profilePageUrl)
        {
            try
            {
                string ResultWeb = HttpDownloader.DownloadString(profilePageUrl);
                IHtmlDocument HtmlDoc = new HtmlParser().Parse(ResultWeb);

                //this finds the Games link on the right side of the profile page. If that's public then so are achievements.
                var gamesPageLink = HtmlDoc.QuerySelector(@".profile_item_links a[href$=""/games/?tab=all""]");
                return gamesPageLink != null;
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return false;
            }
        }
        #endregion
    }
}
