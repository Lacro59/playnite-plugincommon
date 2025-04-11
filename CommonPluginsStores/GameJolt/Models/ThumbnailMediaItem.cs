using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
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
}
