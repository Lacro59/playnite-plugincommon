using Playnite.SDK;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;


namespace PluginCommon
{
    public class CompareValueConverter : IMultiValueConverter
    {
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

    public class VisibilityZeroConverter : IValueConverter
    {
        private static ILogger logger = LogManager.GetLogger();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.ToString() == "0")
            {
                if (parameter.ToString() == "1") {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Hidden;
                }
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class StringToBooleanConverter : IValueConverter
    {
        enum Parameters
        {
            Normal, Inverted
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var direction = parameter == null ? Parameters.Normal : (Parameters)Enum.Parse(typeof(Parameters), (string)parameter);
            if (direction == Parameters.Inverted)
            {
                return string.IsNullOrEmpty(value as string) ? true : false;
            }
            else
            {
                return string.IsNullOrEmpty(value as string) ? false : true;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
