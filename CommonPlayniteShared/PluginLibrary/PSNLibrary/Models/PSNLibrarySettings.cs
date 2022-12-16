using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPlayniteShared.PluginLibrary.PSNLibrary.Models
{
    public class PSNLibrarySettings
    {
        public bool ConnectAccount { get; set; } = true;
        public bool DownloadImageMetadata { get; set; } = true;
        public bool LastPlayed { get; set; } = false;
        public bool Playtime { get; set; } = false;
        public bool PS3 { get; set; } = true;
        public bool PSP { get; set; } = true;
        public bool PSVITA { get; set; } = true;
        public bool Migration { get; set; } = true;
        public string Npsso { get; set; } = null;
    }
}
