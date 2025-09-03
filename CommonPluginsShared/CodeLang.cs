using Playnite.SDK;
using System;
using System.Collections.Generic;

namespace CommonPluginsShared
{
    public class CodeLang
    {
        private static ILogger Logger => LogManager.GetLogger();

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

        private static Dictionary<string, string> SteamLangMap => _steamLangMap;


        /// <summary>
        /// Extracts country.
        /// Returns first two characters.
        /// </summary>
        public static string GetCountryFromFirst(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);
            return playniteLanguage.Length >= 2 ? playniteLanguage.Substring(0, 2) : "en";
        }

        /// <summary>
        /// Extracts country.
        /// Returns last two characters.
        /// </summary>
        public static string GetCountryFromLast(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);
            return playniteLanguage.Length >= 2 ? playniteLanguage.Substring(playniteLanguage.Length - 2) : "US";
        }

        // -------------------------
        // Steam
        // -------------------------

        /// <summary>
        /// Converts Playnite language to Steam language code.
        /// Defaults to "english" if no match is found.
        /// </summary>
        public static string GetSteamLang(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);
            return SteamLangMap.TryGetValue(playniteLanguage, out string steamLang) ? steamLang : "english";
        }

        // -------------------------
        // GOG
        // -------------------------

        /// <summary>
        /// Converts Playnite language to GOG language code.
        /// Defaults to "en-US" if unsupported.
        /// </summary>
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
        /// Converts Playnite language to Genshin Impact language code.
        /// Defaults to "en" if unsupported.
        /// </summary>
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
        /// Converts Playnite language to Wuthering Waves language code.
        /// Defaults to "en" if unsupported.
        /// </summary>
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
        /// Converts Playnite language to Origin language code.
        /// </summary>
        public static string GetEaLang(string playniteLanguage)
        {
            return Normalize(playniteLanguage);
        }

        // -------------------------
        // Epic Games
        // -------------------------

        /// <summary>
        /// Converts Playnite language to Epic Games language code.
        /// Replaces "_" with "-".
        /// </summary>
        public static string GetEpicLang(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);
            return playniteLanguage.Replace("_", "-");
        }

        // -------------------------
        // Xbox
        // -------------------------

        /// <summary>
        /// Converts Playnite language to Xbox language code.
        /// </summary>
        public static string GetXboxLang(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);
            return playniteLanguage.Replace("_", "-");
        }

        // -------------------------
        // Helpers
        // -------------------------

        /// <summary>
        /// Normalize lang to standard Playnite code "en_US".
        /// </summary>
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
        /// Returns the short part of the language code (e.g., "en" from "en_US").
        /// </summary>
        private static string GetShortLang(string playniteLanguage)
        {
            playniteLanguage = Normalize(playniteLanguage);
            string[] parts = playniteLanguage.Split('_');
            return parts[0].ToLower();
        }
    }
}
