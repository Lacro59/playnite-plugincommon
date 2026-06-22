using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsShared.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Returns the start of the week for the given date.
        /// </summary>
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = dt.DayOfWeek - startOfWeek;
            if (diff < 0)
            {
                diff += 7;
            }
            return dt.AddDays(-1 * diff).Date;
        }

        /// <summary>
        /// Returns the end of the week for the given date.
        /// </summary>
        public static DateTime EndOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = dt.DayOfWeek - startOfWeek;
            if (diff < 0)
            {
                diff += 7;
            }
            return dt.AddDays(diff).Date;
        }

        /// <summary>
        /// Calculates the number of days between two dates.
        /// Uses DateTime.Now if no comparison date is provided.
        /// </summary>
        public static int GetDaysBetween(this DateTime dt)
        {
            return dt.GetDaysBetween(DateTime.Now);
        }

        /// <summary>
        /// Calculates the number of days between two dates.
        /// </summary>
        public static int GetDaysBetween(this DateTime dt, DateTime dtCompare)
        {
            // DateTime is a value type and cannot be null.
            // If default value is passed, use Now.
            if (dtCompare == default)
            {
                dtCompare = DateTime.Now;
            }

            if (dt > dtCompare)
            {
                return dtCompare.GetDaysBetween(dt);
            }

            // Optimized calculation using Date property instead of string parsing
            return (int)(dtCompare.Date - dt.Date).TotalDays;
        }

        /// <summary>
        /// Calculates the number of months between two dates.
        /// Uses DateTime.Now if no comparison date is provided.
        /// </summary>
        public static int GetMonthsBetween(this DateTime dt)
        {
            return dt.GetMonthsBetween(DateTime.Now);
        }

        /// <summary>
        /// Calculates the number of months between two dates.
        /// </summary>
        public static int GetMonthsBetween(this DateTime dt, DateTime dtCompare)
        {
            if (dtCompare == default)
            {
                dtCompare = DateTime.Now;
            }

            if (dt > dtCompare)
            {
                return dtCompare.GetMonthsBetween(dt);
            }

            return ((dtCompare.Year - dt.Year) * 12) + dtCompare.Month - dt.Month;
        }

        /// <summary>
        /// Calculates the number of years between two dates.
        /// Uses DateTime.Now if no comparison date is provided.
        /// </summary>
        public static int GetYearsBetween(this DateTime dt)
        {
            return dt.GetYearsBetween(DateTime.Now);
        }

        /// <summary>
        /// Calculates the number of years between two dates.
        /// </summary>
        public static int GetYearsBetween(this DateTime dt, DateTime dtCompare)
        {
            if (dtCompare == default)
            {
                dtCompare = DateTime.Now;
            }

            if (dt > dtCompare)
            {
                return dtCompare.GetYearsBetween(dt);
            }

            int years = dtCompare.Year - dt.Year;

            // Adjust if the month/day hasn't passed yet in the end year
            if (dtCompare.Month < dt.Month || (dtCompare.Month == dt.Month && dtCompare.Day < dt.Day))
            {
                years--;
            }

            return years;
        }
    }
}