using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace CommonPluginsStores.Epic.Models.Query
{
	public class QuerySearchStore
	{
		public string OperationName { get; set; } = "searchStoreQuery";

		public string Query { get; set; } = @"
        query searchStoreQuery(
            $allowCountries: String,
            $category: String,
            $count: Int,
            $country: String!,
            $keywords: String,
            $locale: String,
            $namespace: String,
            $itemNs: String,
            $sortBy: String,
            $sortDir: String,
            $start: Int,
            $tag: String,
            $releaseDate: String,
            $withPrice: Boolean = false,
            $withPromotions: Boolean = false,
            $priceRange: String,
            $freeGame: Boolean,
            $onSale: Boolean,
            $effectiveDate: String
        ) {
            Catalog {
                searchStore(
                    allowCountries: $allowCountries
                    category: $category
                    count: $count
                    country: $country
                    keywords: $keywords
                    locale: $locale
                    namespace: $namespace
                    itemNs: $itemNs
                    sortBy: $sortBy
                    sortDir: $sortDir
                    releaseDate: $releaseDate
                    start: $start
                    tag: $tag
                    priceRange: $priceRange
                    freeGame: $freeGame
                    onSale: $onSale
                    effectiveDate: $effectiveDate
                ) {
                    elements {
                        title
                        id
                        namespace
                        description
                        effectiveDate
                        keyImages { type url }
                        currentPrice
                        seller { id name }
                        productSlug
                        urlSlug
                        url
                        tags { id }
                        items { id namespace }
                        customAttributes { key value }
                        categories { path }
                        catalogNs {
                            mappings(pageType: ""productHome"") {
                                pageSlug
                                pageType
                            }
                        }
                        offerMappings {
                            pageSlug
                            pageType
                        }
                        price(country: $country) @include(if: $withPrice) {
                            totalPrice {
                                discountPrice
                                originalPrice
                                voucherDiscount
                                discount
                                currencyCode
                                currencyInfo { decimals }
                                fmtPrice(locale: $locale) {
                                    originalPrice
                                    discountPrice
                                    intermediatePrice
                                }
                            }
                        }
                        promotions(category: $category) @include(if: $withPromotions) {
                            promotionalOffers {
                                promotionalOffers {
                                    startDate
                                    endDate
                                    discountSetting {
                                        discountType
                                        discountPercentage
                                    }
                                }
                            }
                            upcomingPromotionalOffers {
                                promotionalOffers {
                                    startDate
                                    endDate
                                    discountSetting {
                                        discountType
                                        discountPercentage
                                    }
                                }
                            }
                        }
                    }
                    paging {
                        count
                        total
                    }
                }
            }
        }";

		public SearchStoreVariables Variables { get; set; } = new SearchStoreVariables();
	}

	public class SearchStoreVariables
	{
		public string AllowCountries { get; set; }
		public string Category { get; set; }
		public int? Count { get; set; }
		public string Country { get; set; }
		public string Keywords { get; set; }
		public string Locale { get; set; }
		public string Namespace { get; set; }
		public string ItemNs { get; set; }
		public string SortBy { get; set; }
		public string SortDir { get; set; }
		public int? Start { get; set; }
		public string Tag { get; set; }
		public string ReleaseDate { get; set; }
		public bool WithPrice { get; set; }
		public bool WithPromotions { get; set; }
		public string PriceRange { get; set; }
		public bool? FreeGame { get; set; }
		public bool? OnSale { get; set; }
		public string EffectiveDate { get; set; }
	}

	public class SearchStoreResponse
	{
		[SerializationPropertyName("data")]
		public DataContainer Data { get; set; }

		public class DataContainer
		{
			[SerializationPropertyName("Catalog")]
			public Catalog Catalog { get; set; }
		}

		public class Catalog
		{
			[SerializationPropertyName("searchStore")]
			public SearchStore SearchStore { get; set; }
		}

		public class SearchStore
		{
			[SerializationPropertyName("elements")]
			public List<Element> Elements { get; set; }

			[SerializationPropertyName("paging")]
			public Paging Paging { get; set; }
		}

		public class Paging
		{
			[SerializationPropertyName("count")]
			public int Count { get; set; }

			[SerializationPropertyName("total")]
			public int Total { get; set; }
		}

		public class Element
		{
			[SerializationPropertyName("title")]
			public string Title { get; set; }

			[SerializationPropertyName("id")]
			public string Id { get; set; }

			[SerializationPropertyName("namespace")]
			public string Namespace { get; set; }

			[SerializationPropertyName("description")]
			public string Description { get; set; }

			[SerializationPropertyName("effectiveDate")]
			public DateTime? EffectiveDate { get; set; }

			[SerializationPropertyName("productSlug")]
			public string ProductSlug { get; set; }

			[SerializationPropertyName("urlSlug")]
			public string UrlSlug { get; set; }

			[SerializationPropertyName("url")]
			public string Url { get; set; }

			[SerializationPropertyName("currentPrice")]
			public string CurrentPrice { get; set; }

			[SerializationPropertyName("keyImages")]
			public List<KeyImage> KeyImages { get; set; }

			[SerializationPropertyName("seller")]
			public Seller Seller { get; set; }
		}

		public class KeyImage
		{
			[SerializationPropertyName("type")]
			public string Type { get; set; }

			[SerializationPropertyName("url")]
			public string Url { get; set; }
		}

		public class Seller
		{
			[SerializationPropertyName("id")]
			public string Id { get; set; }

			[SerializationPropertyName("name")]
			public string Name { get; set; }
		}
	}
}