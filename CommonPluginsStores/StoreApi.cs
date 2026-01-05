using Ardalis.GuardClauses;
using CommonPlayniteShared;
using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using CommonPluginsStores.Models.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores
{
    /// <summary>
    /// Abstract base class for store API implementations.
    /// Provides common functionality for managing user accounts, games, achievements, and DLC data.
    /// </summary>
    public abstract class StoreApi : ObservableObject, IStoreApi, IStoreApiInternal
    {
        protected static ILogger Logger => LogManager.GetLogger();

        private readonly SmartCache<AccountInfos> _accountCache;
        private readonly SmartCache<ObservableCollection<AccountInfos>> _friendsCache;
        private readonly SmartCache<ObservableCollection<AccountGameInfos>> _gamesCache;
        private readonly SmartCache<ObservableCollection<GameDlcOwned>> _dlcsCache;

        private Lazy<AccountInfos> _lazyAccount;
        private readonly Lazy<ObservableCollection<AccountInfos>> _lazyFriends;
        private readonly Lazy<ObservableCollection<AccountGameInfos>> _lazyGames;

        #region Account data

        /// <summary>
        /// Get the current account information for the user.
        /// Caches the result for reuse.
        /// </summary>
        public AccountInfos CurrentAccountInfos
        {
            get => _lazyAccount.Value;
            set => SetValue(ref _lazyAccount, new Lazy<AccountInfos>(() => value), nameof(CurrentAccountInfos));
        }

        /// <summary>
        /// Gets or sets the current friends list.
        /// Data is refreshed every 30 minutes.
        /// </summary>
        /// 
        public ObservableCollection<AccountInfos> CurrentFriendsInfos => _lazyFriends.Value;

        /// <summary>
        /// Gets or sets the current games list.
        /// Data is refreshed every 30 minutes.
        /// </summary>
        public ObservableCollection<AccountGameInfos> CurrentGamesInfos => _lazyGames.Value;

        /// <summary>
        /// Gets the list of currently owned DLCs for all games.
        /// </summary>
        public ObservableCollection<GameDlcOwned> CurrentGamesDlcsOwned
        {
            get
            {
                return _dlcsCache.GetOrSet(
                    "current_account",
                    () => LoadGamesDlcsOwned() ?? GetGamesDlcsOwned() ?? LoadGamesDlcsOwned(false),
                    TimeSpan.FromMinutes(5)
                );
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Store configuration settings.
        /// </summary>
        public StoreSettings StoreSettings { get; set; } = new StoreSettings();

        private bool? isUserLoggedIn;

        /// <summary>
        /// Gets or sets whether the user is logged in.
        /// </summary>
        public bool IsUserLoggedIn
        {
            get
            {
                isUserLoggedIn = isUserLoggedIn ?? GetIsUserLoggedIn();
                return (bool)isUserLoggedIn;
            }

            set => SetValue(ref isUserLoggedIn, value);
        }

        /// <summary>
        /// Gets the plugin name identifier.
        /// </summary>
        protected string PluginName { get; }

        /// <summary>
        /// Gets the client display name.
        /// </summary>
        protected string ClientName { get; }

        /// <summary>
        /// Gets the client name formatted for logging (no whitespace).
        /// </summary>
        protected string ClientNameLog { get; }

        /// <summary>
        /// Gets or sets the locale for data retrieval (default: en_US).
        /// </summary>
        protected string Locale { get; set; } = "en_US";

        /// <summary>
        /// Gets the path for storing persistent store data.
        /// </summary>
        protected string PathStoresData { get; }

        /// <summary>
        /// Gets the path for caching app data.
        /// </summary>
        protected string PathAppsData { get; }

        /// <summary>
        /// Gets the path for caching achievements data.
        /// </summary>
        protected string PathAchievementsData { get; }

        /// <summary>
        /// Gets the file path for storing encrypted user data.
        /// </summary>
        protected string FileUser { get; }

        /// <summary>
        /// Gets the file path for storing encrypted cookies.
        /// </summary>
        protected string FileCookies { get; }

        /// <summary>
        /// Gets the file path for storing games DLC ownership data.
        /// </summary>
        protected string FileGamesDlcsOwned { get; }

        /// <summary>
        /// Gets or sets the list of cookie domains for authentication.
        /// </summary>
        protected List<string> CookiesDomains { get; set; }

        /// <summary>
        /// Gets the cookies management tool instance.
        /// </summary>
        protected CookiesTools CookiesTools { get; }

        protected FileDataTools FileDataTools { get; }

        protected string FileToken { get; }

        private StoreToken _storeToken;

		/// <summary>
		/// Gets or sets the authentication token for API requests.
		/// </summary>
		protected StoreToken StoreToken 
        {
            get
            {
                if (_storeToken == null)
                {
                    _storeToken = GetStoredToken();
				}
                return _storeToken;
            }
            set
            {
                _storeToken = value;
                _ = SetStoredToken(_storeToken);
			}
        }

        /// <summary>
        /// Gets the external plugin instance reference.
        /// </summary>
        protected ExternalPlugin PluginLibrary { get; }

        #endregion

        /// <summary>
        /// Constructor for the Store API.
        /// Initializes path structure, cookies tools, and plugin metadata.
        /// </summary>
        /// <param name="pluginName">The name of the plugin</param>
        /// <param name="pluginLibrary">The external plugin instance</param>
        /// <param name="clientName">The display name of the client</param>
        public StoreApi(string pluginName, ExternalPlugin pluginLibrary, string clientName)
        {
            _ = Guard.Against.NullOrWhiteSpace(pluginName, nameof(pluginName));
            _ = Guard.Against.Null(pluginLibrary, nameof(pluginLibrary));
            _ = Guard.Against.EnumOutOfRange(pluginLibrary, nameof(pluginLibrary));
            _ = Guard.Against.NullOrWhiteSpace(clientName, nameof(clientName));

            PluginName = pluginName;
            PluginLibrary = pluginLibrary;
            ClientName = clientName;
            ClientNameLog = clientName.RemoveWhiteSpace();

            _accountCache = new SmartCache<AccountInfos>();
            _friendsCache = new SmartCache<ObservableCollection<AccountInfos>>();
            _gamesCache = new SmartCache<ObservableCollection<AccountGameInfos>>();
            _dlcsCache = new SmartCache<ObservableCollection<GameDlcOwned>>();

            _lazyAccount = new Lazy<AccountInfos>(() => _accountCache.GetOrSet("current_account", GetCurrentAccountInfos, TimeSpan.FromHours(24)));
            _lazyFriends = new Lazy<ObservableCollection<AccountInfos>>(() => _friendsCache.GetOrSet("current_friends", GetCurrentFriendsInfos, TimeSpan.FromMinutes(30)));
            _lazyGames = new Lazy<ObservableCollection<AccountGameInfos>>(() => _gamesCache.GetOrSet("current_games", () => GetAccountGamesInfos(CurrentAccountInfos), TimeSpan.FromMinutes(30)));

            string pathCacheData = Path.Combine(PlaynitePaths.DataCachePath, "StoresData");
            PathAppsData = Path.Combine(pathCacheData, ClientNameLog, "Apps");
            PathAchievementsData = Path.Combine(pathCacheData, ClientNameLog, "Achievements");

            PathStoresData = Path.Combine(PlaynitePaths.ExtensionsDataPath, "StoresData");
            FileUser = Path.Combine(PathStoresData, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientNameLog}_User.dat"));
            FileCookies = Path.Combine(PathStoresData, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientNameLog}_Cookies.dat"));
            FileToken = Path.Combine(PathStoresData, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientNameLog}_Token.dat"));
            FileGamesDlcsOwned = Path.Combine(PathStoresData, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientNameLog}_GamesDlcsOwned.json"));

            CookiesTools = new CookiesTools(
                PluginName,
                ClientName,
                FileCookies,
                CookiesDomains);

            FileDataTools = new FileDataTools(PluginName, ClientName)
            {
                ShowNotificationOldDataHandler = ShowNotificationOldData
            };

            FileSystem.CreateDirectory(PathStoresData);
        }

        #region Cookies

        /// <summary>
        /// Read the last identified cookies stored.
        /// </summary>
        /// <returns>List of stored HTTP cookies</returns>
        protected virtual List<Playnite.SDK.HttpCookie> GetStoredCookies() => CookiesTools.GetStoredCookies();

        /// <summary>
        /// Save the last identified cookies stored.
        /// </summary>
        /// <param name="httpCookies">The HTTP cookies to store</param>
        /// <returns>True if cookies were saved successfully</returns>
        protected virtual bool SetStoredCookies(List<Playnite.SDK.HttpCookie> httpCookies) => CookiesTools.SetStoredCookies(httpCookies);

        /// <summary>
        /// Get cookies in WebView or another method.
        /// </summary>
        /// <param name="deleteCookies">Whether to delete cookies after retrieval</param>
        /// <param name="webView">Optional WebView instance to use</param>
        /// <returns>List of HTTP cookies from web source</returns>
        protected virtual List<Playnite.SDK.HttpCookie> GetWebCookies(bool deleteCookies = false, IWebView webView = null) => CookiesTools.GetWebCookies(deleteCookies, webView);

        /// <summary>
        /// Get new cookies from specific URLs.
        /// </summary>
        /// <param name="urls">List of URLs to retrieve cookies from</param>
        /// <param name="deleteCookies">Whether to delete cookies after retrieval</param>
        /// <param name="webView">Optional WebView instance to use</param>
        /// <returns>List of new HTTP cookies</returns>
        protected virtual List<Playnite.SDK.HttpCookie> GetNewWebCookies(List<string> urls, bool deleteCookies = false, IWebView webView = null) => CookiesTools.GetNewWebCookies(urls, deleteCookies, webView);

		#endregion

		#region Token

        protected virtual StoreToken GetStoredToken() 
        {
			if (File.Exists(FileToken))
			{
				try
				{
					return Serialization.FromJson<StoreToken>(
						Encryption.DecryptFromFile(
							FileToken,
							Encoding.UTF8,
							WindowsIdentity.GetCurrent().User.Value));
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, $"Failed to load saved token for {ClientName}");
				}
			}

			Logger.Warn($"No stored token for {ClientName}");
			return null;
		}

        protected virtual bool SetStoredToken(StoreToken token)
		{
			try
			{
				if (token != null)
				{
					FileSystem.CreateDirectory(Path.GetDirectoryName(FileToken));
					Encryption.EncryptToFile(
						FileToken,
						Serialization.ToJson(token),
						Encoding.UTF8,
						WindowsIdentity.GetCurrent().User.Value);
					return true;
				}
				else
				{
					Logger.Warn($"No token saved for {PluginName}");
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "Failed to save token");
			}

			return false;
		}

		#endregion

		#region Configuration

		/// <summary>
		/// Resets the cached login status, forcing a re-check on next access.
		/// </summary>
		public void ResetIsUserLoggedIn()
        {
            isUserLoggedIn = null;
        }

        /// <summary>
        /// Abstract method to determine if the user is currently logged in.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <returns>True if user is logged in, false otherwise</returns>
        protected abstract bool GetIsUserLoggedIn();

        /// <summary>
        /// Set data language.
        /// </summary>
        /// <param name="locale">ISO 15897 locale identifier</param>
        public void SetLanguage(string locale)
        {
            Locale = locale;
        }

        /// <summary>
        /// Sets whether to force authentication on next operation.
        /// </summary>
        /// <param name="forceAuth">True to force authentication</param>
        public void SetForceAuth(bool forceAuth)
        {
            StoreSettings.ForceAuth = forceAuth;
        }

        /// <summary>
        /// Initiates the login process for the store.
        /// Must be overridden by derived classes.
        /// </summary>
        public virtual void Login()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initiates an alternative login process for the store.
        /// Must be overridden by derived classes if alternative login is supported.
        /// </summary>
        public virtual void LoginAlternative()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Explicit interface implementation for loading current user.
        /// </summary>
        /// <returns>Current user account information</returns>
        AccountInfos IStoreApiInternal.LoadCurrentUser() => LoadCurrentUser();

        /// <summary>
        /// Loads the current user data from encrypted file storage.
        /// </summary>
        /// <returns>AccountInfos object or null if loading fails</returns>
        protected AccountInfos LoadCurrentUser()
        {
            if (FileSystem.FileExists(FileUser))
            {
                try
                {
                    string user = Encryption.DecryptFromFile(
                        FileUser,
                        Encoding.UTF8,
                        WindowsIdentity.GetCurrent().User.Value);

                    _ = Serialization.TryFromJson(user, out AccountInfos accountInfos);
                    return accountInfos;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName, $"Failed to load {ClientName} user.");
                }
            }

            return null;
        }

        /// <summary>
        /// Explicit interface implementation for saving current user.
        /// </summary>
        void IStoreApiInternal.SaveCurrentUser() => SaveCurrentUser();

        /// <summary>
        /// Saves the current user data to encrypted file storage.
        /// </summary>
        protected void SaveCurrentUser()
        {
            if (CurrentAccountInfos != null)
            {
                FileSystem.PrepareSaveFile(FileUser);
                Encryption.EncryptToFile(
                    FileUser,
                    Serialization.ToJson(CurrentAccountInfos),
                    Encoding.UTF8,
                    WindowsIdentity.GetCurrent().User.Value);
            }
        }

        /// <summary>
        /// Initializes the store API with settings and optionally loads user data.
        /// </summary>
        /// <param name="storeSettings">Store configuration settings</param>
        /// <param name="loadUser">Whether to load user data during initialization</param>
        public virtual void Initialization(StoreSettings storeSettings, bool loadUser)
        {
            SetLanguage(API.Instance.ApplicationSettings.Language);
            StoreSettings = storeSettings;
            if (loadUser)
            {
                _ = CurrentAccountInfos;
            }
        }

        /// <summary>
        /// Saves store settings and optionally saves user data.
        /// </summary>
        /// <param name="storeSettings">Store configuration settings to save</param>
        /// <param name="saveUser">Whether to save current user data</param>
        public virtual void SaveSettings(StoreSettings storeSettings, bool saveUser)
        {
            StoreSettings = storeSettings;
            if (saveUser)
            {
                SaveCurrentUser();
                ClearCache();
                _ = CurrentAccountInfos;
            }
        }

        #endregion

        #region Current user

        /// <summary>
        /// Get current user info.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <returns>Current account information</returns>
        protected virtual AccountInfos GetCurrentAccountInfos()
        {
            AccountInfos accountInfos = LoadCurrentUser();
            if (!accountInfos?.UserId?.IsNullOrEmpty() ?? false)
            {
                return accountInfos;
            }
            return new AccountInfos { IsCurrent = true };
        }

		/// <summary>
		/// Asynchronously retrieves the current user's account information.
		/// </summary>
		/// <returns>Task that returns the current account information</returns>
		public async Task<AccountInfos> GetCurrentAccountInfosAsync()
        {
            return await Task.Run(() => GetCurrentAccountInfos());
        }

        /// <summary>
        /// Get current user's friends info.
        /// Override in derived classes if friends functionality is supported.
        /// </summary>
        /// <returns>Collection of friends' account information or null</returns>
        protected virtual ObservableCollection<AccountInfos> GetCurrentFriendsInfos() => null;

        /// <summary>
        /// Get all game's owned for current user.
        /// Override in derived classes if game ownership tracking is supported.
        /// </summary>
        /// <returns>Collection of owned games or null</returns>
        protected virtual ObservableCollection<GameDlcOwned> GetGamesOwned() => null;

        #endregion

        #region User details

        /// <summary>
        /// Get the user's games list.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <param name="accountInfos">Account information for the user</param>
        /// <returns>Collection of user's game information</returns>
        public abstract ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos);

		/// <summary>
		/// Asynchronously retrieves the user's games list.
		/// </summary>
		/// <param name="accountInfos">Account information for the user</param>
		/// <returns>Task that returns a collection of user's game information</returns>
		public async Task<ObservableCollection<AccountGameInfos>> GetAccountGamesInfosAsync(AccountInfos accountInfos)
        {
            Guard.Against.Null(accountInfos, nameof(accountInfos));
            return await Task.Run(() => GetAccountGamesInfos(accountInfos));
        }

        /// <summary>
        /// Get a list of a game's achievements with a user's possessions.
        /// Override in derived classes if achievements are supported.
        /// </summary>
        /// <param name="id">Game identifier</param>
        /// <param name="accountInfos">Account information</param>
        /// <returns>Collection of game achievements or null</returns>
        public virtual ObservableCollection<GameAchievement> GetAchievements(string id, AccountInfos accountInfos) => null;

		/// <summary>
		/// Asynchronously retrieves a list of a game's achievements with a user's possessions.
		/// </summary>
		/// <param name="id">Game identifier</param>
		/// <param name="accountInfos">Account information</param>
		/// <returns>Task that returns a collection of game achievements or null</returns>
		public async Task<ObservableCollection<GameAchievement>> GetAchievementsAsync(string id, AccountInfos accountInfos)
        {
            Guard.Against.NullOrWhiteSpace(id, nameof(id));
            Guard.Against.Null(accountInfos, nameof(accountInfos));
            return await Task.Run(() => GetAchievements(id, accountInfos));
        }

        /// <summary>
        /// Get achievements SourceLink.
        /// Override in derived classes if achievement source links are supported.
        /// </summary>
        /// <param name="name">Game name</param>
        /// <param name="id">Game identifier</param>
        /// <param name="accountInfos">Account information</param>
        /// <returns>Source link for achievements or null</returns>
        public virtual SourceLink GetAchievementsSourceLink(string name, string id, AccountInfos accountInfos) => new SourceLink
        {
            GameName = name,
            Name = ClientName,
            Url = string.Empty
        };

        /// <summary>
        /// Gets the user's wishlist.
        /// Override in derived classes if wishlist functionality is supported.
        /// </summary>
        /// <param name="accountInfos">Account information</param>
        /// <returns>Collection of wishlist items or null</returns>
        public virtual ObservableCollection<AccountWishlist> GetWishlist(AccountInfos accountInfos) => null;

        /// <summary>
        /// Removes an item from the user's wishlist.
        /// Override in derived classes if wishlist modification is supported.
        /// </summary>
        /// <param name="id">Item identifier to remove</param>
        /// <returns>True if removal was successful, false otherwise</returns>
        public virtual bool RemoveWishlist(string id) => false;

        #endregion

        #region Game

        /// <summary>
        /// Get game informations.
        /// Override in derived classes if detailed game information is supported.
        /// </summary>
        /// <param name="id">Game identifier</param>
        /// <param name="accountInfos">Account information</param>
        /// <returns>Game information object or null</returns>
        public virtual GameInfos GetGameInfos(string id, AccountInfos accountInfos) => null;

        /// <summary>
        /// Gets the achievements schema for a game.
        /// Override in derived classes if achievement schemas are supported.
        /// </summary>
        /// <param name="id">Game identifier</param>
        /// <returns>Tuple containing schema identifier and achievements collection, or null</returns>
        public virtual Tuple<string, ObservableCollection<GameAchievement>> GetAchievementsSchema(string id) => null;

        /// <summary>
        /// Get dlc informations for a game.
        /// Override in derived classes if DLC information is supported.
        /// </summary>
        /// <param name="id">Game identifier</param>
        /// <param name="accountInfos">Account information</param>
        /// <returns>Collection of DLC information or null</returns>
        public virtual ObservableCollection<DlcInfos> GetDlcInfos(string id, AccountInfos accountInfos) => null;

		/// <summary>
		/// Asynchronously retrieves DLC information for a game.
		/// </summary>
		/// <param name="id">Game identifier</param>
		/// <param name="accountInfos">Account information</param>
		/// <returns>Task that returns a collection of DLC information or null</returns>
		public async Task<ObservableCollection<DlcInfos>> GetDlcInfosAsync(string id, AccountInfos accountInfos)
        {
            Guard.Against.NullOrWhiteSpace(id, nameof(id));
            Guard.Against.Null(accountInfos, nameof(accountInfos));
            return await Task.Run(() => GetDlcInfos(id, accountInfos));
        }

        #endregion

        #region Games owned

        /// <summary>
        /// Loads games and DLC ownership data from file storage.
        /// </summary>
        /// <param name="onlyNow">If true, only loads data newer than 5 minutes; if false, loads any existing data</param>
        /// <returns>Collection of owned games and DLC or null</returns>
        private ObservableCollection<GameDlcOwned> LoadGamesDlcsOwned(bool onlyNow = true)
        {
            return FileDataTools.LoadData<ObservableCollection<GameDlcOwned>>(FileGamesDlcsOwned, onlyNow ? 5 : 0);
        }

        /// <summary>
        /// Saves games and DLC ownership data to file storage.
        /// </summary>
        /// <param name="gamesDlcsOwned">Collection of owned games and DLC to save</param>
        /// <returns>True if save was successful, false otherwise</returns>
        private bool SaveGamesDlcsOwned(ObservableCollection<GameDlcOwned> gamesDlcsOwned)
        {
            return FileDataTools.SaveData(FileGamesDlcsOwned, gamesDlcsOwned);
        }

        /// <summary>
        /// Gets the current list of owned games and DLC from the store.
        /// Override in derived classes if ownership tracking is supported.
        /// </summary>
        /// <returns>Collection of owned games and DLC or null</returns>
        protected virtual ObservableCollection<GameDlcOwned> GetGamesDlcsOwned() => null;

		/// <summary>
		/// Asynchronously retrieves the current list of owned games and DLC from the store.
		/// </summary>
		/// <returns>Task that returns a collection of owned games and DLC or null</returns>
		public async Task<ObservableCollection<GameDlcOwned>> GetGamesDlcsOwnedAsync()
        {
            return await Task.Run(() => GetGamesDlcsOwned());
        }

        /// <summary>
        /// Checks if a specific DLC is owned by the current user.
        /// </summary>
        /// <param name="id">DLC identifier to check</param>
        /// <returns>True if DLC is owned, false otherwise</returns>
        protected virtual bool IsDlcOwned(string id)
        {
            try
            {
                bool IsOwned = CurrentGamesDlcsOwned?.Count(x => x.Id.IsEqual(id)) > 0;
                return IsOwned;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return false;
            }
        }

        #endregion

        #region Notifications

        /// <summary>
        /// Shows a notification to the user about using old cached data.
        /// </summary>
        /// <param name="dateLastWrite">The date when the data was last updated</param>
        protected void ShowNotificationOldData(DateTime dateLastWrite)
        {
            LocalDateTimeConverter localDateTimeConverter = new LocalDateTimeConverter();
            string formatedDateLastWrite = localDateTimeConverter.Convert(dateLastWrite, null, null, CultureInfo.CurrentCulture).ToString();
            Logger.Warn($"Use saved UserData - {formatedDateLastWrite}");
            API.Instance.Notifications.Add(new NotificationMessage(
                $"{PluginName}-{ClientNameLog}-LoadFileData",
                $"{PluginName}" + Environment.NewLine
                    + string.Format(ResourceProvider.GetString("LOCCommonNotificationOldData"), ClientName, formatedDateLastWrite),
                NotificationType.Info,
                () =>
                {
                    ResetIsUserLoggedIn();
                    ShowPluginSettings(PluginLibrary);
                }
            ));
        }

		/// <summary>
		/// Shows a notification to the user indicating no authentication has been performed.
		/// </summary>
		public virtual void ShowNotificationUserNoAuthenticate()
        {
            string message = string.Format(ResourceProvider.GetString("LOCCommonStoresNoAuthenticate"), ClientName);
            Logger.Warn($"{ClientName}: User is not authenticated");

            API.Instance.Notifications.Add(new NotificationMessage(
                $"{PluginName}-{ClientName.RemoveWhiteSpace()}-noauthenticate",
                $"{PluginName}\r\n{message}",
                NotificationType.Error,
                () =>
                {
                    try
                    {
                        ShowPluginSettings(PluginLibrary);
                        // TODO
                        /*
                        foreach (GenericAchievements achievementProvider in SuccessStoryDatabase.AchievementProviders.Values)
                        {
                            achievementProvider.ResetCachedConfigurationValidationResult();
                            achievementProvider.ResetCachedIsConnectedResult();
                        }
                        */
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }
                }
            ));
        }

        #endregion

        /// <summary>
        /// Calculates gamer score based on achievement rarity percentage.
        /// </summary>
        /// <param name="value">Rarity percentage value</param>
        /// <returns>Calculated gamer score (15, 30, 90, or 180)</returns>
        public static float CalcGamerScore(float value)
        {
            float gamerScore = 15;
            if (value < 2)
            {
                gamerScore = 180;
            }
            else if (value < 10)
            {
                gamerScore = 90;
            }
            else if (value < 30)
            {
                gamerScore = 30;
            }
            return gamerScore;
        }

        /// <summary>
        /// Calculates gamer score based on achievement rarity string.
        /// </summary>
        /// <param name="value">Rarity string ("epic", "rare", "uncommon", or other)</param>
        /// <returns>Calculated gamer score (15, 30, 90, or 180)</returns>
        public static float CalcGamerScore(string value)
        {
            float gamerScore = 15;
            if (value.IsEqual("epic"))
            {
                gamerScore = 180;
            }
            else if (value.IsEqual("rare"))
            {
                gamerScore = 90;
            }
            else if (value.IsEqual("uncommon"))
            {
                gamerScore = 30;
            }
            return gamerScore;
        }

        /// <summary>
        /// Purges all cached data including apps and achievements.
        /// </summary>
        public void PurgeCache()
        {
            FileSystem.DeleteDirectory(PathAppsData);
            FileSystem.DeleteDirectory(PathAchievementsData);
        }

		/// <summary>
		/// Clears all in-memory cache collections.
		/// </summary>
		public void ClearCache()
        {
            _accountCache.Clear();
            _friendsCache.Clear();
            _gamesCache.Clear();
            _dlcsCache.Clear();
        }
    }
}