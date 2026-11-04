using Playnite.SDK.Data;
using System.Collections.Generic;

namespace CommonPluginsStores.Ea.Models.Query
{
    /// <summary>
    /// Response root for public Juno <c>games(slugs)</c>.
    /// </summary>
    public class ResponseGamesBySlugs
    {
        /// <summary>
        /// GraphQL data payload.
        /// </summary>
        [SerializationPropertyName("data")]
        public DataGamesBySlugs Data { get; set; }
    }

    /// <summary>
    /// <c>data</c> node for <see cref="ResponseGamesBySlugs"/>.
    /// </summary>
    public class DataGamesBySlugs
    {
        /// <summary>
        /// Games connection.
        /// </summary>
        [SerializationPropertyName("games")]
        public GamesBySlugsResult Games { get; set; }
    }

    /// <summary>
    /// <c>games</c> connection with game items.
    /// </summary>
    public class GamesBySlugsResult
    {
        /// <summary>
        /// Games matched by slug.
        /// </summary>
        [SerializationPropertyName("items")]
        public List<GameBySlugItem> Items { get; set; }
    }

    /// <summary>
    /// Single game entry with products.
    /// </summary>
    public class GameBySlugItem
    {
        /// <summary>
        /// Catalog slug.
        /// </summary>
        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        /// <summary>
        /// Products under this slug.
        /// </summary>
        [SerializationPropertyName("products")]
        public GameBySlugProducts Products { get; set; }
    }

    /// <summary>
    /// Products connection for a slug.
    /// </summary>
    public class GameBySlugProducts
    {
        /// <summary>
        /// Product list (editions / SKUs).
        /// </summary>
        [SerializationPropertyName("items")]
        public List<GameProductItem> Items { get; set; }
    }
}
