using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
    public class AvatarFrame
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("image_url")]
        public string ImageUrl { get; set; }
    }
}
