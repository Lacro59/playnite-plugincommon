using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Epic.Models.Query
{
    public class QueryGetCatalogMappings
    {
        public string OperationName { get; set; } = "GetCatalogMappings";
        public string Query { get; set; } = @"
            query GetCatalogMappings($Namespace: String!, $PageType: String!) {
              Catalog {
                catalogNs(namespace: $Namespace) {
                  mappings(pageType: $PageType) {
                    createdDate
                    deletedDate
                    mappings {
                      cmsSlug
                      offerId
                      prePurchaseOfferId
                    }
                    pageSlug
                    pageType
                    productId
                    sandboxId
                    updatedDate
                  }
                }
              }
            }";
        public CatalogVariables Variables { get; set; } = new CatalogVariables();
    }

    public class CatalogVariables
    {
        public string Namespace { get; set; }
        public string PageType { get; set; }
    }

    public class CatalogMappingsResponse
    {
        [SerializationPropertyName("data")]
        public CatalogData Data { get; set; }

        public class CatalogData
        {
            public Catalog Catalog { get; set; }
        }

        public class Catalog
        {
            [SerializationPropertyName("catalogNs")]
            public CatalogNs CatalogNs { get; set; }
        }

        public class CatalogNs
        {
            [SerializationPropertyName("mappings")]
            public List<Mapping> Mappings { get; set; }
        }
    }
}