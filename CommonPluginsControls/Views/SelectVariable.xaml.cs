using CommonPluginsStores;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using CommonPluginsShared.Extensions;

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// Logique d'interaction pour SelectVariable.xaml
    /// </summary>
    public partial class SelectVariable : UserControl
    {
        public SelectVariable()
        {
            InitializeComponent();

            List<string> ListVariables = PlayniteTools.ListVariables;
            List<PluginVariable> pluginVariables = ListVariables
                .Select(x => new PluginVariable
                {
                    Name = x,
                    Value = x.IsEqual(PlayniteTools.StringExpandWithStores(null, x)) ? string.Empty : PlayniteTools.StringExpandWithStores(null, x)
                })
                .ToList();

            PART_ListBox.ItemsSource = pluginVariables;
        }


        private void PART_BtClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
        }

        private void PART_BtCopy_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Clipboard.SetText(((PluginVariable)PART_ListBox.SelectedItem).Name.ToString());
            ((Window)this.Parent).Close();
        }


        private void PART_ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PART_BtCopy.IsEnabled = true;
        }
    }


    public class PluginVariable
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
