using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Epic.Models.Query
{
    public class QueryWishlist
    {
        public string OperationName { get; set; } = "wishlistQuery";
        public string Query { get; set; } = @"
            query wishlistQuery($PageType: String) {
              Wishlist {
                wishlistItems {
                  elements {
                    id
                    order
                    created
                    offerId
                    updated
                    namespace
                    offer {
                      id
                      title
                      productSlug
                      urlSlug
                      offerType
                      effectiveDate
                      expiryDate
                      status
                      isCodeRedemptionOnly
                      keyImages {
                        type
                        url
                        width
                        height
                      }
                      catalogNs { 
                        mappings(pageType: $PageType) { 
                          pageSlug
                          pageType
                        }
                      }
                      offerMappings { 
                        pageSlug 
                        pageType 
                      }
                    }
                  }
                }
              }
            }";
        public WishlistVariables Variables { get; set; } = new WishlistVariables();
    }

    public class WishlistVariables
    {
        public string PageType { get; set; }
    }

    public class WishlistResponse
{
    [SerializationPropertyName("data")]
    public WishlistData Data { get; set; }

    public class WishlistData
    {
        [SerializationPropertyName("Wishlist")]
        public Wishlist Wishlist { get; set; }
    }

    public class Wishlist
    {
        [SerializationPropertyName("wishlistItems")]
        public WishlistItems WishlistItems { get; set; }
    }

    public class WishlistItems
    {
        [SerializationPropertyName("elements")]
        public List<WishlistElement> Elements { get; set; }
    }

    public class WishlistElement
    {
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("order")]
        public int Order { get; set; }

        [SerializationPropertyName("created")]
        public DateTime Created { get; set; }

        [SerializationPropertyName("offerId")]
        public string OfferId { get; set; }

        [SerializationPropertyName("updated")]
        public DateTime Updated { get; set; }

        [SerializationPropertyName("namespace")]
        public string Namespace { get; set; }

        [SerializationPropertyName("offer")]
        public Offer Offer { get; set; }
    }

    public class Offer
    {
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("productSlug")]
        public string ProductSlug { get; set; }

        [SerializationPropertyName("urlSlug")]
        public string UrlSlug { get; set; }

        [SerializationPropertyName("offerType")]
        public string OfferType { get; set; }

        [SerializationPropertyName("effectiveDate")]
        public DateTime EffectiveDate { get; set; }

        [SerializationPropertyName("expiryDate")]
        public string ExpiryDate { get; set; }

        [SerializationPropertyName("status")]
        public string Status { get; set; }

        [SerializationPropertyName("isCodeRedemptionOnly")]
        public bool IsCodeRedemptionOnly { get; set; }

        [SerializationPropertyName("keyImages")]
        public List<KeyImage> KeyImages { get; set; }

        [SerializationPropertyName("catalogNs")]
        public CatalogNs CatalogNs { get; set; }

        [SerializationPropertyName("offerMappings")]
        public List<Mapping> OfferMappings { get; set; }
    }

    public class KeyImage
    {
        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("url")]
        public string Url { get; set; }

        [SerializationPropertyName("width")]
        public int Width { get; set; }

        [SerializationPropertyName("height")]
        public int Height { get; set; }
    }

    public class CatalogNs
    {
        [SerializationPropertyName("mappings")]
        public List<Mapping> Mappings { get; set; }
    }

    public class Mapping
    {
        [SerializationPropertyName("pageSlug")]
        public string PageSlug { get; set; }

        [SerializationPropertyName("pageType")]
        public string PageType { get; set; }
    }
}
}