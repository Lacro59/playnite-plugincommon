using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Steam.Models.SteamKit
{
    public class SteamOwnedGame
    {
        public int Appid { get; set; }
        public string Name { get; set; }
        public string ImgIconUrl { get; set; }
        public bool HasCommunityVisibleStats { get; set; }
        public int PlaytimeForever { get; set; }
        public int Playtime2weeks { get; set; }
        public int PlaytimeWindowsForever { get; set; }
        public int PlaytimeMacForever { get; set; }
        public int PlaytimeLinuxForever { get; set; }
        public int PlaytimeDeckForever { get; set; }
        public DateTime RtimeLastPlayed { get; set; }
        public int PlaytimeDisconnected { get; set; }
        public string CapsuleFilename { get; set; }
        public bool HasWorkshop { get; set; }
        public bool HasMarket { get; set; }
        public bool HasDlc { get; set; }
        public bool HasLeaderboards { get; set; }
    }
}
