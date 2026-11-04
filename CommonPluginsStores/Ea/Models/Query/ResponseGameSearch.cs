using Playnite.SDK.Data;
using System.Collections.Generic;

namespace CommonPluginsStores.Ea.Models.Query
{
    /// <summary>
    /// Response root for public Juno <c>gameSearch</c>.
    /// </summary>
    public class ResponseGameSearch
    {
        /// <summary>
        /// GraphQL data payload.
        /// </summary>
        [SerializationPropertyName("data")]
        public DataGameSearch Data { get; set; }
    }

    /// <summary>
    /// <c>data</c> node for <see cref="ResponseGameSearch"/>.
    /// </summary>
    public class DataGameSearch
    {
        /// <summary>
        /// Catalog search connection.
        /// </summary>
        [SerializationPropertyName("gameSearch")]
        public GameSearchResult GameSearch { get; set; }
    }

    /// <summary>
    /// <c>gameSearch</c> connection with slug items.
    /// </summary>
    public class GameSearchResult
    {
        /// <summary>
        /// Catalog slug entries.
        /// </summary>
        [SerializationPropertyName("items")]
        public List<GameSearchItem> Items { get; set; }
    }

    /// <summary>
    /// Single catalog slug from <c>gameSearch</c>.
    /// </summary>
    public class GameSearchItem
    {
        /// <summary>
        /// EA store / drop-api game slug.
        /// </summary>
        [SerializationPropertyName("slug")]
        public string Slug { get; set; }
    }
}
