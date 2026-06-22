using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Converts two boolean values to Visibility.Visible if both are true, otherwise Visibility.Collapsed.
    /// </summary>
    public class TwoBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
            {
                return Visibility.Collapsed;
            }

            if (values[0] is bool a && values[1] is bool b)
            {
                return a && b ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}