using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPluginsShared.Models;
using CommonPluginsPlaynite.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Automation;
using CommonPluginsControls.Controls;
using CommonPluginsPlaynite.Common;
using CommonPluginsShared.Interfaces;

namespace CommonPluginsShared.Collections
{
    public abstract class PluginDatabaseObject<TSettings, TDatabase, TItem> : ObservableObject, IPluginDatabase
        where TSettings : ISettings
        where TDatabase : PluginItemCollection<TItem>
        where TItem : PluginDataBaseGameBase
    {
        protected static readonly ILogger logger = LogManager.GetLogger();
        protected static IResourceProvider resources = new ResourceProvider();

        public IPlayniteAPI PlayniteApi;
        public TSettings PluginSettings;

        public IntegrationUI ui = new IntegrationUI();

        public string PluginName;
        protected PluginPaths Paths;

        private TDatabase _Database;
        public TDatabase Database
        {
            get
            {
                return _Database;
            }
            set
            {
                _Database = value;
            }
        }

        public Game GameContext;

        public List<Tag> PluginTags { get; set; } = new List<Tag>();



        private bool _hasErrorCritical = false;
        public bool HasErrorCritical
        {
            get
            {
                return _hasErrorCritical;
            }

            set
            {
                _hasErrorCritical = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoaded = false;
        public bool IsLoaded
        {
            get
            {
                return _isLoaded;
            }

            set
            {
                _isLoaded = value;
                OnPropertyChanged();
            }
        }

        public bool IsViewOpen = false;


        protected PluginDatabaseObject(IPlayniteAPI PlayniteApi, TSettings PluginSettings, string PluginName, string PluginUserDataPath)
        {
            this.PlayniteApi = PlayniteApi;
            this.PluginSettings = PluginSettings;

            this.PluginName = PluginName;

            Paths = new PluginPaths
            {
                PluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                PluginUserDataPath = PluginUserDataPath,
                PluginDatabasePath = Path.Combine(PluginUserDataPath, PluginName),
                PluginCachePath = Path.Combine(PluginUserDataPath, "Cache"),
            };

            FileSystem.CreateDirectory(Paths.PluginDatabasePath);
            FileSystem.CreateDirectory(Paths.PluginCachePath);
        }


        #region Database
        public Task<bool> InitializeDatabase()
        {
            return Task.Run(() =>
            {
                if (IsLoaded)
                {
                    logger.Info($"{PluginName} - Database is already initialized");
                    return true;
                }

                IsLoaded = LoadDatabase();
                return IsLoaded;
            });
        }

        protected abstract bool LoadDatabase();

        public virtual bool ClearDatabase()
        {
            if (Directory.Exists(Paths.PluginDatabasePath))
            {
                try
                {
                    Directory.Delete(Paths.PluginDatabasePath, true);
                    Directory.CreateDirectory(Paths.PluginDatabasePath);

                    IsLoaded = false;
                    logger.Info($"{PluginName} - Database is cleared");

                    // If tag system
                    PropertyInfo propertyInfo = PluginSettings.GetType().GetProperty("EnableTag");
                    if (propertyInfo != null)
                    {
                        bool EnableTag = (bool)propertyInfo.GetValue(PluginSettings);
                        if (EnableTag)
                        {
#if DEBUG
                            logger.Debug($"{PluginName} [Ignored] - RemoveTagAllGame()");
#endif
                            RemoveTagAllGame();
                        }
                    }

                    return LoadDatabase();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Common.LogError(ex, PluginName + " [Ignored]");
#endif
                }
            }

            return false;
        }


        public virtual void GetSelectDatas()
        {
            var View = new OptionsDownloadData(PlayniteApi);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, PluginName + " - " + resources.GetString("LOCCommonSelectData"), View);
            windowExtension.ShowDialog();

            var PlayniteDb = View.GetFilteredGames();

            if (PlayniteDb == null)
            {
                return;
            }

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonGettingData")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    
                    activateGlobalProgress.ProgressMaxValue = (double)PlayniteDb.Count();

                    string CancelText = string.Empty;

                    foreach (Game game in PlayniteDb)
                    {
                        if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                        {
                            CancelText = " canceled";
                            break;
                        }

                        Thread.Sleep(10);
                        Get(game);
                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"{PluginName} - Task GetDatas(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, PluginName);
                }
            }, globalProgressOptions);
        }

        public virtual void GetAllDatas()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonGettingAllDatas")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var PlayniteDb = PlayniteApi.Database.Games.Where(x => x.Hidden == false);
                    activateGlobalProgress.ProgressMaxValue = (double)PlayniteDb.Count();

                    string CancelText = string.Empty;

