﻿using CommonPluginsShared;
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
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using AngleSharp.Dom;
using CommonPlayniteShared.Common;
using System.Security.Principal;
using System.Threading.Tasks;
using CommonPluginsShared.Converters;
using System.Globalization;
using System.Windows.Media;
using System.Text.RegularExpressions;
using CommonPlayniteShared.PluginLibrary.SteamLibrary.SteamShared;
using System.Net;
using static CommonPluginsShared.PlayniteTools;
using System.Net.Http;

namespace CommonPluginsStores.Steam
{
    public class SteamApi : StoreApi
    {
        #region Url
        private string SteamDbDlc => "https://steamdb.info/app/{0}/dlc/";

        private static string UrlSteamCommunity => @"https://steamcommunity.com";
        private static string UrlApi => @"https://api.steampowered.com";
        private static string UrlStore => @"https://store.steampowered.com";

        private static string UrlProfileById => UrlSteamCommunity + @"/profiles/{0}";
        private static string UrlProfileByName => UrlSteamCommunity + @"/id/{0}";

        private static string UrlUserData => UrlStore + @"/dynamicstore/userdata/";
        private static string UrlWishlist => UrlStore + @"/wishlist/profiles/{0}/wishlistdata/?p={1}&v=";
        private static string UrlWishlistRemove => UrlStore + @"/api/removefromwishlist";

        private static string UrlApiAppsList => UrlApi +  @"/ISteamApps/GetAppList/v2/";
        private static string UrlApiFriendList => UrlApi + @"/ISteamUser/GetFriendList/v1/?key={0}&steamid={1}";
        private static string UrlApiUser => UrlApi + @"/ISteamUser/GetPlayerSummaries/v2/?key={0}&steamids={1}";
        private static string UrlGetPlayerAchievements => UrlApi + @"/ISteamUserStats/GetPlayerAchievements/v1/?appid={0}&language={1}&key={2}&steamid={3}";
        private static string UrlGetGameAchievements => UrlApi + @"/IPlayerService/GetGameAchievements/v1/?format=json&appid={0}&language={1}";

        private static string UrlApiGameDetails => UrlStore + @"/api/appdetails?appids={0}&l={1}";
        private static string UrlSteamGame => UrlApi + @"/app/{0}/?l={1}";

        private static string UrlAchievementImg => @"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{0}/{1}";
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


