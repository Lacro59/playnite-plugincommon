using CommonPluginsShared.Interfaces;
using Newtonsoft.Json;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsShared.Collections
{
    public class PluginDataBaseGameBase : DatabaseObject
    {
        [JsonIgnore]
        internal Game Game { get; set; }


        [JsonIgnore]
        public Guid SourceId { get { return Game.SourceId; } }

        [JsonIgnore]
        public DateTime? LastActivity { get { return Game.LastActivity; } }

        [JsonIgnore]
        public bool Hidden { get { return Game.Hidden; } }

        [JsonIgnore]
        public string Icon { get { return Game.Icon; } }

        [JsonIgnore]
        public string CoverImage { get { return Game.CoverImage; } }

        [JsonIgnore]
        public string BackgroundImage { get { return Game.BackgroundImage; } }

        [JsonIgnore]
        public List<Genre> Genres { get { return Game.Genres; } }

        [JsonIgnore]
        public List<Guid> GenreIds { get { return Game.GenreIds; } }

        [JsonIgnore]
        public Platform Platform { get { return Game.Platform; } }

        [JsonIgnore]
        public long Playtime { get { return Game.Playtime; } }


        [JsonIgnore]
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public bool IsSaved { get; set; }

        [JsonIgnore]
        public virtual bool HasData
        {
            get
            {
                return false;
            }
        }
    }
}
