using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    class ValueOperationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (parameter.ToString() == "-")
                {
                    return double.Parse(values[0].ToString()) - double.Parse(values[1].ToString());
                }
                else
                {
                    return double.Parse(values[0].ToString()) + double.Parse(values[1].ToString());
                }
            }
            catch
            {
                
            }

            return double.NaN;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
