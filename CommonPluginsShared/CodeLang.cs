using CommonPluginsShared.Extensions;
using System;
using System.Collections.Generic;

namespace CommonPluginsShared
{
    public class CodeLang
    {
        // Dictionary mapping Playnite languages to Steam language codes
        private static Dictionary<string, string> SteamLangMap => new Dictionary<string, string>
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
        /// Converts Playnite language to Steam language code.
        /// </summary>
        public static string GetSteamLang(string playniteLanguage)
        {
            return SteamLangMap.TryGetValue(playniteLanguage, out string steamLang) ? steamLang : "english";
        }

        /// <summary>
        /// Converts Playnite language to GOG language code.
        /// </summary>
        public static string GetGogLang(string playniteLanguage)
        {
            string[] arrayLang = { "en-US", "de-DE", "fr-FR", "pl-PL", "ru-RU", "zh-Hans" };
            return arrayLang.ContainsString(playniteLanguage, StringComparison.OrdinalIgnoreCase) ? playniteLanguage : "en-US";
        }


        /// <summary>
        /// Converts Playnite language to Genshin Impact language code.
        /// </summary>
        public static string GetGenshinLang(string playniteLanguage)
        {
            if (playniteLanguage == "zh_CN")
            {
                return "chs";
            }
            if (playniteLanguage == "zh_TW")
            {
                return "cht";
            }

            string shortLang = GetShortLang(playniteLanguage);
            string[] arrayLang = { "chs", "cht", "de", "en", "es", "fr", "id", "jp", "kr", "pt", "ru", "th", "vi" };
            return arrayLang.ContainsString(shortLang, StringComparison.OrdinalIgnoreCase) ? shortLang : "en";
        }

        /// <summary>
        /// Converts Playnite language to Wuthering Waves language code.
        /// </summary>
        public static string GetWuWaLang(string playniteLanguage)
        {
            if (playniteLanguage == "zh_CN")
            {
                return "zh-Hans";
            }
            if (playniteLanguage == "zh_TW")
            {
                return "zh-Hant";
            }

            string shortLang = GetShortLang(playniteLanguage);
            string[] arrayLang = { "de", "en", "es", "fr", "ja", "ko", "zh-Hans", "zh-Hant" };
            return arrayLang.ContainsString(shortLang, StringComparison.OrdinalIgnoreCase) ? shortLang : "en";
        }


        /// <summary>
        /// Converts Playnite language to Origin language code.
        /// </summary>
        public static string GetOriginLang(string playniteLanguage)
        {
            return NormalizeEnglish(playniteLanguage);
        }

        /// <summary>
        /// Extracts country part for Origin language.
        /// </summary>
        public static string GetOriginLangCountry(string playniteLanguage)
        {
            playniteLanguage = NormalizeEnglish(playniteLanguage);
            return playniteLanguage.Substring(playniteLanguage.Length - 2);
        }


        /// <summary>
        /// Converts Playnite language to Epic Games language code.
        /// </summary>
        public static string GetEpicLang(string playniteLanguage)
        {
            playniteLanguage = NormalizeEnglish(playniteLanguage);
            return playniteLanguage.Replace("_", "-");
        }

        /// <summary>
        /// Extracts country part for Epic Games language.
        /// </summary>
        public static string GetEpicLangCountry(string playniteLanguage)
        {
            playniteLanguage = NormalizeEnglish(playniteLanguage);
            return playniteLanguage.Substring(0, 2);
        }


        /// <summary>
        /// Converts Playnite language to Xbox language code.
        /// </summary>
        public static string GetXboxLang(string playniteLanguage)
        {
            playniteLanguage = NormalizeEnglish(playniteLanguage);
            return playniteLanguage.Replace("_", "-");
        }



        /// <summary>
        /// Normalizes 'english' to 'en_US'.
        /// </summary>
        private static string NormalizeEnglish(string lang)
        {
            return lang == "english" ? "en_US" : lang;
        }

        /// <summary>
        /// Helper method to extract short language part (before underscore).
        /// </summary>
        private static string GetShortLang(string playniteLanguage)
        {
            string[] parts = playniteLanguage.Split('_');
            return parts[0].ToLower();
        }
    }
}
