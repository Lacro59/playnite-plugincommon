using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Epic.Models
{
    public class PlayerProfileAchievementsByProductId
    {
        [SerializationPropertyName("data")]
        public DataPublic Data { get; set; }

        [SerializationPropertyName("extensions")]
        public Extensions Extensions { get; set; }
    }

    public class Avatar
    {
        [SerializationPropertyName("small")]
        public string Small { get; set; }

        [SerializationPropertyName("medium")]
        public string Medium { get; set; }

        [SerializationPropertyName("large")]
        public string Large { get; set; }
    }

    public class DataPublic
    {
        [SerializationPropertyName("PlayerProfile")]
        public PlayerProfile PlayerProfile { get; set; }

        [SerializationPropertyName("epicAccountId")]
        public string EpicAccountId { get; set; }

        [SerializationPropertyName("sandboxId")]
        public string SandboxId { get; set; }

        [SerializationPropertyName("totalXP")]
        public int TotalXP { get; set; }

        [SerializationPropertyName("totalUnlocked")]
        public int TotalUnlocked { get; set; }

        [SerializationPropertyName("achievementSets")]
        public List<AchievementSet> AchievementSets { get; set; }

        [SerializationPropertyName("playerAwards")]
        public List<object> PlayerAwards { get; set; }

        [SerializationPropertyName("playerAchievements")]
        public List<PlayerAchievementPublic> PlayerAchievements { get; set; }
    }

    public class PlayerAchievementPublic
    {
        [SerializationPropertyName("playerAchievement")]
        public PlayerAchievementPublic2 PlayerAchievement { get; set; }
    }

    public class PlayerAchievementPublic2
    {
        [SerializationPropertyName("achievementName")]
        public string AchievementName { get; set; }

        [SerializationPropertyName("epicAccountId")]
        public string EpicAccountId { get; set; }

        [SerializationPropertyName("progress")]
        public int Progress { get; set; }

        [SerializationPropertyName("sandboxId")]
        public string SandboxId { get; set; }

        [SerializationPropertyName("unlocked")]
        public bool Unlocked { get; set; }

        [SerializationPropertyName("unlockDate")]
        public DateTime UnlockDate { get; set; }

        [SerializationPropertyName("XP")]
        public int XP { get; set; }

        [SerializationPropertyName("achievementSetId")]
        public string AchievementSetId { get; set; }

        [SerializationPropertyName("isBase")]
        public bool IsBase { get; set; }
    }

    public class PlayerProfile
    {
        [SerializationPropertyName("playerProfile")]
        public PlayerProfile2 PlayerProfile2 { get; set; }
    }

    public class PlayerProfile2
    {
        [SerializationPropertyName("epicAccountId")]
        public string EpicAccountId { get; set; }

        [SerializationPropertyName("displayName")]
        public string DisplayName { get; set; }

        [SerializationPropertyName("relationship")]
        public string Relationship { get; set; }

        [SerializationPropertyName("avatar")]
        public Avatar Avatar { get; set; }

        [SerializationPropertyName("productAchievements")]
        public ProductAchievements ProductAchievements { get; set; }
    }

    public class ProductAchievements
    {
        [SerializationPropertyName("__typename")]
        public string Typename { get; set; }

        [SerializationPropertyName("data")]
        public DataPublic Data { get; set; }
    }
}
