using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using CommonPluginsShared.Extensions;

namespace CommonPluginsShared.Utilities
{
	#region Error Accumulator

	/// <summary>
	/// Accumulates error messages without duplicates for batch display.
	/// Useful for collecting multiple errors during operations.
	/// </summary>
	public class ErrorAccumulator
	{
		private readonly List<string> _messages = new List<string>();

		/// <summary>
		/// Adds an error message if not already present.
		/// </summary>
		/// <param name="message">Error message to add</param>
		public void Add(string message)
		{
			if (!_messages.Contains(message))
			{
				_messages.Add(message);
			}
		}

		/// <summary>
		/// Gets all accumulated messages formatted with line breaks.
		/// </summary>
		/// <returns>Formatted error messages</returns>
		public string GetFormattedMessages()
		{
			return string.Join(Environment.NewLine, _messages);
		}

		/// <summary>
		/// Gets the count of accumulated messages.
		/// </summary>
		public int Count => _messages.Count;

		/// <summary>
		/// Checks if any error messages have been accumulated.
		/// </summary>
		public bool HasErrors => _messages.Count > 0;

		/// <summary>
		/// Clears all accumulated messages.
		/// </summary>
		public void Clear()
		{
			_messages.Clear();
		}

		/// <summary>
		/// Gets all messages as a list.
		/// </summary>
		public List<string> GetMessages() => new List<string>(_messages);
	}

	#endregion

	#region Utility Tools

	/// <summary>
	/// Provides general-purpose utility methods for common operations.
	/// </summary>
	public static class UtilityTools
	{
		#region Size Formatting

		/// <summary>
		/// Converts a byte size to human-readable format with appropriate suffix.
		/// </summary>
		/// <param name="value">Size in bytes</param>
		/// <param name="withoutDecimal">If true, omits decimal places</param>
		/// <returns>Formatted size string (e.g., "1.5 MB")</returns>
		/// <example>
		/// SizeSuffix(1536) => "1.5 KB"
		/// SizeSuffix(1536, true) => "2 KB"
		/// </example>
		public static string SizeSuffix(double value, bool withoutDecimal = false)
		{
			string[] sizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

			if (value < 0)
			{
				return "-" + SizeSuffix(-value, withoutDecimal);
			}

			if (value == 0)
			{
				return "0" + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "0 bytes";
			}

			int magnitude = (int)Math.Log(value, 1024);
			decimal adjustedSize = (decimal)value / (1L << (magnitude * 10));

			string format = withoutDecimal ? "0" : "0.0";
			return string.Format("{0} {1}", adjustedSize.ToString(format, CultureInfo.CurrentCulture), sizeSuffixes[magnitude]);
		}

		#endregion

		#region Date Utilities

		/// <summary>
		/// Gets the ISO 8601 week number for a given date.
		/// </summary>
		/// <param name="date">Date to get week number for</param>
		/// <returns>Week number (1-53)</returns>
		public static int WeekOfYearISO8601(DateTime date)
		{
			int day = (int)CultureInfo.CurrentCulture.Calendar.GetDayOfWeek(date);
			return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
				date.AddDays(4 - (day == 0 ? 7 : day)),
				CalendarWeekRule.FirstFourDayWeek,
				DayOfWeek.Monday);
		}

		/// <summary>
		/// Converts a year, week number, and day of week to a DateTime.
		/// </summary>
		/// <param name="year">Year</param>
		/// <param name="day">Day of week</param>
		/// <param name="week">Week number</param>
		/// <returns>DateTime for the specified week and day</returns>
		public static DateTime YearWeekDayToDateTime(int year, DayOfWeek day, int week)
		{
			DateTime startOfYear = new DateTime(year, 1, 1);
			int daysToFirstCorrectDay = ((int)day - (int)startOfYear.DayOfWeek + 7) % 7;
			return startOfYear.AddDays(7 * (week - 1) + daysToFirstCorrectDay);
		}

