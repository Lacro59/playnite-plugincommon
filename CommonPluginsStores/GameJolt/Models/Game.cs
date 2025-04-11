using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
    public class Game
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("developer")]
        public Developer Developer { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("path")]
        public string Path { get; set; }

        [SerializationPropertyName("img_thumbnail")]
        public string ImgThumbnail { get; set; }

        [SerializationPropertyName("header_media_item")]
        public HeaderMediaItem HeaderMediaItem { get; set; }

        [SerializationPropertyName("thumbnail_media_item")]
        public ThumbnailMediaItem ThumbnailMediaItem { get; set; }

        [SerializationPropertyName("media_count")]
        public int? MediaCount { get; set; }

        [SerializationPropertyName("follower_count")]
        public int? FollowerCount { get; set; }

        [SerializationPropertyName("ratings_enabled")]
        public bool RatingsEnabled { get; set; }

        [SerializationPropertyName("referrals_enabled")]
        public bool ReferralsEnabled { get; set; }

        [SerializationPropertyName("compatibility")]
        public Compatibility Compatibility { get; set; }

        [SerializationPropertyName("modified_on")]
        public long ModifiedOn { get; set; }

        [SerializationPropertyName("posted_on")]
        public long PostedOn { get; set; }

        [SerializationPropertyName("published_on")]
        public long PublishedOn { get; set; }

        [SerializationPropertyName("status")]
        public int Status { get; set; }

        [SerializationPropertyName("development_status")]
        public int DevelopmentStatus { get; set; }

        [SerializationPropertyName("canceled")]
        public bool Canceled { get; set; }

        [SerializationPropertyName("tigrs_age")]
        public int TigrsAge { get; set; }

        [SerializationPropertyName("has_adult_content")]
        public bool HasAdultContent { get; set; }

        [SerializationPropertyName("theme")]
        public object Theme { get; set; }

        [SerializationPropertyName("should_show_ads")]
        public bool? ShouldShowAds { get; set; }

        [SerializationPropertyName("sellable")]
        public Sellable Sellable { get; set; }

        [SerializationPropertyName("comments_enabled")]
        public bool CommentsEnabled { get; set; }
    }
}
