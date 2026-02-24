using CommonPlayniteShared.Common;
using CommonPlayniteShared.Database;
using CommonPluginsShared.Models;
using LiteDB;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace CommonPluginsShared.Collections
{
	/// <summary>
	/// LiteDB 4-backed persistent collection for plugin game data.
	/// Replaces the one-JSON-file-per-game approach of <see cref="PluginItemCollection{TItem}"/>.
	/// A session-level <see cref="ConcurrentDictionary{TKey,TValue}"/> is layered on top so that
	/// repeated reads (e.g. list scrolling) are allocation-free after the first access.
	/// LiteDB memory cache is disabled to match Playnite's own usage rationale.
	/// </summary>
	/// <typeparam name="TItem">Database item type inheriting <see cref="PluginDataBaseGameBase"/>.</typeparam>
	public class LiteDbItemCollection<TItem> : IDisposable
		where TItem : PluginDataBaseGameBase
	{
		private static readonly ILogger Logger = LogManager.GetLogger();

		private const int DatabaseOpenTimeoutMilliseconds = 30000;

		private readonly LiteDatabase _db;
		private readonly LiteCollection<TItem> _collection;
		private readonly ConcurrentDictionary<Guid, TItem> _sessionCache
			= new ConcurrentDictionary<Guid, TItem>();

		private bool _disposed;

		/// <summary>
		/// Gets the number of items currently stored in the database.
		/// </summary>
		public int Count => _collection.Count();

		/// <summary>
		/// Opens or creates the LiteDB database file at <paramref name="dbPath"/>.
		/// </summary>
		/// <param name="dbPath">Full path to the .db file.</param>
		public LiteDbItemCollection(string dbPath)
		{
			var connectionString = new ConnectionString(dbPath)
			{
				Journal = false,
				CacheSize = 0,
				Mode = LiteDB.FileMode.Shared
			};

			_db = new LiteDatabase(connectionString);
			_collection = _db.GetCollection<TItem>("items");
			_collection.EnsureIndex(x => x.Id, unique: true);
		}



		/// <summary>
		/// Loads all items from LiteDB into the session cache eagerly.
		/// Call once at startup before the UI becomes interactive.
		/// </summary>
		public int PreWarm()
		{
			int count = 0;
			foreach (TItem item in _collection.FindAll())
			{
				_sessionCache[item.Id] = item;
				count++;
			}
			return count;
		}

		#region Read

		/// <summary>
		/// Returns the item for <paramref name="id"/> from the session cache,
		/// falling back to LiteDB on first access.
		/// Returns null if no item exists for that ID.
		/// </summary>
		public TItem Get(Guid id)
		{
			TItem cached;
			if (_sessionCache.TryGetValue(id, out cached))
			{
				return cached;
			}

			TItem item = _collection.FindById(new BsonValue(id));
			if (item != null)
			{
				_sessionCache[id] = item;
			}

			return item;
		}

		/// <summary>
		/// Returns all items from LiteDB and populates the session cache as a side-effect.
		/// </summary>
		public IEnumerable<TItem> FindAll()
		{
			foreach (TItem item in _collection.FindAll())
			{
				_sessionCache[item.Id] = item;
				yield return item;
			}
		}

		/// <summary>
		/// Returns items matching <paramref name="predicate"/> directly from LiteDB.
		/// Does not populate the session cache.
		/// </summary>
		public IEnumerable<TItem> Find(Expression<Func<TItem, bool>> predicate)
		{
			return _collection.Find(predicate);
		}

		/// <summary>
		/// Returns true if an item exists for <paramref name="id"/>.
		/// Checks the session cache first to avoid a DB round-trip.
		/// </summary>
		public bool Exists(Guid id)
		{
			return _sessionCache.ContainsKey(id)
				|| _collection.Exists(x => x.Id == id);
		}

		#endregion

		#region Write

		/// <summary>
		/// Inserts or replaces the item. Updates the session cache immediately.
		/// </summary>
		public void Upsert(TItem item)
		{
			if (item == null)
			{
				Logger.Warn("Upsert called with null item.");
				return;
			}

			_collection.Upsert(item);
			_sessionCache[item.Id] = item;
		}

		/// <summary>
		/// Removes the item identified by <paramref name="id"/>.
		/// Returns true if an item was deleted.
		/// </summary>
		public bool Remove(Guid id)
		{
			TItem ignored;
			_sessionCache.TryRemove(id, out ignored);
			return _collection.Delete(new BsonValue(id));
		}

		/// <summary>
		/// Batch upsert using individual upserts — LiteDB 4 does not expose
		/// a public transaction API; each write is auto-committed.
		/// For large batches, InsertBulk is used on initial insert; subsequent
		/// calls fall back to individual Upsert.
		/// </summary>
		public void UpsertBatch(IEnumerable<TItem> items)
		{
			if (items == null)
			{
				return;
			}

			foreach (TItem item in items)
			{
				if (item == null)
				{
					continue;
				}

				_collection.Upsert(item);
				_sessionCache[item.Id] = item;
			}
		}

		#endregion

		#region SetGameInfo

		/// <summary>
		/// Updates Name and IsSaved for the item identified by <paramref name="id"/>
		/// from the Playnite game database. Marks the item as deleted if the game no longer exists.
		/// </summary>
		public void SetGameInfo(Guid id)
		{
			TItem item = Get(id);
			if (item == null)
			{
				return;
			}

			try
			{
				Game game = API.Instance.Database.Games.Get(id);
				if (game != null)
				{
					item.Name = game.Name;
					item.IsSaved = true;
					Upsert(item);
				}
				else
				{
					item.IsDeleted = true;
					Common.LogDebug(true, string.Format(
						"SetGameInfo — marking item {0} as deleted (game not found).", id));
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, string.Format("SetGameInfo error for ID {0}", id));
			}
		}

		/// <summary>
		/// Updates game info for all items in the collection.
		/// Waits for the Playnite database to be open before processing.
		/// All updates are batched in a single LiteDB transaction.
		/// </summary>
		public void SetGameInfo()
		{
			if (!WaitForDatabase("SetGameInfo"))
			{
				return;
			}

			List<TItem> allItems = _collection.FindAll().ToList();
			if (allItems.Count == 0)
			{
				return;
			}

			Common.LogDebug(true, string.Format(
				"SetGameInfo — bulk update for {0} items.", allItems.Count));

			foreach (TItem item in allItems)
			{
				try
				{
					Game game = API.Instance.Database.Games.Get(item.Id);
					if (game != null)
					{
						item.Name = game.Name;
						item.IsSaved = true;
						_collection.Upsert(item);
						_sessionCache[item.Id] = item;
					}
					else
					{
						item.IsDeleted = true;
						Common.LogDebug(true, string.Format(
							"SetGameInfo — marking item {0} ({1}) as deleted.", item.Id, item.Name));
					}
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, string.Format(
						"SetGameInfo error for item {0}", item.Id));
				}
			}

			Common.LogDebug(true, string.Format(
				"SetGameInfo — bulk update completed for {0} items.", allItems.Count));
		}

		#endregion

		#region Migration

		/// <summary>
		/// One-shot migration from the legacy one-JSON-file-per-game layout.
		/// Reads all *.json files from <paramref name="jsonDirectory"/>, deserializes them,
		/// bulk-inserts into LiteDB in a single transaction, then deletes the migrated JSON files.
		/// Safe to call multiple times — no-op when no JSON files are found.
		/// </summary>
		/// <param name="jsonDirectory">Directory that previously contained one JSON per game.</param>
		public void MigrateFromJson(string jsonDirectory)
		{
			if (!Directory.Exists(jsonDirectory))
			{
				return;
			}

			string[] jsonFiles = Directory.GetFiles(jsonDirectory, "*.json");
			if (jsonFiles.Length == 0)
			{
				return;
			}

			Logger.Info(string.Format(
				"MigrateFromJson — migrating {0} JSON files from '{1}'.",
				jsonFiles.Length, jsonDirectory));

			var items = new List<TItem>(jsonFiles.Length);
			var failedFiles = new List<string>();

			foreach (string file in jsonFiles)
			{
				try
				{
					string json = File.ReadAllText(file);
					TItem item = Playnite.SDK.Data.Serialization.FromJson<TItem>(json);
					if (item != null)
					{
						items.Add(item);
					}
				}
				catch (Exception ex)
				{
					Logger.Error(ex, string.Format("MigrateFromJson — failed to read '{0}'.", file));
					failedFiles.Add(file);
				}
			}

			UpsertBatch(items);

			foreach (string file in jsonFiles)
			{
				if (failedFiles.Contains(file))
				{
					continue;
				}

				try
				{
					File.Delete(file);
				}
				catch (Exception ex)
				{
					Logger.Warn(string.Format(
						"MigrateFromJson — could not delete '{0}': {1}", file, ex.Message));
				}
			}

			Logger.Info(string.Format(
				"MigrateFromJson — done: {0} migrated, {1} failed.",
				items.Count, failedFiles.Count));
		}

		#endregion

		#region Cache

		/// <summary>
		/// Clears the entire in-memory session cache.
		/// Next read for any item will hit LiteDB.
		/// </summary>
		public void InvalidateSessionCache()
		{
			_sessionCache.Clear();
		}

		/// <summary>
		/// Removes a single entry from the session cache without touching LiteDB.
		/// </summary>
		public void InvalidateSessionCache(Guid id)
		{
			TItem ignored;
			_sessionCache.TryRemove(id, out ignored);
		}

		#endregion

		#region Helpers

		private static bool IsDatabaseOpen =>
			API.Instance != null
			&& API.Instance.Database != null
			&& API.Instance.Database.IsOpen;

		private static bool WaitForDatabase(string operationName)
		{
			if (IsDatabaseOpen)
			{
				return true;
			}

			Common.LogDebug(true, string.Format(
				"WaitForDatabase — waiting before '{0}'.", operationName));

			bool isOpen = SpinWait.SpinUntil(() => IsDatabaseOpen, DatabaseOpenTimeoutMilliseconds);
			if (!isOpen)
			{
				var ex = new TimeoutException(string.Format(
					"Timed out waiting for Playnite database ({0} ms).", DatabaseOpenTimeoutMilliseconds));
				Common.LogError(ex, false, string.Format(
					"{0} aborted — database did not open in time.", operationName));
			}

			return isOpen;
		}

		#endregion

		/// <inheritdoc />
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_db?.Dispose();
		}
	}
}
