using CommonPluginsShared;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsControls.Settings
{
    /// <summary>
    /// Shared plugin settings section for diagnostic logging: verbose toggle and log package export.
    /// Expects a parent <see cref="DataContext"/> exposing <see cref="IPluginSettingsViewModel.Settings"/>.
    /// </summary>
    public partial class PluginDebugSettingsSection : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="PluginName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PluginNameProperty =
            DependencyProperty.Register(
                nameof(PluginName),
                typeof(string),
                typeof(PluginDebugSettingsSection),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Plugin name passed to <see cref="PlayniteTools.CreateLogPackage"/> for the ZIP file prefix.
        /// </summary>
        public string PluginName
        {
            get { return (string)GetValue(PluginNameProperty); }
            set { SetValue(PluginNameProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginDebugSettingsSection"/> class.
        /// </summary>
        public PluginDebugSettingsSection()
        {
            InitializeComponent();
        }

        private void VerboseLogging_Changed(object sender, RoutedEventArgs e)
        {
            IPluginSettings settings = TryGetPluginSettings();
            if (settings != null)
            {
                Common.SyncVerboseLoggingFromSettings(settings, "settings-ui");
            }
        }

        private void CreateLogPackage_Click(object sender, RoutedEventArgs e)
        {
            string pluginName = PluginName;
            if (string.IsNullOrWhiteSpace(pluginName))
            {
                pluginName = "Plugin";
            }

            PlayniteTools.CreateLogPackage(pluginName);
        }

        private IPluginSettings TryGetPluginSettings()
        {
            if (DataContext is IPluginSettingsViewModel viewModel)
            {
                return viewModel.Settings;
            }

            return DataContext as IPluginSettings;
        }
    }
}
