using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsControls.Stores
{
    /// <summary>
    /// Shared connection block with auth status badge and login actions for store settings panels.
    /// </summary>
    public partial class StoreConnectionSection : UserControl
    {
        public bool HasAlternativeLogin
        {
            get => (bool)GetValue(HasAlternativeLoginProperty);
            set => SetValue(HasAlternativeLoginProperty, value);
        }

        public static readonly DependencyProperty HasAlternativeLoginProperty = DependencyProperty.Register(
            nameof(HasAlternativeLogin),
            typeof(bool),
            typeof(StoreConnectionSection),
            new FrameworkPropertyMetadata(false));

        public StoreConnectionSection()
        {
            InitializeComponent();
        }
    }
}
