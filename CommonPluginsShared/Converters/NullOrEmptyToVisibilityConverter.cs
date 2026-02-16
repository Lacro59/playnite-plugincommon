using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Converts null, empty strings, "0", or empty collections to Visibility.Collapsed.
    /// Otherwise returns Visibility.Visible.
    /// </summary>
    public class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            try
            {
                if (value is string stringValue)
                {
                    if (string.IsNullOrWhiteSpace(stringValue) || stringValue == "0" || stringValue.IndexOf("0 bytes", StringComparison.OrdinalIgnoreCase) > -1)
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
            catch (Exception)
            {
                // Fallback to Visible in case of error, or Collapsed depending on preference.
                // Log if necessary, but converters are noisy.
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}