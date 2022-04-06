using CommonPlayniteShared.PluginLibrary.GogLibrary.Models;
using CommonPlayniteShared.PluginLibrary.Services.GogLibrary;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsStores.Gog.Models;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace CommonPluginsStores.Gog
{
    public class GogApi : StoreApi
    {
        #region Url
        private const string UrlBase = @"https://www.gog.com";

        private const string UrlUserFriends = UrlBase + @"/u/{0}/friends";
        private const string UrlUserGames = UrlBase + @"/u/{0}/games/stats?page={1}";
        private const string UrlGogLang = UrlBase + @"/user/changeLanguage/{0}";

        private const string UrlFriends = @"https://embed.gog.com/users/info/{0}?expand=friendStatus";string url = @"http://api.gog.com/products/{0}?expand=description";
        #endregion


        #region Url API
        private const string UrlApiGamePlay = @"https://gameplay.gog.com";
        private const string UrlApi = @"http://api.gog.com";

        private const string UrlApiGamePlayUserAchievements = UrlApiGamePlay + @"/clients/{0}/users/{1}/achievements";
        private const string UrlApiGamePlayFriendAchievements = UrlApiGamePlay + @"/clients/{0}/users/{1}/friends_achievements_unlock_progresses";

        private const string UrlApiGameInfo = UrlApi + @"/products/{0}?expand=description";
        #endregion


        protected GogAccountClient _GogAPI;
        internal GogAccountClient GogAPI
        {
            get
            {
                if (_GogAPI == null)
                {
                    _GogAPI = new GogAccountClient(WebViewOffscreen);
                }
                return _GogAPI;
            }

            set
            {
                _GogAPI = value;
            }
        }


        private string UserId;
        private string UserName;


        public GogApi() : base("GOG")
        {

        }


        #region Cookies
        internal override List<HttpCookie> GetWebCookies()
        {
            List<HttpCookie> httpCookies = WebViewOffscreen.GetCookies()?.Where(x => x?.Domain?.Contains("gog") ?? false)?.ToList() ?? new List<HttpCookie>();
            return httpCookies;
        }
        #endregion


        #region Configuration
        protected override bool GetIsUserLoggedIn()
        {
            bool isLogged = GogAPI.GetIsUserLoggedIn();

            if (isLogged)
            {
                AccountBasicRespose AccountBasic = GogAPI.GetAccountInfo();
                AuthToken = new StoreToken
                {
                    Token = AccountBasic.accessToken
                };

                UserId = AccountBasic.userId;
                UserName = AccountBasic.username;
            }
            else
            {
                AuthToken = null;
            }

            return isLogged;
        }
        #endregion


        #region Current user
        protected override AccountInfos GetCurrentAccountInfos()
        {
            try
            {
                string WebData = Web.DownloadStringData(string.Format(UrlUserFriends, UserName), GetStoredCookies()).GetAwaiter().GetResult();
                string JsonDataString = Tools.GetJsonInString(WebData, "window.profilesData.currentUser = ", "window.profilesData.profileUser = ", "]}};");
                Serialization.TryFromJson(JsonDataString, out ProfileUser profileUser);

                if (profileUser != null)
                {
                    long.TryParse(profileUser.userId, out long UserId);
                    string Avatar = profileUser.avatar.Replace("\\", string.Empty);
                    string Pseudo = profileUser.username;
                    string Link = string.Format(UrlUserFriends, profileUser.username);

                    AccountInfos userInfos = new AccountInfos
                    {
                        UserId = UserId,
                        Avatar = Avatar,
                        Pseudo = Pseudo,
                        Link = Link,
                        IsCurrent = true
                    };
                    return userInfos;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }

        protected override ObservableCollection<AccountInfos> GetCurrentFriendsInfos()
        {
            try
            {
                string WebData = Web.DownloadStringData(string.Format(UrlUserFriends, UserName), GetStoredCookies()).GetAwaiter().GetResult();
                string JsonDataString = Tools.GetJsonInString(WebData, "window.profilesData.profileUserFriends = ", "window.profilesData.currentUserFriends = ", "}}];");
                Serialization.TryFromJson(JsonDataString, out List<ProfileUserFriends> profileUserFriends);

                ObservableCollection<AccountInfos> accountsInfos = new ObservableCollection<AccountInfos>();
                profileUserFriends.ForEach(x =>
                {
                    DateTime.TryParse(x.date_accepted.date, out DateTime DateAdded);
                    long.TryParse(x.user.id, out long UserId);
                    string Avatar = x.user.avatar.Replace("\\", string.Empty);
                    string Pseudo = x.user.username;
                    string Link = string.Format(UrlUserFriends, Pseudo);

                    AccountInfos userInfos = new AccountInfos
                    {
                        DateAdded = DateAdded,
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
                Common.LogError(ex, false);
            }

            return null;
        }
        #endregion


        #region User details
        public override ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos)
        {
            try
            {
                ObservableCollection<AccountGameInfos> accountGamesInfos = new ObservableCollection<AccountGameInfos>();
                for (int idx = 1; idx < 10; idx++)
                {
                    try
                    {
                        string WebData = Web.DownloadStringData(string.Format(UrlUserGames, accountInfos.Pseudo, idx), GetStoredCookies()).GetAwaiter().GetResult();
                        Serialization.TryFromJson(WebData, out ProfileGames profileGames);

                        if (profileGames == null)
                        {
                            break;
                        }

                        profileGames?._embedded?.items?.ForEach(x =>
                        {
                            string Id = x.game.id;
                            string Name = x.game.title;

                            bool IsCommun = false;
                            if (!accountInfos.IsCurrent)
                            {
                                IsCommun = CurrentGamesInfos?.Where(y => y.Id.IsEqual(Id))?.Count() != 0;
                            }

                            long Playtime = 0;
                            foreach (dynamic data in (dynamic)x.stats)
                            {
                                long.TryParse(((dynamic)x.stats)[data.Path]["playtime"].ToString(), out Playtime);
                                Playtime *= 60;

                                if (Playtime != 0)
                                {
                                    break;
                                }
                            }

                            string Link = UrlBase + x.game.url.Replace("\\", string.Empty);
                            ObservableCollection<GameAchievement> Achievements = GetAchievements(Id, accountInfos);

                            AccountGameInfos accountGameInfos = new AccountGameInfos
                            {
                                Id = Id,
                                Name = Name,
                                Link = Link,
                                IsCommun = IsCommun,
                                Achievements = Achievements,
                                Playtime = Playtime
                            };
                            accountGamesInfos.Add(accountGameInfos);
                        });
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                    }
                }

                return accountGamesInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }

        public override ObservableCollection<GameAchievement> GetAchievements(string Id, AccountInfos accountInfos)
        {
            try
            {
                string Url = string.Empty;
                //if (accountInfos.IsCurrent)
                //{
                //    Url = string.Format(UrlApiGamePlayUserAchievements, Id, UserId);
                //}
                //else
                //{
                //    Url = string.Format(UrlApiGamePlayFriendAchievements, Id, UserId);
                //}
                Url = string.Format(UrlApiGamePlayUserAchievements, Id, UserId);

                string UrlLang = string.Format(UrlGogLang, CodeLang.GetGogLang(Local).ToLower());
                string WebData = Web.DownloadStringData(Url, AuthToken.Token, UrlLang).GetAwaiter().GetResult();

                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
                if (!WebData.IsNullOrEmpty())
                {
                    dynamic resultObj = Serialization.FromJson<dynamic>(WebData);
                    try
                    {
                        dynamic resultItems = resultObj["items"];
                        if (resultItems.Count > 0)
                        {
                            for (int i = 0; i < resultItems.Count; i++)
                            {
                                GameAchievement gameAchievement = new GameAchievement
                                {
                                    Id = (string)resultItems[i]["achievement_key"],
                                    Name = (string)resultItems[i]["name"],
                                    Description = (string)resultItems[i]["description"],
                                    UrlUnlocked = (string)resultItems[i]["image_url_unlocked"],
                                    UrlLocked = (string)resultItems[i]["image_url_locked"],
                                    DateUnlocked = ((string)resultItems[i]["date_unlocked"] == null) ? default : (DateTime)resultItems[i]["date_unlocked"],
                                    Percent = (float)resultItems[i]["rarity"]
                                };
                                gameAchievements.Add(gameAchievement);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                    }
                }

                return gameAchievements;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }
        #endregion


        #region Game
        public static new GameInfos GetGameInfos(string Id, string Local)
        {
            try
            {
                string Url = string.Format(UrlApiGameInfo, Id);
                string UrlLang = string.Format(UrlGogLang, CodeLang.GetGogLang(Local).ToLower());
                string WebData = Web.DownloadStringDataWithUrlBefore(Url, UrlLang).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out ProductApiDetail productApiDetail);

                GameInfos gameInfos = new GameInfos
                {
                    Id = productApiDetail?.id.ToString(),
                    Name = productApiDetail?.title,
                    Link = productApiDetail?.links?.product_card,
                    Description = productApiDetail?.description?.full
                };
                return gameInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }
        #endregion


        #region GOG

        #endregion
    }
}
