using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommonPluginsControls.Controls;
using CommonPluginsShared;
using LiveCharts;
using LiveCharts.Wpf;

namespace CommonPluginsControls.LiveChartsCommon
{
    /// <summary>
    /// Logique d'interaction pour CustomersTooltipForTime.xaml
    /// </summary>
    public partial class CustomerToolTipForTime : IChartTooltip
    {
        public TooltipSelectionMode? SelectionMode { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;


        #region Properties
        private TooltipData _data;
        public TooltipData Data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
                OnPropertyChanged("Data");
            }
        }

        public TextBlockWithIconMode Mode
        {
            get { return (TextBlockWithIconMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            nameof(Mode),
            typeof(TextBlockWithIconMode),
            typeof(CustomerToolTipForTime),
            new FrameworkPropertyMetadata(TextBlockWithIconMode.IconTextFirstWithText));

        public bool ShowIcon
        {
            get { return (bool)GetValue(ShowIconProperty); }
            set { SetValue(ShowIconProperty, value); }
        }

        public static readonly DependencyProperty ShowIconProperty = DependencyProperty.Register(
            nameof(ShowIcon),
            typeof(bool),
            typeof(CustomerToolTipForTime),
            new FrameworkPropertyMetadata(false));
        #endregion


        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public CustomerToolTipForTime()
        {
            InitializeComponent();

            DataContext = this;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (ShowIcon)
            {
                if (Mode == TextBlockWithIconMode.IconTextFirstOnly || Mode == TextBlockWithIconMode.IconTextFirstWithText || Mode == TextBlockWithIconMode.IconTextOnly)
                {
                    Mode = TextBlockWithIconMode.IconTextFirstWithText;
                }
                else if (Mode == TextBlockWithIconMode.IconFirstOnly || Mode == TextBlockWithIconMode.IconFirstWithText || Mode == TextBlockWithIconMode.IconOnly)
                {
                    Mode = TextBlockWithIconMode.IconFirstWithText;
                }
            }
            else
            {
                Mode = TextBlockWithIconMode.TextOnly;
            }
        }
    }
}
