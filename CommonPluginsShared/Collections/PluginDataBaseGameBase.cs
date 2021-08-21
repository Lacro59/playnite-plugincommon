using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsShared.Collections
{
    public class PluginDataBaseGameBase : DatabaseObject
    {
        [DontSerialize]
        internal Game Game { get; set; }


        [DontSerialize]
        public Guid SourceId { get { return Game == null ? default(Guid) : Game.SourceId; } }

        [DontSerialize]
        public DateTime? LastActivity { get { return Game?.LastActivity; } }

        [DontSerialize]
        public bool Hidden { get { return Game.Hidden; } }

        [DontSerialize]
        public string Icon { get { return Game?.Icon == null ? string.Empty : Game.Icon; } }

        [DontSerialize]
        public string CoverImage { get { return Game.CoverImage; } }

        [DontSerialize]
        public string BackgroundImage { get { return Game.BackgroundImage; } }

        [DontSerialize]
        public List<Genre> Genres { get { return Game.Genres; } }

        [DontSerialize]
        public List<Guid> GenreIds { get { return Game.GenreIds; } }

        [DontSerialize]
        public Platform Platform { get { return Game.Platform; } }

        [DontSerialize]
        public long Playtime { get { return Game.Playtime; } }


        [DontSerialize]
        public bool IsDeleted { get; set; }

        [DontSerialize]
        public bool IsSaved { get; set; }


        [DontSerialize]
        public virtual bool HasData
        {
            get
            {
                return false;
            }
        }
    }
}
