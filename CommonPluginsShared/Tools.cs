using Playnite.SDK;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Web;
using System.IO;
using CommonPluginsShared.Extensions;
using System.Text.RegularExpressions;

namespace CommonPluginsShared
{
    public class Tools
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="withoutDouble"></param>
        /// <returns></returns>
        public static string SizeSuffix(double value, bool withoutDouble = false)
        {
            string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0" + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return withoutDouble
                ? string.Format("{0} {1}", adjustedSize.ToString("0", CultureInfo.CurrentCulture), SizeSuffixes[mag])
                : string.Format("{0} {1}", adjustedSize.ToString("0.0", CultureInfo.CurrentCulture), SizeSuffixes[mag]);
        }


        #region Date manipulations
        /// <summary>
        /// Get number week from a DateTime
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int WeekOfYearISO8601(DateTime date)
        {
            int day = (int)CultureInfo.CurrentCulture.Calendar.GetDayOfWeek(date);
            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.AddDays(4 - (day == 0 ? 7 : day)), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        /// <summary>
        /// Get a Datetime from a week number
        /// </summary>
        /// <param name="year"></param>
        /// <param name="day"></param>
        /// <param name="week"></param>
        /// <returns></returns>
        public static DateTime YearWeekDayToDateTime(int year, DayOfWeek day, int week)
        {
            DateTime startOfYear = new DateTime(year, 1, 1);

            // The +7 and %7 stuff is to avoid negative numbers etc.
            int daysToFirstCorrectDay = ((int)day - (int)startOfYear.DayOfWeek + 7) % 7;

            return startOfYear.AddDays(7 * (week - 1) + daysToFirstCorrectDay);
        }

        /// <summary>
        /// Get week count from a year
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        public static int GetWeeksInYear(int year)
        {
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            DateTime date1 = new DateTime(year, 12, 31);
            System.Globalization.Calendar cal = dfi.Calendar;
            return cal.GetWeekOfYear(date1, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
        }
        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <remarks>https://stackoverflow.com/a/3974535</remarks>
        /// <returns></returns>
        public static string ToHex(byte[] bytes)
        {
            char[] c = new char[bytes.Length * 2];

            byte b;

            for (int bx = 0, cx = 0; bx < bytes.Length; ++bx, ++cx)
            {
                b = (byte)(bytes[bx] >> 4);
                c[cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = (byte)(bytes[bx] & 0x0F);
                c[++cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }

            return new string(c);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] HexToBytes(string str)
        {
            if (str.Length == 0 || str.Length % 2 != 0)
            {
                return new byte[0];
            }

            byte[] buffer = new byte[str.Length / 2];
            char c;
            for (int bx = 0, sx = 0; bx < buffer.Length; ++bx, ++sx)
            {
                // Convert first half of byte
                c = str[sx];
                buffer[bx] = (byte)((c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0')) << 4);

                // Convert second half of byte
                c = str[++sx];
                buffer[bx] |= (byte)(c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0'));
            }

            return buffer;
        }


        public static double GetElapsedSeconde(string value, string type)
        {
            return double.TryParse(value, out double time) ? GetElapsedSeconde(time, type) : -1;
        }

        public static double GetElapsedSeconde(double value, string type)
        {
            switch (type.ToLower())
            {
                case "h":
                    double h = value;
                    return h * 3600;

                case "min":
                    double m = value;
                    return m * 60;

                case "s":
                    return value;

                default:
                    return -1;
            }
        }

        public static double GetElapsedSeconde(string value)
        {
            if (value.Contains("h", StringComparison.InvariantCultureIgnoreCase))
            {
                return GetElapsedSeconde(value.ToLower().Replace("h", string.Empty), "h");
            }
            if (value.Contains("min", StringComparison.InvariantCultureIgnoreCase))
            {
                return GetElapsedSeconde(value.ToLower().Replace("min", string.Empty), "min");
            }
            if (value.Contains("s", StringComparison.InvariantCultureIgnoreCase))
            {
                return GetElapsedSeconde(value.ToLower().Replace("s", string.Empty), "s");
            }
            return -1;
        }


        [Obsolete]
        public static string GetJsonInString(string str, string strStart, string strEnd, string strPurge = "")
        {
            try
            {
                int indexStart = str.IndexOf(strStart);
                int indexEnd = str.IndexOf(strEnd);

                string stringStart = str.Substring(0, indexStart + strStart.Length);
                string stringEnd = str.Substring(indexEnd);

                int length = str.Length - stringStart.Length - stringEnd.Length;

                string JsonDataString = str.Substring(indexStart + strStart.Length, length);

                if (!strPurge.IsNullOrEmpty())
                {
                    indexEnd = JsonDataString.IndexOf(strPurge);
                    length = JsonDataString.Length - (JsonDataString.Length - indexEnd - strPurge.Length + 1);
                    JsonDataString = JsonDataString.Substring(0, length);
                }

                return JsonDataString;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return string.Empty;
        }

        public static string GetJsonInString(string source, string regexForward)
        {
            string pattern = regexForward + @"(\[?{.*}\]?)[<]?";
            Match match = Regex.Match(source, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                Common.LogDebug(true, $"Json not found with {pattern} in {source}");
            }
            return string.Empty;
        }


        public static string FixCookieValue(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            if (str[0] != '"' && str.IndexOf(',') >= 0)
            {
                return HttpUtility.UrlEncode(str);
            }

            return str;
        }


        public static List<string> FindFile(string path, string fileName, bool scanSubFolders)
        {
            List<string> founds = new List<string>();

            try
            {
                path = CommonPlayniteShared.Common.Paths.FixPathLength(path);

                // Search for files in the current directory
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    if (Path.GetFileName(file).IsEqual(fileName))
                    {
                        founds.Add(file);
                    }
                }

                // If enabled, search in subdirectories
                if (scanSubFolders)
                {
                    string[] subDirs = Directory.GetDirectories(path);
                    foreach (string subDir in subDirs)
                    {
                        try
                        {
                            List<string> subResults = FindFile(subDir, fileName, true);
                            founds.AddRange(subResults.Where(x => !founds.Contains(x))); // Avoid duplicates
                        }
                        catch (UnauthorizedAccessException) { /* Skip directories we can't access */ }
                        catch (PathTooLongException) { /* Skip directories with overly long paths */ }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return founds;
        }

        public static List<string> FindDirectory(string path, string directoryName, bool scanSubFolders)
        {
            List<string> founds = new List<string>();

            try
            {
                path = CommonPlayniteShared.Common.Paths.FixPathLength(path);

                // Search for matching directories in the current directory
                string[] directories = Directory.GetDirectories(path);
                foreach (string dir in directories)
                {
                    if (Path.GetFileName(dir).IsEqual(directoryName))
                    {
                        founds.Add(dir);
                    }

                    // If enabled, search recursively in subdirectories
                    if (scanSubFolders)
                    {
                        try
                        {
                            List<string> subResults = FindDirectory(dir, directoryName, true);
                            founds.AddRange(subResults.Where(x => !founds.Contains(x))); // Avoid duplicates
                        }
                        catch (UnauthorizedAccessException) { /* Skip inaccessible directories */ }
                        catch (PathTooLongException) { /* Skip directories with overly long paths */ }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return founds;
        }
    }
}
