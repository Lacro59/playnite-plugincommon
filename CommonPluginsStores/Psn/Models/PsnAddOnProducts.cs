using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Psn.Models
{
    public class PsnAddOnProducts
    {
        [SerializationPropertyName("data")]
        public Data Data { get; set; }
    }

    public class AddOnProduct
    {
        [SerializationPropertyName("__typename")]
        public string Typename { get; set; }

        [SerializationPropertyName("boxArt")]
        public BoxArt BoxArt { get; set; }

        [SerializationPropertyName("concept")]
        public Concept Concept { get; set; }

        [SerializationPropertyName("contentRating")]
        public ContentRating ContentRating { get; set; }

        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("invariantName")]
        public string InvariantName { get; set; }

        [SerializationPropertyName("localizedGenres")]
        public List<LocalizedGenre> LocalizedGenres { get; set; }

        [SerializationPropertyName("localizedStoreDisplayClassification")]
        public string LocalizedStoreDisplayClassification { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("platforms")]
        public List<string> Platforms { get; set; }

        [SerializationPropertyName("price")]
        public Price Price { get; set; }

        [SerializationPropertyName("skus")]
        public List<Sku> Skus { get; set; }

        [SerializationPropertyName("storeDisplayClassification")]
        public string StoreDisplayClassification { get; set; }

        [SerializationPropertyName("type")]
        public string Type { get; set; }
    }

    public class AddOnProductsByProductIdRetrieve
    {
        [SerializationPropertyName("__typename")]
        public string Typename { get; set; }

        [SerializationPropertyName("addOnProducts")]
        public List<AddOnProduct> AddOnProducts { get; set; }
    }

    public class BoxArt
    {
        [SerializationPropertyName("__typename")]
        public string Typename { get; set; }

        [SerializationPropertyName("altText")]
        public object AltText { get; set; }

        [SerializationPropertyName("url")]
        public string Url { get; set; }
    }

    public class Concept
    {
        [SerializationPropertyName("__typename")]
        public string Typename { get; set; }

        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("invariantName")]
        public string InvariantName { get; set; }

        [SerializationPropertyName("localizedGenres")]
        public List<LocalizedGenre> LocalizedGenres { get; set; }
    }

    public class ContentRating
    {
        [SerializationPropertyName("__typename")]
        public string Typename { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }
    }

    public class Data
    {
        [SerializationPropertyName("addOnProductsByProductIdRetrieve")]
        public AddOnProductsByProductIdRetrieve AddOnProductsByProductIdRetrieve { get; set; }
    }

    public class LocalizedGenre
    {
        [SerializationPropertyName("__typename")]
        public string Typename { get; set; }

        [SerializationPropertyName("value")]
        public string Value { get; set; }
    }

    public class Price
    {
        [SerializationPropertyName("__typename")]
        public string Typename { get; set; }

        [SerializationPropertyName("basePrice")]
        public string BasePrice { get; set; }

        [SerializationPropertyName("discountText")]
        public object DiscountText { get; set; }

        [SerializationPropertyName("discountedPrice")]
        public string DiscountedPrice { get; set; }

        [SerializationPropertyName("isExclusive")]
        public bool IsExclusive { get; set; }

        [SerializationPropertyName("isFree")]
        public bool IsFree { get; set; }

        [SerializationPropertyName("isTiedToSubscription")]
        public bool? IsTiedToSubscription { get; set; }

        [SerializationPropertyName("serviceBranding")]
        public List<string> ServiceBranding { get; set; }

        [SerializationPropertyName("upsellServiceBranding")]
        public List<string> UpsellServiceBranding { get; set; }

        [SerializationPropertyName("upsellText")]
        public string UpsellText { get; set; }
    }

    public class Sku
    {
        [SerializationPropertyName("__typename")]
        public string Typename { get; set; }

        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("type")]
        public string Type { get; set; }
    }
}
