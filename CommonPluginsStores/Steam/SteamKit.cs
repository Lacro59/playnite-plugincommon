using CommonPluginsShared;
using CommonPluginsStores.Steam.Models;
using CommonPluginsStores.Steam.Models.SteamKit;
using Playnite.SDK;
using Playnite.SDK.Data;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace CommonPluginsStores.Steam
{
    public class SteamKit
    {
        internal static readonly ILogger Logger = LogManager.GetLogger();
        private static readonly TimeSpan ApiRequestMinInterval = TimeSpan.FromMilliseconds(100);
        private static readonly RequestRateLimiter ApiRateLimiter = new RequestRateLimiter(ApiRequestMinInterval);
        private const int MaxWebApiRateLimitRetries = 3;
        /// <summary>HTTP 429 — not defined on <see cref="HttpStatusCode"/> in .NET Framework 4.6.2.</summary>
        private const HttpStatusCode HttpStatusTooManyRequests = (HttpStatusCode)429;
        private static readonly TimeSpan MaxRetryAfterDelay = TimeSpan.FromSeconds(120);

        private static void WaitForApiRateLimit()
        {
            ApiRateLimiter.WaitAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Calls a Steam WebAPI method with proactive throttling and quiet retries on HTTP 429.
        /// Honours <c>Retry-After</c> when present; otherwise applies a bounded backoff.
        /// </summary>
        /// <param name="steamInterface">Open SteamKit2 WebAPI interface.</param>
        /// <param name="apiMethod">WebAPI method name (e.g. GetPlayerSummaries).</param>
        /// <param name="version">Interface method version.</param>
        /// <param name="args">Method arguments, or <c>null</c>.</param>
        /// <param name="operationLabel">Short label for Warn logs (no secrets).</param>
        /// <returns>KeyValue payload from SteamKit2.</returns>
        private static KeyValue CallWithRateLimitRetry(
            WebAPI.Interface steamInterface,
            string apiMethod,
            int version,
            Dictionary<string, string> args,
            string operationLabel)
        {
            int attempt = 0;
            while (true)
            {
                WaitForApiRateLimit();
                try
                {
                    return steamInterface.Call(apiMethod, version, args);
                }
                catch (Exception ex) when (IsHttpTooManyRequests(ex))
                {
                    attempt++;
                    if (attempt > MaxWebApiRateLimitRetries)
                    {
                        Logger.Warn($"[SteamKit] {operationLabel}: HTTP 429 after retries={MaxWebApiRateLimitRetries}, giving up.");
                        throw;
                    }

                    string delaySource;
                    TimeSpan delay = GetRetryAfterDelay(ex, attempt, out delaySource);
                    Common.LogDebug($"[SteamKit] {operationLabel}: rateLimit delaySeconds={delay.TotalSeconds:F0} source={delaySource}");
                    Logger.Warn($"[SteamKit] {operationLabel}: HTTP 429, retry={attempt}/{MaxWebApiRateLimitRetries} delaySeconds={delay.TotalSeconds:F0}.");
                    Thread.Sleep(delay);
                }
            }
        }

        /// <summary>
        /// Returns true when the exception chain represents an HTTP 429 Too Many Requests response.
        /// </summary>
        private static bool IsHttpTooManyRequests(Exception ex)
        {
            if (TryGetHttpWebResponse(ex, out HttpWebResponse response))
            {
                return response.StatusCode == HttpStatusTooManyRequests;
            }

            return ExceptionMessageSuggestsTooManyRequests(ex);
        }

        private static bool TryGetHttpWebResponse(Exception ex, out HttpWebResponse response)
        {
            response = null;
            Exception current = ex;
            while (current != null)
            {
                WebException webException = current as WebException;
                if (webException?.Response is HttpWebResponse httpResponse)
                {
                    response = httpResponse;
                    return true;
                }

                AggregateException aggregate = current as AggregateException;
                if (aggregate != null)
                {
                    foreach (Exception inner in aggregate.InnerExceptions)
                    {
                        if (TryGetHttpWebResponse(inner, out response))
                        {
                            return true;
                        }
                    }
                }

                current = current.InnerException;
            }

            return false;
        }

        private static bool ExceptionMessageSuggestsTooManyRequests(Exception ex)
        {
            Exception current = ex;
            while (current != null)
            {
                if (!string.IsNullOrEmpty(current.Message)
                    && (current.Message.IndexOf("429", StringComparison.OrdinalIgnoreCase) >= 0
                        || current.Message.IndexOf("Too Many Requests", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }

                current = current.InnerException;
            }

            return false;
        }

        /// <summary>
        /// Resolves wait duration from <c>Retry-After</c> (seconds or HTTP-date), else bounded backoff.
        /// </summary>
        /// <param name="ex">Exception that carried the 429 response.</param>
        /// <param name="attempt">1-based retry index.</param>
        /// <param name="delaySource"><c>Retry-After</c> or <c>fallback</c> for Debug diagnostics.</param>
        private static TimeSpan GetRetryAfterDelay(Exception ex, int attempt, out string delaySource)
        {
            delaySource = "fallback";
            if (TryGetHttpWebResponse(ex, out HttpWebResponse response))
            {
                string retryAfter = response.Headers?[HttpResponseHeader.RetryAfter];
                if (!string.IsNullOrWhiteSpace(retryAfter))
                {
                    int seconds;
                    if (int.TryParse(retryAfter.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds)
                        && seconds >= 0)
                    {
                        delaySource = "Retry-After";
                        return CapRetryDelay(TimeSpan.FromSeconds(seconds));
                    }

                    DateTimeOffset retryAt;
                    if (DateTimeOffset.TryParse(retryAfter, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out retryAt))
                    {
                        TimeSpan until = retryAt.UtcDateTime - DateTime.UtcNow;
                        if (until > TimeSpan.Zero)
                        {
                            delaySource = "Retry-After";
                            return CapRetryDelay(until);
                        }
                    }
                }
            }

            int fallbackSeconds = Math.Min(120, Math.Max(5, 15 * attempt));
            return TimeSpan.FromSeconds(fallbackSeconds);
        }

        private static TimeSpan CapRetryDelay(TimeSpan delay)
        {
            if (delay > MaxRetryAfterDelay)
            {
                return MaxRetryAfterDelay;
            }

            if (delay < TimeSpan.FromSeconds(1))
            {
                return TimeSpan.FromSeconds(1);
            }

            return delay;
        }

        /// <summary>
        /// Logs a failed SteamKit call: Warn already emitted for exhausted 429; other failures use Error.
        /// </summary>
        private static void LogNonRateLimitFailure(Exception ex, string debugMessage, string errorMessage = null)
        {
            if (!string.IsNullOrEmpty(debugMessage))
            {
                Common.LogDebug(debugMessage);
            }

            if (IsHttpTooManyRequests(ex))
            {
                return;
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                Common.LogError(ex, false);
            }
            else
            {
                Common.LogError(ex, false, errorMessage);
            }
        }

        #region Urls
        private static string UrlAchievementImg => @"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{0}/{1}";

        private static string UrlApi => @"https://api.steampowered.com";
        private static string UrlGetGameAchievements => UrlApi + @"/IPlayerService/GetGameAchievements/v1/?appid={0}&language={1}";
		#endregion

		#region ISteamApps

		[Obsolete("Used GetAppList(apiKey)")]
		public static List<SteamApp> GetAppList()
        {
            try
            {
                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamApps"))
                {
                    KeyValue results = CallWithRateLimitRetry(steamInterface, "GetAppList", 2, null, "GetAppList");

                    KeyValue applistNode = null;
                    if (results != null)
                    {
                        applistNode = results["applist"] ?? results.Children?.FirstOrDefault(x => x.Name == "applist");
                    }

                    KeyValue appsNode = applistNode?["apps"] ?? applistNode?.Children?.FirstOrDefault(x => x.Name == "apps");

                    var apps = new List<SteamApp>();
                    if (appsNode?.Children != null)
                    {
                        foreach (KeyValue app in appsNode.Children)
                        {
                            // Each child node should contain 'appid' and 'name'
                            try
                            {
                                var idToken = app["appid"];
                                var nameToken = app["name"];
                                if (idToken != null && nameToken != null)
                                {
                                    apps.Add(new SteamApp
                                    {
                                        AppId = idToken.AsUnsignedInteger(),
                                        Name = nameToken.AsString()
                                    });
                                }
                            }
                            catch (Exception exApp)
                            {
                                // Skip malformed entries but log at debug level so we can investigate if it becomes an issue
                                Common.LogDebug($"Skipping malformed Steam app entry: {exApp.Message}");
                            }
                        }
                    }

                    return apps;
                }
            }
            catch (Exception ex)
            {
                LogNonRateLimitFailure(ex, $"[SteamKit] GetAppList failed: {ex.GetType().Name} - {ex.Message}");
                return null;
            }
        }

        #endregion

        #region IStoreService

        public static List<SteamApp> GetAppList(string apiKey, uint last_appid = 0)

        {
            Common.LogDebug($"[SteamKit] GetAppList entry last_appid={last_appid}, apiKeyLength={apiKey?.Length ?? 0}.");
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["include_dlc"] = "true",
                    ["max_results"] = "50000",
                    ["last_appid"] = last_appid.ToString()
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("IStoreService", apiKey))
                {
                    List<SteamApp> appList = new List<SteamApp>();
                    KeyValue results = CallWithRateLimitRetry(steamInterface, "GetAppList", 1, args, "GetAppList");
                    foreach (KeyValue data in results?.Children?.FirstOrDefault()?.Children)
                    {
                        appList.Add(new SteamApp
                        {
                            AppId = data["appid"].AsUnsignedInteger(),
                            Name = data["name"].AsString()
                        });
                    }

                    uint.TryParse(results?.Children?.Where(x => x.Name == "last_appid").FirstOrDefault()?.Value, out last_appid);
                    if (last_appid != 0)
                    {
                        Common.LogDebug($"[SteamKit] GetAppList pagination continues from last_appid={last_appid}.");
                        appList.AddRange(GetAppList(apiKey, last_appid));
                    }

                    Common.LogDebug($"[SteamKit] GetAppList success count={appList.Count}, last_appid={last_appid}.");
                    return appList;
                }
            }
            catch (Exception ex)
            {
                LogNonRateLimitFailure(ex, $"[SteamKit] GetAppList failed: {ex.GetType().Name} - {ex.Message}");
                return null;
            }
        }

        #endregion

        #region ISteamUser

        public static List<SteamFriend> GetFriendList(string apiKey, ulong steamId)
        {
            Common.LogDebug($"[SteamKit] GetFriendList entry steamId={steamId}, apiKeyLength={apiKey?.Length ?? 0}, interface=ISteamUser/GetFriendList/v1.");
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["steamId"] = steamId.ToString()
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamUser", apiKey))
                {
                    List<SteamFriend> friendList = new List<SteamFriend>();
                    KeyValue results = CallWithRateLimitRetry(steamInterface, "GetFriendList", 1, args, "GetFriendList");
                    foreach (KeyValue data in results["friends"].Children)
                    {
                        friendList.Add(new SteamFriend
                        {
                            SteamId = data["steamid"].AsUnsignedLong(),
                            Relationship = data["relationship"].AsString(),
                            FriendSince = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(data["friend_since"].AsInteger()),
                        });
                    }
                    Common.LogDebug($"[SteamKit] GetFriendList success count={friendList.Count}.");
                    return friendList;
                }
            }
            catch (Exception ex)
            {
                LogNonRateLimitFailure(ex, $"[SteamKit] GetFriendList failed: {ex.GetType().Name} - {ex.Message}");
                return null;
            }
        }


        public static List<SteamPlayer> GetPlayerSummaries(string apiKey, List<ulong> steamIds)
        {
            int steamIdsCount = steamIds?.Count ?? 0;
            Common.LogDebug($"[SteamKit] GetPlayerSummaries entry steamIdsCount={steamIdsCount}, apiKeyLength={apiKey?.Length ?? 0}, interface=ISteamUser/GetPlayerSummaries/v2, argKey=steamIds.");
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["steamIds"] = string.Join(",", steamIds)
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamUser", apiKey))
                {
                    List<SteamPlayer> friendList = new List<SteamPlayer>();
                    KeyValue results = CallWithRateLimitRetry(steamInterface, "GetPlayerSummaries", 2, args, "GetPlayerSummaries");
                    foreach (KeyValue data in results["players"].Children)
                    {
                        friendList.Add(new SteamPlayer
						{
                            Avatar = data["avatar"].AsString(),
                            AvatarFull = data["avatarfull"].AsString(),
                            AvatarHash = data["avatarhash"].AsString(),
                            AvatarMedium = data["avatarmedium"].AsString(),
                            CommunityVisibilityState = data["communityvisibilitystate"].AsInteger(),
                            LastLogOff = data["lastlogoff"].AsInteger(),
                            LocCountryCode = data["loccountrycode"].AsString(),
                            PersonaName = data["personaname"].AsString(),
                            PersonaState = data["personastate"].AsInteger(),
                            PersonaStateFlags = data["personastateflags"].AsInteger(),
                            PrimaryClanId = data["primaryclanid"].AsString(),
                            ProfileState = data["profilestate"].AsInteger(),
                            ProfileUrl = data["profileurl"].AsString(),
                            SteamId = data["steamid"].AsUnsignedLong(),
                            TimeCreated = data["timecreated"].AsInteger()
                        });
                    }
                    Common.LogDebug($"[SteamKit] GetPlayerSummaries success count={friendList.Count}.");
                    return friendList;
                }
            }
            catch (Exception ex)
            {
                LogNonRateLimitFailure(ex, $"[SteamKit] GetPlayerSummaries failed: {ex.GetType().Name} - {ex.Message}");
                return null;
            }
        }

        #endregion

        #region IPlayerService

        public static List<SteamGame> GetOwnedGames(string apiKey, ulong steamId)
        {
            int apiKeyLength = apiKey?.Length ?? 0;
            Common.LogDebug($"[SteamKit] GetOwnedGames entry steamId={steamId}, apiKeyLength={apiKeyLength}, interface=IPlayerService/GetOwnedGames/v1.");
            if (apiKeyLength == 17 && ulong.TryParse(apiKey, out _))
            {
                Common.LogDebug($"[SteamKit] GetOwnedGames: apiKeyLength=17 looks like SteamID64, not a Web API key — call will likely 401.");
            }
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["steamId"] = steamId.ToString(),
                    ["include_appinfo"] = "1",
                    ["include_played_free_games"] = "1",
                    ["include_extended_appinfo"] = "1"
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("IPlayerService", apiKey))
                {
                    List<SteamGame> ownedGames = new List<SteamGame>();
                    KeyValue results = CallWithRateLimitRetry(steamInterface, "GetOwnedGames", 1, args, "GetOwnedGames");
                    foreach (KeyValue data in results["games"].Children)
                    {
                        ownedGames.Add(new SteamGame
						{
                            AppId = data["appid"].AsUnsignedInteger(),
                            Name = data["name"].AsString(),
                            ImgIconUrl = data["img_icon_url"].AsString(),
                            HasCommunityVisibleStats = data["has_community_visible_stats"].AsBoolean(),
                            PlaytimeForever = data["playtime_forever"].AsInteger(),CapsuleFilename = data["capsule_filename"].AsString(),
                            HasWorkshop = data["has_workshop"].AsBoolean(),
                            HasMarket = data["has_market"].AsBoolean(),
                            HasDlc = data["has_dlc"].AsBoolean(),
                            HasLeaderboards = data["has_leaderboards"].AsBoolean()
                        });
                    }
                    Common.LogDebug($"[SteamKit] GetOwnedGames success count={ownedGames.Count}.");
                    return ownedGames;
                }
            }
            catch (Exception ex)
            {
                LogNonRateLimitFailure(ex, $"[SteamKit] GetOwnedGames failed: {ex.GetType().Name} - {ex.Message}");
                return null;
            }
        }


        public static List<SteamAchievement> GetGameAchievements(uint appId)
        {
            return GetGameAchievements(appId, "english");
        }

        public static List<SteamAchievement> GetGameAchievements(uint appId, string language)
        {
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["appId"] = appId.ToString(),
                    ["language"] = language
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("IPlayerService"))
                {
                    List<SteamAchievement> achievementLists = new List<SteamAchievement>();
                    KeyValue results = CallWithRateLimitRetry(steamInterface, "GetGameAchievements", 1, args, $"GetGameAchievements appId={appId}");

                    if (results["achievements"].Children.Count != 0)
                    {
						foreach (KeyValue data in results["achievements"].Children)
						{
							achievementLists.Add(new SteamAchievement
							{
								Hidden = data["hidden"].AsBoolean(),
								Icon = string.IsNullOrEmpty(data["icon"].AsString()) ? string.Empty : string.Format(UrlAchievementImg, appId, data["icon"].AsString()),
								IconGray = string.IsNullOrEmpty(data["icon_gray"].AsString()) ? string.Empty : string.Format(UrlAchievementImg, appId, data["icon_gray"].AsString()),
								InternalName = data["internal_name"].AsString() ?? string.Empty,
								LocalizedDesc = data["localized_desc"].AsString() ?? string.Empty,
								LocalizedName = data["localized_name"].AsString() ?? string.Empty,
								PlayerPercentUnlocked = string.IsNullOrEmpty(data["player_percent_unlocked"].AsString()) ? "100" : data["player_percent_unlocked"].AsString(),
							});
						}
					}
                    return achievementLists;
                }
            }
            catch (Exception ex)
            {
                LogNonRateLimitFailure(ex, null, $"With {appId}");
                return null;
            }
        }

        #endregion

        #region ISteamUserStats

        public static SteamSchema GetSchemaForGame(string apiKey, uint appId)
        {
            return GetSchemaForGame(apiKey, appId, "english");
        }

        public static SteamSchema GetSchemaForGame(string apiKey, uint appId, string language)
        {
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["appId"] = appId.ToString(),
                    ["l"] = language
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamUserStats", apiKey))
                {
                    SteamSchema schemaForGame = new SteamSchema();
                    KeyValue results = CallWithRateLimitRetry(steamInterface, "GetSchemaForGame", 2, args, $"GetSchemaForGame appId={appId}");
                    foreach (KeyValue data in results["availableGameStats"]["stats"].Children)
                    {
                        schemaForGame.Stats.Add(new SteamSchemaStats
                        {
                            Name = data["name"].AsString(),
                            DefaultValue = data["defaultvalue"].AsInteger(),
                            DisplayName = data["displayName"].AsString()
                        });
                    }
                    foreach (KeyValue data in results["availableGameStats"]["achievements"].Children)
                    {
                        schemaForGame.Achievements.Add(new SteamSchemaAchievements
                        {
                            Name = data["name"].AsString(),
                            DefaultValue = data["defaultvalue"].AsInteger(),
                            DisplayName = data["displayName"].AsString(),
                            Hidden = data["hidden"].AsBoolean(),
                            Icon = data["icon"].AsString(),
                            IconGray = data["icongray"].AsString()
                        });
                    }
                    return schemaForGame;
                }
            }
            catch (Exception ex)
            {
                LogNonRateLimitFailure(ex, null, $"With {appId}");
                return null;
            }
        }


        public static List<SteamStats> GetUserStatsForGame(string apiKey, uint appId, ulong steamId)
        {
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["appId"] = appId.ToString(),
                    ["steamId"] = steamId.ToString()
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamUserStats", apiKey))
                {
                    List<SteamStats> userStatsForGames = new List<SteamStats>();
                    KeyValue results = CallWithRateLimitRetry(steamInterface, "GetUserStatsForGame", 1, args, $"GetUserStatsForGame appId={appId}");
                    foreach (KeyValue data in results["stats"].Children)
                    {
                        userStatsForGames.Add(new SteamStats
                        {
                            Name = data.Name,
                            Value = data["value"].AsString()
                        });
                    }
                    return userStatsForGames;
                }
            }
            catch (Exception ex)
            {
                LogNonRateLimitFailure(ex, null, $"With {appId}");
                return null;
            }
        }


        public static List<SteamPlayerAchievement> GetPlayerAchievements(string apiKey, uint appId, ulong steamId)
        {
            return GetPlayerAchievements(apiKey, appId, steamId, "english", out _);
        }

        public static List<SteamPlayerAchievement> GetPlayerAchievements(string apiKey, uint appId, ulong steamId, string language)
        {
            return GetPlayerAchievements(apiKey, appId, steamId, language, out _);
        }

        /// <summary>
        /// Fetches player achievements via ISteamUserStats. On HTTP 403 (profile/stats private),
        /// <paramref name="isForbidden"/> is set so callers can skip further API attempts for that user.
        /// </summary>
        public static List<SteamPlayerAchievement> GetPlayerAchievements(string apiKey, uint appId, ulong steamId, string language, out bool isForbidden)
        {
            isForbidden = false;
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["appId"] = appId.ToString(),
                    ["steamId"] = steamId.ToString(),
                    ["l"] = language
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamUserStats", apiKey))
                {
                    List<SteamPlayerAchievement> steamPlayerAchievements = new List<SteamPlayerAchievement>();
                    KeyValue results = CallWithRateLimitRetry(steamInterface, "GetPlayerAchievements", 1, args, $"GetPlayerAchievements appId={appId}");

                    if (results["achievements"].Children.Count == 0)
                    {
                        // Empty achievements node — valid response (no unlocks / no items).
                    }
                    else
                    {
                        foreach (KeyValue data in results["achievements"].Children)
                        {
                            steamPlayerAchievements.Add(new SteamPlayerAchievement
                            {
                                Achieved = data["achieved"].AsInteger(),
                                ApiName = data["apiname"].AsString(),
                                Description = data["description"].AsString(),
                                Name = data["name"].AsString(),
                                UnlockTime = data["unlocktime"].AsInteger() == 0 ? default : new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(data["unlocktime"].AsInteger())
                            });
                        }
                    }
                    return steamPlayerAchievements;
                }
            }
            catch (Exception ex)
            {
                isForbidden = ex.Message != null && ex.Message.IndexOf("403", StringComparison.Ordinal) >= 0;
                LogNonRateLimitFailure(ex, null, $"With {appId}");
                return null;
            }
        }

        public static bool CheckGameIsPrivate(string apiKey, uint appId, ulong steamId)
        {
            try
            {
                Logger.Info($"CheckGameIsPrivate({appId})");
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["appId"] = appId.ToString(),
                    ["steamId"] = steamId.ToString(),
                    ["l"] = "english"
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamUserStats", apiKey))
                {
                    CallWithRateLimitRetry(steamInterface, "GetPlayerAchievements", 1, args, $"CheckGameIsPrivate appId={appId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                return ex.Message != null && ex.Message.IndexOf("403", StringComparison.Ordinal) >= 0;
            }
        }

        #endregion

    }
}
