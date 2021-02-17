using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsShared.Controls
{
    public class PluginUserControlExtend : PluginUserControl
    {
        internal static readonly ILogger logger = LogManager.GetLogger();
        internal static IResourceProvider resources = new ResourceProvider();


        public bool MustDisplay
        {
            get { return (bool)GetValue(MustDisplayProperty); }
            set { SetValue(MustDisplayProperty, value); }
        }

        public static readonly DependencyProperty MustDisplayProperty = DependencyProperty.Register(
            nameof(MustDisplay), 
            typeof(bool), 
            typeof(PluginUserControlExtend),
            new FrameworkPropertyMetadata(true, MustDisplayPropertyChangedCallback));

        private static void MustDisplayPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var obj = sender as PluginUserControlExtend;            
            if (obj != null && e.NewValue != e.OldValue)
            {
                var objParent = Tools.FindParent<ContentControl>(obj);

                if ((bool)e.NewValue)
                {
                    obj.Visibility = Visibility.Visible;
                }
                else
                {
                    obj.Visibility = Visibility.Collapsed;
                }
            }
        }



    }
}
