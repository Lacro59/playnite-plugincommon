using CommonPlayniteShared;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Converts a DateTime to a Year-Month string based on the current culture's short date pattern.
    /// tries to remove the day part from the pattern.
    /// </summary>
    public class LocalDateYMConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is DateTime dt && dt != default)
                {
                    string shortDatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                    string[] result = Regex.Split(shortDatePattern, @"[/\.\- ]");
                    string ymDatePattern = string.Empty;

                    foreach (string str in result)
                    {
                        if (!str.IndexOf("d", StringComparison.InvariantCultureIgnoreCase).Equals(-1))
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(ymDatePattern))
                        {
                            ymDatePattern = str;
                        }
                        else
                        {
                            ymDatePattern += CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator + str;
                        }
                    }

                    return dt.ToLocalTime().ToString(ymDatePattern);
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