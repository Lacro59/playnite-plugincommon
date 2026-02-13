using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Controls;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
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
	/// Uses a singleton event handler pattern to optimize performance with multiple instances.
	/// </summary>
	public class PluginUserControlExtendBase : PluginUserControl
	{
		/// <summary>
		/// Shared logger instance.
		/// </summary>
		protected static readonly ILogger Logger = LogManager.GetLogger();

		/// <summary>
		/// Reference to the plugin's internal data context.
		/// </summary>
		protected virtual IDataContext controlDataContext { get; set; }

		/// <summary>
		/// Dispatcher timer to periodically update control data.
		/// </summary>
		protected DispatcherTimer UpdateDataTimer { get; set; }

		protected Game CurrentGame { get; set; }

		private static bool _staticEventsInitialized = false;
		private static readonly object _initLock = new object();
		private static readonly List<WeakReference<PluginUserControlExtendBase>> _instances = new List<WeakReference<PluginUserControlExtendBase>>();
		private static readonly Dictionary<string, bool> _pluginEventsAttached = new Dictionary<string, bool>();

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
				bool mustDisplay = (bool)e.NewValue;
				Visibility newVisibility = mustDisplay ? Visibility.Visible : Visibility.Collapsed;

				obj.Visibility = newVisibility;

				ContentControl contentControl = obj.Parent as ContentControl;
				if (contentControl != null)
				{
					contentControl.Visibility = newVisibility;
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

			RegisterInstance();
			Unloaded += PluginUserControlExtendBase_Unloaded;
		}

		#region Instance Management

		/// <summary>
		/// Registers this instance in the weak reference collection for event notification
		/// </summary>
		private void RegisterInstance()
		{
			lock (_initLock)
			{
				_instances.Add(new WeakReference<PluginUserControlExtendBase>(this));
			}
		}

		/// <summary>
		/// Initializes static event handlers only once for all instances.
		/// Must be called by derived classes after plugin database is loaded.
		/// </summary>
		protected void InitializeStaticEvents()
		{
			if (_staticEventsInitialized)
			{
				return;
			}

			lock (_initLock)
			{
				if (_staticEventsInitialized)
				{
					return;
				}

				AttachStaticEvents();
				_staticEventsInitialized = true;
			}
		}

		/// <summary>
		/// Attaches plugin-specific event handlers only once per plugin.
		/// Uses a unique key to prevent duplicate attachments.
		/// </summary>
		protected void AttachPluginEvents(string pluginKey, Action attachAction)
		{
			lock (_initLock)
			{
				if (!_pluginEventsAttached.ContainsKey(pluginKey))
				{
					attachAction?.Invoke();
					_pluginEventsAttached[pluginKey] = true;
				}
			}
		}

		/// <summary>
		/// Attaches static event handlers. Override in derived classes to attach to specific plugin databases.
		/// </summary>
		protected virtual void AttachStaticEvents()
		{
			API.Instance.Database.Games.ItemUpdated += OnStaticGamesItemUpdated;
		}

		/// <summary>
		/// Static event handler for game updates that notifies all alive instances
		/// </summary>
		private static void OnStaticGamesItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
		{
			NotifyAllInstances(instance => instance.Games_ItemUpdated(sender, e));
		}

		/// <summary>
		/// Creates a static event handler for plugin settings changes that notifies all instances
		/// </summary>
		protected static PropertyChangedEventHandler CreatePluginSettingsHandler()
		{
			return (sender, e) =>
			{
				NotifyAllInstances(instance => instance.PluginSettings_PropertyChanged(sender, e));
			};
		}

		/// <summary>
		/// Creates a static event handler for database item updates that notifies all instances.
		/// Generic version that preserves type information.
		/// </summary>
		protected static EventHandler<ItemUpdatedEventArgs<TItem>> CreateDatabaseItemUpdatedHandler<TItem>() where TItem : DatabaseObject
		{
			return (sender, e) =>
			{
				NotifyAllInstances(instance => instance.HandleDatabaseItemUpdated(sender, e));
			};
		}

		/// <summary>
		/// Creates a static event handler for database collection changes that notifies all instances.
		/// Generic version that preserves type information.
		/// </summary>
		protected static EventHandler<ItemCollectionChangedEventArgs<TItem>> CreateDatabaseCollectionChangedHandler<TItem>() where TItem : DatabaseObject
		{
			return (sender, e) =>
			{
				NotifyAllInstances(instance => instance.HandleDatabaseCollectionChanged(sender, e));
			};
		}


		/// <summary>
		/// Notifies all alive instances and cleans up dead weak references
		/// </summary>
		protected static void NotifyAllInstances(Action<PluginUserControlExtendBase> action)
		{
			lock (_initLock)
			{
				for (int i = _instances.Count - 1; i >= 0; i--)
				{
					if (_instances[i].TryGetTarget(out PluginUserControlExtendBase instance))
					{
						try
						{
							instance.Dispatcher?.BeginInvoke(action, instance);
						}
						catch
						{
							_instances.RemoveAt(i);
						}
					}
					else
					{
						_instances.RemoveAt(i);
					}
				}
			}
		}

		/// <summary>
		/// Instance handler for database item updates with proper generic type handling.
		/// Uses reflection to invoke the generic Database_ItemUpdated method with the correct type.
		/// </summary>
		protected void HandleDatabaseItemUpdated(object sender, object e)
		{
			Type eventArgsType = e.GetType();
			if (eventArgsType.IsGenericType && eventArgsType.GetGenericTypeDefinition() == typeof(ItemUpdatedEventArgs<>))
			{
				Type itemType = eventArgsType.GetGenericArguments()[0];
				System.Reflection.MethodInfo method = GetType()
					.GetMethod(nameof(Database_ItemUpdated), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

				if (method != null)
				{
					System.Reflection.MethodInfo genericMethod = method.MakeGenericMethod(itemType);
					genericMethod.Invoke(this, new object[] { sender, e });
				}
			}
		}

		/// <summary>
		/// Instance handler for database collection changes with proper generic type handling.
		/// Uses reflection to invoke the generic Database_ItemCollectionChanged method with the correct type.
		/// </summary>
		protected void HandleDatabaseCollectionChanged(object sender, object e)
		{
			Type eventArgsType = e.GetType();
			if (eventArgsType.IsGenericType && eventArgsType.GetGenericTypeDefinition() == typeof(ItemCollectionChangedEventArgs<>))
			{
				Type itemType = eventArgsType.GetGenericArguments()[0];
				System.Reflection.MethodInfo method = GetType()
					.GetMethod(nameof(Database_ItemCollectionChanged), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

				if (method != null)
				{
					System.Reflection.MethodInfo genericMethod = method.MakeGenericMethod(itemType);
					genericMethod.Invoke(this, new object[] { sender, e });
				}
			}
		}

		/// <summary>
		/// Cleanup when control is unloaded
		/// </summary>
		private void PluginUserControlExtendBase_Unloaded(object sender, RoutedEventArgs e)
		{
			lock (_initLock)
			{
				for (int i = _instances.Count - 1; i >= 0; i--)
				{
					if (!_instances[i].TryGetTarget(out PluginUserControlExtendBase _))
					{
						_instances.RemoveAt(i);
					}
				}
			}
		}

		#endregion

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

			if (!(controlDataContext?.IsActivated ?? false) || newContext == null || !MustDisplay)
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
			_ = API.Instance.MainView.UIDispatcher?.BeginInvoke(DispatcherPriority.Render, (Action)delegate
			{
				if (GameContext == null)
				{
					return;
				}

				ItemUpdateEvent<TItem> actualItem = e.UpdatedItems.Find(x => x.NewData.Id == GameContext.Id);
				if (actualItem != null && actualItem.NewData.Id != Guid.Empty)
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

				ItemUpdateEvent<Game> actualItem = e.UpdatedItems.Find(x => x.NewData.Id == GameContext.Id);
				if (actualItem?.NewData != null)
				{
					GameContextChanged(null, actualItem.NewData);
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
		private async void UpdateDataEvent(object sender, EventArgs e)
		{
			await UpdateDataAsync();
		}

		/// <summary>
		/// Updates the control's content asynchronously based on the current game.
		/// </summary>
		public virtual async Task UpdateDataAsync()
		{
			UpdateDataTimer.Stop();
			Visibility = MustDisplay ? Visibility.Visible : Visibility.Collapsed;

			if (GameContext == null || CurrentGame == null || GameContext.Id != CurrentGame.Id)
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