using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Steam.Models
{
	public class SteamApp
	{
		[SerializationPropertyName("appid")]
		public uint AppId { get; set; }

		[SerializationPropertyName("name")]
		public string Name { get; set; }

		[SerializationPropertyName("last_modified")]
		public int LastModified { get; set; }

		[SerializationPropertyName("price_change_number")]
		public int PriceChangeNumber { get; set; }
	}

	public class SteamAppList
	{
		[SerializationPropertyName("apps")]
		public List<SteamApp> Apps { get; set; }

		[SerializationPropertyName("have_more_results")]
		public bool HaveMoreResults { get; set; }

		[SerializationPropertyName("last_appid")]
		public uint LastAppId { get; set; }
	}
}