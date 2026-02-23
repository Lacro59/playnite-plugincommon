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
	/// All methods operate on <see cref="PluginDataBaseGameBase"/>.
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
		PluginDataBaseGameBase Get(Game game, bool onlyCache = false, bool force = false);

		/// <summary>Get the plugin data for a game.</summary>
		PluginDataBaseGameBase Get(Guid id, bool onlyCache = false, bool force = false);

		/// <summary>Get a clone of the plugin data for a game.</summary>
		PluginDataBaseGameBase GetClone(Guid id);

		/// <summary>Get a clone of the plugin data for a game.</summary>
		PluginDataBaseGameBase GetClone(Game game);

		/// <summary>Merge data from one game to another.</summary>
		PluginDataBaseGameBase MergeData(Guid fromId, Guid toId);

		/// <summary>Remove data for a game.</summary>
		bool Remove(Game game);

		/// <summary>Remove data for a game.</summary>
		bool Remove(Guid id);

		/// <summary>Remove data for a list of games.</summary>
		void Remove(List<Guid> ids);

		/// <summary>Add or update data for a game.</summary>
		void AddOrUpdate(PluginDataBaseGameBase item);

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

		/// <summary>Clear all data from the database.</summary>
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

		bool ExtractToCsv();
	}


	/// <summary>
	/// Generic interface inheriting from <see cref="IPluginDatabase"/>.
	/// Only adds members requiring knowledge of <typeparamref name="TItem"/>.
	/// To be used when the concrete type of the item is known (e.g. plugin implementations).
	/// </summary>
	/// <typeparam name="TItem">Database item type inheriting <see cref="PluginDataBaseGameBase"/>.</typeparam>
	public interface IPluginDatabase<TItem> : IPluginDatabase where TItem : PluginDataBaseGameBase
	{

	}
}