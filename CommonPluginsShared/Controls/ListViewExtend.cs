using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsShared.Controls
{
    public class ListViewExtend : ListView
    {
        #region BubblingScrollEvents
        public bool BubblingScrollEvents
        {
            get { return (bool)GetValue(BubblingScrollEventsProperty); }
            set { SetValue(BubblingScrollEventsProperty, value); }
        }

        public static readonly DependencyProperty BubblingScrollEventsProperty = DependencyProperty.Register(
            nameof(BubblingScrollEvents),
            typeof(bool),
            typeof(ListViewExtend),
            new FrameworkPropertyMetadata(false, BubblingScrollEventsChangedCallback));

        private static void BubblingScrollEventsChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var obj = sender as ListViewExtend;
            if (obj != null && e.NewValue != e.OldValue)
            {
                if ((bool)e.NewValue)
                {
                    obj.PreviewMouseWheel += Tools.HandlePreviewMouseWheel;
                }
                else
                {
                    obj.PreviewMouseWheel -= Tools.HandlePreviewMouseWheel;
                }
            }
        }
        #endregion
    }
}
