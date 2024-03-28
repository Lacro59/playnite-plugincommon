using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CommonPluginsControls.Views
{
    /// <summary>
    /// Logique d'interaction pour ListWithNoData.xaml
    /// </summary>
    public partial class ListWithNoData : UserControl
    {
        private IPluginDatabase PluginDatabase { get; set; }
        private List<GameData> gameData = new List<GameData>();

        RelayCommand<Guid> GoToGame { get; set; }

        public ListWithNoData(IPluginDatabase PluginDatabase)
        {
            this.PluginDatabase = PluginDatabase;

            InitializeComponent();

            GoToGame = new RelayCommand<Guid>((Id) =>
            {
                API.Instance.MainView.SelectGame(Id);
                API.Instance.MainView.SwitchToLibraryView();
            });

            RefreshData();
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List<Guid> Ids = gameData.Select(x => x.Id).ToList();
            PluginDatabase.RefreshWithNoData(Ids);

            RefreshData();
        }


        private void RefreshData()
        {
            ListViewGames.ItemsSource = null;
            List<Game> games = PluginDatabase.GetGamesWithNoData();
            gameData = games.Select(x => new GameData { Id = x.Id, Name = x.Name, GoToGame = GoToGame }).ToList();            
            ListViewGames.ItemsSource = gameData;

            PART_Count.Content = gameData.Count;

            ListViewGames.Sorting();
        }
    }


    public class GameData
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public RelayCommand<Guid> GoToGame { get; set; }
    }
}
