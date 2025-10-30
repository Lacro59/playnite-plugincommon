using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Epic.Models.Query
{
    public class QueryPlayerProfileAchievementsByProductId
    {
        public string OperationName { get; set; } = "playerProfileAchievementsByProductId";
        public string Query { get; set; } = @"
            query playerProfileAchievementsByProductId($EpicAccountId: String!, $ProductId: String!) {
              PlayerProfile {
                playerProfile(epicAccountId: $EpicAccountId) {
                  epicAccountId
                  displayName
                  relationship
                  avatar {
                    small
                    medium
                    large
                  }
                  productAchievements(productId: $ProductId) {
                    ... on PlayerProductAchievementsResponseSuccess {
                      data {
                        epicAccountId
                        sandboxId
                        totalXP
                        totalUnlocked
                        achievementSets {
                          achievementSetId
                          isBase
                          totalUnlocked
                          totalXP
                        }
                        playerAwards {
                          awardType
                          unlockedDateTime
                          achievementSetId
                        }
                        playerAchievements {
                          playerAchievement {
                            achievementName
                            epicAccountId
                            progress
                            sandboxId
                            unlocked
                            unlockDate
                            XP
                            achievementSetId
                            isBase
                          }
                        }
                      }
                    }
                  }
                }
              }
            }";
        public PlayerProfileAchievementsByProductIdVariables Variables { get; set; } = new PlayerProfileAchievementsByProductIdVariables();
    }

    public class PlayerProfileAchievementsByProductIdVariables
    {
        public string EpicAccountId { get; set; }
        public string ProductId { get; set; }
    }

    public class PlayerProfileAchievementsByProductIdResponse
    {
        [SerializationPropertyName("data")]
        public PlayerProfileData Data { get; set; }

        public class PlayerProfileData
        {
            [SerializationPropertyName("PlayerProfile")]
            public PlayerProfile PlayerProfile { get; set; }
        }

        public class PlayerProfile
        {
            [SerializationPropertyName("playerProfile")]
            public PlayerProfileInfo PlayerProfileInfo { get; set; }
        }

        public class PlayerProfileInfo
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

        public class Avatar
        {
            [SerializationPropertyName("small")]
            public string Small { get; set; }

            [SerializationPropertyName("medium")]
            public string Medium { get; set; }

            [SerializationPropertyName("large")]
            public string Large { get; set; }
        }

        public class ProductAchievements
        {
            [SerializationPropertyName("data")]
            public ProductAchievementsData Data { get; set; }
        }

        public class ProductAchievementsData
        {
            [SerializationPropertyName("epicAccountId")]
            public string EpicAccountId { get; set; }

            [SerializationPropertyName("sandboxId")]
            public string SandboxId { get; set; }

            [SerializationPropertyName("totalXP")]
            public int TotalXP { get; set; }

            [SerializationPropertyName("totalUnlocked")]
            public int TotalUnlocked { get; set; }

            [SerializationPropertyName("achievementSets")]
            public List<PlayerAchievementSet> AchievementSets { get; set; }

            [SerializationPropertyName("playerAwards")]
            public List<PlayerAward> PlayerAwards { get; set; }

            [SerializationPropertyName("playerAchievements")]
            public List<PlayerAchievementWrapper> PlayerAchievements { get; set; }
        }

        public class PlayerAchievementSet
        {
            [SerializationPropertyName("achievementSetId")]
            public string AchievementSetId { get; set; }

            [SerializationPropertyName("isBase")]
            public bool IsBase { get; set; }

            [SerializationPropertyName("totalUnlocked")]
            public int TotalUnlocked { get; set; }

            [SerializationPropertyName("totalXP")]
            public int TotalXP { get; set; }
        }

        public class PlayerAward
        {
            [SerializationPropertyName("awardType")]
            public string AwardType { get; set; }

            [SerializationPropertyName("unlockedDateTime")]
            public string UnlockedDateTime { get; set; }

            [SerializationPropertyName("achievementSetId")]
            public string AchievementSetId { get; set; }
        }

        public class PlayerAchievementWrapper
        {
            [SerializationPropertyName("playerAchievement")]
            public PlayerAchievementDetail PlayerAchievement { get; set; }
        }

        public class PlayerAchievementDetail
        {
            [SerializationPropertyName("achievementName")]
            public string AchievementName { get; set; }

            [SerializationPropertyName("epicAccountId")]
            public string EpicAccountId { get; set; }

            [SerializationPropertyName("progress")]
            public float Progress { get; set; }

            [SerializationPropertyName("sandboxId")]
            public string SandboxId { get; set; }

            [SerializationPropertyName("unlocked")]
            public bool Unlocked { get; set; }

            [SerializationPropertyName("unlockDate")]
            public string UnlockDate { get; set; }

            [SerializationPropertyName("XP")]
            public int XP { get; set; }

            [SerializationPropertyName("achievementSetId")]
            public string AchievementSetId { get; set; }

            [SerializationPropertyName("isBase")]
            public bool IsBase { get; set; }
        }
    }
}