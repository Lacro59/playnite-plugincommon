using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Converts an Enum value to a Boolean based on a parameter.
    /// Used for RadioButtons binding to Enum properties.
    /// </summary>
    public class EnumToBooleanConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(parameter) ?? false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : Binding.DoNothing;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}