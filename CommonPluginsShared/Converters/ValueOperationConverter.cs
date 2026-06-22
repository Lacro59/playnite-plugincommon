using CommonPlayniteShared;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Performs basic arithmetic operations (+ or -) on two values.
    /// Default is addition. Parameter "-" triggers subtraction (value1 - value2).
    /// </summary>
    internal class ValueOperationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Length < 2)
                {
                    return double.NaN;
                }

                double val1 = 0;
                double val2 = 0;

                try
                {
                    val1 = System.Convert.ToDouble(values[0], culture);
                    val2 = System.Convert.ToDouble(values[1], culture);
                }
                catch
                {
                    // If conversion fails, return NaN
                    return double.NaN;
                }

                if (parameter?.ToString() == "-")
                {
                    return val1 - val2;
                }
                else
                {
                    return val1 + val2;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return double.NaN;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}