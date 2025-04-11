using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
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
}
