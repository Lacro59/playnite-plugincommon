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
using System.Text;
using System.Threading;

namespace CommonPluginsStores.Steam
{
    public class SteamKit
    {
        internal static ILogger Logger => LogManager.GetLogger();

        #region Urls
        private static string UrlAchievementImg => @"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{0}/{1}";

        private static string UrlApi => @"https://api.steampowered.com";
        private static string UrlGetGameAchievements => UrlApi + @"/IPlayerService/GetGameAchievements/v1/?appid={0}&language={1}";
		#endregion

		#region ISteamApps

		[Obsolete("Used GetAppList(apiKey)")]
		public static List<SteamApp> GetAppList()
        {
            Thread.Sleep(100);
            try
            {
                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamApps"))
                {
                    KeyValue results = steamInterface.Call("GetAppList", 2, null);

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
                                Logger.Debug($"Skipping malformed Steam app entry: {exApp.Message}");
                            }
                        }
                    }

                    return apps;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }
        }

        #endregion

        #region IStoreService

        public static List<SteamApp> GetAppList(string apiKey, uint last_appid = 0)

        {
            Thread.Sleep(100);
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
                    KeyValue results = steamInterface.Call("GetAppList", 1, args);
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
                        appList.AddRange(GetAppList(apiKey, last_appid));
                    }

                    return appList;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }
        }

        #endregion

        #region ISteamUser

        public static List<SteamFriend> GetFriendList(string apiKey, ulong steamId)
        {
            Thread.Sleep(100);
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["steamId"] = steamId.ToString()
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamUser", apiKey))
                {
                    List<SteamFriend> friendList = new List<SteamFriend>();
                    KeyValue results = steamInterface.Call("GetFriendList", 1, args);
                    foreach (KeyValue data in results["friends"].Children)
                    {
                        friendList.Add(new SteamFriend
                        {
                            SteamId = data["steamid"].AsUnsignedLong(),
                            Relationship = data["relationship"].AsString(),
                            FriendSince = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(data["friend_since"].AsInteger()),
                        });
                    }
                    return friendList;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }
        }


        public static List<SteamPlayer> GetPlayerSummaries(string apiKey, List<ulong> steamIds)
        {
            Thread.Sleep(100);
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["steamIds"] = string.Join(",", steamIds)
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamUser", apiKey))
                {
                    List<SteamPlayer> friendList = new List<SteamPlayer>();
                    KeyValue results = steamInterface.Call("GetPlayerSummaries", 2, args);
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
                    return friendList;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }
        }

        #endregion

        #region IPlayerService

        public static List<SteamGame> GetOwnedGames(string apiKey, ulong steamId)
        {
            Thread.Sleep(100);
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
                    KeyValue results = steamInterface.Call("GetOwnedGames", 1, args);
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
                    return ownedGames;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }
        }


        public static List<SteamAchievement> GetGameAchievements(uint appId)
        {
            return GetGameAchievements(appId, "english");
        }

        public static List<SteamAchievement> GetGameAchievements(uint appId, string language)
        {
            Thread.Sleep(100);
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
                    KeyValue results = steamInterface.Call("GetGameAchievements", 1, args);

                    if (results["achievements"].Children.Count != 0)
                    {
						foreach (KeyValue data in results["achievements"].Children)
						{
							achievementLists.Add(new SteamAchievement
							{
								Hidden = data["hidden"].AsBoolean(),
								Icon = string.IsNullOrEmpty(data["icon"].AsString()) ? string.Empty : string.Format(UrlAchievementImg, appId, data["icon"].AsString()),
								IconGray = string.IsNullOrEmpty(data["icon_gray"].AsString()) ? string.Empty : string.Format(UrlAchievementImg, appId, data["icon_gray"].AsString()),
								InternalName = data["internal_name"].AsString(),
								LocalizedDesc = data["localized_desc"].AsString(),
								LocalizedName = data["localized_name"].AsString(),
								PlayerPercentUnlocked = string.IsNullOrEmpty(data["player_percent_unlocked"].AsString()) ? "100" : data["player_percent_unlocked"].AsString(),
							});
						}
					}
                    return achievementLists;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"With {appId}");
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
            Thread.Sleep(100);
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
                    KeyValue results = steamInterface.Call("GetSchemaForGame", 2, args);
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
                Common.LogError(ex, false, $"With {appId}");
                return null;
            }
        }


        public static List<SteamStats> GetUserStatsForGame(string apiKey, uint appId, ulong steamId)
        {
            Thread.Sleep(100);
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
                    KeyValue results = steamInterface.Call("GetUserStatsForGame", 1, args);
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
                Common.LogError(ex, false, $"With {appId}");
                return null;
            }
        }


        public static List<SteamPlayerAchievement> GetPlayerAchievements(string apiKey, uint appId, ulong steamId)
        {
            return GetPlayerAchievements(apiKey, appId, steamId, "english");
        }

        public static List<SteamPlayerAchievement> GetPlayerAchievements(string apiKey, uint appId, ulong steamId, string language)
        {
            Thread.Sleep(100);
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
                    KeyValue results = steamInterface.Call("GetPlayerAchievements", 1, args);

                    if (results["achievements"].Children.Count == 0)
                    {
                        //if (Serialization.TryFromJson(data, out dynamic steamAchievements))
                        //string data = Web.DownloadStringData(string.Format(UrlGetPlayerAchievements, appId, language, apiKey, steamId)).GetAwaiter().GetResult();
                        //{
                        //    foreach (dynamic dataApi in steamAchievements["playerstats"]["achievements"])
                        //    {
                        //        steamPlayerAchievements.Add(new SteamPlayerAchievement
                        //        {
                        //            Achieved = (int)dataApi["achieved"],
                        //            ApiName = dataApi["apiname"],
                        //            Description = dataApi["description"],
                        //            Name = dataApi["name"],
                        //            UnlockTime = (int)dataApi["unlocktime"] == 0 ? default : new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((int)dataApi["unlocktime"])
                        //        });
                        //    }
                        //}
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
                Common.LogError(ex, false, $"With {appId}");
                return null;
            }
        }

        public static bool CheckGameIsPrivate(string apiKey, uint appId, ulong steamId)
        {
            Thread.Sleep(100);
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
                    List<SteamPlayerAchievement> steamPlayerAchievements = new List<SteamPlayerAchievement>();
                    KeyValue results = steamInterface.Call("GetPlayerAchievements", 1, args);
                    return false;
                }
            }
            catch (Exception ex)
            {
                return ex.Message.Contains("403");
            }
        }

        #endregion

        private class SteamAppListResponse
        {
            [SerializationPropertyName("applist")]
            public SteamAppListContainer Applist { get; set; }
        }

        private class SteamAppListContainer
        {
            [SerializationPropertyName("apps")]
            public List<SteamApp> Apps { get; set; }
        }
    }
}