using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace CommonPluginsStores.Epic.Models.Query
{
	public class QueryGetMappingByPageSlug
	{
		public string OperationName { get; set; } = "getMappingByPageSlug";

		public string Query { get; set; } = @"
        query getMappingByPageSlug($pageSlug: String!, $locale: String!) {
            StorePageMapping {
                mapping(pageSlug: $pageSlug) {
                    pageSlug
                    pageType
                    sandboxId
                    productId
                    createdDate
                    updatedDate
                    mappings {
                        cmsSlug
                        offerId
                        offer(locale: $locale) {
                            id
                            namespace
                            effectiveDate
                            expiryDate
                        }
                        prePurchaseOfferId
                        prePurchaseOffer(locale: $locale) {
                            id
                            namespace
                            effectiveDate
                            expiryDate
                        }
                        pageId
                    }
                }
            }
        }";

		public GetMappingByPageSlugVariables Variables { get; set; } = new GetMappingByPageSlugVariables();
	}

	public class GetMappingByPageSlugVariables
	{
		public string PageSlug { get; set; }
		public string Locale { get; set; }
	}

	public class GetMappingByPageSlugResponse
	{
		[SerializationPropertyName("data")]
		public DataContainer Data { get; set; }

		public class DataContainer
		{
			[SerializationPropertyName("StorePageMapping")]
			public StorePageMapping StorePageMapping { get; set; }
		}

		public class StorePageMapping
		{
			[SerializationPropertyName("mapping")]
			public Mapping Mapping { get; set; }
		}

		public class Mapping
		{
			[SerializationPropertyName("pageSlug")]
			public string PageSlug { get; set; }

			[SerializationPropertyName("pageType")]
			public string PageType { get; set; }

			[SerializationPropertyName("sandboxId")]
			public string SandboxId { get; set; }

			[SerializationPropertyName("productId")]
			public string ProductId { get; set; }

			[SerializationPropertyName("createdDate")]
			public DateTime? CreatedDate { get; set; }

			[SerializationPropertyName("updatedDate")]
			public DateTime? UpdatedDate { get; set; }

			[SerializationPropertyName("mappings")]
			public MappingDetails Mappings { get; set; }
		}

		public class MappingDetails
		{
			[SerializationPropertyName("cmsSlug")]
			public string CmsSlug { get; set; }

			[SerializationPropertyName("offerId")]
			public string OfferId { get; set; }

			[SerializationPropertyName("offer")]
			public Offer Offer { get; set; }

			[SerializationPropertyName("prePurchaseOfferId")]
			public string PrePurchaseOfferId { get; set; }

			[SerializationPropertyName("prePurchaseOffer")]
			public Offer PrePurchaseOffer { get; set; }

			[SerializationPropertyName("pageId")]
			public string PageId { get; set; }
		}

		public class Offer
		{
			[SerializationPropertyName("id")]
			public string Id { get; set; }

			[SerializationPropertyName("namespace")]
			public string Namespace { get; set; }

			[SerializationPropertyName("effectiveDate")]
			public DateTime? EffectiveDate { get; set; }

			[SerializationPropertyName("expiryDate")]
			public DateTime? ExpiryDate { get; set; }
		}
	}
}