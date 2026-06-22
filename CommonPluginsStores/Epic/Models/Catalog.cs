using System;
using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models
{
    public class Catalog
    {
        [SerializationPropertyName("catalogOffer")]
        public CatalogOffer CatalogOffer { get; set; }

        [SerializationPropertyName("catalogOffers")]
        public CatalogOffers CatalogOffers { get; set; }

        [SerializationPropertyName("catalogNs")]
        public CatalogNs CatalogNs { get; set; }
    }

    public class CatalogOffers
    {
        [SerializationPropertyName("elements")]
        public List<CatalogOffer> Elements { get; set; }
    }
}