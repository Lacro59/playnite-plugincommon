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
using System.Windows;
using System.Threading;

namespace CommonPluginsStores.Steam
{
    public class SteamApi : StoreApi
    {
        #region Url
        private string SteamDbDlc => "https://steamdb.info/app/{0}/dlc/";

        private static string UrlSteamCommunity => @"https://steamcommunity.com";
        private static string UrlApi => @"https://api.steampowered.com";
        private static string UrlStore => @"https://store.steampowered.com";

        private static string UrlAvatarFul => @"https://avatars.akamai.steamstatic.com/{0}_full.jpg";

        private static string UrlLogin => UrlSteamCommunity + @"/login/home/?goto=";
        private static string UrlProfileById => UrlSteamCommunity + @"/profiles/{0}";
        private static string UrlProfileByName => UrlSteamCommunity + @"/id/{0}";

        private static string UrlUserData => UrlStore + @"/dynamicstore/userdata/";
        private static string UrlWishlist => UrlStore + @"/wishlist/profiles/{0}/wishlistdata/?p={1}&v=";
        private static string UrlWishlistRemove => UrlStore + @"/api/removefromwishlist";

        private static string UrlApiAppsList => UrlApi +  @"/ISteamApps/GetAppList/v2/";
        private static string UrlApiFriendList => UrlApi + @"/ISteamUser/GetFriendList/v1/?key={0}&steamid={1}";
        private static string UrlApiPlayerSummaries => UrlApi + @"/ISteamUser/GetPlayerSummaries/v2/?key={0}&steamids={1}";
        private static string UrlGetPlayerAchievements => UrlApi + @"/ISteamUserStats/GetPlayerAchievements/v1/?appid={0}&language={1}&key={2}&steamid={3}";
        private static string UrlGetGameAchievements => UrlApi + @"/IPlayerService/GetGameAchievements/v1/?format=json&appid={0}&language={1}";

        private static string UrlApiGameDetails => UrlStore + @"/api/appdetails?appids={0}&l={1}";
        private static string UrlSteamGame => UrlApi + @"/app/{0}/?l={1}";

        private static string UrlAchievementImg => @"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{0}/{1}";
        #endregion


        protected List<App> appsList;

        internal List<App> AppsList
        {
            get
            {
                if (appsList == null)
                {
                    // From cache if exists & not expired
                    if (File.Exists(AppsListPath) && File.GetLastWriteTime(AppsListPath).AddDays(3) > DateTime.Now)
                    {
                        Common.LogDebug(true, "GetSteamAppListFromCache");
                        appsList = Serialization.FromJsonFile<List<App>>(AppsListPath);
                    }
                    // From web
                    else
                    {
                        Common.LogDebug(true, "GetSteamAppsListFromWeb");
                        appsList = GetSteamAppsList();
                    }
                }
                return appsList;
            }

            set => appsList = value;
        }


        private SteamUserData userData = null;
        public SteamUserData UserData
        {
            get
            {
                if (userData == null)
                {
                    try
                    {
                        userData = LoadUserData(true);
                        if (userData == null)
                        {
                            userData = GetUserData();
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, ClientName, true, PluginName);
                    }
                }
                return userData;
            }

            set => userData = value;
        }


        #region Paths
        private string AppsListPath { get; }
        private string FileUserData { get; }

        private string installationPath;
        public string InstallationPath
        {
            get
            {
                if (installationPath == null)
                {
                    installationPath = GetInstallationPath();
                }
                return installationPath;
            }

            set => SetValue(ref installationPath, value);
        }

        public string LoginUsersPath { get; }
        #endregion


        public SteamApi(string PluginName) : base(PluginName, ExternalPlugin.SteamLibrary, "Steam")
        {
            AppsListPath = Path.Combine(PathStoresData, "Steam_AppsList.json");
            FileUserData = Path.Combine(PathStoresData, "Steam_UserData.json");

            LoginUsersPath = Path.Combine(InstallationPath, "config", "loginusers.vdf");
        }


        #region Configuration
        protected override bool GetIsUserLoggedIn()
        {
            if (CurrentAccountInfos == null)
            {
                return false;
            }

            Task<bool> withId = IsProfilePublic(string.Format(UrlProfileById, CurrentAccountInfos.UserId), GetStoredCookies());
            Task<bool> withPersona = IsProfilePublic(string.Format(UrlProfileByName, CurrentAccountInfos.Pseudo), GetStoredCookies());
            Task.WaitAll(withId, withPersona);

            return withId.Result || withPersona.Result;
        }

