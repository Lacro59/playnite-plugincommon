using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Returns a font icon based on media file extension (Video or Image).
    /// </summary>
    public class GetMediaTypeConverter : IValueConverter
    {
        private static readonly HashSet<string> VideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mkv", ".mp4", ".avi", ".webm"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string strValue = value as string;
                if (!string.IsNullOrEmpty(strValue))
                {
                    string ext = System.IO.Path.GetExtension(strValue);
                    if (string.IsNullOrEmpty(ext))
                    {
                        return "\ueb16"; // Image icon?
                    }

                    if (VideoExtensions.Contains(ext))
                    {
                        return "\ueb13"; // Video icon?
                    }
                    if (ext.Equals(".webp", StringComparison.OrdinalIgnoreCase))
                    {
                        return "\ueb16 \ueb13"; // Both?
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