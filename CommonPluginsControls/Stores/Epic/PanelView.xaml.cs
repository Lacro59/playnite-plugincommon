using System.Windows;
using System.Windows.Controls;
using CommonPluginsStores;
using CommonPluginsStores.Steam;

namespace CommonPluginsControls.Stores.Epic
{
    /// <summary>
    /// Logique d'interaction pour Panel.xaml
    /// </summary>
    public partial class PanelView : UserControl
    {
        private PanelViewModel PanelViewModel { get; set; } = new PanelViewModel();

        #region Properties
        public IStoreApi StoreApi
        {
            get => (SteamApi)GetValue(steamApiProperty);
            set => SetValue(steamApiProperty, value);
        }

        public static readonly DependencyProperty steamApiProperty = DependencyProperty.Register(
            nameof(StoreApi),
            typeof(IStoreApi),
            typeof(PanelView),
            new FrameworkPropertyMetadata(null, SteamApiPropertyChangedCallback));

        private static void SteamApiPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PanelView obj)
            {
                obj.PanelViewModel.StoreApi = (IStoreApi)e.NewValue;
            }
        }
        #endregion

        public PanelView()
        {
            InitializeComponent();

            DataContext = PanelViewModel;
        }
    }
}
