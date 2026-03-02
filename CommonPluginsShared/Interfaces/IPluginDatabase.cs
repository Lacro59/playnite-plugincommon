using CommonPluginsShared.Collections;
using CommonPluginsShared.Models;
using CommonPluginsShared.Plugins;
using CommonPluginsShared.Services;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommonPluginsShared.Interfaces
{
	/// <summary>
	/// Non-generic interface for contexts where <c>TItem</c> is not accessible
	/// (e.g. shared services, generic views, injection without type constraints).
	/// All methods operate on <see cref="PluginGameEntry"/>.
	/// </summary>
	public interface IPluginDatabase
	{
		/// <summary>Gets or sets the plugin name used for logging and file naming.</summary>
		string PluginName { get; set; }

		/// <summary>Gets or sets plugin-specific paths (database, cache, installation directory).</summary>
		PluginPaths Paths { get; set; }

		/// <summary>Gets or sets a value indicating whether the database has finished loading.</summary>
		bool IsLoaded { get; set; }

		/// <summary>Gets or sets the plugin windows helper.</summary>
		IPluginWindows PluginWindows { get; set; }

		/// <summary>Initialize the database.</summary>
		Task<bool> InitializeDatabase();

		/// <summary>Get the plugin data for a game.</summary>
		PluginGameEntry Get(Game game, bool onlyCache = false, bool force = false);

		/// <summary>Get the plugin data for a game.</summary>
		PluginGameEntry Get(Guid id, bool onlyCache = false, bool force = false);

		/// <summary>
		/// Returns the in-memory cached item without any disk or web access.
		/// Returns null if the item has never been loaded this session.
		/// </summary>
		PluginGameEntry GetOnlyCache(Guid id);

		/// <summary>
		/// Returns the in-memory cached item without any disk or web access.
		/// Returns null if the item has never been loaded this session.
		/// </summary>
		PluginGameEntry GetOnlyCache(Game game);

		/// <summary>Get a clone of the plugin data for a game.</summary>
		PluginGameEntry GetClone(Guid id);

		/// <summary>Get a clone of the plugin data for a game.</summary>
		PluginGameEntry GetClone(Game game);

		/// <summary>Merge data from one game to another.</summary>
		PluginGameEntry MergeData(Guid fromId, Guid toId);

		/// <summary>Remove data for a game.</summary>
		bool Remove(Game game);

		/// <summary>Remove data for a game.</summary>
		bool Remove(Guid id);

		/// <summary>Remove data for a list of games.</summary>
		void Remove(List<Guid> ids);

		/// <summary>Add or update data for a game.</summary>
		void AddOrUpdate(PluginGameEntry item);

		/// <summary>Refresh data for a game (reload from disk/source).</summary>
		void Refresh(Guid id);

		/// <summary>Refresh data for a list of games.</summary>
		void Refresh(IEnumerable<Guid> ids);

		/// <summary>Refresh data for a list of games without loading data.</summary>
		[Obsolete("Use Refresh(ids)")]
		void RefreshWithNoData(IEnumerable<Guid> ids);

		/// <summary>Get list of games with no data in the database.</summary>
		IEnumerable<Game> GetGamesWithNoData();

		/// <summary>Get list of games with data older than x months.</summary>
		IEnumerable<Game> GetGamesOldData(int months);

		/// <summary>
		/// Deletes all temporary files stored by this plugin (downloaded images, web responses, etc.).
		/// Plugin data and the Playnite library are not affected.
		/// </summary>
		/// <returns><c>true</c> if all cache directories were deleted successfully; <c>false</c> if any error occurred.</returns>
		bool ClearCache();

		/// <summary>
		/// Removes all plugin data entries for every game in the library, then clears the plugin cache.
		/// This is a combined destructive action: both stored plugin data and temporary cache files are deleted.
		/// </summary>
		/// <returns><c>true</c> if both operations completed without error; <c>false</c> otherwise.</returns>
		bool ClearDatabase();

		/// <summary>Get the selection data for the plugin (e.g. for filter views).</summary>
		void GetSelectData();

		/// <summary>Add tag selection data.</summary>
		void AddTagSelectData();

		/// <summary>Add tags to all games in the database.</summary>
		void AddTagAllGames();

		/// <summary>Remove tags from all games in the database.</summary>
		void RemoveTagAllGames(bool fromClearDatabase = false);

		/// <summary>Returns a projection of all database entries as <see cref="DataGame"/> view models.</summary>
		IEnumerable<DataGame> GetDataGames();

		/// <summary>Returns database entries that are marked as deleted (their Playnite game was removed).</summary>
		IEnumerable<DataGame> GetIsolatedDataGames();

		/// <summary>Exports all database entries to a CSV file.</summary>
		bool ExtractToCsv();

		/// <summary>Returns true if the database is ready for immediate access (loaded and non-null).</summary>
		bool IsDatabaseReady();
	}

	/// <summary>
	/// Generic interface inheriting from <see cref="IPluginDatabase"/>.
	/// Only adds members requiring knowledge of <typeparamref name="TItem"/>.
	/// To be used when the concrete type of the item is known (e.g. plugin implementations).
	/// </summary>
	/// <typeparam name="TItem">Database item type inheriting <see cref="PluginGameEntry"/>.</typeparam>
	public interface IPluginDatabase<TItem> : IPluginDatabase where TItem : PluginGameEntry
	{
		/// <summary>Gets the strongly-typed plugin data for a game.</summary>
		new TItem Get(Game game, bool onlyCache = false, bool force = false);

		/// <summary>Gets the strongly-typed plugin data for a game.</summary>
		new TItem Get(Guid id, bool onlyCache = false, bool force = false);

		/// <summary>Returns the strongly-typed in-memory cached item without any disk or web access.</summary>
		new TItem GetOnlyCache(Guid id);

		/// <summary>Returns the strongly-typed in-memory cached item without any disk or web access.</summary>
		new TItem GetOnlyCache(Game game);

		/// <summary>Gets a strongly-typed deep clone of the plugin data for a game.</summary>
		new TItem GetClone(Guid id);

		/// <summary>Gets a strongly-typed deep clone of the plugin data for a game.</summary>
		new TItem GetClone(Game game);

		/// <summary>Adds or updates strongly-typed data for a game.</summary>
		void AddOrUpdate(TItem item);
	}
}