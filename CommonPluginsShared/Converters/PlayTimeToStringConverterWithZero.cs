using CommonPlayniteShared;
using Playnite.SDK;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Converts play time (seconds) to a formatted string.
    /// Handles 0 values explicitly.
    /// </summary>
    public class PlayTimeToStringConverterWithZero : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                PlayTimeFormat playTimeFormat = PlayTimeFormat.DefaultFormat;
                if (parameter != null && parameter is PlayTimeFormat format)
                {
                    playTimeFormat = format;
                }

                if (value == null)
                {
                    return string.Format(ResourceProvider.GetString("LOCPlayedMinutes"), 0);
                }

                ulong seconds = 0;
                try
                {
                    seconds = System.Convert.ToUInt64(value);
                }
                catch
                {
                    return string.Empty;
                }

                if (seconds == 0)
                {
                    return string.Format(ResourceProvider.GetString("LOCPlayedMinutes"), 0);
                }

                var time = TimeSpan.FromSeconds(seconds);

                switch (playTimeFormat)
                {
                    case PlayTimeFormat.DefaultFormat:
                        if (time.TotalSeconds < 60)
                        {
                            return string.Format(ResourceProvider.GetString("LOCPlayedSeconds"), time.Seconds);
                        }
                        else if (time.TotalHours < 1)
                        {
                            return string.Format(ResourceProvider.GetString("LOCPlayedMinutes"), time.Minutes);
                        }
                        else
                        {
                            return string.Format(ResourceProvider.GetString("LOCPlayedHours"), Math.Floor(time.TotalHours), time.Minutes);
                        }

                    case PlayTimeFormat.OnlyHour:
                        return string.Format(ResourceProvider.GetString("LOCPlayedHoursOnly"), time.TotalHours.ToString("0.##"));

                    case PlayTimeFormat.RoundHour:
                        return string.Format(ResourceProvider.GetString("LOCPlayedHoursOnly"), Math.Round(time.TotalHours * 2, MidpointRounding.AwayFromZero) / 2);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public enum PlayTimeFormat
    {
        OnlyHour, RoundHour, DefaultFormat
    }
}