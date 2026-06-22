using CommonPluginsShared.UI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CommonPluginsShared.Controls
{
    public class ExpanderAttachedProperties
    {
        #region HideExpanderArrow AttachedProperty
        [AttachedPropertyBrowsableForType(typeof(Expander))]
        public static bool GetHideExpanderArrow(DependencyObject obj)
        {
            return (bool)obj.GetValue(HideExpanderArrowProperty);
        }

        [AttachedPropertyBrowsableForType(typeof(Expander))]
        public static void SetHideExpanderArrow(DependencyObject obj, bool value)
        {
            obj.SetValue(HideExpanderArrowProperty, value);
        }

        // Using a DependencyProperty as the backing store for HideExpanderArrow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HideExpanderArrowProperty =
            DependencyProperty.RegisterAttached("HideExpanderArrow", typeof(bool), typeof(ExpanderAttachedProperties), new UIPropertyMetadata(false, OnHideExpanderArrowChanged));

        private static void OnHideExpanderArrowChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Expander expander = (Expander)o;

            if (expander.IsLoaded)
            {
                UpdateExpanderArrow(expander, (bool)e.NewValue);
            }
            else
            {
                expander.Loaded += new RoutedEventHandler((x, y) => UpdateExpanderArrow(expander, (bool)e.NewValue));
            }
        }

        private static void UpdateExpanderArrow(Expander expander, bool visible)
        {
            foreach (TextBlock el in UIHelper.FindVisualChildren<TextBlock>(expander))
            {
                if (el.Name == "DownArrow")
                {
                    el.Visibility = Visibility.Hidden;
                }
                if (el.Name == "UpArrow")
                {
                    el.Visibility = Visibility.Hidden;
                }
                if (el.Name == "CollapsedIcon")
                {
                    el.Visibility = Visibility.Hidden;
                }
                if (el.Name == "ExpandedIcon")
                {
                    el.Visibility = Visibility.Hidden;
                }
            }
        }
        #endregion

        #region HideExpanderHeader AttachedProperty
        [AttachedPropertyBrowsableForType(typeof(Expander))]
        public static bool GetHideExpanderHeader(DependencyObject obj)
        {
            return (bool)obj.GetValue(HideExpanderHeaderProperty);
        }

        [AttachedPropertyBrowsableForType(typeof(Expander))]
        public static void SetHideExpanderHeader(DependencyObject obj, bool value)
        {
            obj.SetValue(HideExpanderHeaderProperty, value);
        }

        public static readonly DependencyProperty HideExpanderHeaderProperty =
            DependencyProperty.RegisterAttached("HideExpanderHeader", typeof(bool), typeof(ExpanderAttachedProperties), new UIPropertyMetadata(false, OnHideExpanderHeaderChanged));

        private static void OnHideExpanderHeaderChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Expander expander = (Expander)o;

            if (expander.IsLoaded)
            {
                UpdateExpanderHeader(expander, (bool)e.NewValue);
            }
            else
            {
                expander.Loaded += new RoutedEventHandler((x, y) => UpdateExpanderHeader(expander, (bool)e.NewValue));
            }
        }

        private static void UpdateExpanderHeader(Expander expander, bool hideHeader)
        {
            foreach (ToggleButton toggleButton in UIHelper.FindVisualChildren<ToggleButton>(expander))
            {
                toggleButton.Visibility = hideHeader ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        #endregion
    }
}
