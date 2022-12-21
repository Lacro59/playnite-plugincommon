using Playnite.SDK;
//using PSNLibrary.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace CommonPlayniteShared.PluginLibrary.PSNLibrary.Models
{
    public class PSNLibrarySettings : ObservableObject
    {
        public bool ConnectAccount { get; set; } = true;
        public bool DownloadImageMetadata { get; set; } = true;
        public bool LastPlayed { get; set; } = false;
        public bool Playtime { get; set; } = false;
        public bool PS3 { get; set; } = true;
        public bool PSP { get; set; } = true;
        public bool PSVITA { get; set; } = true;
        public bool Migration { get; set; } = true;

        private string npsso = null;
        public string Npsso { get => npsso; set => SetValue(ref npsso, value); }
    }
}
