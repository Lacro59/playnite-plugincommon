using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    public class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            if (value is string stringValue)
            {
                if (string.IsNullOrWhiteSpace(stringValue) || stringValue == "0" || stringValue.IndexOf("0 bytes") > -1)
                {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }

            if (value is ICollection collection)
            {
                if (collection.Count == 0)
                {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }

            if (value is int intValue)
            {
                if (intValue == 0)
                {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }

            if (value is long longValue)
            {
                if (longValue == 0)
                {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}