        public override void Login()
        {
            ResetIsUserLoggedIn();
            string steamId = string.Empty;
            using (IWebView view = API.Instance.WebViews.CreateView(675, 440, Colors.Black))
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
                            _ = SetStoredCookies(GetWebCookies());

                            string JsonDataString = Tools.GetJsonInString(source, "g_rgProfileData = ", "\"};") + "\"}";
                            RgProfileData rgProfileData = Serialization.FromJson<RgProfileData>(JsonDataString);

                            string avatarhash = Tools.GetJsonInString(source, "https://avatars.akamai.steamstatic.com/", "_full.jpg\">");

                            CurrentAccountInfos = new AccountInfos
                            {
                                UserId = rgProfileData.SteamId.ToString(),
                                Avatar = string.Format(UrlAvatarFul, avatarhash),
                                Pseudo = rgProfileData.PersonaName,
                                Link = rgProfileData.Url,
                                IsCurrent = true
                            };
                            SaveCurrentUser();
                            _ = GetCurrentAccountInfos();

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

                view.Navigate(UrlLogin);
                _ = view.OpenDialog();
            }
        }

        public bool IsConfigured()
        {
            if (CurrentAccountInfos == null)
            {
                return false;
            }

            string SteamId = CurrentAccountInfos.UserId.ToString();
            string SteamUser = CurrentAccountInfos.Pseudo;

            return !SteamId.IsNullOrEmpty() && !SteamUser.IsNullOrEmpty();
        }
        #endregion


        #region Current user
        protected override AccountInfos GetCurrentAccountInfos()
        {
            AccountInfos accountInfos = LoadCurrentUser();
            if (accountInfos != null)
            {
                _ = Task.Run(() =>
                {
                    Thread.Sleep(1000);

                    if (CurrentAccountInfos.Avatar.IsNullOrEmpty() && IsConfigured())
                    {
                        ObservableCollection<AccountInfos> playerSummaries = GetPlayerSummaries(new List<ulong> { ulong.Parse(currentAccountInfos.UserId) });
                        CurrentAccountInfos.Avatar = playerSummaries?.FirstOrDefault().Avatar;
                    }

                    CurrentAccountInfos.IsPrivate = !CheckIsPublic(accountInfos).GetAwaiter().GetResult();
                    CurrentAccountInfos.AccountStatus = CurrentAccountInfos.IsPrivate ? AccountStatus.Private : AccountStatus.Public;
                });
                return accountInfos;
            }
            return new AccountInfos();
        }

        // TODO
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
        // TODO
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

