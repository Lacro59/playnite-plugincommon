using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CommonPluginsShared.Extensions
{
    public static class StringExtensions
    {
        private static Regex TrimmableWhitespace = new Regex(@"^\s+|\s+$", RegexOptions.Compiled);
        private static Regex NonWordCharactersAndTrimmableWhitespace = new Regex(@"(?<start>^[\W_]+)|(?<end>[\W_]+$)|(?<middle>[\W_]+)", RegexOptions.Compiled);

        public static string TrimWhitespace(this string input)
        {
            return TrimmableWhitespace.Replace(input, "");
        }

        public static string NormalizeTitleForComparison(this string title)
        {
            MatchEvaluator matchEvaluator = (Match match) =>
            {
                if (match.Groups["middle"].Success) //if the match group is the last one in the regex (non-word characters, including whitespace, in the middle of a string)
                    return " ";
                else
                    return string.Empty; //remove non-word characters (including white space) at the start and end of the string
            };
            return NonWordCharactersAndTrimmableWhitespace.Replace(title, matchEvaluator).RemoveDiacritics();
        }

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

    }
}
