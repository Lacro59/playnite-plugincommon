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
using System.Globalization;
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
        private string PathCatalogSlugsCache { get; }

        #endregion

        /// <summary>
        /// Catalog slug index cache TTL in minutes (7 days).
        /// </summary>
        private const int CatalogSlugsCacheMinutes = 10080;

        /// <summary>
        /// Max games(slugs) ids per anonymous GraphQL request.
        /// </summary>
        private const int GamesBySlugsBatchSize = 50;

        public EaApi(string pluginName) : base(pluginName, ExternalPlugin.OriginLibrary, "EA")
        {
            // Legacy apps-list cache (obsolete); catalog search uses EA_CatalogSlugs.json instead.
            AppsListPath = Path.Combine(PathStoresData, "EA_AppsList.json");
            FileSystem.DeleteFile(AppsListPath);

            PathOwnedGameProductsCache = Path.Combine(PathStoresData, "EA_OwnedGameProducts.json");
            PathOfferIdToGameSlugCache = Path.Combine(PathStoresData, "EA_OfferIdToGameSlug.json");
            PathCatalogSlugsCache = Path.Combine(PathStoresData, "EA_CatalogSlugs.json");
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

        // Account games list is partial (current user / BASE_GAME only); see .tasks/TODO-FIXME.md.
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

        // Achievement payload lacks unlock URLs / percent / gamerscore; see .tasks/TODO-FIXME.md.
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
        /// Gets store metadata for an Origin offer id without requiring a logged-in EA account.
        /// Resolves the drop-api slug via public Juno <c>gameProducts</c> (with account fallbacks), then loads description from drop-api.
        /// </summary>
        /// <param name="id">Game identifier (Origin offer id from Playnite).</param>
        /// <param name="accountInfos">Unused for the public store path; kept for <see cref="StoreApi"/> signature compatibility.</param>
        /// <returns>Game information object, or null when the slug or store payload cannot be resolved.</returns>
        public override GameInfos GetGameInfos(string id, AccountInfos accountInfos)
        {
            try
            {
                if (id.IsNullOrEmpty())
                {
                    LogWarn("GetGameInfos: offer id is empty; returning null description.");
                    return null;
                }

                string gameSlug = ResolveGameSlug(id);
                if (gameSlug.IsNullOrEmpty())
                {
                    LogWarn($"GetGameInfos: no game slug for offer id '{id}'; returning null description.");
                    return null;
                }

                Models.GameStoreDataResponse gameStoreDataResponse = GetStoreData(gameSlug);
                if (gameStoreDataResponse == null || gameStoreDataResponse.Name.IsNullOrEmpty())
                {
                    LogWarn($"GetGameInfos: drop-api returned no store data for slug '{gameSlug}' (offer id '{id}'); returning null description.");
                    return null;
                }

                GameInfos gameInfos = new GameInfos
                {
                    Id = id,
                    Id2 = gameSlug,
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

                Common.LogDebug(FormatLogMessage($"GetGameInfos: ok offerId='{id}', slug='{gameSlug}', descriptionLength={(gameInfos.Description ?? string.Empty).Length}"));
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
        /// Tries the public Juno <c>gameProducts</c> catalog first, then account-owned data when logged in.
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

            // Public catalog (no account) — preferred for MetadataLocal.
            string gameSlug = TryResolveGameSlugFromGameProducts(offerId);

            // Account fallbacks for CheckDLC / logged-in plugins.
            if (gameSlug.IsNullOrEmpty() && IsUserLoggedIn)
            {
                gameSlug = TryResolveGameSlugFromOwnedGameProducts(offerId);
                if (gameSlug.IsNullOrEmpty())
                {
                    gameSlug = TryResolveGameSlugFromOriginEntitlements(offerId);
                }
            }

            if (!gameSlug.IsNullOrEmpty())
            {
                cachedMappings[offerId] = gameSlug;
                FileDataService.SaveData(PathOfferIdToGameSlugCache, cachedMappings);
            }

            return gameSlug;
        }

        /// <summary>
        /// Resolves a game slug via public Juno <c>gameProducts(offerIds)</c> (no Bearer).
        /// </summary>
        /// <param name="offerId">Origin offer id.</param>
        /// <returns>Game slug, or null if not found.</returns>
        private string TryResolveGameSlugFromGameProducts(string offerId)
        {
            try
            {
                ResponseGameProducts response = GetGameProducts(new List<string> { offerId }).GetAwaiter().GetResult();
                GameProductItem item = response?.Data?.GameProducts?.Items?
                    .FirstOrDefault(x => x.OriginOfferId.IsEqual(offerId));

                if (item == null)
                {
                    // Some catalog rows key the product id to the offer id.
                    item = response?.Data?.GameProducts?.Items?
                        .FirstOrDefault(x => offerId.IsEqual(x.Id));
                }

                return item?.GameSlug;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }

        private string TryResolveGameSlugFromOwnedGameProducts(string offerId)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                ResponseOwnedGameProducts responseOwnedGameProducts = GetOwnedGameProducts().GetAwaiter().GetResult();
                ItemOwnedGameProducts ownedItem = responseOwnedGameProducts?.Data?.Me?.OwnedGameProducts?.Items?
                    .FirstOrDefault(x => x.OriginOfferId.IsEqual(offerId));

                return ownedItem?.Product?.GameSlug;
            }
            catch (Exception ex)
            {
                LogWarn($"TryResolveGameSlugFromOwnedGameProducts: failed for '{offerId}' ({ex.GetType().Name}: {ex.Message}); returning null.");
                return null;
            }
        }

        private string TryResolveGameSlugFromOriginEntitlements(string offerId)
        {
            if (!IsUserLoggedIn || StoreToken == null)
            {
                return null;
            }

            try
            {
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
            catch (Exception ex)
            {
                // Legacy api1.origin.com entitlements often 404 post-Origin; do not bubble to GetGameInfos.
                LogWarn($"TryResolveGameSlugFromOriginEntitlements: failed for '{offerId}' ({ex.GetType().Name}: {ex.Message}); returning null.");
                return null;
            }
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
            string storeLang = CodeLang.GetCountryFromFirst(Locale);
            string cachePath = GetStoreDataCachePath(gameSlug, storeLang);
            Models.GameStoreDataResponse gameStoreDataResponse = FileDataService.LoadData<Models.GameStoreDataResponse>(cachePath, 1440);

            if (!(gameStoreDataResponse?.Name.IsNullOrEmpty() ?? true))
            {
                Common.LogDebug(FormatLogMessage(
                    $"GetStoreData: cache hit slug='{gameSlug}', playniteLang='{Locale}', storeLang='{storeLang}', path='{cachePath}'"));
                return gameStoreDataResponse;
            }

            try
            {
                string url = string.Format(UrlGameData, gameSlug, storeLang);
                Common.LogDebug(FormatLogMessage(
                    $"GetStoreData: cache miss slug='{gameSlug}', playniteLang='{Locale}', storeLang='{storeLang}', path='{cachePath}'"));
                string response = Task.Run(async () => await Web.DownloadStringDataWithGz(url)).GetAwaiter().GetResult();
                if (response.IsNullOrEmpty())
                {
                    // drop-api returns HTTP 204 No Content for some delisted / legacy slugs.
                    LogWarn($"GetStoreData: empty response for slug '{gameSlug}' (locale '{storeLang}', often HTTP 204); not caching.");
                    return null;
                }

                bool parsed = Serialization.TryFromJson(response, out gameStoreDataResponse);
                if (!parsed || gameStoreDataResponse?.Name.IsNullOrEmpty() == true)
                {
                    LogWarn($"GetStoreData: deserialize failed or missing name for slug '{gameSlug}' (locale '{storeLang}', responseLength={response.Length}, tryFromJson={parsed}); not caching.");
                    return null;
                }

                FileDataService.SaveData(cachePath, gameStoreDataResponse);
                Common.LogDebug(FormatLogMessage(
                    $"GetStoreData: saved slug='{gameSlug}', playniteLang='{Locale}', storeLang='{storeLang}', path='{cachePath}', name='{gameStoreDataResponse.Name}', descriptionLength={(gameStoreDataResponse.ShortDescription ?? string.Empty).Length}"));
            }
            catch (Exception ex)
            {
                LogWarn($"GetStoreData: failed for slug '{gameSlug}' ({ex.GetType().Name}: {ex.Message}); returning null.");
                return null;
            }

            return gameStoreDataResponse;
        }

        /// <summary>
        /// Resolves the on-disk Apps cache path for EA store drop-api data.
        /// Legacy <c>{slug}.json</c> is English-only (<c>en</c>); other locales use <c>{slug}_{storeLang}.json</c>.
        /// </summary>
        /// <param name="gameSlug">EA store game slug.</param>
        /// <param name="storeLang">EA locale from <see cref="CodeLang.GetCountryFromFirst"/>.</param>
        /// <returns>Absolute path under <see cref="StoreApi.PathAppsData"/>.</returns>
        private string GetStoreDataCachePath(string gameSlug, string storeLang)
        {
            if (storeLang.Equals("en", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(PathAppsData, $"{gameSlug}.json");
            }

            return Path.Combine(PathAppsData, $"{gameSlug}_{storeLang}.json");
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

        /// <summary>
        /// Resolves catalog products for Origin offer ids via public Juno <c>gameProducts</c> (no Bearer).
        /// </summary>
        /// <param name="offerIds">Origin offer identifiers (Playnite GameId values).</param>
        /// <returns>Deserialized response, or null on failure.</returns>
        private async Task<ResponseGameProducts> GetGameProducts(List<string> offerIds)
        {
            if (offerIds == null || offerIds.Count == 0)
            {
                return null;
            }

            QueryGameProducts query = new QueryGameProducts();
            query.variables.offerIds = offerIds;
            query.variables.locale = "DEFAULT";
            return await GetGraphQl<ResponseGameProducts>(UrlGraphQL, query, forceAnonymous: true).ConfigureAwait(false);
        }

        private async Task<ResponseGameSearch> GetGameSearchCatalog(int limit = 9999)
        {
            QueryGameSearch query = new QueryGameSearch();
            query.variables.limit = limit;
            return await GetGraphQl<ResponseGameSearch>(UrlGraphQL, query, forceAnonymous: true).ConfigureAwait(false);
        }

        private async Task<ResponseGamesBySlugs> GetGamesBySlugs(List<string> slugs)
        {
            if (slugs == null || slugs.Count == 0)
            {
                return null;
            }

            QueryGamesBySlugs query = new QueryGamesBySlugs();
            query.variables.slugs = slugs;
            query.variables.locale = "DEFAULT";
            return await GetGraphQl<ResponseGamesBySlugs>(UrlGraphQL, query, forceAnonymous: true).ConfigureAwait(false);
        }

        #endregion

        #region Public catalog search

        /// <summary>
        /// Searches the public EA catalog by name using a cached <c>gameSearch</c> slug index and <c>games(slugs)</c> enrichment (no login).
        /// </summary>
        /// <param name="searchTerm">User search term.</param>
        /// <param name="maxResults">Maximum number of candidates to return.</param>
        /// <returns>Matching games (Id = Origin offer id, Id2 = slug), or an empty list.</returns>
        public List<GameInfos> SearchGames(string searchTerm, int maxResults = 20)
        {
            List<GameInfos> results = new List<GameInfos>();
            if (searchTerm.IsNullOrEmpty() || maxResults <= 0)
            {
                Common.LogDebug(FormatLogMessage($"SearchGames: skipped (term empty or maxResults={maxResults})."));
                return results;
            }

            try
            {
                Common.LogDebug(FormatLogMessage($"SearchGames: term='{searchTerm}', maxResults={maxResults}"));

                List<string> catalogSlugs = GetCatalogSlugs();
                if (catalogSlugs == null || catalogSlugs.Count == 0)
                {
                    LogWarn("SearchGames: EA catalog slug index is empty.");
                    return results;
                }

                Common.LogDebug(FormatLogMessage($"SearchGames: catalogSlugCount={catalogSlugs.Count}"));

                string normalizedTerm = NormalizeSearchText(searchTerm);
                if (normalizedTerm.IsNullOrEmpty())
                {
                    Common.LogDebug(FormatLogMessage("SearchGames: normalized term is empty."));
                    return results;
                }

                var scored = catalogSlugs
                    .Select(slug => new { Slug = slug, Score = ScoreCatalogMatch(slug, normalizedTerm) })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.Slug.Length)
                    .Take(maxResults)
                    .ToList();

                List<string> matchedSlugs = scored.Select(x => x.Slug).ToList();
                string topPreview = string.Join(", ", scored.Take(5).Select(x => $"{x.Slug}:{x.Score}"));
                Common.LogDebug(FormatLogMessage($"SearchGames: normalizedTerm='{normalizedTerm}', matchedSlugCount={matchedSlugs.Count}, top=[{topPreview}]"));

                if (matchedSlugs.Count == 0)
                {
                    Common.LogDebug(FormatLogMessage("SearchGames: no slug matches."));
                    return results;
                }

                Dictionary<string, GameProductItem> productsBySlug = EnrichCatalogProducts(matchedSlugs);
                Common.LogDebug(FormatLogMessage($"SearchGames: enrichRequested={matchedSlugs.Count}, enrichResolved={productsBySlug.Count}"));

                int skippedNoOfferId = 0;
                foreach (string slug in matchedSlugs)
                {
                    GameProductItem product = null;
                    productsBySlug.TryGetValue(slug, out product);

                    string offerId = product?.OriginOfferId;
                    string name = product?.Name;
                    if (!name.IsNullOrEmpty())
                    {
                        name = name.Replace("\n", string.Empty).Trim();
                    }

                    if (name.IsNullOrEmpty())
                    {
                        name = FormatSlugAsTitle(slug);
                    }

                    if (offerId.IsNullOrEmpty())
                    {
                        skippedNoOfferId++;
                        LogWarn($"SearchGames: no originOfferId for slug '{slug}'; skipping candidate.");
                        continue;
                    }

                    string image = null;
                    Models.GameStoreDataResponse storeData = GetStoreData(slug);
                    if (storeData != null)
                    {
                        image = storeData.PackArt?.Ar16X9 ?? storeData.HeroImage?.Ar16X9;
                        if (!storeData.Name.IsNullOrEmpty())
                        {
                            name = storeData.Name;
                        }
                    }

                    results.Add(new GameInfos
                    {
                        Id = offerId,
                        Id2 = slug,
                        Name = name,
                        Image = image,
                        Link = storeData?.Logo?.TargetUrl
                    });
                }

                Common.LogDebug(FormatLogMessage($"SearchGames: resultCount={results.Count}, skippedNoOfferId={skippedNoOfferId}"));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return results;
        }

        /// <summary>
        /// Loads (or refreshes) the cached list of public catalog slugs from Juno <c>gameSearch</c>.
        /// </summary>
        /// <returns>Slug list, or empty on failure.</returns>
        private List<string> GetCatalogSlugs()
        {
            List<string> cached = FileDataService.LoadData<List<string>>(PathCatalogSlugsCache, CatalogSlugsCacheMinutes);
            if (cached != null && cached.Count > 0)
            {
                Common.LogDebug(FormatLogMessage($"SearchGames: catalog cache hit ({cached.Count} slugs, ttlMinutes={CatalogSlugsCacheMinutes})."));
                return cached;
            }

            Common.LogDebug(FormatLogMessage("SearchGames: catalog cache miss; fetching gameSearch."));
            ResponseGameSearch response = GetGameSearchCatalog().GetAwaiter().GetResult();
            List<string> slugs = response?.Data?.GameSearch?.Items?
                .Select(x => x.Slug)
                .Where(x => !x.IsNullOrEmpty())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (slugs.Count > 0)
            {
                FileDataService.SaveData(PathCatalogSlugsCache, slugs);
                LogInfo($"SearchGames: catalog index refreshed ({slugs.Count} slugs).");
            }
            else
            {
                Common.LogDebug(FormatLogMessage("SearchGames: gameSearch returned no slugs."));
            }

            return slugs;
        }

        /// <summary>
        /// Resolves display products for catalog slugs via public <c>games(slugs)</c> (batched).
        /// </summary>
        private Dictionary<string, GameProductItem> EnrichCatalogProducts(List<string> slugs)
        {
            Dictionary<string, GameProductItem> map = new Dictionary<string, GameProductItem>(StringComparer.OrdinalIgnoreCase);
            if (slugs == null || slugs.Count == 0)
            {
                return map;
            }

            for (int offset = 0; offset < slugs.Count; offset += GamesBySlugsBatchSize)
            {
                List<string> batch = slugs.Skip(offset).Take(GamesBySlugsBatchSize).ToList();
                ResponseGamesBySlugs response = GetGamesBySlugs(batch).GetAwaiter().GetResult();
                List<GameBySlugItem> items = response?.Data?.Games?.Items;
                if (items == null)
                {
                    continue;
                }

                foreach (GameBySlugItem game in items)
                {
                    if (game?.Slug.IsNullOrEmpty() ?? true)
                    {
                        continue;
                    }

                    GameProductItem best = SelectPreferredProduct(game);
                    if (best != null)
                    {
                        map[game.Slug] = best;
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Picks a representative SKU for multi-search (prefer BASE_GAME / shorter title).
        /// </summary>
        private static GameProductItem SelectPreferredProduct(GameBySlugItem game)
        {
            List<GameProductItem> products = game?.Products?.Items?
                .Where(x => x != null && (!x.OriginOfferId.IsNullOrEmpty() || !x.Name.IsNullOrEmpty()))
                .ToList();
            if (products == null || products.Count == 0)
            {
                return null;
            }

            GameProductItem baseGame = products
                .Where(x => x.BaseItem?.GameType != null && x.BaseItem.GameType.IsEqual("BASE_GAME"))
                .OrderBy(x => (x.Name ?? string.Empty).Length)
                .FirstOrDefault();
            if (baseGame != null)
            {
                return baseGame;
            }

            return products.OrderBy(x => (x.Name ?? string.Empty).Length).FirstOrDefault();
        }

        /// <summary>
        /// Scores how well a catalog slug matches a normalized search term (higher is better; 0 = no match).
        /// Digit-only tokens must match whole slug tokens (avoids "4" matching inside "14" / "2042").
        /// </summary>
        private static int ScoreCatalogMatch(string slug, string normalizedTerm)
        {
            if (slug.IsNullOrEmpty() || normalizedTerm.IsNullOrEmpty())
            {
                return 0;
            }

            string normalizedSlug = NormalizeSearchText(slug);
            if (normalizedSlug.IsNullOrEmpty())
            {
                return 0;
            }

            if (normalizedSlug.IsEqual(normalizedTerm))
            {
                return 1000;
            }

            if (normalizedSlug.StartsWith(normalizedTerm, StringComparison.OrdinalIgnoreCase))
            {
                return 800 - Math.Min(200, normalizedSlug.Length - normalizedTerm.Length);
            }

            if (normalizedSlug.Contains(normalizedTerm))
            {
                return 600 - Math.Min(200, normalizedSlug.Length - normalizedTerm.Length);
            }

            string[] termTokens = normalizedTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] slugTokens = normalizedSlug.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (termTokens.Length == 0 || slugTokens.Length == 0)
            {
                return 0;
            }

            string[] strongTokens = termTokens.Where(t => !IsDigitsOnlyToken(t)).ToArray();
            string[] weakTokens = termTokens.Where(t => IsDigitsOnlyToken(t)).ToArray();

            if (strongTokens.Length > 0)
            {
                if (!strongTokens.All(token => SlugHasWholeToken(slugTokens, token)))
                {
                    return 0;
                }

                int weakHits = weakTokens.Count(token => SlugHasWholeToken(slugTokens, token));
                if (weakTokens.Length > 0 && weakHits == weakTokens.Length)
                {
                    return 450 + strongTokens.Length * 10 + weakHits * 20;
                }

                // Strong tokens match (e.g. "battlefield") but numeric edition differs — keep below exact edition hits.
                return 320 + strongTokens.Length * 10 + weakHits * 5;
            }

            // Digits-only search: whole slug tokens only.
            if (termTokens.All(token => SlugHasWholeToken(slugTokens, token)))
            {
                return 200 + termTokens.Length * 10;
            }

            return 0;
        }

        private static bool IsDigitsOnlyToken(string token)
        {
            if (token.IsNullOrEmpty())
            {
                return false;
            }

            for (int i = 0; i < token.Length; i++)
            {
                if (!char.IsDigit(token[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SlugHasWholeToken(string[] slugTokens, string token)
        {
            if (slugTokens == null || token.IsNullOrEmpty())
            {
                return false;
            }

            return slugTokens.Any(slugToken => slugToken.Equals(token, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Normalizes text for catalog matching (lowercase, separators to spaces, strip symbols).
        /// </summary>
        private static string NormalizeSearchText(string value)
        {
            if (value.IsNullOrEmpty())
            {
                return string.Empty;
            }

            string normalized = value.ToLowerInvariant();
            normalized = normalized.Replace('‑', '-');
            normalized = Regex.Replace(normalized, @"[_\-]+", " ");
            normalized = Regex.Replace(normalized, @"[^\p{L}\p{Nd}\s]", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            return normalized;
        }

        /// <summary>
        /// Builds a display title from a slug when product enrichment is missing.
        /// </summary>
        private static string FormatSlugAsTitle(string slug)
        {
            if (slug.IsNullOrEmpty())
            {
                return string.Empty;
            }

            TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
            return textInfo.ToTitleCase(slug.Replace('-', ' ').Replace('_', ' '));
        }

        #endregion

        /// <summary>
        /// Executes a Juno GraphQL request.
        /// </summary>
        /// <typeparam name="T">Deserialized response type.</typeparam>
        /// <param name="url">GraphQL endpoint URL.</param>
        /// <param name="query">Query object (document + variables).</param>
        /// <param name="forceAnonymous">When true, omit Authorization and send public Juno client headers.</param>
        /// <returns>Deserialized payload, or null on failure.</returns>
        private async Task<T> GetGraphQl<T>(string url, object query, bool forceAnonymous = false) where T : class
        {
            try
            {
                string payload = Serialization.ToJson(query);
                string response;
                if (forceAnonymous)
                {
                    List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("x-client-id", "EAX-JUNO-CLIENT"),
                        new KeyValuePair<string, string>("referer", "https://pc.ea.com/")
                    };
                    response = await Web.PostStringDataPayload(url, payload, null, headers).ConfigureAwait(false);
                }
                else
                {
                    StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
                    response = await Web.PostStringData(url, StoreToken?.Token, content).ConfigureAwait(false);
                }

                if (response.IsNullOrEmpty())
                {
                    return default;
                }

                if (Serialization.TryFromJson(response, out T data, out Exception parseEx))
                {
                    return data;
                }

                if (parseEx != null)
                {
                    Common.LogError(parseEx, false, true, PluginName);
                }

                return default;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return default;
            }
        }
    }
}