        private SteamUserData _UserData = null;
        public SteamUserData UserData
        {
            get
            {
                if (_UserData == null)
                {
                    try
                    {
                        _UserData = LoadUserData(true);
                        if (_UserData == null)
                        {
                            using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                            {
                                WebViewOffscreen.NavigateAndWait(UrlUserData);
                                WebViewOffscreen.NavigateAndWait(UrlUserData);
                                string data = WebViewOffscreen.GetPageText();

                                if (Serialization.TryFromJson(data, out _UserData) &&_UserData?.rgOwnedApps?.Count > 0)
                                {
                                    SaveUserData(_UserData);
                                }
                                else
                                {
                                    SteamUserData loadedData = LoadUserData();
                                    _UserData = loadedData ?? _UserData;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Steam", true, PluginName);
                    }
                }
                return _UserData;
            }
        }


        #region Paths
        private string AppsListPath { get; set; }
        private string FileUserData { get; set; }
        public string KeyPath { get; set; }


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


        public SteamApi(string PluginName) : base(PluginName, ExternalPlugin.SteamLibrary, "Steam")
        {
            AppsListPath = Path.Combine(PathStoresData, "Steam_AppsList.json");
            FileUserData = Path.Combine(PathStoresData, "Steam_UserData.json");
            KeyPath = Path.Combine(PathStoresData, "Steam.dat");
        }


        #region Configuration
        protected override bool GetIsUserLoggedIn()
        {
            if (CurrentUser == null)
            {
                return false;
            }

            bool withId = IsProfilePublic(string.Format(UrlProfileById, CurrentUser.SteamId), GetStoredCookies()).GetAwaiter().GetResult();
            bool withPersona = IsProfilePublic(string.Format(UrlProfileByName, CurrentUser.PersonaName), GetStoredCookies()).GetAwaiter().GetResult();

            return withId || withPersona;
        }

        public override void Login()
        {
            ResetIsUserLoggedIn();
            string steamId = string.Empty;
            using (var view = API.Instance.WebViews.CreateView(675, 440, Colors.Black))
            {
                view.LoadingChanged += async (s, e) =>
                {
                    string address = view.GetCurrentAddress();
                    if (address.Contains(@"steamcommunity.com"))
                    {
                        string source = await view.GetPageSourceAsync();
                        Match idMatch = Regex.Match(source, @"g_steamID = ""(\d+)""");
                        if (idMatch.Success)
                        {
                            steamId = idMatch.Groups[1].Value;
                        }
                        else
                        {
                            idMatch = Regex.Match(source, @"steamid"":""(\d+)""");
                            if (idMatch.Success)
                            {
                                steamId = idMatch.Groups[1].Value;
                            }
                        }

                        if (idMatch.Success)
                        {
                            SetStoredCookies(GetWebCookies());
                            view.Close();
                        }
                    }
                };

                view.DeleteDomainCookies(".steamcommunity.com");
                view.DeleteDomainCookies("steamcommunity.com");
                view.DeleteDomainCookies("steampowered.com");
                view.DeleteDomainCookies("store.steampowered.com");
                view.DeleteDomainCookies("help.steampowered.com");
                view.DeleteDomainCookies("login.steampowered.com");
                view.Navigate(@"https://steamcommunity.com/login/home/?goto=");
                view.OpenDialog();
            }
        }

        public override void Save()
        {
            if (CurrentUser != null)
            {
                SaveKeys(CurrentUser.ApiKey);
            }
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
                string Url = string.Format(UrlApiUser, CurrentUser.ApiKey, CurrentUser.SteamId);
                string WebData = Web.DownloadStringData(Url, GetWebCookies()).GetAwaiter().GetResult();

                if (Serialization.TryFromJson(WebData, out PlayerSummaries playerSummaries))
                {
                    Player player = playerSummaries.response.players[0];
                    AccountInfos userInfos = new AccountInfos
                    {
                        UserId = long.Parse(player.steamid),
                        Avatar = player.avatarfull,
                        Pseudo = player.personaname,
                        Link = player.profileurl,
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
                Common.LogError(ex, false, true, PluginName);
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
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override ObservableCollection<GameAchievement> GetAchievements(string Id, AccountInfos accountInfos)
        {
            try
            {
                if (Id == "0")
                {
                    return new ObservableCollection<GameAchievement>();
                }

                string WebData = Web.DownloadStringData(string.Format(UrlGetGameAchievements, Id, CodeLang.GetSteamLang(Local))).GetAwaiter().GetResult();
                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();

                if (Serialization.TryFromJson(WebData, out SteamAchievements steamAchievements))
                {
                    if (steamAchievements?.response?.achievements == null)
                    {
                        return gameAchievements;
                    }

                    foreach (Achievement achievements in steamAchievements.response.achievements)
                    {
                        GameAchievement gameAchievement = new GameAchievement
                        {
                            Id = achievements.internal_name,
                            Name = achievements.localized_name,
                            Description = achievements.localized_desc,
                            UrlUnlocked = achievements.icon.IsNullOrEmpty() ? string.Empty : string.Format(UrlAchievementImg, Id, achievements.icon),
                            UrlLocked = achievements.icon_gray.IsNullOrEmpty() ? string.Empty : string.Format(UrlAchievementImg, Id, achievements.icon_gray),
                            DateUnlocked = default,
                            Percent = float.Parse(achievements.player_percent_unlocked?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) ?? "100"),
                            IsHidden = achievements.hidden
                        };
                        gameAchievements.Add(gameAchievement);
                    }
                }

                if (gameAchievements?.Count() == 0)
                {
                    return gameAchievements;
                }

                if (accountInfos != null && accountInfos.IsCurrent)
                {
                    if (CurrentUser.IsPrivateAccount || CurrentUser.ApiKey.IsNullOrEmpty())
                    {
                        gameAchievements = GetAchievementsByWeb(Id, gameAchievements);
                    }
                    else
                    {
                        using (dynamic steamWebAPI = WebAPI.GetInterface("ISteamUserStats", CurrentUser.ApiKey))
                        {
                            KeyValue PlayerAchievements = steamWebAPI.GetPlayerAchievements(steamid: CurrentUser.SteamId, appid: Id, l: CodeLang.GetSteamLang(Local));

                            if (PlayerAchievements != null && PlayerAchievements.Children != null)
                            {
                                KeyValue PlayerAchievementsData = PlayerAchievements.Children.Find(x => x.Name == "achievements");
                                if (PlayerAchievementsData != null)
                                {
                                    foreach (KeyValue AchievementsData in PlayerAchievementsData.Children)
                                    {
                                        string ApiName = AchievementsData.Children.Find(x => x.Name == "apiname").Value;
                                        string Name = AchievementsData.Children.Find(x => x.Name == "name").Value;
                                        string Description = AchievementsData.Children.Find(x => x.Name == "description").Value;
                                        bool achieved = int.Parse(AchievementsData.Children.Find(x => x.Name == "achieved").Value) == 1;
                                        int.TryParse(AchievementsData.Children.Find(x => x.Name == "unlocktime").Value, out int unlocktime);
                                        DateTime DateUnlocked = achieved ? new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(unlocktime).ToLocalTime() : default;

                                        gameAchievements.Where(x => x.Id.IsEqual(ApiName)).FirstOrDefault().DateUnlocked = DateUnlocked;
                                    }
                                }
                            }
                        }
                    }
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

        private ObservableCollection<GameAchievement> GetAchievementsByWeb(string Id, ObservableCollection<GameAchievement> gameAchievement)
        {
            string lang = "english";
            bool needLocalized = false;
            DateTime[] unlockedDates = null;

            try
            {
                do
                {
                    string UrlById = string.Format(UrlProfileById, CurrentUser.SteamId) + $"/stats/{Id}/achievements?l={lang}";
                    string UrlByPersona = string.Format(UrlProfileByName, CurrentUser.PersonaName) + $"/stats/{Id}?l={lang}";
                    needLocalized = false;

                    List<HttpCookie> cookies = GetStoredCookies();
                    string Url = UrlById;
                    string ResultWeb = Web.DownloadStringData(Url, cookies, string.Empty, true).GetAwaiter().GetResult();
                    if (ResultWeb.IndexOf("achieveRow") == -1)
                    {
                        Url = UrlByPersona;
                        ResultWeb = Web.DownloadStringData(UrlByPersona, cookies, string.Empty, true).GetAwaiter().GetResult();
                    }

                    if (ResultWeb.IndexOf("achieveRow") > -1)
                    {
                        IHtmlDocument htmlDocument = new HtmlParser().Parse(ResultWeb);
                        int i = 0;
                        IHtmlCollection<IElement> elements = htmlDocument.QuerySelectorAll(".achieveRow");
                        foreach (IElement el in elements)
                        {
                            string UrlUnlocked = el.QuerySelector(".achieveImgHolder img")?.GetAttribute("src") ?? string.Empty;
                            string Name = el.QuerySelector(".achieveTxtHolder h3").InnerHtml;
                            string Description = el.QuerySelector(".achieveTxtHolder h5").InnerHtml;

                            DateTime DateUnlocked = default;

                            if (lang.Equals("english"))
                            {
                                string stringDateUnlocked = el.QuerySelector(".achieveUnlockTime")?.InnerHtml ?? string.Empty;

                                if (!stringDateUnlocked.IsNullOrEmpty())
                                {
                                    stringDateUnlocked = stringDateUnlocked.Replace("Unlocked", string.Empty).Replace("<br>", string.Empty).Trim();
                                    DateTime.TryParseExact(stringDateUnlocked, new[] { "d MMM, yyyy @ h:mmtt", "d MMM @ h:mmtt" }, new CultureInfo("en-US"), DateTimeStyles.AssumeLocal, out DateUnlocked);
                                }

                                if (unlockedDates == null)
                                {
                                    unlockedDates = new DateTime[elements.Length];
                                }
                                unlockedDates[i] = DateUnlocked;
                            }
                            else if (i < unlockedDates?.Length)
                            {
                                DateUnlocked = unlockedDates[i];
                            }

                            if (DateUnlocked != default)
                            {
                                List<GameAchievement> achievements = gameAchievement.Where(x => x.UrlUnlocked.Split('/').Last().IsEqual(UrlUnlocked.Split('/').Last())).ToList();

                                if (achievements.Count == 1)
                                {
                                    achievements[0].DateUnlocked = DateUnlocked;
                                }
                                else
                                {
                                    var achievement = achievements.Find(x => x.Name.IsEqual(Name));
                                    if (achievement != null)
                                    {
                                        achievement.DateUnlocked = DateUnlocked;
                                    }
                                    else
                                    {
                                        if (!CodeLang.GetSteamLang(Local).IsEqual(lang))
                                        {
                                            needLocalized = true;
                                        }
                                    }
                                }
                            }
                            i++;
                        }

                        if (needLocalized)
                        {
                            lang = CodeLang.GetSteamLang(Local);
                        }
                    }
                    else if (ResultWeb.IndexOf("The specified profile could not be found") > -1)
                    {

                    }
                } while (needLocalized);
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return gameAchievement;
        }

        public override ObservableCollection<AccountWishlist> GetWishlist(AccountInfos accountInfos)
        {
            if (accountInfos != null)
            {
                for (int iPage = 0; iPage < 10; iPage++)
                {
                    string url = string.Format(UrlWishlist, accountInfos.UserId, iPage);
                    string response;
                    try
                    {
                        response = Web.DownloadStringData(url, GetStoredCookies()).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error download {ClientName} wishlist for page {iPage}", true, PluginName);
                        return null;
                    }

                    if (response.ToLower().Contains("{\"success\":2}"))
                    {
                        Logger.Warn($"Private wishlist for {accountInfos.UserId}?");
                        API.Instance.Notifications.Add(new NotificationMessage(
                            $"{PluginName}-steam-wishlist-{accountInfos.UserId}",
                            $"{PluginName}" + Environment.NewLine + ResourceProvider.GetString("LOCSteamPrivateAccount"),
                            NotificationType.Error
                        ));
                    }

                    if (!response.IsNullOrEmpty())
                    {
                        if (response == "[]")
                        {
                            Logger.Info($"No result after page {iPage} for {ClientName} wishlist");
                            break;
                        }

                        try
                        {
                            ObservableCollection<AccountWishlist> data = new ObservableCollection<AccountWishlist>();
                            dynamic resultObj = Serialization.FromJson<dynamic>(response);

                            foreach (dynamic gameWishlist in resultObj)
                            {
                                string Id = string.Empty;
                                string Name = string.Empty;
                                DateTime? Released = null;
                                DateTime? Added = null;
                                string Image = string.Empty;

                                try
                                {
                                    dynamic gameWishlistData = (dynamic)gameWishlist.Value;

                                    Id = gameWishlist.Name;
                                    Name = WebUtility.HtmlDecode((string)gameWishlistData["name"]);

                                    string release_date = ((string)gameWishlistData["release_date"])?.Split('.')[0];
                                    if (int.TryParse(release_date, out int release_date_int))
                                    {
                                        Released = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(release_date_int).ToUniversalTime();
                                    }

                                    string added_date = ((string)gameWishlistData["added"])?.Split('.')[0];
                                    if (int.TryParse(added_date, out int added_date_int))
                                    {
                                        Added = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(added_date_int).ToUniversalTime();
                                    }

                                    Image = (string)gameWishlistData["capsule"];

                                    data.Add(new AccountWishlist
                                    {
                                        Id = Id,
                                        Name = Name,
                                        Link = "https://store.steampowered.com/app/" + Id,
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
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, $"Error in parse {ClientName} wishlist", true, PluginName);
                        }
                    }
                    else
                    {
                        Logger.Warn($"No wishlist for {accountInfos.UserId}?");
                    }
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
                    List<HttpCookie> cookies = GetStoredCookies();
                    string sessionid = cookies?.FirstOrDefault(x => x.Name.IsEqual("sessionid"))?.Value;
                    if (sessionid.IsNullOrEmpty())
                    {
                        Logger.Warn($"Steam is not authenticate");
                        API.Instance.Notifications.Add(new NotificationMessage(
                            $"{PluginName}-steam-notlogged",
                            $"{PluginName}" + Environment.NewLine + ResourceProvider.GetString("LOCSteamNotLoggedIn"),
                            NotificationType.Error
                        ));
                    }

                    string url = string.Format(UrlWishlistRemove, CurrentAccountInfos.UserId);

                    Dictionary<string, string> data = new Dictionary<string, string>
                    {
                        { "sessionid", sessionid },
                        { "appid", id }
                    };
                    FormUrlEncodedContent formContent = new FormUrlEncodedContent(data);

                    string response = Web.PostStringDataCookies(url, formContent, cookies).GetAwaiter().GetResult();
                    return response.IndexOf("{\"success\":true") > -1;
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
        public override GameInfos GetGameInfos(string Id, AccountInfos accountInfos)
        {
            try
            {
                string Url = string.Format(UrlApiGameDetails, Id, CodeLang.GetSteamLang(Local));
                string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();

                if (Serialization.TryFromJson(WebData, out Dictionary<string, StoreAppDetailsResult> parsedData))
                {
                    StoreAppDetailsResult storeAppDetailsResult = parsedData[Id];

                    GameInfos gameInfos = new GameInfos
                    {
                        Id = storeAppDetailsResult?.data.steam_appid.ToString(),
                        Name = storeAppDetailsResult?.data.name,
                        Link = string.Format(UrlSteamGame, Id, CodeLang.GetSteamLang(Local)),
                        Image = storeAppDetailsResult?.data.header_image,
                        Description = ParseDescription(storeAppDetailsResult?.data.about_the_game)
                    };

                    // DLC
                    List<int> DlcsIdSteam = storeAppDetailsResult?.data.dlc ?? new List<int>();
                    List<int> DlcsIdSteamDb = GetFromSteamDb(storeAppDetailsResult?.data.steam_appid.ToString());
                    List<int> DlcsId = DlcsIdSteam.Union(DlcsIdSteamDb).Distinct().OrderBy(x => x).ToList();

                    if (DlcsId.Count > 0)
                    {
                        ObservableCollection<DlcInfos> Dlcs = GetDlcInfos(DlcsId, accountInfos);
                        gameInfos.Dlcs = Dlcs;
                    }

                    return gameInfos;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override ObservableCollection<DlcInfos> GetDlcInfos(string Id, AccountInfos accountInfos)
        {
            GameInfos gameInfos = GetGameInfos(Id, accountInfos);
            return gameInfos?.Dlcs ?? new ObservableCollection<DlcInfos>();
        }

        public ObservableCollection<DlcInfos> GetDlcInfos(List<int> dlcs, AccountInfos accountInfos)
        {
            try
            {
                ObservableCollection<DlcInfos> Dlcs = new ObservableCollection<DlcInfos>();
                dlcs.ForEach(x =>
                {
                    string Url = string.Format(UrlApiGameDetails, x, CodeLang.GetSteamLang(Local));
                    string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();

                    if (Serialization.TryFromJson(WebData, out Dictionary<string, StoreAppDetailsResult> parsedData))
                    {
                        StoreAppDetailsResult storeAppDetailsResult = parsedData[x.ToString()];
                        bool IsOwned = false;
                        if (accountInfos != null && accountInfos.IsCurrent)
                        {
                            IsOwned = DlcIsOwned(storeAppDetailsResult?.data.steam_appid.ToString());
                        }

                        DlcInfos dlc = new DlcInfos
                        {
                            Id = storeAppDetailsResult.data.steam_appid.ToString(),
                            Name = storeAppDetailsResult.data.name,
                            Description = ParseDescription(storeAppDetailsResult?.data.about_the_game),
                            Image = storeAppDetailsResult.data.header_image,
                            Link = string.Format(UrlSteamGame, storeAppDetailsResult.data.steam_appid.ToString(), CodeLang.GetSteamLang(Local)),
                            IsOwned = IsOwned,
                            Price = storeAppDetailsResult.data.is_free ? "0" : storeAppDetailsResult.data.price_overview?.final_formatted,
                            PriceBase = storeAppDetailsResult.data.is_free ? "0" : storeAppDetailsResult.data.price_overview?.initial_formatted
                        };

                        Dlcs.Add(dlc);
                    }
                });

                return Dlcs;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
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
                UserData?.rgOwnedApps?.ForEach(x =>
                {
                    GamesDlcsOwned.Add(new GameDlcOwned { Id = x.ToString() });
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


        #region Steam
        private string LoadKey()
        {
            string key = string.Empty;

            if (!FileSystem.FileExists(KeyPath))
            {
                return key;
            }

            try
            {
                key = Encryption.DecryptFromFile(
                    KeyPath,
                    Encoding.UTF8,
                    WindowsIdentity.GetCurrent().User.Value);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName , $"Failed to load {ClientName} API keys.");
            }

            return key;
        }

        private void SaveKeys(string key)
        {
            FileSystem.PrepareSaveFile(KeyPath);
            Encryption.EncryptToFile(
                KeyPath,
                key,
                Encoding.UTF8,
                WindowsIdentity.GetCurrent().User.Value);
        }



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
                _ = Serialization.TryFromJson(WebData, out Models.SteamApps appsListResponse);

                // Write file for cache
                if (appsListResponse?.applist?.apps != null)
                {
                    AppsList = appsListResponse.applist.apps;
                    File.WriteAllText(AppsListPath, Serialization.ToJson(AppsList), Encoding.UTF8);
                }
                else
                {
                    Logger.Warn($"appsListResponse is empty");
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
            List<int> found = AppsList.FindAll(x => x.name.IsEqual(Name, true)).Select(x => x.appid).Distinct().ToList();

            if (found != null && found.Count > 0)
            {
                if (found.Count > 1)
                {
                    Logger.Warn($"Found {found.Count} SteamAppId data for {Name}: " + string.Join(", ", found));
                    return 0;
                }

                Common.LogDebug(true, $"Found SteamAppId data for {Name} - {Serialization.ToJson(found)}");
                return found.First();
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
            App found = AppsList.Find(x => x.appid == AppId); ;

            if (found != null)
            {
                Common.LogDebug(true, $"Found {ClientName} data for {AppId} - {Serialization.ToJson(found)}");
                return found.name;
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
                Common.LogError(ex, false, true, PluginName);
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
                Common.LogError(ex, false, true, PluginName);
            }

            Logger.Warn($"No {ClientName} installation found");
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

            Logger.Warn($"No {ClientName} screenshots folder found");
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
                    Stream sFile = FileSystem.OpenReadFileStreamSafe(LoginUsersPath);
                    config.ReadAsText(sFile);
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
                    Common.LogError(ex, false, true, PluginName);
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
                    string ApiKey = LoadKey();

                    List<Models.SteamUser> SteamUsers = GetSteamUsers();
                    Models.SteamUser steamUser = SteamUsers?.Find(x => x.SteamId == SteamId);
                    if (steamUser != null)
                    {
                        _ = Task.Run(() =>
                          {
                              steamUser.IsPrivateAccount = !CheckIsPublic(steamUser).GetAwaiter().GetResult();
                              steamUser.AccountStatus = steamUser.IsPrivateAccount ? AccountStatus.Private : AccountStatus.Public;
                          });
                        
                        steamUser.ApiKey = ApiKey;
                    }
                    return steamUser;
                }
                else
                {
                    Logger.Warn("No SteamLibrary configuration found");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }


        public async Task<bool> CheckIsPublic(Models.SteamUser steamUser)
        {
            bool withId = await IsProfilePublic(string.Format(UrlProfileById, steamUser.SteamId));
            bool withPersona = await IsProfilePublic(string.Format(UrlProfileByName, steamUser.PersonaName));

            return withId || withPersona;
        }

        private async Task<bool> IsProfilePublic(string profilePageUrl)
        {
            return await IsProfilePublic(profilePageUrl, null);
        }

        private async Task<bool> IsProfilePublic(string profilePageUrl, List<HttpCookie> httpCookies)
        {
            try
            {
                string ResultWeb = await Web.DownloadStringData(profilePageUrl, httpCookies);
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


        private SteamUserData LoadUserData(bool OnlyNow = false)
        {
            if (File.Exists(FileUserData))
            {
                try
                {
                    DateTime DateLastWrite = File.GetLastWriteTime(FileUserData);
                    if (OnlyNow && !(DateLastWrite.ToString("yyyy-MM-dd") == DateTime.Now.ToString("yyyy-MM-dd")))
                    {
                        return null;
                    }

                    if (!OnlyNow)
                    {
                        LocalDateTimeConverter localDateTimeConverter = new LocalDateTimeConverter();
                        string formatedDateLastWrite = localDateTimeConverter.Convert(DateLastWrite, null, null, CultureInfo.CurrentCulture).ToString();
                        Logger.Warn($"Use saved UserData - {formatedDateLastWrite}");
                        API.Instance.Notifications.Add(new NotificationMessage(
                            $"{PluginName}-steam-saveddata",
                            $"{PluginName}" + Environment.NewLine
                                + string.Format(ResourceProvider.GetString("LOCCommonNotificationOldData"), "Steam", formatedDateLastWrite),
                            NotificationType.Info
                        ));
                    }

                    return Serialization.FromJsonFile<SteamUserData>(FileUserData);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }

            return null;
        }

        private void SaveUserData(SteamUserData UserData)
        {
            FileSystem.PrepareSaveFile(FileUserData);
            File.WriteAllText(FileUserData, Serialization.ToJson(UserData));
        }


        internal string ParseDescription(string description)
        {
            return description.Replace("%CDN_HOST_MEDIA_SSL%", "steamcdn-a.akamaihd.net");
        }
        #endregion


        private List<int> GetFromSteamDb(string AppId)
        {
            List<int> Dlcs = new List<int>();

            try
            {
                WebViewSettings settings = new WebViewSettings
                {
                    UserAgent = Web.UserAgent
                };

                using (IWebView WebViewOffScreen = API.Instance.WebViews.CreateOffscreenView(settings))
                {
                    WebViewOffScreen.NavigateAndWait(string.Format(SteamDbDlc, AppId));
                    string data = WebViewOffScreen.GetPageSource();

                    IHtmlDocument htmlDocument = new HtmlParser().Parse(data);
                    IHtmlCollection<IElement> SectionDlcs = htmlDocument.QuerySelectorAll("#dlc tr.app");
                    if (SectionDlcs != null)
                    {
                        foreach (IElement el in SectionDlcs)
                        {
                            string DlcIdString = el.QuerySelector("td a")?.InnerHtml;
                            int.TryParse(DlcIdString, out int DlcId);

                            if (DlcId != 0)
                            {
                                Dlcs.Add(DlcId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return Dlcs;
        }
    }
}
