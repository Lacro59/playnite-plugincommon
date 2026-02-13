using CommonPlayniteShared;
using CommonPlayniteShared.Common;
using CommonPluginsControls.Controls;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Models;
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

namespace CommonPluginsShared.Collections
{
	/// <summary>
	/// Abstract class representing a plugin database object.
	/// </summary>
	/// <typeparam name="TSettings">Type of settings.</typeparam>
	/// <typeparam name="TDatabase">Type of database.</typeparam>
	/// <typeparam name="TItem">Type of database item.</typeparam>
	/// <typeparam name="T">Type of data.</typeparam>
	public abstract class PluginDatabaseObject<TSettings, TDatabase, TItem, T> : ObservableObject, IPluginDatabase
		where TSettings : ISettings
		where TDatabase : PluginItemCollection<TItem>
		where TItem : PluginDataBaseGameBase
	{
		protected static readonly ILogger Logger = LogManager.GetLogger();

		public TSettings PluginSettings { get; set; }
		public UI UI { get; set; } = new UI();
		public string PluginName { get; set; }
		public PluginPaths Paths { get; set; }
		/// <summary>
		/// Timeout in milliseconds for waiting for database to load (default: 10 seconds)
		/// </summary>
		protected virtual int DatabaseLoadTimeout => 10000;

		internal TDatabase _database;

		/// <summary>
		/// Gets the database, ensuring it is loaded before access.
		/// Uses SpinWait with timeout only for external access (not during loading).
		/// </summary>
		public TDatabase Database
		{
			get
			{
				WaitForDatabaseLoad();
				return _database;
			}
			set
			{
				_database = value;
			}
		}

		/// <summary>
		/// Waits for the database to be loaded with a configurable timeout.
		/// Throws TimeoutException if database is not loaded within the timeout period.
		/// </summary>
		private void WaitForDatabaseLoad()
		{
			if (IsLoaded)
			{
				return;
			}

			Logger.Info($"Waiting for database to load (timeout: {DatabaseLoadTimeout}ms)...");

			bool loaded = SpinWait.SpinUntil(() => IsLoaded, DatabaseLoadTimeout);

			if (!loaded)
			{
				string errorMessage = $"Database load timeout after {DatabaseLoadTimeout}ms";
				Logger.Error(errorMessage);
				throw new TimeoutException(errorMessage);
			}

			Logger.Info("Database loaded successfully");
		}

		/// <summary>
		/// Safely accesses the database without throwing exception on timeout.
		/// Returns null if database is not loaded within timeout period.
		/// </summary>
		/// <returns>Database instance or null if not loaded</returns>
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

		/// <summary>
		/// Checks if database is loaded without waiting.
		/// </summary>
		/// <returns>True if database is ready to use</returns>
		public bool IsDatabaseReady()
		{
			return IsLoaded && _database != null;
		}

		public Game GameContext { get; set; }

		protected string TagBefore { get; set; } = string.Empty;
		protected IEnumerable<Tag> PluginTags => GetPluginTags();

		private bool _isLoaded = false;
		public bool IsLoaded { get => _isLoaded; set => SetValue(ref _isLoaded, value); }
		public bool IsViewOpen { get; set; } = false;
		public bool TagMissing { get; set; } = false;

		private IEnumerable<Guid> PreviousIds { get; set; } = new List<Guid>();

		/// <summary>
		/// Constructor for PluginDatabaseObject.
		/// </summary>
		/// <param name="pluginSettings">Plugin settings.</param>
		/// <param name="pluginName">Name of the plugin.</param>
		/// <param name="pluginUserDataPath">Path for plugin user data.</param>
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
			HttpFileCachePlugin.CacheDirectory = Paths.PluginCachePath;

			FileSystem.CreateDirectory(Paths.PluginDatabasePath);
			FileSystem.CreateDirectory(Paths.PluginCachePath);

			API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;
			API.Instance.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
		}

		#region Database Initialization and Management

