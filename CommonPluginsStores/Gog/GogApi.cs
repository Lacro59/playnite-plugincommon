using CommonPlayniteShared.PluginLibrary.GogLibrary.Models;
using CommonPlayniteShared.PluginLibrary.Services.GogLibrary;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Gog.Models;
using CommonPluginsStores.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Gog
{
    // https://gogapidocs.readthedocs.io/en/latest/
    public class GogApi : StoreApi
    {
        #region Url
        private const string UrlBase = @"https://www.gog.com";

        private const string UrlUserData = UrlBase + @"/userData.json";
        private const string UrlUserOwned = "https://embed.gog.com/user/data/games";

        private const string UrlUserFriends = UrlBase + @"/u/{0}/friends";
        private const string UrlUserGames = UrlBase + @"/u/{0}/games/stats?page={1}";
        private const string UrlGogLang = UrlBase + @"/user/changeLanguage/{0}";

        private const string UrlGogGame = UrlBase + @"/game/{0}";

        private const string UrlFriends = @"https://embed.gog.com/users/info/{0}?expand=friendStatus";
        #endregion


        #region Url API
        private const string UrlApiGamePlay = @"https://gameplay.gog.com";
        private const string UrlApi = @"https://api.gog.com";

        private const string UrlApiGamePlayUserAchievements = UrlApiGamePlay + @"/clients/{0}/users/{1}/achievements";
        private const string UrlApiGamePlayFriendAchievements = UrlApiGamePlay + @"/clients/{0}/users/{1}/friends_achievements_unlock_progresses";

        private const string UrlApiGameInfo = UrlApi + @"/products/{0}?expand=description&locale={1}";

        private static string UrlApiPrice = UrlApi + @"/products/prices?ids={0}&countryCode={1}&currency={2}";
        #endregion


        protected static GogAccountClient _GogAPI;
        internal static GogAccountClient GogAPI
        {
            get
            {
                if (_GogAPI == null)
                {
                    _GogAPI = new GogAccountClient(WebViewOffscreen);
                }
                return _GogAPI;
            }

            set => _GogAPI = value;
        }


        private string UserId { get; set; }
        private string UserName { get; set; }

        private static StoreCurrency LocalCurrency { get; set; } = new StoreCurrency { country = "US", currency = "USD", symbol = "$" };


        public GogApi(string PluginName) : base(PluginName, ExternalPlugin.GogLibrary, "GOG")
        {

        }


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

        /// <summary>
        /// Set currency.
        /// </summary>
        /// <param name="currency"></param>
        public void SetCurrency(StoreCurrency currency)
        {
            LocalCurrency = currency;
        }
        #endregion


        #region Current user
        protected override AccountInfos GetCurrentAccountInfos()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

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
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        protected override ObservableCollection<AccountInfos> GetCurrentFriendsInfos()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                string WebData = Web.DownloadStringData(string.Format(UrlUserFriends, UserName), GetStoredCookies()).GetAwaiter().GetResult();
                string JsonDataString = Tools.GetJsonInString(WebData, "window.profilesData.profileUserFriends = ", "window.profilesData.currentUserFriends = ", "}}];");
                Serialization.TryFromJson(JsonDataString, out List<ProfileUserFriends> profileUserFriends);

                if (profileUserFriends == null)
                {
                    return null;
                }

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
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }
        #endregion


        #region User details
        public override ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                ObservableCollection<AccountGameInfos> accountGamesInfos = new ObservableCollection<AccountGameInfos>();
                for (int idx = 1; idx < 50; idx++)
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
                        Common.LogError(ex, false, true, PluginName);
                    }
                }

                return accountGamesInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override ObservableCollection<GameAchievement> GetAchievements(string Id, AccountInfos accountInfos)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

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
                        Common.LogError(ex, false, true, PluginName);
                    }
                }

                return gameAchievements;
            }
            catch (Exception ex)
            {
                // Reset login status when 401 error
                if (ex.Message.Contains("401"))
                {
                    ResetIsUserLoggedIn();
                }

                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override SourceLink GetAchievementsSourceLink(string Name, string Id, AccountInfos accountInfos)
        {
            return new SourceLink
            {
                GameName = Name,
                Name = ClientName,
                Url = $"https://www.gog.com/u/{UserName}/game/{Id}?sort=user_unlock_date&sort_user_id={accountInfos.UserId}"
            };
        }
        #endregion


        #region Game
        public override GameInfos GetGameInfos(string Id, AccountInfos accountInfos)
        {
            try
            {
                string Url = string.Format(UrlApiGameInfo, Id, CodeLang.GetGogLang(Local).ToLower());
                string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                Serialization.TryFromJson(WebData, out Models.ProductApiDetail productApiDetail);

                GameInfos gameInfos = new GameInfos
                {
                    Id = productApiDetail?.id.ToString(),
                    Name = productApiDetail?.title,
                    Link = productApiDetail?.links?.product_card,
                    Image = "https:" + productApiDetail?.images?.logo2x,
                    Description = productApiDetail?.description?.full
                };

                // DLC
                string stringDlcs = Serialization.ToJson(productApiDetail?.dlcs);
                if (!stringDlcs.IsNullOrEmpty() && !stringDlcs.IsEqual("[]"))
                {
                    GogDlcs DlcsData = Serialization.FromJson<GogDlcs>(stringDlcs);
                    ObservableCollection<DlcInfos> Dlcs = GetDlcInfos(DlcsData, accountInfos);
                    gameInfos.Dlcs = Dlcs;
                }

                return gameInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override ObservableCollection<DlcInfos> GetDlcInfos(string Id, AccountInfos accountInfos)
        {
            string Url = string.Format(UrlApiGameInfo, Id, CodeLang.GetGogLang(Local).ToLower());
            string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();
            Serialization.TryFromJson(WebData, out Models.ProductApiDetail productApiDetail);
            
            string stringDlcs = Serialization.ToJson(productApiDetail?.dlcs);
            if (!stringDlcs.IsNullOrEmpty() && !stringDlcs.IsEqual("[]"))
            {
                GogDlcs DlcsData = Serialization.FromJson<GogDlcs>(stringDlcs);
                return GetDlcInfos(DlcsData, accountInfos);
            }

            return null;
        }

        private ObservableCollection<DlcInfos> GetDlcInfos(GogDlcs DlcsData, AccountInfos accountInfos)
        {
            ObservableCollection<DlcInfos> Dlcs = new ObservableCollection<DlcInfos>();

            if (DlcsData?.products == null)
            {
                return Dlcs;
            }

            foreach (Product el in DlcsData?.products)
            {
                try
                {
                    string dataDlc = Web.DownloadStringData(string.Format(UrlApiGameInfo, el.id, CodeLang.GetGogLang(Local).ToLower())).GetAwaiter().GetResult();
                    if (!dataDlc.Contains("<!DOCTYPE html>", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Models.ProductApiDetail productApiDetailDlc = Serialization.FromJson<Models.ProductApiDetail>(dataDlc);

                        bool IsOwned = false;
                        if (accountInfos != null && accountInfos.IsCurrent)
                        {
                            IsOwned = DlcIsOwned(el.id.ToString());
                        }

                        DlcInfos dlc = new DlcInfos
                        {
                            Id = el.id.ToString(),
                            Name = productApiDetailDlc?.title,
                            Description = productApiDetailDlc?.description?.full,
                            Image = "https:" + productApiDetailDlc?.images?.logo2x,
                            Link = string.Format(UrlGogGame, productApiDetailDlc?.slug),
                            IsOwned = IsOwned
                        };

                        Dlcs.Add(dlc);
                    }
                    else
                    {
                        logger.Warn($"No dlc data for {el.id}");
                    }
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("404"))
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }
                    else
                    {
                        logger.Warn($"No dlc data for {el.id}");
                    }
                }
            }

            // Price
            if (Dlcs?.Count > 0)
            {
                try
                {
                    PriceData priceData = GetPrice(Dlcs.Select(x => x.Id).ToList(), Local, LocalCurrency);
                    string dataObjString = Serialization.ToJson(priceData?.dataObj["_embedded"]);
                    PriceResult priceResult = Serialization.FromJson<PriceResult>(dataObjString);

                    foreach (PriceItem el in priceResult?.items)
                    {
                        int idx = Dlcs.ToList().FindIndex(x => x.Id.IsEqual(el._embedded.product.id.ToString()));
                        if (idx > -1)
                        {
                            double.TryParse(el._embedded.prices[0].finalPrice.Replace(priceData.CodeCurrency, string.Empty, StringComparison.InvariantCultureIgnoreCase), out double Price);
                            double.TryParse(el._embedded.prices[0].basePrice.Replace(priceData.CodeCurrency, string.Empty, StringComparison.InvariantCultureIgnoreCase), out double PriceBase);

                            Price *= 0.01;
                            PriceBase *= 0.01;

                            Dlcs[idx].Price = Price + " " + priceData.SymbolCurrency;
                            Dlcs[idx].PriceBase = PriceBase + " " + priceData.SymbolCurrency;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }

            return Dlcs;
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
                string data = Web.DownloadStringData(UrlUserOwned, AuthToken.Token).GetAwaiter().GetResult();
                UserDataOwned UserDataOwned = Serialization.FromJson<UserDataOwned>(data);

                UserDataOwned?.owned?.ForEach(x =>
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


        #region GOG
        private PriceData GetPrice(List<string> ids, string Local, StoreCurrency LocalCurrency)
        {
            string joined = string.Join(",", ids);
            string UrlPrice = string.Format(UrlApiPrice, joined, LocalCurrency.country.ToUpper(), LocalCurrency.currency.ToUpper());
            string DataPrice = Web.DownloadStringData(UrlPrice).GetAwaiter().GetResult();

            Serialization.TryFromJson<dynamic>(DataPrice, out dynamic dataObj);

            string CodeCurrency = LocalCurrency.currency;
            string SymbolCurrency = LocalCurrency.symbol;

            // When no data or error, try with USD
            if (dataObj["message"] != null && ((string)dataObj["message"]).Contains("is not supported in", StringComparison.InvariantCultureIgnoreCase))
            {
                return GetPrice(ids, Local, new StoreCurrency { country = "US", currency = "USD", symbol = "$" });
            }

            if (dataObj["message"] != null)
            {
                logger.Info($"{dataObj["message"]}");
                return null;
            }

            return new PriceData
            {
                dataObj = dataObj,
                CodeCurrency = CodeCurrency,
                SymbolCurrency = SymbolCurrency
            };
        }

        public List<StoreCurrency> GetCurrencies()
        {
            try
            {
                if (IsUserLoggedIn)
                {
                    string webData = Web.DownloadStringData(UrlUserData).GetAwaiter().GetResult();
                    Serialization.TryFromJson<UserData>(webData, out UserData userData);

                    if (userData?.currencies != null)
                    {
                        return userData.currencies.Select(x => new StoreCurrency { country = userData.country, currency = x.code.ToUpper(), symbol = x.symbol }).ToList();
                    }                    
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, ClientName);
            }

            logger.Warn("Used USD only");
            return new List<StoreCurrency>
            {
                new StoreCurrency { country = "US", currency = "USD", symbol = "$" }
            };
        }
        #endregion
    }
}
