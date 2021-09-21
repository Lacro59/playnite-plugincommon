using Playnite.SDK;
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
        public ListWithNoData(IPlayniteAPI PlayniteApi, List<Game> games)
        {
            InitializeComponent();

            RelayCommand<Guid> GoToGame = new RelayCommand<Guid>((Id) =>
            {
                PlayniteApi.MainView.SelectGame(Id);
                PlayniteApi.MainView.SwitchToLibraryView();
            });

            List<GameData> gameData = games.Select(x => new GameData { Id = x.Id,  Name = x.Name, GoToGame = GoToGame }).ToList();

            ListViewGames.ItemsSource = gameData;
        }
    }


    public class GameData
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public RelayCommand<Guid> GoToGame { get; set; }
    }
}
