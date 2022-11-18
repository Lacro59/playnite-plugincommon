using CommonPlayniteShared;
using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
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
using static CommonPluginsShared.PlayniteTools;

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

            set => _WebViewOffscreen = value;
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


        protected ObservableCollection<GameDlcOwned> _CurrentGamesDlcsOwned;
        public ObservableCollection<GameDlcOwned> CurrentGamesDlcsOwned
        {
            get
            {
                if (_CurrentGamesDlcsOwned == null)
                {
                    _CurrentGamesDlcsOwned = LoadGamesDlcsOwned();
                    if (_CurrentGamesDlcsOwned == null)
                    {
                        _CurrentGamesDlcsOwned = GetGamesDlcsOwned();
                        if (_CurrentGamesDlcsOwned?.Count > 0)
                        {
                            SaveGamesDlcsOwned(_CurrentGamesDlcsOwned);
                        }
                        else
                        {
                            _CurrentGamesDlcsOwned = LoadGamesDlcsOwned(false);
                        }
                    }
                }

                return _CurrentGamesDlcsOwned;
            }

            set => SetValue(ref _CurrentGamesDlcsOwned, value);
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


        internal string PluginName { get; }
        internal string ClientName { get; }
        internal string Local = "en_US";

        internal string PathStoresData { get; }
        internal string FileCookies { get; }
        internal string FileGamesDlcsOwned { get; }

        internal StoreToken AuthToken;
        internal ExternalPlugin PluginLibrary { get; }


        public StoreApi(string PluginName, ExternalPlugin PluginLibrary, string ClientName)
        {
            this.PluginName = PluginName;
            this.PluginLibrary = PluginLibrary;
            this.ClientName = ClientName;
            PathStoresData = Path.Combine(PlaynitePaths.ExtensionsDataPath, "StoresData");
            FileCookies = Path.Combine(PathStoresData, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientName}.json"));
            FileGamesDlcsOwned = Path.Combine(PathStoresData, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientName}_GamesDlcsOwned.json"));

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
            string InfoMessage = "No stored cookies";

            if (File.Exists(FileCookies))
            {
                try
                {
                    List<HttpCookie> StoredCookies = Serialization.FromJson<List<HttpCookie>>(
                        Encryption.DecryptFromFile(
                            FileCookies,
                            Encoding.UTF8,
                            WindowsIdentity.GetCurrent().User.Value));

                    var findExpired = StoredCookies.FindAll(x => x.Expires != null && (DateTime)x.Expires <= DateTime.Now);

                    FileInfo fileInfo = new FileInfo(FileCookies);
                    bool isExpired = (fileInfo.LastWriteTime.AddDays(1) < DateTime.Now);

                    if (findExpired?.Count > 0 || isExpired)
                    {
                        InfoMessage = "Expired cookies";
                    }
                    else
                    {
                        return StoredCookies;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "Failed to load saved cookies");
                }
            }

            logger.Info(InfoMessage);
            List<HttpCookie> httpCookies = GetWebCookies();
            if (httpCookies?.Count > 0)
            {
                SetStoredCookies(httpCookies);
                return httpCookies;
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
                FileSystem.CreateDirectory(Path.GetDirectoryName(FileCookies));
                Encryption.EncryptToFile(
                    FileCookies,
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
        internal virtual List<HttpCookie> GetWebCookies()
        {
            List<HttpCookie> httpCookies = WebViewOffscreen.GetCookies()?.Where(x => x?.Domain?.Contains(ClientName.ToLower()) ?? false)?.ToList() ?? new List<HttpCookie>();
            return httpCookies;
        }
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
        /// <param name="Id"></param>
        /// <param name="accountInfos"></param>
        /// <returns></returns>
        public virtual ObservableCollection<GameAchievement> GetAchievements(string Id, AccountInfos accountInfos)
        {
            return null;
        }

        /// <summary>
        /// Get achievements SourceLink.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="accountInfos"></param>
        /// <returns></returns>
        public virtual SourceLink GetAchievementsSourceLink(string Name, string Id, AccountInfos accountInfos)
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


        #region Games owned
        private ObservableCollection<GameDlcOwned> LoadGamesDlcsOwned(bool OnlyNow = true)
        {
            if (File.Exists(FileGamesDlcsOwned))
            {
                try
                {
                    DateTime DateLastWrite = File.GetLastWriteTime(FileGamesDlcsOwned);
                    if (OnlyNow && !DateLastWrite.ToString("yyyy-MM-dd").IsEqual(DateTime.Now.ToString("yyyy-MM-dd")))
                    {
                        return null;
                    }

                    if (!OnlyNow)
                    {
                        LocalDateTimeConverter localDateTimeConverter = new LocalDateTimeConverter();
                        string formatedDateLastWrite = localDateTimeConverter.Convert(DateLastWrite, null, null, CultureInfo.CurrentCulture).ToString();
                        logger.Warn($"Use saved UserData - {formatedDateLastWrite}");
                        API.Instance.Notifications.Add(new NotificationMessage(
                            $"{PluginName}-{ClientName}-LoadGamesDlcsOwned",
                            $"{PluginName}" + Environment.NewLine
                                + string.Format(resources.GetString("LOCCommonNotificationOldData"), ClientName, formatedDateLastWrite),
                            NotificationType.Info,
                            () =>
                            {
                                ResetIsUserLoggedIn();
                                CommonPluginsShared.PlayniteTools.ShowPluginSettings(PluginLibrary);
                            }
                        ));
                    }

                    return Serialization.FromJsonFile<ObservableCollection<GameDlcOwned>>(FileGamesDlcsOwned);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }

            return null;
        }

        private bool SaveGamesDlcsOwned(ObservableCollection<GameDlcOwned> CurrentGamesDlcsOwned)
        {
            try
            {
                FileSystem.PrepareSaveFile(FileGamesDlcsOwned);
                File.WriteAllText(FileGamesDlcsOwned, Serialization.ToJson(CurrentGamesDlcsOwned));
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


        internal virtual bool DlcIsOwned(string Id)
        {
            try
            {
                bool IsOwned = CurrentGamesDlcsOwned.Where(x => x.Id.IsEqual(Id)).Count() != 0;
                return IsOwned;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return false;
            }
        }
        #endregion
    }
}
