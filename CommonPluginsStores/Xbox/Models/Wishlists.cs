using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace CommonPluginsStores.Xbox.Models
{
    public class Disclaimer
    {
        [SerializationPropertyName("hasDisclaimer")]
        public bool HasDisclaimer { get; set; }

        [SerializationPropertyName("text")]
        public string Text { get; set; }
    }

    public class Image
    {
        [SerializationPropertyName("width")]
        public double Width { get; set; }

        [SerializationPropertyName("height")]
        public double Height { get; set; }

        [SerializationPropertyName("baseUri")]
        public string BaseUri { get; set; }

        [SerializationPropertyName("system")]
        public string System { get; set; }

        [SerializationPropertyName("alt")]
        public string Alt { get; set; }

        [SerializationPropertyName("background")]
        public string Background { get; set; }

        [SerializationPropertyName("imagePosition")]
        public string ImagePosition { get; set; }

        [SerializationPropertyName("purpose")]
        public string Purpose { get; set; }
    }

    public class Price
    {
        [SerializationPropertyName("taxLabel")]
        public string TaxLabel { get; set; }

        [SerializationPropertyName("disclaimer")]
        public Disclaimer Disclaimer { get; set; }

        [SerializationPropertyName("hasAddOns")]
        public bool HasAddOns { get; set; }

        [SerializationPropertyName("priceFormat")]
        public string PriceFormat { get; set; }

        [SerializationPropertyName("currency")]
        public string Currency { get; set; }

        [SerializationPropertyName("currencySymbol")]
        public string CurrencySymbol { get; set; }

        [SerializationPropertyName("currentPrice")]
        public string CurrentPrice { get; set; }

        [SerializationPropertyName("currentValue")]
        public double CurrentValue { get; set; }

        [SerializationPropertyName("fromText")]
        public string FromText { get; set; }
    }

    public class Product
    {
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("locale")]
        public string Locale { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("description")]
        public string Description { get; set; }

        [SerializationPropertyName("pdpUri")]
        public string PdpUri { get; set; }

        [SerializationPropertyName("price")]
        public Price Price { get; set; }

        [SerializationPropertyName("image")]
        public Image Image { get; set; }

        [SerializationPropertyName("productFamily")]
        public string ProductFamily { get; set; }

        [SerializationPropertyName("skuId")]
        public string SkuId { get; set; }
    }

    public class Wishlists
    {
        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("products")]
        public List<Product> Products { get; set; }

        [SerializationPropertyName("hasUnavailableProducts")]
        public bool HasUnavailableProducts { get; set; }

        [SerializationPropertyName("settings")]
        public Settings Settings { get; set; }

        [SerializationPropertyName("id")]
        public string Id { get; set; }
    }

    public class Settings
    {
        [SerializationPropertyName("notificationOptIn")]
        public bool NotificationOptIn { get; set; }

        [SerializationPropertyName("visibility")]
        public string Visibility { get; set; }
    }
}
