using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace CommonPluginsShared
{
    public class UI
    {
        private static ILogger Logger => LogManager.GetLogger();


        /// <summary>
        /// Adds or updates global WPF resources.
        /// </summary>
        /// <param name="ResourcesList">A list of resource key-value pairs to add or update in the application resources.</param>
        /// <returns>True if all resources were added or updated without fatal errors.</returns>
        public bool AddResources(List<ResourcesList> resourcesList)
        {
            Common.LogDebug(true, $"AddResources() - {Serialization.ToJson(resourcesList)}");
            foreach (ResourcesList item in resourcesList)
            {
                string itemKey = item.Key;
                try
                {
                    if (!Application.Current.Resources.Contains(item.Key))
                    {
                        Application.Current.Resources.Add(item.Key, item.Value);
                    }
                    else
                    {
                        // Safe replacement of existing resource
                        var existing = Application.Current.Resources[item.Key];
                        var incoming = item.Value;

                        if (existing.GetType() != incoming.GetType())
                        {
                            bool isBrushType = existing is Brush && incoming is Brush;

                            if (!isBrushType)
                            {
                                Logger.Warn($"Different type for {item.Key}");
                                continue;
                            }
                        }

                        Application.Current.Resources[item.Key] = item.Value;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, true, $"Error in AddResources({itemKey})");
                }
            }
            return true;
        }


        /// <summary>
        /// Recursively finds all visual children of a specified type from a DependencyObject.
        /// </summary>
        /// <typeparam name="T">The type of children to search for.</typeparam>
        /// <param name="depObj">The root DependencyObject to search from.</param>
        /// <returns>An enumerable of all found children of type T.</returns>
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

        /// <summary>
        /// Recursively finds a parent of a specified type in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the parent to find.</typeparam>
        /// <param name="child">The starting element.</param>
        /// <returns>The first parent of type T, or null if not found.</returns>
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null)
            {
                return null;
            }

            return parentObject is T parent ? parent : FindParent<T>(parentObject);
        }

        /// <summary>
        /// Obsolete: Use FindParent<T> instead.
        /// </summary>
        [Obsolete("Use UI.FindParent<T>", true)]
        public static T GetAncestorOfType<T>(FrameworkElement child) where T : FrameworkElement
        {
            var parent = VisualTreeHelper.GetParent(child);
            return parent != null && !(parent is T) ? GetAncestorOfType<T>((FrameworkElement)parent) : (T)parent;
        }

        /// <summary>
        /// Searches for a FrameworkElement by name within nested Expanders or TabItems.
        /// </summary>
        /// <param name="control">The starting control to search from.</param>
        /// <param name="elementName">The name of the element to find.</param>
        /// <returns>The element if found, otherwise null.</returns>
        private static FrameworkElement SearchElementByNameInExtander(object control, string elementName)
        {
            if (control is FrameworkElement)
            {
                if (((FrameworkElement)control).Name == elementName)
                {
                    return (FrameworkElement)control;
                }

                var children = LogicalTreeHelper.GetChildren((FrameworkElement)control);
                foreach (object child in children)
                {
                    if (child is FrameworkElement)
                    {
                        if (((FrameworkElement)child).Name == elementName)
                        {
                            return (FrameworkElement)child;
                        }

                        var subItems = LogicalTreeHelper.GetChildren((FrameworkElement)child);
                        foreach (object subItem in subItems)
                        {
                            if (subItem.ToString().ToLower().Contains("expander") || subItem.ToString().ToLower().Contains("tabitem"))
                            {
                                FrameworkElement tmp = null;

                                if (subItem.ToString().ToLower().Contains("expander"))
                                {
                                    tmp = SearchElementByNameInExtander(((Expander)subItem).Content, elementName);
                                }

                                if (subItem.ToString().ToLower().Contains("tabitem"))
                                {
                                    tmp = SearchElementByNameInExtander(((TabItem)subItem).Content, elementName);
                                }

                                if (tmp != null)
                                {
                                    return tmp;
                                }
                            }
                            else
                            {
                                var tmp = SearchElementByNameInExtander(child, elementName);
                                if (tmp != null)
                                {
                                    return tmp;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Searches for a FrameworkElement by name in the main window.
        /// </summary>
        /// <param name="elementName">The name of the element to find.</param>
        /// <param name="mustVisible">If true, element must be visible.</param>
        /// <param name="parentMustVisible">If true, element's parent must also be visible.</param>
        /// <param name="counter">The index of the matching element if multiple exist.</param>
        /// <returns>The matching FrameworkElement if found, otherwise null.</returns>
        public static FrameworkElement SearchElementByName(string elementName, bool mustVisible = false, bool parentMustVisible = false, int counter = 1)
        {
            return SearchElementByName(elementName, Application.Current.MainWindow, mustVisible, parentMustVisible, counter);
        }

        /// <summary>
        /// Searches for a FrameworkElement by name starting from a specific DependencyObject.
        /// </summary>
        /// <param name="elementName">The name of the element to search for.</param>
        /// <param name="dpObj">The root DependencyObject to search from.</param>
        /// <param name="mustVisible">If true, the element must be visible.</param>
        /// <param name="parentMustVisible">If true, the parent of the element must be visible.</param>
        /// <param name="counter">The number of occurrences to skip before returning the result.</param>
        /// <returns>The matching element or null if not found.</returns>
        public static FrameworkElement SearchElementByName(string elementName, DependencyObject dpObj, bool mustVisible = false, bool parentMustVisible = false, int counter = 1)
        {
            FrameworkElement elementFound = null;
            int count = 0;

            foreach (FrameworkElement el in FindVisualChildren<FrameworkElement>(dpObj))
            {
                if (el is Expander || el is TabItem)
                {
                    FrameworkElement tmpEl = null;

                    if (el is Expander)
                    {
                        tmpEl = SearchElementByNameInExtander(((Expander)el).Content, elementName);
                    }

                    if (el is TabItem)
                    {
                        tmpEl = SearchElementByNameInExtander(((TabItem)el).Content, elementName);
                    }

                    if (tmpEl != null && tmpEl.Name == elementName)
                    {
                        if (!mustVisible || tmpEl.IsVisible)
                        {
                            if (!parentMustVisible || ((FrameworkElement)el.Parent).IsVisible)
                            {
                                elementFound = tmpEl;
                                break;
                            }
                        }
                    }
                }
                else if (el.Name == elementName)
                {
                    count++;
                    bool isVisible = !mustVisible || el.IsVisible;
                    bool parentVisible = true;

                    if (parentMustVisible)
                    {
                        try
                        {
                            parentVisible = ((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)el.Parent).Parent).Parent).Parent).Parent).IsVisible;
                        }
                        catch { parentVisible = false; }
                    }

                    if (isVisible && parentVisible && count == counter)
                    {
                        elementFound = el;
                        break;
                    }
                }
            }

            return elementFound;
        }

        /// <summary>
        /// Set control size based on its parent container, with optional fallback dimensions.
        /// </summary>
        public static void SetControlSize(FrameworkElement controlElement)
        {
            SetControlSize(controlElement, 0, 0);
        }

        /// <summary>
        /// Sets the size of a control based on its parent, or uses fallback dimensions.
        /// </summary>
        /// <param name="controlElement">The control to resize.</param>
        /// <param name="defaultHeight">Fallback height if parent height is unavailable.</param>
        /// <param name="defaultWidth">Fallback width if parent width is unavailable.</param>
        public static void SetControlSize(FrameworkElement controlElement, double defaultHeight, double defaultWidth)
        {
            try
            {
                UserControl controlParent = FindParent<UserControl>(controlElement);
                FrameworkElement controlContener = (FrameworkElement)controlParent.Parent;

                Common.LogDebug(true, $"SetControlSize({controlElement.Name}) - parent.name: {controlContener.Name} - parent.Height: {controlContener.Height} - parent.Width: {controlContener.Width} - parent.MaxHeight: {controlContener.MaxHeight} - parent.MaxWidth: {controlContener.MaxWidth}");

                // Set Height
                if (!double.IsNaN(controlContener.Height))
                {
                    controlElement.Height = controlContener.Height;
                }
                else if (defaultHeight != 0)
                {
                    controlElement.Height = defaultHeight;
                }

                if (!double.IsNaN(controlContener.MaxHeight) && controlElement.Height > controlContener.MaxHeight)
                {
                    controlElement.Height = controlContener.MaxHeight;
                }

                // Set Width
                if (!double.IsNaN(controlContener.Width))
                {
                    controlElement.Width = controlContener.Width;
                }
                else if (defaultWidth != 0)
                {
                    controlElement.Width = defaultWidth;
                }

                if (!double.IsNaN(controlContener.MaxWidth) && controlElement.Width > controlContener.MaxWidth)
                {
                    controlElement.Width = controlContener.MaxWidth;
                }
            }
            catch
            {
                // Silently ignore errors
            }
        }

        /// <summary>
        /// Redirects mouse wheel scroll events to the parent container if the scroll boundary is reached.
        /// </summary>
        /// <param name="sender">The element that raised the event.</param>
        /// <param name="e">The mouse wheel event arguments.</param>
        public static void HandlePreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                var scrollViewer = UI.FindVisualChildren<ScrollViewer>((FrameworkElement)sender).FirstOrDefault();

                if (scrollViewer == null)
                {
                    return;
                }

                var scrollPos = scrollViewer.ContentVerticalOffset;
                if ((scrollPos == scrollViewer.ScrollableHeight && e.Delta < 0) || (scrollPos == 0 && e.Delta > 0))
                {
                    e.Handled = true;
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                    eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                    eventArg.Source = sender;
                    var parent = ((Control)sender).Parent as UIElement;
                    parent.RaiseEvent(eventArg);
                }
            }
        }
    }

    /// <summary>
    /// Represents a key-value pair for application resources.
    /// </summary>
    public class ResourcesList
    {
        public string Key { get; set; }
        public object Value { get; set; }
    }
}