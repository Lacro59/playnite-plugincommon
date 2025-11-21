using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Epic.Models.Query
{
    public class QueryAchievement
    {
        public string OperationName { get; set; } = "Achievement";
        public string Query { get; set; } = @"
                query Achievement($SandboxId: String!, $Locale: String!) {
                  Achievement {
                    productAchievementsRecordBySandbox(sandboxId: $SandboxId, locale: $Locale) {
                      productId
                      sandboxId
                      totalAchievements
                      totalProductXP
                      achievementSets {
                        achievementSetId
                        isBase
                        totalAchievements
                        totalXP
                      }
                      platinumRarity {
                        percent
                      }
                      achievements {
                        achievement {
                          sandboxId
                          deploymentId
                          name
                          hidden
                          isBase
                          achievementSetId
                          unlockedDisplayName
                          lockedDisplayName
                          unlockedDescription
                          lockedDescription
                          unlockedIconId
                          lockedIconId
                          XP
                          flavorText
                          unlockedIconLink
                          lockedIconLink
                          tier {
                            name
                            hexColor
                            min
                            max
                          }
                          rarity {
                            percent
                          }
                        }
                      }
                    }
                  }
                }";
        public AchievementVariables Variables { get; set; } = new AchievementVariables();
    }

    public class AchievementVariables
    {
        public string SandboxId { get; set; }
        public string Locale { get; set; }
    }

    public class AchievementResponse
    {
        [SerializationPropertyName("data")]
        public AchievementData Data { get; set; }

        public class AchievementData
        {
            public Achievement Achievement { get; set; }
        }

        public class Achievement
        {
            [SerializationPropertyName("productAchievementsRecordBySandbox")]
            public ProductAchievementsRecord ProductAchievementsRecordBySandbox { get; set; }
        }

        public class ProductAchievementsRecord
        {
            [SerializationPropertyName("productId")]
            public string ProductId { get; set; }

            [SerializationPropertyName("sandboxId")]
            public string SandboxId { get; set; }

            [SerializationPropertyName("totalAchievements")]
            public int? TotalAchievements { get; set; }

            [SerializationPropertyName("totalProductXP")]
            public int? TotalProductXP { get; set; }

            [SerializationPropertyName("achievementSets")]
            public List<AchievementSet> AchievementSets { get; set; }

            [SerializationPropertyName("platinumRarity")]
            public Rarity PlatinumRarity { get; set; }

            [SerializationPropertyName("achievements")]
            public List<AchievementWrapper> Achievements { get; set; }
        }

        public class AchievementSet
        {
            [SerializationPropertyName("achievementSetId")]
            public string AchievementSetId { get; set; }

            [SerializationPropertyName("isBase")]
            public bool IsBase { get; set; }

            [SerializationPropertyName("totalAchievements")]
            public int TotalAchievements { get; set; }

            [SerializationPropertyName("totalXP")]
            public int TotalXP { get; set; }
        }

        public class AchievementWrapper
        {
            [SerializationPropertyName("achievement")]
            public AchievementDetail Achievement { get; set; }
        }

        public class AchievementDetail
        {
            [SerializationPropertyName("sandboxId")]
            public string SandboxId { get; set; }

            [SerializationPropertyName("deploymentId")]
            public string DeploymentId { get; set; }

            [SerializationPropertyName("name")]
            public string Name { get; set; }

            [SerializationPropertyName("hidden")]
            public bool Hidden { get; set; }

            [SerializationPropertyName("isBase")]
            public bool IsBase { get; set; }

            [SerializationPropertyName("achievementSetId")]
            public string AchievementSetId { get; set; }

            [SerializationPropertyName("unlockedDisplayName")]
            public string UnlockedDisplayName { get; set; }

            [SerializationPropertyName("lockedDisplayName")]
            public string LockedDisplayName { get; set; }

            [SerializationPropertyName("unlockedDescription")]
            public string UnlockedDescription { get; set; }

            [SerializationPropertyName("lockedDescription")]
            public string LockedDescription { get; set; }

            [SerializationPropertyName("unlockedIconId")]
            public string UnlockedIconId { get; set; }

            [SerializationPropertyName("lockedIconId")]
            public string LockedIconId { get; set; }

            [SerializationPropertyName("XP")]
            public int XP { get; set; }

            [SerializationPropertyName("flavorText")]
            public string FlavorText { get; set; }

            [SerializationPropertyName("unlockedIconLink")]
            public string UnlockedIconLink { get; set; }

            [SerializationPropertyName("lockedIconLink")]
            public string LockedIconLink { get; set; }

            [SerializationPropertyName("tier")]
            public Tier Tier { get; set; }

            [SerializationPropertyName("rarity")]
            public Rarity Rarity { get; set; }
        }

        public class Tier
        {
            [SerializationPropertyName("name")]
            public string Name { get; set; }

            [SerializationPropertyName("hexColor")]
            public string HexColor { get; set; }

            [SerializationPropertyName("min")]
            public int Min { get; set; }

            [SerializationPropertyName("max")]
            public int Max { get; set; }
        }

        public class Rarity
        {
            [SerializationPropertyName("percent")]
            public float Percent { get; set; }
        }
    }
}