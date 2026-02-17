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
    public partial class ListWithNoData : UserControl
    {
        private IPluginDatabase _pluginDatabase;
        private List<GameDataViewModel> _allGames = new List<GameDataViewModel>();
        private List<GameDataViewModel> _filteredGames = new List<GameDataViewModel>();

        private SelectableDbItemList _sourceList;
        private SelectableDbItemList _platformList;

        private RelayCommand<Guid> _goToGame;

        public ListWithNoData(IPluginDatabase pluginDatabase)
        {
            _pluginDatabase = pluginDatabase;

            InitializeComponent();

            _goToGame = new RelayCommand<Guid>((id) =>
            {
                API.Instance.MainView.SelectGame(id);
                API.Instance.MainView.SwitchToLibraryView();
            });

            RefreshData();
        }

        #region Data Management

        private void RefreshData()
        {
            ShowProgress(true);
            try
            {
                IEnumerable<Game> games = _pluginDatabase.GetGamesWithNoData();
                _allGames = games.Select(game => new GameDataViewModel(game, _goToGame)).ToList();

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
            // Collect distinct DatabaseObject instances directly from games
            Dictionary<Guid, GameSource> sourcesById = new Dictionary<Guid, GameSource>();
            Dictionary<Guid, Platform> platformsById = new Dictionary<Guid, Platform>();

            foreach (GameDataViewModel vm in _allGames)
            {
                if (vm.Source != null && !sourcesById.ContainsKey(vm.Source.Id))
                {
                    sourcesById[vm.Source.Id] = vm.Source;
                }

                foreach (Platform platform in vm.Platforms)
                {
                    if (!platformsById.ContainsKey(platform.Id))
                    {
                        platformsById[platform.Id] = platform;
                    }
                }
            }

            // Preserve current selection across refreshes
            IEnumerable<Guid> previousSourceIds = _sourceList?.GetSelectedIds();
            IEnumerable<Guid> previousPlatformIds = _platformList?.GetSelectedIds();

            if (_sourceList != null) { _sourceList.SelectionChanged -= OnFilterSelectionChanged; }
            if (_platformList != null) { _platformList.SelectionChanged -= OnFilterSelectionChanged; }

            _sourceList = new SelectableDbItemList(
                sourcesById.Values.OrderBy(s => s.Name),
                previousSourceIds);

            _platformList = new SelectableDbItemList(
                platformsById.Values.OrderBy(p => p.Name),
                previousPlatformIds);

            _sourceList.SelectionChanged += OnFilterSelectionChanged;
            _platformList.SelectionChanged += OnFilterSelectionChanged;

            PART_SourceFilter.ItemsList = _sourceList;
            PART_PlatformFilter.ItemsList = _platformList;
        }

        private void ApplyFilters()
        {
            string searchText = PART_SearchBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;

            HashSet<Guid> selectedSourceIds = new HashSet<Guid>(
                _sourceList?.GetSelectedIds() ?? Enumerable.Empty<Guid>());

            HashSet<Guid> selectedPlatformIds = new HashSet<Guid>(
                _platformList?.GetSelectedIds() ?? Enumerable.Empty<Guid>());

            _filteredGames = _allGames.Where(game =>
            {
                if (!string.IsNullOrEmpty(searchText) &&
                    !game.Name.ToLowerInvariant().Contains(searchText))
                {
                    return false;
                }

                if (selectedSourceIds.Count > 0 &&
                    (game.Source == null || !selectedSourceIds.Contains(game.Source.Id)))
                {
                    return false;
                }

                if (selectedPlatformIds.Count > 0 &&
                    !game.Platforms.Any(p => selectedPlatformIds.Contains(p.Id)))
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

        private void OnFilterSelectionChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (GameDataViewModel game in _filteredGames) { game.IsSelected = true; }
            UpdateSelectedCount();
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (GameDataViewModel game in _filteredGames) { game.IsSelected = false; }
            UpdateSelectedCount();
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectedCount();
        }

        private void RefreshSelected_Click(object sender, RoutedEventArgs e)
        {
            List<Guid> selectedIds = _filteredGames
                .Where(g => g.IsSelected)
                .Select(g => g.Id)
                .ToList();

            if (selectedIds.Count == 0) { return; }

            ShowProgress(true);
            try
            {
                _pluginDatabase.Refresh(selectedIds);
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
                _pluginDatabase.Refresh(_allGames.Select(x => x.Id));
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
            int count = _filteredGames.Count(g => g.IsSelected);
            PART_SelectedCount.Text = count.ToString();
            PART_RefreshSelected.IsEnabled = count > 0;
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

        public Guid Id { get; }
        public string Name { get; }
        public string PlatformName { get; }
        public string SourceName { get; }
        public string InstallStatusText { get; }
        public Brush InstallStatusBrush { get; }
        public DateTime? AddedDate { get; }
        public string AddedDateText { get; }
        public string AddedDateFull { get; }
        public DateTime? LastActivityDate { get; }
        public string LastActivityText { get; }
        public string LastActivityFull { get; }
        public Brush LastActivityBrush { get; }
        public RelayCommand<Guid> GoToGame { get; }

        // Raw Playnite objects for Guid-based filtering
        public GameSource Source { get; }
        public IReadOnlyList<Platform> Platforms { get; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public GameDataViewModel(Game game, RelayCommand<Guid> goToGameCommand)
        {
            Id = game.Id;
            Name = game.Name;
            GoToGame = goToGameCommand;

            Source = game.Source;
            Platforms = game.Platforms?.ToList() ?? new List<Platform>();

            PlatformName = BuildPlatformName(game);
            SourceName = game.Source?.Name ?? ResourceProvider.GetString("LOCUnknownLabel");

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
                TimeSpan elapsed = DateTime.Now - game.LastActivity.Value;
                LastActivityText = FormatTimeAgo(elapsed);
                LastActivityFull = game.LastActivity.Value.ToString("dd MMMM yyyy HH:mm");
                LastActivityBrush = elapsed.TotalDays > 365
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

        private static string BuildPlatformName(Game game)
        {
            if (game.Platforms == null || game.Platforms.Count == 0)
            {
                return ResourceProvider.GetString("LOCUnknownLabel");
            }

            if (game.Platforms.Count == 1)
            {
                return game.Platforms[0].Name;
            }

            string joined = string.Join(", ", game.Platforms.Take(2).Select(p => p.Name));
            return game.Platforms.Count > 2
                ? string.Format("{0} +{1}", joined, game.Platforms.Count - 2)
                : joined;
        }

        private static string FormatTimeAgo(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 365)
            {
                int years = (int)(timeSpan.TotalDays / 365);
                return string.Format(ResourceProvider.GetString(years == 1 ? "LOCCommonYearAgo" : "LOCCommonYearsAgo"), years);
            }
            if (timeSpan.TotalDays >= 30)
            {
                int months = (int)(timeSpan.TotalDays / 30);
                return string.Format(ResourceProvider.GetString(months == 1 ? "LOCCommonMonthAgo" : "LOCCommonMonthsAgo"), months);
            }
            if (timeSpan.TotalDays >= 1)
            {
                int days = (int)timeSpan.TotalDays;
                return string.Format(ResourceProvider.GetString(days == 1 ? "LOCCommonDayAgo" : "LOCCommonDaysAgo"), days);
            }
            if (timeSpan.TotalHours >= 1)
            {
                int hours = (int)timeSpan.TotalHours;
                return string.Format(ResourceProvider.GetString(hours == 1 ? "LOCCommonHourAgo" : "LOCCommonHoursAgo"), hours);
            }
            return ResourceProvider.GetString("LOCCommonToday");
        }
    }

    #endregion
}