using Playnite.SDK;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace PluginCommon
{
    public class CompareValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int ValueData = 0;
            int.TryParse(((string)values[0]).Replace("%", string.Empty).Replace("°", string.Empty), out ValueData);

            int ValueControl = 0;
            int.TryParse((string)values[1], out ValueControl);

            bool Enable = (bool)values[2];

            if (Enable)
            {
                int parameterValue = 0;
                int.TryParse((string)parameter, out parameterValue);
                if (parameterValue == 0)
                {
                    if (ValueData > ValueControl)
                    {
                        return Brushes.White;
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
                        return Brushes.White;
                    }
                    else
                    {
                        return Brushes.Orange;
                    }
                }
            }
            else
            {
                return Brushes.White;
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
            if (value.ToString() == "0")
            {
                if (parameter.ToString() == "1") {
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
                return string.IsNullOrEmpty(value as string) ? true : false;
            }
            else
            {
                return string.IsNullOrEmpty(value as string) ? false : true;
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
            if (value != null && (DateTime)value != default(DateTime))
            {
                return ((DateTime)value).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
            }
            else
            {
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
            if (value != null && (DateTime)value != default(DateTime))
            {
                string tmpDate = ((DateTime)value).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
                string tmpDay = string.Empty;
                string tmpDateShort = string.Empty;

                tmpDay = ((DateTime)value).ToString("d");
                Regex rgx = new Regex(@"[/\.\- ]" + tmpDay  + "|" + tmpDay + @"[/\.\- ]");
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class LocalTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class LocalDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && (DateTime)value != default(DateTime)) {
                return ((DateTime)value).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern)
                    + " " + ((DateTime)value).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern);
            }
            else {
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
            return !(bool)value;
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
            ListBoxItem item = (ListBoxItem)value;
            ListBox listView = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
            int index = listView.ItemContainerGenerator.IndexFromContainer(item);
            return index.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
