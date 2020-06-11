using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PluginCommon
{
    public class CompareValueConverter : IMultiValueConverter
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int ValueData = int.Parse(((string)values[0]).Replace("%", "").Replace("°", ""));
            int ValueControl = int.Parse((string)values[1]);
            bool Enable = (bool)values[2];

            if (Enable)
            {
                if (int.Parse((string)parameter) == 0)
                {
                    if (ValueData > ValueControl)
                    {
                        return Brushes.White;
                    }
                    else
                    {
                        return Brushes.Orange;
                    }
                }
                else
                {
                    if (ValueData < ValueControl)
                    {
                        return Brushes.White;
                    }
                    else
                    {
                        return Brushes.Orange;
                    }
                }
            }
            else
            {
                return Brushes.White;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
