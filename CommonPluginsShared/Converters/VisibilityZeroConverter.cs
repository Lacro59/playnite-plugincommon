using CommonPlayniteShared;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Hides visibility if value is "0".
    /// Parameter "1" uses Collapsed, otherwise Hidden.
    /// </summary>
    public class VisibilityZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string stringValue = value?.ToString();

                if (stringValue == "0")
                {
                    string paramString = parameter?.ToString();
                    if (paramString == "1")
                    {
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
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}