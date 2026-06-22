using CommonPluginsShared.Commands;
using CommonPluginsShared.Models;
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
		public bool IsDeleted { get; set; }

		/// <summary>
		/// Indicates whether the plugin data has been saved.
		/// </summary>
		[DontSerialize]
		public bool IsSaved { get; set; }

		/// <summary>
		/// Indicates whether this instance contains meaningful plugin data.
		/// Uses cached value for better performance.
		/// </summary>
		[DontSerialize]
		public virtual bool HasData => false;

		/// <summary>
		/// Gets the count of elements associated with the plugin data.
		/// Uses cached value for better performance.
		/// </summary>
		[DontSerialize]
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
		public RelayCommand<Guid> GoToGame => CommandsNavigation.GoToGame;

		#region Game data

		/// <summary>
		/// Gets the Playnite game associated with this plugin data instance.
		/// </summary>
		[DontSerialize]
		internal Game Game => API.Instance.Database.Games.Get(Id);

		[DontSerialize] public Guid SourceId => Game?.SourceId ?? default;
		[DontSerialize] public DateTime? LastActivity => Game?.LastActivity;
		[DontSerialize] public bool Hidden => Game?.Hidden ?? default;
		[DontSerialize] public string Icon => Game?.Icon ?? string.Empty;
		[DontSerialize] public string CoverImage => Game?.CoverImage ?? string.Empty;
		[DontSerialize] public string BackgroundImage => Game?.BackgroundImage ?? string.Empty;
		[DontSerialize] public List<Genre> Genres => Game?.Genres;
		[DontSerialize] public List<Tag> Tags => Game?.Tags;
		[DontSerialize] public List<Guid> GenreIds => Game?.GenreIds;
		[DontSerialize] public List<Platform> Platforms => Game?.Platforms;
		[DontSerialize] public ulong Playtime => Game?.Playtime ?? default;
		[DontSerialize] public ulong PlayCount => Game?.PlayCount ?? default;
		[DontSerialize] public bool Favorite => Game?.Favorite ?? default;
		[DontSerialize] public GameSource Source => Game?.Source;
		[DontSerialize] public bool IsInstalled => Game?.IsInstalled ?? default;

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