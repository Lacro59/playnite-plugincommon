using CommonPluginsStores.Steam.Models.SteamKit;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CommonPluginsStores.Steam
{
    public class SteamKit
    {
        private static string UrlAchievementImg => @"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{0}/{1}";


        #region ISteamApps
        public static List<SteamApp> GetAppList()
        {
            try
            {
                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamApps"))
                {
                    List<SteamApp> appList = new List<SteamApp>();
                    KeyValue results = steamInterface.Call("GetAppList", 2);
                    foreach (KeyValue data in results["apps"].Children)
                    {
                        appList.Add(new SteamApp
                        {
                            AppId = data["appid"].AsInteger(),
                            Name = data["name"].AsString()
                        });
                    }
                    return appList;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion

        #region ISteamUser
        public static List<SteamFriend> GetFriendList(string apiKey, string steamId)
        {
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["steamId"] = steamId
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamUser", apiKey))
                {
                    List<SteamFriend> friendList = new List<SteamFriend>();
                    KeyValue results = steamInterface.Call("GetFriendList", 1, args);
                    foreach (KeyValue data in results["friends"].Children)
                    {
                        friendList.Add(new SteamFriend
                        {
                            SteamId = data["steamid"].AsString(),
                            Relationship = data["relationship"].AsString(),
                            FriendSince = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(data["friend_since"].AsInteger()).ToLocalTime(),
                        });
                    }
                    return friendList;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public static List<SteamPlayerSummaries> GetPlayerSummaries(string apiKey, List<string> steamIds)
        {
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["steamIds"] = string.Join(",", steamIds)
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamUser", apiKey))
                {
                    List<SteamPlayerSummaries> friendList = new List<SteamPlayerSummaries>();
                    KeyValue results = steamInterface.Call("GetPlayerSummaries", 2, args);
                    foreach (KeyValue data in results["players"].Children)
                    {
                        friendList.Add(new SteamPlayerSummaries
                        {
                            Avatar = data["avatar"].AsString(),
                            AvatarFull = data["avatarfull"].AsString(),
                            AvatarHash = data["avatarhash"].AsString(),
                            AvatarMedium = data["avatarmedium"].AsString(),
                            CommunityVisibilityState = data["communityvisibilitystate"].AsInteger(),
                            LastLogoff = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(data["lastlogoff"].AsInteger()).ToLocalTime(),
                            LocCountryCode = data["loccountrycode"].AsString(),
                            PersonaName = data["personaname"].AsString(),
                            PersonaState = data["personastate"].AsInteger(),
                            PersonaStateFlags = data["personastateflags"].AsInteger(),
                            PrimaryClanId = data["primaryclanid"].AsString(),
                            ProfileState = data["profilestate"].AsInteger(),
                            ProfileUrl = data["profileurl"].AsString(),
                            SteamId = data["steamid"].AsString(),
                            TimeCreated = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(data["timecreated"].AsInteger()).ToLocalTime()
                        });
                    }
                    return friendList;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion

        #region IPlayerService
        public static List<SteamOwnedGame> GetOwnedGames(string apiKey, string steamId)
        {
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["steamId"] = steamId
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("IPlayerService", apiKey))
                {
                    List<SteamOwnedGame> ownedGames = new List<SteamOwnedGame>();
                    KeyValue results = steamInterface.Call("GetOwnedGames", 1, args);
                    foreach (KeyValue data in results["games"].Children)
                    {
                        ownedGames.Add(new SteamOwnedGame
                        {
                            Appid = data["appid"].AsInteger(),
                            PlaytimeDeckForever = data["playtime_deck_forever"].AsInteger(),
                            PlaytimeDisconnected = data["playtime_disconnected"].AsInteger(),
                            PlaytimeForever = data["playtime_forever"].AsInteger(),
                            Playtime2weeks = data["playtime_2weeks"].AsInteger(),
                            PlaytimeLinuxForever = data["playtime_linux_forever"].AsInteger(),
                            PlaytimeMacForever = data["playtime_mac_forever"].AsInteger(),
                            PlaytimeWindowsForever = data["playtime_windows_forever"].AsInteger(),
                            RtimeLastPlayed = data["rtime_last_played"].AsInteger() == 0 ? default : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(data["rtime_last_played"].AsInteger()).ToLocalTime()
                        });
                    }
                    return ownedGames;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public static List<SteamAchievements> GetGameAchievements(int appId)
        {
            return GetGameAchievements(appId, "english");
        }


        public static List<SteamAchievements> GetGameAchievements(int appId, string language)
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
                    List<SteamAchievements> achievementLists = new List<SteamAchievements>();
                    KeyValue results = steamInterface.Call("GetGameAchievements", 1, args);
                    foreach (KeyValue data in results["achievements"].Children)
                    {
                        achievementLists.Add(new SteamAchievements
                        {
                            Hidden = data["hidden"].AsBoolean(),
                            Icon = string.IsNullOrEmpty(data["icon"].AsString()) ? string.Empty : string.Format(UrlAchievementImg, appId, data["icon"].AsString()),
                            IconGray = string.IsNullOrEmpty(data["icon_gray"].AsString()) ? string.Empty : string.Format(UrlAchievementImg, appId, data["icon_gray"].AsString()),
                            InternalName = data["internal_name"].AsString(),
                            LocalizedDesc = data["localized_desc"].AsString(),
                            LocalizedName = data["localized_name"].AsString(),
                            PlayerPercentUnlocked = string.IsNullOrEmpty(data["player_percent_unlocked"].AsString()) ? 100 : float.Parse(data["player_percent_unlocked"].AsString().Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)),
                        });
                    }
                    return achievementLists;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion

        #region ISteamUserStats
        public static SteamSchema GetSchemaForGame(string apiKey, int appId)
        {
            return GetSchemaForGame(apiKey, appId, "english");
        }

        public static SteamSchema GetSchemaForGame(string apiKey, int appId, string language)
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
                return null;
            }
        }


        public static List<SteamStats> GetUserStatsForGame(string apiKey, int appId, string steamId)
        {
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["appId"] = appId.ToString(),
                    ["steamId"] = steamId
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamUserStats", apiKey))
                {
                    List<SteamStats> userStatsForGames = new List<SteamStats>();
                    KeyValue results = steamInterface.Call("GetUserStatsForGame", 1, args);
                    foreach (KeyValue data in results["stats"].Children)
                    {
                        userStatsForGames.Add(new SteamStats
                        {
                            Name = data["name"].AsString(),
                            Value = data["value"].AsString()
                        });
                    }
                    return userStatsForGames;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public static List<SteamPlayerAchievement> GetPlayerAchievements(string apiKey, int appId, string steamId)
        {
            return GetPlayerAchievements(apiKey, appId, steamId, "english");
        }

        public static List<SteamPlayerAchievement> GetPlayerAchievements(string apiKey, int appId, string steamId, string language)
        {
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    ["appId"] = appId.ToString(),
                    ["steamId"] = steamId,
                    ["l"] = language
                };

                using (WebAPI.Interface steamInterface = WebAPI.GetInterface("ISteamUserStats", apiKey))
                {
                    List<SteamPlayerAchievement> steamPlayerAchievements = new List<SteamPlayerAchievement>();
                    KeyValue results = steamInterface.Call("GetPlayerAchievements", 1, args);
                    foreach (KeyValue data in results["achievements"].Children)
                    {
                        steamPlayerAchievements.Add(new SteamPlayerAchievement
                        {
                            Achieved = data["achieved"].AsInteger(),
                            ApiName = data["apiname"].AsString(),
                            Description = data["description"].AsString(),
                            Name = data["name"].AsString(),
                            UnlockTime = data["unlocktime"].AsInteger() == 0 ? default : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(data["unlocktime"].AsInteger()).ToLocalTime()
                        });
                    }
                    return steamPlayerAchievements;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion
    }
}
