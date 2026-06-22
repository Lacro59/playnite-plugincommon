using CommonPlayniteShared.Common;
using CommonPlayniteShared.PluginLibrary.OriginLibrary.Models;
using CommonPlayniteShared.PluginLibrary.OriginLibrary.Services;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsStores.Ea.Models.Query;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Ea
{
    /// <summary>
    /// Based on https://github.com/BellezaEmporium/galaxy-integration-ead
    /// </summary>
    public class EaApi : StoreApi
    {
        #region Urls API

        private static string UrlDropApi => @"https://drop-api.ea.com";
        private static string UrlGameData => UrlDropApi + @"/game/{0}?locale={1}";

        #endregion

        private string UrlGraphQL => @"https://service-aggregation-layer.juno.ea.com/graphql";

        private static readonly Lazy<OriginAccountClient> _originAPI = new Lazy<OriginAccountClient>(() => new OriginAccountClient(API.Instance.WebViews.CreateOffscreenView()));
        private static OriginAccountClient OriginAPI => _originAPI.Value;

        #region Paths

        private string AppsListPath { get; }
        private string PathOwnedGameProductsCache { get; }
        private string PathOfferIdToGameSlugCache { get; }

        #endregion

        public EaApi(string pluginName) : base(pluginName, ExternalPlugin.OriginLibrary, "EA")
        {
            // TODO TEMP
            AppsListPath = Path.Combine(PathStoresData, "EA_AppsList.json");
            FileSystem.DeleteFile(AppsListPath);

            PathOwnedGameProductsCache = Path.Combine(PathStoresData, "EA_OwnedGameProducts.json");
            PathOfferIdToGameSlugCache = Path.Combine(PathStoresData, "EA_OfferIdToGameSlug.json");
        }

        #region Configuration

        protected override bool GetIsUserLoggedIn()
        {
            bool isLogged = OriginAPI.GetIsUserLoggedIn();
            if (isLogged)
            {
                AuthTokenResponse accessToken = OriginAPI.GetAccessToken();
                StoreToken = new StoreToken
                { 
                    Token = accessToken.access_token,
                    Type = accessToken.token_type
                };

                ResponseIdentity responseIdentity = GetIdentity().GetAwaiter().GetResult();
                CurrentAccountInfos = new AccountInfos
                {
                    UserId    = responseIdentity.Data.Me.Player.Pd,
                    ClientId  = responseIdentity.Data.Me.Player.Psd,
                    Pseudo    = responseIdentity.Data.Me.Player.DisplayName,
                    Link      = string.Empty,
                    Avatar    = responseIdentity?.Data?.Me?.Player?.Avatar?.Medium?.Path ?? string.Empty,
                    IsPrivate = true,
                    IsCurrent = true
                };

                SaveCurrentUser();
                //_ = GetCurrentAccountInfos();

                LogInfo("logged");
            }
            else
            {
                StoreToken = null;
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
                    string avatar = x.Player?.Avatar?.Medium?.Path ?? string.Empty;
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

                            ObservableCollection<GameAchievement> achievements = GetAchievements(id, accountInfos);
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

        /// <summary>
        /// Get game informations.
        /// Override in derived classes if detailed game information is supported.
        /// </summary>
        /// <param name="id">Game identifier (Origin offer id from Playnite)</param>
        /// <param name="accountInfos">Account information</param>
        /// <returns>Game information object or null</returns>
        public override GameInfos GetGameInfos(string id, AccountInfos accountInfos)
        {
            try
            {
                string gameSlug = ResolveGameSlug(id);
                if (gameSlug.IsNullOrEmpty())
                {
                    return null;
                }

                Models.GameStoreDataResponse gameStoreDataResponse = GetStoreData(gameSlug);
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
                    Image = gameStoreDataResponse.HeroImage?.Ar16X9,
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

                    Dlcs.Add(dlcInfos);
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

        /// <summary>
        /// Resolves an Origin offer id to the EA drop-api game slug.
        /// </summary>
        /// <param name="offerId">Origin offer id (Playnite game id).</param>
        /// <returns>Game slug, or null if it cannot be resolved.</returns>
        private string ResolveGameSlug(string offerId)
        {
            if (offerId.IsNullOrEmpty())
            {
                return null;
            }

            Dictionary<string, string> cachedMappings = FileDataService.LoadData<Dictionary<string, string>>(PathOfferIdToGameSlugCache, 1440);
            if (cachedMappings == null)
            {
                cachedMappings = new Dictionary<string, string>();
            }

            string cachedSlug;
            if (cachedMappings.TryGetValue(offerId, out cachedSlug) && !cachedSlug.IsNullOrEmpty())
            {
                return cachedSlug;
            }

            string gameSlug = TryResolveGameSlugFromOwnedGameProducts(offerId);
            if (gameSlug.IsNullOrEmpty())
            {
                gameSlug = TryResolveGameSlugFromOriginEntitlements(offerId);
            }

            if (!gameSlug.IsNullOrEmpty())
            {
                cachedMappings[offerId] = gameSlug;
                FileDataService.SaveData(PathOfferIdToGameSlugCache, cachedMappings);
            }

            return gameSlug;
        }

        private string TryResolveGameSlugFromOwnedGameProducts(string offerId)
        {
            ResponseOwnedGameProducts responseOwnedGameProducts = GetOwnedGameProducts().GetAwaiter().GetResult();
            ItemOwnedGameProducts ownedItem = responseOwnedGameProducts?.Data?.Me?.OwnedGameProducts?.Items?
                .FirstOrDefault(x => x.OriginOfferId.IsEqual(offerId));

            return ownedItem?.Product?.GameSlug;
        }

        private string TryResolveGameSlugFromOriginEntitlements(string offerId)
        {
            if (!IsUserLoggedIn || StoreToken == null)
            {
                return null;
            }

            AuthTokenResponse token = new AuthTokenResponse
            {
                access_token = StoreToken.Token,
                token_type = StoreToken.Type
            };

            AccountInfoResponse accountInfo = OriginAPI.GetAccountInfo(token);
            if (accountInfo?.pid == null)
            {
                return null;
            }

            List<AccountEntitlementsResponse.Entitlement> entitlements = OriginAPI.GetOwnedGames(accountInfo.pid.pidId, token);
            AccountEntitlementsResponse.Entitlement entitlement = entitlements?
                .FirstOrDefault(x => x.offerId.IsEqual(offerId));

            return GetGameSlugFromOfferPath(entitlement?.offerPath);
        }

        /// <summary>
        /// Extracts the game slug from an Origin offer path (/franchise/game-slug/edition).
        /// </summary>
        private static string GetGameSlugFromOfferPath(string offerPath)
        {
            if (offerPath.IsNullOrEmpty())
            {
                return null;
            }

            Match match = Regex.Match(offerPath, @"\/[^\/]+\/([^\/]+)\/");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return null;
        }

        private Models.GameStoreDataResponse GetStoreData(string gameSlug)
        {
            string cachePath = Path.Combine(PathAppsData, $"{gameSlug}.json");
            Models.GameStoreDataResponse gameStoreDataResponse = FileDataService.LoadData<Models.GameStoreDataResponse>(cachePath, 1440);

            if (gameStoreDataResponse?.Name.IsNullOrEmpty() ?? true)
            {
                string lang = CodeLang.GetCountryFromFirst(Locale);
                string url = string.Format(UrlGameData, gameSlug, lang);
                string response = Task.Run(async () => await Web.DownloadStringData(url)).GetAwaiter().GetResult();
                Serialization.TryFromJson(response, out gameStoreDataResponse);
				FileDataService.SaveData(cachePath, gameStoreDataResponse);
            }

            return gameStoreDataResponse;
        }

        #endregion

        #region EA GraphQl

        private async Task<ResponseIdentity> GetIdentity()
        {
            QueryIdentity query = new QueryIdentity();
            ResponseIdentity data = await GetGraphQl<ResponseIdentity>(UrlGraphQL, query).ConfigureAwait(false);
            return data;
        }

        private async Task<ResponseFriends> GetFriends()
        {
            QueryFriends query = new QueryFriends();
            ResponseFriends data = await GetGraphQl<ResponseFriends>(UrlGraphQL, query).ConfigureAwait(false);
            return data;
        }

        private async Task<ResponseOwnedGameProducts> GetOwnedGameProducts()
        {
            ResponseOwnedGameProducts data = FileDataService.LoadData<ResponseOwnedGameProducts>(PathOwnedGameProductsCache, 10);
            if (data?.Data?.Me?.OwnedGameProducts?.Items != null && data?.Data?.Me?.OwnedGameProducts?.Items?.Count > 0)
            {
                return data;
            }

            QueryOwnedGameProducts query = new QueryOwnedGameProducts();
            query.variables.locale = CodeLang.GetCountryFromLast(Locale);
            data = await GetGraphQl<ResponseOwnedGameProducts>(UrlGraphQL, query).ConfigureAwait(false);
            FileDataService.SaveData(PathOwnedGameProductsCache, data);
            return data;
        }

        private async Task<ResponseAchievements> GetAchievements(string offerId, string playerPsd)
        {
            string cachePath = Path.Combine(PathAchievementsData, $"{offerId}-{playerPsd}.json");
            ResponseAchievements data = FileDataService.LoadData<ResponseAchievements>(cachePath, 10);
            if (data?.Data?.Achievements?.FirstOrDefault()?.AchievementsData != null && data?.Data?.Achievements?.FirstOrDefault()?.AchievementsData?.Count > 0)
            {
                return data;
            }

            QueryAchievements query = new QueryAchievements();
            query.variables.offerId = offerId;
            query.variables.playerPsd = playerPsd;
            query.variables.locale = CodeLang.GetCountryFromLast(Locale);
            data = await GetGraphQl<ResponseAchievements>(UrlGraphQL, query).ConfigureAwait(false);
            FileDataService.SaveData(cachePath, data);
            return data;
        }

        private async Task<ResponseRecentGames> GetRecentGames(List<string> gameSlugs)
        {
            QueryRecentGames query = new QueryRecentGames();
            query.variables.gameSlugs = gameSlugs;
            ResponseRecentGames data = await GetGraphQl<ResponseRecentGames>(UrlGraphQL, query).ConfigureAwait(false);
            return data;
        }

        #endregion

        private async Task<T> GetGraphQl<T>(string url, object query) where T : class
        {
            try
            {
                StringContent content = new StringContent(Serialization.ToJson(query), Encoding.UTF8, "application/json");
                string response = await Web.PostStringData(url, StoreToken?.Token, content).ConfigureAwait(false);
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