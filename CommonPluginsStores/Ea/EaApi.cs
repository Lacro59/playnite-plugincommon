using CommonPlayniteShared.Common.Web;
using CommonPlayniteShared.PluginLibrary.OriginLibrary.Models;
using CommonPlayniteShared.PluginLibrary.OriginLibrary.Services;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Ea.Models;
using CommonPluginsStores.Ea.Models.Query;
using CommonPluginsStores.Epic.Models.Query;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Ea
{
    public class EaApi : StoreApi
    {
        #region Urls API

        private static string UrlDropApi => @"https://drop-api.ea.com";
        private static string UrlGameData => UrlDropApi + @"/game/{0}?locale={1}";

        private static string UrlApi3 => @"https://api3.origin.com";

        private static string UrlApi3AppsList => UrlApi3 + @"/supercat/{0}/{1}/supercat-PCWIN_MAC-{0}-{1}.json.gz";

        #endregion

        private string UrlGraphQL => @"https://service-aggregation-layer.juno.ea.com/graphql";

        private static readonly Lazy<OriginAccountClient> _originAPI = new Lazy<OriginAccountClient>(() => new OriginAccountClient(API.Instance.WebViews.CreateOffscreenView()));
        private static OriginAccountClient OriginAPI => _originAPI.Value;

        #region Paths

        private string AppsListPath { get; }
        private string PathOwnedGameProductsCache { get; }

        #endregion

        public EaApi(string pluginName) : base(pluginName, ExternalPlugin.OriginLibrary, "EA")
        {
            AppsListPath = Path.Combine(PathStoresData, "EA_AppsList.json");

            PathOwnedGameProductsCache = Path.Combine(PathStoresData, "EA_OwnedGameProducts.json");
        }

        #region Configuration

        protected override bool GetIsUserLoggedIn()
        {
            bool isLogged = OriginAPI.GetIsUserLoggedIn();
            if (isLogged)
            {
                AuthTokenResponse accessToken = OriginAPI.GetAccessToken();
                AuthToken = new StoreToken
                { 
                    Token = accessToken.access_token,
                    Type = accessToken.token_type
                };

                ResponseIdentity responseIdentity = GetIdentity().GetAwaiter().GetResult();
                CurrentAccountInfos = new AccountInfos
                {
                    UserId = responseIdentity.Data.Me.Player.Pd,
                    ClientId = responseIdentity.Data.Me.Player.Psd,
                    Pseudo = responseIdentity.Data.Me.Player.DisplayName,
                    Link = string.Empty,
                    Avatar = responseIdentity.Data.Me.Player.Avatar.Medium.Path,
                    IsPrivate = true,
                    IsCurrent = true
                };
                SaveCurrentUser();
                _ = GetCurrentAccountInfos();

                Logger.Info($"{ClientName} logged");
            }
            else
            {
                AuthToken = null;
            }

            return isLogged;
        }

        #endregion

        #region Current user

        protected override ObservableCollection<AccountInfos> GetCurrentFriendsInfos()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                ObservableCollection<AccountInfos> accountsInfos = new ObservableCollection<AccountInfos>();

                ResponseFriends responseFriends = GetFriends().GetAwaiter().GetResult();
                responseFriends?.Data?.Me?.Friends?.Items?.ForEach(x =>
                {
                    string userId = x.Player.Pd;
                    string clientId = x.Player.Psd;
                    string avatar = x.Player.Avatar.Medium.Path;
                    string pseudo = x.Player.DisplayName;
                    string link = string.Empty;
                    DateTime? dateAdded = null;

                    AccountInfos userInfos = new AccountInfos
                    {
                        UserId = userId,
                        ClientId = clientId,
                        Avatar = avatar,
                        Pseudo = pseudo,
                        Link = link,
                        DateAdded = dateAdded
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

        // TODO Incomplete
        public override ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                ObservableCollection<AccountGameInfos> accountGamesInfos = new ObservableCollection<AccountGameInfos>();

                if (accountInfos.IsCurrent)
                {
                    ResponseOwnedGameProducts responseOwnedGameProducts = GetOwnedGameProducts().GetAwaiter().GetResult();
                    responseOwnedGameProducts.Data.Me.OwnedGameProducts.Items
                        .Where(x => x.Product?.BaseItem?.GameType == "BASE_GAME")
                        .ForEach(x =>
                        {
                            string id = x.OriginOfferId;
                            string name = x.Product.Name;

                            bool isCommun = false;
                            if (!accountInfos.IsCurrent)
                            {
                                isCommun = CurrentGamesInfos?.Where(y => y.Id.IsEqual(id))?.Count() != 0;
                            }

                            ObservableCollection<GameAchievement> achievements = achievements = GetAchievements(id, accountInfos);
                            ResponseRecentGames responseRecentGames = GetRecentGames(new List<string> { x.Product.GameSlug }).GetAwaiter().GetResult();

                            AccountGameInfos accountGameInfos = new AccountGameInfos
                            {
                                Id = id,
                                Name = name,
                                Link = string.Empty,
                                IsCommun = isCommun,
                                Achievements = achievements,
                                Playtime = responseRecentGames?.Data?.Me?.RecentGames?.Items?.FirstOrDefault(y => y.GameSlug.IsEqual(x.Product.GameSlug))?.TotalPlayTimeSeconds ?? 0,
                            };
                            accountGamesInfos.Add(accountGameInfos);
                        });
                }

                return accountGamesInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        // TODO Incomplete
        public override ObservableCollection<GameAchievement> GetAchievements(string id, AccountInfos accountInfos)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();

                ResponseAchievements responseAchievements = GetAchievements(id, accountInfos.ClientId).GetAwaiter().GetResult();
                if (responseAchievements?.Data?.Achievements?.Count > 0)
                {
                    responseAchievements.Data.Achievements.First().AchievementsData.ForEach(x =>
                    {
                        GameAchievement gameAchievement = new GameAchievement
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Description = x.Description,
                            UrlUnlocked = string.Empty, // No URL in this API
                            UrlLocked = string.Empty, // No URL in this API
                            DateUnlocked = x.AwardCount > 0 ? x.Date : default,
                            Percent = 100, // No percent in this API
                            GamerScore = 0 // No gamer score in this API
                        };
                        gameAchievements.Add(gameAchievement);
                    });
                }

                return gameAchievements;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        #endregion

        #region Game

        /// Get game informations.
        /// Override in derived classes if detailed game information is supported.
        /// </summary>
        /// <param name="id">Game identifier (gameSlug)</param>
        /// <param name="accountInfos">Account information</param>
        /// <returns>Game information object or null</returns>
        public override GameInfos GetGameInfos(string id, AccountInfos accountInfos)
        {
            try
            {
                Models.GameStoreDataResponse gameStoreDataResponse = GetStoreData(id);
                if (gameStoreDataResponse == null)
                {
                    return null;
                }

                GameInfos gameInfos = new GameInfos
                {
                    Id = string.Empty,
                    Id2 = string.Empty,
                    Name = gameStoreDataResponse.Name,
                    Link = gameStoreDataResponse.Logo?.TargetUrl,
                    Image = gameStoreDataResponse.HeroImage.Ar16X9,
                    Description = gameStoreDataResponse.ShortDescription
                };

                // DLC
                ObservableCollection<DlcInfos> Dlcs = new ObservableCollection<DlcInfos>();
                gameStoreDataResponse?.AddonsInfo?.Items?.ForEach(x =>
                {
                    DlcInfos dlcInfos = new DlcInfos
                    {
                        Id = string.Empty,
                        Id2 = x.Slug,
                        Name = x.Title.Replace("\n", string.Empty),
                        Link = string.Empty,
                        Image = x.PackArt?.Ar16X9,
                        Description = x.ShortDescription,
                        IsOwned = false,
                        PriceBase = x.Price?.DisplayTotal,
                        Price = x.Price?.DisplayTotalWithDiscount,
                        Released = x.ReleaseDate != null && x.ReleaseDate != string.Empty && DateTime.TryParse(x.ReleaseDate, out DateTime dt) ? dt : (DateTime?)null
                    };
                });
                gameInfos.Dlcs = Dlcs;

                return gameInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        #endregion

        #region Games owned

        protected override ObservableCollection<GameDlcOwned> GetGamesDlcsOwned()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                ObservableCollection<GameDlcOwned> GamesDlcsOwned = new ObservableCollection<GameDlcOwned>();

                ResponseOwnedGameProducts responseOwnedGameProducts = GetOwnedGameProducts().GetAwaiter().GetResult();
                responseOwnedGameProducts.Data.Me.OwnedGameProducts.Items
                    .Where(x => x.Product?.BaseItem?.GameType == "MICRO_CONTENT")
                    .ForEach(x =>
                    {
                        GamesDlcsOwned.Add(new GameDlcOwned { Id = x.OriginOfferId, Id2 = x.Product?.GameSlug });
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

        #region EA

        private Models.GameStoreDataResponse GetStoreData(string gameSlug)
        {
            string cachePath = Path.Combine(PathAppsData, $"{gameSlug}.json");
            Models.GameStoreDataResponse gameStoreDataResponse = LoadData<Models.GameStoreDataResponse>(cachePath, 1440);

            if (gameStoreDataResponse == null)
            {
                string url = string.Format(UrlGameData, gameSlug, CodeLang.GetEaLangCountry(Locale));
                string response = Web.DownloadStringData(url).GetAwaiter().GetResult();
                Serialization.TryFromJson(response, out gameStoreDataResponse);
            }

            return gameStoreDataResponse;
        }

        #endregion

        #region EA GraphQl

        private async Task<ResponseIdentity> GetIdentity()
        {
            QueryIdentity query = new QueryIdentity();
            ResponseIdentity data = await GetGraphQl<ResponseIdentity>(UrlGraphQL, query);
            return data;
        }

        private async Task<ResponseFriends> GetFriends()
        {
            QueryFriends query = new QueryFriends();
            ResponseFriends data = await GetGraphQl<ResponseFriends>(UrlGraphQL, query);
            return data;
        }

        private async Task<ResponseOwnedGameProducts> GetOwnedGameProducts()
        {
            ResponseOwnedGameProducts data = LoadData<ResponseOwnedGameProducts>(PathOwnedGameProductsCache, 10);
            if (data?.Data?.Me?.OwnedGameProducts?.Items?.Count > 0)
            {
                return data;
            }

            QueryOwnedGameProducts query = new QueryOwnedGameProducts();
            query.variables.locale = CodeLang.GetEaLangCountry(Locale);
            data = await GetGraphQl<ResponseOwnedGameProducts>(UrlGraphQL, query);
            return data;
        }

        private async Task<ResponseAchievements> GetAchievements(string offerId, string playerPsd)
        {
            string cachePath = Path.Combine(PathAchievementsData, $"{offerId}-{playerPsd}.json");
            ResponseAchievements data = LoadData<ResponseAchievements>(cachePath, 10);
            if (data?.Data?.Achievements?.FirstOrDefault()?.AchievementsData?.Count > 0)
            {
                return data;
            }

            QueryAchievements query = new QueryAchievements();
            query.variables.offerId = offerId;
            query.variables.playerPsd = playerPsd;
            query.variables.locale = CodeLang.GetEaLangCountry(Locale);
            data = await GetGraphQl<ResponseAchievements>(UrlGraphQL, query);
            return data;
        }

        private async Task<ResponseRecentGames> GetRecentGames(List<string> gameSlugs)
        {
            QueryRecentGames query = new QueryRecentGames();
            query.variables.gameSlugs = gameSlugs;
            ResponseRecentGames data = await GetGraphQl<ResponseRecentGames>(UrlGraphQL, query);
            return data;
        }

        #endregion

        private async Task<T> GetGraphQl<T>(string url, object query) where T : class
        {
            try
            {
                StringContent content = new StringContent(Serialization.ToJson(query), Encoding.UTF8, "application/json");
                string response = await Web.PostStringData(url, AuthToken?.Token, content);
                T data = Serialization.FromJson<T>(response);
                return data;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return default;
            }
        }
    }
}