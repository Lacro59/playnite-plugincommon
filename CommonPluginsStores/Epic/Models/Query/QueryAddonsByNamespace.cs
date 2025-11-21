using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Epic.Models.Query
{
    public class QueryGetAddonsByNamespace
    {
        public string OperationName { get; set; } = "getAddonsByNamespace";
        public string Query { get; set; } = @"
            query getAddonsByNamespace($Categories: String!, $Count: Int!, $Country: String!, $Locale: String!, $Namespace: String!, $SortBy: String!, $SortDir: String!) {
                Catalog {
                    catalogOffers(namespace: $Namespace, locale: $Locale, params: {
                        category: $Categories,
                        count: $Count,
                        country: $Country,
                        sortBy: $SortBy,
                        sortDir: $SortDir
                    }) {
                        elements {
                            countriesBlacklist
                            customAttributes {
                                key
                                value
                            }
                            description
                            developer
                            effectiveDate
                            id
                            isFeatured
                            keyImages {
                                type
                                url
                            }
                            lastModifiedDate
                            longDescription
                            namespace
                            offerType
                            productSlug
                            releaseDate
                            status
                            technicalDetails
                            title
                            urlSlug
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
                            }
                        }
                    }
                }
            }";
        public AddonsByNamespaceVariables Variables { get; set; } = new AddonsByNamespaceVariables();
    }

    public class AddonsByNamespaceVariables
    {
        public string Categories { get; set; }
        public int Count { get; set; }
        public string Country { get; set; }
        public string Locale { get; set; }
        public string Namespace { get; set; }
        public string SortBy { get; set; }
        public string SortDir { get; set; }
    }

    public class AddonsByNamespaceResponse
    {
        [SerializationPropertyName("data")]
        public CatalogData Data { get; set; }

        public class CatalogData
        {
            [SerializationPropertyName("Catalog")]
            public Catalog Catalog { get; set; }
        }
    }
}