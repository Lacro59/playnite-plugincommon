using CommonPlayniteShared;
using Playnite.SDK;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Converts a DateTime to a relative local date label (today, yesterday, or N days ago).
    /// </summary>
    public class LocalDateRelativeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (!(value is DateTime dt) || dt == default(DateTime))
                {
                    return string.Empty;
                }

                DateTime localDate = dt.ToLocalTime().Date;
                DateTime today = DateTime.Now.Date;
                int dayOffset = (today - localDate).Days;

                if (dayOffset == 0)
                {
                    return ResourceProvider.GetString("LOCToday");
                }

                if (dayOffset == 1)
                {
                    return ResourceProvider.GetString("LOCYesterday");
                }

                if (dayOffset > 1)
                {
                    string format = ResourceProvider.GetString("LOCCommonDaysAgo");
                    return string.Format(CultureInfo.CurrentCulture, format, dayOffset);
                }

                // Future date: fall back to short local date.
                return localDate.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
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
