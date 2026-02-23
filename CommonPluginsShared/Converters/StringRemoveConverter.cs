using CommonPlayniteShared;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Removes a specified string (parameter) from the input string (value).
    /// </summary>
    public class StringRemoveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string valStr && parameter is string paramStr)
                {
                    return valStr.Replace(paramStr, string.Empty);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}