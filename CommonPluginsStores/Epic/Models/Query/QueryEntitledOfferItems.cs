using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Epic.Models.Query
{
    public class QueryGetEntitledOfferItems
    {
        public string OperationName { get; set; } = "getEntitledOfferItems";
        public string Query { get; set; } = @"
            query getEntitledOfferItems($Namespace: String!, $OfferId: String!)
            {
                Launcher {
                    entitledOfferItems(namespace: $Namespace, offerId: $OfferId) {
                        namespace
                        offerId
                        entitledToAllItemsInOffer
                        entitledToAnyItemInOffer
                    }
                }
            }";
        public EntitledOfferItemsVariables Variables { get; set; } = new EntitledOfferItemsVariables();
    }

    public class EntitledOfferItemsVariables
    {
        public string Namespace { get; set; }
        public string OfferId { get; set; }
    }

    public class EntitledOfferItemsResponse
    {
        [SerializationPropertyName("data")]
        public LauncherData Data { get; set; }

        public class LauncherData
        {
            [SerializationPropertyName("Launcher")]
            public Launcher Launcher { get; set; }
        }

        public class Launcher
        {
            [SerializationPropertyName("entitledOfferItems")]
            public EntitledOfferItems EntitledOfferItems { get; set; }
        }

        public class EntitledOfferItems
        {
            [SerializationPropertyName("namespace")]
            public string Namespace { get; set; }

            [SerializationPropertyName("offerId")]
            public string OfferId { get; set; }

            [SerializationPropertyName("entitledToAllItemsInOffer")]
            public bool EntitledToAllItemsInOffer { get; set; }

            [SerializationPropertyName("entitledToAnyItemInOffer")]
            public bool EntitledToAnyItemInOffer { get; set; }
        }
    }
}