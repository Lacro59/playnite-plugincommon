using CommonPlayniteShared;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Converts a DateTime to the local short date and time string representation.
    /// </summary>
    public class LocalDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is DateTime dt && dt != default)
                {
                    DateTime localDt = dt.ToLocalTime();
                    return $"{localDt.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern)} {localDt.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern)}";
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}