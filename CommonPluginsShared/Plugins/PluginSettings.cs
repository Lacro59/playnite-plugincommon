using CommonPluginsShared.Interfaces;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsShared.Plugins
{
    public class PluginSettings : ObservableObject, IPluginSettings
    {
        private bool _menuInExtensions = true;
        public bool MenuInExtensions
        {
            get => _menuInExtensions;
            set => SetValue(ref _menuInExtensions, value);
        }

        private bool _enableTag = false;
        public bool EnableTag
        {
            get => _enableTag;
            set => SetValue(ref _enableTag, value);
        }

        #region Automatic update when updating library

        public DateTime LastAutoLibUpdateAssetsDownload { get; set; } = DateTime.Now;

        private bool _autoImport = false;
        public bool AutoImport
        {
            get => _autoImport;
            set => SetValue(ref _autoImport, value);
        }

        #endregion

        #region Automatic update when game is installed

        public bool AutoImportOnInstalled { get; set; } = false;

        #endregion

        #region Variables exposed for custom themes

        private bool _hasData = false;
        [DontSerialize]
        public bool HasData { get => _hasData; set => SetValue(ref _hasData, value); }

        #endregion

        [DontSerialize]
        public PluginState PluginState => new PluginState();
    }

    public class PluginState
    {
        public bool SteamIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.SteamLibrary));
        public bool EpicIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.EpicLibrary)) || PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.LegendaryLibrary));
        public bool GogIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.GogLibrary)) || PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.GogOssLibrary));
        public bool OriginIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.OriginLibrary));
        public bool XboxIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.XboxLibrary));
        public bool PsnIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.PSNLibrary));
        public bool NintendosEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.NintendoLibrary));
        public bool BattleNetIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.BattleNetLibrary));
        public bool GameJoltIsEnabled => PlayniteTools.IsEnabledPlaynitePlugin(PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.GameJoltLibrary));
    }

    public class PluginUpdate
    {
        public bool OnStart { get; set; } = false;
        public bool EveryHours { get; set; } = false;
        public uint Hours { get; set; } = 3;
    }
}