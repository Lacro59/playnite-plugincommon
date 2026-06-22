using CommonPlayniteShared;
using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Checks if a list contains more than one item.
    /// </summary>
    public class ListMoreOneBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is IList list)
                {
                    return list.Count > 1;
                }

                return false;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}