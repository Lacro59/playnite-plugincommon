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

        public string PluginName { get; set; }
        public PluginPaths Paths { get; set; }
        public TDatabase Database { get; set; }
        public Game GameContext { get; set; }
        public List<Tag> PluginTags { get; set; } = new List<Tag>();


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

            PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
        }


        public abstract void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e);


        #region Database
        public Task<bool> InitializeDatabase()
        {
            return Task.Run(() =>
            {
                if (IsLoaded)
                {
                    logger.Info($"Database is already initialized");
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
                    logger.Info($"Database is cleared");

                    // If tag system
                    PropertyInfo propertyInfo = PluginSettings.GetType().GetProperty("EnableTag");
                    if (propertyInfo != null)
                    {
                        bool EnableTag = (bool)propertyInfo.GetValue(PluginSettings);
                        if (EnableTag)
                        {

                            Common.LogDebug(true, $"RemoveTagAllGame()");

                            RemoveTagAllGame();
                        }
                    }

                    return LoadDatabase();
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, true);
                }
            }

            return false;
        }


        public virtual void GetSelectData()
        {
            var View = new OptionsDownloadData(PlayniteApi);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, PluginName + " - " + resources.GetString("LOCCommonSelectData"), View);
            windowExtension.ShowDialog();

            var PlayniteDb = View.GetFilteredGames();
            bool OnlyMissing = View.GetOnlyMissing();

            if (OnlyMissing)
            {
                PlayniteDb = PlayniteDb.FindAll(x => !Get(x.Id, true).HasData);
            }

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
                        Remove(game);
                        Get(game);

                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"Task GetSelectData(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
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
                    logger.Info($"Task GetAllDatas(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
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
            newItem.BackgroundImage = game.BackgroundImage;
            newItem.GenreIds = game.GenreIds;
            newItem.Genres = game.Genres;
            newItem.Playtime = game.Playtime;
            newItem.LastActivity = game.LastActivity;
            newItem.IsSaved = false;

            return newItem;
        }


        public virtual void Add(TItem itemToAdd)
        {
            itemToAdd.IsSaved = true;
            Database.Add(itemToAdd);

            // If tag system
            var Settings = PluginSettings.GetType().GetProperty("Settings").GetValue(PluginSettings);
            PropertyInfo propertyInfo = Settings.GetType().GetProperty("EnableTag");

            if (propertyInfo != null)
            {
                bool EnableTag = (bool)propertyInfo.GetValue(Settings);
                if (EnableTag)
                {
                    Common.LogDebug(true, $"RemoveTag & AddTag for {itemToAdd.Name} with {itemToAdd.Id.ToString()}");
                    RemoveTag(itemToAdd.Id, true);
                    AddTag(itemToAdd.Id);
                }
            }
        }


        public virtual void Update(TItem itemToUpdate)
        {
            itemToUpdate.IsSaved = true;
            Database.Items.TryUpdate(itemToUpdate.Id, itemToUpdate, Get(itemToUpdate.Id, true));
            Database.Update(itemToUpdate);

            // If tag system
            var Settings = PluginSettings.GetType().GetProperty("Settings").GetValue(PluginSettings);
            PropertyInfo propertyInfo = Settings.GetType().GetProperty("EnableTag");

            if (propertyInfo != null)
            {
                bool EnableTag = (bool)propertyInfo.GetValue(PluginSettings);
                if (EnableTag)
                {
                    Common.LogDebug(true, $"RemoveTag & AddTag for {itemToUpdate.Name} with {itemToUpdate.Id.ToString()}");
                    RemoveTag(itemToUpdate.Id);
                    AddTag(itemToUpdate.Id);
                }
            }
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

                if (webItem != null && !ReferenceEquals(loadedItem, webItem))
                {
                    Update(webItem);
                }
            }, globalProgressOptions);
        }


        public virtual bool Remove(Guid Id)
        {
            // If tag system
            var Settings = PluginSettings.GetType().GetProperty("Settings").GetValue(PluginSettings);
            PropertyInfo propertyInfo = Settings.GetType().GetProperty("EnableTag");

            if (propertyInfo != null)
            {
                Common.LogDebug(true, $"RemoveTag for {Id.ToString()}");
                RemoveTag(Id);
            }

            if (Database.Items.ContainsKey(Id))
            {
                return Database.Remove(Id);
            }
            return false;
        }

        public virtual bool Remove(Game game)
        {
            return Remove(game.Id);
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


        public virtual TItem GetWeb(Guid Id)
        {
            return null;
        }

        public virtual TItem GetWeb(Game game)
        {
            return GetWeb(game.Id);
        }
        #endregion


        #region Tag system
        protected virtual void GetPluginTags()
        {

        }

        public virtual void AddTag(Game game, bool noUpdate = false)
        {

        }

        public void AddTag(Guid Id, bool noUpdate = false)
        {
            Game game = PlayniteApi.Database.Games.Get(Id);
            if (game != null)
            {
                AddTag(game, noUpdate);
            }
        }

        public void RemoveTag(Game game, bool noUpdate = false)
        {
            if (game != null && game.TagIds != null)
            {
                if (game.TagIds.Where(x => PluginTags.Any(y => x == y.Id)).Count() > 0)
                {
                    game.TagIds = game.TagIds.Where(x => !PluginTags.Any(y => x == y.Id)).ToList();
                    if (noUpdate)
                    {
                        PlayniteApi.Database.Games.Update(game);
                    }
                }
            }
        }

        public void RemoveTag(Guid Id, bool noUpdate = false)
        {
            Game game = PlayniteApi.Database.Games.Get(Id);
            if (game != null)
            {
                RemoveTag(game, noUpdate);
            }
        }

        public void AddTagAllGame()
        {
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
                        Application.Current.Dispatcher.BeginInvoke((Action)delegate
                        {
                            RemoveTag(game, true);
                            AddTag(game, true);
                            PlayniteApi.Database.Games.Update(game);
                        });


                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"AddTagAllGame(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }, globalProgressOptions);
        }

        public void AddTagSelectData()
        {
            var View = new OptionsDownloadData(PlayniteApi, true);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, PluginName + " - " + resources.GetString("LOCCommonSelectGames"), View);
            windowExtension.ShowDialog();

            var PlayniteDb = View.GetFilteredGames();
            PlayniteDb = PlayniteDb.FindAll(x => Get(x.Id, true).HasData);

            if (PlayniteDb == null)
            {
                return;
            }

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
                        Application.Current.Dispatcher.BeginInvoke((Action)delegate
                        {
                            RemoveTag(game, true);
                            AddTag(game, true);
                            PlayniteApi.Database.Games.Update(game);
                        });


                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"AddTagSelectData(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }, globalProgressOptions);
        }


        public void RemoveTagAllGame(bool FromClearDatabase = false)
        {
            Common.LogDebug(true, $"RemoveTagAllGame");

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
                    logger.Info($"RemoveTagAllGame(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }, globalProgressOptions);
        }

        public virtual Guid? FindGoodPluginTags(string TagName)
        {
            return PluginTags.Find(x => x.Name.ToLower() == TagName.ToLower()).Id;
        }
        #endregion


        public virtual void SetThemesResources(Game game)
        {
        }
    }
}
