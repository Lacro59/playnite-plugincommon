using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Steam.Models.SteamKit
{
    public class SteamProfileGames
    {
        [SerializationPropertyName("strProfileName")]
        public string StrProfileName { get; set; }

        [SerializationPropertyName("bViewingOwnProfile")]
        public bool BViewingOwnProfile { get; set; }

        [SerializationPropertyName("strSteamId")]
        public string StrSteamId { get; set; }

        [SerializationPropertyName("gcpdGames")]
        public List<int> GcpdGames { get; set; }

        [SerializationPropertyName("nUserReviewCount")]
        public int NUserReviewCount { get; set; }

        [SerializationPropertyName("nUserFollowedCount")]
        public int NUserFollowedCount { get; set; }

        [SerializationPropertyName("rgContentDescriptorPreferences")]
        public RgContentDescriptorPreferences RgContentDescriptorPreferences { get; set; }

        [SerializationPropertyName("rgGames")]
        public List<RgGame> RgGames { get; set; }

        [SerializationPropertyName("rgPerfectUnownedGames")]
        public List<RgPerfectUnownedGame> RgPerfectUnownedGames { get; set; }

        [SerializationPropertyName("rgRecentlyPlayedGames")]
        public List<object> RgRecentlyPlayedGames { get; set; }

        [SerializationPropertyName("achievement_progress")]
        public List<AchievementProgress> AchievementProgress { get; set; }
    }

    public class AchievementProgress
    {
        [SerializationPropertyName("appid")]
        public int Appid { get; set; }

        [SerializationPropertyName("unlocked")]
        public int Unlocked { get; set; }

        [SerializationPropertyName("total")]
        public int Total { get; set; }

        [SerializationPropertyName("percentage")]
        public string Percentage { get; set; }

        [SerializationPropertyName("all_unlocked")]
        public int AllUnlocked { get; set; }

        [SerializationPropertyName("cache_time")]
        public int CacheTime { get; set; }

        [SerializationPropertyName("vetted")]
        public int? Vetted { get; set; }
    }

    public class RgContentDescriptorPreferences
    {
        [SerializationPropertyName("content_descriptors_to_exclude")]
        public List<object> ContentDescriptorsToExclude { get; set; }
    }

    public class RgGame
    {
        [SerializationPropertyName("appid")]
        public int Appid { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("playtime_forever")]
        public int PlaytimeForever { get; set; }

        [SerializationPropertyName("img_icon_url")]
        public string ImgIconUrl { get; set; }

        [SerializationPropertyName("playtime_windows_forever")]
        public int PlaytimeWindowsForever { get; set; }

        [SerializationPropertyName("playtime_mac_forever")]
        public int PlaytimeMacForever { get; set; }

        [SerializationPropertyName("playtime_linux_forever")]
        public int PlaytimeLinuxForever { get; set; }

        [SerializationPropertyName("playtime_deck_forever")]
        public int PlaytimeDeckForever { get; set; }

        [SerializationPropertyName("rtime_last_played")]
        public int RtimeLastPlayed { get; set; }

        [SerializationPropertyName("capsule_filename")]
        public string CapsuleFilename { get; set; }

        [SerializationPropertyName("has_workshop")]
        public int HasWorkshop { get; set; }

        [SerializationPropertyName("has_market")]
        public int HasMarket { get; set; }

        [SerializationPropertyName("has_dlc")]
        public int HasDlc { get; set; }

        [SerializationPropertyName("content_descriptorids")]
        public List<int> ContentDescriptorids { get; set; }

        [SerializationPropertyName("playtime_disconnected")]
        public int PlaytimeDisconnected { get; set; }

        [SerializationPropertyName("sort_as")]
        public string SortAs { get; set; }

        [SerializationPropertyName("has_community_visible_stats")]
        public int? HasCommunityVisibleStats { get; set; }

        [SerializationPropertyName("has_leaderboards")]
        public int? HasLeaderboards { get; set; }
    }

    public class RgPerfectUnownedGame
    {
        [SerializationPropertyName("appid")]
        public int Appid { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }
    }
}
