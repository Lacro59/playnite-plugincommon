using System.Windows;

namespace CommonPluginsControls.Stores
{
    /// <summary>
    /// Reserved attached properties for store panel controls hosted in <see cref="StoresSettingsView"/>.
    /// Store panels no longer use expanders; this type remains for API compatibility.
    /// </summary>
    public static class StorePanelAttachedProperties
    {
        /// <summary>
        /// Gets the navigation embedded state for a store panel.
        /// </summary>
        public static bool GetNavigationEmbedded(DependencyObject obj)
        {
            return (bool)obj.GetValue(NavigationEmbeddedProperty);
        }

        /// <summary>
        /// Sets the navigation embedded state for a store panel.
        /// </summary>
        public static void SetNavigationEmbedded(DependencyObject obj, bool value)
        {
            obj.SetValue(NavigationEmbeddedProperty, value);
        }

        /// <summary>
        /// Reserved for backward compatibility. Store panels render flat content without expanders.
        /// </summary>
        public static readonly DependencyProperty NavigationEmbeddedProperty =
            DependencyProperty.RegisterAttached(
                "NavigationEmbedded",
                typeof(bool),
                typeof(StorePanelAttachedProperties),
                new UIPropertyMetadata(false));
    }
}
