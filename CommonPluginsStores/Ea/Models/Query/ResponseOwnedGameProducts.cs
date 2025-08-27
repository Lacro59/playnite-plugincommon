using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Ea.Models.Query
{
    public class ResponseOwnedGameProducts
    {
        [SerializationPropertyName("data")]
        public DataOwnedGameProducts Data { get; set; }
    }

    public class BaseItem
    {
        [SerializationPropertyName("gameType")]
        public string GameType { get; set; }
    }

    public class DataOwnedGameProducts
    {
        [SerializationPropertyName("me")]
        public MeOwnedGameProducts Me { get; set; }
    }

    public class GameProductUser
    {
        [SerializationPropertyName("ownershipMethods")]
        public List<string> OwnershipMethods { get; set; }

        [SerializationPropertyName("entitlementId")]
        public string EntitlementId { get; set; }
    }

    public class ItemOwnedGameProducts
    {
        [SerializationPropertyName("originOfferId")]
        public string OriginOfferId { get; set; }

        [SerializationPropertyName("product")]
        public Product Product { get; set; }
    }

    public class MeOwnedGameProducts
    {
        [SerializationPropertyName("ownedGameProducts")]
        public OwnedGameProducts OwnedGameProducts { get; set; }
    }

    public class OwnedGameProducts
    {
        [SerializationPropertyName("items")]
        public List<ItemOwnedGameProducts> Items { get; set; }
    }

    public class Product
    {
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("gameSlug")]
        public string GameSlug { get; set; }

        [SerializationPropertyName("baseItem")]
        public BaseItem BaseItem { get; set; }

        [SerializationPropertyName("gameProductUser")]
        public GameProductUser GameProductUser { get; set; }
    }
}