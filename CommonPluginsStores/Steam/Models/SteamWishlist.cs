using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Steam.Models
{
    public class SteamWishlistResponse
    {
        [SerializationPropertyName("response")]
        public SteamWishlist Response { get; set; }
    }

    public class SteamWishlist
    {
        [SerializationPropertyName("items")]
        public List<Item> Items { get; set; }
    }

    public class Item
    {
        [SerializationPropertyName("appid")]
        public uint AppId { get; set; }

        [SerializationPropertyName("priority")]
        public int Priority { get; set; }

        [SerializationPropertyName("date_added")]
        public int DateAdded { get; set; }
    }
}