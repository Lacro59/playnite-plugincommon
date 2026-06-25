using CommonPluginsShared.Extensions;
using CommonPluginsStores;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// Dialog content for browsing categorized Playnite path variables with resolved preview values.
    /// </summary>
    public partial class SelectVariable : UserControl
    {
        private ListBox _activeListBox;

        /// <summary>
        /// Initializes path-variable picker using the most recently active game for preview resolution.
        /// </summary>
        public SelectVariable()
            : this(SelectVariableMode.Path, null)
        {
        }

        /// <summary>
        /// Initializes variable picker for the requested mode and optional preview game context.
        /// </summary>
        /// <param name="mode">Variable catalog to display.</param>
        /// <param name="previewGame">Game used to resolve preview values; defaults to last active game.</param>
        public SelectVariable(SelectVariableMode mode, Game previewGame = null)
        {
            InitializeComponent();

            Game game = previewGame
                ?? API.Instance.Database.Games.OrderByDescending(x => x.LastActivity).FirstOrDefault();

            PART_Categories.ItemsSource = mode == SelectVariableMode.FilePattern
                ? BuildFilePatternCategories(game)
                : BuildPathCategories(game);
        }

        /// <summary>
        /// Gets the variable token chosen by the user, or <c>null</c> when the dialog was cancelled.
        /// </summary>
        public string SelectedVariable { get; private set; }

        /// <summary>
        /// Gets whether the user confirmed a variable selection.
        /// </summary>
        public bool WasSelected => !string.IsNullOrEmpty(SelectedVariable);

        private static readonly string[] GameInfoVariables =
        {
            "{Name}", "{Platform}", "{GameId}", "{DatabaseId}", "{PluginId}", "{Version}",
            "{EmulatorDir}", "{InstallDirName}", "{ImagePath}", "{ImageName}", "{ImageNameNoExt}",
            "{SteamId}", "{SteamAccountId}"
        };

        private static readonly string[] FilePatternVariables =
        {
            "{Name}", "{DateModified}", "{DateTimeModified}", "{digit}"
        };

        private static readonly string[] FilePatternGameInfoVariables =
        {
            "{Platform}", "{GameId}", "{DatabaseId}", "{PluginId}", "{Version}",
            "{EmulatorDir}", "{InstallDirName}", "{ImagePath}", "{ImageName}", "{ImageNameNoExt}",
            "{SteamId}", "{SteamAccountId}"
        };

        private static ObservableCollection<PluginVariableCategory> BuildPathCategories(Game game)
        {
            return new ObservableCollection<PluginVariableCategory>
            {
                CreateCategory(
                    ResourceProvider.GetString("LOCCommonVariableCategoryFolderPath"),
                    new[]
                    {
                        "{DropboxFolder}", "{OneDriveFolder}",
                        "{InstallDir}", "{PlayniteDir}",
                        "{SteamInstallDir}", "{SteamScreenshotsDir}",
                        "{UbisoftInstallDir}", "{UbisoftScreenshotsDir}",
                        "{RetroArchScreenshotsDir}",
                        "{WinDir}", "{AllUsersProfile}", "{AppData}", "{HomePath}", "{UserProfile}",
                        "{HomeDrive}", "{SystemDrive}", "{SystemRoot}", "{Public}",
                        "{CommonProgramW6432}", "{CommonProgramFiles}", "{ProgramFiles}",
                        "{CommonProgramFiles(x86)}", "{ProgramFiles(x86)}"
                    },
                    game),
                CreateCategory(
                    ResourceProvider.GetString("LOCCommonVariableCategoryGameInfo"),
                    GameInfoVariables,
                    game)
            };
        }

        private static ObservableCollection<PluginVariableCategory> BuildFilePatternCategories(Game game)
        {
            return new ObservableCollection<PluginVariableCategory>
            {
                CreateCategory(
                    ResourceProvider.GetString("LOCCommonVariableCategoryFilePattern"),
                    FilePatternVariables,
                    game,
                    isFilePattern: true),
                CreateCategory(
                    ResourceProvider.GetString("LOCCommonVariableCategoryGameInfo"),
                    FilePatternGameInfoVariables,
                    game,
                    isFilePattern: true)
            };
        }

        private static PluginVariableCategory CreateCategory(
            string categoryName,
            IEnumerable<string> variables,
            Game game,
            bool isFilePattern = false)
        {
            return new PluginVariableCategory
            {
                CategoryName = categoryName,
                Variables = variables
                    .Select(variable => new PluginVariable
                    {
                        Name = variable,
                        Value = ResolvePreviewValue(game, variable, isFilePattern)
                    })
                    .ToList()
            };
        }

        private static string ResolvePreviewValue(Game game, string variable, bool isFilePattern)
        {
            if (isFilePattern)
            {
                switch (variable)
                {
                    case "{DateModified}":
                        return DateTime.Now.ToString("yyyy-MM-dd");
                    case "{DateTimeModified}":
                        return DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
                    case "{digit}":
                        return string.Empty;
                }
            }

            string resolved = PlayniteTools.StringExpandWithStores(game, variable);
            return variable.IsEqual(resolved) ? string.Empty : resolved;
        }

        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem != null)
            {
                _activeListBox = listBox;
                PART_BtInsert.IsEnabled = true;
            }
        }

        private void CategoryListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is PluginVariable variable)
            {
                _activeListBox = listBox;
                ConfirmSelection(variable.Name);
            }
        }

        private void PART_BtInsert_Click(object sender, RoutedEventArgs e)
        {
            if (_activeListBox?.SelectedItem is PluginVariable variable)
            {
                ConfirmSelection(variable.Name);
            }
        }

        private void PART_BtClose_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog(false);
        }

        private void ConfirmSelection(string variableName)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                return;
            }

            SelectedVariable = variableName;
            CloseDialog(true);
        }

        private void CloseDialog(bool dialogResult)
        {
            Window window = Window.GetWindow(this);
            if (window == null)
            {
                return;
            }

            window.DialogResult = dialogResult;
            window.Close();
        }
    }

    /// <summary>
    /// Variable catalog displayed by <see cref="SelectVariable"/>.
    /// </summary>
    public enum SelectVariableMode
    {
        /// <summary>Playnite path and game metadata variables.</summary>
        Path,

        /// <summary>Screenshots Visualizer file-pattern tokens.</summary>
        FilePattern
    }

    /// <summary>
    /// Group of variables shown under one expander header.
    /// </summary>
    public class PluginVariableCategory
    {
        /// <summary>Localized category title.</summary>
        public string CategoryName { get; set; }

        /// <summary>Variables in this category.</summary>
        public List<PluginVariable> Variables { get; set; }
    }

    /// <summary>
    /// Variable placeholder and resolved preview value.
    /// </summary>
    public class PluginVariable
    {
        /// <summary>Placeholder token (for example <c>{Name}</c>).</summary>
        public string Name { get; set; }

        /// <summary>Resolved preview for the active game context.</summary>
        public string Value { get; set; }
    }
}
