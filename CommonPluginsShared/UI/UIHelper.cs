using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CommonPluginsShared.UI
{
    /// <summary>
    /// Provides WPF UI helper utilities for visual tree navigation and control manipulation.
    /// </summary>
    public static class UIHelper
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        #region Resource Management

        /// <summary>
        /// Adds or updates global application resources.
        /// Safely handles type mismatches and updates existing resources.
        /// </summary>
        /// <param name="resourcesList">List of resources to add/update.</param>
        /// <returns>True if all operations succeeded without fatal errors.</returns>
        public static bool AddResources(List<ResourcesList> resourcesList)
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
                        object existing = Application.Current.Resources[item.Key];
                        object incoming = item.Value;

                        if (existing.GetType() != incoming.GetType())
                        {
                            // Allow Brush subtype substitutions (e.g. SolidColorBrush → LinearGradientBrush)
                            bool isBrushType = existing is Brush && incoming is Brush;
                            if (!isBrushType)
                            {
                                Logger.Warn($"Type mismatch for resource: {item.Key}");
                                continue;
                            }
                        }

                        Application.Current.Resources[item.Key] = item.Value;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, true, $"Error adding resource: {itemKey}");
                }
            }

            return true;
        }

        #endregion

        #region Visual Tree Navigation

        /// <summary>
        /// Recursively finds all visual children of a specified type.
        /// </summary>
        /// <typeparam name="T">Type of children to find.</typeparam>
        /// <param name="depObj">Root DependencyObject to search from.</param>
        /// <returns>Enumerable of all children of type T.</returns>
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null)
            {
                yield break;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                if (child is T typedChild)
                {
                    yield return typedChild;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }

        /// <summary>
        /// Recursively finds the first parent of a specified type in the visual tree.
        /// </summary>
        /// <typeparam name="T">Type of parent to find.</typeparam>
        /// <param name="child">Starting element.</param>
        /// <returns>First parent of type T, or null if not found.</returns>
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
            {
                return null;
            }

            return parentObject is T parent ? parent : FindParent<T>(parentObject);
        }

        #endregion

        #region Element Search

        /// <summary>
        /// Searches for a FrameworkElement by name in the main window.
        /// Supports nested Expanders and TabItems.
        /// </summary>
        /// <param name="elementName">Name of the element to find.</param>
        /// <param name="mustVisible">Element must be visible.</param>
        /// <param name="parentMustVisible">Element's parent must be visible.</param>
        /// <param name="counter">Skip count for multiple matches (1-based).</param>
        /// <returns>Matching FrameworkElement or null if not found.</returns>
        public static FrameworkElement SearchElementByName(
            string elementName,
            bool mustVisible = false,
            bool parentMustVisible = false,
            int counter = 1)
        {
            return SearchElementByName(elementName, Application.Current.MainWindow, mustVisible, parentMustVisible, counter);
        }

        /// <summary>
        /// Searches for a FrameworkElement by name starting from a specific DependencyObject.
        /// </summary>
        /// <param name="elementName">Name of the element to find.</param>
        /// <param name="dpObj">Root DependencyObject to search from.</param>
        /// <param name="mustVisible">Element must be visible.</param>
        /// <param name="parentMustVisible">Parent must be visible.</param>
        /// <param name="counter">Skip count for multiple matches (1-based).</param>
        /// <returns>Matching FrameworkElement or null if not found.</returns>
        public static FrameworkElement SearchElementByName(
            string elementName,
            DependencyObject dpObj,
            bool mustVisible = false,
            bool parentMustVisible = false,
            int counter = 1)
        {
            int count = 0;

            foreach (FrameworkElement el in FindVisualChildren<FrameworkElement>(dpObj))
            {
                if (el is Expander expander)
                {
                    FrameworkElement tmpEl = SearchElementByNameInExpander(expander.Content, elementName);
                    if (IsMatchInContainer(tmpEl, elementName, el, mustVisible, parentMustVisible))
                    {
                        return tmpEl;
                    }
                }
                else if (el is TabItem tabItem)
                {
                    FrameworkElement tmpEl = SearchElementByNameInExpander(tabItem.Content, elementName);
                    if (IsMatchInContainer(tmpEl, elementName, el, mustVisible, parentMustVisible))
                    {
                        return tmpEl;
                    }
                }
                else if (el.Name == elementName)
                {
                    count++;

                    bool isVisible = !mustVisible || el.IsVisible;
                    bool parentVisible = !parentMustVisible || IsAncestorVisible(el);

                    if (isVisible && parentVisible && count == counter)
                    {
                        return el;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks whether a candidate element found inside a container (Expander/TabItem)
        /// satisfies the visibility constraints.
        /// </summary>
        private static bool IsMatchInContainer(
            FrameworkElement candidate,
            string elementName,
            FrameworkElement container,
            bool mustVisible,
            bool parentMustVisible)
        {
            if (candidate == null || candidate.Name != elementName)
            {
                return false;
            }

            if (mustVisible && !candidate.IsVisible)
            {
                return false;
            }

            if (parentMustVisible && container.Parent is FrameworkElement containerParent && !containerParent.IsVisible)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Walks up five parent levels to check ancestor visibility.
        /// This heuristic mirrors the original implementation depth.
        /// </summary>
        private static bool IsAncestorVisible(FrameworkElement element)
        {
            try
            {
                FrameworkElement current = element;
                for (int i = 0; i < 5; i++)
                {
                    if (!(current.Parent is FrameworkElement parent))
                    {
                        return false;
                    }
                    current = parent;
                }
                return current.IsVisible;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Searches for an element by name within the logical subtree of a nested
        /// Expander or TabItem. Recurses into further nested Expanders/TabItems.
        /// </summary>
        /// <param name="control">Starting content object.</param>
        /// <param name="elementName">Name of element to find.</param>
        /// <returns>Found FrameworkElement or null.</returns>
        private static FrameworkElement SearchElementByNameInExpander(object control, string elementName)
        {
            if (!(control is FrameworkElement frameworkElement))
            {
                return null;
            }

            if (frameworkElement.Name == elementName)
            {
                return frameworkElement;
            }

            foreach (object child in LogicalTreeHelper.GetChildren(frameworkElement))
            {
                if (!(child is FrameworkElement childElement))
                {
                    continue;
                }

                if (childElement.Name == elementName)
                {
                    return childElement;
                }

                foreach (object subItem in LogicalTreeHelper.GetChildren(childElement))
                {
                    FrameworkElement tmp = null;

                    if (subItem is Expander nestedExpander)
                    {
                        tmp = SearchElementByNameInExpander(nestedExpander.Content, elementName);
                    }
                    else if (subItem is TabItem nestedTabItem)
                    {
                        tmp = SearchElementByNameInExpander(nestedTabItem.Content, elementName);
                    }
                    else
                    {
                        tmp = SearchElementByNameInExpander(child, elementName);
                    }

                    if (tmp != null)
                    {
                        return tmp;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Control Sizing

        /// <summary>
        /// Sets control size based on its parent container dimensions.
        /// </summary>
        /// <param name="controlElement">Control to resize.</param>
        public static void SetControlSize(FrameworkElement controlElement)
        {
            SetControlSize(controlElement, 0, 0);
        }

        /// <summary>
        /// Sets control size based on parent container, with optional fallback dimensions.
        /// Priority: parent explicit size → fallback → unchanged.
        /// </summary>
        /// <param name="controlElement">Control to resize.</param>
        /// <param name="defaultHeight">Fallback height (0 = skip).</param>
        /// <param name="defaultWidth">Fallback width (0 = skip).</param>
        public static void SetControlSize(FrameworkElement controlElement, double defaultHeight, double defaultWidth)
        {
            try
            {
                UserControl controlParent = FindParent<UserControl>(controlElement);
                FrameworkElement controlContainer = (FrameworkElement)controlParent.Parent;

                Common.LogDebug(true, $"SetControlSize({controlElement.Name}) - parent: {controlContainer.Name}, H: {controlContainer.Height}, W: {controlContainer.Width}");

                // Height
                if (!double.IsNaN(controlContainer.Height))
                {
                    controlElement.Height = controlContainer.Height;
                }
                else if (defaultHeight != 0)
                {
                    controlElement.Height = defaultHeight;
                }

                if (!double.IsNaN(controlContainer.MaxHeight) && controlElement.Height > controlContainer.MaxHeight)
                {
                    controlElement.Height = controlContainer.MaxHeight;
                }

                // Width
                if (!double.IsNaN(controlContainer.Width))
                {
                    controlElement.Width = controlContainer.Width;
                }
                else if (defaultWidth != 0)
                {
                    controlElement.Width = defaultWidth;
                }

                if (!double.IsNaN(controlContainer.MaxWidth) && controlElement.Width > controlContainer.MaxWidth)
                {
                    controlElement.Width = controlContainer.MaxWidth;
                }
            }
            catch
            {
                // Silently ignore sizing errors — container may not yet be in the visual tree
            }
        }

        #endregion

        #region Mouse Wheel Handling

        /// <summary>
        /// Redirects mouse wheel events to the parent element when the inner
        /// <see cref="ScrollViewer"/> has reached its scroll boundary.
        /// Prevents scroll blocking in nested ScrollViewers.
        /// </summary>
        /// <param name="sender">Element that raised the event.</param>
        /// <param name="e">Mouse wheel event args.</param>
        public static void HandlePreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }

            ScrollViewer scrollViewer = FindVisualChildren<ScrollViewer>((FrameworkElement)sender).FirstOrDefault();
            if (scrollViewer == null)
            {
                return;
            }

            double scrollPos = scrollViewer.ContentVerticalOffset;
            bool atBottom = scrollPos == scrollViewer.ScrollableHeight && e.Delta < 0;
            bool atTop = scrollPos == 0 && e.Delta > 0;

            if (atBottom || atTop)
            {
                e.Handled = true;

                MouseWheelEventArgs eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = sender
                };

                (((Control)sender).Parent as UIElement)?.RaiseEvent(eventArg);
            }
        }

        #endregion
    }
}