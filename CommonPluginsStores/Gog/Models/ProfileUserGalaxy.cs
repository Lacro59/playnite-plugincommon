using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Gog.Models
{
    public class ProfileUserGalaxy
    {
        [SerializationPropertyName("id")]
        public long Id { get; set; }

        [SerializationPropertyName("username")]
        public string Username { get; set; }

        [SerializationPropertyName("created_date")]
        public DateTime CreatedDate { get; set; }

        [SerializationPropertyName("avatar")]
        public Avatar Avatar { get; set; }

        [SerializationPropertyName("is_employee")]
        public bool IsEmployee { get; set; }

        [SerializationPropertyName("tags")]
        public List<object> Tags { get; set; }
    }

    public class Avatar
    {
        [SerializationPropertyName("gog_image_id")]
        public string GogImageId { get; set; }

        [SerializationPropertyName("small")]
        public string Small { get; set; }

        [SerializationPropertyName("small_2x")]
        public string Small2x { get; set; }

        [SerializationPropertyName("medium")]
        public string Medium { get; set; }

        [SerializationPropertyName("medium_2x")]
        public string Medium2x { get; set; }

        [SerializationPropertyName("large")]
        public string Large { get; set; }

        [SerializationPropertyName("large_2x")]
        public string Large2x { get; set; }

        [SerializationPropertyName("sdk_img_32")]
        public string SdkImg32 { get; set; }

        [SerializationPropertyName("sdk_img_64")]
        public string SdkImg64 { get; set; }

        [SerializationPropertyName("sdk_img_184")]
        public string SdkImg184 { get; set; }

        [SerializationPropertyName("menu_small")]
        public string MenuSmall { get; set; }

        [SerializationPropertyName("menu_small_2")]
        public string MenuSmall2 { get; set; }

        [SerializationPropertyName("menu_big")]
        public string MenuBig { get; set; }

        [SerializationPropertyName("menu_big_2")]
        public string MenuBig2 { get; set; }
    }
}
