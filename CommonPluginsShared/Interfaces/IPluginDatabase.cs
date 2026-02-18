using CommonPluginsShared.Collections;
using CommonPluginsShared.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommonPluginsShared.Interfaces
{
    public interface IPluginDatabase
    {
        string PluginName { get; set; }

        bool IsLoaded { get; set; }

        IWindowPluginService WindowPluginService { get; set; }

        /// <summary>
        /// Initialize the database.
        /// </summary>
        /// <returns></returns>
        Task<bool> InitializeDatabase();

        /// <summary>
        /// Get the plugin data for a game.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="onlyCache"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        PluginDataBaseGameBase Get(Game game, bool onlyCache = false, bool force = false);

        /// <summary>
        /// Get the plugin data for a game.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="onlyCache"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        PluginDataBaseGameBase Get(Guid id, bool onlyCache = false, bool force = false);

        /// <summary>
        /// Get a clone of the plugin data for a game.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        PluginDataBaseGameBase GetClone(Guid id);

        /// <summary>
        /// Get a clone of the plugin data for a game.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        PluginDataBaseGameBase GetClone(Game game);

        /// <summary>
        /// Merge data from one game to another.
        /// </summary>
        /// <param name="fromId"></param>
        /// <param name="toId"></param>
        /// <returns></returns>
        PluginDataBaseGameBase MergeData(Guid fromId, Guid toId);

        /// <summary>
        /// Remove data for a game.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        bool Remove(Game game);

        /// <summary>
        /// Remove data for a game.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool Remove(Guid id);

        /// <summary>
        /// Remove data for a list of games.
        /// </summary>
        /// <param name="ids"></param>
        void Remove(List<Guid> ids);

        /// <summary>
        /// Add or update data for a game.
        /// </summary>
        /// <param name="item"></param>
        void AddOrUpdate(PluginDataBaseGameBase item);

        /// <summary>
        /// Refresh data for a game (reload from disk/source).
        /// </summary>
        /// <param name="id"></param>
        void Refresh(Guid id);

        /// <summary>
        /// Refresh data for a list of games.
        /// </summary>
        /// <param name="ids"></param>
        void Refresh(IEnumerable<Guid> ids);

        /// <summary>
        /// Refresh data for a list of games without loading data.
        /// </summary>
        /// <param name="ids"></param>
        [Obsolete("Use Refresh(ids)")]
        void RefreshWithNoData(IEnumerable<Guid> ids);

        /// <summary>
        /// Get list of games with no data in the database.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Game> GetGamesWithNoData();

        /// <summary>
        /// Get list of games with data older than x months.
        /// </summary>
        /// <param name="months"></param>
        /// <returns></returns>
        IEnumerable<Game> GetGamesOldData(int months);

        /// <summary>
        /// Clear all data from the database.
        /// </summary>
        /// <returns></returns>
        bool ClearDatabase();

        /// <summary>
        /// Get the selection data for the plugin (e.g. for filter views).
        /// </summary>
        void GetSelectData();

        /// <summary>
        /// Add tag selection data.
        /// </summary>
        void AddTagSelectData();

        /// <summary>
        /// Add tags to all games in the database.
        /// </summary>
        void AddTagAllGames();

        /// <summary>
        /// Remove tags from all games in the database.
        /// </summary>
        /// <param name="fromClearDatabase"></param>
        void RemoveTagAllGames(bool fromClearDatabase = false);
    }
}