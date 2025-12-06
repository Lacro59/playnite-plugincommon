using System;
using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models
{
    public class CatalogNs
    {
        [SerializationPropertyName("displayName")]
        public string DisplayName { get; set; }

        [SerializationPropertyName("mappings")]
        public List<Mapping> Mappings { get; set; }

        [SerializationPropertyName("parent")]
        public string Parent { get; set; }

        [SerializationPropertyName("store")]
        public string Store { get; set; }
    }
}
