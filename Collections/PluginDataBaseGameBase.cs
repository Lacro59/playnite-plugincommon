using Newtonsoft.Json;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginCommon.Collections
{
    public class PluginDataBaseGameBase : DatabaseObject
    {
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                return _Genres;
            }

            set
            {
                _Genres = value;
                OnPropertyChanged();
            }
        }

        private List<Guid> genreIds;
        /// <summary>
        /// Gets or sets list of genres.
        /// </summary>
        [JsonIgnore]
        public List<Guid> GenreIds
        {
            get
            {
                return genreIds;
            }

            set
            {
                genreIds = value;
                OnPropertyChanged();
            }
        }
    }
}
