﻿using CommonPluginsShared.Extensions;
using System.Collections.Generic;

namespace CommonPluginsShared
{
    public class CodeLang
    {
        /// <summary>
        /// String lang format for Steam
        /// </summary>
        /// <param name="playniteLanguage"></param>
        /// <returns></returns>
        /// <remarks>https://partner.steamgames.com/doc/store/localization?#supported_languages</remarks>
        public static string GetSteamLang(string playniteLanguage)
        {
            string SteamLang = string.Empty;

            switch (playniteLanguage)
            {
                case "ar_SA":
                    SteamLang = "arabic";
                    break;
                case "ca_ES":
                    SteamLang = "arabic";
                    break;
                case "cs_CZ":
                    SteamLang = "czech";
                    break;
                case "de_DE":
                    SteamLang = "german";
                    break;
                case "el_GR":
                    SteamLang = "greek";
                    break;
                case "es_ES":
                    SteamLang = "spanish";
                    break;
                case "fi_FI":
                    SteamLang = "finnish";
                    break;
                case "fr_FR":
                    SteamLang = "french";
                    break;
                case "he_IL":
                    break;
                case "hu_HU":
                    SteamLang = "hungarian";
                    break;
                case "id_ID":
                    SteamLang = "indonesian";
                    break;
                case "it_IT":
                    SteamLang = "italian";
                    break;
                case "ja_JP":
                    SteamLang = "japanese";
                    break;
                case "ko_KO":
                    SteamLang = "koreana";
                    break;
                case "nl_NL":
                    SteamLang = "dutch";
                    break;
                case "no_NO":
                    SteamLang = "norwegian";
                    break;
                case "pl_PL":
                    SteamLang = "polish";
                    break;
                case "pt_BR":
                    SteamLang = "brazilian";
                    break;
                case "pt_PT":
                    SteamLang = "portuguese";
                    break;
                case "ro_RO":
                    SteamLang = "romanian";
                    break;
                case "ru_RU":
                    SteamLang = "russian";
                    break;
                case "sv_SE":
                    SteamLang = "swedish";
                    break;
                case "tr_TR":
                    SteamLang = "turkish";
                    break;
                case "uk_UA":
                    SteamLang = "ukrainian";
                    break;
                case "vi_VN":
                    SteamLang = "vietnamese";
                    break;
                case "zh_CN":
                    SteamLang = "schinese";
                    break;
                case "zh_TW":
                    SteamLang = "tchinese";
                    break;
                default:
                    SteamLang = "english";
                    break;
            }

            return SteamLang;
        }


        /// <summary>
        /// String lang format for GOG
        /// </summary>
        /// <param name="playniteLanguage"></param>
        /// <returns></returns>
        public static string GetGogLang(string playniteLanguage)
        {
            // Only languages available
            string[] arrayLang = { "de", "en", "fr", "ru", "zh", "zh-Hans" };

            playniteLanguage = playniteLanguage.Substring(0, 2).ToLower();
            if (!arrayLang.ContainsString(playniteLanguage))
            {
                playniteLanguage = "en";
            }

            return playniteLanguage;
        }


        public static string GetGenshinLang(string playniteLanguage)
        {
            // Only languages available
            string[] arrayLang = { "chs", "cht", "de", "en", "es", "fr", "id", "jp", "kr", "pt", "ru", "th", "vi" };

            playniteLanguage = playniteLanguage.Substring(playniteLanguage.Length - 2).ToLower();
            if (!arrayLang.ContainsString(playniteLanguage))
            {
                playniteLanguage = "en";
            }

            if (playniteLanguage.IsEqual("zh_CN"))
            {
                playniteLanguage = "chs";
            }
            if (playniteLanguage.IsEqual("zh_CN"))
            {
                playniteLanguage = "cht";
            }

            return playniteLanguage;
        }

        public static string GetWuWaLang(string playniteLanguage)
        {
            // Only languages available
            string[] arrayLang = { "de", "en", "es", "fr", "ja", "ko", "zh-Hans", "zh-Hant" };

            playniteLanguage = playniteLanguage.Substring(0, 2).ToLower();
            if (!arrayLang.ContainsString(playniteLanguage))
            {
                playniteLanguage = "en";
            }

            if (playniteLanguage.IsEqual("zh_CN"))
            {
                playniteLanguage = "zh-Hans";
            }
            if (playniteLanguage.IsEqual("zh_CN"))
            {
                playniteLanguage = "zh-Hant";
            }

            return playniteLanguage;
        }


        /// <summary>
        /// String lang format for Origin
        /// </summary>
        /// <param name="playniteLanguage"></param>
        /// <returns></returns>
        public static string GetOriginLang(string playniteLanguage)
        {
            if (playniteLanguage == "english")
            {
                playniteLanguage = "en_US";
            }
            return playniteLanguage;
        }

        /// <summary>
        /// String lang country for Origin
        /// </summary>
        /// <param name="playniteLanguage"></param>
        /// <returns></returns>
        public static string GetOriginLangCountry(string playniteLanguage)
        {
            return playniteLanguage.Substring(playniteLanguage.Length - 2);
        }


        /// <summary>
        /// String lang format for Epic Game
        /// </summary>
        /// <param name="playniteLanguage"></param>
        /// <returns></returns>
        public static string GetEpicLang(string playniteLanguage)
        {
            if (playniteLanguage == "english")
            {
                playniteLanguage = "en_US";
            }
            return playniteLanguage.Replace("_", "-");
        }

        /// <summary>
        /// String lang country for Epic Game
        /// </summary>
        /// <param name="playniteLanguage"></param>
        /// <returns></returns>
        public static string GetEpicLangCountry(string playniteLanguage)
        {
            if (playniteLanguage == "english")
            {
                playniteLanguage = "en_US";
            }
            return playniteLanguage.Substring(0, 2);
        }


        /// <summary>
        /// String lang format for Xbox / Windows Store
        /// </summary>
        /// <param name="playniteLanguage"></param>
        /// <returns></returns>
        public static string GetXboxLang(string playniteLanguage)
        {
            if (playniteLanguage == "english")
            {
                playniteLanguage = "en_US";
            }
            return playniteLanguage.Replace("_", "-");
        }
    }
}
