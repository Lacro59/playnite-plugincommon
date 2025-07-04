using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    public class GetMediaTypeConverter : IValueConverter
    {
        private static readonly HashSet<string> VideoExtensions = new HashSet<string>
        {
            ".mkv", ".mp4", ".avi", ".webm"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var strValue = value as string;
                if (!string.IsNullOrEmpty(strValue))
                {
                    var ext = System.IO.Path.GetExtension(strValue)?.ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext))
                    {
                        return "\ueb16";
                    }

                    if (VideoExtensions.Contains(ext))
                    {
                        return "\ueb13";
                    }
                    if (ext == ".webp")
                    {
                        return "\ueb16 \ueb13";
                    }

                    return "\ueb16";
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