using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CommonPluginsControls.Views
{
    /// <summary>
    /// Interaction logic for ListWithNoData.xaml
    /// </summary>
    public partial class ListWithNoData : UserControl
    {
        private IPluginDatabase PluginDatabase { get; set; }
        private List<GameDataViewModel> _allGames = new List<GameDataViewModel>();
        private List<GameDataViewModel> _filteredGames = new List<GameDataViewModel>();

        private RelayCommand<Guid> GoToGame { get; set; }

        public ListWithNoData(IPluginDatabase pluginDatabase)
        {
            this.PluginDatabase = pluginDatabase;

            InitializeComponent();

            GoToGame = new RelayCommand<Guid>((id) =>
            {
                API.Instance.MainView.SelectGame(id);
                API.Instance.MainView.SwitchToLibraryView();
            });

            InitializeFilters();
            RefreshData();
        }

        #region Initialization

        private void InitializeFilters()
        {
            PART_SourceFilter.Items.Add(new FilterItem { Display = "All Sources", Value = null });
            PART_PlatformFilter.Items.Add(new FilterItem { Display = "All Platforms", Value = null });

            PART_SourceFilter.SelectedIndex = 0;
            PART_PlatformFilter.SelectedIndex = 0;
        }

        #endregion

        #region Data Management

        private void RefreshData()
        {
            ShowProgress(true);

            try
            {
                IEnumerable<Game> games = PluginDatabase.GetGamesWithNoData();
                _allGames = games.Select(game => new GameDataViewModel(game, GoToGame)).ToList();

                UpdateFilterOptions();
                ApplyFilters();
            }
            finally
            {
                ShowProgress(false);
            }
        }

        private void UpdateFilterOptions()
        {
            HashSet<string> sources = new HashSet<string>();
            HashSet<string> platforms = new HashSet<string>();

            foreach (GameDataViewModel game in _allGames)
            {
                if (!string.IsNullOrEmpty(game.SourceName))
                {
                    sources.Add(game.SourceName);
                }
                if (!string.IsNullOrEmpty(game.PlatformName))
                {
                    platforms.Add(game.PlatformName);
                }
            }

            object selectedSource = PART_SourceFilter.SelectedItem;
            object selectedPlatform = PART_PlatformFilter.SelectedItem;

            PART_SourceFilter.Items.Clear();
            PART_SourceFilter.Items.Add(new FilterItem { Display = "All Sources", Value = null });
            foreach (string source in sources.OrderBy(s => s))
            {
                PART_SourceFilter.Items.Add(new FilterItem { Display = source, Value = source });
            }

            PART_PlatformFilter.Items.Clear();
            PART_PlatformFilter.Items.Add(new FilterItem { Display = "All Platforms", Value = null });
            foreach (string platform in platforms.OrderBy(p => p))
            {
                PART_PlatformFilter.Items.Add(new FilterItem { Display = platform, Value = platform });
            }

            PART_SourceFilter.SelectedItem = selectedSource ?? PART_SourceFilter.Items[0];
            PART_PlatformFilter.SelectedItem = selectedPlatform ?? PART_PlatformFilter.Items[0];
        }

        private void ApplyFilters()
        {
            string searchText = PART_SearchBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            string selectedSource = (PART_SourceFilter.SelectedItem as FilterItem)?.Value;
            string selectedPlatform = (PART_PlatformFilter.SelectedItem as FilterItem)?.Value;

            _filteredGames = _allGames.Where(game =>
            {
                if (!string.IsNullOrEmpty(searchText) && !game.Name.ToLowerInvariant().Contains(searchText))
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(selectedSource) && game.SourceName != selectedSource)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(selectedPlatform) && game.PlatformName != selectedPlatform)
                {
                    return false;
                }

                return true;
            }).ToList();

            ListViewGames.ItemsSource = null;
            ListViewGames.ItemsSource = _filteredGames;

            PART_Count.Text = _filteredGames.Count.ToString();
            UpdateSelectedCount();

            if (_filteredGames.Count > 0)
            {
                ListViewGames.Sorting();
            }
        }

        #endregion

        #region Event Handlers

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SourceFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allGames.Count > 0)
            {
                ApplyFilters();
            }
        }

        private void PlatformFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allGames.Count > 0)
            {
                ApplyFilters();
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (GameDataViewModel game in _filteredGames)
            {
                game.IsSelected = true;
            }
            UpdateSelectedCount();
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (GameDataViewModel game in _filteredGames)
            {
                game.IsSelected = false;
            }
            UpdateSelectedCount();
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectedCount();
        }

        private void RefreshSelected_Click(object sender, RoutedEventArgs e)
        {
            List<Guid> selectedIds = _filteredGames.Where(g => g.IsSelected).Select(g => g.Id).ToList();

            if (selectedIds.Count == 0)
            {
                return;
            }

            ShowProgress(true);

            try
            {
                PluginDatabase.Refresh(selectedIds);
                RefreshData();
            }
            finally
            {
                ShowProgress(false);
            }
        }

        private void RefreshAll_Click(object sender, RoutedEventArgs e)
        {
            ShowProgress(true);

            try
            {
                PluginDatabase.Refresh(_allGames.Select(x => x.Id));
                RefreshData();
            }
            finally
            {
                ShowProgress(false);
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateSelectedCount()
        {
            int selectedCount = _filteredGames.Count(g => g.IsSelected);
            PART_SelectedCount.Text = selectedCount.ToString();

            PART_RefreshSelected.IsEnabled = selectedCount > 0;
        }

        private void ShowProgress(bool show)
        {
            PART_ProgressBar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion
    }

    #region View Models

    public class GameDataViewModel : ObservableObject
    {
        private bool _isSelected;

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string PlatformName { get; set; }
        public string SourceName { get; set; }
        public string InstallStatusText { get; set; }
        public Brush InstallStatusBrush { get; set; }
        public DateTime? AddedDate { get; set; }
        public string AddedDateText { get; set; }
        public string AddedDateFull { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public string LastActivityText { get; set; }
        public string LastActivityFull { get; set; }
        public Brush LastActivityBrush { get; set; }
        public RelayCommand<Guid> GoToGame { get; set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public GameDataViewModel(Game game, RelayCommand<Guid> goToGameCommand)
        {
            Id = game.Id;
            Name = game.Name;
            GoToGame = goToGameCommand;

            PlatformName = GetPlatformNames(game);
            SourceName = game.Source?.Name ?? ResourceProvider.GetString("LOCUnknownLabel");

            // Utiliser les clés natives de Playnite
            InstallStatusText = game.IsInstalled
                ? ResourceProvider.GetString("LOCGameIsInstalledTitle")
                : ResourceProvider.GetString("LOCGameIsUnInstalledTitle");

            InstallStatusBrush = game.IsInstalled
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                : new SolidColorBrush(Color.FromRgb(158, 158, 158));

            AddedDate = game.Added;
            if (game.Added.HasValue)
            {
                AddedDateText = game.Added.Value.ToString("dd/MM/yyyy");
                AddedDateFull = game.Added.Value.ToString("dd MMMM yyyy HH:mm");
            }
            else
            {
                AddedDateText = "N/A";
                AddedDateFull = "Not available";
            }

            LastActivityDate = game.LastActivity;
            if (game.LastActivity.HasValue)
            {
                TimeSpan timeSinceActivity = DateTime.Now - game.LastActivity.Value;
                LastActivityText = FormatTimeAgo(timeSinceActivity);
                LastActivityFull = game.LastActivity.Value.ToString("dd MMMM yyyy HH:mm");
                LastActivityBrush = timeSinceActivity.TotalDays > 365
                    ? new SolidColorBrush(Color.FromRgb(244, 67, 54))
                    : new SolidColorBrush(Color.FromRgb(255, 152, 0));
            }
            else
            {
                LastActivityText = ResourceProvider.GetString("LOCCommonNever");
                LastActivityFull = ResourceProvider.GetString("LOCCommonNever");
                LastActivityBrush = new SolidColorBrush(Color.FromRgb(158, 158, 158));
            }
        }

        private string GetPlatformNames(Game game)
        {
            if (game.Platforms == null || game.Platforms.Count == 0)
            {
                return ResourceProvider.GetString("LOCUnknownLabel");
            }

            if (game.Platforms.Count == 1)
            {
                return game.Platforms[0].Name;
            }

            return string.Join(", ", game.Platforms.Take(2).Select(p => p.Name)) +
                   (game.Platforms.Count > 2 ? $" +{game.Platforms.Count - 2}" : string.Empty);
        }

        private string FormatTimeAgo(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 365)
            {
                int years = (int)(timeSpan.TotalDays / 365);
                string key = years == 1 ? "LOCCommonYearAgo" : "LOCCommonYearsAgo";
                return string.Format(ResourceProvider.GetString(key), years);
            }

            if (timeSpan.TotalDays >= 30)
            {
                int months = (int)(timeSpan.TotalDays / 30);
                string key = months == 1 ? "LOCCommonMonthAgo" : "LOCCommonMonthsAgo";
                return string.Format(ResourceProvider.GetString(key), months);
            }

            if (timeSpan.TotalDays >= 1)
            {
                int days = (int)timeSpan.TotalDays;
                string key = days == 1 ? "LOCCommonDayAgo" : "LOCCommonDaysAgo";
                return string.Format(ResourceProvider.GetString(key), days);
            }

            if (timeSpan.TotalHours >= 1)
            {
                int hours = (int)timeSpan.TotalHours;
                string key = hours == 1 ? "LOCCommonHourAgo" : "LOCCommonHoursAgo";
                return string.Format(ResourceProvider.GetString(key), hours);
            }

            return ResourceProvider.GetString("LOCCommonToday");
        }
    }

    public class FilterItem
    {
        public string Display { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return Display;
        }
    }

    #endregion
}