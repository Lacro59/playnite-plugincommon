using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Controls;
using Playnite.SDK.Models;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CommonPluginsShared.Controls
{
    /// <summary>
    /// Extended base class for plugin user controls that supports dynamic updates,
    /// game context tracking, and plugin settings integration.
    /// </summary>
    public class PluginUserControlExtendBase : PluginUserControl
    {
        /// <summary>
        /// Shared logger instance.
        /// </summary>
        protected static ILogger Logger => LogManager.GetLogger();

        /// <summary>
        /// Reference to the plugin's internal data context.
        /// </summary>
        protected virtual IDataContext controlDataContext { get; set; }

        /// <summary>
        /// Dispatcher timer to periodically update control data.
        /// </summary>
        protected DispatcherTimer UpdateDataTimer { get; set; }

        protected Game CurrentGame { get; set; }

        #region Properties

        /// <summary>
        /// Determines whether the control should always be displayed.
        /// </summary>
        public static readonly DependencyProperty AlwaysShowProperty;
        public bool AlwaysShow { get; set; } = false;

        /// <summary>
        /// Delay in milliseconds between updates.
        /// </summary>
        public int Delay
        {
            get => (int)GetValue(DelayProperty);
            set => SetValue(DelayProperty, value);
        }

        public static readonly DependencyProperty DelayProperty = DependencyProperty.Register(
            nameof(Delay),
            typeof(int),
            typeof(PluginUserControlExtendBase),
            new FrameworkPropertyMetadata(10, DelayPropertyChangedCallback));

        private static void DelayPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PluginUserControlExtendBase obj && e.NewValue != e.OldValue)
            {
                obj.UpdateDataTimer.Interval = TimeSpan.FromMilliseconds((int)e.NewValue);
                obj.RestartTimer();
            }
        }

        /// <summary>
        /// The desktop view that was active when the control was created.
        /// </summary>
        public DesktopView ActiveViewAtCreation { get; set; }

        /// <summary>
        /// Indicates whether the control must be displayed.
        /// </summary>
        public bool MustDisplay
        {
            get => (bool)GetValue(MustDisplayProperty);
            set => SetValue(MustDisplayProperty, value);
        }

        public static readonly DependencyProperty MustDisplayProperty = DependencyProperty.Register(
            nameof(MustDisplay),
            typeof(bool),
            typeof(PluginUserControlExtendBase),
            new FrameworkPropertyMetadata(true, MustDisplayPropertyChangedCallback));

        private static void MustDisplayPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PluginUserControlExtendBase obj && e.NewValue != e.OldValue)
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

        /// <summary>
        /// Indicates whether the control should ignore plugin settings and always update.
        /// </summary>
        public bool IgnoreSettings
        {
            get => (bool)GetValue(IgnoreSettingsProperty);
            set => SetValue(IgnoreSettingsProperty, value);
        }

        public static readonly DependencyProperty IgnoreSettingsProperty = DependencyProperty.Register(
            nameof(IgnoreSettings),
            typeof(bool),
            typeof(PluginUserControlExtendBase),
            new FrameworkPropertyMetadata(false, SettingsPropertyChangedCallback));

        private static void SettingsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PluginUserControlExtendBase obj && e.NewValue != e.OldValue)
            {
                obj.PluginSettings_PropertyChanged(null, null);
            }
        }

        #endregion

        /// <summary>
        /// Constructor initializing the update timer and setting the initial view.
        /// </summary>
        public PluginUserControlExtendBase()
        {
            if (API.Instance?.ApplicationInfo?.Mode == ApplicationMode.Desktop)
            {
                ActiveViewAtCreation = API.Instance.MainView.ActiveDesktopView;
            }

            UpdateDataTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(Delay)
            };
            UpdateDataTimer.Tick += new EventHandler(UpdateDataEvent);
        }

        #region Property Change Handling

        /// <summary>
        /// Called when a dependency property changes.
        /// </summary>
        protected static void ControlsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PluginUserControlExtendBase obj && e.NewValue != e.OldValue)
            {
                obj.GameContextChanged(null, obj.GameContext);
            }
        }

        /// <summary>
        /// Called when plugin settings are updated.
        /// </summary>
        protected virtual void PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            GameContextChanged(null, GameContext);
        }

        /// <summary>
        /// Called when the selected game changes.
        /// </summary>
        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            CurrentGame = newContext;
            UpdateDataTimer.Stop();

            Visibility = Visibility.Collapsed;
            SetDefaultDataContext();
            MustDisplay = AlwaysShow ? AlwaysShow : controlDataContext.IsActivated;

            if (!(controlDataContext?.IsActivated ?? false) || newContext is null || !MustDisplay)
            {
                return;
            }

            RestartTimer();
        }

        /// <summary>
        /// Called when the plugin database item is updated.
        /// </summary>
        protected virtual void Database_ItemUpdated<TItem>(object sender, ItemUpdatedEventArgs<TItem> e) where TItem : DatabaseObject
        {
            _ = Application.Current.Dispatcher?.BeginInvoke(DispatcherPriority.Render, (Action)delegate
            {
                if (GameContext == null)
                {
                    return;
                }

                ItemUpdateEvent<TItem> ActualItem = e.UpdatedItems.Find(x => x.NewData.Id == GameContext.Id);
                if (ActualItem != null && ActualItem.NewData.Id != Guid.Empty)
                {
                    GameContextChanged(null, GameContext);
                }
            });
        }

        /// <summary>
        /// Called when the plugin database collection is changed.
        /// </summary>
        protected virtual void Database_ItemCollectionChanged<TItem>(object sender, ItemCollectionChangedEventArgs<TItem> e) where TItem : DatabaseObject
        {
            _ = Application.Current.Dispatcher?.BeginInvoke(DispatcherPriority.Render, (Action)delegate
            {
                if (GameContext == null)
                {
                    return;
                }

                GameContextChanged(null, GameContext);
            });
        }

        /// <summary>
        /// Called when a game is updated.
        /// </summary>
        protected virtual void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            _ = Application.Current.Dispatcher?.BeginInvoke(DispatcherPriority.Render, (Action)delegate
            {
                if (GameContext == null)
                {
                    return;
                }

                ItemUpdateEvent<Game> ActualItem = e.UpdatedItems.Find(x => x.NewData.Id == GameContext.Id);
                if (ActualItem?.NewData != null)
                {
                    GameContextChanged(null, ActualItem.NewData);
                }
            });
        }

        #endregion

        /// <summary>
        /// Sets a default data context. Can be overridden by derived classes.
        /// </summary>
        public virtual void SetDefaultDataContext()
        {
        }

        /// <summary>
        /// Sets the control data based on the current game context.
        /// </summary>
        public virtual void SetData(Game newContext)
        {
        }

        /// <summary>
        /// Called when the update timer ticks.
        /// </summary>
        private async void UpdateDataEvent(object sender, EventArgs e) => await UpdateDataAsync();

        /// <summary>
        /// Updates the control's content asynchronously based on the current game.
        /// </summary>
        public virtual async Task UpdateDataAsync()
        {
            UpdateDataTimer.Stop();
            Visibility = MustDisplay ? Visibility.Visible : Visibility.Collapsed;

            if (GameContext is null)
            {
                return;
            }

            if (CurrentGame is null || GameContext.Id != CurrentGame.Id)
            {
                return;
            }

            await Application.Current.Dispatcher?.InvokeAsync(() => SetData(GameContext), DispatcherPriority.Render);
        }

        /// <summary>
        /// Restarts the update timer.
        /// </summary>
        public void RestartTimer()
        {
            UpdateDataTimer.Stop();
            UpdateDataTimer.Start();
        }
    }
}