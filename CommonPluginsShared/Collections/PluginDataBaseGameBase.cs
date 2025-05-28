using Playnite.SDK.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using Playnite.SDK;

namespace CommonPluginsShared.Collections
{
    /// <summary>
    /// Base class for plugin-related game data, offering access to Playnite game information and common metadata.
    /// </summary>
    public class PluginDataBaseGameBase : DatabaseObject
    {
        #region Data

        /// <summary>
        /// Gets or sets the date and time when the data was last refreshed.
        /// </summary>
        public DateTime DateLastRefresh { get; set; } = default;

        /// <summary>
        /// Indicates whether the game has been marked as deleted.
        /// </summary>
        [DontSerialize]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Indicates whether the plugin data has been saved.
        /// </summary>
        [DontSerialize]
        public bool IsSaved { get; set; }

        /// <summary>
        /// Indicates whether this instance contains meaningful plugin data.
        /// </summary>
        [DontSerialize]
        public virtual bool HasData => false;

        /// <summary>
        /// Gets the count of elements associated with the plugin data.
        /// </summary>
        [DontSerialize]
        public virtual ulong Count => 0;

        #endregion

        /// <summary>
        /// Indicates whether the referenced Playnite game exists in the database.
        /// </summary>
        public bool GameExist => Game != null;

        /// <summary>
        /// Command to navigate to the associated game in Playnite.
        /// </summary>
        [DontSerialize]
        public RelayCommand<Guid> GoToGame => Commands.GoToGame;

        #region Game data

        /// <summary>
        /// Gets the Playnite game associated with this plugin data instance.
        /// </summary>
        [DontSerialize]
        internal Game Game => API.Instance.Database.Games.Get(Id);

        /// <summary>
        /// Gets the source ID of the game (e.g., Steam, GOG).
        /// </summary>
        [DontSerialize]
        public Guid SourceId => Game?.SourceId ?? default;

        /// <summary>
        /// Gets the date and time of the last activity recorded for the game.
        /// </summary>
        [DontSerialize]
        public DateTime? LastActivity => Game?.LastActivity ?? null;

        /// <summary>
        /// Indicates whether the game is marked as hidden in Playnite.
        /// </summary>
        [DontSerialize]
        public bool Hidden => Game?.Hidden ?? default;

        /// <summary>
        /// Gets the icon path of the game.
        /// </summary>
        [DontSerialize]
        public string Icon => Game?.Icon ?? string.Empty;

        /// <summary>
        /// Gets the path to the cover image of the game.
        /// </summary>
        [DontSerialize]
        public string CoverImage => Game?.CoverImage ?? string.Empty;

        /// <summary>
        /// Gets the background image path of the game.
        /// </summary>
        [DontSerialize]
        public string BackgroundImage => Game?.BackgroundImage ?? string.Empty;

        /// <summary>
        /// Gets the list of genres assigned to the game.
        /// </summary>
        [DontSerialize]
        public List<Genre> Genres => Game?.Genres ?? default;

        /// <summary>
        /// Gets the list of tags assigned to the game.
        /// </summary>
        [DontSerialize]
        public List<Tag> Tags => Game?.Tags ?? default;

        /// <summary>
        /// Gets the list of genre IDs assigned to the game.
        /// </summary>
        [DontSerialize]
        public List<Guid> GenreIds => Game?.GenreIds ?? default;

        /// <summary>
        /// Gets the list of platforms the game is available on.
        /// </summary>
        [DontSerialize]
        public List<Platform> Platforms => Game?.Platforms ?? default;

        /// <summary>
        /// Gets the total recorded playtime of the game, in minutes.
        /// </summary>
        [DontSerialize]
        public ulong Playtime => Game?.Playtime ?? default;

        /// <summary>
        /// Gets the number of times the game has been launched.
        /// </summary>
        [DontSerialize]
        public ulong PlayCount => Game?.PlayCount ?? default;

        /// <summary>
        /// Indicates whether the game is marked as a favorite in Playnite.
        /// </summary>
        [DontSerialize]
        public bool Favorite => Game?.Favorite ?? default;

        /// <summary>
        /// Gets the game source (e.g., Steam, Epic, GOG) object.
        /// </summary>
        [DontSerialize]
        public GameSource Source => Game?.Source ?? default;

        /// <summary>
        /// Indicates whether the game is currently installed on the system.
        /// </summary>
        [DontSerialize]
        public bool IsInstalled => Game?.IsInstalled ?? default;

        #endregion
    }
}