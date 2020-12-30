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
        private Guid _SourceId;
        /// <summary>
        /// Gets or sets source of the game.
        /// </summary>
        [JsonIgnore]
        public Guid SourceId
        {
            get
            {
                return _SourceId;
            }

            set
            {
                _SourceId = value;
            }
        }

        private DateTime? _LastActivity;
        /// <summary>
        /// Gets or sets last played date.
        /// </summary>
        [JsonIgnore]
        public DateTime? LastActivity
        {
            get
            {
                return _LastActivity;
            }

            set
            {
                _LastActivity = value;
            }
        }

        private bool _Hidden;
        /// <summary>
        /// Gets or sets value indicating if the game is hidden in library.
        /// </summary>
        [JsonIgnore]
        public bool Hidden
        {
            get
            {
                return _Hidden;
            }

            set
            {
                _Hidden = value;
            }
        }

        private string _Icon;
        /// <summary>
        /// Gets or sets game icon. Local file path, HTTP URL or database file ids are supported.
        /// </summary>
        [JsonIgnore]
        public string Icon
        {
            get
            {
                return _Icon;
            }

            set
            {
                _Icon = value;
            }
        }

        private string _CoverImage;
        /// <summary>
        /// Gets or sets game cover image. Local file path, HTTP URL or database file ids are supported.
        /// </summary>
        [JsonIgnore]
        public string CoverImage
        {
            get
            {
                return _CoverImage;
            }

            set
            {
                _CoverImage = value;
            }
        }

        private List<Genre> _Genres;
        /// <summary>
        /// Gets game's genres.
        /// </summary>
        [JsonIgnore]
        public List<Genre> Genres
        {
            get
            {
                if (_Genres == null)
                {
                    return new List<Genre>();
                }
                return _Genres;
            }

            set
            {
                _Genres = value;
            }
        }

        private List<Guid> _GenreIds;
        /// <summary>
        /// Gets or sets list of genres.
        /// </summary>
        [JsonIgnore]
        public List<Guid> GenreIds
        {
            get
            {
                if (_GenreIds == null)
                {
                    return new List<Guid>();
                }
                return _GenreIds;
            }

            set
            {
                _GenreIds = value;
            }
        }

        private long _Playtime;
        /// <summary>
        /// Gets or sets played time in seconds.
        /// </summary>
        [JsonIgnore]
        public long Playtime
        {
            get
            {
                return _Playtime;
            }

            set
            {
                _Playtime = value;
            }
        }

        private bool _IsDeleted;
        /// <summary>
        /// Gets or sets value indicating if the game is deleted in library.
        /// </summary>
        [JsonIgnore]
        public bool IsDeleted
        {
            get
            {
                return _IsDeleted;
            }

            set
            {
                _IsDeleted = value;
            }
        }

        private bool _IsSaved;
        /// <summary>
        /// Gets or sets value indicating if the game is saved in disk.
        /// </summary>
        [JsonIgnore]
        public bool IsSaved
        {
            get
            {
                return _IsSaved;
            }

            set
            {
                _IsSaved = value;
            }
        }
    }
}
