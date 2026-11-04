namespace CommonPlayniteShared
{
    /// <summary>
    /// Playnite date display options (format string + past-week relative).
    /// Ported from Playnite <c>PlayniteSettings.DateFormattingOptions</c> (DTO without UI notifications).
    /// </summary>
    public class DateFormattingOptions
    {
        /// <summary>
        /// Gets or sets the .NET date format string (e.g. <c>d</c>).
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets whether dates within the past week use relative labels.
        /// </summary>
        public bool PastWeekRelativeFormat { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateFormattingOptions"/> class.
        /// </summary>
        public DateFormattingOptions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateFormattingOptions"/> class.
        /// </summary>
        /// <param name="format">Date format string.</param>
        /// <param name="pastWeekRelativeFormat">Whether to use relative labels for the past week.</param>
        public DateFormattingOptions(string format, bool pastWeekRelativeFormat)
        {
            Format = format;
            PastWeekRelativeFormat = pastWeekRelativeFormat;
        }
    }
}
