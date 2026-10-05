using CommonPlayniteShared.Common;
using Playnite.SDK;
using System;

namespace CommonPlayniteShared
{
    /// <summary>
    /// Date helpers aligned on Playnite host display rules.
    /// Ported from Playnite <c>DateTimes</c> (DateTime path only).
    /// </summary>
    public static class DateTimes
    {
        /// <summary>
        /// Abstraction for current date/time (tests can substitute).
        /// </summary>
        public interface IDateTimes
        {
            /// <summary>Gets the current date and time.</summary>
            DateTime Now { get; }

            /// <summary>Gets today's date.</summary>
            DateTime Today { get; }
        }

        /// <summary>
        /// Default provider using <see cref="DateTime.Now"/> / <see cref="DateTime.Today"/>.
        /// </summary>
        public class DefaultDateProvider : IDateTimes
        {
            /// <inheritdoc />
            public DateTime Now => DateTime.Now;

            /// <inheritdoc />
            public DateTime Today => DateTime.Today;
        }

        /// <summary>
        /// Temporarily replaces the date provider; restores the default on dispose.
        /// </summary>
        public class TempDateTime : IDisposable
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TempDateTime"/> class.
            /// </summary>
            /// <param name="customDates">Custom date provider.</param>
            public TempDateTime(IDateTimes customDates)
            {
                dateProvider = customDates;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                dateProvider = defaultDateProvider;
            }
        }

        private static readonly IDateTimes defaultDateProvider = new DefaultDateProvider();
        private static IDateTimes dateProvider = defaultDateProvider;

        /// <summary>Gets the current date and time from the active provider.</summary>
        public static DateTime Now => dateProvider.Now;

        /// <summary>Gets today's date from the active provider.</summary>
        public static DateTime Today => dateProvider.Today;

        /// <summary>
        /// Uses a custom date provider until the returned disposable is disposed.
        /// </summary>
        /// <param name="dates">Custom provider.</param>
        /// <returns>Disposable that restores the default provider.</returns>
        public static IDisposable UseCustomDates(IDateTimes dates)
        {
            return new TempDateTime(dates);
        }

        /// <summary>
        /// Formats a date for UI using Playnite date formatting options.
        /// </summary>
        /// <param name="date">Date to format.</param>
        /// <param name="options">Formatting options; when null, uses <see cref="Constants.DateUiFormat"/>.</param>
        /// <returns>Localized relative label or formatted absolute date.</returns>
        public static string ToDisplayString(this DateTime date, DateFormattingOptions options = null)
        {
            try
            {
                if (options == null)
                {
                    return date.ToString(Constants.DateUiFormat);
                }

                if (options.PastWeekRelativeFormat)
                {
                    DateTime today = Today;
                    double dayDiff = (today - date.Date).TotalDays;

                    if (dayDiff == 0)
                    {
                        return ResourceProvider.GetString(LOC.Today);
                    }

                    if (dayDiff == 1)
                    {
                        return ResourceProvider.GetString(LOC.Yesterday);
                    }

                    if (dayDiff > 1 && dayDiff < 7)
                    {
                        switch (date.DayOfWeek)
                        {
                            case DayOfWeek.Sunday:
                                return ResourceProvider.GetString(LOC.Sunday);
                            case DayOfWeek.Monday:
                                return ResourceProvider.GetString(LOC.Monday);
                            case DayOfWeek.Tuesday:
                                return ResourceProvider.GetString(LOC.Tuesday);
                            case DayOfWeek.Wednesday:
                                return ResourceProvider.GetString(LOC.Wednesday);
                            case DayOfWeek.Thursday:
                                return ResourceProvider.GetString(LOC.Thursday);
                            case DayOfWeek.Friday:
                                return ResourceProvider.GetString(LOC.Friday);
                            case DayOfWeek.Saturday:
                                return ResourceProvider.GetString(LOC.Saturday);
                        }
                    }
                }

                return date.ToString(options.Format ?? Constants.DateUiFormat);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Rare calendar range failures (same guard as Playnite host).
                return "unsupported";
            }
        }
    }
}
