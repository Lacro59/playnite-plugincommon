using CommonPlayniteShared;
using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
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
    public abstract class StoreApi : ObservableObject, IStoreApi
    {
        internal static ILogger Logger => LogManager.GetLogger();


        #region Account data
        protected AccountInfos currentAccountInfos;
        public AccountInfos CurrentAccountInfos
        {
            get
            {
                if (currentAccountInfos == null)
                {
                    currentAccountInfos = GetCurrentAccountInfos();
                }
                return currentAccountInfos;
            }

            set => SetValue(ref currentAccountInfos, value);
        }

        protected ObservableCollection<AccountInfos> currentFriendsInfos;
        public ObservableCollection<AccountInfos> CurrentFriendsInfos
        {
            get
            {
                if (currentFriendsInfos == null)
                {
                    currentFriendsInfos = GetCurrentFriendsInfos();
                }
                return currentFriendsInfos;
            }

            set => SetValue(ref currentFriendsInfos, value);
        }

        protected ObservableCollection<AccountGameInfos> currentGamesInfos;
        public ObservableCollection<AccountGameInfos> CurrentGamesInfos
        {
            get
            {
                if (currentGamesInfos == null)
                {
                    currentGamesInfos = GetAccountGamesInfos(CurrentAccountInfos);
                }
                return currentGamesInfos;
            }

            set => SetValue(ref currentGamesInfos, value);
        }

        protected ObservableCollection<GameDlcOwned> currentGamesDlcsOwned;
        public ObservableCollection<GameDlcOwned> CurrentGamesDlcsOwned
        {
            get
            {
                if (currentGamesDlcsOwned == null)
                {
                    currentGamesDlcsOwned = LoadGamesDlcsOwned();
                    if (currentGamesDlcsOwned == null)
                    {
                        currentGamesDlcsOwned = GetGamesDlcsOwned();
                        if (currentGamesDlcsOwned?.Count > 0)
                        {
                            _ = SaveGamesDlcsOwned(currentGamesDlcsOwned);
                        }
                        else
                        {
                            currentGamesDlcsOwned = LoadGamesDlcsOwned(false);
                        }
                    }
                }

                return currentGamesDlcsOwned;
            }

            set => SetValue(ref currentGamesDlcsOwned, value);
        }
        #endregion


        protected bool? isUserLoggedIn;
        public bool IsUserLoggedIn
        {
            get
            {
                if (isUserLoggedIn == null)
                {
                    isUserLoggedIn = GetIsUserLoggedIn();
                    if ((bool)isUserLoggedIn)
                    {
                        _ = SetStoredCookies(GetWebCookies());
                    }
                }
                return (bool)isUserLoggedIn;
            }

            set => SetValue(ref isUserLoggedIn, value);
        }

        public bool ForceAuth { get; set; } = false;

        internal string PluginName { get; }
        internal string ClientName { get; }
        internal string ClientNameLog { get; }
        internal string Local { get; set; } = "en_US";

        internal string PathStoresData { get; }
        internal string FileUser { get; }
        internal string FileCookies { get; }
        internal string FileGamesDlcsOwned { get; }

        internal StoreToken AuthToken { get; set; }
        internal ExternalPlugin PluginLibrary { get; }


        public StoreApi(string pluginName, ExternalPlugin pluginLibrary, string clientName)
        {
            PluginName = pluginName;
            PluginLibrary = pluginLibrary;
            ClientName = clientName;
            ClientNameLog = clientName.RemoveWhiteSpace();

            PathStoresData = Path.Combine(PlaynitePaths.ExtensionsDataPath, "StoresData"); 
            FileUser = Path.Combine(PathStoresData, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientNameLog}_User.dat"));
            FileCookies = Path.Combine(PathStoresData, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientNameLog}_Cookies.dat"));
            FileGamesDlcsOwned = Path.Combine(PathStoresData, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientNameLog}_GamesDlcsOwned.json"));

            FileSystem.CreateDirectory(PathStoresData);
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

                    List<HttpCookie> findExpired = StoredCookies.FindAll(x => x.Expires != null && (DateTime)x.Expires <= DateTime.Now);

                    FileInfo fileInfo = new FileInfo(FileCookies);
                    bool isExpired = fileInfo.LastWriteTime.AddDays(1) < DateTime.Now;

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

            Logger.Warn(InfoMessage);
            List<HttpCookie> httpCookies = GetWebCookies();
            if (httpCookies?.Count > 0)
            {
                _ = SetStoredCookies(httpCookies);
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
            using (IWebView webViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
            {
                List<HttpCookie> httpCookies = webViewOffscreen.GetCookies()?.Where(x => x?.Domain?.Contains(ClientName.ToLower()) ?? false)?.ToList() ?? new List<HttpCookie>();
                return httpCookies;
            }
        }
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
        /// <param name="local">ISO 15897</param>
        public void SetLanguage(string local)
        {
            Local = local;
        }

        public void SetForceAuth(bool forceAuth)
        {
            ForceAuth = forceAuth;
        }

        public virtual void Login()
        {
            throw new NotImplementedException();
        }

        public AccountInfos LoadCurrentUser()
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

        public virtual void SaveCurrentUser()
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

                    return Serialization.FromJsonFile<ObservableCollection<GameDlcOwned>>(FileGamesDlcsOwned);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }

            return null;
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
                bool IsOwned = CurrentGamesDlcsOwned.Count(x => x.Id.IsEqual(id)) != 0;
                return IsOwned;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return false;
            }
        }
        #endregion


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
    }
}
