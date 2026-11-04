using Playnite.SDK;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace CommonPlayniteShared.Converters
{
    /// <summary>
    /// Converts a last-played <see cref="DateTime"/> using Playnite date formatting options.
    /// Ported from Playnite <c>DateTimeToLastPlayedConverter</c>.
    /// When <c>parameter</c> is not a <see cref="DateFormattingOptions"/>,
    /// uses <see cref="PlayniteDateFormats.LastPlayed"/> from Playnite config.json.
    /// Values are converted to local time before formatting (plugin session timestamps are UTC).
    /// </summary>
    public class DateTimeToLastPlayedConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Shared instance for code usage.
        /// </summary>
        public static DateTimeToLastPlayedConverter Instance { get; } = new DateTimeToLastPlayedConverter();

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? lastPlayed = null;
            if (value is DateTime dateTime)
            {
                if (dateTime == default(DateTime))
                {
                    return string.Empty;
                }

                lastPlayed = dateTime.ToLocalTime();
            }
            else if (value == null)
            {
                return ResourceProvider.GetString(LOC.Never);
            }

            if (lastPlayed == null)
            {
                return ResourceProvider.GetString(LOC.Never);
            }

            if (parameter is DateFormattingOptions options)
            {
                return lastPlayed.Value.ToDisplayString(options);
            }

            return lastPlayed.Value.ToDisplayString(PlayniteDateFormats.LastPlayed);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
