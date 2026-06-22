using Playnite.SDK;
using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace CommonPluginsShared.Converters
{
    /// <summary>
    /// Converts Dock enum to localized string.
    /// </summary>
    public class DockToStringConverter : MarkupExtension, IValueConverter
    {
        public static string GetString(Dock value)
        {
            switch (value)
            {
                case Dock.Left:
                    return ResourceProvider.GetString("LOCDockLeft");
                case Dock.Right:
                    return ResourceProvider.GetString("LOCDockRight");
                case Dock.Top:
                    return ResourceProvider.GetString("LOCDockTop");
                case Dock.Bottom:
                    return ResourceProvider.GetString("LOCDockBottom");
            }

            return "<UnknownDockMode>";
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Dock dock)
            {
                return GetString(dock);
            }
            return "<UnknownDockMode>";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}