using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
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
}
