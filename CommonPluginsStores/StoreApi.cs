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
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores
{
    public abstract class StoreApi : ObservableObject, IStoreApi, IStoreApiInternal
    {
        internal static ILogger Logger => LogManager.GetLogger();


        #region Account data

        protected AccountInfos _currentAccountInfos;
        public AccountInfos CurrentAccountInfos
        {
            get
            {
                _currentAccountInfos = _currentAccountInfos ?? GetCurrentAccountInfos();
                return _currentAccountInfos;
            }

            set => SetValue(ref _currentAccountInfos, value);
        }

        protected ObservableCollection<AccountInfos> _currentFriendsInfos;
        public ObservableCollection<AccountInfos> CurrentFriendsInfos
        {
            get
            {
                if (_currentFriendsInfos?.FirstOrDefault()?.LastCall.AddMinutes(30) < DateTime.Now)
                {
                    _currentFriendsInfos = null;
                }
                _currentFriendsInfos = _currentFriendsInfos ?? GetCurrentFriendsInfos();
                return _currentFriendsInfos;
            }

            set => SetValue(ref _currentFriendsInfos, value);
        }

        protected ObservableCollection<AccountGameInfos> _currentGamesInfos;
        public ObservableCollection<AccountGameInfos> CurrentGamesInfos
        {
            get
            {
                if (_currentGamesInfos?.FirstOrDefault()?.LastCall.AddMinutes(30) < DateTime.Now)
                {
                    _currentGamesInfos = null;
                }
                _currentGamesInfos = _currentGamesInfos ?? GetAccountGamesInfos(CurrentAccountInfos);
                return _currentGamesInfos;
            }

            set => SetValue(ref _currentGamesInfos, value);
        }

        public ObservableCollection<GameDlcOwned> CurrentGamesDlcsOwned
        {
            get
            {
                ObservableCollection<GameDlcOwned> currentGamesDlcsOwned = LoadGamesDlcsOwned() ?? GetGamesDlcsOwned() ?? LoadGamesDlcsOwned(false);
                _ = SaveGamesDlcsOwned(currentGamesDlcsOwned);
                return currentGamesDlcsOwned;
            }
        }

        #endregion


        #region Properties

        public StoreSettings StoreSettings { get; set; } = new StoreSettings();

        protected bool? isUserLoggedIn;
        public bool IsUserLoggedIn
        {
            get
            {
                isUserLoggedIn = isUserLoggedIn ?? GetIsUserLoggedIn();
                return (bool)isUserLoggedIn;
            }

            set => SetValue(ref isUserLoggedIn, value);
        }

        internal string PluginName { get; }
        internal string ClientName { get; }
        internal string ClientNameLog { get; }
        internal string Locale { get; set; } = "en_US";

        internal string PathStoresData { get; }
        internal string PathAppsData { get; }
        internal string PathAchievementsData { get; }

        internal string FileUser { get; }
        internal string FileCookies { get; }
        internal string FileGamesDlcsOwned { get; }

        internal List<string> CookiesDomains { get; set; }

        internal CookiesTools CookiesTools { get; }

        internal StoreToken AuthToken { get; set; }
        internal ExternalPlugin PluginLibrary { get; }

        #endregion


        public StoreApi(string pluginName, ExternalPlugin pluginLibrary, string clientName)
        {
            PluginName = pluginName;
            PluginLibrary = pluginLibrary;
            ClientName = clientName;
            ClientNameLog = clientName.RemoveWhiteSpace();

            string pathCacheData = Path.Combine(PlaynitePaths.DataCachePath, "StoresData");
            PathAppsData = Path.Combine(pathCacheData, ClientNameLog, "Apps");
            PathAchievementsData = Path.Combine(pathCacheData, ClientNameLog, "Achievements");

            PathStoresData = Path.Combine(PlaynitePaths.ExtensionsDataPath, "StoresData");
            FileUser = Path.Combine(PathStoresData, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientNameLog}_User.dat"));
            FileCookies = Path.Combine(PathStoresData, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientNameLog}_Cookies.dat"));
            FileGamesDlcsOwned = Path.Combine(PathStoresData, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientNameLog}_GamesDlcsOwned.json"));

            CookiesTools = new CookiesTools(
                PluginName,
                ClientName,
                FileCookies,
                CookiesDomains);

            FileSystem.CreateDirectory(PathStoresData);
        }


        #region Cookies

        /// <summary>
        /// Read the last identified cookies stored.
        /// </summary>
        /// <returns></returns>
        internal virtual List<HttpCookie> GetStoredCookies() => CookiesTools.GetStoredCookies();

        /// <summary>
        /// Save the last identified cookies stored.
        /// </summary>
        /// <param name="httpCookies"></param>
        internal virtual bool SetStoredCookies(List<HttpCookie> httpCookies) => CookiesTools.SetStoredCookies(httpCookies);

        /// <summary>
        /// Get cookies in WebView or another method.
        /// </summary>
        /// <returns></returns>
        internal virtual List<HttpCookie> GetWebCookies(bool deleteCookies = false, IWebView webView = null) => CookiesTools.GetWebCookies(deleteCookies, webView);

        internal virtual List<HttpCookie> GetNewWebCookies(List<string> urls, bool deleteCookies = false, IWebView webView = null) => CookiesTools.GetNewWebCookies(urls, deleteCookies, webView);

        #endregion


        #region Configuration

        public void ResetIsUserLoggedIn()
        {
            isUserLoggedIn = null;
        }

        protected abstract bool GetIsUserLoggedIn();

        /// <summary>
        /// Set data language.
        /// </summary>
        /// <param name="locale">ISO 15897</param>
        public void SetLanguage(string locale)
        {
            Locale = locale;
        }

        public void SetForceAuth(bool forceAuth)
        {
            StoreSettings.ForceAuth = forceAuth;
        }

        public virtual void Login()
        {
            throw new NotImplementedException();
        }

        public virtual void LoginAlternative()
        {
            throw new NotImplementedException();
        }

        AccountInfos IStoreApiInternal.LoadCurrentUser() => LoadCurrentUser();

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

        void IStoreApiInternal.SaveCurrentUser() => SaveCurrentUser();

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


        public virtual void Intialization(StoreSettings storeSettings, bool loadUser)
        {
            SetLanguage(API.Instance.ApplicationSettings.Language);
            StoreSettings = storeSettings;
            if (loadUser)
            {
                _ = CurrentAccountInfos;
            }
        }

        public virtual void SaveSettings(StoreSettings storeSettings, bool saveUser) 
        {
            StoreSettings = storeSettings;
            if (saveUser)
            {
                SaveCurrentUser();
                CurrentAccountInfos = null;
                _ = CurrentAccountInfos;
            }
        }

        #endregion


        #region Current user

        /// <summary>
        /// Get current user info.
        /// </summary>
        /// <returns></returns>
        protected abstract AccountInfos GetCurrentAccountInfos();


        /// <summary>
        /// Get current user's friends info.
        /// </summary>
        /// <returns></returns>
        protected virtual ObservableCollection<AccountInfos> GetCurrentFriendsInfos()
        {
            return null;
        }

        /// <summary>
        /// Get all game's owned for current user.
        /// </summary>
        /// <returns></returns>
        protected virtual ObservableCollection<GameDlcOwned> GetGamesOwned()
        {
            return null;
        }
        #endregion


        #region User details
        /// <summary>
        /// Get the user's games list.
        /// /// </summary>
        /// <param name="accountInfos"></param>
        /// <returns></returns>
        public abstract ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos);

        /// <summary>
        /// Get a list of a game's achievements with a user's possessions.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accountInfos"></param>
        /// <returns></returns>
        public virtual ObservableCollection<GameAchievement> GetAchievements(string id, AccountInfos accountInfos)
        {
            return null;
        }

        /// <summary>
        /// Get achievements SourceLink.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="accountInfos"></param>
        /// <returns></returns>
        public virtual SourceLink GetAchievementsSourceLink(string name, string id, AccountInfos accountInfos)
        {
            return null;
        }

        public virtual ObservableCollection<AccountWishlist> GetWishlist(AccountInfos accountInfos)
        {
            return null;
        }

        public virtual bool RemoveWishlist(string id)
        {
            return false;
        }
        #endregion


        #region Game
        /// <summary>
        /// Get game informations.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accountInfos"></param>
        /// <returns></returns>
        public virtual GameInfos GetGameInfos(string id, AccountInfos accountInfos)
        {
            return null;
        }

        public virtual Tuple<string, ObservableCollection<GameAchievement>> GetAchievementsSchema(string id)
        {
            return null;
        }

        /// <summary>
        /// Get dlc informations for a game.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accountInfos"></param>
        /// <returns></returns>
        public virtual ObservableCollection<DlcInfos> GetDlcInfos(string id, AccountInfos accountInfos)
        {
            return null;
        }
        #endregion


        #region Games owned
        private ObservableCollection<GameDlcOwned> LoadGamesDlcsOwned(bool onlyNow = true)
        {
            return LoadData<ObservableCollection<GameDlcOwned>>(FileGamesDlcsOwned, onlyNow ? 5 : 0);
        }

        private bool SaveGamesDlcsOwned(ObservableCollection<GameDlcOwned> gamesDlcsOwned)
        {
            try
            {
                FileSystem.PrepareSaveFile(FileGamesDlcsOwned);
                File.WriteAllText(FileGamesDlcsOwned, Serialization.ToJson(gamesDlcsOwned));
                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return false;
            }
        }


        internal virtual ObservableCollection<GameDlcOwned> GetGamesDlcsOwned()
        {
            return null;
        }


        internal virtual bool IsDlcOwned(string id)
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


        internal void ShowNotificationOldData(DateTime dateLastWrite)
        {
            LocalDateTimeConverter localDateTimeConverter = new LocalDateTimeConverter();
            string formatedDateLastWrite = localDateTimeConverter.Convert(dateLastWrite, null, null, CultureInfo.CurrentCulture).ToString();
            Logger.Warn($"Use saved UserData - {formatedDateLastWrite}");
            API.Instance.Notifications.Add(new NotificationMessage(
                $"{PluginName}-{ClientNameLog}-LoadGamesDlcsOwned",
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


        internal T LoadData<T>(string filePath, int minutes) where T : class
        {
            if (filePath.IsNullOrEmpty())
            {
                Logger.Warn($"filePath is empty");
                return null;
            }

            if (!File.Exists(filePath))
            {
                //Logger.Warn($"File not found: {filePath}");
                return null;
            }

            try
            {
                DateTime dateLastWrite = File.GetLastWriteTime(filePath);

                if (minutes > 0 && dateLastWrite.AddMinutes(minutes) <= DateTime.Now)
                {
                    return null;
                }

                if (minutes == 0)
                {
                    ShowNotificationOldData(dateLastWrite);
                }

                return Serialization.FromJsonFile<T>(filePath);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }

        public void PurgeCache()
        {
            FileSystem.DeleteDirectory(PathAppsData);
            FileSystem.DeleteDirectory(PathAchievementsData);
        }
    }
}