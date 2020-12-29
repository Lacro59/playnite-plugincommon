using Playnite.SDK;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace CommonShared
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
                Common.LogError(ex, "CommonShared", $"Error on CompareValueConverter");
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
                Common.LogError(ex, "CommonShared", $"Error on VisibilityZeroConverter");
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
                Common.LogError(ex, "CommonShared", $"Error on LocalDateConverter");
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
                Common.LogError(ex, "CommonShared", $"Error on LocalDateYMConverter");
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
                Common.LogError(ex, "CommonShared", $"Error on LocalTimeConverter");
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
                Common.LogError(ex, "CommonShared", $"Error on LocalDateTimeConverter");
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
                ListBoxItem item = (ListBoxItem)value;
                ListBox listView = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
                int index = listView.ItemContainerGenerator.IndexFromContainer(item);
                return index.ToString();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "CommonShared", $"Error on IndexConverter");
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
}
