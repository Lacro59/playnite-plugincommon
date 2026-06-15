using CommonPluginsShared;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// Interaction logic for OptionsDownloadData.xaml
    /// </summary>
    public partial class OptionsDownloadData : UserControl
    {
        private List<Game> FilteredGames { get; set; }
        private IPluginDatabase PluginDatabase { get; }


        public OptionsDownloadData(IPluginDatabase pluginDatabase, bool withoutMissing = false)
        {
            PluginDatabase = pluginDatabase;

            InitializeComponent();

            if (withoutMissing)
            {
                PART_OnlyMissing.Visibility = Visibility.Collapsed;
                PART_BtDownload.Content = ResourceProvider.GetString("LOCGameTagsTitle");
            }
            else
            {
                PART_TagMissing.Visibility = Visibility.Collapsed;
            }

            UpdateDependentControls();
        }


        private void PART_BtClose_Click(object sender, RoutedEventArgs e)
        {
            ((Window)Parent).Close();
        }

        private void PART_BtDownload_Click(object sender, RoutedEventArgs e)
        {
            FilteredGames = API.Instance.Database.Games.Where(x => x.Hidden == false).ToList();
            int months = (int)PART_Months.LongValue;

            if ((bool)PART_AllGames.IsChecked)
            {
            }

            if ((bool)PART_Filtred.IsChecked)
            {
                FilteredGames = API.Instance.MainView.FilteredGames;
            }

            if ((bool)PART_Selected.IsChecked)
            {
                FilteredGames = API.Instance.MainView.SelectedGames.ToList();
            }

            if ((bool)PART_GamesRecentlyPlayed.IsChecked)
            {
                FilteredGames = FilteredGames.Where(x => x.LastActivity != null && (DateTime)x.LastActivity >= DateTime.Now.AddMonths(-months)).ToList();
            }

            if ((bool)PART_GamesRecentlyAdded.IsChecked)
            {
                FilteredGames = FilteredGames.Where(x => x.Added != null && (DateTime)x.Added >= DateTime.Now.AddMonths(-months)).ToList();
            }

            if ((bool)PART_GamesInstalled.IsChecked)
            {
                FilteredGames = FilteredGames.Where(x => x.IsInstalled).ToList();
            }

            if ((bool)PART_GamesNotInstalled.IsChecked)
            {
                FilteredGames = FilteredGames.Where(x => !x.IsInstalled).ToList();
            }

            if ((bool)PART_GamesFavorite.IsChecked)
            {
                FilteredGames = FilteredGames.Where(x => x.Favorite).ToList();
            }

            if ((bool)PART_OldData.IsChecked)
            {
                HashSet<Guid> oldDataIds = new HashSet<Guid>(
                    PluginDatabase.GetGamesOldData(months).Select(x => x.Id));
                FilteredGames = FilteredGames.Where(x => oldDataIds.Contains(x.Id)).ToList();
            }

            if (PluginDatabase.FilterSettings != null)
            {
                int beforeLibraryFilter = FilteredGames?.Count ?? 0;
                FilteredGames = PlayniteTools.FilterLibraryGames(FilteredGames, PluginDatabase.FilterSettings).ToList();
                Common.LogDebug(true, string.Format(
                    "[LibraryFilter] OptionsDownloadData: {0} -> {1} games after library filter (IncludeEmulatedGames={2}, SourceFilter={3})",
                    beforeLibraryFilter,
                    FilteredGames.Count,
                    PluginDatabase.FilterSettings.IncludeEmulatedGames,
                    PlayniteTools.FormatSourceFilterForLog(PluginDatabase.FilterSettings)));
            }

            LogDownloadFilters(months);

            ((Window)Parent).Close();
        }

        private void LogDownloadFilters(int months)
        {
            string gameSource = "AllGames";
            if (PART_Filtred.IsChecked == true)
            {
                gameSource = "Filtered";
            }
            else if (PART_Selected.IsChecked == true)
            {
                gameSource = "Selected";
            }

            var installationFilters = new List<string>();
            if (PART_GamesInstalled.IsChecked == true)
            {
                installationFilters.Add("Installed");
            }

            if (PART_GamesNotInstalled.IsChecked == true)
            {
                installationFilters.Add("NotInstalled");
            }

            if (PART_GamesFavorite.IsChecked == true)
            {
                installationFilters.Add("Favorite");
            }

            var timeFilters = new List<string>();
            if (PART_OldData.IsChecked == true)
            {
                timeFilters.Add("OldData");
            }

            if (PART_GamesRecentlyPlayed.IsChecked == true)
            {
                timeFilters.Add("RecentlyPlayed");
            }

            if (PART_GamesRecentlyAdded.IsChecked == true)
            {
                timeFilters.Add("RecentlyAdded");
            }

            string installation = installationFilters.Count > 0
                ? string.Join(", ", installationFilters)
                : "(none)";
            string time = timeFilters.Count > 0
                ? string.Join(", ", timeFilters)
                : "(none)";
            int gameCount = FilteredGames?.Count ?? 0;

            Common.LogDebug(true, string.Format(
                "[OptionsDownloadData] GameSource={0}, Installation={1}, Time={2}, Months={3}, TagMissing={4}, OnlyMissing={5}, Games={6}",
                gameSource,
                installation,
                time,
                months,
                PART_TagMissing.IsChecked == true,
                PART_OnlyMissing.IsChecked == true,
                gameCount));
        }


        public List<Game> GetFilteredGames()
        {
            return FilteredGames;
        }

        public bool GetTagMissing()
        {
            return (bool)PART_TagMissing.IsChecked;
        }

        public bool GetOnlyMissing()
        {
            return (bool)PART_OnlyMissing.IsChecked;
        }

        private void FilterRadio_Checked(object sender, RoutedEventArgs e)
        {
            UpdateDependentControls();
        }

        private void UpdateDependentControls()
        {
            bool useMonthFilter = PART_OldData.IsChecked == true
                || PART_GamesRecentlyAdded.IsChecked == true
                || PART_GamesRecentlyPlayed.IsChecked == true;

            Part_MonthSelect.IsEnabled = useMonthFilter;
            PART_OnlyMissing.IsEnabled = PART_OldData.IsChecked != true;
        }
    }
}
