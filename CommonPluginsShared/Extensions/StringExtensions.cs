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
        /// Decodes HTML strings and normalizes their whitespace.
        /// Replaces newlines in HTML with Environment.NewLine and trims the result.
        /// </summary>
        public static string HtmlDecode(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return Regex.Replace(WebUtility.HtmlDecode(input), @"\s+", match =>
            {
                if (match.Value.Contains('\n'))
                    return Environment.NewLine;
                else
                    return " ";
            }).Trim();
        }

        /// <summary>
        /// Removes diacritics (accents) from the string.
        /// </summary>
        public static string RemoveDiacritics(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string normalizedString = text.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Removes all whitespace characters from the string.
        /// </summary>
        public static string RemoveWhiteSpace(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return Regex.Replace(text, @"\s+", "");
        }

        /// <summary>
        /// Compares two strings for equality with optional normalization.
        /// Using PlayniteTools.NormalizeGameName if Normalize is true.
        /// </summary>
        public static bool IsEqual(this string source, string text, bool normalize = false)
        {
            try
            {
                if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(text))
                {
                    return false;
                }

                if (normalize)
                {
                    return string.Equals(
                        PlayniteTools.NormalizeGameName(source).Trim(),
                        PlayniteTools.NormalizeGameName(text).Trim(),
                        StringComparison.OrdinalIgnoreCase);
                }

                return string.Equals(source.Trim(), text.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return false;
            }
        }

        /// <summary>
        /// Escapes the string for use in a URI.
        /// </summary>
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