		/// <summary>
		/// Initializes the database.
		/// </summary>
		/// <returns>Task with a boolean indicating success.</returns>
		public Task<bool> InitializeDatabase()
		{
			return Task.Run(() =>
			{
				if (IsLoaded)
				{
					Logger.Info($"Database is already initialized");
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
		/// Loads the plugin's database using the provided generic types.
		/// </summary>
		/// <returns>True if the database was loaded successfully.</returns>
		protected bool LoadDatabase()
		{
			try
			{
				Stopwatch stopWatch = new Stopwatch();
				stopWatch.Start();

				_database = ObjectExtensions.CrateInstance<TDatabase>(typeof(TDatabase), Paths.PluginDatabasePath, GameDatabaseCollection.Uknown);
				_database.SetGameInfo<T>();

				DeleteDataWithDeletedGame();
				LoadMoreData();

				stopWatch.Stop();
				TimeSpan ts = stopWatch.Elapsed;
				Logger.Info(string.Format(
					"LoadDatabase with {0} items - {1:00}:{2:00}.{3:00}",
					_database.Count,
					ts.Minutes,
					ts.Seconds,
					ts.Milliseconds / 10));

				return true;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return false;
			}
		}

		protected virtual void LoadMoreData()
		{
		}

		/// <summary>
		/// Clears the database.
		/// </summary>
		/// <returns>True if the database was cleared successfully.</returns>
		public bool ClearDatabase()
		{
			bool IsOk = true;

			GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
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
						Common.LogError(ex, false, $"Error on {x.Key} - {x.Value.Name}", false, PluginName);
						IsOk = false;
					}
				});
			}, globalProgressOptions);

