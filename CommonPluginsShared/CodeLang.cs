using Playnite.SDK;
using System;
using System.Collections.Generic;

namespace CommonPluginsShared
{
    /// <summary>
    /// Provides helper methods to convert Playnite language codes
    /// into platform specific language identifiers (Steam, GOG, etc.).
    /// </summary>
    public class CodeLang
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        // Static dictionary mapping Playnite language codes to Steam language codes
        private static readonly Dictionary<string, string> _steamLangMap = new Dictionary<string, string>
        {
            { "ar_SA", "arabic" },
            { "bg_BG", "bulgarian" },
            { "cs_CZ", "czech" },
            { "da_DK", "danish" },
            { "de_DE", "german" },
            { "el_GR", "greek" },
            { "en_US", "english" },
            { "es_419", "latam" },
            { "es_ES", "spanish" },
            { "fi_FI", "finnish" },
            { "fr_FR", "french" },
            { "hu_HU", "hungarian" },
            { "id_ID", "indonesian" },
            { "it_IT", "italian" },
            { "ja_JP", "japanese" },
            { "ko_KR", "koreana" },
            { "nl_NL", "dutch" },
            { "no_NO", "norwegian" },
            { "pl_PL", "polish" },
            { "pt_BR", "brazilian" },
            { "pt_PT", "portuguese" },
            { "ro_RO", "romanian" },
            { "ru_RU", "russian" },
            { "sv_SE", "swedish" },
            { "th_TH", "thai" },
            { "tr_TR", "turkish" },
            { "uk_UA", "ukrainian" },
            { "vi_VN", "vietnamese" },
            { "zh_CN", "schinese" },
            { "zh_TW", "tchinese" },
        };

        /// <summary>
        /// Gets the mapping between Playnite language codes and Steam language codes.
        /// </summary>
        private static Dictionary<string, string> SteamLangMap => _steamLangMap;


