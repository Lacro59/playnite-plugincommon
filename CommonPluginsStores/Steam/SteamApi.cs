using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPlayniteShared.Common;
using CommonPlayniteShared.PluginLibrary.SteamLibrary;
using CommonPlayniteShared.PluginLibrary.SteamLibrary.SteamShared;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using CommonPluginsStores.Models.Enumerations;
using CommonPluginsStores.Steam.Models;
using CommonPluginsStores.Steam.Models.SteamKit;
using FuzzySharp;
using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SystemChecker.Clients;
using static CommonPlayniteShared.PluginLibrary.SteamLibrary.SteamShared.StoreAppDetailsResult.AppDetails;
using static CommonPlayniteShared.PluginLibrary.XboxLibrary.Models.TitleHistoryResponse;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Steam
{
    public class SteamApi : StoreApi
    {
		#region Urls

		private static string SteamDbDlc => "https://steamdb.info/app/{0}/dlc/";
        private static string SteamDbExtensionAchievements => "https://steamdb.info/api/ExtensionGetAchievements/?appid={0}";

        private static string UrlCapsuleSteam => "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{0}/capsule_184x69.jpg";

        private static string UrlSteamCommunity => @"https://steamcommunity.com";
        private static string UrlApi => @"https://api.steampowered.com";
        private static string UrlStore => @"https://store.steampowered.com";
        private static string UrlLogin => @"https://login.steampowered.com";

        private static string UrlAvatarFul => @"https://avatars.akamai.steamstatic.com/{0}_full.jpg";

		private static string UrlWishlistApi => UrlApi + @"/IWishlistService/GetWishlist/v1";
		private static string UrlWishlistByApi => UrlWishlistApi + @"?steamid={0}&key={1}";
        private static string UrlGetAppListApi => UrlApi + @"/IStoreService/GetAppList/v1";
        private static string UrlGetOwnedGamesApi => UrlApi + @"/IPlayerService/GetOwnedGames/v1";


        private static string UrlRefreshToken => UrlLogin + @"/jwt/refresh?redir={0}";

        private static string UrlProfileLogin => UrlSteamCommunity + @"/login/home/?goto=";
        private static string UrlProfileById => UrlSteamCommunity + @"/profiles/{0}";
        private static string UrlProfileByName => UrlSteamCommunity + @"/id/{0}";
        private static string UrlFriends => UrlSteamCommunity + @"/profiles/{0}/friends";
        private static string UrlProfileGamesById => UrlProfileById + @"/games/?tab=all";
        private static string UrlProfilById => UrlProfileById + @"/stats/{1}?tab=achievements&l={2}";

        private static string UrlAccount => UrlStore + @"/account";
        private static string UrlUserData => UrlStore + @"/dynamicstore/userdata/";
        private static string UrlWishlist => UrlStore + @"/wishlist/profiles/{0}/";
        private static string UrlWishlistRemove => UrlStore + @"/api/removefromwishlist";

        private static string UrlApiGameDetails => UrlStore + @"/api/appdetails?appids={0}&l={1}";
        private static string UrlSteamGame => UrlStore + @"/app/{0}";
        private static string UrlSteamGameLocalised => UrlStore + @"/app/{0}/?l={1}";

		private static string RecommendationQueueUrl => UrlStore + @"/explore/";

		private static string UrlSteamGameSearch => UrlStore + @"/api/storesearch/?term={0}&cc={1}&l={1}";

		#endregion

		private List<SteamApp> _steamApps;
		private Dictionary<uint, SteamApp> _steamAppsDict;

		/// <summary>
		/// Cached list of all Steam applications.
		/// Automatically loads from cache if available and not expired (3 days),
		/// otherwise fetches from Steam API and caches the result.
		/// </summary>
		protected List<SteamApp> SteamApps
		{
			get
			{
				if (_steamApps == null)
				{
					// 1. Try to load from cache (valid for 3 days)
					_steamApps = FileDataTools.LoadData<List<SteamApp>>(AppsListPath, 4320);

					// 2. If cache is expired or missing, fetch from Web
					if (_steamApps == null)
					{
						Common.LogDebug(true, "GetSteamAppsListFromWeb");

						List<SteamApp> steamAppsNew = null;

						// Determine retrieval method
						if (CurrentAccountInfos == null || CurrentAccountInfos.ApiKey.IsNullOrEmpty())
						{
							if (!(StoreToken?.Token.IsNullOrEmpty() ?? true))
							{
								steamAppsNew = GetSteamAppsByWebToken();
							}
							else
							{
								Logger.Warn("Steam API key and web token are both missing - cannot fetch app list");
							}
						}
						else
						{
							steamAppsNew = SteamKit.GetAppList(CurrentAccountInfos.ApiKey);
						}

						// 3. Load existing cache to merge with new data
						// Use -1 to load silently if we have new data to add, otherwise 0 to flag as expired
						_steamApps = FileDataTools.LoadData<List<SteamApp>>(AppsListPath, steamAppsNew?.Count > 0 ? -1 : 0)
									 ?? new List<SteamApp>();

						// 4. Merge new apps without duplicates
						if (steamAppsNew != null && steamAppsNew.Count > 0)
						{
							// Use a HashSet for high-performance O(1) lookup
							var existingIds = new HashSet<uint>(_steamApps.Select(a => a.AppId));

							// Filter apps that aren't already in the list
							var distinctNewApps = steamAppsNew
								.Where(a => existingIds.Add(a.AppId))
								.ToList();

							if (distinctNewApps.Count > 0)
							{
								_steamApps.AddRange(distinctNewApps);

								// Persist the updated list to disk
								FileSystem.WriteStringToFileSafe(AppsListPath, Serialization.ToJson(_steamApps));
							}
						}
					}
				}

				return _steamApps;
			}
			set
			{
				_steamApps = value;
				_steamAppsDict = null; // Reset dictionary cache on change
			}
		}

		/// <summary>
		/// Dictionary version of SteamApps for O(1) lookups by AppId
		/// </summary>
		protected Dictionary<uint, SteamApp> SteamAppsDict
		{
			get
			{
				if (_steamAppsDict == null && SteamApps != null)
				{
					_steamAppsDict = SteamApps.ToDictionary(x => x.AppId, x => x);
				}
				return _steamAppsDict;
			}
		}

		/// <summary>
		/// Gets Steam user data including owned games and wishlist.
		/// Uses cached data if available and not expired (10 minutes),
		/// otherwise fetches fresh data from Steam.
		/// </summary>
		private SteamUserData UserData => FileDataTools.LoadData<SteamUserData>(FileUserData, 10) ?? GetUserData() ?? FileDataTools.LoadData<SteamUserData>(FileUserData, 0);

        #region Paths

        private string AppsListPath { get; }
        private string FileUserData { get; }

        private string _installationPath;

		/// <summary>
		/// Gets or sets the Steam installation directory path.
		/// Automatically detects the path from registry if not already set.
		/// </summary>
		public string InstallationPath
        {
            get
            {
                if (_installationPath == null)
                {
                    _installationPath = GetInstallationPath();
                }
                return _installationPath;
            }

            set => SetValue(ref _installationPath, value);
        }

        public string LoginUsersPath { get; }

		#endregion

		/// <summary>
		/// Initializes a new instance of the SteamApi class.
		/// </summary>
		/// <param name="pluginName">Name of the plugin using this API</param>
		/// <param name="pluginLibrary">Reference to the external plugin library</param>
		public SteamApi(string pluginName, ExternalPlugin pluginLibrary) : base(pluginName, pluginLibrary, "Steam")
        {
            AppsListPath = Path.Combine(PathStoresData, "Steam_AppsList.json");
            FileUserData = Path.Combine(PathStoresData, "Steam_UserData.json");

			LoginUsersPath = Path.Combine(InstallationPath, "config", "loginusers.vdf");

            CookiesDomains = new List<string>
            {
                "steamcommunity.com", ".steamcommunity.com",
                "steampowered.com",  ".steampowered.com",
                "store.steampowered.com", ".store.steampowered.com",
                "checkout.steampowered.com", ".checkout.steampowered.com",
                "help.steampowered.com", ".help.steampowered.com",
                "login.steampowered.com", ".login.steampowered.com",
            };
        }

		#region Configuration

		/// <summary>
		/// Checks if the user is currently logged in to Steam.
		/// Attempts multiple verification methods including token refresh if initial check fails.
		/// </summary>
		/// <returns>True if user is authenticated and logged in, false otherwise</returns>
		protected override bool GetIsUserLoggedIn()
        {
            if (CurrentAccountInfos == null)
            {
                return false;
            }

            if (StoreSettings.UseAuth)
            {
                SteamUserData userData = GetUserData();
                bool isLogged = userData?.RgOwnedApps?.Count > 0;
                if (!isLogged)
                {
                    Thread.Sleep(2000);
                    userData = GetUserData();
                    isLogged = userData?.RgOwnedApps?.Count > 0;

                    // renew
                    if (!isLogged)
                    {
                        string url = string.Format(UrlRefreshToken, CurrentAccountInfos.Link);

                        Thread.Sleep(250);
                        List<HttpCookie> cookies = GetNewWebCookies(new List<string> { url, "https://steamcommunity.com/my", UrlStore });
                        _ = SetStoredCookies(cookies);
                        userData = GetUserData();
                        isLogged = userData?.RgOwnedApps?.Count > 0;

                        if (!isLogged)
                        {
                            Thread.Sleep(250);
                            cookies = GetNewWebCookies(new List<string> { url, "https://steamcommunity.com/my", UrlStore }, true);
                            _ = SetStoredCookies(cookies);
                            userData = GetUserData();
                            isLogged = userData?.RgOwnedApps?.Count > 0;
                        }

                        if (!isLogged)
                        {
                            Thread.Sleep(250);
                            cookies = GetNewWebCookies(new List<string> { url, "https://steamcommunity.com/my", UrlStore }, true);
                            _ = SetStoredCookies(cookies);
                            userData = GetUserData();
                            isLogged = userData?.RgOwnedApps?.Count > 0;
                        }
                    }
                }
                return isLogged;
            }

            Task<bool> withId = IsProfilePublic(string.Format(UrlProfileById, CurrentAccountInfos.UserId), GetStoredCookies());
            Task<bool> withPersona = IsProfilePublic(string.Format(UrlProfileByName, CurrentAccountInfos.Pseudo), GetStoredCookies());
            Task.WaitAll(withId, withPersona);

            return withId.Result || withPersona.Result;
        }

		/// <summary>
		/// Opens a web view for Steam login and captures authentication credentials.
		/// Saves the authenticated user information and cookies upon successful login.
		/// </summary>
		public override void Login()
        {
            ResetIsUserLoggedIn();
            string steamId = string.Empty;
            StoreToken = null;
            SetStoredToken(new StoreToken());

			var view = API.Instance.WebViews.CreateView(600, 720);
			try
			{
				view.LoadingChanged += CloseWhenLoggedIn;
				CookiesDomains.ForEach(x => { view.DeleteDomainCookies(x); });
				view.Navigate(RecommendationQueueUrl);

				view.OpenDialog();
                return;
			}
			catch (Exception e) when (!Debugger.IsAttached)
			{
				Common.LogError(e, false, "Failed to authenticate user.", false, PluginName);
				return;
			}
			finally
			{
				if (view != null)
				{
					view.LoadingChanged -= CloseWhenLoggedIn;
					_ = SetStoredCookies(GetWebCookies(false, view));
					view.Dispose();
				}
			}
        }

		private async void CloseWhenLoggedIn(object sender, WebViewLoadingChangedEventArgs e)
		{
			try
			{
                if (e.IsLoading) { return; };

				var view = (IWebView)sender;

                if (view.GetCurrentAddress().Contains("/profiles/"))
				{
					await GetSteamProfil(view);
					await GetSteamUserTokenFromWebViewAsync(view);
				}
                else
                {
					view.NavigateAndWait(UrlProfileLogin);
				}

                if (StoreToken?.Token != null)
				{
					view.Close();
                }
			}
			catch (Exception ex)
			{
				Logger.Warn(ex, "Failed to check authentication status");
			}
		}

		private async Task GetSteamUserTokenFromWebViewAsync(IWebView webView)
		{
			var url = webView.GetCurrentAddress();
            if (url.Contains("/login")) { return; }

			var source = await webView.GetPageSourceAsync();
			var userIdMatch = Regex.Match(source, "&quot;steamid&quot;:&quot;(?<id>[0-9]+)&quot;");
			var tokenMatch = Regex.Match(source, "&quot;webapi_token&quot;:&quot;(?<token>[^&]+)&quot;");

			if (!userIdMatch.Success || !tokenMatch.Success)
			{
				Logger.Warn("Could not find Steam user ID or token");
				return;
			}

			StoreToken = new StoreToken
            {
                AccountId = userIdMatch.Groups["id"].Value,
                Token = tokenMatch.Groups["token"].Value
            };
            SetStoredToken(StoreToken);
		}

        private async Task GetSteamProfil(IWebView webView)
        {
            string source = await webView.GetPageSourceAsync();
			AccountInfos accountInfos = GetAccountInfosFromRgProfileData(source);

			if (accountInfos != null)
            {
                CurrentAccountInfos = accountInfos;
                SaveCurrentUser();

                Logger.Info($"{ClientName} logged");
            }
		}

        private AccountInfos GetAccountInfosFromRgProfileData(string source)
        {
            AccountInfos accountInfos = null;
			var profileDatamatch = Regex.Match(source, @"g_rgProfileData\s*=\s*(?<json>\{.*?\});");
			var avatarMatch = Regex.Match(source, @"<link rel=""image_src"" href=""(?<avatar>[^""]+)"">");

			string json = string.Empty;
			if (profileDatamatch.Success)
			{
				json = profileDatamatch.Groups["json"].Value;
			}

			string avatar = string.Empty;
			if (avatarMatch.Success)
			{
				avatar = avatarMatch.Groups["avatar"].Value;
			}

			RgProfileData rgProfileData = Serialization.FromJson<RgProfileData>(json);
			if (rgProfileData != null)
			{
				accountInfos = new AccountInfos
				{
					ApiKey = CurrentAccountInfos?.ApiKey,
					UserId = rgProfileData.SteamId.ToString(),
					Avatar = avatar,
					Pseudo = rgProfileData.PersonaName,
					Link = rgProfileData.Url,
					IsPrivate = true,
					IsCurrent = true
				};
			}

            return accountInfos;
		}

		/// <summary>
		/// Checks if Steam API is properly configured with required user credentials.
		/// </summary>
		/// <returns>True if both Steam ID and username are configured, false otherwise</returns>
		public bool IsConfigured()
        {
            if (CurrentAccountInfos == null)
            {
                return false;
            }

            string steamId = CurrentAccountInfos.UserId;
            string steamUser = CurrentAccountInfos.Pseudo;

            return !steamId.IsNullOrEmpty() && !steamUser.IsNullOrEmpty();
        }

		#endregion

		#region Current user

		/// <summary>
		/// Gets the current authenticated user's account information.
		/// Automatically fetches updated profile data including avatar, username, and privacy status.
		/// </summary>
		/// <returns>AccountInfos object with current user details, or a new empty AccountInfos if not logged in</returns>
		protected override AccountInfos GetCurrentAccountInfos()
        {
            AccountInfos accountInfos = LoadCurrentUser();
            if (!accountInfos?.UserId?.IsNullOrEmpty() ?? false)
            {
                _ = Task.Run(() =>
                {
                    Thread.Sleep(1000);

                    if (CurrentAccountInfos.ApiKey.IsNullOrEmpty())
                    {
                        string response = Web.DownloadStringData(string.Format(UrlProfileById, accountInfos.UserId), GetStoredCookies()).GetAwaiter().GetResult();
                        AccountInfos newAccountInfos = GetAccountInfosFromRgProfileData(response);

                        if (newAccountInfos == null)
                        {
							response = Web.DownloadStringData(string.Format(UrlProfileByName, accountInfos.Pseudo), GetStoredCookies()).GetAwaiter().GetResult();
							newAccountInfos = GetAccountInfosFromRgProfileData(response);
						}

                        if (newAccountInfos != null)
						{
							CurrentAccountInfos.Avatar = newAccountInfos.Avatar;
                            CurrentAccountInfos.Pseudo = newAccountInfos.Pseudo;
                            CurrentAccountInfos.Link = newAccountInfos.Link;
						}
                    }
                    else if (ulong.TryParse(accountInfos.UserId, out ulong steamId))
                    {
                        ObservableCollection<AccountInfos> playerSummaries = GetPlayerSummaries(new List<ulong> { steamId });
                        CurrentAccountInfos.Avatar = playerSummaries?.FirstOrDefault().Avatar ?? CurrentAccountInfos.Avatar;
                        CurrentAccountInfos.Pseudo = playerSummaries?.FirstOrDefault().Pseudo ?? CurrentAccountInfos.Pseudo;
                        CurrentAccountInfos.Link = playerSummaries?.FirstOrDefault().Link ?? CurrentAccountInfos.Link;
                    }

                    CurrentAccountInfos.IsPrivate = !CheckIsPublic(accountInfos).GetAwaiter().GetResult();
                    CurrentAccountInfos.AccountStatus = CurrentAccountInfos.IsPrivate ? AccountStatus.Private : AccountStatus.Public;
                });
                return accountInfos;
            }
            return new AccountInfos { IsCurrent = true };
        }

		/// <summary>
		/// Gets the list of friends for the current user.
		/// Uses web scraping or API methods depending on authentication state and privacy settings.
		/// </summary>
		/// <returns>Collection of AccountInfos for each friend, or null on error</returns>
		protected override ObservableCollection<AccountInfos> GetCurrentFriendsInfos()
        {
            try
            {
                ObservableCollection<AccountInfos> accountInfos = null;
                if (CurrentAccountInfos != null && CurrentAccountInfos.IsCurrent)
                {
                    accountInfos = StoreSettings.UseAuth || CurrentAccountInfos.IsPrivate || !StoreSettings.UseApi || CurrentAccountInfos.ApiKey.IsNullOrEmpty()
                        ? GetCurrentFriendsInfosByWeb()
                        : GetCurrentFriendsInfosByApi();
                }
                return accountInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

		#endregion

		#region User details

		/// <summary>
		/// Gets detailed game information for a specific user account.
		/// Includes game names, playtime, achievements, and ownership status.
		/// </summary>
		/// <param name="accountInfos">The account to fetch game information for</param>
		/// <returns>Collection of AccountGameInfos for each game owned by the user</returns>
		public override ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos)
        {
            try
            {
                if (CurrentAccountInfos != null && CurrentAccountInfos.IsCurrent)
                {
                    ObservableCollection<AccountGameInfos> accountGameInfos = StoreSettings.UseAuth || CurrentAccountInfos.IsPrivate || !StoreSettings.UseApi || CurrentAccountInfos.ApiKey.IsNullOrEmpty()
                        ? GetAccountGamesInfosByWebToken(accountInfos)
                        : GetAccountGamesInfosByApi(accountInfos);
                    return accountGameInfos;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

		/// <summary>
		/// Gets achievement data for a specific game and user.
		/// Includes unlock status, dates, descriptions, and rarity percentages.
		/// </summary>
		/// <param name="id">Steam App ID of the game</param>
		/// <param name="accountInfos">Account to fetch achievements for</param>
		/// <returns>Collection of GameAchievement objects with unlock data</returns>
		public override ObservableCollection<GameAchievement> GetAchievements(string id, AccountInfos accountInfos)
        {
            try
            {
                if (!uint.TryParse(id, out uint appId))
                {
                    return new ObservableCollection<GameAchievement>();
                }

                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
                Tuple<string, ObservableCollection<GameAchievement>> data = GetAchievementsSchema(id);

                if (data?.Item2?.Count() == 0)
                {
                    return gameAchievements;
                }

                gameAchievements = data.Item2;

                gameAchievements = StoreSettings.UseAuth || accountInfos.IsPrivate || !StoreSettings.UseApi || accountInfos.ApiKey.IsNullOrEmpty()
                    ? GetAchievementsByWeb(appId, accountInfos, gameAchievements)
                    : GetAchievementsByApi(appId, accountInfos, gameAchievements);

                //gameAchievements = SetExtensionsAchievementsFromSteamDb(appId, gameAchievements);

                return gameAchievements;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

		/// <summary>
		/// Gets the user's Steam wishlist.
		/// </summary>
		/// <param name="accountInfos">Account to fetch wishlist for</param>
		/// <returns>Collection of AccountWishlist items with game details and dates added</returns>
		public override ObservableCollection<AccountWishlist> GetWishlist(AccountInfos accountInfos)
        {
            ObservableCollection<AccountWishlist> accountWishlists = new ObservableCollection<AccountWishlist>();
            if (accountInfos != null)
            {
                // Private with api key
                if (StoreSettings.UseApi && !accountInfos.ApiKey.IsNullOrEmpty())
                {
                    return GetWishlistByApi(accountInfos);
                }

                // Public
                if (!accountInfos.IsPrivate)
                {
                    return GetWishlistByApi(accountInfos);
                }

                // Private
                return GetWishlistByWebToken(accountInfos);
            }

            return accountWishlists;
        }

		/// <summary>
		/// Removes a game from the current user's Steam wishlist.
		/// Requires authenticated session with valid cookies.
		/// </summary>
		/// <param name="id">Steam App ID to remove from wishlist</param>
		/// <returns>True if successfully removed, false otherwise</returns>
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

		/// <summary>
		/// Gets comprehensive information about a specific Steam game.
		/// Includes name, description, release date, languages, and DLC list.
		/// </summary>
		/// <param name="id">Steam App ID</param>
		/// <param name="accountInfos">Account information for ownership checks</param>
		/// <returns>GameInfos object with game details</returns>
		public override GameInfos GetGameInfos(string id, AccountInfos accountInfos)
        {
            return GetGameInfos(id, accountInfos);
        }

		/// <summary>
		/// Internal method to get game information with optional minimal info mode.
		/// </summary>
		/// <param name="id">Steam App ID</param>
		/// <param name="accountInfos">Account information for ownership checks</param>
		/// <param name="minimalInfos">If true, returns only basic info without detailed DLC data</param>
		/// <returns>GameInfos object with game details</returns>
		private GameInfos GetGameInfos(string id, AccountInfos accountInfos, bool minimalInfos = false)
        {
            try
            {
                StoreAppDetailsResult storeAppDetailsResult = GetAppDetails(uint.Parse(id), 1);
                if (storeAppDetailsResult?.data == null)
                {
                    return null;
                }

                GameInfos gameInfos = new GameInfos
                {
                    Id = storeAppDetailsResult?.data.steam_appid.ToString(),
                    Name = storeAppDetailsResult?.data.name,
                    Link = string.Format(UrlSteamGameLocalised, id, CodeLang.GetSteamLang(Locale)),
                    Image = storeAppDetailsResult?.data.header_image,
                    Description = ParseDescription(storeAppDetailsResult?.data.about_the_game),
                    Languages = storeAppDetailsResult?.data.supported_languages,
                    Released = DateHelper.ParseReleaseDate(storeAppDetailsResult?.data?.release_date?.date)?.Date
				};

                // DLC
                List<uint> dlcsIdSteam = storeAppDetailsResult?.data.dlc ?? new List<uint>();
                List<uint> dlcsIdSteamDb = new List<uint>(); // GetDlcFromSteamDb(storeAppDetailsResult?.data.steam_appid ?? 0);
                List<uint> dlcsId = dlcsIdSteam.Union(dlcsIdSteamDb).Distinct().OrderBy(x => x).ToList();

                if (dlcsId.Count > 0 && !minimalInfos)
                {
                    ObservableCollection<DlcInfos> Dlcs = GetDlcInfos(dlcsId, accountInfos);
                    gameInfos.Dlcs = Dlcs;
                }
                else if (dlcsId.Count > 0)
                {
                    gameInfos.Dlcs = dlcsId.Select(x => new DlcInfos { Id = x.ToString() }).ToObservable();
                }

                return gameInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

		/// <summary>
		/// Gets the achievement schema for a game, including names, descriptions, and unlock percentages.
		/// Uses caching to minimize API calls (10 minute cache).
		/// </summary>
		/// <param name="id">Steam App ID</param>
		/// <returns>Tuple containing the app ID and collection of achievement definitions</returns>
		public override Tuple<string, ObservableCollection<GameAchievement>> GetAchievementsSchema(string id)
        {
            string cachePath = Path.Combine(PathAchievementsData, $"{id}.json");
            Tuple<string, ObservableCollection<GameAchievement>> data = FileDataTools.LoadData<Tuple<string, ObservableCollection<GameAchievement>>>(cachePath, 1440);

            if (data?.Item2 == null)
            {
                ObservableCollection<GameAchievement> gameAchievements = GetAchievementsSchema(uint.Parse(id));

                data = new Tuple<string, ObservableCollection<GameAchievement>>(id, gameAchievements);
                data?.Item2?.ForEach(x =>
                {
                    x.GamerScore = CalcGamerScore(x.Percent);
                });

				FileDataTools.SaveData(cachePath, data);
            }

            return data;
        }

		/// <summary>
		/// Gets DLC information for a specific game.
		/// </summary>
		/// <param name="id">Steam App ID of the base game</param>
		/// <param name="accountInfos">Account information for ownership checks</param>
		/// <returns>Collection of DlcInfos objects</returns>
		public override ObservableCollection<DlcInfos> GetDlcInfos(string id, AccountInfos accountInfos)
        {
            GameInfos gameInfos = GetGameInfos(id, accountInfos);
            return gameInfos?.Dlcs ?? new ObservableCollection<DlcInfos>();
        }

		/// <summary>
		/// Gets detailed information for multiple DLCs.
		/// </summary>
		/// <param name="ids">List of Steam App IDs for DLCs</param>
		/// <param name="accountInfos">Account information for ownership checks</param>
		/// <returns>Collection of DlcInfos with names, descriptions, prices, and ownership status</returns>
		public ObservableCollection<DlcInfos> GetDlcInfos(List<uint> ids, AccountInfos accountInfos)
        {
            try
            {
                ObservableCollection<DlcInfos> dlcs = new ObservableCollection<DlcInfos>();
                ids.ForEach(x =>
                {
                    StoreAppDetailsResult storeAppDetailsResult = GetAppDetails(x, 1);
                    if (storeAppDetailsResult?.data != null)
                    {
                        bool isOwned = false;
                        if (accountInfos != null && accountInfos.IsCurrent)
                        {
                            isOwned = IsDlcOwned(storeAppDetailsResult?.data.steam_appid.ToString());
                        }

                        DlcInfos dlc = new DlcInfos
                        {
                            Id = storeAppDetailsResult.data.steam_appid.ToString(),
                            Name = storeAppDetailsResult.data.name,
                            Description = ParseDescription(storeAppDetailsResult?.data.about_the_game),
                            Image = storeAppDetailsResult.data.header_image,
                            Link = string.Format(UrlSteamGameLocalised, storeAppDetailsResult.data.steam_appid.ToString(), CodeLang.GetSteamLang(Locale)),
                            IsOwned = isOwned,
                            Price = storeAppDetailsResult.data.is_free ? "0" : storeAppDetailsResult.data.price_overview?.final_formatted,
                            PriceBase = storeAppDetailsResult.data.is_free ? "0" : storeAppDetailsResult.data.price_overview?.initial_formatted
                        };

                        dlcs.Add(dlc);
                    }
                });

                return dlcs;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        #endregion

        #region Games owned
        
        //TODO Rewrite
        protected override ObservableCollection<GameDlcOwned> GetGamesDlcsOwned()
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
		/// Get AppId from Steam store with a game.
		/// </summary>
		/// <param name="game"></param>
		/// <returns></returns>
		public uint GetAppId(Game game)
		{
			// 1. Check links first
			uint appIdFromLinks = GetAppIdFromLinks(game);
			if (appIdFromLinks != 0)
			{
				return appIdFromLinks;
			}

			// 2. Try dictionary lookup if available
			if (SteamAppsDict != null)
			{
				var matches = SteamAppsDict.Values
					.Where(x => x.Name.IsEqual(game.Name, true))
					.ToList();

				if (matches.Count > 0)
				{
					if (matches.Count == 1)
					{
						return matches[0].AppId;
					}

					// Multiple matches - use fuzzy matching
					Logger.Warn($"Found {matches.Count} SteamAppId data for {game.Name}: " +
							   string.Join(", ", matches.Select(m => $"{m.AppId}:{m.Name}")));

					var best = matches
						.Select(x => new { MatchPercent = Fuzz.Ratio(game.Name.ToLower(), x.Name.ToLower()), AppId = x.AppId })
						.OrderByDescending(x => x.MatchPercent)
						.First();

					return best.MatchPercent >= 90 ? best.AppId : 0;
				}
			}

			// 3. Fallback to Steam search
			Logger.Warn("SteamApps lookup failed - Use Steam search");
			var search = GetSearchGame(game.Name, true);

			if (search.Count == 0)
			{
				Logger.Warn($"Steam appId not found for {game.Name}");
				return 0;
			}

			var bestMatch = search
				.Select(x => new { MatchPercent = Fuzz.Ratio(game.Name.ToLower(), x.Name.ToLower()), Data = x })
				.OrderByDescending(x => x.MatchPercent)
				.First();

			if (bestMatch.MatchPercent >= 90)
			{
				return uint.Parse(bestMatch.Data.Description);
			}

			Logger.Warn($"Steam appId not found for {game.Name} (best match: {bestMatch.MatchPercent}%)");
			return 0;
		}

		private uint GetAppIdFromLinks(Game game)
        {
            Link steamLink = game.Links?.FirstOrDefault(link => link.Name.ToLower() == "steam");
            if (steamLink == null)
            {
                return 0;
            }

            string[] linkSplit = steamLink.Url.Split(new[] { "/app/" }, StringSplitOptions.None);
            string steamIdString = linkSplit?.ElementAtOrDefault(1)?.Split('/').FirstOrDefault();
            if (steamIdString == null)
            {
                return 0;
            }

            bool success = uint.TryParse(steamIdString, out uint steamId);
            return !success ? 0 : steamId;
        }

		/// <summary>
		/// Get name from Steam store with an appId.
		/// </summary>
		/// <param name="appId"></param>
		/// <returns></returns>
		public string GetGameName(uint appId, bool searchWithApi = true)
		{
			if (SteamAppsDict != null && SteamAppsDict.TryGetValue(appId, out SteamApp found))
			{
				return found.Name;
			}
			else if (searchWithApi)
			{
				Logger.Warn($"Not found with SteamApps for {appId} - Use Steam API");

                var appDetails = GetAppDetails(appId, 1);
				if (appDetails?.data != null)
				{
					return appDetails.data.name;
				}
				else
				{
					Logger.Warn($"Not found {ClientName} data for {appId}");
				}
			}
			return $"SteamApp? - {appId}";
		}

		/// <summary>
		/// Get AccountID for a SteamId
		/// </summary>
		/// <returns></returns>
		public static uint GetAccountId(ulong steamId)
        {
            try
            {
                SteamID steamID = new SteamID();
                steamID.SetFromUInt64(steamId);
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

        public List<SteamStats> GetUsersStats(uint appId, AccountInfos accountInfos)
        {
            return StoreSettings.UseAuth || accountInfos.IsPrivate || !StoreSettings.UseApi || accountInfos.ApiKey.IsNullOrEmpty()
                        ? new List<SteamStats>()
                        : SteamKit.GetUserStatsForGame(accountInfos.ApiKey, appId, ulong.Parse(accountInfos.UserId));
        }

        public async Task<bool> CheckIsPublic(Models.SteamUser steamUser)
        {
            bool withId = await IsProfilePublic(string.Format(UrlProfileById, steamUser.SteamId));
            bool withPersona = await IsProfilePublic(string.Format(UrlProfileByName, steamUser.PersonaName));

            return withId || withPersona;
        }

        public async Task<bool> CheckIsPublic(AccountInfos accountInfos)
        {
            accountInfos.AccountStatus = AccountStatus.Checking;
            return accountInfos != null && !accountInfos.UserId.IsNullOrEmpty() && accountInfos.UserId != "0" && !accountInfos.Pseudo.IsNullOrEmpty() && await CheckIsPublic(new Models.SteamUser { SteamId = ulong.Parse(accountInfos.UserId), PersonaName = accountInfos.Pseudo });
        }

        private async Task<bool> IsProfilePublic(string profilePageUrl)
        {
            return await IsProfilePublic(profilePageUrl, null);
        }

        private async Task<bool> IsProfilePublic(string profilePageUrl, List<HttpCookie> httpCookies)
        {
            try
            {
                string response = await Web.DownloadStringData(profilePageUrl, httpCookies);
                IHtmlDocument HtmlDoc = new HtmlParser().Parse(response);
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

        private SteamUserData GetUserData()
        {
            try
            {
                List<HttpCookie> cookies = GetStoredCookies();
                string result = Web.DownloadStringData(UrlUserData, cookies, Web.UserAgent, true).GetAwaiter().GetResult();
                if (Serialization.TryFromJson(result, out SteamUserData userData, out Exception ex) && userData?.RgOwnedApps?.Count > 0)
                {
                    SaveUserData(userData);
                }
                else
                {
                    if (ex != null)
                    {
                        Common.LogError(ex, false, false, PluginName);
                    }
                }
                return userData;
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }

        private void SaveUserData(SteamUserData userData)
        {
            if (userData?.RgOwnedApps?.Count == 0)
            {
                return;
            }

            FileSystem.PrepareSaveFile(FileUserData);
            File.WriteAllText(FileUserData, Serialization.ToJson(userData));
        }

        private string ParseDescription(string description)
        {
            return description.Replace("%CDN_HOST_MEDIA_SSL%", "steamcdn-a.akamaihd.net");
        }


        public bool CheckGameIsPrivate(uint appId, AccountInfos accountInfos)
        {
            return StoreSettings.UseAuth || accountInfos.IsPrivate || !StoreSettings.UseApi || accountInfos.ApiKey.IsNullOrEmpty()
                ? CheckGameIsPrivateByWeb(appId, accountInfos)
                : SteamKit.CheckGameIsPrivate(accountInfos.ApiKey, appId, ulong.Parse(accountInfos.UserId));
        }

        #endregion

        #region Steam Api

        public StoreAppDetailsResult GetAppDetails(uint appId, int retryCount)
        {
            string cachePath = Path.Combine(PathAppsData, $"{appId}.json");
            StoreAppDetailsResult storeAppDetailsResult = FileDataTools.LoadData<StoreAppDetailsResult>(cachePath, 4320);

            if (storeAppDetailsResult == null)
            {
                Thread.Sleep(1000); // Prevent http 429
                string url = string.Format(UrlApiGameDetails, appId, CodeLang.GetSteamLang(Locale));
                string response = Web.DownloadStringData(url).GetAwaiter().GetResult();

                if (Serialization.TryFromJson(response, out Dictionary<string, StoreAppDetailsResult> parsedData, out Exception ex) && parsedData != null)
                {
                    storeAppDetailsResult = parsedData[appId.ToString()];
					FileDataTools.SaveData(cachePath, storeAppDetailsResult);
                }
                else if (ex != null)
                {
                    Common.LogError(ex, false, false, PluginName);
                }
                else
                {
                    if (response.Length < 25 && retryCount < 11)
                    {
                        Thread.Sleep(20000 * retryCount);
                        Logger.Warn($"Api limit for Steam with {appId} - {retryCount}");
                        retryCount++;
                        return GetAppDetails(appId, retryCount);
                    }
                    else if (retryCount == 10)
                    {
                        Logger.Warn($"Api limit exced for Steam with {appId}");
                    }
                    return null;
                }
            }

            return storeAppDetailsResult;
        }

		/// <summary>
		/// Downloads and parses the Windows PC system requirements for a given Steam App ID.
		/// Returns null if the app has no requirements data or the request fails.
		/// </summary>
		/// <param name="appId">Steam App ID</param>
		public GameRequirements GetGameRequirements(uint appId)
		{
			try
			{
				StoreAppDetailsResult storeAppDetailsResult = GetAppDetails(appId, 1);
				if (storeAppDetailsResult?.data == null)
				{
					Logger.Warn($"SteamApi.GetSystemRequirements - No app data for {appId}");
					return null;
				}

				string rawRequirements = Serialization.ToJson(storeAppDetailsResult.data.pc_requirements);
				if (rawRequirements == "[]" || rawRequirements.IsNullOrEmpty())
				{
					Logger.Warn($"SteamApi.GetSystemRequirements - No pc_requirements for {appId}");
					return null;
				}

				Serialization.TryFromJson(rawRequirements, out Requirement pcRequirements);
				if (pcRequirements == null)
				{
					return null;
				}

				GameRequirements result = new GameRequirements
				{
					Id = appId.ToString(),
					GameName = storeAppDetailsResult.data.name ?? string.Empty
				};

				if (!pcRequirements.minimum.IsNullOrEmpty())
				{
					result.Minimum = ParseSteamRequirementHtml(pcRequirements.minimum, true);
				}
				if (!pcRequirements.recommended.IsNullOrEmpty())
				{
					result.Recommended = ParseSteamRequirementHtml(pcRequirements.recommended, false);
				}

				result.SourceLink = new SourceLink
				{
					Name = "Steam",
					GameName = result.GameName,
					Url = $"https://store.steampowered.com/app/{appId}"
				};

				return result;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return null;
			}
		}

		private static RequirementEntry ParseSteamRequirementHtml(string html, bool isMinimum)
		{
			RequirementEntry entry = new RequirementEntry { IsMinimum = isMinimum };

			HtmlParser parser = new HtmlParser();
			IHtmlDocument document = parser.Parse(html);

			foreach (IElement element in document.QuerySelectorAll("li"))
			{
				Common.LogDebug(true, $"SteamApi.ParseSteamRequirementHtml - {element.InnerHtml}");

				if (element.InnerHtml.Contains("TBD", StringComparison.InvariantCultureIgnoreCase))
				{
					continue;
				}

				if (element.InnerHtml.IndexOf("<strong>OS") > -1)
				{
					ParseOs(element.InnerHtml, entry);
				}
				else if (element.InnerHtml.IndexOf("<strong>Processor") > -1)
				{
					ParseCpu(element.InnerHtml, entry);
				}
				else if (element.InnerHtml.IndexOf("<strong>Memory") > -1)
				{
					entry.Ram = ParseMemorySize(element.InnerHtml);
				}
				else if (element.InnerHtml.IndexOf("<strong>Graphics") > -1)
				{
					ParseGpu(element.InnerHtml, entry);
				}
				else if (element.InnerHtml.IndexOf("<strong>DirectX") > -1)
				{
					ParseDirectX(element.InnerHtml, entry);
				}
				else if (element.InnerHtml.IndexOf("<strong>Storage") > -1
					  || element.InnerHtml.IndexOf("<strong>Hard Drive") > -1)
				{
					entry.Storage = ParseStorageSize(element.InnerHtml);
				}
			}

			return entry;
		}

		private static void ParseOs(string html, RequirementEntry entry)
		{
			string os = Regex.Replace(html, "<[^>]*>", string.Empty)
				.ToLower()
				.Replace("\t", " ")
				.Replace("os: ", string.Empty)
				.Replace("os *: ", string.Empty)
				.Replace("pc/ms-dos 5.0", string.Empty)
				.Replace("(64-bit required)", string.Empty)
				.Replace("(32/64-bit)", string.Empty)
				.Replace("with platform update for  7 ( versions only)", string.Empty)
				.Replace("(vista+ probably works)", string.Empty)
				.Replace("win ", string.Empty)
				.Replace("windows", string.Empty)
				.Replace("microsoft", string.Empty)
				.Replace(", 32-bit", string.Empty)
				.Replace(", 32bit", string.Empty)
				.Replace(", 64-bit", string.Empty)
				.Replace(", 64bit", string.Empty)
				.Replace("®", string.Empty)
				.Replace("™", string.Empty)
				.Replace("+", string.Empty)
				.Replace("and above", string.Empty)
				.Replace("x32", string.Empty)
				.Replace("and", string.Empty)
				.Replace("x64", string.Empty)
				.Replace("32-bit", string.Empty)
				.Replace("32bit", string.Empty)
				.Replace("32 bit", string.Empty)
				.Replace("64-bit", string.Empty)
				.Replace("64bit", string.Empty)
				.Replace("64 bit", string.Empty)
				.Replace("latest service pack", string.Empty)
				.Replace("32-bit/64-bit", string.Empty)
				.Replace("32bit/64bit", string.Empty)
				.Replace("64-bit operating system required", string.Empty)
				.Replace("32-bit operating system required", string.Empty)
				.Replace(" operating system required", string.Empty)
				.Replace("operating system required", string.Empty)
				.Replace(" equivalent or better", string.Empty)
				.Replace(" or equivalent.", string.Empty)
				.Replace(" or equivalent", string.Empty)
				.Replace(" or newer", string.Empty)
				.Replace("or newer", string.Empty)
				.Replace("or later", string.Empty)
				.Replace("or higher", string.Empty)
				.Replace("()", string.Empty)
				.Trim();

			if (os.Trim() == "(/)" || os.Trim().IsNullOrEmpty())
			{
				return;
			}

			foreach (string token in os.Replace(",", "¤").Replace(" or ", "¤").Replace("/", "¤").Split('¤'))
			{
				string t = token.Trim();
				if (!t.IsNullOrEmpty())
				{
					entry.Os.Add(t);
				}
			}
		}

		private static void ParseCpu(string html, RequirementEntry entry)
		{
			string cpu = html
				.Replace("\t", " ")
				.Replace("<strong>Processor:</strong>", string.Empty)
				.Replace("<strong>Processor: </strong>", string.Empty)
				.Replace("&nbsp;", string.Empty)
				.Replace("GHz, or better)", "GHz)")
				.Replace("Requires a 64-bit processor and operating system", string.Empty)
				.Replace("More than a Pentium", string.Empty)
				.Replace("equivalent or higher processor", string.Empty)
				.Replace("- Low budget CPUs such as Celeron or Duron needs to be at about twice the CPU speed", string.Empty)
				.Replace(" equivalent or faster processor", string.Empty)
				.Replace(" equivalent or better", string.Empty)
				.Replace("above", string.Empty)
				.Replace("or similar", string.Empty)
				.Replace("or faster", string.Empty)
				.Replace("and up", string.Empty)
				.Replace("(or higher)", string.Empty)
				.Replace("or higher", string.Empty)
				.Replace(" or equivalent.", string.Empty)
				.Replace(" over", string.Empty)
				.Replace(" or faster", string.Empty)
				.Replace(" or better", string.Empty)
				.Replace(" or equivalent", string.Empty)
				.Replace(" or Equivalent", string.Empty)
				.Replace("4 CPUs", string.Empty)
				.Replace("(3 GHz Pentium® 4 recommended)", string.Empty)
				.Replace("ghz", "GHz")
				.Replace("Ghz", "GHz")
				.Replace("®", string.Empty)
				.Replace("™", string.Empty)
				.Replace("or later that's SSE2 capable", string.Empty)
				.Replace("Processor", string.Empty)
				.Replace("processor", string.Empty)
				.Replace("x86-compatible", string.Empty)
				.Replace("(not recommended for Intel HD Graphics cards)", ", not recommended for Intel HD Graphics cards")
				.Replace("()", string.Empty)
				.Replace("<br>", string.Empty)
				.Replace(", x86", string.Empty)
				.Replace("Yes", string.Empty)
				.Replace("GHz+", "GHz")
				.Trim();

			cpu = Regex.Replace(cpu, "<[^>]*>", string.Empty);
			cpu = Regex.Replace(cpu, @", ~?(\d+(\.\d+)?)", " $1", RegexOptions.IgnoreCase);
			cpu = Regex.Replace(cpu, @"(\d+),(\d+ GHz)", "$1.$2", RegexOptions.IgnoreCase);
			cpu = Regex.Replace(cpu, @"(\d+),(\d+) - (\d+ GHz)", "$3", RegexOptions.IgnoreCase);
			cpu = Regex.Replace(cpu, @"(\d+)GHz", "$1 GHz", RegexOptions.IgnoreCase);
			cpu = Regex.Replace(cpu, @"(\d+)k", "$1K", RegexOptions.IgnoreCase);
			cpu = Regex.Replace(cpu, @"(\d+\.\d+)\+ (GHz)", "$1 $2", RegexOptions.IgnoreCase);
			cpu = Regex.Replace(cpu, @"(,|\/|\sor\s|\sand\s|\|)", "¤", RegexOptions.IgnoreCase);

			foreach (string token in cpu.Split('¤'))
			{
				string t = token.Trim();
				if (!t.IsNullOrEmpty())
				{
					entry.Cpu.Add(t);
				}
			}
		}

		private static void ParseGpu(string html, RequirementEntry entry)
		{
			string gpu = Regex.Replace(html, @"with [(]?\d+[ ]?(MB)?(GB)?[)]? (Memory)?(Video RAM)?", string.Empty, RegexOptions.IgnoreCase);
			gpu = Regex.Replace(gpu, @"\(GTX \d+ or above required for VR\)", string.Empty, RegexOptions.IgnoreCase);
			gpu = Regex.Replace(gpu, @"DirectX \d class GPU with \dGB VRAM \(", string.Empty, RegexOptions.IgnoreCase);
			gpu = Regex.Replace(gpu, @"Shader Model \d+(\.\d+)?", string.Empty, RegexOptions.IgnoreCase);
			gpu = Regex.Replace(gpu, @"card capable of shader \d+(\.\d+)?", string.Empty, RegexOptions.IgnoreCase);
			gpu = Regex.Replace(gpu, @"DX\d+ Compliant with PS\d+(\.\d+)? support", string.Empty, RegexOptions.IgnoreCase);
			gpu = Regex.Replace(gpu, @"DX\d+ Compliant", string.Empty, RegexOptions.IgnoreCase);
			gpu = Regex.Replace(gpu, @"de \d+ GB", string.Empty, RegexOptions.IgnoreCase);
			gpu = Regex.Replace(gpu, @"GPU (\d+)GB VRAM", "GPU $1 GB VRAM", RegexOptions.IgnoreCase);
			gpu = Regex.Replace(gpu, @"(,|with)?\s*(\d+)\s*(GB|MB)(\s* system ram)?", " ($2 $3)", RegexOptions.IgnoreCase);
			gpu = Regex.Replace(gpu, @"\(A minimum of \(\d+ GB\) of VRAM\)", string.Empty, RegexOptions.IgnoreCase);

			gpu = gpu.Replace("\t", " ")
				.Replace("<strong>Graphics:</strong>", string.Empty)
				.Replace("<strong>Graphics: </strong>", string.Empty)
				.Replace("requires graphics card. minimum GPU needed: \"", string.Empty)
				.Replace("(Integrated Graphics)", string.Empty)
				.Replace("\" or similar", string.Empty)
				.Replace("Graphics card supporting", string.Empty)
				.Replace("compatible video card (integrated or dedicated with min 512MB memory)", string.Empty)
				.Replace("capable GPU", string.Empty)
				.Replace("at least ", string.Empty)
				.Replace(" capable.", string.Empty)
				.Replace(" or Higher VRAM Graphics Cards", string.Empty)
				.Replace("VRAM Graphics Cards", "VRAM")
				.Replace("hardware driver support required for WebGL acceleration. (AMD Catalyst 10.9, nVidia 358.50)", string.Empty)
				.Replace("ATI or NVidia card w/ 1024 MB RAM (NVIDIA GeForce GTX 260 or ATI HD 4890)", "NVIDIA GeForce GTX 260 or ATI HD 4890")
				.Replace("Video card must be 128 MB or more and should be a DirectX 9-compatible with support for Pixel Shader 2.0b (", string.Empty)
				.Replace("- *NOT* an Express graphics card).", string.Empty)
				.Replace("2GB (GeForce GTX 970 / amd RX 5500 XT)", "GeForce GTX 970 / AMD RX 5500 XT")
				.Replace(" - anything capable of running OpenGL 4.0 (eg. ATI Radeon HD 57xx or Nvidia GeForce 400 and higher)", string.Empty)
				.Replace("(AMD or NVIDIA equivalent)", string.Empty)
				.Replace("/320M 512MB VRAM", string.Empty)
				.Replace("/Intel Extreme Graphics 82845, 82865, 82915", string.Empty)
				.Replace(" 512MB VRAM (Intel integrated GPUs are not supported!)", " / Intel integrated GPUs are not supported!")
				.Replace("(not recommended for Intel HD Graphics cards)", ", not recommended for Intel HD Graphics cards")
				.Replace("or similar (no support for onboard cards)", string.Empty)
				.Replace("level Graphics Card (requires support for SSE)", string.Empty)
				.Replace("- Integrated graphics and very low budget cards might not work.", string.Empty)
				.Replace("3D with TnL support and", string.Empty)
				.Replace(" compatible", string.Empty)
				.Replace("of addressable memory", string.Empty)
				.Replace("Any", string.Empty)
				.Replace("any", string.Empty)
				.Replace("/Nvidia", " / Nvidia")
				.Replace("or AMD equivalent", string.Empty)
				.Replace("(Requires support for SSE)", string.Empty)
				.Replace("ATI or NVidia card", "Card")
				.Replace("w/", "with")
				.Replace("Graphics: ", string.Empty)
				.Replace(" equivalent or better", string.Empty)
				.Replace(" or equivalent.", string.Empty)
				.Replace("or equivalent.", string.Empty)
				.Replace(" or equivalent", string.Empty)
				.Replace(" or better.", string.Empty)
				.Replace("or better.", string.Empty)
				.Replace(" or better", string.Empty)
				.Replace(" or newer", string.Empty)
				.Replace("or newer", string.Empty)
				.Replace("or higher", string.Empty)
				.Replace("or better", string.Empty)
				.Replace("or greater graphics card", string.Empty)
				.Replace("or equivalent", string.Empty)
				.Replace("Mid-range", string.Empty)
				.Replace(" Memory Minimum", string.Empty)
				.Replace(" memory minimum", string.Empty)
				.Replace(" Memory Recommended", string.Empty)
				.Replace(" memory recommended", string.Empty)
				.Replace("e.g.", string.Empty)
				.Replace("Laptop integrated ", string.Empty)
				.Replace("GPU 1GB VRAM", "GPU 1 GB VRAM")
				.Replace("8GB Memory 8 GB RAM", "(8 GB)")
				.Replace(" or more and should be a DirectX 9-compatible with support for Pixel Shader 3.0", string.Empty)
				.Replace("Yes", string.Empty)
				.Replace(", or ", string.Empty)
				.Replace("()", string.Empty)
				.Replace("<br>", string.Empty)
				.Replace("®", string.Empty)
				.Replace("™", string.Empty)
				.Replace(" Compatible", string.Empty)
				.Replace("  ", " ")
				.Replace("(Shared Memory is not recommended)", string.Empty)
				.Replace(". Integrated Intel HD Graphics should work but is not supported; problems are generally solved with a driver update.", string.Empty)
				.Trim();

			gpu = Regex.Replace(gpu, "<[^>]*>", string.Empty);

			// Strip a leading VRAM size that has no GPU name
			string gpuVramOnly = Regex.Match(gpu.ToLower(), @"\d+((.|,)\d+)?[ ]?(mb|gb)").ToString().Trim();
			if (!gpuVramOnly.IsNullOrEmpty() && gpu.ToLower().IndexOf(gpuVramOnly) == 0)
			{
				gpu = gpuVramOnly;
			}

			gpu = Regex.Replace(gpu, @"(gb|mb)(\))?\s*\+", "$1$2", RegexOptions.IgnoreCase);
			gpu = Regex.Replace(gpu, @" - (\d+) (gb|mb)", " ($1 $2)", RegexOptions.IgnoreCase);
			gpu = gpu.Replace(",", "¤").Replace(" or ", "¤").Replace(" OR ", "¤")
					 .Replace(" / ", "¤").Replace(" /", "¤").Replace(" | ", "¤");

			foreach (string token in gpu.Split('¤'))
			{
				string t = token.Trim();
				if (t.IsNullOrEmpty()) { continue; }

				if (t.ToLower().IndexOf("nvidia") > -1 || t.ToLower().IndexOf("amd") > -1 || t.ToLower().IndexOf("intel") > -1)
				{
					entry.Gpu.Add(Regex.Replace(t, @"\(\d+\s*(mb|gb)\)", string.Empty, RegexOptions.IgnoreCase).Trim());
				}
				else if (Regex.IsMatch(t, @"\d+((.|,)\d+)?\s*(mb|gb)", RegexOptions.IgnoreCase) && t.Length < 10)
				{
					entry.Gpu.Add(t.ToUpper().Replace("(", string.Empty).Replace(")", string.Empty).Trim() + " VRAM");
				}
				else if (Regex.IsMatch(t, @"\(\d+((.|,)\d+)?\s*(mb|gb)\) vram", RegexOptions.IgnoreCase))
				{
					entry.Gpu.Add(t.ToUpper().Replace("(", string.Empty).Replace(")", string.Empty).Trim());
				}
				else if (Regex.IsMatch(t, @"\(\d+((.|,)\d+)?\s*(mb|gb)\) ram", RegexOptions.IgnoreCase))
				{
					entry.Gpu.Add(t.ToUpper().Replace("(", string.Empty).Replace(")", string.Empty).Replace("RAM", "VRAM").Trim());
				}
				else if (Regex.IsMatch(t, @"DirectX \d+[.]\d", RegexOptions.IgnoreCase))
				{
					entry.Gpu.Add(Regex.Replace(t, @"[.]\d", string.Empty));
				}
				else
				{
					entry.Gpu.Add(t);
				}
			}
		}

		private static void ParseDirectX(string html, RequirementEntry entry)
		{
			int[] versions = { 8, 9, 10, 11, 12 };
			foreach (int version in versions)
			{
				string dx = $"DirectX {version}";
				if (html.IndexOf(version.ToString()) > -1 && entry.Gpu.Find(x => x.IsEqual(dx)) == null)
				{
					entry.Gpu.Add(dx);
				}
			}
		}

		/// <summary>Parses "NNN MB/GB" or "NNN.N GB/MB" from a raw HTML element string into bytes.</summary>
		private static double ParseMemorySize(string html)
		{
			string ram = Regex.Match(html.ToLower()
				.Replace("\t", " ")
				.Replace("<strong>memory:</strong>", string.Empty)
				.Replace("<strong>memory: </strong>", string.Empty),
				@"\d+((.|,)\d+)?[ ]?(mb|gb)").ToString().Trim();

			// Keep highest value when multiple sizes appear (e.g. "4/8 GB")
			ram = ram.Split('/')[ram.Split('/').Length - 1];
			return ParseSizeString(ram);
		}

		private static double ParseStorageSize(string html)
		{
			string storage = Regex.Match(html.ToLower()
				.Replace("\t", " ")
				.Replace("<strong>storage:</strong>", string.Empty)
				.Replace("<strong>storage: </strong>", string.Empty)
				.Replace("<strong>hard drive:</strong>", string.Empty)
				.Replace("<strong>hard drive: </strong>", string.Empty),
				@"\d+((.|,)\d+)?[ ]?(mb|gb)").ToString().Trim();

			return ParseSizeString(storage);
		}

		/// <summary>Converts a "NNN MB" / "NNN.N GB" string to bytes as double.</summary>
		private static double ParseSizeString(string sizeToken)
		{
			if (sizeToken.IndexOf("mb", StringComparison.OrdinalIgnoreCase) > -1)
			{
				double.TryParse(
					sizeToken.ToLower().Replace("mb", string.Empty)
						.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator).Trim(),
					out double mb);
				return 1024.0 * 1024 * mb;
			}
			if (sizeToken.IndexOf("gb", StringComparison.OrdinalIgnoreCase) > -1)
			{
				double.TryParse(
					sizeToken.ToLower().Replace("gb", string.Empty)
						.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator).Trim(),
					out double gb);
				return 1024.0 * 1024 * 1024 * gb;
			}
			return 0;
		}

		/// <summary>
		/// Get game achievements schema with hidden description & percent & without stats
		/// </summary>
		/// <param name="appId"></param>
		/// <returns></returns>
		private ObservableCollection<GameAchievement> GetAchievementsSchema(uint appId)
        {
            ObservableCollection<GameAchievement> gameAchievements = null;
            if (appId > 0)
            {
                List<SteamAchievement> steamAchievements = SteamKit.GetGameAchievements(appId, CodeLang.GetSteamLang(Locale));
                gameAchievements = steamAchievements?.Select(x => new GameAchievement
                {
                    Id = x.InternalName,
                    Name = x.LocalizedName.Trim(),
                    Description = x.LocalizedDesc.Trim(),
                    UrlUnlocked = x.Icon,
                    UrlLocked = x.IconGray,
                    DateUnlocked = default,
                    Percent = float.Parse(x.PlayerPercentUnlocked?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) ?? "100"),
                    IsHidden = x.Hidden
                }).ToObservable();
            }

            return gameAchievements ?? new ObservableCollection<GameAchievement>();
        }

        public List<GenericItemOption> GetSearchGame(string searchTerm, bool noDlcs = false)
        {
            List<GenericItemOption> results = new List<GenericItemOption>();

            try
            {
                string url = string.Format(UrlSteamGameSearch, searchTerm.NormalizeGameName(), "en");
                string response = Web.DownloadStringData(url).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(response, out SteamSearch steamSearch, out Exception ex);
                if (ex != null)
                {
                    throw ex;
                }

                results = steamSearch?.Items
                    ?.Select(x => new GenericItemOption { Name = x.Name, Description = x.Id.ToString() + (noDlcs ? string.Empty : $" - {GetGameInfos(x.Id.ToString(), null, true)?.Dlcs?.Count() ?? 0} DLC") })
                    ?.ToList() ?? new List<GenericItemOption>();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, false, PluginName);
            }

            return results;
        }

        #endregion

        #region Steam Api with api key

        private ObservableCollection<AccountInfos> GetPlayerSummaries(List<ulong> steamIds)
        {
            ObservableCollection<AccountInfos> playerSummaries = null;
            if (steamIds?.Count > 0 && !CurrentAccountInfos.ApiKey.IsNullOrEmpty())
            {
                List<SteamPlayer> steamPlayerSummaries = SteamKit.GetPlayerSummaries(CurrentAccountInfos.ApiKey, steamIds);
                playerSummaries = steamPlayerSummaries?.Select(x => new AccountInfos
                {
                    UserId = x.SteamId.ToString(),
                    Avatar = x.AvatarFull,
                    Pseudo = x.PersonaName,
                    Link = x.ProfileUrl
                }).ToObservable();
            }
            return playerSummaries;
        }

        private ObservableCollection<AccountGameInfos> GetAccountGamesInfosByApi(AccountInfos accountInfos)
        {
            ObservableCollection<AccountGameInfos> accountGameInfos = null;
            if (!CurrentAccountInfos.ApiKey.IsNullOrEmpty() && ulong.TryParse(accountInfos.UserId, out ulong steamId))
            {
                accountGameInfos = new ObservableCollection<AccountGameInfos>();
                List<SteamGame> steamOwnedGame = SteamKit.GetOwnedGames(CurrentAccountInfos.ApiKey, steamId);
                steamOwnedGame?.ForEach(x =>
                {
                    ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
                    if (x.PlaytimeForever > 0)
                    {
                        // TODO "error": "Profile is not public"
                        gameAchievements = GetAchievements(x.AppId.ToString(), accountInfos);
                    }

                    AccountGameInfos gameInfos = new AccountGameInfos
                    {
                        Id = x.AppId.ToString(),
                        Name = x.Name,
                        Link = string.Format(UrlSteamGame, x.AppId),
                        IsCommun = !accountInfos.IsCurrent && CurrentGamesInfos.FirstOrDefault(y => y.Id == x.AppId.ToString()) != null,
                        Achievements = gameAchievements,
                        Playtime = x.PlaytimeForever
                    };

                    accountGameInfos.Add(gameInfos);
                });
            }
            return accountGameInfos;
        }

        private ObservableCollection<AccountInfos> GetCurrentFriendsInfosByApi()
        {
            ObservableCollection<AccountInfos> currentFriendsInfos = null;
            if (!CurrentAccountInfos.ApiKey.IsNullOrEmpty() && ulong.TryParse(CurrentAccountInfos.UserId, out ulong steamId))
            {
                List<SteamFriend> friendList = SteamKit.GetFriendList(CurrentAccountInfos.ApiKey, steamId);
                List<ulong> steamIds = friendList?.Select(x => x.SteamId)?.ToList() ?? new List<ulong>();
                currentFriendsInfos = GetPlayerSummaries(steamIds);

                friendList?.ForEach(x =>
                {
                    AccountInfos userInfos = currentFriendsInfos?.FirstOrDefault(y => y.UserId.IsEqual(x.SteamId.ToString()));
                    if (userInfos != null)
                    {
                        userInfos.DateAdded = x.FriendSince;
                    }
                });
            }
            return currentFriendsInfos;
        }

        private ObservableCollection<GameAchievement> GetAchievementsByApi(uint appId, AccountInfos accountInfos, ObservableCollection<GameAchievement> gameAchievements)
        {
            Logger.Info($"GetAchievementsByApi({appId})");
            if (appId > 0 && ulong.TryParse(accountInfos.UserId, out ulong steamId) && !CurrentAccountInfos.ApiKey.IsNullOrEmpty())
            {
                List<SteamPlayerAchievement> steamPlayerAchievements = SteamKit.GetPlayerAchievements(CurrentAccountInfos.ApiKey, appId, steamId, CodeLang.GetSteamLang(Locale));
                steamPlayerAchievements?.ForEach(x =>
                {
                    // Some achievements don't have a valid unlock time, use fallback date instead
                    DateTime unlockTime = x.UnlockTime.Year == 1 && x.Achieved == 1 ? new DateTime(year: 2007, month: 10, day: 10) : x.UnlockTime;
                    gameAchievements.FirstOrDefault(y => y.Id.IsEqual(x.ApiName)).DateUnlocked = unlockTime;
                });
            }
            return gameAchievements;
        }

        private ObservableCollection<AccountWishlist> GetWishlistByApi(AccountInfos accountInfos)
        {
            try
            {
                Logger.Info($"GetWishlistByApi()");
                ObservableCollection<AccountWishlist> accountWishlists = new ObservableCollection<AccountWishlist>();

                if (ulong.TryParse(accountInfos.UserId, out ulong steamId) && (!CurrentAccountInfos.ApiKey.IsNullOrEmpty() || !CurrentAccountInfos.IsPrivate))
                {
                    string json = Web.DownloadStringData(string.Format(UrlWishlistByApi, steamId, CurrentAccountInfos.ApiKey)).GetAwaiter().GetResult();
                    _ = Serialization.TryFromJson(json, out SteamWishlistResponse steamWishlistApi, out Exception ex);
                    if (ex != null)
                    {
                        throw ex;
                    }
                    return GetWishListFromSteamWishlist(steamWishlistApi.Response);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
			}
			return new ObservableCollection<AccountWishlist>();
		}

        #endregion

        #region Steam Web     

		private bool CheckGameIsPrivateByWeb(uint appId, AccountInfos accountInfos)
        {
            Logger.Info($"CheckGameIsPrivateByWeb({appId})");
            string urlById = string.Format(UrlProfileById, accountInfos.UserId) + $"/stats/{appId}/achievements";
            string urlByPersona = string.Format(UrlProfileByName, accountInfos.Pseudo) + $"/stats/{appId}";
            List<HttpCookie> cookies = GetStoredCookies();

            string resultWeb = Web.DownloadStringData(urlById, cookies, string.Empty, true).GetAwaiter().GetResult();
            if (resultWeb.IndexOf("profile_fatalerror") == 1)
            {
                return true;
            }
            else
            {
                resultWeb = Web.DownloadStringData(urlByPersona, cookies, string.Empty, true).GetAwaiter().GetResult();
                if (resultWeb.IndexOf("profile_fatalerror") == 1)
                {
                    return true;
                }
            }
            return false;
        }

        private ObservableCollection<GameAchievement> GetAchievementsByWeb(uint appId, AccountInfos accountInfos, ObservableCollection<GameAchievement> gameAchievements)
        {
            Logger.Info($"GetAchievementsByWeb({appId})");
            string lang = "english";
            bool needLocalized = false;
            DateTime[] unlockedDates = null;

            try
            {
                do
                {
                    string urlById = string.Format(UrlProfileById, accountInfos.UserId) + $"/stats/{appId}/achievements?l={lang}";
                    string urlByPersona = string.Format(UrlProfileByName, accountInfos.Pseudo) + $"/stats/{appId}?l={lang}";
                    needLocalized = false;

                    List<HttpCookie> cookies = GetStoredCookies();
                    string url = urlById;
                    string response = Web.DownloadStringData(url, cookies, string.Empty, true).GetAwaiter().GetResult();
                    if (response.IndexOf("achieveRow") == -1)
                    {
                        url = urlByPersona;
                        response = Web.DownloadStringData(urlByPersona, cookies, string.Empty, true).GetAwaiter().GetResult();
                    }

                    if (response.IndexOf("achieveRow") > -1)
                    {
                        IHtmlDocument htmlDocument = new HtmlParser().Parse(response);
                        int i = 0;
                        IHtmlCollection<IElement> elements = htmlDocument.QuerySelectorAll(".achieveRow");
                        foreach (IElement el in elements)
                        {
                            string urlUnlocked = el.QuerySelector(".achieveImgHolder img")?.GetAttribute("src") ?? string.Empty;
                            string name = el.QuerySelector(".achieveTxtHolder h3").InnerHtml.Trim();
                            string description = el.QuerySelector(".achieveTxtHolder h5").InnerHtml.Trim();

                            DateTime dateUnlocked = default;

                            if (lang.Equals("english"))
                            {
                                bool isUnlocked = (el.GetAttribute("data-panel") ?? string.Empty).Contains("autoFocus");
                                string stringDateUnlocked = el.QuerySelector(".achieveUnlockTime")?.InnerHtml ?? string.Empty;

                                if (!stringDateUnlocked.IsNullOrEmpty())
                                {
                                    stringDateUnlocked = stringDateUnlocked.Replace("Unlocked", string.Empty).Replace("<br>", string.Empty).Trim();
                                    _ = DateTime.TryParseExact(stringDateUnlocked, new[] { "d MMM, yyyy @ h:mmtt", "d MMM @ h:mmtt", "MMM d, yyyy @ h:mmtt", "MMM d @ h:mmtt" }, new CultureInfo("en-US"), DateTimeStyles.AssumeLocal, out dateUnlocked);
                                }
                                else if (isUnlocked)
                                {
                                    dateUnlocked = i > 0 ? unlockedDates[i - 1] : DateTime.Today;
                                    Logger.Warn($"No valid date found for unlocked achievement \"{name}\"");
                                }

                                if (unlockedDates == null)
                                {
                                    unlockedDates = new DateTime[elements.Length];
                                }
                                unlockedDates[i] = dateUnlocked;
                            }
                            else if (i < unlockedDates?.Length)
                            {
                                dateUnlocked = unlockedDates[i];
                            }

                            if (dateUnlocked != default)
                            {
                                List<GameAchievement> achievements = gameAchievements.Where(x => x.UrlUnlocked.Split('/').Last().IsEqual(urlUnlocked.Split('/').Last())).ToList();

                                if (achievements.Count == 1)
                                {
                                    achievements[0].DateUnlocked = dateUnlocked;
                                }
                                else
                                {
                                    GameAchievement achievement = achievements.Find(x => x.Name.IsEqual(name));
                                    if (achievement != null)
                                    {
                                        achievement.DateUnlocked = dateUnlocked;
                                    }
                                    else
                                    {
                                        if (!CodeLang.GetSteamLang(Locale).IsEqual(lang))
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
                            lang = CodeLang.GetSteamLang(Locale);
                        }
                    }
                    else if (response.IndexOf("The specified profile could not be found") > -1)
                    {

                    }
                } while (needLocalized);
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, true, PluginName);
                IsUserLoggedIn = false;
            }

            return gameAchievements;
        }

        private ObservableCollection<AccountInfos> GetCurrentFriendsInfosByWeb()
        {
            try
            {
                ObservableCollection<AccountInfos> currentFriendsInfos = new ObservableCollection<AccountInfos>();
                List<HttpCookie> cookies = GetStoredCookies();
                string url = string.Format(UrlFriends, CurrentAccountInfos.UserId);
                string response = Web.DownloadStringData(url, cookies).GetAwaiter().GetResult();

                IHtmlDocument htmlDocument = new HtmlParser().Parse(response);
                IHtmlCollection<IElement> elements = htmlDocument.QuerySelectorAll(".friend_block_v2");
                foreach (IElement el in elements)
                {
                    string steamId = el.GetAttribute("data-steamid");
                    string urlProfil = el.QuerySelector("a.selectable_overlay").GetAttribute("href");
                    string avatar = el.QuerySelector("img").GetAttribute("src").Replace("_medium", "_full");
                    string pseudo = el.QuerySelector("div.friend_block_content").InnerHtml.Split(new string[] { "<br>" }, StringSplitOptions.None)[0];

                    currentFriendsInfos.Add(new AccountInfos
                    {
                        UserId = steamId,
                        Avatar = avatar,
                        Pseudo = pseudo,
                        Link = urlProfil
                    });
                }
                return currentFriendsInfos;
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, true, PluginName);
                IsUserLoggedIn = false;
                return null;
            }
        }

        public ObservableCollection<AchievementProgression> GetProgressionByWeb(uint appId, AccountInfos accountInfos)
        {
            Logger.Info($"GetProgressionByWeb()");
            ObservableCollection<AchievementProgression> achievementsProgression = new ObservableCollection<AchievementProgression>();

            try
            {
                // Schema achievements
                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
                Tuple<string, ObservableCollection<GameAchievement>> data = GetAchievementsSchema(appId.ToString());

                if (data?.Item2?.Count() == 0)
                {
                    return achievementsProgression;
                }

                gameAchievements = data.Item2;

                // Progression achievements
                string url = string.Format(UrlProfilById, accountInfos.UserId, appId, "english");
                string response = Web.DownloadStringData(url, GetStoredCookies(), Web.UserAgent, true).GetAwaiter().GetResult();

                IHtmlDocument htmlDocument = new HtmlParser().Parse(response);
                IHtmlCollection<IElement> elements = htmlDocument.QuerySelectorAll("#personalAchieve div.achieveRow");
                foreach (IElement el in elements)
                {
                    string name = el.QuerySelector("div.achieveTxt h3").InnerHtml.Trim();
                    string progress = el.QuerySelector("div.achievementProgressBar .progressText")?.InnerHtml.Trim();
                    string id = gameAchievements.FirstOrDefault(x => x.Name.IsEqual(name))?.Id;

                    if (!id.IsNullOrEmpty())
                    {
                        if (!progress.IsNullOrEmpty())
                        {
                            achievementsProgression.Add(new AchievementProgression
                            {
                                Id = id,
                                Value = double.Parse(progress.Split('/')[0].Trim(), CultureInfo.InvariantCulture),
                                Max = double.Parse(progress.Split('/')[1].Trim(), CultureInfo.InvariantCulture)
                            });
                        }
                    }
                    else
                    {
                        Logger.Warn($"Achievement \"{name}\" not found in game schema for {appId}");
                    }
                }
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, true, PluginName);
                IsUserLoggedIn = false;
            }

            return achievementsProgression;
        }

		#endregion

		#region Steam Web with store token

		private ObservableCollection<AccountGameInfos> GetAccountGamesInfosByWebToken(AccountInfos accountInfos)
		{
			try
			{
				ObservableCollection<AccountGameInfos> accountGameInfos = new ObservableCollection<AccountGameInfos>();

				if (StoreToken?.Token.IsNullOrEmpty() ?? true)
				{
					Logger.Warn("StoreToken is not available for GetAccountGamesInfosByWeb");
					return accountGameInfos;
				}

				var dic = new Dictionary<string, string>
				{
					{ "access_token", StoreToken.Token },
					{ "steamId", accountInfos.UserId },
					{ "include_appinfo", "1" },
					{ "include_played_free_games", "1" },
					{ "include_extended_appinfo", "1" }
				};

				var steamOwnedGames = SteamApiServiceBase.Get<SteamOwnedGames>(UrlGetOwnedGamesApi, dic);
				steamOwnedGames?.Games?.ForEach(x =>
				{
					ObservableCollection<GameAchievement> gameAchievements = GetAchievements(x.AppId.ToString(), accountInfos);

					accountGameInfos.Add(new AccountGameInfos
					{
						Id = x.AppId.ToString(),
						Name = x.Name,
						Link = string.Format(UrlSteamGame, x.AppId),
						IsCommun = !accountInfos.IsCurrent && CurrentGamesInfos.FirstOrDefault(y => y.Id == x.AppId.ToString()) != null,
						Achievements = gameAchievements,
						Playtime = x.PlaytimeForever
					});
				});

				return accountGameInfos;
			}
			catch (WebException ex)
			{
				Common.LogError(ex, false, true, PluginName);
				IsUserLoggedIn = false;
				return null;
			}
		}

		private List<SteamApp> GetSteamAppsByWebToken()
		{
			try
			{
				List<SteamApp> steamApps = new List<SteamApp>();
				uint lastAppid = 0;

				if (StoreToken?.Token.IsNullOrEmpty() ?? true)
				{
					Logger.Warn("StoreToken is not available for GetSteamAppsByWeb");
					return steamApps;
				}

				do
				{
					var dic = new Dictionary<string, string>
					{
						{ "access_token", StoreToken.Token },
						{ "include_dlc", "true" },
						{ "max_results", "50000" }
					};
					if (lastAppid > 0)
					{
						dic.Add("last_appid", lastAppid.ToString());
					}

					var getApps = SteamApiServiceBase.Get<SteamAppList>(UrlGetAppListApi, dic);

					if (getApps?.Apps != null)
					{
						steamApps.AddRange(getApps.Apps.Select(x => new SteamApp
						{
							AppId = x.AppId,
							Name = x.Name
						}));
						lastAppid = getApps.HaveMoreResults ? getApps.LastAppId : 0;
					}
					else
					{
						lastAppid = 0;
					}
				} while (lastAppid > 0);

				return steamApps;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return null;
			}
		}

		private ObservableCollection<AccountWishlist> GetWishlistByWebToken(AccountInfos accountInfos)
		{
			try
			{
				Logger.Info($"GetWishlistByWebToken()");

				if (StoreToken?.Token.IsNullOrEmpty() ?? true)
				{
					Logger.Warn("StoreToken is not available for GetWishlistByWebToken");
					return new ObservableCollection<AccountWishlist>();
				}

				var dic = new Dictionary<string, string>
				{
					{ "access_token", StoreToken.Token },
					{ "steamid", accountInfos.UserId }
				};

				var getWishlist = SteamApiServiceBase.Get<SteamWishlist>(UrlWishlistApi, dic);
                return GetWishListFromSteamWishlist(getWishlist);
			}
			catch (WebException ex)
			{
				Common.LogError(ex, false, true, PluginName);
				IsUserLoggedIn = false;
				return new ObservableCollection<AccountWishlist>();
			}
		}

		#endregion

        private ObservableCollection<AccountWishlist> GetWishListFromSteamWishlist(SteamWishlist steamWishlist)
        {
            var accountWishlists = new ObservableCollection<AccountWishlist>();
			steamWishlist.Items.ForEach(x =>
			{
				var gameData = GetAppDetails(x.AppId, 1);
				accountWishlists.Add(new AccountWishlist
				{
					Id = x.AppId.ToString(),
					Name = gameData?.data?.name ?? GetGameName(x.AppId, false),
					Link = string.Format(UrlSteamGame, x.AppId),
					Released = DateHelper.ParseReleaseDate(gameData?.data?.release_date?.date)?.Date,
					Added = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(x.DateAdded),
					Image = gameData?.data?.header_image ?? string.Empty
				});
			});
            return accountWishlists;
		}

		private List<uint> GetDlcFromSteamDb(uint appId)
        {
            List<uint> dlcs = new List<uint>();
            if (appId == 0)
            {
                return dlcs;
            }

            try
            {
                var pageSource = Web.DownloadSourceDataWebView(string.Format(SteamDbDlc, appId)).GetAwaiter().GetResult();
                string data = pageSource.Item1;

				IHtmlDocument htmlDocument = new HtmlParser().Parse(data);
				IHtmlCollection<IElement> sectionDlcs = htmlDocument.QuerySelectorAll("#dlc tr.app");
				if (sectionDlcs != null)
				{
					foreach (IElement el in sectionDlcs)
					{
						string dlcIdString = el.QuerySelector("td a")?.InnerHtml;
						if (uint.TryParse(dlcIdString, out uint DlcId))
						{
							dlcs.Add(DlcId);
						}
					}
				}
			}
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return dlcs;
        }

        private ObservableCollection<GameAchievement> SetExtensionsAchievementsFromSteamDb(uint appId, ObservableCollection<GameAchievement> gameAchievements)
        {
            try
            {
                Thread.Sleep(1000);
                string data = Web.DownloadStringData(string.Format(SteamDbExtensionAchievements, appId)).GetAwaiter().GetResult();
                if (Serialization.TryFromJson(data, out ExtensionsAchievements extensionsAchievementse))
                {
                    if (!extensionsAchievementse?.Success ?? true)
                    {
                        Logger.Warn($"No success in ExtensionsAchievements for {appId}");
                    }

                    int categoryOrder = 1;
                    extensionsAchievementse?.Data?.ForEach(x =>
                    {
                        gameAchievements?.ForEach(y =>
                        {
                            if (x.AchievementApiNames.Contains(y.Id))
                            {
                                int id = (x.DlcAppId != null && x.DlcAppId != 0) ? (int)x.DlcAppId : (int)appId;
                                y.CategoryIcon = string.Format(UrlCapsuleSteam, id);
                                y.Category = x.Name.IsNullOrEmpty() ? x.DlcAppName : x.Name;
                                y.CategoryOrder = categoryOrder;
                                y.CategoryDlc = !x.DlcAppName.IsNullOrEmpty();
                            }
                        });
                        categoryOrder++;
                    });
                }
                else
                {
                    Logger.Warn($"No data find in ExtensionsAchievements for {appId}");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, false, PluginName);
            }

            gameAchievements?.ForEach(y =>
            {
                if (y.CategoryOrder == 0)
                {
                    y.CategoryIcon = string.Format(UrlCapsuleSteam, appId);
                }
            });

            return gameAchievements;
        }
    }
}