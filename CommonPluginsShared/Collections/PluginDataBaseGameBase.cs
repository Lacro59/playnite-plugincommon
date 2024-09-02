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
        public DateTime DateLastRefresh { get; set; } = default;

        [DontSerialize]
        internal Game Game { get; set; }


        [DontSerialize]
        public Guid SourceId => Game?.SourceId ?? default;

        [DontSerialize]
        public DateTime? LastActivity => Game?.LastActivity ?? null;

        [DontSerialize]
        public bool Hidden => Game?.Hidden ?? default;

        [DontSerialize]
        public string Icon => Game?.Icon ?? string.Empty;

        [DontSerialize]
        public string CoverImage => Game?.CoverImage ?? string.Empty;

        [DontSerialize]
        public string BackgroundImage => Game?.BackgroundImage ?? string.Empty;

        [DontSerialize]
        public List<Genre> Genres => Game?.Genres ?? default;

        [DontSerialize]
        public List<Tag> Tags => Game?.Tags ?? default;

        [DontSerialize]
        public List<Guid> GenreIds => Game?.GenreIds ?? default;

        [DontSerialize]
        public List<Platform> Platforms => Game?.Platforms ?? default;

        [DontSerialize]
        public ulong Playtime => Game?.Playtime ?? default;

        [DontSerialize]
        public bool Favorite => Game?.Favorite ?? default;

        [DontSerialize]
        public GameSource Source => Game?.Source ?? default;


        [DontSerialize]
        public bool IsInstalled => Game?.IsInstalled ?? default;

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
