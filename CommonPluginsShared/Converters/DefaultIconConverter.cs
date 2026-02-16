using CommonPlayniteShared;
using Playnite.SDK;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Returns the default game icon if the input value (icon path) is missing or invalid.
    /// Checks if file exists, or tries to resolve it relative to Playnite database.
    /// </summary>
    public class DefaultIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null || (value is string strVal && strVal.IsNullOrEmpty()))
                {
                    return GetDefaultIcon();
                }

                if (value is string path)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }

                    string fullPath = API.Instance?.Database?.GetFullFilePath(path);
                    if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                    {
                        return fullPath;
                    }

                    return GetDefaultIcon();
                }

                return value;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return value;
        }

        private object GetDefaultIcon()
        {
            if (ResourceProvider.GetResource("DefaultGameIcon") != null)
            {
                return ResourceProvider.GetResource("DefaultGameIcon");
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}