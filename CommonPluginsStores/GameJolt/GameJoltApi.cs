using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.GameJolt.Models;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.GameJolt
{
    public class GameJoltApi : StoreApi
    {
        #region Urls
        private static string UrlBase => @"https://gamejolt.com";
        private static string UrlSiteApi => UrlBase + "/site-api";

        private static string UrlProfil => UrlSiteApi + "/web/profile/{0}";
        private static string UrlTrophiesGame => UrlSiteApi + "/web/discover/games/trophies/{0}";
        private static string UrlProfilTrophiesGame => UrlSiteApi + "/web/profile/trophies/game/{0}/{1}";
        private static string UrlTrophiesOverview => UrlSiteApi + "/web/profile/trophies/overview/{0}";

        private static string UrlLogin => UrlBase + "/login";
        private static string UrlUserGameAchievements => UrlBase + "/{0}/trophies/game/{1}";
        #endregion


        public GameJoltApi(string pluginName, ExternalPlugin pluginLibrary) : base(pluginName, pluginLibrary, "Game Jolt")
        {
            CookiesDomains = new List<string> { "gamejolt.com", ".gamejolt.com" };
        }

        #region Configuration
        protected override bool GetIsUserLoggedIn()
        {
            if (CurrentAccountInfos?.Pseudo?.IsNullOrEmpty() ?? true)
            {
                return false;
            }

            Profile profile = GetUser(CurrentAccountInfos?.Pseudo);
            return profile?.User != null;
        }

        public override void Login()
        {
            try
            {
                ResetIsUserLoggedIn();
                GameJoltLogin();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, false, PluginName);
            }
        }
        #endregion

        #region Current user
        protected override AccountInfos GetCurrentAccountInfos()
        {
            AccountInfos accountInfos = LoadCurrentUser();
            if (!accountInfos?.Pseudo?.IsNullOrEmpty() ?? false)
            {
                Profile profile = GetUser(accountInfos.Pseudo);
                profile = profile ?? GetUser(accountInfos.Pseudo);
                accountInfos = new AccountInfos
                {
                    UserId = profile.Payload.User.Id.ToString(),
                    Pseudo = profile.Payload.User.Username,
                    Link = UrlBase + FormatUser(profile.Payload.User.Username),
                    Avatar = profile.Payload.User.ImgAvatar,
                    IsPrivate = true,
                    IsCurrent = true
                };
                return accountInfos;
            }
            return new AccountInfos { IsCurrent = true };
        }
        #endregion

        #region User details
        public override ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos)
        {
            throw new NotImplementedException();
        }

        public override ObservableCollection<GameAchievement> GetAchievements(string id, AccountInfos accountInfos)
        {
            try
            {
                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
                Tuple<string, ObservableCollection<GameAchievement>> data = GetAchievementsSchema(id);

                if (data?.Item2?.Count() == 0)
                {
                    return gameAchievements;
                }

                gameAchievements = data.Item2;

                if (accountInfos?.Pseudo.IsNullOrEmpty() ?? true)
                {
                    return gameAchievements;
                }

                string url = string.Format(UrlProfilTrophiesGame, FormatUser(accountInfos.Pseudo), id);
                var jsonData = Web.DownloadJsonDataWebView(url, GetStoredCookies(), true, CookiesDomains).GetAwaiter().GetResult();
                string response = jsonData.Item1;

                if (response.IsNullOrEmpty())
                {
                    Thread.Sleep(1000);
                    jsonData = Web.DownloadJsonDataWebView(url, GetStoredCookies(), true, CookiesDomains).GetAwaiter().GetResult();
                    response = jsonData.Item1;
                }
                _ = Serialization.TryFromJson(response, out ProfileTrophiesGame profileTrophiesGame, out Exception ex);
                if (ex != null)
                {
                    Common.LogError(ex, false, true, PluginName);
                    return null;
                }

                SetStoredCookies(jsonData.Item2);

                profileTrophiesGame?.Payload?.Trophies?
                    .Where(x => x?.GameId.ToString().IsEqual(id) ?? false)?
                    .ForEach(x =>
                    {
                        GameAchievement item = gameAchievements.FirstOrDefault(y => y.Id.IsEqual(x.GameTrophyId.ToString()));
                        if (item != null)
                        {
                            item.DateUnlocked = x.LoggedOn == null ? new DateTime(1982, 15, 12, 0, 0, 0, 0) : new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((long)x.LoggedOn);
                            item.UrlUnlocked = x.GameTrophy.ImgThumbnail;
                        }
                    });

                return gameAchievements;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override SourceLink GetAchievementsSourceLink(string name, string id, AccountInfos accountInfos)
        {
            return new SourceLink
            {
                GameName = name,
                Name = ClientName,
                Url = string.Format(UrlUserGameAchievements, FormatUser(accountInfos.Pseudo), id)
            };
        }
        #endregion

        #region Game
        public override Tuple<string, ObservableCollection<GameAchievement>> GetAchievementsSchema(string id)
        {
            string cachePath = Path.Combine(PathAchievementsData, $"{id}.json");
            Tuple<string, ObservableCollection<GameAchievement>> data = LoadData<Tuple<string, ObservableCollection<GameAchievement>>>(cachePath, 1440);

            if (data == null || !(data.Item2?.Any() ?? false))
            {
                string url = string.Format(UrlTrophiesGame, id);
                var jsonData = Web.DownloadJsonDataWebView(url, GetStoredCookies(), true, CookiesDomains).GetAwaiter().GetResult();
                string response = jsonData.Item1;

                if (response.IsNullOrEmpty())
                {
                    Thread.Sleep(1000);
                    jsonData = Web.DownloadJsonDataWebView(url, GetStoredCookies(), true, CookiesDomains).GetAwaiter().GetResult();
                    response = jsonData.Item1;
                }
                _ = Serialization.TryFromJson(response, out TrophiesGame trophiesGame, out Exception ex);
                if (ex != null)
                {
                    Common.LogError(ex, false, true, PluginName);
                    return null;
                }

                SetStoredCookies(jsonData.Item2);

                ObservableCollection<GameAchievement> gameAchievements = trophiesGame?.Payload?.Trophies?
                    .Where(x => x?.GameId.ToString().IsEqual(id) ?? false)?
                    .Select(x => new GameAchievement
                    {
                        Id = x.Id.ToString(),
                        Name = x.Title.Trim(),
                        Description = x.Description.Trim(),
                        UrlUnlocked = x.ImgThumbnail,
                        UrlLocked = x.ImgThumbnail,
                        Percent = 100,
                        GamerScore = x.Experience,
                        IsHidden = x.Secret
                    }).ToObservable() ?? new ObservableCollection<GameAchievement>();

                data = new Tuple<string, ObservableCollection<GameAchievement>>(id, gameAchievements);
                FileSystem.WriteStringToFile(cachePath, Serialization.ToJson(data));
            }

            return data;
        }
        #endregion

        #region Games owned

        #endregion

        #region Game Jolt
        private string FormatUser(string pseudo)
        {
            string user = pseudo;
            if (!user?.StartsWith("@") ?? false)
            {
                user = "@" + user;
            }
            return user;
        }

        private void GameJoltLogin()
        {
            using (IWebView webView = API.Instance.WebViews.CreateView(new WebViewSettings
            {
                WindowWidth = 580,
                WindowHeight = 700,
                // This is needed otherwise captcha won't pass
                UserAgent = Web.UserAgent
            }))
            {
                bool isRedirect = false;
                webView.LoadingChanged += async (s, e) =>
                {
                    string url = webView.GetCurrentAddress();
                    if (url.IsEqual(UrlBase) || url.IsEqual(UrlBase + "/"))
                    {
                        _ = await Task.Run(() =>
                        {
                            Thread.Sleep(3000);
                            return true;
                        });

                        if (!isRedirect)
                        {
                            isRedirect = true;
                            string src = webView.GetPageSource();

                            Match match = Regex.Match(src, @"<div[^>]*class=""-username"">(.*?)<\/div>");
                            if (match.Success && match.Groups.Count > 1)
                            {
                                _ = SetStoredCookies(GetWebCookies(true));

                                string user = match.Groups[1].Value.Replace("Hey @", string.Empty, StringComparison.InvariantCultureIgnoreCase);
                                Profile profile = GetUser(user);
                                if (profile != null)
                                {
                                    CurrentAccountInfos = new AccountInfos
                                    {
                                        UserId = profile.Payload.User.Id.ToString(),
                                        Pseudo = profile.Payload.User.Username,
                                        Link = string.Format(UrlProfil, profile.Payload.User.Username),
                                        Avatar = profile.Payload.User.ImgAvatar,
                                        IsPrivate = true,
                                        IsCurrent = true
                                    };

                                    SaveCurrentUser();
                                    //_ = GetCurrentAccountInfos();

                                    Logger.Info($"{ClientName} logged");

                                    webView.NavigateAndWait(CurrentAccountInfos.Link);
                                }
                            }

                            webView.Close();
                        }
                    }
                };

                CookiesDomains.ForEach(x => { webView.DeleteDomainCookies(x); });
                webView.Navigate(UrlLogin);
                _ = webView.OpenDialog();
            }
        }

        private Profile GetUser(string pseudo)
        {
            string url = string.Format(UrlProfil, FormatUser(pseudo));
            var data = Web.DownloadJsonDataWebView(url, GetStoredCookies(), true, CookiesDomains).GetAwaiter().GetResult();

            _ = Serialization.TryFromJson(data.Item1, out Profile profile, out Exception ex);
            if (ex != null)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }

            SetStoredCookies(data.Item2);
            return profile;
        }
        #endregion
    }
}
