using CommonPlayniteShared;
using Playnite.SDK;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CommonPluginsShared.Converters
{
    public class CompareValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Length < 2)
                {
                    return ResourceProvider.GetResource("TextBrush");
                }

                // Handle string cleaning and parsing
                string val0 = values[0]?.ToString()?.Replace("%", string.Empty)?.Replace("°", string.Empty);
                string val1 = values[1]?.ToString();

                if (!int.TryParse(val0, out int valueData) || !int.TryParse(val1, out int valueControl))
                {
                    return ResourceProvider.GetResource("TextBrush");
                }

                bool enable = false;
                if (values.Length > 2 && values[2] is bool boolValue)
                {
                    enable = boolValue;
                }

                if (enable)
                {
                    int.TryParse(parameter?.ToString(), out int parameterValue);

                    if (parameterValue == 0)
                    {
                        // Higher is better (or standard check)
                        if (valueData > valueControl)
                        {
                            return ResourceProvider.GetResource("TextBrush");
                        }
                        return Brushes.Orange;
                    }
                    else
                    {
                        // Lower is better
                        if (valueData < valueControl)
                        {
                            return ResourceProvider.GetResource("TextBrush");
                        }
                        return Brushes.Orange;
                    }
                }

                return ResourceProvider.GetResource("TextBrush");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return ResourceProvider.GetResource("TextBrush");
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}