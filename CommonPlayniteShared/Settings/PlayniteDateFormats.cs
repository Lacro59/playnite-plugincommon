using CommonPlayniteShared.Common;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.IO;

namespace CommonPlayniteShared
{
    /// <summary>
    /// Cached Playnite date-format options read from <c>config.json</c>.
    /// </summary>
    public static class PlayniteDateFormats
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static readonly object syncRoot = new object();
        private static bool loaded;
        private static DateFormattingOptions lastPlayed;

        /// <summary>
        /// Gets the cached <c>DateTimeFormatLastPlayed</c> options (loads on first access if needed).
        /// </summary>
        public static DateFormattingOptions LastPlayed
        {
            get
            {
                EnsureLoaded();
                return lastPlayed;
            }
        }

        /// <summary>
        /// Loads (or reloads) date format options from Playnite <c>config.json</c>.
        /// </summary>
        public static void Load()
        {
            lock (syncRoot)
            {
                LoadCore();
            }
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            lock (syncRoot)
            {
                if (!loaded)
                {
                    LoadCore();
                }
            }
        }

        private static void LoadCore()
        {
            // Playnite default for Last Played: format "d", relative past week enabled.
            lastPlayed = new DateFormattingOptions(Constants.DefaultDateTimeFormat, true);

            try
            {
                string configPath = PlaynitePaths.ConfigFilePath;
                if (!File.Exists(configPath))
                {
                    logger.Warn($"Playnite config not found: {configPath}");
                    loaded = true;
                    return;
                }

                PlayniteConfigDateFormatsSnippet config = Serialization.FromJsonFile<PlayniteConfigDateFormatsSnippet>(configPath);
                if (config != null && config.DateTimeFormatLastPlayed != null)
                {
                    lastPlayed = config.DateTimeFormatLastPlayed;
                    if (string.IsNullOrEmpty(lastPlayed.Format))
                    {
                        lastPlayed.Format = Constants.DefaultDateTimeFormat;
                    }
                }

                logger.Debug(
                    $"DateTimeFormatLastPlayed loaded: Format={lastPlayed.Format}, PastWeekRelativeFormat={lastPlayed.PastWeekRelativeFormat}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to load DateTimeFormatLastPlayed from Playnite config");
            }

            loaded = true;
        }

        /// <summary>
        /// Minimal DTO for deserializing date-format keys from Playnite config.json.
        /// </summary>
        private class PlayniteConfigDateFormatsSnippet
        {
            public DateFormattingOptions DateTimeFormatLastPlayed { get; set; }
        }
    }
}
