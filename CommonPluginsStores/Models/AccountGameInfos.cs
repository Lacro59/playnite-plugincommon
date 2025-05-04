using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace CommonPluginsStores.Models
{
    public class AccountGameInfos : BasicAccountGameInfos
    {
        public bool IsCommun { get; set; }
        public int Playtime { get; set; }

        public int AchievementsUnlocked => Achievements?.Where(y => y.DateUnlocked != default)?.Count() ?? 0;
        public ObservableCollection<GameAchievement> Achievements { get; set; }

        public DateTime LastCall => DateTime.Now;
    }
}
