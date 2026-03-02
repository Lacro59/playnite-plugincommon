using CommonPluginsShared.Commands;
using CommonPluginsShared.Models;
using LiteDB;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;

namespace CommonPluginsShared.Collections
{
	/// <summary>
	/// Base class for plugin-related game data, offering access to Playnite game information,
	/// common metadata, and shared cache infrastructure.
	/// </summary>
	public class PluginGameEntry : DatabaseObject
	{
		#region Cache fields

		/// <summary>Backing nullable cache for <see cref="HasData"/>. Null = invalidated.</summary>
		protected bool? _hasData;

		/// <summary>Backing nullable cache for <see cref="Count"/>. Null = invalidated.</summary>
		protected ulong? _count;

		#endregion

		#region Data

		/// <summary>
		/// Gets or sets the date and time when the data was last refreshed.
		/// </summary>
		public DateTime DateLastRefresh { get; set; } = default;

		/// <summary>
		/// Indicates whether the game has been marked as deleted.
		/// </summary>
		[DontSerialize]
		[BsonIgnore]
		public bool IsDeleted { get; set; }

		/// <summary>
		/// Indicates whether the plugin data has been saved.
		/// </summary>
		[DontSerialize]
		[BsonIgnore]
		public bool IsSaved { get; set; }

		/// <summary>
		/// Indicates whether this instance contains meaningful plugin data.
		/// Uses cached value for better performance.
		/// </summary>
		[DontSerialize]
		[BsonIgnore]
		public virtual bool HasData => false;

		/// <summary>
		/// Gets the count of elements associated with the plugin data.
		/// Uses cached value for better performance.
		/// </summary>
		[DontSerialize]
		[BsonIgnore]
		public virtual ulong Count => 0;

		private SourceLink _sourcesLink;

		/// <summary>
		/// Gets or sets the URL of the source from which this plugin's data was scraped.
		/// Common to all plugins; defaults to <c>null</c> when not applicable.
		/// </summary>
		public SourceLink SourcesLink
		{
			get => _sourcesLink;
			set => SetValue(ref _sourcesLink, value);
		}

		#endregion

		/// <summary>
		/// Indicates whether the referenced Playnite game exists in the database.
		/// </summary>
		public bool GameExist => Game != null;

		/// <summary>
		/// Command to navigate to the associated game in Playnite.
		/// </summary>
		[DontSerialize]
		[BsonIgnore]
		public RelayCommand<Guid> GoToGame => CommandsNavigation.GoToGame;

		#region Game data

		/// <summary>
		/// Gets the Playnite game associated with this plugin data instance.
		/// </summary>
		[DontSerialize]
		[BsonIgnore]
		internal Game Game => API.Instance.Database.Games.Get(Id);

		[DontSerialize][BsonIgnore] public Guid SourceId => Game?.SourceId ?? default;
		[DontSerialize][BsonIgnore] public DateTime? LastActivity => Game?.LastActivity;
		[DontSerialize][BsonIgnore] public bool Hidden => Game?.Hidden ?? default;
		[DontSerialize][BsonIgnore] public string Icon => Game?.Icon ?? string.Empty;
		[DontSerialize][BsonIgnore] public string CoverImage => Game?.CoverImage ?? string.Empty;
		[DontSerialize][BsonIgnore] public string BackgroundImage => Game?.BackgroundImage ?? string.Empty;
		[DontSerialize][BsonIgnore] public List<Genre> Genres => Game?.Genres;
		[DontSerialize][BsonIgnore] public List<Tag> Tags => Game?.Tags;
		[DontSerialize][BsonIgnore] public List<Guid> GenreIds => Game?.GenreIds;
		[DontSerialize][BsonIgnore] public List<Platform> Platforms => Game?.Platforms;
		[DontSerialize][BsonIgnore] public ulong Playtime => Game?.Playtime ?? default;
		[DontSerialize][BsonIgnore] public ulong PlayCount => Game?.PlayCount ?? default;
		[DontSerialize][BsonIgnore] public bool Favorite => Game?.Favorite ?? default;
		[DontSerialize][BsonIgnore] public GameSource Source => Game?.Source;
		[DontSerialize][BsonIgnore] public bool IsInstalled => Game?.IsInstalled ?? default;

		#endregion

		/// <summary>
		/// Invalidates <see cref="HasData"/> and <see cref="Count"/> caches.
		/// Called by <see cref="PluginGameCollection{T}"/> when Items changes.
		/// Override in derived classes to invalidate additional cached values.
		/// </summary>
		protected virtual void RefreshCachedValues()
		{
			_hasData = null;
			_count = null;
		}
	}
}