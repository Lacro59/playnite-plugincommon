using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;

namespace CommonPluginsShared.Extensions
{
    // https://stackoverflow.com/a/60671815

    /// <summary>
    /// A class to hold extension methods for C# Lists 
    /// </summary>
    /// <summary>
    /// A class to hold extension methods for C# Lists 
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Convert a list of Type T to a CSV string.
        /// </summary>
        /// <typeparam name="T">The type of the object held in the list</typeparam>
        /// <param name="items">The list of items to process</param>
        /// <param name="orderBy">Whether to order properties alphabetically.</param>
        /// <param name="delimiter">Specify the delimiter, default is comma.</param>
        /// <param name="noHeader">If true, omits the header row.</param>
        /// <param name="header">Optional custom header list.</param>
        /// <returns>A CSV formatted string.</returns>
        public static string ToCsv<T>(this List<T> items, bool orderBy = true, string delimiter = ",", bool noHeader = false, List<string> header = null)
        {
            try
            {
                Type itemType = typeof(T);
                IEnumerable<PropertyInfo> props = orderBy
                    ? itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.Name)
                    : (IEnumerable<PropertyInfo>)itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                StringBuilder csv = new StringBuilder();

                // Write Headers
                if (!noHeader)
                {
                    if (header?.Count > 0)
                    {
                        csv.AppendLine(string.Join(delimiter, header));
                    }
                    else
                    {
                        csv.AppendLine(string.Join(delimiter, props.Select(p => p.Name)));
                    }
                }

                // Write Rows
                foreach (T item in items)
                {
                    // Write Fields
                    csv.AppendLine(string.Join(delimiter, props.Select(p => GetCsvFieldBasedOnValue(p, item))));
                }

                return csv.ToString();
            }
            catch (Exception)
            {
                // Re-throw to let the caller handle it, or log it if possible.
                // Preserving stack trace is important.
                throw;
            }
        }

        /// <summary>
        /// Provide generic and specific handling of fields for CSV export.
        /// </summary>
        private static object GetCsvFieldBasedOnValue<T>(PropertyInfo p, T item)
        {
            string value;
            try
            {
                value = p.GetValue(item, null)?.ToString();
                if (value == null)
                {
                    return "NULL";
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    return "";
                }

                // Guard strings with quotes if they contain the delimiter or quotes
                // For simplicity assuming basic CSV escaping requirements or just quotes
                if (p.PropertyType == typeof(string))
                {
                    // Escape double quotes by doubling them
                    value = value.Replace("\"", "\"\"");
                    value = $"\"{value}\"";
                }
            }
            catch
            {
                throw;
            }
            return value;
        }
    }
}