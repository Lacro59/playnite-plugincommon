using CommonPlayniteShared;
using CommonPlayniteShared.Common;
using CommonPluginsControls.Controls;
using CommonPluginsShared.Caching;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Models;
using CommonPluginsShared.Plugins;
using CommonPluginsShared.Services;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SystemChecker.Models;

namespace CommonPluginsShared.Collections
{
	/// <summary>
	/// Abstract base class for plugin database objects backed by LiteDB.
	/// Provides CRUD, tag management, refresh orchestration, and CSV extraction.
	/// </summary>
	/// <typeparam name="TSettings">Settings view-model type implementing <see cref="ISettings"/>.</typeparam>
	/// <typeparam name="TItem">Database item type inheriting <see cref="PluginGameEntry"/>.</typeparam>
	/// <typeparam name="T">Inner data type used to parameterise <see cref="PluginGameCollection{T}"/>.</typeparam>
	public abstract class PluginDatabaseObject<TSettings, TItem, T> : ObservableObject, IPluginDatabase<TItem>
		where TSettings : ISettings
		where TItem : PluginGameEntry
	{
		protected static readonly ILogger Logger = LogManager.GetLogger();
		
		/// <inheritdoc cref="IPluginDatabase.PluginName"/>
		public string PluginName { get; set; }

		/// <summary>Gets or sets the strongly-typed plugin settings view model.</summary>
		public TSettings PluginSettings { get; set; }

		public PluginExportCsv<TItem> PluginExportCsv { get; set; }

		/// <summary>Gets or sets plugin-specific paths (database, cache, installation directory).</summary>
		public PluginPaths Paths { get; set; }

		/// <summary>
		/// Timeout in milliseconds to wait for the database to finish loading.
		/// Override to adjust per-plugin (default: 10 000 ms).
		/// </summary>
		protected virtual int DatabaseLoadTimeout => 10000;

		/// <summary>Gets or sets the current game displayed in the active UI panel.</summary>
		public Game GameContext { get; set; }

		/// <inheritdoc cref="IPluginDatabase.PluginWindows"/>
		public IPluginWindows PluginWindows { get; set; }

		/// <summary>Prefix prepended to every tag created by this plugin (e.g. <c>"[SC]"</c>).</summary>
		protected string TagBefore { get; set; } = string.Empty;

		/// <summary>Gets or sets a value indicating whether a "No Data" tag should be added to games without data.</summary>
		public bool TagMissing { get; set; } = false;

		// ── LiteDB backend ────────────────────────────────────────────────────────

		protected LiteDbItemCollection<TItem> _database;

		// ── IsLoaded ─────────────────────────────────────────────────────────────

		private volatile bool _isLoaded = false;

		/// <inheritdoc cref="IPluginDatabase.IsLoaded"/>
		public bool IsLoaded
		{
			get => _isLoaded;
			set
			{
				if (_isLoaded == value)
				{
					return;
				}

				_isLoaded = value;

				// PropertyChanged must be raised on the UI thread.
				// SetValue() from ObservableObject would raise it on whichever thread calls it,
				// causing cross-thread exceptions when called from Task.Run.
				if (Application.Current?.Dispatcher?.CheckAccess() == true)
				{
					OnPropertyChanged(nameof(IsLoaded));
				}
				else
				{
					Application.Current?.Dispatcher?.BeginInvoke(
						new Action(() => OnPropertyChanged(nameof(IsLoaded))));
				}
			}
		}

		// ── Database events (replace PluginItemCollection.ItemUpdated / ItemCollectionChanged) ──

		/// <summary>Raised after any item is inserted or updated via <see cref="Add"/> or <see cref="Update"/>.</summary>
		public event EventHandler<ItemUpdatedEventArgs<TItem>> DatabaseItemUpdated;

		/// <summary>Raised after any item is removed via <see cref="Remove(Guid)"/>.</summary>
		public event EventHandler<ItemCollectionChangedEventArgs<TItem>> DatabaseItemCollectionChanged;

		// ── Tag cache ─────────────────────────────────────────────────────────────

		private List<Tag> _pluginTagsCache;
		private bool _pluginTagsCacheInitialized;

		/// <summary>Gets all Playnite tags that start with <see cref="TagBefore"/>.</summary>
		protected IEnumerable<Tag> PluginTags => GetPluginTags();

		private IEnumerable<Guid> PreviousIds { get; set; } = new List<Guid>();

		/// <summary>
		/// Initialises paths, cache directories, and subscribes to Playnite game events.
		/// </summary>
		protected PluginDatabaseObject(TSettings pluginSettings, string pluginName, string pluginUserDataPath)
		{
			PluginSettings = pluginSettings;
			PluginName = pluginName;

			Paths = new PluginPaths
			{
				PluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
				PluginUserDataPath = pluginUserDataPath,
				PluginDatabasePath = Path.Combine(pluginUserDataPath, pluginName),
				PluginCachePath = Path.Combine(PlaynitePaths.DataCachePath, pluginName),
			};

			HttpFileCacheService.CacheDirectory = Paths.PluginCachePath;

			CommonPlayniteShared.Common.FileSystem.CreateDirectory(Paths.PluginDatabasePath);
			CommonPlayniteShared.Common.FileSystem.CreateDirectory(Paths.PluginCachePath);

			API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;
			API.Instance.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
		}

		#region Database access helpers

		/// <summary>
		/// Blocks the calling thread until <see cref="IsLoaded"/> is true or
		/// <see cref="DatabaseLoadTimeout"/> elapses.
		/// FIX: replaced SpinWait.SpinUntil (busy-wait) with a sleep-based poll
		/// to avoid burning a CPU core while waiting for background initialisation.
		/// </summary>
		private void WaitForDatabaseLoad()
		{
			if (IsLoaded)
			{
				return;
			}

			Logger.Info(string.Format("Waiting for database to load (timeout: {0} ms)…", DatabaseLoadTimeout));

			int elapsed = 0;
			const int pollInterval = 100;

			while (!IsLoaded && elapsed < DatabaseLoadTimeout)
			{
				Thread.Sleep(pollInterval);
				elapsed += pollInterval;
			}

			if (!IsLoaded)
			{
				string message = string.Format("Database load timeout after {0} ms", DatabaseLoadTimeout);
				Logger.Error(message);
				throw new TimeoutException(message);
			}

			Logger.Info("Database loaded successfully.");
		}

		/// <summary>Returns the database without throwing on timeout; returns <c>null</c> instead.</summary>
		protected LiteDbItemCollection<TItem> GetDatabaseSafe()
		{
			try
			{
				WaitForDatabaseLoad();
				return _database;
			}
			catch (TimeoutException ex)
			{
				Logger.Warn(string.Format("Database access timeout: {0}", ex.Message));
				return null;
			}
		}

		/// <summary>Returns <c>true</c> if the database is ready for immediate access.</summary>
		public bool IsDatabaseReady() => IsLoaded && _database != null;

		#endregion

		#region Shared UI helpers

		/// <summary>Adds <paramref name="tagId"/> to <paramref name="game"/>'s tag list if not already present.</summary>
		protected static void AppendTagId(Game game, Guid tagId)
		{
			if (game.TagIds == null)
			{
				game.TagIds = new List<Guid> { tagId };
			}
			else if (!game.TagIds.Contains(tagId))
			{
				game.TagIds.Add(tagId);
			}
		}

		/// <summary>Persists <paramref name="game"/> changes to Playnite on the UI dispatcher thread.</summary>
		protected static void PersistGameUpdate(Game game)
		{
			API.Instance.MainView.UIDispatcher?.Invoke(() =>
			{
				API.Instance.Database.Games.Update(game);
				game.OnPropertyChanged();
			});
		}

		#endregion

		#region Database initialisation & lifecycle

		/// <inheritdoc/>
		public Task<bool> InitializeDatabase()
		{
			return Task.Run(() =>
			{
				if (IsLoaded)
				{
					Logger.Info("Database is already initialised.");
					return true;
				}

				bool result = LoadDatabase();
				IsLoaded = result;

				if (!result)
				{
					Logger.Error("LoadDatabase() returned false — plugin database is unavailable.");
				}

				return result;
			});
		}


		/// <summary>
		/// Opens the LiteDB file, runs the one-shot JSON migration if needed,
		/// pre-warms the session cache, and calls <see cref="LoadMoreData"/>.
		/// SetGameInfo and DeleteDataWithDeletedGame are intentionally deferred
		/// to <see cref="RunPostLoadMaintenance"/> which runs after IsLoaded is set,
		/// avoiding a deadlock where WaitForDatabaseLoad (10 s) would expire before
		/// SetGameInfo finishes waiting for Playnite's database (up to 30 s).
		/// </summary>
		protected bool LoadDatabase()
		{
			try
			{
				Stopwatch stopWatch = Stopwatch.StartNew();

				string dbPath = Path.Combine(Paths.PluginDatabasePath, PluginName + ".db");
				_database = new LiteDbItemCollection<TItem>(dbPath);

				_database.BackupDatabase(Paths.PluginDatabasePath);

				// JSON → LiteDB one-shot migration (no-op when no JSON files are present).
				MigrateJsonToLiteDb();

				// Pre-warm must complete before IsLoaded = true so every subsequent Get(id)
				// is a ConcurrentDictionary lookup, not a LiteDB round-trip.
				Stopwatch pwSw = Stopwatch.StartNew();
				int cached = _database.PreWarm();
				pwSw.Stop();
				Logger.Info(string.Format(
					"PreWarm — {0} items cached in {1} ms", cached, pwSw.ElapsedMilliseconds));

				LoadMoreData();

				stopWatch.Stop();
				Logger.Info(string.Format(
					"LoadDatabase — {0} items in {1:00}:{2:00}.{3:00}",
					_database.Count,
					stopWatch.Elapsed.Minutes,
					stopWatch.Elapsed.Seconds,
					stopWatch.Elapsed.Milliseconds / 10));

				// SetGameInfo() and DeleteDataWithDeletedGame() are deferred.
				// They depend on Playnite's game database being open (up to 30 s wait),
				// which would exceed WaitForDatabaseLoad's 10 s timeout if run here.
				// The plugin is fully usable once PreWarm() is done; maintenance runs in background.
				Task.Run(() => RunPostLoadMaintenance());

				return true;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return false;
			}
		}

		/// <summary>
		/// Runs game-info synchronisation and orphan cleanup after the database is marked as loaded.
		/// Waits for Playnite's game database internally via SetGameInfo().
		/// Errors are caught individually so a failure in one step does not abort the other.
		/// </summary>
		private void RunPostLoadMaintenance()
		{
			Logger.Info("RunPostLoadMaintenance — started.");

			try
			{
				// Synchronises Name / IsSaved / IsDeleted against Playnite's game list.
				// Internally waits up to 30 s for Playnite's database to open.
				_database.SetGameInfo();
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "RunPostLoadMaintenance — SetGameInfo failed.", false, PluginName);
			}

			try
			{
				// An exception here would have silently aborted LoadDatabase() and kept IsLoaded = false.
				DeleteDataWithDeletedGame();
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "RunPostLoadMaintenance — DeleteDataWithDeletedGame failed.", false, PluginName);
			}

