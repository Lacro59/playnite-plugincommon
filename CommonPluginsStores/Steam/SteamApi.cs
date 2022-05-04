using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsStores.Models;
using CommonPluginsStores.Steam.Models;
using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Data;
using SteamKit2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommonPlayniteShared;
using static CommonPluginsShared.PlayniteTools;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using AngleSharp.Dom;
using System.Net;

namespace CommonPluginsStores.Steam
{
    public class SteamApi : StoreApi
    {
        #region Url
        private const string UrlSteamCommunity = @"https://steamcommunity.com";

        private const string UrlProfileById = UrlSteamCommunity + @"/profiles/{0}";
        private const string UrlProfileByName = UrlSteamCommunity + @"/id/{0}";
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
                    if (File.Exists(AppsListPath) && File.GetLastWriteTime(AppsListPath).AddDays(3) > DateTime.Now)
                    {
                        Common.LogDebug(true, "GetSteamAppListFromCache");
                        AppsList = Serialization.FromJsonFile<List<App>>(AppsListPath);
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

            set => _AppsList = value;
        }


        protected bool? _IsPrivate;
        public bool IsPrivate
        {
            get
            {
                if (_IsPrivate == null)
                {
                    //_IsPrivate = GetIsPrivate();
                }
                return (bool)_IsPrivate;
            }

            set => SetValue(ref _IsPrivate, value);
        }


        private Models.SteamUser _CurrentUser;
        public Models.SteamUser CurrentUser
        {
            get
            {
                if (_CurrentUser == null)
                {
                    _CurrentUser = GetCurrentUser();
                }
                return _CurrentUser;
            }

            set => SetValue(ref _CurrentUser, value);
        }


        #region Paths
        private string AppsListPath { get; set; }

        private string _InstallationPath;
        public string InstallationPath
        {
            get
            {
                if (_InstallationPath == null)
                {
                    _InstallationPath = GetInstallationPath();
                }
                return _InstallationPath;
            }

            set => SetValue(ref _InstallationPath, value);
        }

        public string LoginUsersPath => Path.Combine(InstallationPath, "config", "loginusers.vdf");
        public string SteamLibraryConfigFile => Path.Combine(PlaynitePaths.ExtensionsDataPath, CommonPluginsShared.PlayniteTools.GetPluginId(ExternalPlugin.SteamLibrary).ToString(), "config.json");
        #endregion


        public SteamApi() : base("Steam")
        {
            AppsListPath = Path.Combine(PathStoresData, "SteamAppsList.json");
        }


        #region Cookies
        internal override List<HttpCookie> GetWebCookies()
        {
            List<HttpCookie> httpCookies = WebViewOffscreen.GetCookies()?.Where(x => x?.Domain?.Contains("steam") ?? false)?.ToList() ?? new List<HttpCookie>();
            return httpCookies;
        }
        #endregion


        #region Configuration
        protected override bool GetIsUserLoggedIn()
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
        protected override AccountInfos GetCurrentAccountInfos()
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

        protected override ObservableCollection<AccountInfos> GetCurrentFriendsInfos()
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
        public override GameInfos GetGameInfos(string Id, AccountInfos accountInfos)
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
        /// Get the list of all games from the Steam store.
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
                Serialization.TryFromJson(WebData, out Models.SteamApps appsListResponse);