        /// <summary>
        /// Extracts the country/region prefix from a Playnite language code.
        /// </summary>
        /// <param name="playniteLanguage">Playnite language code (for example, "en_US").</param>
        /// <returns>
        /// The first two characters of the normalized language code, or "en" if the value is invalid.
        /// </returns>
        public static string GetCountryFromFirst(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);
            return playniteLanguage.Length >= 2 ? playniteLanguage.Substring(0, 2) : "en";
        }

        /// <summary>
        /// Extracts the country/region suffix from a Playnite language code.
        /// </summary>
        /// <param name="playniteLanguage">Playnite language code (for example, "en_US").</param>
        /// <returns>
        /// The last two characters of the normalized language code, or "US" if the value is invalid.
        /// </returns>
        public static string GetCountryFromLast(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);
            return playniteLanguage.Length >= 2 ? playniteLanguage.Substring(playniteLanguage.Length - 2) : "US";
        }

        // -------------------------
        // Steam
        // -------------------------

        /// <summary>
        /// Converts a Playnite language code to its Steam language identifier.
        /// </summary>
        /// <param name="playniteLanguage">Playnite language code.</param>
        /// <returns>
        /// The corresponding Steam language code if mapped; otherwise, "english".
        /// </returns>
        public static string GetSteamLang(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);
            return SteamLangMap.TryGetValue(playniteLanguage, out string steamLang) ? steamLang : "english";
        }

        // -------------------------
        // GOG
        // -------------------------

        /// <summary>
        /// Converts a Playnite language code to its GOG language identifier.
        /// </summary>
        /// <param name="playniteLanguage">Playnite language code.</param>
        /// <returns>
        /// The corresponding GOG language code if supported; otherwise, "en".
        /// </returns>
        public static string GetGogLang(string playniteLanguage)
        {
            if (playniteLanguage == "zh_CN") return "zh-Hans";
            if (playniteLanguage == "zh_TW") return "zh-Hant";

            string shortLang = GetShortLang(playniteLanguage);
            string[] arrayLang = { "de", "en", "es", "fr", "ja", "ko", "zh-Hans", "zh-Hant" };
            return arrayLang.ContainsString(shortLang, StringComparison.OrdinalIgnoreCase) ? shortLang : "en";
        }

        // -------------------------
        // Genshin Impact
        // -------------------------

        /// <summary>
        /// Converts a Playnite language code to its Genshin Impact language identifier.
        /// </summary>
        /// <param name="playniteLanguage">Playnite language code.</param>
        /// <returns>
        /// The corresponding Genshin Impact language code if supported; otherwise, "en".
        /// </returns>
        public static string GetGenshinLang(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);
            switch (playniteLanguage)
            {
                case "zh_CN": return "chs";
                case "zh_TW": return "cht";
            }

            string shortLang = GetShortLang(playniteLanguage);
            string[] arrayLang = { "chs", "cht", "de", "en", "es", "fr", "id", "jp", "kr", "pt", "ru", "th", "vi" };
            return arrayLang.ContainsString(shortLang, StringComparison.OrdinalIgnoreCase) ? shortLang : "en";
        }

        // -------------------------
        // Wuthering Waves
        // -------------------------

        /// <summary>
        /// Converts a Playnite language code to its Wuthering Waves language identifier.
        /// </summary>
        /// <param name="playniteLanguage">Playnite language code.</param>
        /// <returns>
        /// The corresponding Wuthering Waves language code if supported; otherwise, "en".
        /// </returns>
        public static string GetWuWaLang(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);

            if (playniteLanguage == "zh_CN") return "zh-Hans";
            if (playniteLanguage == "zh_TW") return "zh-Hant";

            string shortLang = GetShortLang(playniteLanguage);
            string[] arrayLang = { "de", "en", "es", "fr", "ja", "ko", "zh-Hans", "zh-Hant" };
            return arrayLang.ContainsString(shortLang, StringComparison.OrdinalIgnoreCase) ? shortLang : "en";
        }

        // -------------------------
        // Origin
        // -------------------------

        /// <summary>
        /// Converts a Playnite language code to an EA App/Origin language identifier.
        /// </summary>
        /// <param name="playniteLanguage">Playnite language code.</param>
        /// <returns>The normalized language code.</returns>
        public static string GetEaLang(string playniteLanguage)
        {
            return Normalize(playniteLanguage);
        }

        // -------------------------
        // Epic Games
        // -------------------------

        /// <summary>
        /// Converts a Playnite language code to an Epic Games language identifier.
        /// </summary>
        /// <param name="playniteLanguage">Playnite language code.</param>
        /// <returns>The normalized language code with underscores replaced by hyphens.</returns>
        public static string GetEpicLang(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);
            return playniteLanguage.Replace("_", "-");
        }

        // -------------------------
        // Xbox
        // -------------------------

        /// <summary>
        /// Converts a Playnite language code to an Xbox language identifier.
        /// </summary>
        /// <param name="playniteLanguage">Playnite language code.</param>
        /// <returns>The normalized language code with underscores replaced by hyphens.</returns>
        public static string GetXboxLang(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);
            return playniteLanguage.Replace("_", "-");
        }

        // -------------------------
        // Helpers
        // -------------------------

        /// <summary>
        /// Normalizes a raw language value into a standard Playnite language code.
        /// </summary>
        /// <param name="lang">Input language value (for example, "english" or "en_US").</param>
        /// <returns>
        /// The normalized Playnite language code (for example, "en_US"). If the value is
        /// null or empty, "en_US" is returned and a warning is logged.
        /// </returns>
        private static string Normalize(string lang)
        {
            if (lang.IsNullOrEmpty())
            {
                Logger.Warn($"lang is null or empty");
                return "en_US";
            }
            return lang == "english" ? "en_US" : lang;
        }

        /// <summary>
        /// Gets the short language code from a Playnite language code.
        /// </summary>
        /// <param name="playniteLanguage">Playnite language code (for example, "en_US").</param>
        /// <returns>The lower-cased primary language tag (for example, "en").</returns>
        private static string GetShortLang(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);
            string[] parts = playniteLanguage.Split('_');
            return parts[0].ToLower();
        }
    }
}