using CommonPlayniteShared;
using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Principal;
using System.Text;

namespace CommonPluginsStores
{
    public abstract class StoreApi : ObservableObject
    {
        internal static readonly ILogger logger = LogManager.GetLogger();
        internal static readonly IResourceProvider resources = new ResourceProvider();


        protected static IWebView _WebViewOffscreen;
        internal static IWebView WebViewOffscreen
        {
            get
            {
                if (_WebViewOffscreen == null)
                {
                    _WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView();
                }
                return _WebViewOffscreen;
            }

            set
            {
                _WebViewOffscreen = value;
            }
        }


        #region Account data
        protected AccountInfos _CurrentAccountInfos;
        public AccountInfos CurrentAccountInfos
        {
            get
            {
                if (_CurrentAccountInfos == null)
                {
                    _CurrentAccountInfos = GetCurrentAccountInfos();
                }
                return _CurrentAccountInfos;
            }

            set => SetValue(ref _CurrentAccountInfos, value);
        }

        protected ObservableCollection<AccountInfos> _CurrentFriendsInfos;
        public ObservableCollection<AccountInfos> CurrentFriendsInfos
        {
            get
            {
                if (_CurrentFriendsInfos == null)
                {
                    _CurrentFriendsInfos = GetCurrentFriendsInfos();
                }
                return _CurrentFriendsInfos;
            }

            set => SetValue(ref _CurrentFriendsInfos, value);
        }

        protected ObservableCollection<AccountGameInfos> _CurrentGamesInfos;
        public ObservableCollection<AccountGameInfos> CurrentGamesInfos
        {
            get
            {
                if (_CurrentGamesInfos == null)
                {
                    _CurrentGamesInfos = GetAccountGamesInfos(CurrentAccountInfos);
                }
                return _CurrentGamesInfos;
            }

            set => SetValue(ref _CurrentGamesInfos, value);
        }
        #endregion


        protected bool? _IsUserLoggedIn;
        public bool IsUserLoggedIn
        {
            get
            {
                if (_IsUserLoggedIn == null)
                {
                    _IsUserLoggedIn = GetIsUserLoggedIn();
                }
                return (bool)_IsUserLoggedIn;
            }

            set => SetValue(ref _IsUserLoggedIn, value);
        }


        internal string ClientName { get; }
        internal string Local = "en_US";

        internal readonly string PathStoresData;
        internal readonly string PathCookies;

        internal StoreToken AuthToken;


        public StoreApi(string ClientName)
        {
            this.ClientName = ClientName;
            PathStoresData = Path.Combine(PlaynitePaths.ExtensionsDataPath, "StoresData");
            PathCookies = Path.Combine(PathStoresData, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientName}.json"));

            if (!Directory.Exists(PathStoresData))
            {
                Directory.CreateDirectory(PathStoresData);
            }
        }


        #region Cookies
        /// <summary>
        /// Read the last identified cookies stored.
        /// </summary>
        /// <returns></returns>
        internal List<HttpCookie> GetStoredCookies()
        {
            if (File.Exists(PathCookies))
            {
                try
                {
                    return Serialization.FromJson<List<HttpCookie>>(
                        Encryption.DecryptFromFile(
                            PathCookies,
                            Encoding.UTF8,
                            WindowsIdentity.GetCurrent().User.Value));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "Failed to load saved cookies");
                }
            }
            else
            {
                logger.Info("No stored cookies");
                List<HttpCookie> httpCookies = GetWebCookies();
                if (httpCookies?.Count > 0)
                {
                    SetStoredCookies(httpCookies);
                }
            }

            return null;
        }

        /// <summary>
        /// Save the last identified cookies stored.
        /// </summary>
        /// <param name="httpCookies"></param>
        internal bool SetStoredCookies(List<HttpCookie> httpCookies)
        {
            try 
            { 
                FileSystem.CreateDirectory(Path.GetDirectoryName(PathCookies));
                Encryption.EncryptToFile(
                    PathCookies,
                    Serialization.ToJson(httpCookies),
                    Encoding.UTF8,
                    WindowsIdentity.GetCurrent().User.Value);
                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "Failed to save cookies");
            }

            return false;
        }

        /// <summary>
        /// Get cookies in WebView or another method.
        /// </summary>
        /// <returns></returns>
        internal abstract List<HttpCookie> GetWebCookies();
        #endregion


        #region Configuration
        public void ResetIsUserLoggedIn()
        {
            _IsUserLoggedIn = null;
        }

        protected abstract bool GetIsUserLoggedIn();

        /// <summary>
        /// Set data language.
        /// </summary>
        /// <param name="local">ISO 15897</param>
        public void SetLanguage(string local) 
        {
            Local = local;
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
        protected abstract ObservableCollection<AccountInfos> GetCurrentFriendsInfos();
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
        /// <param name="Id"></param>
        /// <param name="accountInfos"></param>
        /// <returns></returns>
        public virtual ObservableCollection<GameAchievement> GetAchievements(string Id, AccountInfos accountInfos)
        {
            return null;
        }
        #endregion


        #region Game
        /// <summary>
        /// Get game informations.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Local"></param>
        /// <returns></returns>
        public virtual GameInfos GetGameInfos(string Id, AccountInfos accountInfos) { return null; }

        /// <summary>
        /// Get dlc informations for a game.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public virtual ObservableCollection<DlcInfos> GetDlcInfos(string Id, AccountInfos accountInfos) { return null; }
        #endregion
    }
}
