using System;
using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models
{
    public class CatalogOffer
    {
        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("namespace")]
        public string Namespace { get; set; }

        [SerializationPropertyName("countriesBlacklist")]
        public object CountriesBlacklist { get; set; }

        [SerializationPropertyName("countriesWhitelist")]
        public object CountriesWhitelist { get; set; }

        [SerializationPropertyName("developerDisplayName")]
        public object DeveloperDisplayName { get; set; }

        [SerializationPropertyName("developer")]
        public string Developer { get; set; }

        [SerializationPropertyName("description")]
        public string Description { get; set; }

        [SerializationPropertyName("effectiveDate")]
        public DateTime EffectiveDate { get; set; }

        [SerializationPropertyName("expiryDate")]
        public object ExpiryDate { get; set; }

        [SerializationPropertyName("viewableDate")]
        public object ViewableDate { get; set; }

        [SerializationPropertyName("allowPurchaseForPartialOwned")]
        public bool AllowPurchaseForPartialOwned { get; set; }

        [SerializationPropertyName("offerType")]
        public string OfferType { get; set; }

        [SerializationPropertyName("externalLinks")]
        public object ExternalLinks { get; set; }

        [SerializationPropertyName("isCodeRedemptionOnly")]
        public bool IsCodeRedemptionOnly { get; set; }

        [SerializationPropertyName("isFeatured")]
        public bool IsFeatured { get; set; }

        [SerializationPropertyName("keyImages")]
        public List<KeyImage> KeyImages { get; set; }

        [SerializationPropertyName("longDescription")]
        public object LongDescription { get; set; }

        [SerializationPropertyName("lastModifiedDate")]
        public string LastModifiedDate { get; set; }

        [SerializationPropertyName("seller")]
        public Seller Seller { get; set; }

        [SerializationPropertyName("productSlug")]
        public string ProductSlug { get; set; }

        [SerializationPropertyName("publisherDisplayName")]
        public object PublisherDisplayName { get; set; }

        [SerializationPropertyName("releaseDate")]
        public DateTime ReleaseDate { get; set; }

        [SerializationPropertyName("urlSlug")]
        public string UrlSlug { get; set; }

        [SerializationPropertyName("url")]
        public object Url { get; set; }

        [SerializationPropertyName("tags")]
        public List<Tag> Tags { get; set; }

        [SerializationPropertyName("items")]
        public List<Item> Items { get; set; }

        [SerializationPropertyName("customAttributes")]
        public List<CustomAttribute> CustomAttributes { get; set; }

        [SerializationPropertyName("categories")]
        public List<Category> Categories { get; set; }

        [SerializationPropertyName("catalogNs")]
        public CatalogNs CatalogNs { get; set; }

        [SerializationPropertyName("offerMappings")]
        public List<object> OfferMappings { get; set; }

        [SerializationPropertyName("pcReleaseDate")]
        public DateTime PcReleaseDate { get; set; }

        [SerializationPropertyName("prePurchase")]
        public object PrePurchase { get; set; }

        [SerializationPropertyName("approximateReleasePlan")]
        public object ApproximateReleasePlan { get; set; }

        [SerializationPropertyName("price")]
        public Price Price { get; set; }

        [SerializationPropertyName("allDependNsOfferIds")]
        public object AllDependNsOfferIds { get; set; }

        [SerializationPropertyName("majorNsOffers")]
        public List<object> MajorNsOffers { get; set; }

        [SerializationPropertyName("subNsOffers")]
        public List<object> SubNsOffers { get; set; }

        [SerializationPropertyName("status")]
        public string Status { get; set; }

        [SerializationPropertyName("refundType")]
        public string RefundType { get; set; }

        [SerializationPropertyName("technicalDetails")]
        public string TechnicalDetails { get; set; }
    }

    public class AppliedRule
    {
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("endDate")]
        public DateTime EndDate { get; set; }

        [SerializationPropertyName("discountSetting")]
        public DiscountSetting DiscountSetting { get; set; }
    }

    public class CurrencyInfo
    {
        [SerializationPropertyName("decimals")]
        public int Decimals { get; set; }
    }

    public class DiscountSetting
    {
        [SerializationPropertyName("discountType")]
        public string DiscountType { get; set; }
    }

    public class FmtPrice
    {
        [SerializationPropertyName("originalPrice")]
        public string OriginalPrice { get; set; }

        [SerializationPropertyName("discountPrice")]
        public string DiscountPrice { get; set; }

        [SerializationPropertyName("intermediatePrice")]
        public string IntermediatePrice { get; set; }
    }

    public class Item
    {
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("namespace")]
        public string Namespace { get; set; }

        [SerializationPropertyName("releaseInfo")]
        public List<object> ReleaseInfo { get; set; }
    }

    public class LineOffer
    {
        [SerializationPropertyName("appliedRules")]
        public List<AppliedRule> AppliedRules { get; set; }
    }

    public class Price
    {
        [SerializationPropertyName("totalPrice")]
        public TotalPrice TotalPrice { get; set; }

        [SerializationPropertyName("lineOffers")]
        public List<LineOffer> LineOffers { get; set; }

        [SerializationPropertyName("priceDetails")]
        public PriceDetails PriceDetails { get; set; }
    }

    public class PriceDetails
    {
        [SerializationPropertyName("promotions")]
        public List<Promotion> Promotions { get; set; }
    }

    public class Promotion
    {
        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("promotionId")]
        public string PromotionId { get; set; }

        [SerializationPropertyName("membershipId")]
        public object MembershipId { get; set; }

        [SerializationPropertyName("amount")]
        public int Amount { get; set; }
    }

    public class Seller
    {
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }
    }

    public class Tag
    {
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("groupName")]
        public string GroupName { get; set; }
    }

    public class TotalPrice
    {
        [SerializationPropertyName("discountPrice")]
        public int DiscountPrice { get; set; }

        [SerializationPropertyName("originalPrice")]
        public int OriginalPrice { get; set; }

        [SerializationPropertyName("voucherDiscount")]
        public int VoucherDiscount { get; set; }

        [SerializationPropertyName("discount")]
        public int Discount { get; set; }

        [SerializationPropertyName("currencyCode")]
        public string CurrencyCode { get; set; }

        [SerializationPropertyName("currencyInfo")]
        public CurrencyInfo CurrencyInfo { get; set; }

        [SerializationPropertyName("fmtPrice")]
        public FmtPrice FmtPrice { get; set; }
    }
}