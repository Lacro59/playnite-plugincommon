using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CommonPluginsShared.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Decodes HTML strings and normalizes their whitespace (no more non-breaking spaces, f.e.)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string HtmlDecode(this string input)
        {
            if (input == null)
                return null;

            return Regex.Replace(WebUtility.HtmlDecode(input), @"\s+", match =>
            {
                if (match.Value.Contains('\n'))
                    return Environment.NewLine;
                else
                    return " ";
            }).Trim();
        }

        public static string RemoveDiacritics(this string text)
        {
            string normalizedString = text.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    _ = stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }


        public static string RemoveWhiteSpace(this string text)
        {
            return Regex.Replace(text, @"\s+", "");
        }


        public static bool IsEqual(this string source, string text, bool Normalize = false)
        {
            try
            {
                return string.IsNullOrEmpty(source) || string.IsNullOrEmpty(text)
                    ? false
                    : Normalize
                    ? PlayniteTools.NormalizeGameName(source).Trim().ToLower() == PlayniteTools.NormalizeGameName(text).Trim().ToLower()
                    : source.Trim().ToLower() == text.Trim().ToLower();
            }
            catch(Exception ex)
            {
                Common.LogError(ex, false);
                return false;
            }
        }


        public static string EscapeDataString(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            return Uri.EscapeDataString(str);
        }
    }
}
