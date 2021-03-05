using CommonPluginsShared.Collections;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Controls;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CommonPluginsShared.Controls
{
    public abstract class PluginUserControlExtend : PluginUserControl
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
            PluginUserControlExtend obj = sender as PluginUserControlExtend;            
            if (obj != null && e.NewValue != e.OldValue)
            {
                ContentControl contentControl = obj.Parent as ContentControl;

                if ((bool)e.NewValue)
                {
                    obj.Visibility = Visibility.Visible;
                    if (contentControl != null)
                    {
                        contentControl.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    obj.Visibility = Visibility.Collapsed;
                    if (contentControl != null)
                    {
                        contentControl.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }


        public bool IgnoreSettings
        {
            get { return (bool)GetValue(IgnoreSettingsProperty); }
            set { SetValue(IgnoreSettingsProperty, value); }
        }

        public static readonly DependencyProperty IgnoreSettingsProperty = DependencyProperty.Register(
            nameof(IgnoreSettings),
            typeof(bool),
            typeof(PluginUserControlExtend),
            new FrameworkPropertyMetadata(false, SettingsPropertyChangedCallback));

        private static void SettingsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PluginUserControlExtend obj = sender as PluginUserControlExtend;
            if (obj != null && e.NewValue != e.OldValue)
            {
                obj.PluginSettings_PropertyChanged(null, null);
            }
        }


        #region OnPropertyChange
        // When plugin settings is updated
        public abstract void PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e);

        // When plugin datbase is udpated
        internal virtual void Database_ItemUpdated<TItem>(object sender, ItemUpdatedEventArgs<TItem> e) where TItem : DatabaseObject
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
            {
                if (GameContext == null)
                {
                    return;
                }

                // Publish changes for the currently displayed game if updated
                var ActualItem = e.UpdatedItems.Find(x => x.NewData.Id == GameContext.Id);
                if (ActualItem != null)
                {
                    Guid Id = ActualItem.NewData.Id;
                    if (Id != null)
                    {
                        GameContextChanged(null, GameContext);
                    }
                }
            }));
        }
        internal virtual void Database_ItemCollectionChanged<TItem>(object sender, ItemCollectionChangedEventArgs<TItem> e) where TItem : DatabaseObject
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
            {
                if (GameContext == null)
                {
                    return;
                }

                GameContextChanged(null, GameContext);
            }));
        }

        // When game is updated
        internal virtual void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            // Publish changes for the currently displayed game if updated
            if (GameContext == null)
            {
                return;
            }

            var ActualItem = e.UpdatedItems.Find(x => x.NewData.Id == GameContext.Id);
            if (ActualItem != null)
            {
                Game newContext = ActualItem.NewData;
                if (newContext != null)
                {
                    GameContextChanged(null, newContext);
                }
            }
        }
        #endregion
    }
}
