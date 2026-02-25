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
	///
	/// A session-level <see cref="ConcurrentDictionary{TKey,TValue}"/> is layered on top so that
	/// repeated reads (e.g. list scrolling) are allocation-free after the first access.
	///
	/// Design rationale:
	/// - LiteDB memory cache is disabled (<see cref="ConnectionString.CacheSize"/> = 0)
	///   to match Playnite's own rationale and avoid double-caching.
	/// - Journaling is disabled (<see cref="ConnectionString.Journal"/> = false) for write performance.
	///   WARNING: a crash during a write may leave the database in a corrupt state.
	///   Use <see cref="BackupDatabase"/> regularly to mitigate this risk.
	/// - Shared file mode allows external tools to open the file read-only while the plugin has it open.
	/// </summary>
	/// <typeparam name="TItem">Database item type inheriting <see cref="PluginDataBaseGameBase"/>.</typeparam>
	public class LiteDbItemCollection<TItem> : IDisposable
		where TItem : PluginDataBaseGameBase
	{
		#region Fields & Constants

		private static readonly ILogger Logger = LogManager.GetLogger();

		/// <summary>Maximum milliseconds to wait for the Playnite game database to open.</summary>
		private const int DatabaseOpenTimeoutMilliseconds = 30000;

		/// <summary>
		/// Polling interval used by <see cref="WaitForDatabase"/>.
		/// </summary>
		private const int DatabasePollIntervalMilliseconds = 200;

		/// <summary>Maximum number of backup files retained by <see cref="BackupDatabase"/>.</summary>
		private const int MaxBackupCount = 3;

		/// <summary>Full path to the .db file; stored for backup file naming.</summary>
		private readonly string _dbPath;

		private readonly LiteDatabase _db;

		/// <summary>
		/// LiteDB collection handle.
		/// </summary>
		private LiteCollection<TItem> _collection;

		/// <summary>
		/// Session-level read cache. Populated on first DB access per item, invalidated on writes.
		/// Thread-safe for concurrent reads and writes via <see cref="ConcurrentDictionary{TKey,TValue}"/>.
		/// NOTE: returned <typeparamref name="TItem"/> instances are not defensively copied;
		/// callers must not mutate them without calling <see cref="Upsert"/> afterwards.
		/// </summary>
		private readonly ConcurrentDictionary<Guid, TItem> _sessionCache
			= new ConcurrentDictionary<Guid, TItem>();

		/// <summary>
		/// In-memory document count kept in sync with LiteDB to avoid repeated COUNT queries.
		/// Mutated exclusively via <see cref="Interlocked"/> for thread safety.
		/// </summary>
		private int _count;

		/// <summary>
		/// Guards <see cref="PreWarm"/> against concurrent or duplicate execution.
		/// 0 = not started, 1 = started or completed.
		/// </summary>
		private int _preWarmState;

		private bool _disposed;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the number of items currently stored in the database.
		/// </summary>
		public int Count => _count;

		#endregion

		#region Lifecycle

		/// <summary>
		/// Opens or creates the LiteDB database file at <paramref name="dbPath"/>.
		/// Initialises the collection, ensures the unique index on <c>Id</c>,
		/// and seeds the in-memory document counter.
		/// </summary>
		/// <param name="dbPath">Full path to the .db file.</param>
		public LiteDbItemCollection(string dbPath)
		{
			_dbPath = dbPath;

			var connectionString = new ConnectionString(dbPath)
			{
				// Disabled for write performance. Risk: no crash recovery.
				// Mitigate with regular BackupDatabase() calls.
				Journal = false,

				// Disable LiteDB's internal page cache; the session cache above handles reads.
				CacheSize = 0,

				// Shared mode so external tools (e.g. DB browser) can open the file read-only.
				Mode = LiteDB.FileMode.Shared
			};

			_db = new LiteDatabase(connectionString);
			_collection = _db.GetCollection<TItem>("items");

			// NOTE: if Id is mapped to _id via [BsonId], this index is redundant.
			// Kept to guarantee uniqueness regardless of BsonMapper configuration.
			_collection.EnsureIndex(x => x.Id, unique: true);

			_count = _collection.Count();
		}

		/// <summary>
		/// Eagerly loads all items from LiteDB into the session cache.
		/// Call once at plugin startup, before the UI becomes interactive, to eliminate
		/// per-item DB round-trips during list rendering.
		/// Idempotent — subsequent calls return the current cache size immediately.
		/// </summary>
		/// <returns>
		/// Number of items loaded into the cache on this call, or 0 if already warmed.
		/// </returns>
		public int PreWarm()
		{
			if (Interlocked.CompareExchange(ref _preWarmState, 1, 0) != 0)
			{
				return _sessionCache.Count;
			}

			int loaded = 0;
			foreach (TItem item in _collection.FindAll())
			{
				_sessionCache[item.Id] = item;
				loaded++;
			}

			Logger.Info(string.Format("PreWarm — loaded {0} items into session cache.", loaded));
			return loaded;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_sessionCache.Clear();
			_db?.Dispose();
		}

		#endregion

		#region Read

		/// <summary>
		/// Returns the item for <paramref name="id"/> from the session cache,
		/// falling back to LiteDB on first access.
		/// Returns <see langword="null"/> if no item exists for that ID.
		/// </summary>
		public TItem Get(Guid id)
		{
			ThrowIfDisposed();

			TItem cached;
			if (_sessionCache.TryGetValue(id, out cached))
			{
				return cached;
			}

			// Cache miss — fetch from LiteDB and populate the cache for subsequent reads.
			TItem item = _collection.FindById(new BsonValue(id));
			if (item != null)
			{
				_sessionCache[id] = item;
			}

			return item;
		}

		/// <summary>
		/// Returns all items from LiteDB and populates the session cache as a side-effect.
		/// Prefer <see cref="PreWarm"/> for bulk warm-up at startup; use this for live enumeration.
		/// </summary>
		public IEnumerable<TItem> FindAll()
		{
			ThrowIfDisposed();

			foreach (TItem item in _collection.FindAll())
			{
				_sessionCache[item.Id] = item;
				yield return item;
			}
		}

		/// <summary>
		/// Returns items matching <paramref name="predicate"/> from LiteDB
		/// and populates the session cache for each matched item.
		/// </summary>
		public IEnumerable<TItem> Find(Expression<Func<TItem, bool>> predicate)
		{
			ThrowIfDisposed();

			foreach (TItem item in _collection.Find(predicate))
			{
				_sessionCache[item.Id] = item;
				yield return item;
			}
		}

		/// <summary>
		/// Returns <see langword="true"/> if an item exists for <paramref name="id"/>.
		/// Checks the session cache first to avoid a DB round-trip.
		/// </summary>
		public bool Exists(Guid id)
		{
			ThrowIfDisposed();

			return _sessionCache.ContainsKey(id)
				|| _collection.Exists(x => x.Id == id);
		}

		#endregion

		#region Write

		/// <summary>
		/// Inserts or replaces the item in LiteDB and refreshes the session cache.
		/// The in-memory document counter is incremented only on actual inserts.
		/// </summary>
		public void Upsert(TItem item)
		{
			ThrowIfDisposed();

			if (item == null)
			{
				Logger.Warn("Upsert called with null item.");
				return;
			}

			// LiteDB Upsert returns true when a new document is inserted, false on update.
			bool inserted = _collection.Upsert(item);
			if (inserted)
			{
				Interlocked.Increment(ref _count);
			}

			_sessionCache[item.Id] = item;
		}

		/// <summary>
		/// Removes the item identified by <paramref name="id"/> from LiteDB and the session cache.
		/// Returns <see langword="true"/> if an item was deleted.
		/// </summary>
		public bool Remove(Guid id)
		{
			ThrowIfDisposed();

			bool deleted = _collection.Delete(new BsonValue(id));
			if (deleted)
			{
				TItem ignored;
				_sessionCache.TryRemove(id, out ignored);
				Interlocked.Decrement(ref _count);
			}

			return deleted;
		}

		/// <summary>
		/// Batch-upserts all non-null items from <paramref name="items"/>.
		/// NOTE: LiteDB 4 does not expose a public transaction API (removed in v4, re-added in v5).
		/// Each write is an individual auto-commit. For pure initial inserts, use
		/// <see cref="InsertBulk"/> directly on the collection for better performance.
		/// </summary>
		public void UpsertBatch(IEnumerable<TItem> items)
		{
			ThrowIfDisposed();

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

				// Each Upsert is an individual auto-commit in LiteDB 4.
				bool inserted = _collection.Upsert(item);
				if (inserted)
				{
					Interlocked.Increment(ref _count);
				}

				_sessionCache[item.Id] = item;
			}
		}

		#endregion

		#region Cache

		/// <summary>
		/// Clears the entire in-memory session cache.
		/// The next read for any item will hit LiteDB.
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

		#region GameInfo

		/// <summary>
		/// Synchronises <see cref="PluginDataBaseGameBase.Name"/> and
		/// <see cref="PluginDataBaseGameBase.IsSaved"/> for the item identified by <paramref name="id"/>
		/// against the Playnite game database.
		/// If the game no longer exists, sets <see cref="PluginDataBaseGameBase.IsDeleted"/> to
		/// <see langword="true"/> and persists the change.
		/// </summary>
		public void SetGameInfo(Guid id)
		{
			ThrowIfDisposed();

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
					Upsert(item);

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
		/// Synchronises game info for every item in the collection against the Playnite database.
		/// Waits for the Playnite database to be available before processing.
		/// NOTE: LiteDB 4 has no public transaction API; each write is an individual auto-commit.
		/// Per-item errors are caught individually and do not interrupt the overall update.
		/// </summary>
		public void SetGameInfo()
		{
			ThrowIfDisposed();

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
					}
					else
					{
						item.IsDeleted = true;
						Common.LogDebug(true, string.Format(
							"SetGameInfo — marking item {0} ({1}) as deleted.", item.Id, item.Name));
					}

					// Each Upsert is an individual auto-commit in LiteDB 4.
					_collection.Upsert(item);
					_sessionCache[item.Id] = item;
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

		#region Backup & Restore

		/// <summary>
		/// Creates a timestamped backup of the current collection in <paramref name="backupDirectory"/>.
		/// Backup files are named <c>{dbname}_{yyyyMMdd_HHmmss}.db</c> and are valid LiteDB databases.
		/// Only the <see cref="MaxBackupCount"/> most recent backups are kept; older files are deleted
		/// automatically via <see cref="EnforceBackupRotation"/>.
		/// </summary>
		/// <param name="backupDirectory">Directory where backup files are written. Created if absent.</param>
		/// <returns>Full path of the backup file that was created.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="backupDirectory"/> is null or empty.
		/// </exception>
		public string BackupDatabase(string backupDirectory)
		{
			ThrowIfDisposed();

			if (string.IsNullOrEmpty(backupDirectory))
			{
				throw new ArgumentException(
					"Backup directory must not be null or empty.", "backupDirectory");
			}

			Directory.CreateDirectory(backupDirectory);

			string dbName = Path.GetFileNameWithoutExtension(_dbPath);
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string backupPath = Path.Combine(backupDirectory, string.Format("{0}_{1}.db", dbName, timestamp));

			Logger.Info(string.Format("BackupDatabase — creating backup at '{0}'.", backupPath));

			// Snapshot: read all current documents and write them to a new LiteDB file.
			// InsertBulk uses an internal batch mechanism — no manual transaction needed here.
			List<TItem> allItems = _collection.FindAll().ToList();
			using (var backupDb = new LiteDatabase(backupPath))
			{
				LiteCollection<TItem> backupCollection = backupDb.GetCollection<TItem>("items");
				backupCollection.EnsureIndex(x => x.Id, unique: true);
				if (allItems.Count > 0)
				{
					backupCollection.InsertBulk(allItems);
				}
			}

			Logger.Info(string.Format(
				"BackupDatabase — {0} items written to '{1}'.", allItems.Count, backupPath));

			// Remove backup files beyond the MaxBackupCount limit (oldest first).
			EnforceBackupRotation(backupDirectory, dbName);

			return backupPath;
		}

		/// <summary>
		/// Replaces the entire collection content with data from <paramref name="backupFilePath"/>.
		/// The current collection is dropped and recreated; the session cache is refreshed on success.
		/// NOTE: LiteDB 4 has no public transaction API. The restore uses <c>InsertBulk</c> which
		/// is safe here because the collection is freshly recreated (no duplicates possible).
		/// FIX (revised) — BeginTrans/Commit/Rollback removed; not available in LiteDB 4.
		/// </summary>
		/// <param name="backupFilePath">
		/// Full path to a backup .db file previously created by <see cref="BackupDatabase"/>.
		/// </param>
		/// <exception cref="FileNotFoundException">
		/// Thrown when <paramref name="backupFilePath"/> does not exist.
		/// </exception>
		public void RestoreDatabase(string backupFilePath)
		{
			ThrowIfDisposed();

			if (!File.Exists(backupFilePath))
			{
				throw new FileNotFoundException("Backup file not found.", backupFilePath);
			}

			Logger.Info(string.Format("RestoreDatabase — restoring from '{0}'.", backupFilePath));

			// Read the backup first so the source file is not held open during the write phase.
			List<TItem> backupItems;
			using (var backupDb = new LiteDatabase(backupFilePath))
			{
				LiteCollection<TItem> backupCollection = backupDb.GetCollection<TItem>("items");
				backupItems = backupCollection.FindAll().ToList();
			}

			// Drop the current collection for a clean slate, then recreate it.
			_db.DropCollection("items");
			_collection = _db.GetCollection<TItem>("items");
			_collection.EnsureIndex(x => x.Id, unique: true);
			_sessionCache.Clear();

			if (backupItems.Count > 0)
			{
				// InsertBulk is available in LiteDB 4 and safe here: collection was just recreated.
				// It batches writes internally for better performance than individual Insert calls.
				_collection.InsertBulk(backupItems);

				// Populate the session cache with the restored data.
				foreach (TItem item in backupItems)
				{
					_sessionCache[item.Id] = item;
				}
			}

			Interlocked.Exchange(ref _count, backupItems.Count);

			Logger.Info(string.Format(
				"RestoreDatabase — restored {0} items successfully.", backupItems.Count));
		}

		#endregion

		#region Migration

		/// <summary>
		/// One-shot migration from the legacy one-JSON-file-per-game layout.
		/// Reads all <c>*.json</c> files from <paramref name="jsonDirectory"/>, deserialises them,
		/// bulk-inserts into LiteDB via <see cref="UpsertBatch"/> (single transaction),
		/// then deletes the successfully migrated JSON files.
		/// Safe to call multiple times — no-op when no JSON files are present.
		/// </summary>
		/// <param name="jsonDirectory">Directory that previously contained one JSON file per game.</param>
		public void MigrateFromJson(string jsonDirectory)
		{
			ThrowIfDisposed();

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
			var failedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

			// Bulk-insert all successfully parsed items inside a single transaction (via UpsertBatch).
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

		#region Helpers

		/// <summary>
		/// Returns <see langword="true"/> when the Playnite game database is initialised and open.
		/// </summary>
		private static bool IsDatabaseOpen =>
			API.Instance != null
			&& API.Instance.Database != null
			&& API.Instance.Database.IsOpen;

		/// <summary>
		/// Polls until the Playnite game database is open or <see cref="DatabaseOpenTimeoutMilliseconds"/>
		/// elapses.
		/// </summary>
		/// <param name="operationName">Caller name used in diagnostic log messages.</param>
		/// <returns><see langword="true"/> if the database opened within the timeout.</returns>
		private static bool WaitForDatabase(string operationName)
		{
			if (IsDatabaseOpen)
			{
				return true;
			}

			Common.LogDebug(true, string.Format(
				"WaitForDatabase — waiting before '{0}'.", operationName));

			int elapsed = 0;

			while (!IsDatabaseOpen && elapsed < DatabaseOpenTimeoutMilliseconds)
			{
				Thread.Sleep(DatabasePollIntervalMilliseconds);
				elapsed += DatabasePollIntervalMilliseconds;
			}

			if (!IsDatabaseOpen)
			{
				var ex = new TimeoutException(string.Format(
					"Timed out waiting for Playnite database ({0} ms).", DatabaseOpenTimeoutMilliseconds));
				Common.LogError(ex, false, string.Format(
					"{0} aborted — database did not open in time.", operationName));
				return false;
			}

			return true;
		}

		/// <summary>
		/// Throws <see cref="ObjectDisposedException"/> if this instance has already been disposed.
		/// </summary>
		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}
		}

		/// <summary>
		/// Deletes backup files beyond the <see cref="MaxBackupCount"/> most recent ones.
		/// Files are sorted descending by name; because the timestamp format is
		/// <c>yyyyMMdd_HHmmss</c>, lexicographic order equals chronological order.
		/// </summary>
		/// <param name="backupDirectory">Directory containing backup files.</param>
		/// <param name="dbName">Base database name used to filter backup files.</param>
		private void EnforceBackupRotation(string backupDirectory, string dbName)
		{
			string pattern = string.Format("{0}_*.db", dbName);
			string[] allBackups = Directory.GetFiles(backupDirectory, pattern)
				.OrderByDescending(f => f)
				.ToArray();

			// Files at index >= MaxBackupCount are older than the retained window.
			for (int i = MaxBackupCount; i < allBackups.Length; i++)
			{
				try
				{
					File.Delete(allBackups[i]);
					Logger.Info(string.Format(
						"BackupDatabase — removed old backup '{0}' (limit: {1}).",
						allBackups[i], MaxBackupCount));
				}
				catch (Exception ex)
				{
					Logger.Warn(string.Format(
						"BackupDatabase — could not delete old backup '{0}': {1}",
						allBackups[i], ex.Message));
				}
			}
		}

		#endregion
	}
}