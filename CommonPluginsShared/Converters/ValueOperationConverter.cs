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
                if (values[0] is double && values[1] is double)
                {
                    if (parameter.ToString() == "-")
                    {
                        return (double)values[0] - (double)values[1];
                    }
                    else
                    {
                        return (double)values[0] + (double)values[1];
                    }
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