			return IsOk;
		}

		/// <summary>
		/// Deletes data associated with deleted games.
		/// </summary>
		public virtual void DeleteDataWithDeletedGame()
		{
			IEnumerable<KeyValuePair<Guid, TItem>> GamesDeleted = _database.Items.Where(x => API.Instance.Database.Games.Get(x.Key) == null).Select(x => x);
			GamesDeleted.ForEach(x =>
			{
				Logger.Info($"Delete data for missing game: {x.Value.Name} - {x.Key}");
				_database.Remove(x.Key);
			});
		}

		private void Database_ItemUpdated(object sender, ItemUpdatedEventArgs<TItem> e)
		{
			if (GameContext == null)
			{
				return;
			}

			ItemUpdateEvent<TItem> ActualItem = e.UpdatedItems.Find(x => x.NewData.Id == GameContext.Id);
			if (ActualItem?.NewData?.Id != null)
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

		/// <summary>
		/// Opens a dialog window to select and filter games for data download.
		/// Refreshes the selected games based on the filter criteria.
		/// </summary>
		public virtual void GetSelectData()
		{
			OptionsDownloadData view = new OptionsDownloadData(this);
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginName + " - " + ResourceProvider.GetString("LOCCommonSelectData"), view);
			windowExtension.ShowDialog();

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
		/// Retrieves a list of all games present in the database.
		/// </summary>
		/// <returns>An enumerable collection of games.</returns>
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

		/// <summary>
		/// Retrieves a list of games that have no associated data in the database.
		/// </summary>
		/// <returns>An enumerable collection of games with no data.</returns>
		public virtual IEnumerable<Game> GetGamesWithNoData()
		{
			IEnumerable<Game> gamesWithNoData = Database.Items.Where(x => !x.Value.HasData).Select(x => API.Instance.Database.Games.Get(x.Key)).Where(x => x != null);
			IEnumerable<Game> gamesNotInDb = API.Instance.Database.Games.Where(x => !Database.Items.Any(y => y.Key == x.Id));
			IEnumerable<Game> mergedList = gamesWithNoData.Union(gamesNotInDb).Distinct();
			return mergedList.Where(x => !x.Hidden);
		}

		/// <summary>
		/// Retrieves a list of games with old data that hasn't been refreshed in the specified number of months.
		/// </summary>
		/// <param name="months">The number of months to consider data as old.</param>
		/// <returns>An enumerable collection of games with old data.</returns>
		public virtual IEnumerable<Game> GetGamesOldData(int months)
		{
			IEnumerable<Game> gamesOldData = Database.Items.Where(x => x.Value.DateLastRefresh <= DateTime.Now.AddMonths(-months)).Select(x => API.Instance.Database.Games.Get(x.Key)).Where(x => x != null);
			return gamesOldData;
		}

		/// <summary>
		/// Retrieves a list of data games with their associated information.
		/// </summary>
		/// <returns>An enumerable collection of data games.</returns>
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

		/// <summary>
		/// Retrieves a list of isolated data games that are marked as deleted.
		/// </summary>
		/// <returns>An enumerable collection of isolated data games.</returns>
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

		#region Database Item Methods

		/// <summary>
		/// Gets a default item for a game.
		/// </summary>
		/// <param name="id">Game ID.</param>
		/// <returns>Default item.</returns>
		public virtual TItem GetDefault(Guid id)
		{
			Game game = API.Instance.Database.Games.Get(id);
			return game == null ? null : GetDefault(game);
		}

		/// <summary>
		/// Gets a default item for a game.
		/// </summary>
		/// <param name="game">Game.</param>
		/// <returns>Default item.</returns>
		public virtual TItem GetDefault(Game game)
		{
			TItem newItem = typeof(TItem).CrateInstance<TItem>();

			newItem.Id = game.Id;
			newItem.Name = game.Name;
			newItem.IsSaved = false;

			return newItem;
		}

		/// <summary>
		/// Adds an item to the database.
		/// </summary>
		/// <param name="itemToAdd">Item to add.</param>
		public virtual void Add(TItem itemToAdd)
		{
			try
			{
				if (itemToAdd == null)
				{
					Logger.Warn("itemToAdd is null in Add()");
					return;
				}

				itemToAdd.IsSaved = true;
				API.Instance.MainView.UIDispatcher?.Invoke(() => Database?.Add(itemToAdd), DispatcherPriority.Send);

				object Settings = PluginSettings.GetType().GetProperty("Settings").GetValue(PluginSettings);
				PropertyInfo propertyInfo = Settings.GetType().GetProperty("EnableTag");

				if (propertyInfo != null && (bool)propertyInfo.GetValue(Settings))
				{
					RemoveTag(itemToAdd.Id);
					AddTag(itemToAdd.Id);
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, false, PluginName);
				API.Instance.Notifications.Add(new NotificationMessage(
					$"{PluginName}-Error-Add",
					$"{PluginName}" + Environment.NewLine + $"{ex.Message}",
					NotificationType.Error,
					() => PlayniteTools.CreateLogPackage(PluginName)
				));
			}
		}

		/// <summary>
		/// Updates an item in the database.
		/// </summary>
		/// <param name="itemToUpdate">Item to update.</param>
		public virtual void Update(TItem itemToUpdate)
		{
			try
			{
				if (itemToUpdate == null)
				{
					Logger.Warn("itemToAdd is null in Update()");
					return;
				}

				itemToUpdate.IsSaved = true;
				itemToUpdate.DateLastRefresh = DateTime.Now.ToUniversalTime();

				TItem cachedItem = Get(itemToUpdate.Id, true);
				Database?.Items.TryUpdate(itemToUpdate.Id, itemToUpdate, cachedItem);
				API.Instance.MainView.UIDispatcher?.Invoke(() => Database?.Update(itemToUpdate), DispatcherPriority.Send);

				object Settings = PluginSettings.GetType().GetProperty("Settings").GetValue(PluginSettings);
				PropertyInfo propertyInfo = Settings.GetType().GetProperty("EnableTag");

				if (propertyInfo != null && (bool)propertyInfo.GetValue(Settings))
				{
					RemoveTag(itemToUpdate.Id);
					AddTag(itemToUpdate.Id);
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, false, PluginName);
				API.Instance.Notifications.Add(new NotificationMessage(
					$"{PluginName}-Error-Update",
					$"{PluginName}" + Environment.NewLine + $"{ex.Message}",
					NotificationType.Error,
					() => PlayniteTools.CreateLogPackage(PluginName)
				));
			}
		}

		/// <summary>
		/// Adds or updates an item in the database.
		/// </summary>
		/// <param name="item">Item to add or update.</param>
		public virtual void AddOrUpdate(TItem item)
		{
			if (item == null)
			{
				Logger.Warn("item is null in AddOrUpdate()");
				return;
			}

			TItem itemCached = GetOnlyCache(item.Id);
			if (itemCached == null)
			{
				Add(item);
			}
			else
			{
				Update(item);
			}
		}

		/// <summary>
		/// Refreshes data for a game.
		/// </summary>
		/// <param name="game">Game to refresh.</param>
		public void Refresh(Game game) => Refresh(game.Id);

		/// <summary>
		/// Refreshes data for a game ID.
		/// </summary>
		/// <param name="id">Game ID to refresh.</param>
		public void Refresh(Guid id)
		{
			GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
			{
				Cancelable = false,
				IsIndeterminate = true
			};

			API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
			{
				RefreshNoLoader(id, activateGlobalProgress.CancelToken);
			}, globalProgressOptions);
		}

		/// <summary>
		/// Refreshes data for a list of game IDs.
		/// </summary>
		/// <param name="ids">List of game IDs to refresh.</param>
		public void Refresh(IEnumerable<Guid> ids)
		{
			Logger.Info("Refresh() started");
			Refresh(ids, ResourceProvider.GetString("LOCCommonProcessing"));
		}

		/// <summary>
		/// Refreshes data for a list of game IDs with a custom message.
		/// </summary>
		/// <param name="ids">List of game IDs to refresh.</param>
		/// <param name="message">Custom message.</param>
		public virtual void Refresh(IEnumerable<Guid> ids, string message)
		{
			GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"{PluginName} - {message}")
			{
				Cancelable = true,
				IsIndeterminate = ids.Count() == 1
			};

			API.Instance.Dialogs.ActivateGlobalProgress((a) =>
			{
				API.Instance.Database.BeginBufferUpdate();
				Database.BeginBufferUpdate();

				Stopwatch stopWatch = new Stopwatch();
				stopWatch.Start();

				a.ProgressMaxValue = ids.Count();

				foreach (Guid id in ids)
				{
					Game game = API.Instance.Database.Games.Get(id);
					a.Text = $"{PluginName} - {message}"
						+ (ids.Count() == 1 ? string.Empty : "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}")
						+ "\n" + game?.Name + (game?.Source == null ? string.Empty : $" ({game?.Source.Name})");

					if (a.CancelToken.IsCancellationRequested)
					{
						break;
					}

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
				Logger.Info($"Task Refresh(){(a.CancelToken.IsCancellationRequested ? " canceled" : string.Empty)} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{ids.Count()} items");

				Database.EndBufferUpdate();
				API.Instance.Database.EndBufferUpdate();
			}, globalProgressOptions);
		}

		public virtual void RefreshAll()
		{
			var ids = Database.Where(x => x.HasData).Select(x => x.Id);
			Refresh(ids);
		}

		/// <summary>
		/// Refreshes data for a game ID without a loader.
		/// </summary>
		/// <param name="id">Game ID to refresh.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		public virtual void RefreshNoLoader(Guid id, CancellationToken cancellationToken = default(CancellationToken))
		{
			Game game = API.Instance.Database.Games.Get(id);
			Logger.Info($"RefreshNoLoader({game?.Name} - {game?.Id})");

			TItem loadedItem = Get(id, true);
			TItem webItem = GetWeb(id);

			if (webItem != null && !ReferenceEquals(loadedItem, webItem))
			{
				Update(webItem);
			}
			else
			{
				webItem = loadedItem;
			}

			ActionAfterRefresh(webItem);
		}

		[Obsolete("Used Refresh(ids)")]
		public virtual void RefreshWithNoData(IEnumerable<Guid> ids) => Refresh(ids);

		/// <summary>
		/// Refreshes data for installed games.
		/// </summary>
		public virtual void RefreshInstalled()
		{
			Logger.Info("RefreshInstalled() started");
			IEnumerable<Guid> ids = API.Instance.Database.Games.Where(x => x.IsInstalled && !x.Hidden).Select(x => x.Id);
			Logger.Info($"RefreshInstalled found {ids.Count()} game(s) that need updating");
			Refresh(ids, ResourceProvider.GetString("LOCCommonGettingInstalledDatas"));
		}

		/// <summary>
		/// Refreshes data for a list of installed game IDs.
		/// </summary>
		/// <param name="ids">List of game IDs to refresh.</param>
		public virtual void RefreshInstalled(IEnumerable<Guid> ids)
		{
			Logger.Info("RefreshInstalled() started");
			Refresh(ids, ResourceProvider.GetString("LOCCommonGettingInstalledDatas"));
		}

		/// <summary>
		/// Refreshes data for recently added games.
		/// </summary>
		public virtual void RefreshRecent()
		{
			Logger.Info("RefreshRecent() started");
			object Settings = PluginSettings.GetType().GetProperty("Settings").GetValue(PluginSettings);
			PropertyInfo propertyInfo = Settings.GetType().GetProperty("LastAutoLibUpdateAssetsDownload");
			DateTime LastAutoLibUpdateAssetsDownload;
			if (propertyInfo == null)
			{
				Logger.Warn($"No LastAutoLibUpdateAssetsDownload find");
				LastAutoLibUpdateAssetsDownload = DateTime.Now.AddMonths(-1);
			}
			else
			{
				LastAutoLibUpdateAssetsDownload = (DateTime)propertyInfo.GetValue(Settings);
			}

			IEnumerable<Guid> ids = API.Instance.Database.Games
				.Where(x => x.Added != null && x.Added > LastAutoLibUpdateAssetsDownload)
				.Select(x => x.Id);
			Logger.Info($"RefreshRecent found {ids.Count()} game(s) that need updating");
			Refresh(ids, ResourceProvider.GetString("LOCCommonGettingNewDatas"));
		}

		/// <summary>
		/// Action to perform after refreshing an item.
		/// </summary>
		/// <param name="item">Item that was refreshed.</param>
		public virtual void ActionAfterRefresh(TItem item)
		{
		}

		/// <summary>
		/// Merges data from one game to another.
		/// </summary>
		/// <param name="fromId">Source game ID.</param>
		/// <param name="toId">Destination game ID.</param>
		/// <returns>Merged item.</returns>
		public virtual PluginDataBaseGameBase MergeData(Guid fromId, Guid toId) => null;

		/// <summary>
		/// Removes a game from the database.
		/// </summary>
		/// <param name="game">Game to remove.</param>
		/// <returns>True if the game was removed successfully.</returns>
		public virtual bool Remove(Game game) => Remove(game.Id);

		/// <summary>
		/// Removes a game from the database by ID.
		/// </summary>
		/// <param name="id">Game ID to remove.</param>
		/// <returns>True if the game was removed successfully.</returns>
		public virtual bool Remove(Guid id)
		{
			RemoveTag(id);
			bool result = false;
			if (Database.Items.ContainsKey(id))
			{
				result = (bool)API.Instance.MainView.UIDispatcher?.Invoke(() => { return Database.Remove(id); });
			}
			return result;
		}

		/// <summary>
		/// Removes a list of games from the database by IDs.
		/// </summary>
		/// <param name="ids">List of game IDs to remove.</param>
		/// <returns>True if the games were removed successfully.</returns>
		public virtual bool Remove(IEnumerable<Guid> ids)
		{
			Logger.Info($"Remove() started");
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

		/// <summary>
		/// Gets an item from the cache by ID.
		/// </summary>
		/// <param name="id">Item ID.</param>
		/// <returns>Item from the cache.</returns>
		public virtual TItem GetOnlyCache(Guid id) => Database?.Get(id);

		/// <summary>
		/// Gets an item from the cache by game.
		/// </summary>
		/// <param name="game">Game.</param>
		/// <returns>Item from the cache.</returns>
		public virtual TItem GetOnlyCache(Game game) => Database?.Get(game.Id);

		/// <summary>
		/// Gets a clone of an item by ID.
		/// </summary>
		/// <param name="id">Item ID.</param>
		/// <returns>Cloned item.</returns>
		public virtual TItem GetClone(Guid id) => Serialization.GetClone(Get(id, true, false));

		/// <summary>
		/// Gets a clone of an item by game.
		/// </summary>
		/// <param name="game">Game.</param>
		/// <returns>Cloned item.</returns>
		public virtual TItem GetClone(Game game) => Serialization.GetClone(Get(game, true, false));

		/// <summary>
		/// Gets an item by ID.
		/// </summary>
		/// <param name="id">Item ID.</param>
		/// <param name="onlyCache">Whether to use only cache.</param>
		/// <param name="force">Whether to force refresh.</param>
		/// <returns>Item.</returns>
		public abstract TItem Get(Guid id, bool onlyCache = false, bool force = false);

		/// <summary>
		/// Gets an item by game.
		/// </summary>
		/// <param name="game">Game.</param>
		/// <param name="onlyCache">Whether to use only cache.</param>
		/// <param name="force">Whether to force refresh.</param>
		/// <returns>Item.</returns>
		public virtual TItem Get(Game game, bool onlyCache = false, bool force = false) => Get(game.Id, onlyCache, force);

		/// <summary>
		/// Gets an item by game.
		/// </summary>
		/// <param name="game">Game.</param>
		/// <param name="onlyCache">Whether to use only cache.</param>
		/// <param name="force">Whether to force refresh.</param>
		/// <returns>Item.</returns>
		PluginDataBaseGameBase IPluginDatabase.Get(Game game, bool onlyCache, bool force) => Get(game, onlyCache, force);

		/// <summary>
		/// Gets an item by ID.
		/// </summary>
		/// <param name="id">Item ID.</param>
		/// <param name="onlyCache">Whether to use only cache.</param>
		/// <param name="force">Whether to force refresh.</param>
		/// <returns>Item.</returns>
		PluginDataBaseGameBase IPluginDatabase.Get(Guid id, bool onlyCache, bool force) => Get(id, onlyCache, force);

		/// <summary>
		/// Gets a clone of an item by game.
		/// </summary>
		/// <param name="game">Game.</param>
		/// <returns>Cloned item.</returns>
		PluginDataBaseGameBase IPluginDatabase.GetClone(Game game) => GetClone(game);

		/// <summary>
		/// Gets a clone of an item by ID.
		/// </summary>
		/// <param name="id">Item ID.</param>
		/// <returns>Cloned item.</returns>
		PluginDataBaseGameBase IPluginDatabase.GetClone(Guid id) => GetClone(id);

		/// <summary>
		/// Adds or updates an item in the database.
		/// </summary>
		/// <param name="item">Item to add or update.</param>
		void IPluginDatabase.AddOrUpdate(PluginDataBaseGameBase item) => AddOrUpdate((TItem)item);

		/// <summary>
		/// Gets an item from the web by ID.
		/// </summary>
		/// <param name="id">Item ID.</param>
		/// <returns>Item from the web.</returns>
		public virtual TItem GetWeb(Guid id) => null;

		/// <summary>
		/// Gets an item from the web by game.
		/// </summary>
		/// <param name="game">Game.</param>
		/// <returns>Item from the web.</returns>
		public virtual TItem GetWeb(Game game) => GetWeb(game.Id);

		#endregion

		#region Tag System

		/// <summary>
		/// Gets plugin tags.
		/// </summary>
		/// <returns>List of plugin tags.</returns>
		private IEnumerable<Tag> GetPluginTags()
		{
			IEnumerable<Tag> PluginTags = new List<Tag>();
			if (!TagBefore.IsNullOrEmpty())
			{
				PluginTags = API.Instance.Database.Tags?.Where(x => (bool)x.Name?.StartsWith(TagBefore))?.ToList() ?? new List<Tag>();
			}
			return PluginTags;
		}

		/// <summary>
		/// Checks if a tag exists and creates it if it doesn't.
		/// </summary>
		/// <param name="tagName">Tag name.</param>
		/// <returns>Tag ID.</returns>
		internal Guid? CheckTagExist(string tagName)
		{
			string completTagName = TagBefore.IsNullOrEmpty() ? tagName : TagBefore + " " + tagName;
			Guid? findGoodPluginTags = PluginTags.FirstOrDefault(x => x.Name == completTagName)?.Id;
			if (findGoodPluginTags == null)
			{
				API.Instance.Database.Tags.Add(new Tag { Name = completTagName });
				findGoodPluginTags = PluginTags.FirstOrDefault(x => x.Name == completTagName).Id;
			}
			return findGoodPluginTags;
		}

		/// <summary>
		/// Adds a "No Data" tag.
		/// </summary>
		/// <returns>Tag ID.</returns>
		public Guid? AddNoDataTag()
		{
			return CheckTagExist($"{ResourceProvider.GetString("LOCNoData")}");
		}

		/// <summary>
		/// Adds a tag to a game.
		/// </summary>
		/// <param name="game">Game.</param>
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
						if (game.TagIds != null)
						{
							game.TagIds.Add((Guid)tagId);
						}
						else
						{
							game.TagIds = new List<Guid> { (Guid)tagId };
						}
					}
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, $"Tag insert error with {game.Name}", true, PluginName, string.Format(ResourceProvider.GetString("LOCCommonNotificationTagError"), game.Name));
					return;
				}
			}
			else if (TagMissing)
			{
				if (game.TagIds != null)
				{
					game.TagIds.Add((Guid)AddNoDataTag());
				}
				else
				{
					game.TagIds = new List<Guid> { (Guid)AddNoDataTag() };
				}
			}

			API.Instance.MainView.UIDispatcher?.Invoke(() =>
			{
				API.Instance.Database.Games.Update(game);
				game.OnPropertyChanged();
			});
		}

		/// <summary>
		/// Adds a tag to a game by ID.
		/// </summary>
		/// <param name="id">Game ID.</param>
		public void AddTag(Guid id)
		{
			Game game = API.Instance.Database.Games.Get(id);
			if (game != null)
			{
				AddTag(game);
			}
		}

		/// <summary>
		/// Adds a tag to a list of games by IDs.
		/// </summary>
		/// <param name="ids">List of game IDs.</param>
		/// <param name="message">Custom message.</param>
		public void AddTag(IEnumerable<Guid> ids, string message)
		{
			GlobalProgressOptions options = new GlobalProgressOptions(message)
			{
				Cancelable = true,
				IsIndeterminate = ids.Count() == 1
			};

			API.Instance.Dialogs.ActivateGlobalProgress((a) =>
			{
				try
				{
					API.Instance.Database.BeginBufferUpdate();
					API.Instance.Database.Games.BeginBufferUpdate();

					Stopwatch stopWatch = new Stopwatch();
					stopWatch.Start();

					a.ProgressMaxValue = ids.Count();

					foreach (Guid id in ids)
					{
						Game game = API.Instance.Database.Games.Get(id);
						if (game == null)
						{
							continue;
						}

						a.Text = message
							+ (ids.Count() == 1 ? string.Empty : "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}")
							+ "\n" + game?.Name + (game?.Source == null ? string.Empty : $" ({game?.Source.Name})");

						if (a.CancelToken.IsCancellationRequested)
						{
							break;
						}

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
					Logger.Info($"AddTag(){(a.CancelToken.IsCancellationRequested ? " canceled" : string.Empty)} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{(double)ids.Count()} items");
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
		/// Adds a tag to all games.
		/// </summary>
		public void AddTagAllGame()
		{
			Logger.Info($"AddTagAllGame() started");
			IEnumerable<Guid> ids = API.Instance.Database.Games.Where(x => x.Hidden == false).Select(x => x.Id);
			AddTag(ids, $"{PluginName} - {ResourceProvider.GetString("LOCCommonAddingAllTag")}");
		}

		/// <summary>
		/// Adds a tag to selected games.
		/// </summary>
		public void AddTagSelectData()
		{
			Logger.Info($"AddTagSelectData() started");

			OptionsDownloadData view = new OptionsDownloadData(this, true);
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginName + " - " + ResourceProvider.GetString("LOCCommonSelectGames"), view);
			windowExtension.ShowDialog();

			List<Game> playniteDb = view.GetFilteredGames();
			TagMissing = view.GetTagMissing();

			if (playniteDb == null)
			{
				TagMissing = false;
				return;
			}

			IEnumerable<Guid> ids = playniteDb.Select(x => x.Id);
			AddTag(ids, $"{PluginName} - {ResourceProvider.GetString("LOCCommonAddingAllTag")}");
			TagMissing = false;
		}

		/// <summary>
		/// Removes a tag from a game.
		/// </summary>
		/// <param name="game">Game.</param>
		public void RemoveTag(Game game)
		{
			if (game?.TagIds != null)
			{
				game.TagIds = game.TagIds.Where(x => !PluginTags.Any(y => x == y.Id)).ToList();
				API.Instance.MainView.UIDispatcher?.Invoke(() =>
				{
					API.Instance.Database.Games.Update(game);
					game.OnPropertyChanged();
				});
			}
		}

		/// <summary>
		/// Removes a tag from a game by ID.
		/// </summary>
		/// <param name="id">Game ID.</param>
		public void RemoveTag(Guid id)
		{
			Game game = API.Instance.Database.Games.Get(id);
			if (game != null)
			{
				RemoveTag(game);
			}
		}

		/// <summary>
		/// Removes a tag from all games.
		/// </summary>
		/// <param name="fromClearDatabase">Whether the call is from clearing the database.</param>
		public void RemoveTagAllGame(bool fromClearDatabase = false)
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
					Logger.Info($"RemoveTagAllGame() started");
					API.Instance.Database.BeginBufferUpdate();
					API.Instance.Database.Games.BeginBufferUpdate();

					Stopwatch stopWatch = new Stopwatch();
					stopWatch.Start();

					IEnumerable<Game> playniteDb = API.Instance.Database.Games.Where(x => x.Hidden == false);
					a.ProgressMaxValue = playniteDb.Count();

					foreach (Game game in playniteDb)
					{
						a.Text = message
							+ "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
							+ "\n" + game?.Name + (game?.Source == null ? string.Empty : $" ({game?.Source.Name})");

						if (a.CancelToken.IsCancellationRequested)
						{
							break;
						}

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
					Logger.Info($"RemoveTagAllGame(){(a.CancelToken.IsCancellationRequested ? " canceled" : string.Empty)} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{(double)playniteDb.Count()} items");
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
		/// Finds a good plugin tag.
		/// </summary>
		/// <param name="tagName">Tag name.</param>
		/// <returns>Tag ID.</returns>
		internal virtual Guid? FindGoodPluginTags(string tagName)
		{
			return CheckTagExist(tagName);
		}

		#endregion

		#region Utility Methods

		/// <summary>
		/// Clears the cache.
		/// </summary>
		public void ClearCache()
		{
			string PathDirectory = Path.Combine(PlaynitePaths.DataCachePath, PluginName);

			GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
			{
				Cancelable = false,
				IsIndeterminate = true
			};

			API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
			{
				try
				{
					Thread.Sleep(2000);
					FileSystem.DeleteDirectory(PathDirectory, true);
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, false, PluginName);
					API.Instance.Dialogs.ShowErrorMessage(
						string.Format(ResourceProvider.GetString("LOCCommonErrorDeleteCache"), PathDirectory),
						PluginName
					);
				}
			}, globalProgressOptions);
		}

		/// <summary>
		/// Handles game item updates.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event arguments.</param>
		public virtual void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
		{
			try
			{
				if (e?.UpdatedItems?.Count > 0 && Database != null)
				{
					e?.UpdatedItems?.ForEach(x =>
					{
						if (x.NewData?.Id != null)
						{
							Database.SetGameInfo<T>(x.NewData.Id);
							ActionAfterGames_ItemUpdated(x.OldData, x.NewData);
						}
					});

					object settings = PluginSettings.GetType().GetProperty("Settings").GetValue(PluginSettings);
					PropertyInfo propertyInfo = settings.GetType().GetProperty("AutoImportOnInstalled");
					if (propertyInfo != null && (bool)propertyInfo.GetValue(settings))
					{
						List<Guid> ids = e.UpdatedItems.Where(x => !x.OldData.IsInstalled & x.NewData.IsInstalled && !PreviousIds.Contains(x.NewData.Id)).Select(x => x.NewData.Id).ToList();
						PreviousIds = ids;
						if (ids?.Count() > 0)
						{
							RefreshInstalled(ids);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, false, PluginName);
			}
		}

		/// <summary>
		/// Action to perform after game item updates.
		/// </summary>
		/// <param name="gameOld">Old game data.</param>
		/// <param name="gameNew">New game data.</param>
		public virtual void ActionAfterGames_ItemUpdated(Game gameOld, Game gameNew)
		{
		}

		/// <summary>
		/// Handles game item collection changes.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event arguments.</param>
		private void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
		{
			try
			{
				e?.RemovedItems?.ForEach(x =>
				{
					Remove(x);
				});
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, false, PluginName);
			}
		}

		/// <summary>
		/// Sets theme resources for a game.
		/// </summary>
		/// <param name="game">Game.</param>
		public virtual void SetThemesResources(Game game)
		{
		}

		#endregion

		#region Data Extraction

		/// <summary>
		/// Extracts data to a CSV file.
		/// </summary>
		/// <param name="path">Path to save the CSV file.</param>
		/// <param name="minimum">Whether to extract minimum data.</param>
		/// <returns>True if the extraction was successful.</returns>
		public bool ExtractToCsv(string path, bool minimum)
		{
			bool isOK = false;
			try
			{
				Logger.Info($"ExtractToCsv({minimum}) started");
				GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonExtracting")}")
				{
					Cancelable = true,
					IsIndeterminate = false
				};

				API.Instance.Dialogs.ActivateGlobalProgress((a) =>
				{
					Stopwatch stopWatch = new Stopwatch();
					stopWatch.Start();

					try
					{
						ulong totalItems = 0;
						foreach (TItem item in Database.Items?.Values)
						{
							totalItems += item.Count;
						}
						a.ProgressMaxValue = totalItems;

						string filePath = Path.Combine(path, $"{PluginName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");
						filePath = CommonPlayniteShared.Common.Paths.FixPathLength(filePath);
						FileSystem.PrepareSaveFile(filePath);
						string csvData = GetCsvData(a, minimum);
						if (!csvData.IsNullOrEmpty())
						{
							if (!a.CancelToken.IsCancellationRequested)
							{
								File.WriteAllText(filePath, csvData, Encoding.UTF8);
								Process.Start("explorer.exe", path);
								isOK = true;
							}
						}
						else
						{
							Logger.Warn($"No csv data for {PluginName}");
						}
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, true, PluginName);
					}

					stopWatch.Stop();
					TimeSpan ts = stopWatch.Elapsed;
					Logger.Info($"ExtractToCsv({minimum}){(a.CancelToken.IsCancellationRequested ? " canceled" : string.Empty)} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {Database.Items?.Count()} items");
				}, options);
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
			}

			return isOK;
		}

		/// <summary>
		/// Gets CSV data.
		/// </summary>
		/// <param name="a">Progress action arguments.</param>
		/// <param name="minimum">Whether to get minimum data.</param>
		/// <returns>CSV data.</returns>
		internal virtual string GetCsvData(GlobalProgressActionArgs a, bool minimum)
		{
			return null;
		}

		#endregion
	}
}