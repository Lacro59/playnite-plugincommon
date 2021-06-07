using CommonPlayniteShared;
using Playnite.SDK;
using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CommonPluginsShared.Converters
{
    public class ImageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is string && !((string)values[0]).IsNullOrEmpty() && File.Exists((string)values[0]))
            {
                if (((string)values[0]).EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                {
                    return values[0];
                }

                BitmapLoadProperties bitmapLoadProperties = null;
                if (parameter is string && (string)parameter == "1")
                {
                    bitmapLoadProperties = new BitmapLoadProperties(100, 0)
                    {
                        Source = (string)values[0]
                    };
                }
                if (parameter is string && (string)parameter == "2")
                {
                    bitmapLoadProperties = new BitmapLoadProperties(200, 0)
                    {
                        Source = (string)values[0]
                    };
                }
                if (parameter is string && (string)parameter == "0")
                {
                    double ActualHeight = (double)values[1];

                    if (ActualHeight > 200)
                    {
                        bitmapLoadProperties = new BitmapLoadProperties((int)ActualHeight, 0)
                        {
                            Source = (string)values[0]
                        };
                    }
                    else
                    {
                        bitmapLoadProperties = new BitmapLoadProperties(200, 0)
                        {
                            Source = (string)values[0]
                        };
                    }
                }


                if (((string)values[0]).EndsWith(".tga", StringComparison.OrdinalIgnoreCase))
                {
                    BitmapImage bitmapImage = BitmapExtensions.TgaToBitmap((string)values[0]);

                    if (bitmapLoadProperties == null)
                    {
                        return bitmapImage;
                    }
                    else
                    {
                        return bitmapImage.GetClone(bitmapLoadProperties);
                    }
                }


                if (bitmapLoadProperties == null)
                {
                    return BitmapExtensions.BitmapFromFile((string)values[0]);
                }
                else
                {
                    return BitmapExtensions.BitmapFromFile((string)values[0], bitmapLoadProperties);
                }
            }

            return values[0];
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
