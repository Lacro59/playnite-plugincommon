using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
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

        [SerializationPropertyName("is_achieved")]
        public bool IsAchieved { get; set; }
    }
}
