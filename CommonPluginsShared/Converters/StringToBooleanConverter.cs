using CommonPlayniteShared;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Converts a string to boolean based on if it's null/empty.
    /// Supports parameter "Inverted" or "Normal".
    /// </summary>
    public class StringToBooleanConverter : IValueConverter
    {
        enum Parameters
        {
            Normal, Inverted
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                Parameters direction = Parameters.Normal;
                if (parameter != null)
                {
                    try
                    {
                        direction = (Parameters)Enum.Parse(typeof(Parameters), parameter.ToString());
                    }
                    catch
                    {
                        // Ignore parameter parsing error, default to Normal
                    }
                }

                string stringValue = value?.ToString();
                bool isNullOrEmpty = string.IsNullOrEmpty(stringValue);

                if (direction == Parameters.Inverted)
                {
                    return isNullOrEmpty;
                }
                else
                {
                    return !isNullOrEmpty;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}