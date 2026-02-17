using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;

namespace CommonPluginsShared.UI
{
	#region Commands

	/// <summary>
	/// Provides common relay commands for Playnite plugins.
	/// </summary>
	public static class CommandsHelper
	{
		/// <summary>
		/// Marks a view model as requiring a Playnite restart.
		/// Sets the IsRestartRequired property on the parent window's DataContext.
		/// </summary>
		/// <remarks>
		/// The command parameter should be the originating FrameworkElement.
		/// </remarks>
		public static RelayCommand<FrameworkElement> RestartRequired => new RelayCommand<FrameworkElement>((sender) =>
		{
			try
			{
				Window parentWindow = UIHelper.FindParent<Window>(sender);
				if (parentWindow?.DataContext?.GetType().GetProperty("IsRestartRequired") != null)
				{
					((dynamic)parentWindow.DataContext).IsRestartRequired = true;
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "Failed to set restart required flag");
			}
		});

		/// <summary>
		/// Selects a game in the library view and switches to library view.
		/// </summary>
		/// <remarks>
		/// Command parameter should be the game ID (Guid).
		/// </remarks>
		public static RelayCommand<Guid> GoToGame => new RelayCommand<Guid>((id) =>
		{
			API.Instance.MainView.SelectGame(id);
			API.Instance.MainView.SwitchToLibraryView();
		});
	}

	#endregion

