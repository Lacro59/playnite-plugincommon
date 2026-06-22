using System;
using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models.Response
{
    public class CatalogOfferResponse
    {
        [SerializationPropertyName("data")]
        public DataCatalogOffer Data { get; set; }
    }

    public class DataCatalogOffer
    {
        [SerializationPropertyName("Catalog")]
        public Catalog Catalog { get; set; }
    }
}