using Playnite.SDK;
using CommonPluginsShared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared;

namespace CommonPluginsControls.Views
{
    /// <summary>
    /// Logique d'interaction pour TransfertData.xaml
    /// </summary>
    public partial class TransfertData : UserControl
    {
        private IPluginDatabase PluginDatabase { get; set; }

        public TransfertData(List<DataGame> DataPluginGames, IPluginDatabase PluginDatabase)
        {
            this.PluginDatabase = PluginDatabase;

            InitializeComponent();


            var DataGames = API.Instance.Database.Games.Where(x => !x.Hidden).Select(x => new DataGame
            {
                Id = x.Id,
                Icon = x.Icon.IsNullOrEmpty() ? x.Icon : API.Instance.Database.GetFullFilePath(x.Icon),
                Name = x.Name,
                CountData = PluginDatabase.Get(x.Id, true)?.Count ?? 0
            }).Distinct().ToList();
            
            PART_CbPluginGame.ItemsSource = DataPluginGames.OrderBy(x => x.Name).ToList();
            PART_CbGame.ItemsSource = DataGames.OrderBy(x => x.Name).ToList();
        }


        private void PART_BtClose_Click(object sender, RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
        }

        private void PART_BtDTransfer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var PluginData = PluginDatabase.Get(((DataGame)PART_CbPluginGame.SelectedItem).Id, true);

                PluginData.Id = ((DataGame)PART_CbGame.SelectedItem).Id;
                PluginData.Name = ((DataGame)PART_CbGame.SelectedItem).Name;
                PluginData.Game = API.Instance.Database.Games.Get(((DataGame)PART_CbGame.SelectedItem).Id);

                PluginDatabase.AddOrUpdate(PluginData);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            ((Window)this.Parent).Close();
        }


        private void PART_Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PART_CbPluginGame.SelectedIndex == -1 || PART_CbGame.SelectedIndex == -1)
            {
                PART_BtDTransfer.IsEnabled = false;
            }
            else
            {
                if (((DataGame)PART_CbPluginGame.SelectedItem).Id == ((DataGame)PART_CbGame.SelectedItem).Id)
                {
                    PART_BtDTransfer.IsEnabled = false;
                }
                else
                {
                    PART_BtDTransfer.IsEnabled = true;
                }
            }
        }
    }
}
