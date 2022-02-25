using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CommonPluginsShared.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveDiacritics(this string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }


        public static string RemoveWhiteSpace(this string text)
        {
            return Regex.Replace(text, @"s", "");
        }


        public static bool IsEqual(this string source, string text, bool Normalize = false)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (Normalize)
            {
                return PlayniteTools.NormalizeGameName(source).Trim().ToLower() == PlayniteTools.NormalizeGameName(text).Trim().ToLower();
            }
            else
            {
                return source.Trim().ToLower() == text.Trim().ToLower();
            }
        }
    }
}
