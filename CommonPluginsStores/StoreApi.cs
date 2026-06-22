using Ardalis.GuardClauses;
using CommonPlayniteShared;
using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Caching;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.IO;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using CommonPluginsStores.Models.Enumerations;
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
using System.Threading;
using System.Threading.Tasks;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores
{
    /// <summary>
    /// Abstract base class for store API implementations.
    /// Provides common functionality for managing user accounts, games, achievements, and DLC data.
    /// </summary>
    public abstract class StoreApi : ObservableObject, IStoreApi, IStoreApiInternal
    {
        protected static readonly ILogger Logger = LogManager.GetLogger();

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
        private bool _forceFullLoginCheck;
        private long _authOperationGeneration;
        private long _authCheckGeneration;
        private readonly object _loginRefreshSync = new object();
        private Task _pendingLoginRefreshTask = Task.CompletedTask;
        private readonly List<Action> _loginRefreshCallbacks = new List<Action>();
        private int _loginRefreshResolutionDepth;
        private int _displayOnlyResolutionDepth;

        /// <summary>
        /// When true, <see cref="GetIsUserLoggedIn"/> must run a full verification (network), not cache shortcuts.
        /// Suppressed while resolving <see cref="IsUserLoggedInForDisplay"/>.
        /// </summary>
        protected bool ForceFullLoginCheck => _forceFullLoginCheck && _displayOnlyResolutionDepth == 0;

        /// <summary>
        /// Gets or sets whether the user is logged in.
        /// Does not block on in-flight <see cref="RefreshIsUserLoggedInInBackground"/>; use cached or fast-path status instead.
        /// </summary>
        public bool IsUserLoggedIn
        {
            get => ResolveIsUserLoggedIn();

            set
            {
                WaitForPendingLoginRefresh();
                SetValue(ref isUserLoggedIn, value);
            }
        }

        /// <inheritdoc />
        public bool IsLoginRefreshInProgress
        {
            get
            {
                lock (_loginRefreshSync)
                {
                    return !_pendingLoginRefreshTask.IsCompleted;
                }
            }
        }

        /// <inheritdoc />
        public bool IsUserLoggedInForDisplay
        {
            get
            {
                if (isUserLoggedIn.HasValue)
                {
                    return isUserLoggedIn.Value;
                }

                _displayOnlyResolutionDepth++;
                try
                {
                    return GetIsUserLoggedIn();
                }
                finally
                {
                    _displayOnlyResolutionDepth--;
                }
            }
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
        /// Prefixes a log message with the store client name (for example "[GOG] message").
        /// </summary>
        /// <param name="message">Log message body without client prefix.</param>
        /// <returns>Formatted message for extension log filtering.</returns>
        protected string FormatLogMessage(string message)
        {
            return $"[{ClientName}] {message}";
        }

        /// <summary>
        /// Writes an info log entry prefixed with the store client name.
        /// </summary>
        /// <param name="message">Log message body without client prefix.</param>
        protected void LogInfo(string message)
        {
            Logger.Info(FormatLogMessage(message));
        }

        /// <summary>
        /// Writes a warning log entry prefixed with the store client name.
        /// </summary>
        /// <param name="message">Log message body without client prefix.</param>
        protected void LogWarn(string message)
        {
            Logger.Warn(FormatLogMessage(message));
        }

        /// <summary>
        /// Writes an error log entry prefixed with the store client name.
        /// </summary>
        /// <param name="ex">Exception to log.</param>
        /// <param name="message">Log message body without client prefix.</param>
        protected void LogError(Exception ex, string message)
        {
            Logger.Error(ex, FormatLogMessage(message));
        }

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

        protected FileDataService FileDataService { get; }

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
		/// Clears the in-memory token without deleting the persisted token file.
		/// </summary>
		protected void ClearInMemoryStoreToken()
		{
			_storeToken = null;
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

            _lazyAccount = CreateAccountInfosLazy();
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

            FileDataService = new FileDataService(PluginName, ClientName)
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
        /// Reads stored cookies without logging when the cookie file is missing.
        /// Used for auth status probes during settings UI binding.
        /// </summary>
        /// <returns>List of stored HTTP cookies, or an empty list when none are available.</returns>
        protected List<Playnite.SDK.HttpCookie> PeekStoredCookies() => CookiesTools.GetStoredCookies(warnIfMissing: false);

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
        /// Serialized per store client so concurrent auth checks do not open competing WebViews.
        /// </summary>
        /// <param name="urls">List of URLs to retrieve cookies from</param>
        /// <param name="deleteCookies">Whether to delete cookies after retrieval</param>
        /// <param name="webView">Optional WebView instance to use</param>
        /// <returns>List of new HTTP cookies</returns>
        protected virtual List<Playnite.SDK.HttpCookie> GetNewWebCookies(List<string> urls, bool deleteCookies = false, IWebView webView = null)
        {
            return CookiesTools.GetNewWebCookiesSerialized(urls, deleteCookies, webView);
        }

		#endregion

		#region Token

        protected virtual StoreToken GetStoredToken() => LoadStoredToken(warnIfMissing: true);

        /// <summary>
        /// Loads the persisted OAuth token from disk.
        /// </summary>
        /// <param name="warnIfMissing">When true, logs a warning if the token file is absent.</param>
        /// <returns>The stored token, or null when missing or invalid.</returns>
        protected StoreToken LoadStoredToken(bool warnIfMissing)
        {
			lock (FileSystem.GetPathSyncRoot(FileToken))
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
						Common.LogError(ex, false, FormatLogMessage("Failed to load saved token"));
					}
				}
				else if (warnIfMissing)
				{
					LogWarn("No stored token");
				}

				return null;
			}
		}

        protected virtual bool SetStoredToken(StoreToken token)
		{
			lock (FileSystem.GetPathSyncRoot(FileToken))
			{
				try
				{
					if (token != null)
					{
						Encryption.EncryptToFileSafe(
							FileToken,
							Serialization.ToJson(token),
							Encoding.UTF8,
							WindowsIdentity.GetCurrent().User.Value);
						return true;
					}

					Common.LogDebug(true, FormatLogMessage("Session token cleared (nothing to save)."));
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, FormatLogMessage("Failed to save token"));
				}

				return false;
			}
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
		/// Blocks the caller until any queued background login refresh finishes.
		/// Skipped while the current thread is resolving login inside a refresh worker.
		/// </summary>
		private void WaitForPendingLoginRefresh()
		{
			if (_loginRefreshResolutionDepth > 0)
			{
				return;
			}

			Task pendingTask;
			lock (_loginRefreshSync)
			{
				pendingTask = _pendingLoginRefreshTask;
			}

			if (pendingTask != null && !pendingTask.IsCompleted)
			{
				Common.LogDebug(true, FormatLogMessage("IsUserLoggedIn: waiting for pending background login refresh."));
				pendingTask.GetAwaiter().GetResult();
			}
		}

		/// <summary>
		/// Resolves and caches login status without waiting on a pending background refresh.
		/// </summary>
		/// <returns>True when the user is authenticated for the current store session.</returns>
		private bool ResolveIsUserLoggedIn()
		{
			_loginRefreshResolutionDepth++;
			try
			{
				if (!isUserLoggedIn.HasValue)
				{
					isUserLoggedIn = GetIsUserLoggedIn();
				}

				return isUserLoggedIn.Value;
			}
			finally
			{
				_loginRefreshResolutionDepth--;
			}
		}

		/// <summary>
		/// Re-evaluates login status on a background thread and invokes queued callbacks on the UI thread.
		/// Concurrent requests are coalesced into a single in-flight refresh.
		/// UI bindings should use <see cref="IsUserLoggedInForDisplay"/> and <see cref="IsLoginRefreshInProgress"/>.
		/// </summary>
		/// <param name="onCompleted">Optional callback after the check finishes (UI thread).</param>
		public void RefreshIsUserLoggedInInBackground(Action onCompleted = null)
		{
			long operationGeneration = Interlocked.Read(ref _authOperationGeneration);
			TaskCompletionSource<object> refreshCompleted;
			Task previousGate;

			lock (_loginRefreshSync)
			{
				if (!_pendingLoginRefreshTask.IsCompleted)
				{
					if (onCompleted != null)
					{
						_loginRefreshCallbacks.Add(onCompleted);
					}

					Common.LogDebug(true, FormatLogMessage("RefreshIsUserLoggedInInBackground: coalesced with in-flight refresh."));
					return;
				}

				if (onCompleted != null)
				{
					_loginRefreshCallbacks.Add(onCompleted);
				}

				Common.LogDebug(true, FormatLogMessage($"RefreshIsUserLoggedInInBackground: scheduling full login check, generation={operationGeneration}."));

				refreshCompleted = new TaskCompletionSource<object>();
				previousGate = _pendingLoginRefreshTask;
				_pendingLoginRefreshTask = refreshCompleted.Task;
			}

			previousGate.ContinueWith(_ =>
			{
				try
				{
					if (!IsAuthOperationCurrent(operationGeneration))
					{
						Common.LogDebug(true, FormatLogMessage($"RefreshIsUserLoggedInInBackground: aborted before start (generation={operationGeneration}, current={Interlocked.Read(ref _authOperationGeneration)})."));
						return;
					}

					try
					{
						BeginFullLoginCheck(operationGeneration);
						if (!IsAuthOperationCurrent(operationGeneration))
						{
							Common.LogDebug(true, FormatLogMessage("RefreshIsUserLoggedInInBackground: aborted after BeginFullLoginCheck (superseded auth operation)."));
							return;
						}

						bool isLoggedIn;
						_loginRefreshResolutionDepth++;
						try
						{
							isLoggedIn = GetIsUserLoggedIn();
						}
						finally
						{
							_loginRefreshResolutionDepth--;
						}

						if (!IsAuthCheckCurrent())
						{
							Common.LogDebug(true, FormatLogMessage("RefreshIsUserLoggedInInBackground: result discarded (superseded auth operation)."));
							return;
						}

						SetValue(ref isUserLoggedIn, isLoggedIn);
						Common.LogDebug(true, FormatLogMessage($"RefreshIsUserLoggedInInBackground: full check completed, IsUserLoggedIn={isLoggedIn}."));
					}
					catch (Exception ex)
					{
						LogError(ex, "RefreshIsUserLoggedInInBackground failed");
					}
					finally
					{
						EndFullLoginCheck();
					}
				}
				finally
				{
					refreshCompleted.TrySetResult(null);
					NotifyLoginRefreshCallbacks();
				}
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
		}

		/// <summary>
		/// Invokes and clears callbacks registered for the completed login refresh.
		/// </summary>
		private void NotifyLoginRefreshCallbacks()
		{
			List<Action> callbacks;
			lock (_loginRefreshSync)
			{
				if (_loginRefreshCallbacks.Count == 0)
				{
					return;
				}

				callbacks = new List<Action>(_loginRefreshCallbacks);
				_loginRefreshCallbacks.Clear();
			}

			Common.LogDebug(true, FormatLogMessage($"RefreshIsUserLoggedInInBackground: notifying UI ({callbacks.Count} callback(s))."));
			foreach (Action callback in callbacks)
			{
				if (callback != null)
				{
					API.Instance.MainView.UIDispatcher?.BeginInvoke(callback);
				}
			}
		}

		/// <summary>
		/// Invalidates in-flight auth checks and returns the new operation generation.
		/// </summary>
		protected long BeginAuthOperation()
		{
			long generation = Interlocked.Increment(ref _authOperationGeneration);
			Common.LogDebug(true, FormatLogMessage($"BeginAuthOperation: generation={generation}."));
			return generation;
		}

		/// <summary>
		/// Returns true when <paramref name="operationGeneration"/> is still the active auth generation.
		/// </summary>
		protected bool IsAuthOperationCurrent(long operationGeneration)
		{
			return operationGeneration == Interlocked.Read(ref _authOperationGeneration);
		}

		/// <summary>
		/// Returns true when the current full-login check has not been superseded.
		/// </summary>
		protected bool IsAuthCheckCurrent()
		{
			return IsAuthOperationCurrent(_authCheckGeneration);
		}

		/// <summary>
		/// Enables full login verification for the current background refresh.
		/// </summary>
		/// <param name="operationGeneration">Auth generation captured when the check started.</param>
		protected virtual void BeginFullLoginCheck(long operationGeneration)
		{
			_forceFullLoginCheck = true;
			_authCheckGeneration = operationGeneration;
		}

		/// <summary>
		/// Disables full login verification after a background refresh.
		/// </summary>
		protected virtual void EndFullLoginCheck()
		{
			_forceFullLoginCheck = false;
			_authCheckGeneration = 0;
		}

		/// <summary>
		/// Reads persisted OAuth token metadata without calling store APIs.
		/// </summary>
		/// <returns>True or false when a token file exists; null when absent.</returns>
		protected bool? TryGetAuthStatusFromStoredToken()
		{
			StoreToken token = _storeToken ?? LoadStoredToken(warnIfMissing: false);
			if (token == null)
			{
				return null;
			}

			if (token.Token.IsNullOrEmpty() && token.RefreshToken.IsNullOrEmpty())
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Returns true when HTTP cookies were persisted for this store client.
		/// </summary>
		protected bool TryGetAuthStatusFromStoredCookies()
		{
			List<Playnite.SDK.HttpCookie> cookies = PeekStoredCookies();
			return cookies != null && cookies.Count > 0;
		}

		/// <summary>
		/// Returns true when encrypted user profile data exists from a prior session.
		/// </summary>
		protected bool TryGetAuthStatusFromSavedUser()
		{
			AccountInfos savedUser = LoadCurrentUser();
			return savedUser != null && !savedUser.UserId.IsNullOrEmpty();
		}

		/// <summary>
		/// Returns true when persisted cookies or a non-empty OAuth token exist for web auth verification.
		/// Saved user profile data alone is not sufficient.
		/// </summary>
		protected bool HasSessionCredentialsForAuth()
		{
			if (TryGetAuthStatusFromStoredCookies())
			{
				return true;
			}

			bool? tokenAuth = TryGetAuthStatusFromStoredToken();
			return tokenAuth == true;
		}

        /// <inheritdoc />
        public void ReloadAccountInfos()
        {
            _accountCache.Clear();
            SetValue(ref _lazyAccount, CreateAccountInfosLazy(), nameof(CurrentAccountInfos));
            isUserLoggedIn = null;
            Common.LogDebug(true, FormatLogMessage("ReloadAccountInfos: account cache invalidated."));
        }

        private Lazy<AccountInfos> CreateAccountInfosLazy()
        {
            return new Lazy<AccountInfos>(() => _accountCache.GetOrSet("current_account", GetCurrentAccountInfos, TimeSpan.FromHours(24)));
        }

        /// <summary>
        /// Clears stored authentication data and session profile fields for the current account.
        /// Manual store settings such as the Steam Web API key are preserved.
        /// </summary>
        public virtual void ClearSession()
        {
            LogInfo("ClearSession started");
            _ = BeginAuthOperation();

            try
            {
                CookiesTools.ClearStoredCookies();
                CookiesTools.ClearDomainCookies();
                ClearStoredToken();
                ClearSessionProfileFields();
                SetSessionLoggedOutByUser();
                IsUserLoggedIn = false;
                SaveCurrentUser();
                LogInfo("ClearSession completed");
            }
            catch (Exception ex)
            {
                LogError(ex, "ClearSession failed");
                throw;
            }
        }

        /// <summary>
        /// Deletes the stored authentication token file and in-memory token cache.
        /// </summary>
        protected void ClearStoredToken()
        {
            _storeToken = null;

            lock (FileSystem.GetPathSyncRoot(FileToken))
            {
                if (File.Exists(FileToken))
                {
                    FileSystem.DeleteFileSafe(FileToken);
                    LogInfo("Stored token file deleted");
                    Common.LogDebug(true, FormatLogMessage($"ClearStoredToken: {FileToken}"));
                }
                else
                {
                    Common.LogDebug(true, FormatLogMessage("ClearStoredToken: no token file found"));
                }
            }
        }

        /// <summary>
        /// Clears profile fields populated by an authenticated session.
        /// </summary>
        protected void ClearSessionProfileFields()
        {
            AccountInfos user = CurrentAccountInfos;

            if (user == null)
            {
                LogWarn("ClearSession: no current account to update");
                return;
            }

            user.UserId = null;
            user.Avatar = null;
            user.Link = null;
            user.Pseudo = null;
            user.AccountStatus = AccountStatus.Unknown;
            Common.LogDebug(true, FormatLogMessage("ClearSession: session profile fields cleared (UserId, Pseudo, avatar, link)"));
        }

        /// <summary>
        /// Marks the account as explicitly logged out so stores can suppress automatic SSO re-authentication.
        /// </summary>
        protected void SetSessionLoggedOutByUser()
        {
            AccountInfos user = CurrentAccountInfos;
            if (user == null)
            {
                LogWarn("ClearSession: no current account to persist logged-out flag");
                return;
            }

            user.SessionLoggedOutByUser = true;
            Common.LogDebug(true, FormatLogMessage("ClearSession: session logged-out flag set (SSO refresh suppressed)."));
        }

        /// <inheritdoc />
        public void PrepareExplicitLogin()
        {
            AccountInfos user = CurrentAccountInfos;
            if (user == null || !user.SessionLoggedOutByUser)
            {
                return;
            }

            user.SessionLoggedOutByUser = false;
            SaveCurrentUser();
            Common.LogDebug(true, FormatLogMessage("PrepareExplicitLogin: session logged-out flag cleared."));
        }

        /// <summary>
        /// Returns true when persisted account data should be kept after session identifiers were cleared.
        /// </summary>
        protected static bool HasPersistedManualAccountData(AccountInfos accountInfos)
        {
            return accountInfos != null && !accountInfos.ApiKey.IsNullOrEmpty();
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
        /// Strips query strings from auth WebView URLs before logging.
        /// </summary>
        /// <param name="url">Full WebView address.</param>
        /// <returns>Path-only URL safe for logs, or a fallback label.</returns>
        protected static string FormatAuthWebViewUrlForLog(string url)
        {
            if (url.IsNullOrEmpty())
            {
                return "(empty)";
            }

            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                {
                    return uri.GetLeftPart(UriPartial.Path);
                }
            }
            catch
            {
            }

            return url;
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
            lock (FileSystem.GetPathSyncRoot(FileUser))
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
                        Common.LogError(ex, false, true, PluginName, FormatLogMessage("Failed to load user"));
                    }
                }

                return null;
            }
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
                lock (FileSystem.GetPathSyncRoot(FileUser))
                {
                    Encryption.EncryptToFileSafe(
                        FileUser,
                        Serialization.ToJson(CurrentAccountInfos),
                        Encoding.UTF8,
                        WindowsIdentity.GetCurrent().User.Value);
                }
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

            if (HasPersistedManualAccountData(accountInfos))
            {
                accountInfos.IsCurrent = true;
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
            return FileDataService.LoadData<ObservableCollection<GameDlcOwned>>(FileGamesDlcsOwned, onlyNow ? 5 : 0);
        }

        /// <summary>
        /// Saves games and DLC ownership data to file storage.
        /// </summary>
        /// <param name="gamesDlcsOwned">Collection of owned games and DLC to save</param>
        /// <returns>True if save was successful, false otherwise</returns>
        private bool SaveGamesDlcsOwned(ObservableCollection<GameDlcOwned> gamesDlcsOwned)
        {
            return FileDataService.SaveData(FileGamesDlcsOwned, gamesDlcsOwned);
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
                Common.LogError(ex, false, true, PluginName, FormatLogMessage("Failed to check DLC ownership"));
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
            LogWarn($"Using saved user data from {formatedDateLastWrite}");
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
            LogWarn("User is not authenticated");

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
                        Common.LogError(ex, false, true, PluginName, FormatLogMessage("Failed to open plugin settings from authentication notification"));
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