        public override ObservableCollection<GameAchievement> GetAchievements(string id, AccountInfos accountInfos)
        {
            try
            {
                if (id.IsNullOrEmpty() || id == "0")
                {
                    return new ObservableCollection<GameAchievement>();
                }

                #region Get game achievements schema
                string WebData = Web.DownloadStringData(string.Format(UrlGetGameAchievements, id, CodeLang.GetSteamLang(Local))).GetAwaiter().GetResult();
                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();

                if (Serialization.TryFromJson(WebData, out SteamAchievements steamAchievements))
                {
                    if (steamAchievements?.Response?.Achievements == null)
                    {
                        return gameAchievements;
                    }

                    foreach (Achievement achievements in steamAchievements.Response.Achievements)
                    {
                        GameAchievement gameAchievement = new GameAchievement
                        {
                            Id = achievements.InternalName,
                            Name = achievements.LocalizedName,
                            Description = achievements.LocalizedDesc,
                            UrlUnlocked = achievements.Icon.IsNullOrEmpty() ? string.Empty : string.Format(UrlAchievementImg, id, achievements.Icon),
                            UrlLocked = achievements.IconGray.IsNullOrEmpty() ? string.Empty : string.Format(UrlAchievementImg, id, achievements.IconGray),
                            DateUnlocked = default,
                            Percent = float.Parse(achievements.PlayerPercentUnlocked?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) ?? "100"),
                            IsHidden = achievements.Hidden
                        };
                        gameAchievements.Add(gameAchievement);
                    }
                }
                #endregion

                if (gameAchievements?.Count() == 0)
                {
                    return gameAchievements;
                }

                if (accountInfos != null && accountInfos.IsCurrent)
                {
                    gameAchievements = accountInfos.IsPrivate || accountInfos.ApiKey.IsNullOrEmpty()
                        ? GetAchievementsByWeb(id, accountInfos, gameAchievements)
                        : GetAchievementsByApi(id, accountInfos, gameAchievements);
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
                    List<int> DlcsIdSteamDb = GetDlcFromSteamDb(storeAppDetailsResult?.data.steam_appid.ToString());
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
                            IsOwned = IsDlcOwned(storeAppDetailsResult?.data.steam_appid.ToString());
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
                UserData?.RgOwnedApps?.ForEach(x =>
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
        /// <summary>
        /// Get AppId from Steam store with a game name.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public int GetAppId(string Name)
        {
            if (AppsList == null)
            {
                Logger.Warn("AppsList is empty");
                return 0;
            }

            AppsList.Sort((x, y) => x.AppId.CompareTo(y.AppId));
            List<int> found = AppsList.FindAll(x => x.Name.IsEqual(Name, true)).Select(x => x.AppId).Distinct().ToList();

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
            App found = AppsList?.Find(x => x.AppId == AppId);

            if (found != null)
            {
                Common.LogDebug(true, $"Found {ClientName} data for {AppId} - {Serialization.ToJson(found)}");
                return found.Name;
            }
            else
            {
                Logger.Warn("AppsList is empty");
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
            if (CurrentAccountInfos != null)
            {
                string PathScreeshotsFolder = Path.Combine(InstallationPath, "userdata", GetAccountId(ulong.Parse(CurrentAccountInfos.UserId)).ToString(), "760", "remote");
                if (Directory.Exists(PathScreeshotsFolder))
                {
                    return PathScreeshotsFolder;
                }
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
                    _ = config.ReadAsText(sFile);
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

        public async Task<bool> CheckIsPublic(Models.SteamUser steamUser)
        {
            bool withId = await IsProfilePublic(string.Format(UrlProfileById, steamUser.SteamId));
            bool withPersona = await IsProfilePublic(string.Format(UrlProfileByName, steamUser.PersonaName));

            return withId || withPersona;
        }

        public async Task<bool> CheckIsPublic(AccountInfos accountInfos)
        {
            return accountInfos == null || accountInfos.UserId.IsNullOrEmpty() || accountInfos.UserId == "0" || accountInfos.Pseudo.IsNullOrEmpty()
                ? false
                : await CheckIsPublic(new Models.SteamUser { SteamId = ulong.Parse(accountInfos.UserId), PersonaName = accountInfos.Pseudo });
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
                                + string.Format(ResourceProvider.GetString("LOCCommonNotificationOldData"), ClientName, formatedDateLastWrite),
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

        private SteamUserData GetUserData()
        {
            using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
            {
                WebViewOffscreen.NavigateAndWait(UrlUserData);
                WebViewOffscreen.NavigateAndWait(UrlUserData);

                string data = WebViewOffscreen.GetPageText();
                if (Serialization.TryFromJson(data, out userData) && userData?.RgOwnedApps?.Count > 0)
                {
                    SaveUserData(userData);
                }
                else
                {
                    userData = LoadUserData();
                }

                return userData;
            }
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

        #region Steam Api
        /// <summary>
        /// Get the list of all games.
        /// </summary>
        /// <returns></returns>
        private List<App> GetSteamAppsList()
        {
            string Url = string.Empty;
            List<App> AppsList = null;
            try
            {
                string WebData = Web.DownloadStringData(UrlApiAppsList).GetAwaiter().GetResult();
                if (WebData.IsNullOrEmpty() || WebData == "{\"applist\":{\"apps\":[]}}")
                {
                    WebData = "{}";
                }
                _ = Serialization.TryFromJson(WebData, out Models.SteamApps appsListResponse);

                // Write file for cache
                if (appsListResponse?.AppList?.Apps != null)
                {
                    AppsList = appsListResponse.AppList.Apps;
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

        private ObservableCollection<AccountInfos> GetPlayerSummaries(List<ulong> steamIds)
        {
            try
            {
                string url = string.Format(UrlApiPlayerSummaries, currentAccountInfos.ApiKey, string.Join(",", steamIds));
                string webData = Web.DownloadStringData(url).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(webData, out SteamPlayerSummaries steamPlayerSummaries);

                ObservableCollection<AccountInfos> accountsInfos = new ObservableCollection<AccountInfos>();
                steamPlayerSummaries?.Response.Players?.ForEach(x =>
                {
                    AccountInfos userInfos = new AccountInfos
                    {
                        UserId = x.SteamId.ToString(),
                        Avatar = x.AvatarFull,
                        Pseudo = x.PersonaName,
                        Link = x.ProfileUrl
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

        private ObservableCollection<AccountInfos> GetCurrentFriendsInfosByApi()
        {
            try
            {
                string Url = string.Format(UrlApiFriendList, currentAccountInfos.ApiKey, currentAccountInfos.UserId);
                string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(WebData, out SteamFriendsList friendsList);

                List<ulong> steamIds = friendsList?.FriendsList?.Friends?.Select(x => x.SteamId)?.ToList() ?? new List<ulong>();
                ObservableCollection<AccountInfos> accountInfos = GetPlayerSummaries(steamIds);

                friendsList?.FriendsList?.Friends?.ForEach(x =>
                {
                    DateTime? DateAdded = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(x.FriendSince);
                    AccountInfos userInfos = accountInfos?.FirstOrDefault(y => ulong.Parse(y.UserId) == x.SteamId);

                    if (userInfos != null)
                    {
                        userInfos.DateAdded = DateAdded;
                    }
                    else
                    {
                        accountInfos.Add(new AccountInfos
                        {
                            UserId = x.SteamId.ToString(),
                            DateAdded = DateAdded
                        });
                    }
                });

                return accountInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        private ObservableCollection<GameAchievement> GetAchievementsByApi(string id, AccountInfos accountInfos, ObservableCollection<GameAchievement> gameAchievements)
        {
            using (dynamic steamWebAPI = WebAPI.GetInterface("ISteamUserStats", accountInfos.ApiKey))
            {
                KeyValue PlayerAchievements = steamWebAPI.GetPlayerAchievements(steamid: accountInfos.UserId, appid: id, l: CodeLang.GetSteamLang(Local));

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
                            _ = int.TryParse(AchievementsData.Children.Find(x => x.Name == "unlocktime").Value, out int unlocktime);
                            DateTime DateUnlocked = achieved ? new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(unlocktime).ToLocalTime() : default;

                            gameAchievements.FirstOrDefault(x => x.Id.IsEqual(ApiName)).DateUnlocked = DateUnlocked;
                        }
                    }
                }

                return gameAchievements;
            }
        }
        #endregion

        #region Steam Web
        private ObservableCollection<GameAchievement> GetAchievementsByWeb(string id, AccountInfos accountInfos, ObservableCollection<GameAchievement> gameAchievements)
        {
            string lang = "english";
            bool needLocalized = false;
            DateTime[] unlockedDates = null;

            try
            {
                do
                {
                    string UrlById = string.Format(UrlProfileById, accountInfos.UserId) + $"/stats/{id}/achievements?l={lang}";
                    string UrlByPersona = string.Format(UrlProfileByName, accountInfos.Pseudo) + $"/stats/{id}?l={lang}";
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
                                    _ = DateTime.TryParseExact(stringDateUnlocked, new[] { "d MMM, yyyy @ h:mmtt", "d MMM @ h:mmtt" }, new CultureInfo("en-US"), DateTimeStyles.AssumeLocal, out DateUnlocked);
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
                                List<GameAchievement> achievements = gameAchievements.Where(x => x.UrlUnlocked.Split('/').Last().IsEqual(UrlUnlocked.Split('/').Last())).ToList();

                                if (achievements.Count == 1)
                                {
                                    achievements[0].DateUnlocked = DateUnlocked;
                                }
                                else
                                {
                                    GameAchievement achievement = achievements.Find(x => x.Name.IsEqual(Name));
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

            return gameAchievements;
        }
        #endregion


        private List<int> GetDlcFromSteamDb(string AppId)
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
                            _ = int.TryParse(DlcIdString, out int DlcId);

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