	#region UI Helper

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
		/// <param name="resourcesList">List of resources to add/update</param>
		/// <returns>True if all operations succeeded without fatal errors</returns>
		public static bool AddResources(List<ResourcesList> resourcesList)
		{
			Common.LogDebug(true, $"AddResources() - {Serialization.ToJson(resourcesList)}");

			foreach (ResourcesList item in resourcesList)
			{
				string itemKey = item.Key;

				try
				{
					if (!System.Windows.Application.Current.Resources.Contains(item.Key))
					{
						System.Windows.Application.Current.Resources.Add(item.Key, item.Value);
					}
					else
					{
						object existing = System.Windows.Application.Current.Resources[item.Key];
						object incoming = item.Value;

						if (existing.GetType() != incoming.GetType())
						{
							bool isBrushType = existing is Brush && incoming is Brush;
							if (!isBrushType)
							{
								Logger.Warn($"Type mismatch for resource: {item.Key}");
								continue;
							}
						}

						System.Windows.Application.Current.Resources[item.Key] = item.Value;
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
		/// <typeparam name="T">Type of children to find</typeparam>
		/// <param name="depObj">Root DependencyObject to search from</param>
		/// <returns>Enumerable of all children of type T</returns>
		public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
		{
			if (depObj != null)
			{
				for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
				{
					DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

					if (child != null && child is T typedChild)
					{
						yield return typedChild;
					}

					foreach (T childOfChild in FindVisualChildren<T>(child))
					{
						yield return childOfChild;
					}
				}
			}
		}

		/// <summary>
		/// Recursively finds the first parent of a specified type in the visual tree.
		/// </summary>
		/// <typeparam name="T">Type of parent to find</typeparam>
		/// <param name="child">Starting element</param>
		/// <returns>First parent of type T or null if not found</returns>
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
		/// <param name="elementName">Name of the element to find</param>
		/// <param name="mustVisible">Element must be visible</param>
		/// <param name="parentMustVisible">Element's parent must be visible</param>
		/// <param name="counter">Skip count (for multiple matches)</param>
		/// <returns>Matching FrameworkElement or null if not found</returns>
		public static FrameworkElement SearchElementByName(string elementName, bool mustVisible = false, bool parentMustVisible = false, int counter = 1)
		{
			return SearchElementByName(elementName, System.Windows.Application.Current.MainWindow, mustVisible, parentMustVisible, counter);
		}

		/// <summary>
		/// Searches for a FrameworkElement by name starting from a specific DependencyObject.
		/// </summary>
		/// <param name="elementName">Name of the element to find</param>
		/// <param name="dpObj">Root DependencyObject to search from</param>
		/// <param name="mustVisible">Element must be visible</param>
		/// <param name="parentMustVisible">Parent must be visible</param>
		/// <param name="counter">Skip count (for multiple matches)</param>
		/// <returns>Matching FrameworkElement or null if not found</returns>
		public static FrameworkElement SearchElementByName(string elementName, DependencyObject dpObj, bool mustVisible = false, bool parentMustVisible = false, int counter = 1)
		{
			FrameworkElement elementFound = null;
			int count = 0;

			foreach (FrameworkElement el in FindVisualChildren<FrameworkElement>(dpObj))
			{
				if (el is Expander || el is TabItem)
				{
					FrameworkElement tmpEl = null;

					if (el is Expander expander)
					{
						tmpEl = SearchElementByNameInExpander(expander.Content, elementName);
					}
					else if (el is TabItem tabItem)
					{
						tmpEl = SearchElementByNameInExpander(tabItem.Content, elementName);
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
						catch
						{
							parentVisible = false;
						}
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
		/// Searches for an element by name within nested Expanders or TabItems.
		/// </summary>
		/// <param name="control">Starting control</param>
		/// <param name="elementName">Name of element to find</param>
		/// <returns>Found FrameworkElement or null</returns>
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

			System.Collections.IEnumerable children = LogicalTreeHelper.GetChildren(frameworkElement);

			foreach (object child in children)
			{
				if (!(child is FrameworkElement childElement))
				{
					continue;
				}

				if (childElement.Name == elementName)
				{
					return childElement;
				}

				System.Collections.IEnumerable subItems = LogicalTreeHelper.GetChildren(childElement);

				foreach (object subItem in subItems)
				{
					string subItemType = subItem.ToString().ToLower();

					if (subItemType.Contains("expander") || subItemType.Contains("tabitem"))
					{
						FrameworkElement tmp = null;

						if (subItemType.Contains("expander"))
						{
							tmp = SearchElementByNameInExpander(((Expander)subItem).Content, elementName);
						}
						else if (subItemType.Contains("tabitem"))
						{
							tmp = SearchElementByNameInExpander(((TabItem)subItem).Content, elementName);
						}

						if (tmp != null)
						{
							return tmp;
						}
					}
					else
					{
						FrameworkElement tmp = SearchElementByNameInExpander(child, elementName);
						if (tmp != null)
						{
							return tmp;
						}
					}
				}
			}

			return null;
		}

		#endregion

		#region Control Sizing

		/// <summary>
		/// Sets control size based on its parent container.
		/// </summary>
		/// <param name="controlElement">Control to resize</param>
		public static void SetControlSize(FrameworkElement controlElement)
		{
			SetControlSize(controlElement, 0, 0);
		}

		/// <summary>
		/// Sets control size based on parent or uses fallback dimensions.
		/// </summary>
		/// <param name="controlElement">Control to resize</param>
		/// <param name="defaultHeight">Fallback height</param>
		/// <param name="defaultWidth">Fallback width</param>
		public static void SetControlSize(FrameworkElement controlElement, double defaultHeight, double defaultWidth)
		{
			try
			{
				UserControl controlParent = FindParent<UserControl>(controlElement);
				FrameworkElement controlContainer = (FrameworkElement)controlParent.Parent;

				Common.LogDebug(true, $"SetControlSize({controlElement.Name}) - parent: {controlContainer.Name}, H: {controlContainer.Height}, W: {controlContainer.Width}");

				// Set Height
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

				// Set Width
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
				// Silently ignore sizing errors
			}
		}

		#endregion

		#region Mouse Wheel Handling

		/// <summary>
		/// Redirects mouse wheel events to parent when scroll boundary is reached.
		/// Prevents scroll blocking in nested ScrollViewers.
		/// </summary>
		/// <param name="sender">Element that raised the event</param>
		/// <param name="e">Mouse wheel event args</param>
		public static void HandlePreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (!e.Handled)
			{
				ScrollViewer scrollViewer = FindVisualChildren<ScrollViewer>((FrameworkElement)sender).FirstOrDefault();

				if (scrollViewer == null)
				{
					return;
				}

				double scrollPos = scrollViewer.ContentVerticalOffset;

				if ((scrollPos == scrollViewer.ScrollableHeight && e.Delta < 0) || (scrollPos == 0 && e.Delta > 0))
				{
					e.Handled = true;

					MouseWheelEventArgs eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
					{
						RoutedEvent = UIElement.MouseWheelEvent,
						Source = sender
					};

					UIElement parent = ((Control)sender).Parent as UIElement;
					parent?.RaiseEvent(eventArg);
				}
			}
		}

		#endregion
	}

	#endregion

	#region Models

	/// <summary>
	/// Represents a key-value pair for application resources.
	/// </summary>
	public class ResourcesList
	{
		/// <summary>
		/// Gets or sets the resource key.
		/// </summary>
		public string Key { get; set; }

		/// <summary>
		/// Gets or sets the resource value.
		/// </summary>
		public object Value { get; set; }
	}

	#endregion
}