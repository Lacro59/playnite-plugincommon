using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Ea.Models.Query
{
    public class ResponseRecentGames
    {
        [SerializationPropertyName("data")]
        public DataRecentGames Data { get; set; }
    }

    public class DataRecentGames
    {
        [SerializationPropertyName("me")]
        public MeRecentGames Me { get; set; }
    }

    public class ItemRecentGames
    {
        [SerializationPropertyName("gameSlug")]
        public string GameSlug { get; set; }

        [SerializationPropertyName("lastSessionEndDate")]
        public DateTime LastSessionEndDate { get; set; }

        [SerializationPropertyName("totalPlayTimeSeconds")]
        public int TotalPlayTimeSeconds { get; set; }
    }

    public class MeRecentGames
    {
        [SerializationPropertyName("recentGames")]
        public RecentGames RecentGames { get; set; }
    }

    public class RecentGames
    {
        [SerializationPropertyName("items")]
        public List<ItemRecentGames> Items { get; set; }
    }
}