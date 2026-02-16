using CommonPlayniteShared;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Converts a height value to a font size (returns 50% of the input value).
    /// </summary>
    public class HeightToFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                {
                    return 10.0; // Default size
                }

                double height = System.Convert.ToDouble(value, culture);
                return 0.5 * height;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return 10.0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}