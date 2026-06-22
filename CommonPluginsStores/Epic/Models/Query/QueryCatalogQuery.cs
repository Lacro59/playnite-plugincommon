using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Epic.Models.Query
{
    public class QueryCatalogQuery
    {
        public string OperationName { get; set; } = "catalogQuery";
        public string Query { get; set; } = @"
        query catalogQuery($Namespace: String!, $OfferId: String!, $Locale: String, $Country: String!, $IncludeSubItems: Boolean!) {
            Catalog {
                catalogOffer(namespace: $Namespace, id: $OfferId, locale: $Locale) {
                    title
                    id
                    namespace
                    description
                    effectiveDate
                    expiryDate
                    isCodeRedemptionOnly
                    keyImages {
                        type
                        url
                    }
                    seller {
                        id
                        name
                    }
                    productSlug
                    urlSlug
                    url
                    tags {
                        id
                    }
                    items {
                        id
                        namespace
                    }
                    customAttributes {
                        key
                        value
                    }
                    categories {
                        path
                    }
                    price(country: $Country) {
                        totalPrice {
                            discountPrice
                            originalPrice
                            voucherDiscount
                            discount
                            currencyCode
                            currencyInfo {
                                decimals
                            }
                            fmtPrice(locale: $Locale) {
                                originalPrice
                                discountPrice
                                intermediatePrice
                            }
                        }
                        lineOffers {
                            appliedRules {
                                id
                                endDate
                                discountSetting {
                                    discountType
                                }
                            }
                        }
                    }
                }
                offerSubItems(namespace: $Namespace, id: $OfferId) @include(if: $IncludeSubItems) {
                    namespace
                    id
                    releaseInfo {
                        appId
                        platform
                    }
                }
            }
        }";
        public CatalogQueryVariables Variables { get; set; } = new CatalogQueryVariables();
    }

    public class CatalogQueryVariables
    {
        public string Namespace { get; set; }
        public string OfferId { get; set; }
        public string Locale { get; set; }
        public string Country { get; set; }
        public bool IncludeSubItems { get; set; }
    }

    public class CatalogQueryResponse
    {
        [SerializationPropertyName("data")]
        public CatalogData Data { get; set; }

        public class CatalogData
        {
            [SerializationPropertyName("Catalog")]
            public Catalog Catalog { get; set; }
        }

        public class Catalog
        {
            [SerializationPropertyName("catalogOffer")]
            public CatalogOffer CatalogOffer { get; set; }

            [SerializationPropertyName("offerSubItems")]
            public List<OfferSubItem> OfferSubItems { get; set; }
        }

        public class OfferSubItem
        {
            [SerializationPropertyName("namespace")]
            public string Namespace { get; set; }

            [SerializationPropertyName("id")]
            public string Id { get; set; }

            [SerializationPropertyName("releaseInfo")]
            public List<ReleaseInfo> ReleaseInfo { get; set; }
        }

        public class ReleaseInfo
        {
            [SerializationPropertyName("appId")]
            public string AppId { get; set; }

            [SerializationPropertyName("platform")]
            public string Platform { get; set; }
        }
    }
}