using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using Playnite.SDK;

namespace CommonPluginsControls.LiveChartsCommon
{
    /// <summary>
    /// Custom tooltip for LiveCharts 0.9.7 with direct Series binding.
    /// </summary>
    public partial class CustomerToolTipForLog : UserControl, IChartTooltip, INotifyPropertyChanged
    {
        public TooltipSelectionMode? SelectionMode { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private TooltipData _data;
        public TooltipData Data
        {
            get => _data;
            set
            {
                _data = value;

                if (_data != null)
                {
                    var sharedConverter = new SharedConverter();
                    DataTitle = sharedConverter.Convert(_data, typeof(string), null, CultureInfo.CurrentCulture)?.ToString();
                }
                else
                {
                    DataTitle = string.Empty;
                }

                OnPropertyChanged(nameof(Data));
                OnPropertyChanged(nameof(DataTitle));
            }
        }

        public string DataTitle { get; set; }

        public bool ShowTitle
        {
            get => (bool)GetValue(ShowTitleProperty);
            set => SetValue(ShowTitleProperty, value);
        }

        public static readonly DependencyProperty ShowTitleProperty = DependencyProperty.Register(
            nameof(ShowTitle), typeof(bool), typeof(CustomerToolTipForLog),
            new FrameworkPropertyMetadata(true));

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CustomerToolTipForLog()
        {
            InitializeComponent();
            DataContext = this;
        }
    }


    public class DashArrayFromTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string title = value as string;

            if (title != null)
            {
                string gpuTemp = ResourceProvider.GetString("LOCGameActivityGpuTemp");
                string cpuTemp = ResourceProvider.GetString("LOCGameActivityCpuTemp");
                string gpuPower = ResourceProvider.GetString("LOCGameActivityGpuPower");
                string cpuPower = ResourceProvider.GetString("LOCGameActivityCpuPower");
                string fps1Low = ResourceProvider.GetString("LOCGameActivityFps1PercentLow");
                string fps01Low = ResourceProvider.GetString("LOCGameActivityFps0Point1PercentLow");

                if (title.Contains(gpuTemp) || title.Contains(cpuTemp) || title.Contains(gpuPower) || title.Contains(cpuPower)
                    || title.Contains(fps1Low) || title.Contains(fps01Low))
                {
                    return new DoubleCollection { 4, 2 };
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}