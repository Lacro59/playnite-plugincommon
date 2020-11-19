using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon.Models;
using PluginCommon.PlayniteResources.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PluginCommon.Collections
{
    public abstract class PluginDatabaseObject : ObservableObject
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
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        private ISettings _PluginSettings;
        public ISettings PluginSettings
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


        protected PluginDatabaseObject(IPlayniteAPI PlayniteApi, ISettings PluginSettings, string PluginUserDataPath)
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

        public virtual bool ClearDatabase()
        {
            if (Directory.Exists(PluginDatabaseDirectory))
            {
                try
                {
                    Directory.Delete(PluginDatabaseDirectory, true);
                    Directory.CreateDirectory(PluginDatabaseDirectory);

                    IsLoaded = false;
                    return true;
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
    }
}