		/// <summary>
		/// Gets the number of weeks in a specific year according to current culture.
		/// </summary>
		/// <param name="year">Year to check</param>
		/// <returns>Number of weeks (52 or 53)</returns>
		public static int GetWeeksInYear(int year)
		{
			DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
			DateTime date = new DateTime(year, 12, 31);
			System.Globalization.Calendar cal = dfi.Calendar;
			return cal.GetWeekOfYear(date, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
		}

		#endregion

		#region Hex Conversion

		/// <summary>
		/// Converts a byte array to a hexadecimal string.
		/// </summary>
		/// <param name="bytes">Bytes to convert</param>
		/// <returns>Hexadecimal string (lowercase)</returns>
		/// <example>
		/// ToHex(new byte[] { 255, 16 }) => "ff10"
		/// </example>
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
		/// Converts a hexadecimal string to a byte array.
		/// </summary>
		/// <param name="str">Hexadecimal string</param>
		/// <returns>Byte array or empty array if invalid</returns>
		/// <example>
		/// HexToBytes("ff10") => new byte[] { 255, 16 }
		/// </example>
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
				c = str[sx];
				buffer[bx] = (byte)((c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0')) << 4);

				c = str[++sx];
				buffer[bx] |= (byte)(c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0'));
			}

			return buffer;
		}

		#endregion

		#region Time Conversion

		/// <summary>
		/// Converts a time value with unit string to seconds.
		/// </summary>
		/// <param name="value">Time value as string</param>
		/// <param name="type">Unit type ("h", "min", "s")</param>
		/// <returns>Time in seconds or -1 if invalid</returns>
		public static double GetElapsedSeconds(string value, string type)
		{
			return double.TryParse(value, out double time) ? GetElapsedSeconds(time, type) : -1;
		}

		/// <summary>
		/// Converts a time value with unit to seconds.
		/// </summary>
		/// <param name="value">Time value</param>
		/// <param name="type">Unit type ("h", "min", "s")</param>
		/// <returns>Time in seconds or -1 if invalid</returns>
		public static double GetElapsedSeconds(double value, string type)
		{
			switch (type.ToLower())
			{
				case "h":
					return value * 3600;

				case "min":
					return value * 60;

				case "s":
					return value;

				default:
					return -1;
			}
		}

		/// <summary>
		/// Parses a time string with embedded unit to seconds.
		/// Supports formats: "1.5h", "30min", "90s"
		/// </summary>
		/// <param name="value">Time string with unit</param>
		/// <returns>Time in seconds or -1 if invalid</returns>
		/// <example>
		/// GetElapsedSeconds("1.5h") => 5400
		/// GetElapsedSeconds("30min") => 1800
		/// </example>
		public static double GetElapsedSeconds(string value)
		{
			if (value.Contains("h", StringComparison.InvariantCultureIgnoreCase))
			{
				return GetElapsedSeconds(value.ToLower().Replace("h", string.Empty), "h");
			}

			if (value.Contains("min", StringComparison.InvariantCultureIgnoreCase))
			{
				return GetElapsedSeconds(value.ToLower().Replace("min", string.Empty), "min");
			}

			if (value.Contains("s", StringComparison.InvariantCultureIgnoreCase))
			{
				return GetElapsedSeconds(value.ToLower().Replace("s", string.Empty), "s");
			}

			return -1;
		}

		#endregion

		#region JSON Extraction

		/// <summary>
		/// Extracts JSON from a string using forward-looking regex pattern.
		/// </summary>
		/// <param name="source">Source string containing JSON</param>
		/// <param name="regexForward">Regex pattern before JSON (without capture group)</param>
		/// <returns>Extracted JSON string or empty string if not found</returns>
		/// <example>
		/// GetJsonInString("data = {\"key\":\"value\"}", "data = ") => "{\"key\":\"value\"}"
		/// </example>
		public static string GetJsonInString(string source, string regexForward)
		{
			string pattern = regexForward + @"(\[?{.*}\]?)[<]?";
			Match match = Regex.Match(source, pattern);

			if (match.Success)
			{
				return match.Groups[1].Value;
			}

			Common.LogDebug(true, $"JSON not found with pattern: {pattern}");
			return string.Empty;
		}

		/// <summary>
		/// Obsolete: Use GetJsonInString(string, string) instead.
		/// </summary>
		[Obsolete("Use GetJsonInString(string source, string regexForward)")]
		public static string GetJsonInString(string str, string strStart, string strEnd, string strPurge = "")
		{
			try
			{
				int indexStart = str.IndexOf(strStart);
				int indexEnd = str.IndexOf(strEnd);

				string stringStart = str.Substring(0, indexStart + strStart.Length);
				string stringEnd = str.Substring(indexEnd);

				int length = str.Length - stringStart.Length - stringEnd.Length;
				string jsonDataString = str.Substring(indexStart + strStart.Length, length);

				if (!strPurge.IsNullOrEmpty())
				{
					indexEnd = jsonDataString.IndexOf(strPurge);
					length = jsonDataString.Length - (jsonDataString.Length - indexEnd - strPurge.Length + 1);
					jsonDataString = jsonDataString.Substring(0, length);
				}

				return jsonDataString;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "Error extracting JSON from string");
				return string.Empty;
			}
		}

		#endregion

		#region Cookie Utilities

		/// <summary>
		/// Fixes cookie value formatting for HTTP headers.
		/// URL-encodes values containing commas if not already quoted.
		/// </summary>
		/// <param name="str">Cookie value</param>
		/// <returns>Fixed cookie value</returns>
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

		#endregion

		#region File System Search

		/// <summary>
		/// Recursively searches for files by name in a directory.
		/// </summary>
		/// <param name="path">Starting directory path</param>
		/// <param name="fileName">Filename to search for (case-insensitive)</param>
		/// <param name="scanSubFolders">If true, searches subdirectories</param>
		/// <returns>List of matching file paths</returns>
		public static List<string> FindFile(string path, string fileName, bool scanSubFolders)
		{
			List<string> founds = new List<string>();

			try
			{
				path = CommonPlayniteShared.Common.Paths.FixPathLength(path);

				string[] files = Directory.GetFiles(path);
				foreach (string file in files)
				{
					if (Path.GetFileName(file).IsEqual(fileName))
					{
						founds.Add(file);
					}
				}

				if (scanSubFolders)
				{
					string[] subDirs = Directory.GetDirectories(path);
					foreach (string subDir in subDirs)
					{
						try
						{
							List<string> subResults = FindFile(subDir, fileName, true);
							founds.AddRange(subResults.Where(x => !founds.Contains(x)));
						}
						catch (UnauthorizedAccessException)
						{
							// Skip inaccessible directories
						}
						catch (PathTooLongException)
						{
							// Skip paths that are too long
						}
					}
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, $"Error searching for file: {fileName}");
			}

			return founds;
		}

		/// <summary>
		/// Recursively searches for directories by name.
		/// </summary>
		/// <param name="path">Starting directory path</param>
		/// <param name="directoryName">Directory name to search for (case-insensitive)</param>
		/// <param name="scanSubFolders">If true, searches subdirectories</param>
		/// <returns>List of matching directory paths</returns>
		public static List<string> FindDirectory(string path, string directoryName, bool scanSubFolders)
		{
			List<string> founds = new List<string>();

			try
			{
				path = CommonPlayniteShared.Common.Paths.FixPathLength(path);

				string[] directories = Directory.GetDirectories(path);
				foreach (string dir in directories)
				{
					if (Path.GetFileName(dir).IsEqual(directoryName))
					{
						founds.Add(dir);
					}

					if (scanSubFolders)
					{
						try
						{
							List<string> subResults = FindDirectory(dir, directoryName, true);
							founds.AddRange(subResults.Where(x => !founds.Contains(x)));
						}
						catch (UnauthorizedAccessException)
						{
							// Skip inaccessible directories
						}
						catch (PathTooLongException)
						{
							// Skip paths that are too long
						}
					}
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, $"Error searching for directory: {directoryName}");
			}

			return founds;
		}

		#endregion
	}

	#endregion
}