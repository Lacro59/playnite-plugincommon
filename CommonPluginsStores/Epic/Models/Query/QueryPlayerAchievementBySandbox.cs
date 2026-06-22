using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Epic.Models.Query
{
	/// <summary>
	/// GraphQL query that retrieves a player's achievement unlock records for a specific sandbox,
	/// using the sandbox ID directly (no product ID required).
	/// </summary>
	public class QueryPlayerAchievementBySandbox
	{
		public string OperationName => "PlayerAchievement";

		public string Query =>
			@"query PlayerAchievement($epicAccountId: String!, $sandboxId: String!) {
              PlayerAchievement {
                playerAchievementGameRecordsBySandbox(epicAccountId: $epicAccountId, sandboxId: $sandboxId) {
                  records {
                    totalXP
                    totalUnlocked
                    playerAwards {
                      awardType
                      unlockedDateTime
                      achievementSetId
                    }
                    achievementSets {
                      achievementSetId
                      isBase
                      totalUnlocked
                      totalXP
                    }
                    playerAchievements {
                      playerAchievement {
                        sandboxId
                        epicAccountId
                        unlocked
                        progress
                        XP
                        unlockDate
                        achievementName
                        isBase
                        achievementSetId
                      }
                    }
                  }
                }
              }
            }";

		public PlayerAchievementBySandboxVariables Variables { get; set; } = new PlayerAchievementBySandboxVariables();
	}

	public class PlayerAchievementBySandboxVariables
	{
		public string EpicAccountId { get; set; }
		public string SandboxId { get; set; }
	}

	public class PlayerAchievementBySandboxResponse
	{
		[SerializationPropertyName("data")]
		public PlayerAchievementBySandboxData Data { get; set; }

		public class PlayerAchievementBySandboxData
		{
			[SerializationPropertyName("PlayerAchievement")]
			public PlayerAchievementBySandboxRoot PlayerAchievement { get; set; }
		}

		public class PlayerAchievementBySandboxRoot
		{
			[SerializationPropertyName("playerAchievementGameRecordsBySandbox")]
			public PlayerAchievementBySandboxRecords PlayerAchievementGameRecordsBySandbox { get; set; }
		}

		public class PlayerAchievementBySandboxRecords
		{
			[SerializationPropertyName("records")]
			public List<PlayerAchievementBySandboxRecord> Records { get; set; }
		}

		public class PlayerAchievementBySandboxRecord
		{
			[SerializationPropertyName("totalXP")]
			public int TotalXP { get; set; }

			[SerializationPropertyName("totalUnlocked")]
			public int TotalUnlocked { get; set; }

			[SerializationPropertyName("playerAwards")]
			public List<PlayerAchievementBySandboxAward> PlayerAwards { get; set; }

			[SerializationPropertyName("achievementSets")]
			public List<PlayerAchievementBySandboxSet> AchievementSets { get; set; }

			[SerializationPropertyName("playerAchievements")]
			public List<PlayerAchievementBySandboxEntryWrapper> PlayerAchievements { get; set; }
		}

		public class PlayerAchievementBySandboxAward
		{
			[SerializationPropertyName("awardType")]
			public string AwardType { get; set; }

			[SerializationPropertyName("unlockedDateTime")]
			public string UnlockedDateTime { get; set; }

			[SerializationPropertyName("achievementSetId")]
			public string AchievementSetId { get; set; }
		}

		public class PlayerAchievementBySandboxSet
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

		public class PlayerAchievementBySandboxEntryWrapper
		{
			[SerializationPropertyName("playerAchievement")]
			public PlayerAchievementBySandboxEntry PlayerAchievement { get; set; }
		}

		public class PlayerAchievementBySandboxEntry
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