			Logger.Info("RunPostLoadMaintenance — completed.");
		}


		/// <summary>
		/// One-shot migration from the legacy one-JSON-file-per-game layout to LiteDB.
		/// Shows a progress dialog while migrating.
		/// No-op when no JSON files are present.
		/// </summary>
		private void MigrateJsonToLiteDb()
		{
			if (!Directory.Exists(Paths.PluginDatabasePath))
			{
				return;
			}

			string[] jsonFiles = Directory.GetFiles(Paths.PluginDatabasePath, "*.json");
			if (jsonFiles.Length == 0)
			{
				return;
			}

			Logger.Info(string.Format(
				"MigrateJsonToLiteDb — {0} JSON file(s) found, starting migration.",
				jsonFiles.Length));

			int migrated = 0;
			int failed = 0;

			GlobalProgressOptions options = new GlobalProgressOptions(
				string.Format("{0} - {1}", PluginName,
					ResourceProvider.GetString("LOCCommonMigratingDatabase")))
			{
				Cancelable = false,
				IsIndeterminate = false
			};

			API.Instance.Dialogs.ActivateGlobalProgress((a) =>
			{
				a.ProgressMaxValue = jsonFiles.Length;

				foreach (string file in jsonFiles)
				{
					a.Text = string.Format(
						"{0} - {1}\n\n{2}/{3}\n{4}",
						PluginName,
						ResourceProvider.GetString("LOCCommonMigratingDatabase"),
						migrated + failed + 1,
						jsonFiles.Length,
						Path.GetFileNameWithoutExtension(file));

					try
					{
						string json = File.ReadAllText(file);
						TItem item = Serialization.FromJson<TItem>(json);

						if (item == null)
						{
							Logger.Warn(string.Format(
								"MigrateJsonToLiteDb — null result for '{0}', skipping.", file));
							failed++;
							a.CurrentProgressValue++;
							continue;
						}

						item.IsSaved = true;
						_database.Upsert(item);

						File.Delete(file);
						migrated++;
					}
					catch (Exception ex)
					{
						Logger.Error(ex, string.Format(
							"MigrateJsonToLiteDb — failed on '{0}'.", file));
						failed++;
					}

					a.CurrentProgressValue++;
				}
			}, options);

			Logger.Info(string.Format(
				"MigrateJsonToLiteDb — completed: {0} migrated, {1} failed.",
				migrated, failed));

			if (failed > 0)
			{
				API.Instance.Dialogs.ShowMessage(
					string.Format(
						ResourceProvider.GetString("LOCCommonMigrationPartialFailure"),
						failed, jsonFiles.Length),
					PluginName);
			}
		}

		/// <summary>Override to perform additional plugin-specific initialisation after the base database loads.</summary>
		protected virtual void LoadMoreData() { }

		/// <inheritdoc/>
		public bool ClearDatabase()
		{
			bool isOk = true;
			int removedCount = 0;

			GlobalProgressOptions options = new GlobalProgressOptions(
				string.Format("{0} - {1}", PluginName, ResourceProvider.GetString("LOCCommonProcessing")))
			{
				Cancelable = false,
				IsIndeterminate = false
			};

			API.Instance.Dialogs.ActivateGlobalProgress((a) =>
			{
				List<TItem> allItems = _database.FindAll().ToList();
				a.ProgressMaxValue = allItems.Count;

				foreach (TItem item in allItems)
				{
					try
					{
						RemoveTag(item.Id);
						_database.Remove(item.Id);
						Common.LogDebug(true, string.Format("ClearDatabase — removed item {0} ({1})", item.Id, item.Name));
						removedCount++;
						a.CurrentProgressValue++;
					}
					catch (Exception ex)
					{
						isOk = false;
						Common.LogError(ex, false, string.Format("Error clearing {0} — {1}", item.Id, item.Name), false, PluginName);
					}
				}

				Logger.Info(string.Format("ClearDatabase — {0}/{1} items removed successfully.", removedCount, allItems.Count));

			}, options);

			bool cacheOk = ClearCache();
			if (!cacheOk)
			{
				isOk = false;
			}

			// Notify after all operations (DB removal + cache clear) are complete.
			DatabaseItemCollectionChanged?.Invoke(this, new ItemCollectionChangedEventArgs<TItem>(
				new List<TItem>(), new List<TItem>()));

			return isOk;
		}

		/// <summary>Removes database entries whose corresponding Playnite game no longer exists.</summary>
		public virtual void DeleteDataWithDeletedGame()
		{
			List<TItem> orphaned = _database.FindAll()
				.Where(x => API.Instance.Database.Games.Get(x.Id) == null)
				.ToList();

			foreach (TItem item in orphaned)
			{
				Logger.Info(string.Format(
					"Deleting orphaned data: {0} ({1})", item.Name, item.Id));
				_database.Remove(item.Id);
			}
		}

		#endregion

		#region Query helpers

		/// <inheritdoc/>
		public virtual void GetSelectData()
		{
			OptionsDownloadData view = new OptionsDownloadData(this);
			Window window = PlayniteUiHelper.CreateExtensionWindow(
				PluginName + " - " + ResourceProvider.GetString("LOCCommonSelectData"), view);
			window.ShowDialog();

			List<Game> playniteDb = view.GetFilteredGames();
			bool onlyMissing = view.GetOnlyMissing();

			if (playniteDb == null)
			{
				return;
			}

			if (onlyMissing)
			{
				playniteDb = playniteDb.FindAll(x => !Get(x.Id, true).HasData);
			}

			Refresh(playniteDb.Select(x => x.Id));
		}

		/// <summary>
		/// Returns all cached items directly from the in-memory ConcurrentDictionary.
		/// Faster than GetGamesList() which does an extra Games.Get() per item.
		/// </summary>
		public IEnumerable<TItem> GetAllCache()
		{
			if (_database == null)
			{
				return Enumerable.Empty<TItem>();
			}
			return _database.FindAll();
		}

		/// <summary>
		/// Returns all Playnite games that have a corresponding entry in the plugin database.
		/// Guard added: waits for database load via GetDatabaseSafe().
		/// </summary>
		public virtual IEnumerable<Game> GetGamesList()
		{
			LiteDbItemCollection<TItem> db = GetDatabaseSafe();
			if (db == null)
			{
				yield break;
			}

			foreach (TItem item in db.FindAll())
			{
				Game game = API.Instance.Database.Games.Get(item.Id);
				if (game != null)
				{
					yield return game;
				}
			}
		}

		/// <inheritdoc/>
		public virtual IEnumerable<Game> GetGamesWithNoData()
		{
			LiteDbItemCollection<TItem> db = GetDatabaseSafe();
			if (db == null)
			{
				return Enumerable.Empty<Game>();
			}

			IEnumerable<Game> withNoData = db.FindAll()
				.Where(x => !x.HasData)
				.Select(x => API.Instance.Database.Games.Get(x.Id))
				.Where(x => x != null);

			IEnumerable<Game> notInDb = API.Instance.Database.Games
				.Where(x => !db.Exists(x.Id));

			return withNoData.Union(notInDb).Distinct().Where(x => !x.Hidden);
		}

		/// <inheritdoc/>
		public virtual IEnumerable<Game> GetGamesOldData(int months)
		{
			LiteDbItemCollection<TItem> db = GetDatabaseSafe();
			if (db == null)
			{
				return Enumerable.Empty<Game>();
			}

			return db.Find(x => x.DateLastRefresh <= DateTime.Now.AddMonths(-months))
				.Select(x => API.Instance.Database.Games.Get(x.Id))
				.Where(x => x != null);
		}

		/// <summary>Returns a projection of all database entries as DataGame view models.</summary>
		public virtual IEnumerable<DataGame> GetDataGames()
		{
			LiteDbItemCollection<TItem> db = GetDatabaseSafe();
			if (db == null)
			{
				return Enumerable.Empty<DataGame>();
			}

			return db.FindAll().Select(x => new DataGame
			{
				Id = x.Id,
				Icon = x.Icon.IsNullOrEmpty() ? x.Icon : API.Instance.Database.GetFullFilePath(x.Icon),
				Name = x.Name,
				IsDeleted = x.IsDeleted,
				CountData = x.Count
			}).Distinct();
		}

		/// <summary>Returns database entries that are marked as deleted.</summary>
		public virtual IEnumerable<DataGame> GetIsolatedDataGames()
		{
			LiteDbItemCollection<TItem> db = GetDatabaseSafe();
			if (db == null)
			{
				return Enumerable.Empty<DataGame>();
			}

			return db.FindAll().Where(x => x.IsDeleted).Select(x => new DataGame
			{
				Id = x.Id,
				Icon = x.Icon.IsNullOrEmpty() ? x.Icon : API.Instance.Database.GetFullFilePath(x.Icon),
				Name = x.Name,
				IsDeleted = x.IsDeleted,
				CountData = x.Count
			}).Distinct();
		}

		#endregion

		#region CRUD

		/// <summary>Creates a minimal default item for <paramref name="id"/>.</summary>
		public virtual TItem GetDefault(Guid id)
		{
			Game game = API.Instance.Database.Games.Get(id);
			return game == null ? null : GetDefault(game);
		}

		/// <summary>Creates a minimal default item populated with basic game metadata.</summary>
		public virtual TItem GetDefault(Game game)
		{
			TItem item = typeof(TItem).CrateInstance<TItem>();
			item.Id = game.Id;
			item.Name = game.Name;
			item.IsSaved = false;
			return item;
		}

		/// <summary>Adds a new item to the database and raises <see cref="DatabaseItemUpdated"/>.</summary>
		public virtual void Add(TItem itemToAdd)
		{
			try
			{
				if (itemToAdd == null)
				{
					Logger.Warn("Add() called with null item.");
					return;
				}

				itemToAdd.IsSaved = true;
				_database.Upsert(itemToAdd);

				DatabaseItemUpdated?.Invoke(this, new ItemUpdatedEventArgs<TItem>(
					new List<ItemUpdateEvent<TItem>>
					{
						new ItemUpdateEvent<TItem>(itemToAdd, itemToAdd)
					}));

				if (IsTaggingEnabled())
				{
					RemoveTag(itemToAdd.Id);
					AddTag(itemToAdd.Id);
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, false, PluginName);
				NotifyError("Add", ex);
			}
		}

		/// <summary>Updates an existing item in the database and raises <see cref="DatabaseItemUpdated"/>.</summary>
		public virtual void Update(TItem itemToUpdate)
		{
			try
			{
				if (itemToUpdate == null)
				{
					Logger.Warn("Update() called with null item.");
					return;
				}

				itemToUpdate.IsSaved = true;
				itemToUpdate.DateLastRefresh = DateTime.Now.ToUniversalTime();
				_database.Upsert(itemToUpdate);

				DatabaseItemUpdated?.Invoke(this, new ItemUpdatedEventArgs<TItem>(
					new List<ItemUpdateEvent<TItem>>
					{
						new ItemUpdateEvent<TItem>(itemToUpdate, itemToUpdate)
					}));

				if (IsTaggingEnabled())
				{
					RemoveTag(itemToUpdate.Id);
					AddTag(itemToUpdate.Id);
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, false, PluginName);
				NotifyError("Update", ex);
			}
		}

		/// <summary>Adds the item if no entry exists for its ID; otherwise updates the existing entry.</summary>
		public virtual void AddOrUpdate(TItem item)
		{
			if (item == null)
			{
				Logger.Warn("AddOrUpdate() called with null item.");
				return;
			}

			if (GetOnlyCache(item.Id) == null)
			{
				Add(item);
			}
			else
			{
				Update(item);
			}
		}

		/// <inheritdoc/>
		public virtual bool Remove(Game game) => Remove(game.Id);

		/// <summary>
		/// Removes the item identified by <paramref name="id"/> and raises
		/// <see cref="DatabaseItemCollectionChanged"/>.
		/// </summary>
		public virtual bool Remove(Guid id)
		{
			RemoveTag(id);
			bool removed = _database.Remove(id);

			if (removed)
			{
				DatabaseItemCollectionChanged?.Invoke(this,
					new ItemCollectionChangedEventArgs<TItem>(
						new List<TItem>(), new List<TItem>()));
			}

			return removed;
		}

		/// <inheritdoc/>
		public virtual void Remove(List<Guid> ids) => Remove((IEnumerable<Guid>)ids);

		/// <inheritdoc/>
		public virtual bool Remove(IEnumerable<Guid> ids)
		{
			Logger.Info("Remove(IEnumerable<Guid>) started.");
			foreach (Guid id in ids)
			{
				try
				{
					Remove(id);
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, true, PluginName);
				}
			}

			return true;
		}

		// ── Cache accessors ───────────────────────────────────────────────────────

		/// <summary>Returns the item from the LiteDB session cache without any web access.</summary>
		public virtual TItem GetOnlyCache(Guid id) => _database?.Get(id);

		/// <summary>Returns the item from the LiteDB session cache without any web access.</summary>
		public virtual TItem GetOnlyCache(Game game) => _database?.Get(game.Id);

		/// <summary>Returns a deep clone of the item for <paramref name="id"/>.</summary>
		public virtual TItem GetClone(Guid id) => Serialization.GetClone(Get(id, true, false));

		/// <summary>Returns a deep clone of the item for <paramref name="game"/>.</summary>
		public virtual TItem GetClone(Game game) => Serialization.GetClone(Get(game, true, false));

		/// <summary>Gets an item by ID, optionally fetching from the web or forcing a refresh.</summary>
		public abstract TItem Get(Guid id, bool onlyCache = false, bool force = false);

		/// <summary>Gets an item by game, optionally fetching from the web or forcing a refresh.</summary>
		public virtual TItem Get(Game game, bool onlyCache = false, bool force = false)
			=> Get(game.Id, onlyCache, force);

		/// <summary>Returns <c>null</c> by default; override to fetch data from an online source.</summary>
		public virtual TItem GetWeb(Guid id) => null;

		/// <summary>Fetches data from an online source using the game object.</summary>
		public virtual TItem GetWeb(Game game) => GetWeb(game.Id);

		// ── Explicit IPluginDatabase bridges ─────────────────────────────────────
		PluginGameEntry IPluginDatabase.Get(Game game, bool onlyCache, bool force)
			=> Get(game, onlyCache, force);
		PluginGameEntry IPluginDatabase.Get(Guid id, bool onlyCache, bool force)
			=> Get(id, onlyCache, force);
		PluginGameEntry IPluginDatabase.GetOnlyCache(Guid id)
			=> GetOnlyCache(id);
		PluginGameEntry IPluginDatabase.GetOnlyCache(Game game)
			=> GetOnlyCache(game);
		PluginGameEntry IPluginDatabase.GetClone(Game game)
			=> GetClone(game);
		PluginGameEntry IPluginDatabase.GetClone(Guid id)
			=> GetClone(id);
		void IPluginDatabase.AddOrUpdate(PluginGameEntry item)
			=> AddOrUpdate((TItem)item);

		#endregion

		#region Refresh

		/// <summary>Refreshes a single game with a progress dialog.</summary>
		public void Refresh(Game game) => Refresh(game.Id);

		/// <summary>Refreshes a single game by ID with a progress dialog.</summary>
		public void Refresh(Guid id)
		{
			GlobalProgressOptions options = new GlobalProgressOptions(
				string.Format("{0} - {1}", PluginName,
					ResourceProvider.GetString("LOCCommonProcessing")))
			{
				Cancelable = false,
				IsIndeterminate = true
			};

			API.Instance.Dialogs.ActivateGlobalProgress(a => RefreshNoLoader(id, a.CancelToken), options);
		}

		/// <inheritdoc/>
		public void Refresh(IEnumerable<Guid> ids)
		{
			Logger.Info("Refresh() started.");
			Refresh(ids, ResourceProvider.GetString("LOCCommonProcessing"));
		}

		/// <summary>Refreshes a batch of games with a cancellable progress dialog.</summary>
		public virtual void Refresh(IEnumerable<Guid> ids, string message)
		{
			List<Guid> idList = ids.ToList();

			GlobalProgressOptions options = new GlobalProgressOptions(
				string.Format("{0} - {1}", PluginName, message))
			{
				Cancelable = true,
				IsIndeterminate = idList.Count == 1
			};

			API.Instance.Dialogs.ActivateGlobalProgress((a) =>
			{
				Stopwatch stopWatch = Stopwatch.StartNew();
				a.ProgressMaxValue = idList.Count;

				foreach (Guid id in idList)
				{
					if (a.CancelToken.IsCancellationRequested)
					{
						break;
					}

					Game game = API.Instance.Database.Games.Get(id);
					a.Text = BuildProgressText(message, a.CurrentProgressValue, idList.Count, game);

					try
					{
						Thread.Sleep(100);
						RefreshNoLoader(id, a.CancelToken);
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, true, PluginName);
					}

					a.CurrentProgressValue++;
				}

				stopWatch.Stop();
				TimeSpan ts = stopWatch.Elapsed;
				Logger.Info(string.Format(
					"Refresh(){0} — {1:00}:{2:00}.{3:00} for {4}/{5} items",
					a.CancelToken.IsCancellationRequested ? " (canceled)" : string.Empty,
					ts.Minutes, ts.Seconds, ts.Milliseconds / 10,
					a.CurrentProgressValue, idList.Count));
			}, options);
		}

		/// <summary>Refreshes all items that currently have data.</summary>
		public virtual void RefreshAll()
		{
			LiteDbItemCollection<TItem> db = GetDatabaseSafe();
			if (db == null)
			{
				return;
			}

			IEnumerable<Guid> ids = db.FindAll()
				.Where(x => x.HasData)
				.Select(x => x.Id);
			Refresh(ids);
		}

		/// <summary>Core refresh logic without a progress dialog.</summary>
		public virtual void RefreshNoLoader(Guid id, CancellationToken cancellationToken = default)
		{
			Game game = API.Instance.Database.Games.Get(id);
			Logger.Info(string.Format("RefreshNoLoader — {0} ({1})", game?.Name, id));

			TItem cached = Get(id, true);
			TItem webItem = GetWeb(id);

			if (webItem != null && !ReferenceEquals(cached, webItem))
			{
				Update(webItem);
			}
			else
			{
				webItem = cached;
			}

			ActionAfterRefresh(webItem);
		}

		/// <summary>Called after each item refresh. Override to perform post-processing.</summary>
		public virtual void ActionAfterRefresh(TItem item) { }

		/// <summary>Refreshes all installed, non-hidden games.</summary>
		public virtual void RefreshInstalled()
		{
			Logger.Info("RefreshInstalled() started.");
			IEnumerable<Guid> ids = API.Instance.Database.Games
				.Where(x => x.IsInstalled && !x.Hidden)
				.Select(x => x.Id);
			Logger.Info(string.Format(
				"RefreshInstalled — {0} game(s) queued.", ids.Count()));
			Refresh(ids, ResourceProvider.GetString("LOCCommonGettingInstalledDatas"));
		}

		/// <summary>Refreshes a specific set of installed games.</summary>
		public virtual void RefreshInstalled(IEnumerable<Guid> ids)
		{
			Logger.Info("RefreshInstalled(ids) started.");
			Refresh(ids, ResourceProvider.GetString("LOCCommonGettingInstalledDatas"));
		}

		/// <summary>Refreshes games added since the last auto-update timestamp stored in settings.</summary>
		public virtual void RefreshRecent()
		{
			Logger.Info("RefreshRecent() started.");

			object settings = PluginSettings.GetType()
				.GetProperty("Settings").GetValue(PluginSettings);
			PropertyInfo propertyInfo = settings.GetType()
				.GetProperty("LastAutoLibUpdateAssetsDownload");

			DateTime since = propertyInfo != null
				? (DateTime)propertyInfo.GetValue(settings)
				: DateTime.Now.AddMonths(-1);

			if (propertyInfo == null)
			{
				Logger.Warn(
					"LastAutoLibUpdateAssetsDownload not found in settings; defaulting to -1 month.");
			}

			IEnumerable<Guid> ids = API.Instance.Database.Games
				.Where(x => x.Added != null && x.Added > since)
				.Select(x => x.Id);

			Logger.Info(string.Format(
				"RefreshRecent — {0} game(s) queued.", ids.Count()));
			Refresh(ids, ResourceProvider.GetString("LOCCommonGettingNewDatas"));
		}

		/// <inheritdoc/>
		[Obsolete("Use Refresh(ids)")]
		public virtual void RefreshWithNoData(IEnumerable<Guid> ids) => Refresh(ids);

		/// <inheritdoc/>
		public virtual PluginGameEntry MergeData(Guid fromId, Guid toId) => null;

		#endregion

		#region Tag management

		private IEnumerable<Tag> GetPluginTags()
		{
			if (_pluginTagsCacheInitialized)
			{
				return _pluginTagsCache;
			}

			_pluginTagsCacheInitialized = true;

			if (TagBefore.IsNullOrEmpty() || API.Instance.Database.Tags == null)
			{
				_pluginTagsCache = new List<Tag>();
				return _pluginTagsCache;
			}

			_pluginTagsCache = API.Instance.Database.Tags
				.Where(t => !t.Name.IsNullOrEmpty()
					&& t.Name.StartsWith(TagBefore, StringComparison.Ordinal))
				.ToList();

			return _pluginTagsCache;
		}

		private void ResetPluginTagsCache()
		{
			_pluginTagsCacheInitialized = false;
		}

		/// <summary>Returns the ID of the tag with the full prefixed name, creating it if needed.</summary>
		internal Guid? CheckTagExist(string tagName)
		{
			string fullName = TagBefore.IsNullOrEmpty()
				? tagName
				: string.Format("{0} {1}", TagBefore, tagName);

			Tag existing = PluginTags.FirstOrDefault(
				t => string.Equals(t.Name, fullName, StringComparison.Ordinal));

			if (existing != null)
			{
				return existing.Id;
			}

			API.Instance.Database.Tags.Add(new Tag { Name = fullName });
			ResetPluginTagsCache();

			Tag created = PluginTags.FirstOrDefault(
				t => string.Equals(t.Name, fullName, StringComparison.Ordinal));

			return created?.Id;
		}

		/// <summary>Returns the ID of the localised "No Data" tag, creating it if needed.</summary>
		public Guid? AddNoDataTag() => CheckTagExist(ResourceProvider.GetString("LOCNoData"));

		/// <summary>
		/// Resolves and appends the appropriate plugin tag to <paramref name="game"/>.TagIds in memory.
		/// Does NOT persist the change — caller is responsible for calling PersistGameUpdate.
		/// </summary>
		/// <returns>
		/// <c>true</c> if TagIds was modified and a persist is needed; <c>false</c> otherwise.
		/// </returns>
		protected virtual bool AppendPluginTag(Game game)
		{
			TItem item = Get(game, true);

			if (item.HasData)
			{
				try
				{
					Guid? tagId = FindGoodPluginTags(string.Empty);
					if (tagId != null)
					{
						AppendTagId(game, tagId.Value);
						return true;
					}
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, $"Tag insert error {game.Name}", true, PluginName,
						string.Format(ResourceProvider.GetString("LOCCommonNotificationTagError"), game.Name));
				}
				return false;
			}

			if (TagMissing)
			{
				Guid? noDataTagId = AddNoDataTag();
				if (noDataTagId != null)
				{
					AppendTagId(game, noDataTagId.Value);
					return true;
				}
			}

			return false;
		}

		/// <summary>Adds the appropriate plugin tag to <paramref name="game"/>.</summary>
		public void AddTag(Game game)
		{
			bool modified = AppendPluginTag(game);
			if (modified)
			{
				PersistGameUpdate(game);
			}
		}

		/// <summary>Adds the appropriate plugin tag to the game identified by <paramref name="id"/>.</summary>
		public void AddTag(Guid id)
		{
			Game game = API.Instance.Database.Games.Get(id);
			if (game != null)
			{
				AddTag(game);
			}
		}

		/// <inheritdoc/>
		public void AddTagAllGames()
		{
			Logger.Info("AddTagAllGame() started.");
			IEnumerable<Guid> ids = API.Instance.Database.Games
				.Where(x => !x.Hidden)
				.Select(x => x.Id);
			AddTag(ids, string.Format("{0} - {1}", PluginName,
				ResourceProvider.GetString("LOCCommonAddingAllTag")));
		}

		/// <inheritdoc/>
		public void AddTagSelectData()
		{
			Logger.Info("AddTagSelectData() started.");

			OptionsDownloadData view = new OptionsDownloadData(this, true);
			Window window = PlayniteUiHelper.CreateExtensionWindow(
				PluginName + " - " + ResourceProvider.GetString("LOCCommonSelectGames"), view);
			window.ShowDialog();

			List<Game> playniteDb = view.GetFilteredGames();
			TagMissing = view.GetTagMissing();

			if (playniteDb == null)
			{
				TagMissing = false;
				return;
			}

			AddTag(playniteDb.Select(x => x.Id),
				string.Format("{0} - {1}", PluginName,
					ResourceProvider.GetString("LOCCommonAddingAllTag")));
			TagMissing = false;
		}

		/// <summary>Adds plugin tags to a batch of games with a cancellable progress dialog.</summary>
		/// <summary>
		/// Adds plugin tags to a batch of games with a cancellable progress dialog.
		/// </summary>
		/// <summary>
		/// Adds plugin tags to a batch of games with a cancellable progress dialog.
		/// Removes existing plugin tags before applying the new ones.
		/// A single database write is performed per game.
		/// </summary>
		/// <param name="ids">The identifiers of the games to tag.</param>
		/// <param name="message">The message displayed in the progress dialog.</param>
		public void AddTag(IEnumerable<Guid> ids, string message)
		{
			List<Guid> idList = ids.ToList();
			if (idList.Count == 0) return;

			GlobalProgressOptions options = new GlobalProgressOptions(message)
			{
				Cancelable = true,
				IsIndeterminate = idList.Count == 1
			};

			API.Instance.Dialogs.ActivateGlobalProgress(a =>
			{
				int errorCount = 0;

				try
				{
					API.Instance.Database.BeginBufferUpdate();
					API.Instance.Database.Games.BeginBufferUpdate();

					Stopwatch stopWatch = Stopwatch.StartNew();
					a.ProgressMaxValue = idList.Count;

					foreach (Guid id in idList)
					{
						if (a.CancelToken.IsCancellationRequested) break;

						Game game = API.Instance.Database.Games.Get(id);
						if (game == null)
						{
							a.CurrentProgressValue++;
							continue;
						}

						a.Text = BuildProgressText(message, a.CurrentProgressValue, idList.Count, game);

						try
						{
							StripPluginTags(game);
							bool modified = AppendPluginTag(game);
							if (modified)
							{
								PersistGameUpdate(game);
							}
						}
						catch (Exception ex)
						{
							errorCount++;
							Common.LogError(ex, false, false, PluginName);
						}

						a.CurrentProgressValue++;
					}

					stopWatch.Stop();
					TimeSpan ts = stopWatch.Elapsed;
					Logger.Info(string.Format(
						"AddTag {0} {1:00}:{2:00}.{3:000} for {4}/{5} items",
						a.CancelToken.IsCancellationRequested ? "canceled" : string.Empty,
						ts.Minutes, ts.Seconds, ts.Milliseconds / 10,
						a.CurrentProgressValue, idList.Count));

					if (errorCount > 0)
					{
						API.Instance.Notifications.Add(new NotificationMessage(
							string.Format("{0}-AddTag-Error", PluginName),
							string.Format(ResourceProvider.GetString("LOCCommonNotificationTagBatchError"), errorCount),
							NotificationType.Error,
							() => PlayniteTools.CreateLogPackage(PluginName)));
					}
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, false, PluginName);
				}
				finally
				{
					API.Instance.Database.Games.EndBufferUpdate();
					API.Instance.Database.EndBufferUpdate();
				}
			}, options);
		}

		/// <summary>
		/// Removes all plugin-owned tags from <paramref name="game"/>.TagIds in memory.
		/// Does NOT persist the change — caller is responsible for calling PersistGameUpdate.
		/// </summary>
		protected void StripPluginTags(Game game)
		{
			if (game?.TagIds == null) return;

			game.TagIds = game.TagIds
				.Where(x => !PluginTags.Any(y => x == y.Id))
				.ToList();
		}

		/// <summary>Removes all plugin tags from <paramref name="game"/>.</summary>
		public void RemoveTag(Game game)
		{
			if (game?.TagIds == null)
			{ 
				return; 
			}

			StripPluginTags(game);
			PersistGameUpdate(game);
		}

		/// <summary>Removes all plugin tags from the game identified by <paramref name="id"/>.</summary>
		public void RemoveTag(Guid id)
		{
			Game game = API.Instance.Database.Games.Get(id);
			if (game != null)
			{
				RemoveTag(game);
			}
		}

		/// <inheritdoc/>
		public void RemoveTagAllGames(bool fromClearDatabase = false)
		{
			Common.LogDebug(true, "RemoveTagAllGame()");

			string message = fromClearDatabase
				? string.Format("{0} - {1}", PluginName,
					ResourceProvider.GetString("LOCCommonClearingAllTag"))
				: string.Format("{0} - {1}", PluginName,
					ResourceProvider.GetString("LOCCommonRemovingAllTag"));

			GlobalProgressOptions options = new GlobalProgressOptions(message)
			{
				Cancelable = true,
				IsIndeterminate = false
			};

			API.Instance.Dialogs.ActivateGlobalProgress((a) =>
			{
				try
				{
					Logger.Info("RemoveTagAllGame() started.");
					API.Instance.Database.BeginBufferUpdate();
					API.Instance.Database.Games.BeginBufferUpdate();

					Stopwatch stopWatch = Stopwatch.StartNew();
					List<Game> playniteDb = API.Instance.Database.Games
						.Where(x => !x.Hidden)
						.ToList();
					a.ProgressMaxValue = playniteDb.Count;

					foreach (Game game in playniteDb)
					{
						if (a.CancelToken.IsCancellationRequested)
						{
							break;
						}

						a.Text = BuildProgressText(
							message, a.CurrentProgressValue, playniteDb.Count, game);

						try
						{
							RemoveTag(game);
						}
						catch (Exception ex)
						{
							Common.LogError(ex, false, false, PluginName);
						}

						a.CurrentProgressValue++;
					}

					stopWatch.Stop();
					TimeSpan ts = stopWatch.Elapsed;
					Logger.Info(string.Format(
						"RemoveTagAllGame(){0} — {1:00}:{2:00}.{3:00} for {4}/{5} items",
						a.CancelToken.IsCancellationRequested ? " (canceled)" : string.Empty,
						ts.Minutes, ts.Seconds, ts.Milliseconds / 10,
						a.CurrentProgressValue, playniteDb.Count));
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, false, PluginName);
				}
				finally
				{
					API.Instance.Database.Games.EndBufferUpdate();
					API.Instance.Database.EndBufferUpdate();
				}
			}, options);
		}

		/// <summary>Resolves the tag ID to apply to a game. Override to implement custom tag selection.</summary>
		internal virtual Guid? FindGoodPluginTags(string tagName) => CheckTagExist(tagName);

		#endregion

		#region Playnite event handlers

		/// <summary>
		/// Responds to Playnite game updates.
		/// Guard added: skips processing if the plugin database is not yet loaded
		/// to avoid NullReferenceException on _database during startup race condition.
		/// </summary>
		public virtual void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
		{
			// Guard: _database is null until InitializeDatabase() completes.
			if (!IsLoaded || _database == null)
			{
				Logger.Warn("Games_ItemUpdated fired before database was loaded; skipping.");
				return;
			}

			try
			{
				if (e?.UpdatedItems?.Count > 0)
				{
					e.UpdatedItems.ForEach(x =>
					{
						if (x.NewData?.Id != null)
						{
							_database.SetGameInfo(x.NewData.Id);
							ActionAfterGames_ItemUpdated(x.OldData, x.NewData);
						}
					});

					if (IsAutoImportOnInstalledEnabled())
					{
						List<Guid> newlyInstalled = e.UpdatedItems
							.Where(x => !x.OldData.IsInstalled
								&& x.NewData.IsInstalled
								&& !PreviousIds.Contains(x.NewData.Id))
							.Select(x => x.NewData.Id)
							.ToList();

						PreviousIds = newlyInstalled;

						if (newlyInstalled.Count > 0)
						{
							RefreshInstalled(newlyInstalled);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, false, PluginName);
			}
		}

		/// <summary>Called after each individual game update event.</summary>
		public virtual void ActionAfterGames_ItemUpdated(Game gameOld, Game gameNew) { }

		/// <summary>
		/// Guard added: skips processing if the plugin database is not yet loaded.
		/// Removes orphaned plugin data when a Playnite game is deleted.
		/// </summary>
		private void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
		{
			// Guard: _database is null until InitializeDatabase() completes.
			if (!IsLoaded || _database == null)
			{
				Logger.Warn("Games_ItemCollectionChanged fired before database was loaded; skipping.");
				return;
			}

			try
			{
				e?.RemovedItems?.ForEach(x => Remove(x));
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, false, PluginName);
			}
		}


		/// <summary>Updates theme/UI resources for the given game.</summary>
		public virtual void SetThemesResources(Game game) { }

		#endregion

		#region CSV extraction

		public bool ExtractToCsv()
		{
			return PluginExportCsv.ExportToCsv(PluginName, _database.FindAll());
		}

		#endregion

		#region Cache management

		/// <summary>Deletes the plugin's on-disk file cache with a progress dialog.</summary>
		public bool ClearCache()
		{
			bool isOk = true;

			string cacheDir = Path.Combine(PlaynitePaths.DataCachePath, PluginName);
			GlobalProgressOptions options = new GlobalProgressOptions(
				string.Format("{0} - {1}", PluginName,
					ResourceProvider.GetString("LOCCommonProcessing")))
			{
				Cancelable = false,
				IsIndeterminate = true
			};

			API.Instance.Dialogs.ActivateGlobalProgress((a) =>
			{
				Thread.Sleep(2000);

				if (!Directory.Exists(cacheDir))
				{
					Logger.Info(string.Format("Cache directory does not exist, nothing to clear: {0}", cacheDir));
					return;
				}

				// Delete all files recursively, logging each one individually.
				foreach (string file in Directory.GetFiles(cacheDir, "*", SearchOption.AllDirectories))
				{
					try
					{
						File.Delete(file);
						Logger.Info(string.Format("Cache file deleted: {0}", file));
					}
					catch (Exception ex)
					{
						isOk = false;
						Common.LogError(ex, false,
							string.Format("Failed to delete cache file: {0}", file),
							false, PluginName);
						API.Instance.Dialogs.ShowErrorMessage(
							string.Format(
								ResourceProvider.GetString("LOCCommonErrorDeleteCache"), file),
							PluginName);
					}
				}

				// Delete all subdirectories once files are gone.
				foreach (string subDir in Directory.GetDirectories(cacheDir))
				{
					try
					{
						Directory.Delete(subDir, true);
						Logger.Info(string.Format("Cache subdirectory deleted: {0}", subDir));
					}
					catch (Exception ex)
					{
						isOk = false;
						Common.LogError(ex, false,
							string.Format("Failed to delete cache subdirectory: {0}", subDir),
							false, PluginName);
						API.Instance.Dialogs.ShowErrorMessage(
							string.Format(
								ResourceProvider.GetString("LOCCommonErrorDeleteCache"), subDir),
							PluginName);
					}
				}

				// Invalidate the in-memory file cache so subsequent requests
				// do not return stale paths pointing to deleted files.
				HttpFileCacheService.ClearAllCache();
				Logger.Info(string.Format("Cache cleared: {0}", cacheDir));

			}, options);

			return isOk;
		}

		#endregion

		#region Private utilities

		private static string BuildProgressText(
			string message, double current, int total, Game game)
		{
			string gameLine = game == null
				? string.Empty
				: "\n" + game.Name + (game.Source == null
					? string.Empty
					: string.Format(" ({0})", game.Source.Name));

			string counterLine = total == 1
				? string.Empty
				: string.Format("\n\n{0}/{1}", current, total);

			return message + counterLine + gameLine;
		}

		private bool IsTaggingEnabled()
		{
			object settings = PluginSettings.GetType()
				.GetProperty("Settings")?.GetValue(PluginSettings);
			PropertyInfo prop = settings?.GetType().GetProperty("EnableTag");
			return prop != null && (bool)prop.GetValue(settings);
		}

		private bool IsAutoImportOnInstalledEnabled()
		{
			object settings = PluginSettings.GetType()
				.GetProperty("Settings")?.GetValue(PluginSettings);
			PropertyInfo prop = settings?.GetType().GetProperty("AutoImportOnInstalled");
			return prop != null && (bool)prop.GetValue(settings);
		}

		private void NotifyError(string operation, Exception ex)
		{
			API.Instance.Notifications.Add(new NotificationMessage(
				string.Format("{0}-Error-{1}", PluginName, operation),
				string.Format("{0}\n{1}", PluginName, ex.Message),
				NotificationType.Error,
				() => PlayniteTools.CreateLogPackage(PluginName)));
		}

		#endregion
	}
}