using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Steam.Models
{
    public class ItemSearch
    {
        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("tiny_image")]
        public string TinyImage { get; set; }

        [SerializationPropertyName("metascore")]
        public string Metascore { get; set; }

        [SerializationPropertyName("platforms")]
        public Platforms Platforms { get; set; }

        [SerializationPropertyName("streamingvideo")]
        public bool Streamingvideo { get; set; }

        [SerializationPropertyName("price")]
        public Price Price { get; set; }
    }

    public class Platforms
    {
        [SerializationPropertyName("windows")]
        public bool Windows { get; set; }

        [SerializationPropertyName("mac")]
        public bool Mac { get; set; }

        [SerializationPropertyName("linux")]
        public bool Linux { get; set; }
    }

    public class Price
    {
        [SerializationPropertyName("currency")]
        public string Currency { get; set; }

        [SerializationPropertyName("initial")]
        public int Initial { get; set; }

        [SerializationPropertyName("final")]
        public int Final { get; set; }
    }

    public class SteamSearch
    {
        [SerializationPropertyName("total")]
        public int Total { get; set; }

        [SerializationPropertyName("items")]
        public List<ItemSearch> Items { get; set; }
    }
}
