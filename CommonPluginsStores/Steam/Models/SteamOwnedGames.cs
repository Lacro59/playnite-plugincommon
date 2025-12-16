using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Steam.Models
{
	public class SteamOwnedGames
	{
		[SerializationPropertyName("game_count")]
		public int GameCount { get; set; }

		[SerializationPropertyName("games")]
		public List<SteamGame> Games { get; set; }
	}

	public class SteamGame
	{
		[SerializationPropertyName("appid")]
		public int Appid { get; set; }

		[SerializationPropertyName("name")]
		public string Name { get; set; }

		[SerializationPropertyName("playtime_forever")]
		public int PlaytimeForever { get; set; }

		[SerializationPropertyName("img_icon_url")]
		public string ImgIconUrl { get; set; }

		[SerializationPropertyName("capsule_filename")]
		public string CapsuleFilename { get; set; }

		[SerializationPropertyName("sort_as")]
		public string SortAs { get; set; }

		[SerializationPropertyName("has_workshop")]
		public bool HasWorkshop { get; set; }

		[SerializationPropertyName("has_market")]
		public bool HasMarket { get; set; }

		[SerializationPropertyName("has_dlc")]
		public bool HasDlc { get; set; }

		[SerializationPropertyName("content_descriptorids")]
		public List<int> ContentDescriptorids { get; set; }

		[SerializationPropertyName("has_community_visible_stats")]
		public bool? HasCommunityVisibleStats { get; set; }

		[SerializationPropertyName("has_leaderboards")]
		public bool? HasLeaderboards { get; set; }
	}
}