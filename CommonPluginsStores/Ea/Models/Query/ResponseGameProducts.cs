using Playnite.SDK.Data;
using System.Collections.Generic;

namespace CommonPluginsStores.Ea.Models.Query
{
    /// <summary>
    /// Response root for the public Juno <c>gameProducts</c> query.
    /// </summary>
    public class ResponseGameProducts
    {
        /// <summary>
        /// GraphQL data payload.
        /// </summary>
        [SerializationPropertyName("data")]
        public DataGameProducts Data { get; set; }
    }

    /// <summary>
    /// <c>data</c> node for <see cref="ResponseGameProducts"/>.
    /// </summary>
    public class DataGameProducts
    {
        /// <summary>
        /// Catalog products matched by offer ids.
        /// </summary>
        [SerializationPropertyName("gameProducts")]
        public GameProductsResult GameProducts { get; set; }
    }

    /// <summary>
    /// <c>gameProducts</c> connection with product items.
    /// </summary>
    public class GameProductsResult
    {
        /// <summary>
        /// Product list for the requested offer ids.
        /// </summary>
        [SerializationPropertyName("items")]
        public List<GameProductItem> Items { get; set; }
    }

    /// <summary>
    /// Single catalog product returned by <c>gameProducts</c>.
    /// </summary>
    public class GameProductItem
    {
        /// <summary>
        /// Internal product identifier.
        /// </summary>
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Display name.
        /// </summary>
        [SerializationPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Origin offer id (Playnite GameId).
        /// </summary>
        [SerializationPropertyName("originOfferId")]
        public string OriginOfferId { get; set; }

        /// <summary>
        /// EA drop-api / store game slug.
        /// </summary>
        [SerializationPropertyName("gameSlug")]
        public string GameSlug { get; set; }

        /// <summary>
        /// Base catalog item (title / type).
        /// </summary>
        [SerializationPropertyName("baseItem")]
        public GameProductBaseItem BaseItem { get; set; }
    }

    /// <summary>
    /// Base item metadata under a <see cref="GameProductItem"/>.
    /// </summary>
    public class GameProductBaseItem
    {
        /// <summary>
        /// Base title.
        /// </summary>
        [SerializationPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// Product type (e.g. BASE_GAME).
        /// </summary>
        [SerializationPropertyName("gameType")]
        public string GameType { get; set; }
    }
}