                    foreach (Game game in PlayniteDb)
                    {
                        if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                        {
                            CancelText = " canceled";
                            break;
                        }

                        Thread.Sleep(10);
                        Get(game);
                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"{PluginName} - Task GetAllDatas(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, PluginName);
                }
            }, globalProgressOptions);
        }
        #endregion


        #region Database item methods
        public virtual TItem GetDefault(Guid Id)
        {
            Game game = PlayniteApi.Database.Games.Get(Id);

            if (game == null)
            {
                return null;
            }

            return GetDefault(game);
        }

        public virtual TItem GetDefault(Game game)
        {
            var newItem = typeof(TItem).CrateInstance<TItem>();

            newItem.Id = game.Id;
            newItem.Name = game.Name;
            newItem.SourceId = game.SourceId;
            newItem.Hidden = game.Hidden;
            newItem.Icon = game.Icon;
            newItem.CoverImage = game.CoverImage;
            newItem.GenreIds = game.GenreIds;
            newItem.Genres = game.Genres;
            newItem.Playtime = game.Playtime;
            newItem.LastActivity = game.LastActivity;

            return newItem;
        }


        public virtual void Add(TItem itemToAdd)
        {
            itemToAdd.IsSaved = true;
            Database.Add(itemToAdd);

            // If tag system
            PropertyInfo propertyInfo = PluginSettings.GetType().GetProperty("EnableTag");
            if (propertyInfo != null)
            {
                bool EnableTag = (bool)propertyInfo.GetValue(PluginSettings);
                if (EnableTag)
                {
#if DEBUG
                    logger.Debug($"{PluginName} [Ignored] - RemoveTag & AddTag for {itemToAdd.Name} with {itemToAdd.Id.ToString()}");
#endif
                    RemoveTag(itemToAdd.Id);
                    AddTag(itemToAdd.Id);
                }
            }
        }

        public virtual void Update(TItem itemToUpdate)
        {
            itemToUpdate.IsSaved = true;
            Database.Items.TryUpdate(itemToUpdate.Id, itemToUpdate, Get(itemToUpdate.Id, true));
            Database.Update(itemToUpdate);            
        }

        public virtual void Refresh(Guid Id)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonProcessing")}",
                false
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                var loadedItem = Get(Id, true);
                var webItem = GetWeb(Id);

                if (!ReferenceEquals(loadedItem, webItem))
                {
                    Update(webItem);
                }
            }, globalProgressOptions);
        }

        public virtual bool Remove(Guid Id)
        {
            // If tag system
            PropertyInfo propertyInfo = PluginSettings.GetType().GetProperty("EnableTag");
            if (propertyInfo != null)
            {
                bool EnableTag = (bool)propertyInfo.GetValue(PluginSettings);
                if (EnableTag)
                {
#if DEBUG
                    logger.Debug($"{PluginName} [Ignored] - RemoveTag for {Id.ToString()}");
#endif
                    RemoveTag(Id);
                }
            }

            return Database.Remove(Id);
        }

        public virtual bool Remove(Game game)
        {
            return Database.Remove(game.Id);
        }


        public virtual TItem GetOnlyCache(Guid Id)
        {
            return Database.Get(Id);
        }

        public virtual TItem GetOnlyCache(Game game)
        {
            return Database.Get(game.Id);
        }


        public abstract TItem Get(Guid Id, bool OnlyCache = false);

        public virtual TItem Get(Game game, bool OnlyCache = false)
        {
            return Get(game.Id, OnlyCache);
        }

        public abstract TItem GetWeb(Guid Id);

        public virtual TItem GetWeb(Game game)
        {
            return GetWeb(game.Id);
        }
        #endregion


        public abstract void SetThemesResources(Game game);


        #region Tag system
        protected virtual void GetPluginTags()
        {

        }

        public virtual void AddTag(Game game)
        {

        }

        public void AddTag(Guid Id)
        {
            Game game = PlayniteApi.Database.Games.Get(Id);
            if (game != null)
            {
                AddTag(game);
            }
        }

        public void RemoveTag(Game game)
        {
            if (game != null && game.TagIds != null)
            {
                if (game.TagIds.Where(x => PluginTags.Any(y => x == y.Id)).Count() > 0)
                {
                    game.TagIds = game.TagIds.Where(x => !PluginTags.Any(y => x == y.Id)).ToList();
#if DEBUG
                    logger.Debug($"{PluginName} [Ignored] - PluginTags: {JsonConvert.SerializeObject(PluginTags)}");
                    logger.Debug($"{PluginName} [Ignored] - game.TagIds: {JsonConvert.SerializeObject(game.TagIds)}");
#endif
                    PlayniteApi.Database.Games.Update(game);
                }
            }
        }

        public void RemoveTag(Guid Id)
        {
            Game game = PlayniteApi.Database.Games.Get(Id);
            if (game != null)
            {
                RemoveTag(game);
            }
        }

        public void AddTagAllGame()
        {
#if DEBUG
            logger.Debug($"{PluginName} [Ignored] - AddTagAllGame");
#endif

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonAddingAllTag")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var PlayniteDb = PlayniteApi.Database.Games.Where(x => x.Hidden == false);
                    activateGlobalProgress.ProgressMaxValue = (double)PlayniteDb.Count();

                    string CancelText = string.Empty;

                    foreach (Game game in PlayniteDb)
                    {
                        if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                        {
                            CancelText = " canceled";
                            break;
                        }

                        Thread.Sleep(10);
                        RemoveTag(game);
                        AddTag(game);

                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"{PluginName} - AddTagAllGame(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, PluginName);
                }
            }, globalProgressOptions);
        }

        public void RemoveTagAllGame(bool FromClearDatabase = false)
        {
#if DEBUG
            logger.Debug($"{PluginName} [Ignored] - RemoveTagAllGame");
#endif

            string Message = string.Empty;
            if (FromClearDatabase)
            {
                Message = $"{PluginName} - {resources.GetString("LOCCommonClearingAllTag")}";
            }
            else
            {
                Message = $"{PluginName} - {resources.GetString("LOCCommonRemovingAllTag")}";
            }

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(Message, true);
            globalProgressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var PlayniteDb = PlayniteApi.Database.Games.Where(x => x.Hidden == false);
                    activateGlobalProgress.ProgressMaxValue = (double)PlayniteDb.Count();

                    string CancelText = string.Empty;

                    foreach (Game game in PlayniteDb)
                    {
                        if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                        {
                            CancelText = " canceled";
                            break;
                        }

                        RemoveTag(game);
                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"{PluginName} - RemoveTagAllGame(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, PluginName);
                }
            }, globalProgressOptions);
        }

        public virtual Guid? FindGoodPluginTags(string TagName)
        {
            return PluginTags.Find(x => x.Name.ToLower() == TagName.ToLower()).Id;
        }
        #endregion
    }
}
