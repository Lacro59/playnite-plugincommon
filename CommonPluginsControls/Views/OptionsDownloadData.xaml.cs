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
    /// Logique d'interaction pour OptionsDownloadData.xaml
    /// </summary>
    public partial class OptionsDownloadData : UserControl
    {
        private List<Game> FilteredGames { get; set; }


        public OptionsDownloadData(bool WithoutMissing = false)
        {
            InitializeComponent();

            if (WithoutMissing)
            {
                PART_OnlyMissing.Visibility = Visibility.Collapsed;
                PART_BtDownload.Content = ResourceProvider.GetString("LOCGameTagsTitle");
            }
            else
            {
                PART_TagMissing.Visibility = Visibility.Collapsed;
            }
        }


        private void PART_BtClose_Click(object sender, RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
        }

        private void PART_BtDownload_Click(object sender, RoutedEventArgs e)
        {
            FilteredGames = API.Instance.Database.Games.Where(x => x.Hidden == false).ToList();

            if ((bool)PART_AllGames.IsChecked)
            {
                
            }

            if ((bool)PART_GamesRecentlyPlayed.IsChecked)
            {
                FilteredGames = FilteredGames.Where(x => x.LastActivity != null && (DateTime)x.LastActivity >= DateTime.Now.AddMonths(-1)).ToList();
            }

            if ((bool)PART_GamesRecentlyAdded.IsChecked)
            {
                FilteredGames = FilteredGames.Where(x => x.Added != null && (DateTime)x.Added >= DateTime.Now.AddMonths(-1)).ToList();
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

            ((Window)this.Parent).Close();
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
    }
}
