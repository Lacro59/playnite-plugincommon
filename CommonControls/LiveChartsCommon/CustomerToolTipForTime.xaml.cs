using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using LiveCharts;
using LiveCharts.Wpf;

namespace PluginCommon.LiveChartsCommon
{
    /// <summary>
    /// Logique d'interaction pour CustomersTooltipForTime.xaml
    /// </summary>
    public partial class CustomerToolTipForTime : IChartTooltip
    {
        private TooltipData _data;

        public static readonly DependencyProperty ShowIconProperty = DependencyProperty.Register(
            "ShowIcon", typeof(Boolean), typeof(CustomerToolTipForTime), new PropertyMetadata(false));

        public static bool _ShowIcon = false;

        public bool ShowIcon
        {
            get { return (bool)GetValue(ShowIconProperty); }
            set { SetValue(ShowIconProperty, value); }
        }

        public CustomerToolTipForTime()
        {
            InitializeComponent();

            //LiveCharts will inject the tooltip data in the Data property
            //your job is only to display this data as required

            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TooltipData Data
        {
            get
            {
                _ShowIcon = ShowIcon;
                return _data;
            }
            set
            {
                _data = value;
                OnPropertyChanged("Data");
            }
        }

        public TooltipSelectionMode? SelectionMode { get; set; }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class ShowIconConverterTime : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string Result = "";
            if (TransformIcon.Get((string)value).Length == 1 && CustomerToolTipForTime._ShowIcon)
            {
                Result = TransformIcon.Get((string)value);
            }

            return Result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return "";
        }
    }
}
