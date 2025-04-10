using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
    public class TrophiesOverwiew
    {
        [SerializationPropertyName("payload")]
        public PayloadTrophies Payload { get; set; }

        [SerializationPropertyName("ver")]
        public string Ver { get; set; }

        [SerializationPropertyName("user")]
        public object User { get; set; }

        [SerializationPropertyName("c")]
        public C C { get; set; }
    }

    public class Artist
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("username")]
        public string Username { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("display_name")]
        public string DisplayName { get; set; }

        [SerializationPropertyName("web_site")]
        public string WebSite { get; set; }

        [SerializationPropertyName("url")]
        public string Url { get; set; }

        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("img_avatar")]
        public string ImgAvatar { get; set; }

        [SerializationPropertyName("shouts_enabled")]
        public bool ShoutsEnabled { get; set; }

        [SerializationPropertyName("friend_requests_enabled")]
        public bool FriendRequestsEnabled { get; set; }

        [SerializationPropertyName("is_spawnday")]
        public bool IsSpawnday { get; set; }

        [SerializationPropertyName("status")]
        public int Status { get; set; }

        [SerializationPropertyName("permission_level")]
        public int PermissionLevel { get; set; }

        [SerializationPropertyName("created_on")]
        public object CreatedOn { get; set; }

        [SerializationPropertyName("theme")]
        public Theme Theme { get; set; }

        [SerializationPropertyName("follower_count")]
        public int FollowerCount { get; set; }

        [SerializationPropertyName("following_count")]
        public int FollowingCount { get; set; }

        [SerializationPropertyName("avatar_frame")]
        public AvatarFrame AvatarFrame { get; set; }

        [SerializationPropertyName("is_verified")]
        public bool IsVerified { get; set; }

        [SerializationPropertyName("is_creator")]
        public bool IsCreator { get; set; }
    }

    public class AvatarFrame
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("image_url")]
        public string ImageUrl { get; set; }
    }

    public class Compatibility
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("game_id")]
        public int GameId { get; set; }

        [SerializationPropertyName("os_windows")]
        public bool OsWindows { get; set; }

        [SerializationPropertyName("os_windows_64")]
        public bool OsWindows64 { get; set; }

        [SerializationPropertyName("type_html")]
        public bool TypeHtml { get; set; }
    }

    public class Developer
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("username")]
        public string Username { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("display_name")]
        public string DisplayName { get; set; }

        [SerializationPropertyName("web_site")]
        public string WebSite { get; set; }

        [SerializationPropertyName("url")]
        public string Url { get; set; }

        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("img_avatar")]
        public string ImgAvatar { get; set; }

        [SerializationPropertyName("shouts_enabled")]
        public bool ShoutsEnabled { get; set; }

        [SerializationPropertyName("friend_requests_enabled")]
        public bool FriendRequestsEnabled { get; set; }

        [SerializationPropertyName("is_spawnday")]
        public bool IsSpawnday { get; set; }

        [SerializationPropertyName("status")]
        public int Status { get; set; }

        [SerializationPropertyName("permission_level")]
        public int PermissionLevel { get; set; }

        [SerializationPropertyName("created_on")]
        public long CreatedOn { get; set; }

        [SerializationPropertyName("theme")]
        public Theme Theme { get; set; }

        [SerializationPropertyName("follower_count")]
        public int FollowerCount { get; set; }

        [SerializationPropertyName("following_count")]
        public int FollowingCount { get; set; }

        [SerializationPropertyName("avatar_frame")]
        public object AvatarFrame { get; set; }
    }

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

    public class GameTrophy
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("game_id")]
        public int GameId { get; set; }

        [SerializationPropertyName("difficulty")]
        public int Difficulty { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("description")]
        public string Description { get; set; }

        [SerializationPropertyName("secret")]
        public bool Secret { get; set; }

        [SerializationPropertyName("visible")]
        public bool Visible { get; set; }

        [SerializationPropertyName("sort")]
        public int Sort { get; set; }

        [SerializationPropertyName("experience")]
        public int Experience { get; set; }

        [SerializationPropertyName("img_thumbnail")]
        public string ImgThumbnail { get; set; }

        [SerializationPropertyName("has_thumbnail")]
        public bool HasThumbnail { get; set; }

        [SerializationPropertyName("is_owner")]
        public bool IsOwner { get; set; }
    }

    public class HeaderMediaItem
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("parent_id")]
        public int ParentId { get; set; }

        [SerializationPropertyName("hash")]
        public string Hash { get; set; }

        [SerializationPropertyName("filename")]
        public string Filename { get; set; }

        [SerializationPropertyName("filetype")]
        public string Filetype { get; set; }

        [SerializationPropertyName("is_animated")]
        public bool IsAnimated { get; set; }

        [SerializationPropertyName("width")]
        public int Width { get; set; }

        [SerializationPropertyName("height")]
        public int Height { get; set; }

        [SerializationPropertyName("filesize")]
        public int Filesize { get; set; }

        [SerializationPropertyName("crop_start_x")]
        public object CropStartX { get; set; }

        [SerializationPropertyName("crop_start_y")]
        public object CropStartY { get; set; }

        [SerializationPropertyName("crop_end_x")]
        public object CropEndX { get; set; }

        [SerializationPropertyName("crop_end_y")]
        public object CropEndY { get; set; }

        [SerializationPropertyName("avg_img_color")]
        public string AvgImgColor { get; set; }

        [SerializationPropertyName("img_has_transparency")]
        public bool ImgHasTransparency { get; set; }

        [SerializationPropertyName("added_on")]
        public long AddedOn { get; set; }

        [SerializationPropertyName("status")]
        public string Status { get; set; }

        [SerializationPropertyName("img_url")]
        public string ImgUrl { get; set; }

        [SerializationPropertyName("mediaserver_url")]
        public string MediaserverUrl { get; set; }

        [SerializationPropertyName("mediaserver_url_webm")]
        public object MediaserverUrlWebm { get; set; }

        [SerializationPropertyName("mediaserver_url_mp4")]
        public object MediaserverUrlMp4 { get; set; }

        [SerializationPropertyName("video_card_url_mp4")]
        public object VideoCardUrlMp4 { get; set; }
    }

    public class PayloadTrophies
    {
        [SerializationPropertyName("trophies")]
        public List<Trophy> Trophies { get; set; }

        [SerializationPropertyName("pageSize")]
        public int PageSize { get; set; }
    }

    public class Resource
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("game_id")]
        public int GameId { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("description")]
        public object Description { get; set; }

        [SerializationPropertyName("sort")]
        public int Sort { get; set; }

        [SerializationPropertyName("added_on")]
        public long AddedOn { get; set; }

        [SerializationPropertyName("published_on")]
        public long PublishedOn { get; set; }

        [SerializationPropertyName("updated_on")]
        public long UpdatedOn { get; set; }

        [SerializationPropertyName("visibility")]
        public string Visibility { get; set; }

        [SerializationPropertyName("partner_visibility")]
        public object PartnerVisibility { get; set; }

        [SerializationPropertyName("status")]
        public string Status { get; set; }

        [SerializationPropertyName("game")]
        public Game Game { get; set; }
    }

    public class Sellable
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("primary")]
        public bool Primary { get; set; }

        [SerializationPropertyName("key")]
        public string Key { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("description")]
        public object Description { get; set; }

        [SerializationPropertyName("pricings")]
        public List<object> Pricings { get; set; }

        [SerializationPropertyName("linked_key_providers")]
        public List<object> LinkedKeyProviders { get; set; }

        [SerializationPropertyName("resource_type")]
        public string ResourceType { get; set; }

        [SerializationPropertyName("resource")]
        public Resource Resource { get; set; }
    }

    public class SiteTrophy
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("difficulty")]
        public int Difficulty { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("description")]
        public string Description { get; set; }

        [SerializationPropertyName("secret")]
        public bool Secret { get; set; }

        [SerializationPropertyName("experience")]
        public int Experience { get; set; }

        [SerializationPropertyName("img_thumbnail")]
        public string ImgThumbnail { get; set; }

        [SerializationPropertyName("has_thumbnail")]
        public bool HasThumbnail { get; set; }

        [SerializationPropertyName("is_owner")]
        public bool IsOwner { get; set; }

        [SerializationPropertyName("artist")]
        public Artist Artist { get; set; }
    }

    public class Theme
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("theme_preset_id")]
        public int ThemePresetId { get; set; }

        [SerializationPropertyName("highlight")]
        public string Highlight { get; set; }

        [SerializationPropertyName("backlight")]
        public string Backlight { get; set; }

        [SerializationPropertyName("notice")]
        public string Notice { get; set; }

        [SerializationPropertyName("tint")]
        public string Tint { get; set; }

        [SerializationPropertyName("custom")]
        public string Custom { get; set; }
    }

    public class ThumbnailMediaItem
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("parent_id")]
        public int ParentId { get; set; }

        [SerializationPropertyName("hash")]
        public string Hash { get; set; }

        [SerializationPropertyName("filename")]
        public string Filename { get; set; }

        [SerializationPropertyName("filetype")]
        public string Filetype { get; set; }

        [SerializationPropertyName("is_animated")]
        public bool IsAnimated { get; set; }

        [SerializationPropertyName("width")]
        public int Width { get; set; }

        [SerializationPropertyName("height")]
        public int Height { get; set; }

        [SerializationPropertyName("filesize")]
        public int Filesize { get; set; }

        [SerializationPropertyName("crop_start_x")]
        public object CropStartX { get; set; }

        [SerializationPropertyName("crop_start_y")]
        public object CropStartY { get; set; }

        [SerializationPropertyName("crop_end_x")]
        public object CropEndX { get; set; }

        [SerializationPropertyName("crop_end_y")]
        public object CropEndY { get; set; }

        [SerializationPropertyName("avg_img_color")]
        public string AvgImgColor { get; set; }

        [SerializationPropertyName("img_has_transparency")]
        public bool ImgHasTransparency { get; set; }

        [SerializationPropertyName("added_on")]
        public long AddedOn { get; set; }

        [SerializationPropertyName("status")]
        public string Status { get; set; }

        [SerializationPropertyName("img_url")]
        public string ImgUrl { get; set; }

        [SerializationPropertyName("mediaserver_url")]
        public string MediaserverUrl { get; set; }

        [SerializationPropertyName("mediaserver_url_webm")]
        public object MediaserverUrlWebm { get; set; }

        [SerializationPropertyName("mediaserver_url_mp4")]
        public object MediaserverUrlMp4 { get; set; }

        [SerializationPropertyName("video_card_url_mp4")]
        public object VideoCardUrlMp4 { get; set; }
    }

    public class Trophy
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("user_id")]
        public int UserId { get; set; }

        [SerializationPropertyName("game_id")]
        public int GameId { get; set; }

        [SerializationPropertyName("game_trophy_id")]
        public int GameTrophyId { get; set; }

        [SerializationPropertyName("logged_on")]
        public long? LoggedOn { get; set; }

        [SerializationPropertyName("viewed_on")]
        public long? ViewedOn { get; set; }

        [SerializationPropertyName("game_trophy")]
        public GameTrophy GameTrophy { get; set; }

        [SerializationPropertyName("user")]
        public User User { get; set; }

        [SerializationPropertyName("game")]
        public Game Game { get; set; }

        [SerializationPropertyName("site_trophy_id")]
        public int? SiteTrophyId { get; set; }

        [SerializationPropertyName("site_trophy")]
        public SiteTrophy SiteTrophy { get; set; }
    }
}
