using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace CommonPluginsStores.Epic.Models
{
	public class About
	{
		[SerializationPropertyName("image")]
		public Image Image { get; set; }

		[SerializationPropertyName("developerAttribution")]
		public string DeveloperAttribution { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("publisherAttribution")]
		public string PublisherAttribution { get; set; }

		[SerializationPropertyName("description")]
		public string Description { get; set; }

		[SerializationPropertyName("shortDescription")]
		public string ShortDescription { get; set; }

		[SerializationPropertyName("title")]
		public string Title { get; set; }

		[SerializationPropertyName("developerLogo")]
		public DeveloperLogo DeveloperLogo { get; set; }
	}

	public class Action
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class AgeGate
	{
		[SerializationPropertyName("hasAgeGate")]
		public bool HasAgeGate { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class Banner
	{
		[SerializationPropertyName("showPromotion")]
		public bool ShowPromotion { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("link")]
		public Link Link { get; set; }

		[SerializationPropertyName("description")]
		public string Description { get; set; }

		[SerializationPropertyName("title")]
		public string Title { get; set; }
	}

	public class Carousel
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("items")]
		public List<Item> Items { get; set; }
	}

	public class ContingentOffer
	{
		[SerializationPropertyName("regionRestrictions")]
		public RegionRestrictions RegionRestrictions { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("hasOffer")]
		public bool HasOffer { get; set; }
	}

	public class Dark
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("theme")]
		public string Theme { get; set; }

		[SerializationPropertyName("accent")]
		public string Accent { get; set; }
	}

	public class Data
	{
		[SerializationPropertyName("productLinks")]
		public ProductLinks ProductLinks { get; set; }

		[SerializationPropertyName("socialLinks")]
		public SocialLinks SocialLinks { get; set; }

		[SerializationPropertyName("requirements")]
		public Requirements Requirements { get; set; }

		[SerializationPropertyName("navOrder")]
		public int NavOrder { get; set; }

		[SerializationPropertyName("footer")]
		public Footer Footer { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("about")]
		public About About { get; set; }

		[SerializationPropertyName("banner")]
		public Banner Banner { get; set; }

		[SerializationPropertyName("hero")]
		public Hero Hero { get; set; }

		[SerializationPropertyName("carousel")]
		public Carousel Carousel { get; set; }

		[SerializationPropertyName("experiences")]
		public Experiences Experiences { get; set; }

		[SerializationPropertyName("editions")]
		public Editions Editions { get; set; }

		[SerializationPropertyName("meta")]
		public Meta Meta { get; set; }

		[SerializationPropertyName("markdown")]
		public Markdown Markdown { get; set; }

		[SerializationPropertyName("dlc")]
		public Dlc Dlc { get; set; }

		[SerializationPropertyName("seo")]
		public Seo Seo { get; set; }

		[SerializationPropertyName("productSections")]
		public List<Section> ProductSections { get; set; }

		[SerializationPropertyName("gallery")]
		public Gallery Gallery { get; set; }

		[SerializationPropertyName("navTitle")]
		public string NavTitle { get; set; }
	}

	public class Detail
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("title")]
		public string Title { get; set; }

		[SerializationPropertyName("minimum")]
		public string Minimum { get; set; }

		[SerializationPropertyName("recommended")]
		public string Recommended { get; set; }
	}

	public class DeveloperLogo
	{
		[SerializationPropertyName("src")]
		public string Src { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class Dlc
	{
		[SerializationPropertyName("contingentOffer")]
		public ContingentOffer ContingentOffer { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("enableImages")]
		public bool EnableImages { get; set; }
	}

	public class Editions
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("enableImages")]
		public bool EnableImages { get; set; }
	}

	public class Experiences
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class ExternalNavLinks
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class Footer
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("copy")]
		public string Copy { get; set; }

		[SerializationPropertyName("privacyPolicyLink")]
		public PrivacyPolicyLink PrivacyPolicyLink { get; set; }
	}

	public class Gallery
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class Hero
	{
		[SerializationPropertyName("logoImage")]
		public LogoImage LogoImage { get; set; }

		[SerializationPropertyName("portraitBackgroundImageUrl")]
		public string PortraitBackgroundImageUrl { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("action")]
		public Action Action { get; set; }

		[SerializationPropertyName("video")]
		public Video Video { get; set; }

		[SerializationPropertyName("isFullBleed")]
		public bool IsFullBleed { get; set; }

		[SerializationPropertyName("altContentPosition")]
		public bool AltContentPosition { get; set; }

		[SerializationPropertyName("backgroundImageUrl")]
		public string BackgroundImageUrl { get; set; }
	}

	public class Image
	{
		[SerializationPropertyName("src")]
		public string Src { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class ItemProduct
	{
		[SerializationPropertyName("catalogId")]
		public string CatalogId { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("namespace")]
		public string Namespace { get; set; }

		[SerializationPropertyName("hasItem")]
		public bool HasItem { get; set; }
	}

	public class Item2
	{
		[SerializationPropertyName("image")]
		public Image Image { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("video")]
		public Video Video { get; set; }
	}

	public class LegalTag
	{
		[SerializationPropertyName("countryCodes")]
		public string CountryCodes { get; set; }

		[SerializationPropertyName("visibility")]
		public string Visibility { get; set; }

		[SerializationPropertyName("src")]
		public string Src { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("title")]
		public string Title { get; set; }
	}

	public class Light
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("theme")]
		public string Theme { get; set; }

		[SerializationPropertyName("accent")]
		public string Accent { get; set; }
	}

	public class Link
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class Logo
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class LogoImage
	{
		[SerializationPropertyName("src")]
		public string Src { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class Markdown
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class Meta
	{
		[SerializationPropertyName("releaseDate")]
		public DateTime ReleaseDate { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("publisher")]
		public List<string> Publisher { get; set; }

		[SerializationPropertyName("logo")]
		public Logo Logo { get; set; }

		[SerializationPropertyName("developer")]
		public List<string> Developer { get; set; }

		[SerializationPropertyName("platform")]
		public List<string> Platform { get; set; }

		[SerializationPropertyName("tags")]
		public List<string> Tags { get; set; }
	}

	public class Offer
	{
		[SerializationPropertyName("regionRestrictions")]
		public RegionRestrictions RegionRestrictions { get; set; }

		[SerializationPropertyName("productId")]
		public string ProductId { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("namespace")]
		public string Namespace { get; set; }

		[SerializationPropertyName("id")]
		public string Id { get; set; }

		[SerializationPropertyName("hasOffer")]
		public bool HasOffer { get; set; }
	}

	public class Og
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class Page
	{
		[SerializationPropertyName("productRatings")]
		public ProductRatings ProductRatings { get; set; }

		[SerializationPropertyName("disableNewAddons")]
		public bool DisableNewAddons { get; set; }

		[SerializationPropertyName("modMarketplaceEnabled")]
		public bool ModMarketplaceEnabled { get; set; }

		[SerializationPropertyName("_title")]
		public string Title { get; set; }

		[SerializationPropertyName("regionBlock")]
		public string RegionBlock { get; set; }

		[SerializationPropertyName("_noIndex")]
		public bool NoIndex { get; set; }

		[SerializationPropertyName("_images_")]
		public List<string> Images { get; set; }

		[SerializationPropertyName("productName")]
		public string ProductName { get; set; }

		[SerializationPropertyName("pageTheme")]
		public PageTheme PageTheme { get; set; }

		[SerializationPropertyName("namespace")]
		public string Namespace { get; set; }

		[SerializationPropertyName("theme")]
		public Theme Theme { get; set; }

		[SerializationPropertyName("reviewOptOut")]
		public bool ReviewOptOut { get; set; }

		[SerializationPropertyName("externalNavLinks")]
		public ExternalNavLinks ExternalNavLinks { get; set; }

		[SerializationPropertyName("_urlPattern")]
		public string UrlPattern { get; set; }

		[SerializationPropertyName("_slug")]
		public string Slug { get; set; }

		[SerializationPropertyName("_activeDate")]
		public DateTime ActiveDate { get; set; }

		[SerializationPropertyName("lastModified")]
		public DateTime LastModified { get; set; }

		[SerializationPropertyName("_locale")]
		public string Locale { get; set; }

		[SerializationPropertyName("_id")]
		public string Id { get; set; }

		[SerializationPropertyName("_templateName")]
		public string TemplateName { get; set; }

		[SerializationPropertyName("item")]
		public ItemProduct Item { get; set; }

		[SerializationPropertyName("data")]
		public Data Data { get; set; }

		[SerializationPropertyName("overwriteMapping")]
		public bool OverwriteMapping { get; set; }

		[SerializationPropertyName("type")]
		public string Type { get; set; }

		[SerializationPropertyName("offer")]
		public Offer Offer { get; set; }

		[SerializationPropertyName("pageRegionBlock")]
		public string PageRegionBlock { get; set; }

		[SerializationPropertyName("ageGate")]
		public AgeGate AgeGate { get; set; }
	}

	public class PageTheme
	{
		[SerializationPropertyName("preferredMode")]
		public string PreferredMode { get; set; }

		[SerializationPropertyName("light")]
		public Light Light { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("dark")]
		public Dark Dark { get; set; }
	}

	public class PrivacyPolicyLink
	{
		[SerializationPropertyName("src")]
		public string Src { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("title")]
		public string Title { get; set; }
	}

	public class ProductLinks
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class ProductRatings
	{
		[SerializationPropertyName("ratings")]
		public List<Rating> Ratings { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class Section
	{
		[SerializationPropertyName("productSection")]
		public string ProductSection { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class Rating
	{
		[SerializationPropertyName("image")]
		public Image Image { get; set; }

		[SerializationPropertyName("countryCodes")]
		public string CountryCodes { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("title")]
		public string Title { get; set; }

		[SerializationPropertyName("interactiveElements")]
		public List<string> InteractiveElements { get; set; }

		[SerializationPropertyName("contentDescriptors")]
		public List<string> ContentDescriptors { get; set; }
	}

	public class Rating3
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class RegionRestrictions
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class Requirements
	{
		[SerializationPropertyName("systems")]
		public List<System> Systems { get; set; }

		[SerializationPropertyName("legalTags")]
		public List<LegalTag> LegalTags { get; set; }

		[SerializationPropertyName("accountRequirements")]
		public string AccountRequirements { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("rating")]
		public Rating Rating { get; set; }
	}

	public class Product
	{
		[SerializationPropertyName("productRatings")]
		public ProductRatings ProductRatings { get; set; }

		[SerializationPropertyName("disableNewAddons")]
		public bool DisableNewAddons { get; set; }

		[SerializationPropertyName("modMarketplaceEnabled")]
		public bool ModMarketplaceEnabled { get; set; }

		[SerializationPropertyName("_title")]
		public string Title { get; set; }

		[SerializationPropertyName("regionBlock")]
		public string RegionBlock { get; set; }

		[SerializationPropertyName("_noIndex")]
		public bool NoIndex { get; set; }

		[SerializationPropertyName("_images_")]
		public List<string> Images { get; set; }

		[SerializationPropertyName("productName")]
		public string ProductName { get; set; }

		[SerializationPropertyName("pageTheme")]
		public PageTheme PageTheme { get; set; }

		[SerializationPropertyName("namespace")]
		public string Namespace { get; set; }

		[SerializationPropertyName("theme")]
		public Theme Theme { get; set; }

		[SerializationPropertyName("reviewOptOut")]
		public bool ReviewOptOut { get; set; }

		[SerializationPropertyName("externalNavLinks")]
		public ExternalNavLinks ExternalNavLinks { get; set; }

		[SerializationPropertyName("_urlPattern")]
		public string UrlPattern { get; set; }

		[SerializationPropertyName("_slug")]
		public string Slug { get; set; }

		[SerializationPropertyName("_activeDate")]
		public DateTime ActiveDate { get; set; }

		[SerializationPropertyName("lastModified")]
		public DateTime LastModified { get; set; }

		[SerializationPropertyName("_locale")]
		public string Locale { get; set; }

		[SerializationPropertyName("_id")]
		public string Id { get; set; }

		[SerializationPropertyName("_templateName")]
		public string TemplateName { get; set; }

		[SerializationPropertyName("pages")]
		public List<Page> Pages { get; set; }
	}

	public class Seo
	{
		[SerializationPropertyName("image")]
		public Image Image { get; set; }

		[SerializationPropertyName("twitter")]
		public Twitter Twitter { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("og")]
		public Og Og { get; set; }

		[SerializationPropertyName("description")]
		public string Description { get; set; }

		[SerializationPropertyName("title")]
		public string Title { get; set; }
	}

	public class SocialLinks
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class System
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("systemType")]
		public string SystemType { get; set; }

		[SerializationPropertyName("details")]
		public List<Detail> Details { get; set; }
	}

	public class Theme
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("buttonPrimaryBg")]
		public string ButtonPrimaryBg { get; set; }

		[SerializationPropertyName("customPrimaryBg")]
		public string CustomPrimaryBg { get; set; }

		[SerializationPropertyName("accentColor")]
		public string AccentColor { get; set; }

		[SerializationPropertyName("colorScheme")]
		public string ColorScheme { get; set; }
	}

	public class Twitter
	{
		[SerializationPropertyName("_type")]
		public string Type { get; set; }
	}

	public class Video
	{
		[SerializationPropertyName("loop")]
		public bool Loop { get; set; }

		[SerializationPropertyName("_type")]
		public string Type { get; set; }

		[SerializationPropertyName("hasFullScreen")]
		public bool HasFullScreen { get; set; }

		[SerializationPropertyName("hasControls")]
		public bool HasControls { get; set; }

		[SerializationPropertyName("muted")]
		public bool Muted { get; set; }

		[SerializationPropertyName("autoplay")]
		public bool Autoplay { get; set; }

		[SerializationPropertyName("recipes")]
		public string Recipes { get; set; }
	}
}
