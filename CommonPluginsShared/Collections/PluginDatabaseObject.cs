using CommonPlayniteShared;
using CommonPlayniteShared.Common;
using CommonPluginsControls.Controls;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Models;
using CommonPluginsShared.Caching;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommonPluginsShared.Services;

namespace CommonPluginsShared.Collections
{
	/// <summary>
	/// Abstract base class for plugin database objects. Provides CRUD, tag management,
	/// refresh orchestration, and CSV extraction for a typed plugin data store.
	/// </summary>
	/// <typeparam name="TSettings">Settings view-model type implementing <see cref="ISettings"/>.</typeparam>
	/// <typeparam name="TDatabase">Typed collection inheriting <see cref="PluginItemCollection{TItem}"/>.</typeparam>
	/// <typeparam name="TItem">Database item type inheriting <see cref="PluginDataBaseGameBase"/>.</typeparam>
	/// <typeparam name="T">Inner data type used to parameterise <see cref="PluginDataBaseGame{T}"/>.</typeparam>
	public abstract class PluginDatabaseObject<TSettings, TDatabase, TItem, T> : ObservableObject, IPluginDatabase
		where TSettings : ISettings
		where TDatabase : PluginItemCollection<TItem>
		where TItem : PluginDataBaseGameBase
	{
		protected static readonly ILogger Logger = LogManager.GetLogger();

		/// <inheritdoc cref="IPluginDatabase.PluginName"/>
		public string PluginName { get; set; }

		/// <summary>Gets or sets the strongly-typed plugin settings view model.</summary>
		public TSettings PluginSettings { get; set; }

		/// <summary>Gets or sets plugin-specific paths (database, cache, installation directory).</summary>
		public PluginPaths Paths { get; set; }

		/// <summary>
		/// Timeout in milliseconds to wait for the database to finish loading.
		/// Override to adjust per-plugin (default: 10 000 ms).
		/// </summary>
		protected virtual int DatabaseLoadTimeout => 10000;

		/// <summary>
		/// Gets or sets the current game displayed in the active UI panel.
		/// Used by theme resource callbacks.
		/// </summary>
		public Game GameContext { get; set; }

		/// <inheritdoc cref="IPluginDatabase.WindowPluginService"/>
		public IWindowPluginService WindowPluginService { get; set; }

		/// <summary>
		/// Prefix prepended to every tag created by this plugin (e.g. <c>"[SC]"</c>).
		/// Leave empty to create un-prefixed tags.
		/// </summary>
		protected string TagBefore { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether a "No Data" tag should be added
		/// to games whose plugin data is missing.
		/// </summary>
		public bool TagMissing { get; set; } = false;

		// ── Database backing field ────────────────────────────────────────────────

		internal TDatabase _database;

		/// <summary>
		/// Gets the database, blocking until it has finished loading (up to <see cref="DatabaseLoadTimeout"/>).
		/// Throws <see cref="TimeoutException"/> on timeout.
		/// </summary>
		public TDatabase Database
		{
			get
			{
				WaitForDatabaseLoad();
				return _database;
			}
			set { _database = value; }
		}

		// ── IsLoaded ─────────────────────────────────────────────────────────────

		private bool _isLoaded = false;

		/// <inheritdoc cref="IPluginDatabase.IsLoaded"/>
		public bool IsLoaded { get => _isLoaded; set => SetValue(ref _isLoaded, value); }

		// ── Tag cache ─────────────────────────────────────────────────────────────

		/// <summary>Lazily-populated cache of tags that belong to this plugin.</summary>
		private List<Tag> _pluginTagsCache;
		private bool _pluginTagsCacheInitialized;

		/// <summary>Gets all Playnite tags that start with <see cref="TagBefore"/>.</summary>
		protected IEnumerable<Tag> PluginTags => GetPluginTags();

		private IEnumerable<Guid> PreviousIds { get; set; } = new List<Guid>();

        /// <summary>
        /// Initialises paths, cache directories, and subscribes to Playnite game events.
        /// </summary>
        /// <param name="pluginSettings">Plugin settings view model.</param>
        /// <param name="pluginName">Human-readable plugin name used for logging and file naming.</param>
        /// <param name="pluginUserDataPath">Root path for all plugin user data.</param>
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

			FileSystem.CreateDirectory(Paths.PluginDatabasePath);
			FileSystem.CreateDirectory(Paths.PluginCachePath);

			API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;
			API.Instance.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
		}

		#region Database access helpers

		/// <summary>
		/// Blocks until <see cref="IsLoaded"/> becomes true or <see cref="DatabaseLoadTimeout"/> elapses.
		/// </summary>
		/// <exception cref="TimeoutException">Thrown when the database does not load in time.</exception>
		private void WaitForDatabaseLoad()
		{
			if (IsLoaded)
			{
				return;
			}

			Logger.Info($"Waiting for database to load (timeout: {DatabaseLoadTimeout} ms)…");

			bool loaded = SpinWait.SpinUntil(() => IsLoaded, DatabaseLoadTimeout);
			if (!loaded)
			{
				string message = $"Database load timeout after {DatabaseLoadTimeout} ms";
				Logger.Error(message);
				throw new TimeoutException(message);
			}

			Logger.Info("Database loaded successfully.");
		}

		/// <summary>
		/// Returns the database without throwing on timeout; logs a warning and returns <c>null</c> instead.
		/// </summary>
		protected TDatabase GetDatabaseSafe()
		{
			try
			{
				return Database;
			}
			catch (TimeoutException ex)
			{
				Logger.Warn($"Database access timeout: {ex.Message}");
				return null;
			}
		}

		/// <summary>Returns <c>true</c> if the database is ready for immediate access.</summary>
		public bool IsDatabaseReady() => IsLoaded && _database != null;

		#endregion

		#region Shared UI/tag helpers (usable by derived classes and this class)

		/// <summary>
		/// Adds <paramref name="tagId"/> to <paramref name="game"/>'s tag list if not already present.
		/// Handles the <c>null</c> initialisation case.
		/// </summary>
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

		/// <summary>
		/// Persists <paramref name="game"/> changes to the Playnite database on the UI dispatcher thread.
		/// </summary>
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

				IsLoaded = LoadDatabase();

				if (IsLoaded)
				{
					Database.ItemCollectionChanged += Database_ItemCollectionChanged;
					Database.ItemUpdated += Database_ItemUpdated;
				}

				return IsLoaded;
			});
		}

		/// <summary>
		/// Instantiates the typed collection, populates game info, removes stale entries,
		/// and calls <see cref="LoadMoreData"/> for plugin-specific initialisation.
		/// </summary>
		protected bool LoadDatabase()
		{
			try
			{
				Stopwatch stopWatch = Stopwatch.StartNew();

				_database = ObjectExtensions.CrateInstance<TDatabase>(typeof(TDatabase), Paths.PluginDatabasePath, GameDatabaseCollection.Uknown);
				_database.SetGameInfo<T>();

				DeleteDataWithDeletedGame();
				LoadMoreData();

				stopWatch.Stop();
				TimeSpan ts = stopWatch.Elapsed;
				Logger.Info(string.Format(
					"LoadDatabase — {0} items in {1:00}:{2:00}.{3:00}",
					_database.Count,
					ts.Minutes, ts.Seconds, ts.Milliseconds / 10));

				return true;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return false;
			}
		}

		/// <summary>
		/// Override to perform additional plugin-specific initialisation after the base database loads.
		/// Exceptions thrown here will cause <see cref="LoadDatabase"/> to return <c>false</c>.
		/// </summary>
		protected virtual void LoadMoreData() { }

		/// <inheritdoc/>
		public bool ClearDatabase()
		{
			bool isOk = true;

			GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
			{
				Cancelable = false,
				IsIndeterminate = false
			};

			API.Instance.Dialogs.ActivateGlobalProgress((a) =>
			{
				a.ProgressMaxValue = Database.Items.Count();
				Database.Items.ForEach(x =>
				{
					try
					{
						Remove(x.Key);
						a.CurrentProgressValue++;
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, $"Error clearing {x.Key} — {x.Value.Name}", false, PluginName);
						isOk = false;
					}
				});
			}, options);

			return isOk;
		}

		/// <summary>Removes database entries whose corresponding Playnite game no longer exists.</summary>
		public virtual void DeleteDataWithDeletedGame()
		{
			IEnumerable<KeyValuePair<Guid, TItem>> deleted = _database.Items
				.Where(x => API.Instance.Database.Games.Get(x.Key) == null);

			deleted.ForEach(x =>
			{
				Logger.Info($"Deleting orphaned data: {x.Value.Name} ({x.Key})");
				_database.Remove(x.Key);
			});
		}

		#endregion

		#region Database event handlers

		private void Database_ItemUpdated(object sender, ItemUpdatedEventArgs<TItem> e)
		{
			if (GameContext == null)
			{
				return;
			}

			ItemUpdateEvent<TItem> match = e.UpdatedItems.Find(x => x.NewData.Id == GameContext.Id);
			if (match?.NewData?.Id != null)
			{
				SetThemesResources(GameContext);
			}
		}

		private void Database_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<TItem> e)
		{
			if (GameContext != null)
			{
				SetThemesResources(GameContext);
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

		/// <summary>Returns all Playnite games that have a corresponding entry in the plugin database.</summary>
		public virtual IEnumerable<Game> GetGamesList()
		{
			foreach (KeyValuePair<Guid, TItem> item in Database.Items)
			{
				Game game = API.Instance.Database.Games.Get(item.Key);
				if (game != null)
				{
					yield return game;
				}
			}
		}

		/// <inheritdoc/>
		public virtual IEnumerable<Game> GetGamesWithNoData()
		{
			IEnumerable<Game> withNoData = Database.Items
				.Where(x => !x.Value.HasData)
				.Select(x => API.Instance.Database.Games.Get(x.Key))
				.Where(x => x != null);

			IEnumerable<Game> notInDb = API.Instance.Database.Games
				.Where(x => !Database.Items.Any(y => y.Key == x.Id));

			return withNoData.Union(notInDb).Distinct().Where(x => !x.Hidden);
		}

		/// <inheritdoc/>
		public virtual IEnumerable<Game> GetGamesOldData(int months)
		{
			return Database.Items
				.Where(x => x.Value.DateLastRefresh <= DateTime.Now.AddMonths(-months))
				.Select(x => API.Instance.Database.Games.Get(x.Key))
				.Where(x => x != null);
		}

		/// <summary>Returns a projection of all database entries as <see cref="DataGame"/> view models.</summary>
		public virtual IEnumerable<DataGame> GetDataGames()
		{
			return Database.Items.Select(x => new DataGame
			{
				Id = x.Value.Id,
				Icon = x.Value.Icon.IsNullOrEmpty() ? x.Value.Icon : API.Instance.Database.GetFullFilePath(x.Value.Icon),
				Name = x.Value.Name,
				IsDeleted = x.Value.IsDeleted,
				CountData = x.Value.Count
			}).Distinct();
		}

		/// <summary>Returns database entries that are marked as deleted (their Playnite game was removed).</summary>
		public virtual IEnumerable<DataGame> GetIsolatedDataGames()
		{
			return Database.Items.Where(x => x.Value.IsDeleted).Select(x => new DataGame
			{
				Id = x.Value.Id,
				Icon = x.Value.Icon.IsNullOrEmpty() ? x.Value.Icon : API.Instance.Database.GetFullFilePath(x.Value.Icon),
				Name = x.Value.Name,
				IsDeleted = x.Value.IsDeleted,
				CountData = x.Value.Count
			}).Distinct();
		}

		#endregion

		#region CRUD

		/// <summary>Creates a minimal default item for <paramref name="id"/>. Returns <c>null</c> if the game does not exist.</summary>
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

		/// <summary>Adds a new item to the database and optionally applies tags.</summary>
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
				API.Instance.MainView.UIDispatcher?.Invoke(() => Database?.Add(itemToAdd), DispatcherPriority.Send);

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

		/// <summary>
		/// Updates an existing item in the database.
		/// Performs an optimistic in-memory update before dispatching to the UI thread
		/// to reduce the window where stale data might be read by concurrent callers.
		/// </summary>
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

				// Optimistic in-memory swap before the UI-thread write.
				TItem cached = Get(itemToUpdate.Id, true);
				Database?.Items.TryUpdate(itemToUpdate.Id, itemToUpdate, cached);
				API.Instance.MainView.UIDispatcher?.Invoke(() => Database?.Update(itemToUpdate), DispatcherPriority.Send);

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

		/// <inheritdoc/>
		public virtual bool Remove(Guid id)
		{
			RemoveTag(id);

			if (!Database.Items.ContainsKey(id))
			{
				return false;
			}

			return (bool)API.Instance.MainView.UIDispatcher?.Invoke(() => Database.Remove(id));
		}

		/// <summary>
		/// Delegates to <see cref="Remove(IEnumerable{Guid})"/> so buffer-update wrapping is applied consistently.
		/// </summary>
		public virtual void Remove(List<Guid> ids) => Remove((IEnumerable<Guid>)ids);

		/// <inheritdoc/>
		public virtual bool Remove(IEnumerable<Guid> ids)
		{
			Logger.Info("Remove(IEnumerable<Guid>) started.");
			API.Instance.Database.Games.BeginBufferUpdate();
			Database.BeginBufferUpdate();

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

			API.Instance.Database.Games.EndBufferUpdate();
			Database.EndBufferUpdate();
			return true;
		}

		// ── Cache accessors ───────────────────────────────────────────────────────

		/// <summary>Returns the cached item for <paramref name="id"/> without hitting any web source.</summary>
		public virtual TItem GetOnlyCache(Guid id) => Database?.Get(id);

		/// <summary>Returns the cached item for <paramref name="game"/> without hitting any web source.</summary>
		public virtual TItem GetOnlyCache(Game game) => Database?.Get(game.Id);

		/// <summary>Returns a deep clone of the cached item for <paramref name="id"/>.</summary>
		public virtual TItem GetClone(Guid id) => Serialization.GetClone(Get(id, true, false));

		/// <summary>Returns a deep clone of the cached item for <paramref name="game"/>.</summary>
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

		// ── Explicit IPluginDatabase implementations ──────────────────────────────

		PluginDataBaseGameBase IPluginDatabase.Get(Game game, bool onlyCache, bool force) => Get(game, onlyCache, force);
		PluginDataBaseGameBase IPluginDatabase.Get(Guid id, bool onlyCache, bool force) => Get(id, onlyCache, force);
		PluginDataBaseGameBase IPluginDatabase.GetClone(Game game) => GetClone(game);
		PluginDataBaseGameBase IPluginDatabase.GetClone(Guid id) => GetClone(id);
		void IPluginDatabase.AddOrUpdate(PluginDataBaseGameBase item) => AddOrUpdate((TItem)item);

		#endregion

		#region Refresh

		/// <summary>Refreshes a single game with a progress dialog.</summary>
		public void Refresh(Game game) => Refresh(game.Id);

		/// <summary>Refreshes a single game by ID with a progress dialog.</summary>
		public void Refresh(Guid id)
		{
			GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
			{
				Cancelable = false,
				IsIndeterminate = true
			};

			API.Instance.Dialogs.ActivateGlobalProgress(
				a => RefreshNoLoader(id, a.CancelToken), options);
		}

		/// <inheritdoc/>
		public void Refresh(IEnumerable<Guid> ids)
		{
			Logger.Info("Refresh() started.");
			Refresh(ids, ResourceProvider.GetString("LOCCommonProcessing"));
		}

		/// <summary>Refreshes a batch of games, showing a cancellable progress dialog.</summary>
		public virtual void Refresh(IEnumerable<Guid> ids, string message)
		{
			List<Guid> idList = ids.ToList();

			GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {message}")
			{
				Cancelable = true,
				IsIndeterminate = idList.Count == 1
			};

			API.Instance.Dialogs.ActivateGlobalProgress((a) =>
			{
				API.Instance.Database.BeginBufferUpdate();
				Database.BeginBufferUpdate();

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

				Database.EndBufferUpdate();
				API.Instance.Database.EndBufferUpdate();
			}, options);
		}

		/// <summary>Refreshes all items that currently have data.</summary>
		public virtual void RefreshAll()
		{
			IEnumerable<Guid> ids = _database.Where(x => x.HasData).Select(x => x.Id);
			Refresh(ids);
		}

		/// <summary>
		/// Core refresh logic without a progress dialog.
		/// Fetches from the web and calls <see cref="Update"/> if the data changed.
		/// </summary>
		public virtual void RefreshNoLoader(Guid id, CancellationToken cancellationToken = default(CancellationToken))
		{
			Game game = API.Instance.Database.Games.Get(id);
			Logger.Info($"RefreshNoLoader — {game?.Name} ({id})");

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
			Logger.Info($"RefreshInstalled — {ids.Count()} game(s) queued.");
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

			object settings = PluginSettings.GetType().GetProperty("Settings").GetValue(PluginSettings);
			PropertyInfo propertyInfo = settings.GetType().GetProperty("LastAutoLibUpdateAssetsDownload");

			DateTime since = propertyInfo != null
				? (DateTime)propertyInfo.GetValue(settings)
				: DateTime.Now.AddMonths(-1);

			if (propertyInfo == null)
			{
				Logger.Warn("LastAutoLibUpdateAssetsDownload not found in settings; defaulting to -1 month.");
			}

			IEnumerable<Guid> ids = API.Instance.Database.Games
				.Where(x => x.Added != null && x.Added > since)
				.Select(x => x.Id);

			Logger.Info($"RefreshRecent — {ids.Count()} game(s) queued.");
			Refresh(ids, ResourceProvider.GetString("LOCCommonGettingNewDatas"));
		}

		/// <inheritdoc/>
		[Obsolete("Use Refresh(ids)")]
		public virtual void RefreshWithNoData(IEnumerable<Guid> ids) => Refresh(ids);

		/// <inheritdoc/>
		public virtual PluginDataBaseGameBase MergeData(Guid fromId, Guid toId) => null;

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
				.Where(t => !t.Name.IsNullOrEmpty() && t.Name.StartsWith(TagBefore, StringComparison.Ordinal))
				.ToList();

			return _pluginTagsCache;
		}

		/// <summary>Invalidates the tag cache so it is rebuilt on next access.</summary>
		private void ResetPluginTagsCache()
		{
			_pluginTagsCacheInitialized = false;
		}

		/// <summary>
		/// Returns the ID of the tag with the full prefixed name, creating it if it does not exist.
		/// Returns <c>null</c> if creation fails.
		/// </summary>
		internal Guid? CheckTagExist(string tagName)
		{
			string fullName = TagBefore.IsNullOrEmpty()
				? tagName
				: $"{TagBefore} {tagName}";

			Tag existing = PluginTags.FirstOrDefault(t => string.Equals(t.Name, fullName, StringComparison.Ordinal));
			if (existing != null)
			{
				return existing.Id;
			}

			API.Instance.Database.Tags.Add(new Tag { Name = fullName });
			ResetPluginTagsCache();

			Tag created = PluginTags.FirstOrDefault(t => string.Equals(t.Name, fullName, StringComparison.Ordinal));
			return created?.Id;
		}

		/// <summary>Returns the ID of the localised "No Data" tag, creating it if needed.</summary>
		public Guid? AddNoDataTag()
			=> CheckTagExist(ResourceProvider.GetString("LOCNoData"));

		/// <summary>
		/// Adds the appropriate plugin tag to <paramref name="game"/>.
		/// Override in derived classes to customise tag selection logic.
		/// </summary>
		public virtual void AddTag(Game game)
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
					}
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, $"Tag insert error — {game.Name}", true, PluginName,
						string.Format(ResourceProvider.GetString("LOCCommonNotificationTagError"), game.Name));
					return;
				}
			}
			else if (TagMissing)
			{
				Guid? noDataTagId = AddNoDataTag();
				if (noDataTagId != null)
				{
					AppendTagId(game, noDataTagId.Value);
				}
			}

			PersistGameUpdate(game);
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
			IEnumerable<Guid> ids = API.Instance.Database.Games.Where(x => !x.Hidden).Select(x => x.Id);
			AddTag(ids, $"{PluginName} - {ResourceProvider.GetString("LOCCommonAddingAllTag")}");
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

			AddTag(playniteDb.Select(x => x.Id), $"{PluginName} - {ResourceProvider.GetString("LOCCommonAddingAllTag")}");
			TagMissing = false;
		}

		/// <summary>Adds plugin tags to a batch of games with a cancellable progress dialog.</summary>
		public void AddTag(IEnumerable<Guid> ids, string message)
		{
			List<Guid> idList = ids.ToList();
			if (idList.Count == 0)
			{
				return;
			}

			GlobalProgressOptions options = new GlobalProgressOptions(message)
			{
				Cancelable = true,
				IsIndeterminate = idList.Count == 1
			};

			API.Instance.Dialogs.ActivateGlobalProgress((a) =>
			{
				try
				{
					API.Instance.Database.BeginBufferUpdate();
					API.Instance.Database.Games.BeginBufferUpdate();

					Stopwatch stopWatch = Stopwatch.StartNew();
					a.ProgressMaxValue = idList.Count;

					foreach (Guid id in idList)
					{
						if (a.CancelToken.IsCancellationRequested)
						{
							break;
						}

						Game game = API.Instance.Database.Games.Get(id);
						if (game == null)
						{
							continue;
						}

						a.Text = BuildProgressText(message, a.CurrentProgressValue, idList.Count, game);

						Thread.Sleep(10);

						try
						{
							RemoveTag(game);
							AddTag(game);
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
						"AddTag(){0} — {1:00}:{2:00}.{3:00} for {4}/{5} items",
						a.CancelToken.IsCancellationRequested ? " (canceled)" : string.Empty,
						ts.Minutes, ts.Seconds, ts.Milliseconds / 10,
						a.CurrentProgressValue, idList.Count));
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

		/// <summary>Removes all plugin tags from <paramref name="game"/>.</summary>
		public void RemoveTag(Game game)
		{
			if (game?.TagIds == null)
			{
				return;
			}

			game.TagIds = game.TagIds.Where(x => !PluginTags.Any(y => x == y.Id)).ToList();
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
				? $"{PluginName} - {ResourceProvider.GetString("LOCCommonClearingAllTag")}"
				: $"{PluginName} - {ResourceProvider.GetString("LOCCommonRemovingAllTag")}";

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
					List<Game> playniteDb = API.Instance.Database.Games.Where(x => !x.Hidden).ToList();
					a.ProgressMaxValue = playniteDb.Count;

					foreach (Game game in playniteDb)
					{
						if (a.CancelToken.IsCancellationRequested)
						{
							break;
						}

						a.Text = BuildProgressText(message, a.CurrentProgressValue, playniteDb.Count, game);

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

		/// <summary>
		/// Resolves the tag ID to apply to a game. Override to implement custom tag selection.
		/// </summary>
		internal virtual Guid? FindGoodPluginTags(string tagName) => CheckTagExist(tagName);

		#endregion

		#region Playnite event handlers

		/// <summary>
		/// Responds to Playnite game updates: refreshes game info in the plugin database
		/// and optionally triggers a data refresh for newly-installed games.
		/// </summary>
		public virtual void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
		{
			try
			{
				if (e?.UpdatedItems?.Count > 0 && Database != null)
				{
					e.UpdatedItems.ForEach(x =>
					{
						if (x.NewData?.Id != null)
						{
							Database.SetGameInfo<T>(x.NewData.Id);
							ActionAfterGames_ItemUpdated(x.OldData, x.NewData);
						}
					});

					if (IsAutoImportOnInstalledEnabled())
					{
						List<Guid> newlyInstalled = e.UpdatedItems
							.Where(x => !x.OldData.IsInstalled && x.NewData.IsInstalled && !PreviousIds.Contains(x.NewData.Id))
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

		/// <summary>Called after each individual game update event. Override to react to specific field changes.</summary>
		public virtual void ActionAfterGames_ItemUpdated(Game gameOld, Game gameNew) { }

		private void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
		{
			try
			{
				e?.RemovedItems?.ForEach(x => Remove(x));
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, false, PluginName);
			}
		}

		/// <summary>Updates theme/UI resources for the given game. Override to push data to theme bindings.</summary>
		public virtual void SetThemesResources(Game game) { }

		#endregion

		#region CSV extraction

		/// <summary>
		/// Exports all plugin data to a timestamped CSV file and opens the containing folder in Explorer.
		/// </summary>
		/// <param name="path">Target directory for the output file.</param>
		/// <param name="minimum">When <c>true</c>, exports minimum requirement data; otherwise recommended.</param>
		/// <returns><c>true</c> if the file was written successfully.</returns>
		public bool ExtractToCsv(string path, bool minimum)
		{
			bool isOk = false;

			try
			{
				Logger.Info($"ExtractToCsv(minimum={minimum}) started.");

				GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonExtracting")}")
				{
					Cancelable = true,
					IsIndeterminate = false
				};

				API.Instance.Dialogs.ActivateGlobalProgress((a) =>
				{
					Stopwatch stopWatch = Stopwatch.StartNew();

					try
					{
						ulong totalItems = 0;
						foreach (TItem item in Database.Items?.Values)
						{
							totalItems += item.Count;
						}
						a.ProgressMaxValue = totalItems;

						string filePath = CommonPlayniteShared.Common.Paths.FixPathLength(
							Path.Combine(path, $"{PluginName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv"));

						FileSystem.PrepareSaveFile(filePath);
						string csvData = GetCsvData(a, minimum);

						if (!csvData.IsNullOrEmpty() && !a.CancelToken.IsCancellationRequested)
						{
							File.WriteAllText(filePath, csvData, Encoding.UTF8);
							System.Diagnostics.Process.Start("explorer.exe", path);
							isOk = true;
						}
						else if (csvData.IsNullOrEmpty())
						{
							Logger.Warn($"No CSV data available for {PluginName}.");
						}
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, true, PluginName);
					}

					stopWatch.Stop();
					TimeSpan ts = stopWatch.Elapsed;
					Logger.Info(string.Format(
						"ExtractToCsv(minimum={0}){1} — {2:00}:{3:00}.{4:00} for {5} items",
						minimum,
						a.CancelToken.IsCancellationRequested ? " (canceled)" : string.Empty,
						ts.Minutes, ts.Seconds, ts.Milliseconds / 10,
						Database.Items?.Count()));
				}, options);
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
			}

			return isOk;
		}

		/// <summary>
		/// Override to produce CSV content. Return <c>null</c> or empty to skip file creation.
		/// </summary>
		internal virtual string GetCsvData(GlobalProgressActionArgs a, bool minimum) => null;

		#endregion

		#region Cache management

		/// <summary>Deletes the plugin's on-disk file cache with a progress dialog.</summary>
		public void ClearCache()
		{
			string cacheDir = Path.Combine(PlaynitePaths.DataCachePath, PluginName);

			GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
			{
				Cancelable = false,
				IsIndeterminate = true
			};

			API.Instance.Dialogs.ActivateGlobalProgress((a) =>
			{
				try
				{
					Thread.Sleep(2000);
					FileSystem.DeleteDirectory(cacheDir, true);
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, false, PluginName);
					API.Instance.Dialogs.ShowErrorMessage(
						string.Format(ResourceProvider.GetString("LOCCommonErrorDeleteCache"), cacheDir),
						PluginName);
				}
			}, options);
		}

		#endregion

		#region Private utilities

		/// <summary>
		/// Builds the progress dialog text line shown during batch operations.
		/// </summary>
		private static string BuildProgressText(string message, double current, int total, Game game)
		{
			string gameLine = game == null
				? string.Empty
				: "\n" + game.Name + (game.Source == null ? string.Empty : $" ({game.Source.Name})");

			string counterLine = total == 1
				? string.Empty
				: $"\n\n{current}/{total}";

			return message + counterLine + gameLine;
		}

		/// <summary>
		/// Returns <c>true</c> if the <c>EnableTag</c> setting is active on the plugin settings object.
		/// Uses reflection because <typeparamref name="TSettings"/> is not constrained to expose the property.
		/// </summary>
		private bool IsTaggingEnabled()
		{
			object settings = PluginSettings.GetType().GetProperty("Settings")?.GetValue(PluginSettings);
			PropertyInfo prop = settings?.GetType().GetProperty("EnableTag");
			return prop != null && (bool)prop.GetValue(settings);
		}

		/// <summary>
		/// Returns <c>true</c> if the <c>AutoImportOnInstalled</c> setting is active.
		/// </summary>
		private bool IsAutoImportOnInstalledEnabled()
		{
			object settings = PluginSettings.GetType().GetProperty("Settings")?.GetValue(PluginSettings);
			PropertyInfo prop = settings?.GetType().GetProperty("AutoImportOnInstalled");
			return prop != null && (bool)prop.GetValue(settings);
		}

		/// <summary>Sends a Playnite UI notification for a database operation error.</summary>
		private void NotifyError(string operation, Exception ex)
		{
			API.Instance.Notifications.Add(new NotificationMessage(
				$"{PluginName}-Error-{operation}",
				$"{PluginName}\n{ex.Message}",
				NotificationType.Error,
				() => PlayniteTools.CreateLogPackage(PluginName)));
		}

		#endregion
	}
}