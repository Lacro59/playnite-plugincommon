using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsShared.Controls
{
    public class ListBoxExtend : ListBox
    {
        #region HeightStretch

        /// <summary>
        /// Dependency property for HeightStretch.
        /// </summary>
        public static readonly DependencyProperty HeightStretchProperty;

        /// <summary>
        /// Gets or sets whether the ListView should stretch to fill the available height.
        /// </summary>
        public bool HeightStretch
        {
            get => HeightStretchProperty == null || (bool)GetValue(HeightStretchProperty);
            set => SetValue(HeightStretchProperty, value);
        }

        #endregion

        #region WidthStretch

        /// <summary>
        /// Dependency property for WidthStretch.
        /// </summary>
        public static readonly DependencyProperty WidthStretchProperty;

        /// <summary>
        /// Gets or sets whether the ListView should stretch to fill the available width.
        /// </summary>
        public bool WidthStretch
        {
            get => WidthStretchProperty == null || (bool)GetValue(WidthStretchProperty);
            set => SetValue(WidthStretchProperty, value);
        }

        #endregion

        #region BubblingScrollEvents

        public bool BubblingScrollEvents
        {
            get => (bool)GetValue(BubblingScrollEventsProperty);
            set => SetValue(BubblingScrollEventsProperty, value);
        }

        public static readonly DependencyProperty BubblingScrollEventsProperty = DependencyProperty.Register(
            nameof(BubblingScrollEvents),
            typeof(bool),
            typeof(ListBoxExtend),
            new FrameworkPropertyMetadata(false, BubblingScrollEventsChangedCallback));

        private static void BubblingScrollEventsChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ListBoxExtend obj && e.NewValue != e.OldValue)
            {
                if ((bool)e.NewValue)
                {
                    obj.PreviewMouseWheel += UI.HandlePreviewMouseWheel;
                }
                else
                {
                    obj.PreviewMouseWheel -= UI.HandlePreviewMouseWheel;
                }
            }
        }

        #endregion


        public ListBoxExtend()
        {
            this.Loaded += ListBoxExtend_Loaded;
        }


        private void ListBoxExtend_Loaded(object sender, RoutedEventArgs e)
        {
            if (HeightStretch)
            {
                this.Height = ((FrameworkElement)sender).ActualHeight;
            }
            if (WidthStretch)
            {
                this.Width = ((FrameworkElement)sender).ActualWidth;
            }

            ((FrameworkElement)this.Parent).SizeChanged += Parent_SizeChanged;
        }

        private void Parent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (HeightStretch)
            {
                this.Height = ((FrameworkElement)sender).ActualHeight;
            }
            if (WidthStretch)
            {
                this.Width = ((FrameworkElement)sender).ActualWidth;
            }
        }
    }
}