                // Write file for cache
                if (appsListResponse != null)
                {
                    AppsList = appsListResponse.applist.apps;
                    File.WriteAllText(AppsListPath, Serialization.ToJson(AppsList), Encoding.UTF8);
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
        /// Get AppId from Steam store with a game name.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public int GetAppId(string Name)
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
        /// Get name from Steam store with a AppId.
        /// </summary>
        /// <param name="AppId"></param>
        /// <returns></returns>
        public string GetGameName(int AppId)
        {
            AppsList.Sort((x, y) => x.appid.CompareTo(y.appid));
            App finded = AppsList.Find(x => x.appid == AppId); ;

            if (finded != null)
            {
                Common.LogDebug(true, $"Find Steam data for {AppId} - {Serialization.ToJson(finded)}");
                return finded.name;
            }

            return string.Empty;
        }

        /// <summary>
        /// Get AccountID for a SteamId
        /// </summary>
        /// <returns></returns>
        public static uint GetAccountId(ulong SteamId)
        {
            try
            {
                SteamID steamID = new SteamID();
                steamID.SetFromUInt64(SteamId);

                return steamID.AccountID;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return 0;
        }


        private ObservableCollection<AccountInfos> GetCurrentFriendsInfos_Api()
        {
            try
            {
                string Url = string.Format(UrlApiFriendList, CurrentUser.ApiKey, CurrentUser.SteamId);
                string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out FriendsList friendsList);

                ObservableCollection<AccountInfos> accountsInfos = new ObservableCollection<AccountInfos>();
                friendsList?.friendslist?.friends?.ForEach(x =>
                {
                    long.TryParse(x.steamid, out long UserId);
                    string Avatar = string.Empty;
                    string Pseudo = string.Empty;
                    string Link = string.Empty;
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


        /// <summary>
        /// Get the Steam installation path.
        /// </summary>
        /// <returns></returns>
        public string GetInstallationPath()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key?.GetValueNames().Contains("SteamPath") == true)
                    {
                        return key.GetValue("SteamPath")?.ToString().Replace('/', '\\') ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            logger.Warn("No find Steam installation");
            return string.Empty;
        }

        /// <summary>
        /// Get the Steam screanshots folder.
        /// </summary>
        /// <returns></returns>
        public string GetScreeshotsPath()
        {
            string PathScreeshotsFolder = Path.Combine(InstallationPath, "userdata", CurrentUser.AccountId.ToString(), "760", "remote");

            if (Directory.Exists(PathScreeshotsFolder))
            {
                return PathScreeshotsFolder;
            }

            logger.Warn("No find Steam screenshots folder");
            return string.Empty;
        }


        /// <summary>
        /// Get the list of all users defined in local.
        /// </summary>
        /// <returns></returns>
        public List<Models.SteamUser> GetSteamUsers()
        {
            List<Models.SteamUser> users = new List<Models.SteamUser>();
            if (File.Exists(LoginUsersPath))
            {
                KeyValue config = new KeyValue();
                try
                {
                    config.ReadFileAsText(LoginUsersPath);
                    foreach (KeyValue user in config.Children)
                    {
                        users.Add(new Models.SteamUser()
                        {
                            SteamId = ulong.Parse(user.Name),
                            AccountName = user["AccountName"].Value,
                            PersonaName = user["PersonaName"].Value,
                        });
                    }
                    return users;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }

            return null;
        }

        /// <summary>
        /// Get Playnite configured user.
        /// </summary>
        /// <returns></returns>
        private Models.SteamUser GetCurrentUser()
        {
            try
            {
                if (File.Exists(SteamLibraryConfigFile))
                {
                    dynamic SteamConfig = Serialization.FromJsonFile<dynamic>(SteamLibraryConfigFile);
                    ulong.TryParse(SteamConfig["UserId"].ToString(), out ulong SteamId);
                    string ApiKey = SteamConfig["ApiKey"];

                    List<Models.SteamUser> SteamUsers = GetSteamUsers();
                    Models.SteamUser steamUser = SteamUsers.Find(x => x.SteamId == SteamId);
                    if (steamUser != null)
                    {
                        steamUser.IsPrivateAccount = !CheckIsPublic(steamUser);
                        steamUser.ApiKey = ApiKey;
                    }
                    return steamUser;
                }
                else
                {
                    logger.Warn("No find SteamLibrary configuration");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }


        public static bool CheckIsPublic(Models.SteamUser steamUser)
        {
            return IsProfilePublic(string.Format(UrlProfileById, steamUser.SteamId)) || IsProfilePublic(string.Format(UrlProfileById, steamUser.PersonaName));
        }

        private static bool IsProfilePublic(string profilePageUrl)
        {
            try
            {
                string ResultWeb = Web.DownloadStringData(profilePageUrl).GetAwaiter().GetResult();
                IHtmlDocument HtmlDoc = new HtmlParser().Parse(ResultWeb);
                IElement profile_private_info = HtmlDoc.QuerySelector("div.profile_private_info");
                IElement error_ctn = HtmlDoc.QuerySelector("div.error_ctn");

                return profile_private_info == null && error_ctn == null;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return false;
        }
        #endregion
    }
}
