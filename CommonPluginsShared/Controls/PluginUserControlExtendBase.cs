using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Controls;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
		protected static readonly ILogger Logger = LogManager.GetLogger();

		protected virtual IDataContext controlDataContext { get; set; }

		protected DispatcherTimer UpdateDataTimer { get; set; }

		protected Game CurrentGame { get; set; }

		private static readonly object _initLock = new object();

		// Keyed by concrete type so each derived class initialises independently.
		private static readonly Dictionary<Type, bool> _typeEventsInitialized = new Dictionary<Type, bool>();

		// Separate flag for the single Games.ItemUpdated subscription shared by all types.
		private static bool _baseEventsAttached = false;

		// Per-plugin guard (e.g. PluginButton and PluginViewItem share the same plugin key).
		private static readonly Dictionary<string, bool> _pluginEventsAttached = new Dictionary<string, bool>();

		private static readonly List<WeakReference<PluginUserControlExtendBase>> _instances =
			new List<WeakReference<PluginUserControlExtendBase>>();

		#region Dependency Properties

		public bool AlwaysShow { get; set; } = false;

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

		public DesktopView ActiveViewAtCreation { get; set; }

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

				if (obj.Parent is ContentControl contentControl)
				{
					contentControl.Visibility = newVisibility;
				}

				if (!mustDisplay)
				{
					obj.Visibility = Visibility.Collapsed;
				}
			}
		}

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

		protected static void ControlsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is PluginUserControlExtendBase obj && e.NewValue != e.OldValue)
			{
				obj.GameContextChanged(null, obj.GameContext);
			}
		}

		#endregion

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
			UpdateDataTimer.Tick += UpdateDataEvent;

			RegisterInstance();
			Unloaded += PluginUserControlExtendBase_Unloaded;
		}

		#region Instance Management

		private void RegisterInstance()
		{
			lock (_initLock)
			{
				_instances.Add(new WeakReference<PluginUserControlExtendBase>(this));
			}
		}

		/// <summary>
		/// Initializes static events once per concrete type.
		/// Derived types get their own initialization cycle, while base events (Games.ItemUpdated)
		/// are guarded by a separate flag to prevent double-subscription.
		/// </summary>
		protected void InitializeStaticEvents()
		{
			Type type = GetType();

			if (_typeEventsInitialized.ContainsKey(type))
			{
				return;
			}

			lock (_initLock)
			{
				if (_typeEventsInitialized.ContainsKey(type))
				{
					return;
				}

				AttachStaticEvents();
				_typeEventsInitialized[type] = true;
			}
		}

		/// <summary>
		/// Attaches plugin-specific event handlers once per plugin key.
		/// </summary>
		protected void AttachPluginEvents(string pluginKey, Action attachAction)
		{
			// Must be called inside the _initLock held by InitializeStaticEvents.
			if (!_pluginEventsAttached.ContainsKey(pluginKey))
			{
				attachAction?.Invoke();
				_pluginEventsAttached[pluginKey] = true;
			}
		}

		/// <summary>
		/// Override in derived classes to attach plugin-specific handlers via <see cref="AttachPluginEvents"/>.
		/// This method is always called inside <see cref="_initLock"/>.
		/// </summary>
		protected virtual void AttachStaticEvents()
		{
			if (!_baseEventsAttached)
			{
				API.Instance.Database.Games.ItemUpdated += OnStaticGamesItemUpdated;
				_baseEventsAttached = true;
			}
		}

		private static void OnStaticGamesItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
		{
			NotifyAllInstances(instance => instance.Games_ItemUpdated(sender, e));
		}

		protected static PropertyChangedEventHandler CreatePluginSettingsHandler()
		{
			return (sender, e) =>
			{
				NotifyAllInstances(instance => instance.PluginSettings_PropertyChanged(sender, e));
			};
		}

		/// <summary>
		/// Creates a type-safe database item update handler.
		/// The updated item IDs are extracted here — where TItem is known — eliminating reflection.
		/// </summary>
		protected static EventHandler<ItemUpdatedEventArgs<TItem>> CreateDatabaseItemUpdatedHandler<TItem>()
			where TItem : DatabaseObject
		{
			return (sender, e) =>
			{
				var updatedIds = new List<Guid>(e.UpdatedItems.Count);
				foreach (ItemUpdateEvent<TItem> item in e.UpdatedItems)
				{
					if (item.NewData?.Id != null && item.NewData.Id != Guid.Empty)
					{
						updatedIds.Add(item.NewData.Id);
					}
				}

				NotifyAllInstances(instance => instance.OnDatabaseItemUpdated(updatedIds));
			};
		}

		/// <summary>
		/// Creates a type-safe database collection change handler.
		/// </summary>
		protected static EventHandler<ItemCollectionChangedEventArgs<TItem>> CreateDatabaseCollectionChangedHandler<TItem>()
			where TItem : DatabaseObject
		{
			return (sender, e) =>
			{
				NotifyAllInstances(instance => instance.OnDatabaseCollectionChanged());
			};
		}

		/// <summary>
		/// Handles a database item update for this instance.
		/// Called on the UI thread via <see cref="NotifyAllInstances"/>.
		/// </summary>
		/// <param name="updatedIds">IDs of the updated database items.</param>
		protected virtual void OnDatabaseItemUpdated(List<Guid> updatedIds)
		{
			if (GameContext == null)
			{
				return;
			}

			if (updatedIds.Contains(GameContext.Id))
			{
				GameContextChanged(null, GameContext);
			}
		}

		/// <summary>
		/// Handles a database collection change for this instance.
		/// Called on the UI thread via <see cref="NotifyAllInstances"/>.
		/// </summary>
		protected virtual void OnDatabaseCollectionChanged()
		{
			if (GameContext != null)
			{
				GameContextChanged(null, GameContext);
			}
		}

		/// <summary>
		/// Dispatches an action to all alive instances, batched by dispatcher.
		/// Cleans up dead weak references under lock.
		/// </summary>
		protected static void NotifyAllInstances(Action<PluginUserControlExtendBase> action)
		{
			List<PluginUserControlExtendBase> alive;

			lock (_initLock)
			{
				alive = new List<PluginUserControlExtendBase>(_instances.Count);

				for (int i = _instances.Count - 1; i >= 0; i--)
				{
					if (_instances[i].TryGetTarget(out PluginUserControlExtendBase instance))
					{
						alive.Add(instance);
					}
					else
					{
						_instances.RemoveAt(i);
					}
				}
			}

			// Batch by dispatcher to minimise cross-thread overhead.
			var groups = alive.GroupBy(i => i.Dispatcher);
			foreach (var group in groups)
			{
				Dispatcher dispatcher = group.Key;
				List<PluginUserControlExtendBase> groupList = group.ToList();

				dispatcher?.BeginInvoke((Action)delegate
				{
					foreach (PluginUserControlExtendBase instance in groupList)
					{
						try
						{
							action(instance);
						}
						catch { }
					}
				}, DispatcherPriority.Background);
			}
		}

		/// <summary>
		/// Removes the current instance (and any dead references) from the tracking list on unload.
		/// </summary>
		private void PluginUserControlExtendBase_Unloaded(object sender, RoutedEventArgs e)
		{
			UpdateDataTimer.Stop();

			lock (_initLock)
			{
				for (int i = _instances.Count - 1; i >= 0; i--)
				{
					if (!_instances[i].TryGetTarget(out PluginUserControlExtendBase instance)
						|| ReferenceEquals(instance, this))
					{
						_instances.RemoveAt(i);
					}
				}
			}
		}

		#endregion

		#region Event Handlers

		protected virtual void PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			GameContextChanged(null, GameContext);
		}

		public override void GameContextChanged(Game oldContext, Game newContext)
		{
			CurrentGame = newContext;
			UpdateDataTimer.Stop();
			Visibility = Visibility.Collapsed;

			SetDefaultDataContext();

			MustDisplay = AlwaysShow || controlDataContext.IsActivated;

			if (!(controlDataContext?.IsActivated ?? false) || newContext == null || !MustDisplay)
			{
				return;
			}

			RestartTimer();
		}

		protected virtual void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
		{
			_ = API.Instance.MainView.UIDispatcher?.BeginInvoke(DispatcherPriority.Render, (Action)delegate
			{
				if (GameContext == null)
				{
					return;
				}

				ItemUpdateEvent<Game> updated = e.UpdatedItems.Find(x => x.NewData.Id == GameContext.Id);
				if (updated?.NewData != null)
				{
					GameContextChanged(null, updated.NewData);
				}
			});
		}

		#endregion

		public virtual void SetDefaultDataContext() { }

		private async void UpdateDataEvent(object sender, EventArgs e)
		{
			await UpdateDataAsync();
		}

		public virtual async Task UpdateDataAsync()
		{
			UpdateDataTimer.Stop();

			if (GameContext == null || CurrentGame == null || GameContext.Id != CurrentGame.Id)
			{
				Visibility = Visibility.Collapsed;
				return;
			}

			await API.Instance.MainView.UIDispatcher?.InvokeAsync(() =>
			{
				SetData(GameContext);
				Visibility = MustDisplay ? Visibility.Visible : Visibility.Collapsed;
			}, DispatcherPriority.Render);
		}

		/// <summary>Kept for backward compatibility with controls that override the single-argument form.</summary>
		public virtual void SetData(Game newContext) { }

		public void RestartTimer()
		{
			UpdateDataTimer.Stop();
			UpdateDataTimer.Start();
		}
	}
}