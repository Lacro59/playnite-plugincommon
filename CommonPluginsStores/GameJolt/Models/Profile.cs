using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
    public class Profile
    {
        [SerializationPropertyName("payload")]
        public PayloadProfile Payload { get; set; }

        [SerializationPropertyName("ver")]
        public string Ver { get; set; }

        [SerializationPropertyName("user")]
        public User User { get; set; }

        [SerializationPropertyName("c")]
        public C C { get; set; }

        [SerializationPropertyName("t")]
        public List<object> T { get; set; }
    }

    public class AvatarMediaItem
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
        public int CropStartX { get; set; }

        [SerializationPropertyName("crop_start_y")]
        public int CropStartY { get; set; }

        [SerializationPropertyName("crop_end_x")]
        public int CropEndX { get; set; }

        [SerializationPropertyName("crop_end_y")]
        public int CropEndY { get; set; }

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

    public class Dogtag
    {
        [SerializationPropertyName("text")]
        public string Text { get; set; }

        [SerializationPropertyName("ids")]
        public List<int> Ids { get; set; }
    }

    public class PayloadProfile
    {
        [SerializationPropertyName("user")]
        public User User { get; set; }

        [SerializationPropertyName("userFriendship")]
        public object UserFriendship { get; set; }

        [SerializationPropertyName("previewTrophies")]
        public List<PreviewTrophy> PreviewTrophies { get; set; }

        [SerializationPropertyName("placeholderCommunitiesCount")]
        public int PlaceholderCommunitiesCount { get; set; }

        [SerializationPropertyName("communitiesCount")]
        public int CommunitiesCount { get; set; }

        [SerializationPropertyName("trophyCount")]
        public int TrophyCount { get; set; }
    }

    public class PreviewTrophy
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
        public object LoggedOn { get; set; }

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
