using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon.Models;
using PluginCommon.PlayniteResources.Database;
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

namespace PluginCommon.Collections
{
    public abstract class PluginDatabaseObject<TypeSettings, TypeDatabase, TItem> : ObservableObject 
        where TypeSettings : ISettings
        where TypeDatabase : PluginItemCollection<TItem>
        where TItem : PluginDataBaseGameBase
    {
        protected static readonly ILogger logger = LogManager.GetLogger();
        protected static IResourceProvider resources = new ResourceProvider();

        protected readonly IPlayniteAPI _PlayniteApi;

        protected string PluginDatabaseDirectory;

        private string _PluginName;
        public string PluginName
        {
            get
            {
                return _PluginName;
            }

            set
            {
                _PluginName = value;
            }
        }

        private string _PluginUserDataPath;
        public string PluginUserDataPath
        {
            get
            {
                return _PluginUserDataPath;
            }

            set
            {
                _PluginUserDataPath = value;
            }
        }


        private TypeSettings _PluginSettings;
        public TypeSettings PluginSettings
        {
            get
            {
                return _PluginSettings;
            }

            set
            {
                _PluginSettings = value;
                OnPropertyChanged();
            }
        }

        private TypeDatabase _Database;
        public TypeDatabase Database
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

        private TItem _GameSelectedData;
        public TItem GameSelectedData
        {
            get
            {
                return _GameSelectedData;
            }

            set
            {
                _GameSelectedData = value;
                OnPropertyChanged();
            }
        }

        private List<Tag> _PluginTags;
        public List<Tag> PluginTags
        {
            get
            {
                return _PluginTags;
            }

            set
            {
                _PluginTags = value;
            }
        }

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

        private bool _GameIsLoaded = false;
        public bool GameIsLoaded
        {
            get
            {
                return _GameIsLoaded;
            }

            set
            {
                _GameIsLoaded = value;
                OnPropertyChanged();
            }
        }

        public bool IsViewOpen = false;


        protected PluginDatabaseObject(IPlayniteAPI PlayniteApi, TypeSettings PluginSettings, string PluginUserDataPath)
        {
            _PlayniteApi = PlayniteApi;
            this.PluginUserDataPath = PluginUserDataPath;
            this.PluginSettings = PluginSettings;
        }


        protected bool ControlAndCreateDirectory(string PluginUserDataPath, string DirectoryName)
        {
            string PluginDatabasePath = Path.Combine(PluginUserDataPath, DirectoryName);

            try
            {
                if (!Directory.Exists(PluginDatabasePath))
                {
                    Directory.CreateDirectory(PluginDatabasePath);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, PluginName);

                HasErrorCritical = true;
                return false;
            }

            PluginDatabaseDirectory = PluginDatabasePath;
            return true;
        }


        public Task<bool> InitializeDatabase()
        {
            return Task.Run(() =>
            {
                if (IsLoaded)
                {
                    logger.Info($"{PluginName} - Database is already initialized");
                    return true;
                }

                return LoadDatabase();
            });
        }

        protected abstract bool LoadDatabase();

        public virtual void GetAllDatas()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonGettingAllDatas")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            _PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var PlayniteDb = _PlayniteApi.Database.Games.Where(x => x.Hidden == false);
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

        public virtual bool ClearDatabase()
        {
            if (Directory.Exists(PluginDatabaseDirectory))
            {
                try
                {
                    Directory.Delete(PluginDatabaseDirectory, true);
                    Directory.CreateDirectory(PluginDatabaseDirectory);

                    IsLoaded = false;
                    logger.Info($"{PluginName} - Database is cleared");

                    RemoveTagAllGame();

                    return LoadDatabase();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Common.LogError(ex, PluginName);
#endif
                }
            }

            return false;
        }


        public virtual void Add(TItem itemToAdd)
        {
            itemToAdd.IsSaved = true;
            Database.Add(itemToAdd);

            PropertyInfo propertyInfo = PluginSettings.GetType().GetProperty("EnableTag");
            if (propertyInfo != null)
            {
                bool EnableTag = (bool)propertyInfo.GetValue(PluginSettings);
                if (EnableTag)
                {
#if DEBUG
                    logger.Debug($"{PluginName} - RemoveTag & AddTag for {itemToAdd.Name} with {itemToAdd.Id}");
#endif
                    RemoveTag(itemToAdd.Id);
                    AddTag(itemToAdd.Id);
                }
            }
        }

        public virtual void Update(TItem itemToUpdate)
        {
            Database.Update(itemToUpdate);
        }

        public virtual bool Remove(Guid Id)
        {
            RemoveTag(Id);
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


        public virtual void SetCurrent(Guid Id)
        {
            SetCurrent(GetOnlyCache(Id));
        }

        public virtual void SetCurrent(Game game)
        {
            SetCurrent(GetOnlyCache(game.Id));
        }

        public virtual void SetCurrent(TItem gameSelectedData)
        {
#if DEBUG
            logger.Debug($"{PluginName} - SetCurrent() - {gameSelectedData.Name}");
#endif
            GameSelectedData = gameSelectedData;
        }


        public abstract TItem GetDefault(Game game);


        protected virtual void GetPluginTags()
        {

        }

        public virtual void AddTag(Game game)
        {
            
        }

        public void AddTag(Guid Id)
        {
            Game game = _PlayniteApi.Database.Games.Get(Id);
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
                    logger.Debug($"{PluginName} - PluginTags: {JsonConvert.SerializeObject(PluginTags)}");
                    logger.Debug($"{PluginName} - game.TagIds: {JsonConvert.SerializeObject(game.TagIds)}");
#endif
                    _PlayniteApi.Database.Games.Update(game);
                }
            }
        }

        public void RemoveTag(Guid Id)
        {
            Game game = _PlayniteApi.Database.Games.Get(Id);
            if (game != null)
            {
                RemoveTag(game);
            }
        }

        public void AddTagAllGame()
        {
#if DEBUG
            logger.Debug($"{PluginName} - AddTagAllGame");
#endif

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonAddingAllTag")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            _PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var PlayniteDb = _PlayniteApi.Database.Games.Where(x => x.Hidden == false);
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
            logger.Debug($"{PluginName} - RemoveTagAllGame");
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

            _PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var PlayniteDb = _PlayniteApi.Database.Games.Where(x => x.Hidden == false);
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
    }
}
