using Playnite.SDK.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonPluginsShared.Collections
{
    public class PluginDataBaseGameBase : DatabaseObject
    {
        [DontSerialize]
        internal Game Game { get; set; }        


        [DontSerialize]
        public Guid SourceId => Game == null ? default(Guid) : Game.SourceId; 

        [DontSerialize]
        public DateTime? LastActivity => Game?.LastActivity; 

        [DontSerialize]
        public bool Hidden => Game == null ? default(bool) : Game.Hidden; 

        [DontSerialize]
        public string Icon => Game?.Icon ?? string.Empty; 

        [DontSerialize]
        public string CoverImage => Game?.CoverImage ?? string.Empty; 

        [DontSerialize]
        public string BackgroundImage => Game?.BackgroundImage ?? string.Empty; 

        [DontSerialize]
        public List<Genre> Genres => Game?.Genres;

        [DontSerialize]
        public List<Guid> GenreIds => Game?.GenreIds;

        [DontSerialize]
        public List<Platform> Platforms => Game?.Platforms;

        [DontSerialize]
        public ulong Playtime => Game == null ? default(ulong) : Game.Playtime; 


        [DontSerialize]
        public bool IsDeleted { get; set; }

        [DontSerialize]
        public bool IsSaved { get; set; }


        [DontSerialize]
        public virtual bool HasData => false;


        [DontSerialize]
        public virtual int Count => 0;
    }
}
