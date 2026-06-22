using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models.Response
{
    public class CatalogNamespaceResponse
    {
        [SerializationPropertyName("data")]
        public DataCatalogNamespace Data { get; set; }
    }

    public class DataCatalogNamespace
    {
        [SerializationPropertyName("Catalog")]
        public Catalog Catalog { get; set; }
    }
}