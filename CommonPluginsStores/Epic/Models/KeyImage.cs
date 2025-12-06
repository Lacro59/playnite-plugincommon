using System;
using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models
{
    public class KeyImage
    {
        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("url")]
        public string Url { get; set; }

        [SerializationPropertyName("md5")]
        public string Md5 { get; set; }

        [SerializationPropertyName("width")]
        public int? Width { get; set; }

        [SerializationPropertyName("height")]
        public int? Height { get; set; }

        [SerializationPropertyName("size")]
        public int? Size { get; set; }

        [SerializationPropertyName("uploadedDate")]
        public DateTime? UploadedDate { get; set; }
    }
}
