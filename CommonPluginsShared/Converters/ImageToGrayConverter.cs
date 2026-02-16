using CommonPlayniteShared;
using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Converts an image path to a grayscale BitmapImage.
    /// </summary>
    public class ImageToGrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string str && !str.IsNullOrEmpty())
                {
                    string imagePath = ImageSourceManagerPlugin.GetImagePath(str);
                    BitmapImage tmpImg = BitmapExtensions.BitmapFromFile(imagePath);
                    return ImageTools.ConvertBitmapImage(tmpImg, ImageColor.Gray);
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