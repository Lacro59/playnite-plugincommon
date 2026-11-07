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

        /// <summary>
        /// Reverses common UTF-8→Windows-1252→UTF-8 mojibake (e.g. <c>Ã¢â€žÂ¢</c> → <c>™</c>).
        /// Only runs when the string shows typical mojibake markers.
        /// </summary>
        public static string FixUtf8Mojibake(this string input)
        {
            if (string.IsNullOrEmpty(input) || !LooksLikeUtf8Mojibake(input))
            {
                return input;
            }

            Encoding cp1252 = Encoding.GetEncoding(1252);
            string current = input;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    byte[] bytes = cp1252.GetBytes(current);
                    string next = Encoding.UTF8.GetString(bytes);
                    if (string.Equals(next, current, StringComparison.Ordinal) || next.IndexOf('\uFFFD') >= 0)
                    {
                        break;
                    }

                    current = next;
                    if (!LooksLikeUtf8Mojibake(current))
                    {
                        break;
                    }
                }
                catch
                {
                    break;
                }
            }

            return current;
        }

        private static bool LooksLikeUtf8Mojibake(string value)
        {
            // Markers from UTF-8 bytes misread as Windows-1252 then re-encoded as UTF-8.
            return value.IndexOf('Ã') >= 0
                || value.IndexOf('Â') >= 0
                || value.IndexOf("â€", StringComparison.Ordinal) >= 0;
        }
    }
}
