using Newtonsoft.Json.Linq;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PluginCommon
{
    public class Tools
    {
        private static readonly ILogger logger = LogManager.GetLogger();


        // https://stackoverflow.com/a/48735199
        public static JArray RemoveValue(JArray oldArray, dynamic obj)
        {
            List<string> newArray = oldArray.ToObject<List<string>>();
            newArray.Remove(obj);
            return JArray.FromObject(newArray);
        }

        /// <summary>
        /// Get number week.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int WeekOfYearISO8601(DateTime date)
        {
            var day = (int)CultureInfo.CurrentCulture.Calendar.GetDayOfWeek(date);
            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.AddDays(4 - (day == 0 ? 7 : day)), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        /// <summary>
        /// Gel all control in depObj.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="depObj"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        // https://www.infragistics.com/community/blogs/b/blagunas/posts/find-the-parent-control-of-a-specific-type-in-wpf-and-silverlight
        /// <summary>
        /// Get control's parent by type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="child"></param>
        /// <returns></returns>
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                return FindParent<T>(parentObject);
            }
        }


        public static void DesactivePlayniteWindowControl(DependencyObject depObj)
        {
            foreach (Button sp in FindVisualChildren<Button>(depObj))
            {
                if (sp.Name == "PART_ButtonMinimize")
                {
                    sp.Visibility = Visibility.Hidden;
                }
                if (sp.Name == "PART_ButtonMaximize")
                {
                    sp.Visibility = Visibility.Hidden;
                }
            }
        }

        public static bool IsDisabledPlaynitePlugins(string PluginName, string ConfigurationPath)
        {
            JArray DisabledPlugins = new JArray();
            JObject PlayniteConfig = new JObject();
            try
            {
                string FileConfig = ConfigurationPath + "\\config.json";
                if (File.Exists(FileConfig))
                {
                    PlayniteConfig = JObject.Parse(File.ReadAllText(FileConfig));
                    DisabledPlugins = (JArray)PlayniteConfig["DisabledPlugins"];

                    if (DisabledPlugins != null)
                    {
                        foreach (string name in DisabledPlugins)
                        {
                            if (name.ToLower() == PluginName.ToLower())
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    logger.Warn($"PluginCommon - File not found {FileConfig}");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", "Error on IsDisabledPlaynitePlugins()");
                return false;
            }

            return false;
        }
    }
}
