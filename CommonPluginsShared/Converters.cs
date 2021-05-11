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

namespace CommonPluginsShared
{
    public class CompareValueConverter : IMultiValueConverter
    {
        public static IResourceProvider resources = new ResourceProvider();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                int.TryParse(((string)values[0]).Replace("%", string.Empty).Replace("°", string.Empty), out int ValueData);
                int.TryParse((string)values[1], out int ValueControl);

                bool Enable = (bool)values[2];

                if (Enable)
                {
                    int.TryParse((string)parameter, out int parameterValue);
                    if (parameterValue == 0)
                    {
                        if (ValueData > ValueControl)
                        {
                            return resources.GetResource("TextBrush");
                        }
                        else
                        {
                            return Brushes.Orange;
                        }
                    }
                    else
                    {
                        if (ValueData < ValueControl)
                        {
                            return resources.GetResource("TextBrush");
                        }
                        else
                        {
                            return Brushes.Orange;
                        }
                    }
                }
                else
                {
                    return resources.GetResource("TextBrush");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return (Brushes)resources.GetResource("TextBrush");
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class VisibilityZeroConverter : IValueConverter
    {
        private static ILogger logger = LogManager.GetLogger();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value.ToString() == "0")
                {
                    if (parameter.ToString() == "1")
                    {
                        return Visibility.Collapsed;
                    }
                    else
                    {
                        return Visibility.Hidden;
                    }
                }
                else
                {
                    return Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class StringToBooleanConverter : IValueConverter
    {
        enum Parameters
        {
            Normal, Inverted
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var direction = parameter == null ? Parameters.Normal : (Parameters)Enum.Parse(typeof(Parameters), (string)parameter);
            if (direction == Parameters.Inverted)
            {
                return string.IsNullOrEmpty(value.ToString()) ? true : false;
            }
            else
            {
                return string.IsNullOrEmpty(value.ToString()) ? false : true;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LocalDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value != null && (DateTime)value != default(DateTime))
                {
                    return ((DateTime)value).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class LocalDateYMConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value != null && (DateTime)value != default(DateTime))
                {
                    string tmpDate = ((DateTime)value).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
                    string tmpDay = string.Empty;
                    string tmpDateShort = string.Empty;

                    tmpDay = ((DateTime)value).ToString("d");
                    Regex rgx = new Regex(@"[/\.\- ]" + tmpDay + "|" + tmpDay + @"[/\.\- ]");
                    tmpDateShort = rgx.Replace(tmpDate, string.Empty, 1);

                    if (tmpDateShort.Length == tmpDate.Length)
                    {
                        tmpDay = ((DateTime)value).ToString("dd");
                        rgx = new Regex(@"[/\.\- ]" + tmpDay + "|" + tmpDay + @"[/\.\- ]");
                        tmpDateShort = rgx.Replace(tmpDate, string.Empty, 1);
                    }

                    return tmpDateShort;
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class LocalTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value != null && (DateTime)value != default(DateTime))
                {
                    return ((DateTime)value).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern);
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class LocalDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value != null && (DateTime)value != default(DateTime))
                {
                    return ((DateTime)value).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern)
                        + " " + ((DateTime)value).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern);
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InvertedBoolenConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is ListBoxItem)
                {
                    ListBoxItem item = (ListBoxItem)value;
                    ListBox listView = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
                    int index = listView.ItemContainerGenerator.IndexFromContainer(item);
                    return index.ToString();
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
            throw new NotImplementedException();
        }
    }

    public class TwoBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool a = (bool)values[0];
            bool b = (bool)values[1];

            return a && b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class DefaultIconConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null || (value is string && ((string)value).IsNullOrEmpty()))
                {
                    if (ResourceProvider.GetResource("DefaultGameIcon") != null)
                    {
                        return ResourceProvider.GetResource("DefaultGameIcon");
                    }
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
            throw new NotImplementedException();
        }
    }


    public class ImageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is string && !((string)values[0]).IsNullOrEmpty() && File.Exists((string)values[0]))
            {
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


                string str = ImageSourceManager.GetImagePath((string)values[0]);


                if (((string)values[0]).EndsWith(".tga", StringComparison.OrdinalIgnoreCase))
                {
                    BitmapImage bitmapImage = BitmapExtensions.TgaToBitmap(str);

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
                    return BitmapExtensions.BitmapFromFile(str);
                }
                else
                {
                    return BitmapExtensions.BitmapFromFile(str, bitmapLoadProperties);
                }
            }

            return values[0];
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


    public class HeightToFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var height = (double)value;
            return .5 * height;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class ImageToGrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                str = ImageSourceManager.GetImagePath(str);
                BitmapImage tmpImg = BitmapExtensions.BitmapFromFile(str);
                return ImageTools.ConvertBitmapImage(tmpImg, ImageColor.Gray);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class VisibilityLessThanConverter : IValueConverter
    {
        private static ILogger logger = LogManager.GetLogger();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                double.TryParse(value.ToString(), out double valueDouble);
                double.TryParse(parameter.ToString(), out double parameterDouble);

                if (valueDouble > parameterDouble)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
