using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Ea.Models
{
    public class GameStoreDataResponse
    {
        [SerializationPropertyName("publisher")]
        public string Publisher { get; set; }

        [SerializationPropertyName("developer")]
        public string Developer { get; set; }

        [SerializationPropertyName("shortDescription")]
        public string ShortDescription { get; set; }

        [SerializationPropertyName("releaseDate")]
        public DateTime ReleaseDate { get; set; }

        [SerializationPropertyName("legalFlags")]
        public List<string> LegalFlags { get; set; }

        [SerializationPropertyName("supportedLanguages")]
        public List<string> SupportedLanguages { get; set; }

        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("packArt")]
        public PackArt PackArt { get; set; }

        [SerializationPropertyName("heroImage")]
        public HeroImage HeroImage { get; set; }

        [SerializationPropertyName("fallbackImage")]
        public object FallbackImage { get; set; }

        [SerializationPropertyName("promotions")]
        public List<Promotion> Promotions { get; set; }

        [SerializationPropertyName("trailer")]
        public Trailer Trailer { get; set; }

        [SerializationPropertyName("systemRequirements")]
        public SystemRequirements SystemRequirements { get; set; }

        [SerializationPropertyName("supportedPlatforms")]
        public List<SupportedPlatform> SupportedPlatforms { get; set; }

        [SerializationPropertyName("tagline")]
        public string Tagline { get; set; }

        [SerializationPropertyName("phaseEndDate")]
        public object PhaseEndDate { get; set; }

        [SerializationPropertyName("metaDescription")]
        public object MetaDescription { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("franchise")]
        public Franchise Franchise { get; set; }

        [SerializationPropertyName("legalText")]
        public string LegalText { get; set; }

        [SerializationPropertyName("broadcastAnnouncement")]
        public object BroadcastAnnouncement { get; set; }

        [SerializationPropertyName("phase")]
        public string Phase { get; set; }

        [SerializationPropertyName("gameTier")]
        public string GameTier { get; set; }

        [SerializationPropertyName("isPreorder")]
        public bool IsPreorder { get; set; }

        [SerializationPropertyName("genres")]
        public List<Genre> Genres { get; set; }

        [SerializationPropertyName("bonuses")]
        public object Bonuses { get; set; }

        [SerializationPropertyName("features")]
        public List<Feature> Features { get; set; }

        [SerializationPropertyName("media")]
        public List<Medium> Media { get; set; }

        [SerializationPropertyName("studio")]
        public Studio Studio { get; set; }

        [SerializationPropertyName("buyUrls")]
        public List<BuyUrl> BuyUrls { get; set; }

        [SerializationPropertyName("subscriptionInfo")]
        public SubscriptionInfo SubscriptionInfo { get; set; }

        [SerializationPropertyName("maturity")]
        public Maturity Maturity { get; set; }

        [SerializationPropertyName("logo")]
        public Logo Logo { get; set; }

        [SerializationPropertyName("eaLabel")]
        public Logo EaLabel { get; set; }

        [SerializationPropertyName("partnerLogos")]
        public List<Logo> PartnerLogos { get; set; }

        [SerializationPropertyName("techLogos")]
        public List<object> TechLogos { get; set; }

        [SerializationPropertyName("socialLinks")]
        public List<SocialLink> SocialLinks { get; set; }

        [SerializationPropertyName("spotlightContent")]
        public SpotlightContent SpotlightContent { get; set; }

        [SerializationPropertyName("spotlightItems")]
        public object SpotlightItems { get; set; }

        [SerializationPropertyName("addonsInfo")]
        public AddonsInfo AddonsInfo { get; set; }

        [SerializationPropertyName("editions")]
        public List<Edition> Editions { get; set; }

        [SerializationPropertyName("lowestPriceGameEdition")]
        public LowestPriceGameEdition LowestPriceGameEdition { get; set; }

        [SerializationPropertyName("ratings")]
        public Ratings Ratings { get; set; }

        [SerializationPropertyName("isPurchasableGame")]
        public bool IsPurchasableGame { get; set; }

        [SerializationPropertyName("isCMSDataOnly")]
        public bool IsCMSDataOnly { get; set; }

        [SerializationPropertyName("platformDetails")]
        public List<PlatformDetail> PlatformDetails { get; set; }

        [SerializationPropertyName("isFreeToPlay")]
        public bool IsFreeToPlay { get; set; }

        [SerializationPropertyName("isWishlist")]
        public bool IsWishlist { get; set; }
    }

    public class Promotion
    {
        [SerializationPropertyName("type")]
        public PromotionType Type { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("subtitle")]
        public string Subtitle { get; set; }

        [SerializationPropertyName("media")]
        public Image Media { get; set; }

        [SerializationPropertyName("tagline")]
        public object Tagline { get; set; }

        [SerializationPropertyName("mediaImage")]
        public object MediaImage { get; set; }

        [SerializationPropertyName("mediaVideo")]
        public object MediaVideo { get; set; }

        [SerializationPropertyName("logo")]
        public object Logo { get; set; }

        [SerializationPropertyName("body")]
        public string Body { get; set; }
    }

    public class PromotionType
    {
        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }
    }

    public class Trailer
    {
        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("version")]
        public Version Version { get; set; }

        [SerializationPropertyName("coverImage")]
        public Image CoverImage { get; set; }

        [SerializationPropertyName("publishingDate")]
        public DateTime PublishingDate { get; set; }

        [SerializationPropertyName("videoType")]
        public string VideoType { get; set; }
    }

    public class Feature
    {
        [SerializationPropertyName("__typename")]
        public string Typename { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("subtitle")]
        public object Subtitle { get; set; }

        [SerializationPropertyName("description")]
        public string Description { get; set; }

        [SerializationPropertyName("link")]
        public object Link { get; set; }

        [SerializationPropertyName("linkText")]
        public object LinkText { get; set; }

        [SerializationPropertyName("image")]
        public Image Image { get; set; }
    }

    public class Studio
    {
        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("description")]
        public object Description { get; set; }

        [SerializationPropertyName("logo")]
        public Logo Logo { get; set; }
    }

    public class Logo
    {
        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("asset")]
        public Asset Asset { get; set; }

        [SerializationPropertyName("targetUrl")]
        public string TargetUrl { get; set; }

        [SerializationPropertyName("alternateText")]
        public string AlternateText { get; set; }
    }

    public class Asset
    {
        [SerializationPropertyName("url")]
        public string Url { get; set; }
    }

    public class AddonItem
    {
        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("shortDescription")]
        public string ShortDescription { get; set; }

        [SerializationPropertyName("packArt")]
        public PackArt PackArt { get; set; }

        [SerializationPropertyName("price")]
        public Price Price { get; set; }

        [SerializationPropertyName("labels")]
        public List<Label> Labels { get; set; }

        [SerializationPropertyName("addonType")]
        public AddonType AddonType { get; set; }

        [SerializationPropertyName("releaseDate")]
        public string ReleaseDate { get; set; }
    }

    public class Label
    {
        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("id")]
        public string Id { get; set; }
    }

    public class AddonType
    {
        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("image")]
        public object Image { get; set; }

        [SerializationPropertyName("namePlural")]
        public string NamePlural { get; set; }

        [SerializationPropertyName("description")]
        public object Description { get; set; }
    }

    public class SpotlightItem
    {
        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("details")]
        public SpotlightItemDetails Details { get; set; }
    }

    public class SpotlightItemDetails
    {
        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("publishingDate")]
        public DateTime PublishingDate { get; set; }

        [SerializationPropertyName("media")]
        public Image Media { get; set; }

        [SerializationPropertyName("link")]
        public string Link { get; set; }

        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("tag")]
        public string Tag { get; set; }
    }

    public class AddonsInfo
    {
        [SerializationPropertyName("items")]
        public List<AddonItem> Items { get; set; }
    }

    public class ANNUAL
    {
        [SerializationPropertyName("price")]
        public Price Price { get; set; }
    }

    public class Benefit
    {
        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("key")]
        public string Key { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("description")]
        public string Description { get; set; }

        [SerializationPropertyName("contentIncluded")]
        public bool ContentIncluded { get; set; }
    }

    public class BuyUrl
    {
        [SerializationPropertyName("url")]
        public string Url { get; set; }

        [SerializationPropertyName("platform")]
        public Platform Platform { get; set; }

        [SerializationPropertyName("edition")]
        public string Edition { get; set; }
    }

    public class ContentDescriptor
    {
        [SerializationPropertyName("alternateName")]
        public object AlternateName { get; set; }

        [SerializationPropertyName("image")]
        public string Image { get; set; }
    }

    public class Edition
    {
        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("editionName")]
        public string EditionName { get; set; }

        [SerializationPropertyName("packArt")]
        public PackArt PackArt { get; set; }

        [SerializationPropertyName("acquirable")]
        public bool Acquirable { get; set; }

        [SerializationPropertyName("price")]
        public Price Price { get; set; }

        [SerializationPropertyName("isFree")]
        public bool IsFree { get; set; }

        [SerializationPropertyName("checkoutId")]
        public string CheckoutId { get; set; }

        [SerializationPropertyName("legalDisclaimers")]
        public List<LegalDisclaimer> LegalDisclaimers { get; set; }

        [SerializationPropertyName("buyUrls")]
        public List<BuyUrl> BuyUrls { get; set; }

        [SerializationPropertyName("benefits")]
        public List<Benefit> Benefits { get; set; }

        [SerializationPropertyName("buyUrl")]
        public string BuyUrl { get; set; }
    }

    public class Franchise
    {
        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("logo")]
        public object Logo { get; set; }
    }

    public class Genre
    {
        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("slug")]
        public string Slug { get; set; }
    }

    public class HeroImage
    {
        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("description")]
        public object Description { get; set; }

        [SerializationPropertyName("alternateText")]
        public string AlternateText { get; set; }

        [SerializationPropertyName("ar1X1")]
        public string Ar1X1 { get; set; }

        [SerializationPropertyName("ar2X1")]
        public string Ar2X1 { get; set; }

        [SerializationPropertyName("ar2X3")]
        public string Ar2X3 { get; set; }

        [SerializationPropertyName("ar3X1")]
        public string Ar3X1 { get; set; }

        [SerializationPropertyName("ar3X4")]
        public string Ar3X4 { get; set; }

        [SerializationPropertyName("ar4X3")]
        public string Ar4X3 { get; set; }

        [SerializationPropertyName("ar9X16")]
        public string Ar9X16 { get; set; }

        [SerializationPropertyName("ar16X9")]
        public string Ar16X9 { get; set; }
    }

    public class Image
    {
        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("description")]
        public object Description { get; set; }

        [SerializationPropertyName("alternateText")]
        public string AlternateText { get; set; }

        [SerializationPropertyName("ar1X1")]
        public string Ar1X1 { get; set; }

        [SerializationPropertyName("ar2X1")]
        public string Ar2X1 { get; set; }

        [SerializationPropertyName("ar2X3")]
        public string Ar2X3 { get; set; }

        [SerializationPropertyName("ar3X1")]
        public string Ar3X1 { get; set; }

        [SerializationPropertyName("ar3X4")]
        public string Ar3X4 { get; set; }

        [SerializationPropertyName("ar4X3")]
        public string Ar4X3 { get; set; }

        [SerializationPropertyName("ar9X16")]
        public string Ar9X16 { get; set; }

        [SerializationPropertyName("ar16X9")]
        public string Ar16X9 { get; set; }
    }

    public class InteractiveElement
    {
        [SerializationPropertyName("alternateName")]
        public string AlternateName { get; set; }

        [SerializationPropertyName("image")]
        public object Image { get; set; }
    }

    public class LegalDisclaimer
    {
        [SerializationPropertyName("limitedHtml")]
        public string LimitedHtml { get; set; }
    }

    public class LowestPriceGameEdition
    {
        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("editionName")]
        public string EditionName { get; set; }

        [SerializationPropertyName("packArt")]
        public PackArt PackArt { get; set; }

        [SerializationPropertyName("acquirable")]
        public bool Acquirable { get; set; }

        [SerializationPropertyName("price")]
        public Price Price { get; set; }

        [SerializationPropertyName("isFree")]
        public bool IsFree { get; set; }

        [SerializationPropertyName("checkoutId")]
        public string CheckoutId { get; set; }

        [SerializationPropertyName("legalDisclaimers")]
        public List<LegalDisclaimer> LegalDisclaimers { get; set; }

        [SerializationPropertyName("buyUrls")]
        public List<BuyUrl> BuyUrls { get; set; }

        [SerializationPropertyName("benefits")]
        public List<Benefit> Benefits { get; set; }
    }

    public class MainSpotlight
    {
        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("details")]
        public SpotlightItemDetails Details { get; set; }
    }

    public class Maturity
    {
        [SerializationPropertyName("minAge")]
        public int MinAge { get; set; }

        [SerializationPropertyName("alternateName")]
        public string AlternateName { get; set; }
    }

    public class Medium
    {
        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("video")]
        public Video Video { get; set; }

        [SerializationPropertyName("image")]
        public Image Image { get; set; }
    }

    public class MONTHLY
    {
        [SerializationPropertyName("price")]
        public Price Price { get; set; }
    }

    public class PackArt
    {
        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("description")]
        public object Description { get; set; }

        [SerializationPropertyName("alternateText")]
        public string AlternateText { get; set; }

        [SerializationPropertyName("ar1X1")]
        public string Ar1X1 { get; set; }

        [SerializationPropertyName("ar2X1")]
        public string Ar2X1 { get; set; }

        [SerializationPropertyName("ar2X3")]
        public string Ar2X3 { get; set; }

        [SerializationPropertyName("ar3X1")]
        public string Ar3X1 { get; set; }

        [SerializationPropertyName("ar3X4")]
        public string Ar3X4 { get; set; }

        [SerializationPropertyName("ar4X3")]
        public string Ar4X3 { get; set; }

        [SerializationPropertyName("ar9X16")]
        public string Ar9X16 { get; set; }

        [SerializationPropertyName("ar16X9")]
        public string Ar16X9 { get; set; }
    }

    public class Platform
    {
        [SerializationPropertyName("__typename")]
        public string Typename { get; set; }

        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }
    }

    public class PlatformDetail
    {
        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("path")]
        public object Path { get; set; }

        [SerializationPropertyName("editions")]
        public List<PlatformEdition> Editions { get; set; }

        [SerializationPropertyName("isThirdParty")]
        public bool IsThirdParty { get; set; }

        [SerializationPropertyName("umbrellaName")]
        public string UmbrellaName { get; set; }
    }

    public class PlatformEdition
    {
        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("buyUrl")]
        public string BuyUrl { get; set; }

        [SerializationPropertyName("price")]
        public Price Price { get; set; }

        [SerializationPropertyName("checkoutId")]
        public string CheckoutId { get; set; }
    }

    public class Play
    {
        [SerializationPropertyName("acquisitionStartDate")]
        public DateTime AcquisitionStartDate { get; set; }

        [SerializationPropertyName("acquisitionEndDate")]
        public object AcquisitionEndDate { get; set; }

        [SerializationPropertyName("ANNUAL")]
        public ANNUAL ANNUAL { get; set; }

        [SerializationPropertyName("MONTHLY")]
        public MONTHLY MONTHLY { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("benefits")]
        public List<Benefit> Benefits { get; set; }

        [SerializationPropertyName("image")]
        public Image Image { get; set; }
    }

    public class Price
    {
        [SerializationPropertyName("displayTotal")]
        public string DisplayTotal { get; set; }

        [SerializationPropertyName("displayTotalWithDiscount")]
        public string DisplayTotalWithDiscount { get; set; }

        [SerializationPropertyName("discountPercentage")]
        public int DiscountPercentage { get; set; }

        [SerializationPropertyName("currency")]
        public string Currency { get; set; }
    }

    public class Ratings
    {
        [SerializationPropertyName("alternateName")]
        public string AlternateName { get; set; }

        [SerializationPropertyName("minAge")]
        public int MinAge { get; set; }

        [SerializationPropertyName("image")]
        public string Image { get; set; }

        [SerializationPropertyName("contentDescriptors")]
        public List<ContentDescriptor> ContentDescriptors { get; set; }

        [SerializationPropertyName("interactiveElements")]
        public List<InteractiveElement> InteractiveElements { get; set; }

        [SerializationPropertyName("ratingNotices")]
        public List<object> RatingNotices { get; set; }

        [SerializationPropertyName("ratingStatus")]
        public RatingStatus RatingStatus { get; set; }

        [SerializationPropertyName("ratingAgencyUrl")]
        public string RatingAgencyUrl { get; set; }

        [SerializationPropertyName("ratingAgencySlug")]
        public string RatingAgencySlug { get; set; }
    }

    public class RatingStatus
    {
        [SerializationPropertyName("alternateName")]
        public object AlternateName { get; set; }

        [SerializationPropertyName("image")]
        public object Image { get; set; }
    }

    public class SocialLink
    {
        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("platform")]
        public string Platform { get; set; }

        [SerializationPropertyName("url")]
        public string Url { get; set; }
    }

    public class SpotlightContent
    {
        [SerializationPropertyName("label")]
        public string Label { get; set; }

        [SerializationPropertyName("description")]
        public string Description { get; set; }

        [SerializationPropertyName("mainSpotlight")]
        public MainSpotlight MainSpotlight { get; set; }

        [SerializationPropertyName("spotlightItems")]
        public List<SpotlightItem> SpotlightItems { get; set; }
    }

    public class SubscriptionInfo
    {
        [SerializationPropertyName("play")]
        public Play Play { get; set; }

        [SerializationPropertyName("play_pro")]
        public Play PlayPro { get; set; }
    }

    public class SupportedPlatform
    {
        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("path")]
        public object Path { get; set; }

        [SerializationPropertyName("editions")]
        public List<object> Editions { get; set; }

        [SerializationPropertyName("isThirdParty")]
        public bool IsThirdParty { get; set; }
    }

    public class SystemRequirements
    {
        [SerializationPropertyName("WINDOWS")]
        public WINDOWS WINDOWS { get; set; }

        [SerializationPropertyName("MAC")]
        public MAC MAC { get; set; }
    }

    public class MAC
    {
        [SerializationPropertyName("minimum")]
        public string Minimum { get; set; }

        [SerializationPropertyName("recommended")]
        public string Recommended { get; set; }
    }

    public class Version
    {
        [SerializationPropertyName("url")]
        public string Url { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("description")]
        public string Description { get; set; }

        [SerializationPropertyName("thumbnail")]
        public string Thumbnail { get; set; }
    }

    public class Video
    {
        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("version")]
        public Version Version { get; set; }

        [SerializationPropertyName("coverImage")]
        public Image CoverImage { get; set; }

        [SerializationPropertyName("publishingDate")]
        public DateTime PublishingDate { get; set; }

        [SerializationPropertyName("videoType")]
        public string VideoType { get; set; }
    }

    public class WINDOWS
    {
        [SerializationPropertyName("minimum")]
        public string Minimum { get; set; }

        [SerializationPropertyName("recommended")]
        public string Recommended { get; set; }
    }
}