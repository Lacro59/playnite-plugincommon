using System;
using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models
{
    public class ReleaseInfo
    {
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("appId")]
        public string AppId { get; set; }

        [SerializationPropertyName("platform")]
        public List<string> Platform { get; set; }

        [SerializationPropertyName("dateAdded")]
        public DateTime DateAdded { get; set; }
    }
}
