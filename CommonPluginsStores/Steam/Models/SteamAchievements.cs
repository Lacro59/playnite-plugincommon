using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Steam.Models
{
    public class SteamAchievements
    {
        public SteamAchievementsResponse response { get; set; }
    }

    public class Achievement
    {
        public string internal_name { get; set; }
        public string localized_name { get; set; }
        public string localized_desc { get; set; }
        public string icon { get; set; }
        public string icon_gray { get; set; }
        public bool hidden { get; set; }
        public string player_percent_unlocked { get; set; }
    }

    public class SteamAchievementsResponse
    {
        public List<Achievement> achievements { get; set; }
    }
}
