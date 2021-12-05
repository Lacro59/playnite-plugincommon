using CommonPluginsStores;
using System.Windows;
using System.Windows.Controls;

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

            PART_ListBox.ItemsSource = PlayniteTools.ListVariables;
        }


        private void PART_BtClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
        }

        private void PART_BtCopy_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Clipboard.SetText(PART_ListBox.SelectedItem.ToString());
            ((Window)this.Parent).Close();
        }


        private void PART_ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PART_BtCopy.IsEnabled = true;
        }
    }
}
