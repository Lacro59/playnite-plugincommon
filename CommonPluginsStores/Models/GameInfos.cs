using CommonPlayniteShared;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CommonPluginsStores.Models
{
    /// <summary>
    /// Represents detailed information about a game, including a list of downloadable content (DLC).
    /// Inherits from <see cref="BasicGameInfos"/> to include basic game details.
    /// </summary>
    public class GameInfos : BasicGameInfos
    {
        private ObservableCollection<DlcInfos> _dlcs;

        /// <summary>
        /// Gets or sets the list of downloadable content (DLC) associated with the game.
        /// </summary>
        public ObservableCollection<DlcInfos> Dlcs { get => _dlcs; set => _dlcs = value; }
    }
}
