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
		/// <summary>
		/// Upper bound for debounced updates. If <see cref="RestartTimer"/> keeps resetting the
		/// short delay timer, <see cref="_updateDeadlineTimer"/> still fires once per burst.
		/// </summary>
		private const int MaxUpdateDelayMs = 250;

		/// <summary>
		/// Coalesces rapid <see cref="INotifyPropertyChanged"/> bursts from plugin settings
		/// (many properties × many control instances) into a single refresh wave.
		/// </summary>
		private const int SettingsCoalesceDelayMs = 50;

		protected static readonly ILogger Logger = LogManager.GetLogger();
		protected virtual IDataContext controlDataContext { get; set; }
		protected DispatcherTimer UpdateDataTimer { get; set; }
		protected Game CurrentGame { get; set; }

		private readonly DispatcherTimer _updateDeadlineTimer;
		private int _scheduledUpdateGeneration;

		private static readonly object _initLock = new object();
		private static readonly Dictionary<Type, bool> _typeEventsInitialized = new Dictionary<Type, bool>();
		private static bool _baseEventsAttached = false;
		private static readonly Dictionary<string, bool> _pluginEventsAttached = new Dictionary<string, bool>();
		private static readonly List<WeakReference<PluginUserControlExtendBase>> _instances =
			new List<WeakReference<PluginUserControlExtendBase>>();

		private static readonly object _settingsCoalesceLock = new object();
		private static DispatcherTimer _settingsCoalesceTimer;

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
					obj.SetVisibility(obj.AlwaysShow ? Visibility.Visible : Visibility.Collapsed);
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
#if DEBUG
			var timer = new DebugTimer(GetType().Name + ".ctor");
#endif

			if (API.Instance?.ApplicationInfo?.Mode == ApplicationMode.Desktop)
			{
				ActiveViewAtCreation = API.Instance.MainView.ActiveDesktopView;
			}

#if DEBUG
			timer.Step("ActiveViewAtCreation resolved");
#endif

			UpdateDataTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(Delay)
			};
			UpdateDataTimer.Tick += UpdateDataEvent;

			_updateDeadlineTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(MaxUpdateDelayMs)
			};
			_updateDeadlineTimer.Tick += UpdateDeadlineEvent;

#if DEBUG
			timer.Step("DispatcherTimer created");
#endif

			RegisterInstance();
			Unloaded += PluginUserControlExtendBase_Unloaded;

#if DEBUG
			timer.Stop();
#endif
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
#if DEBUG
			var timer = new DebugTimer(GetType().Name + ".InitializeStaticEvents");
#endif

			Type type = GetType();
			if (_typeEventsInitialized.ContainsKey(type))
			{
#if DEBUG
				timer.Stop("already initialized, skip");
#endif
				return;
			}

			lock (_initLock)
			{
				if (_typeEventsInitialized.ContainsKey(type))
				{
#if DEBUG
					timer.Stop("already initialized (inside lock), skip");
#endif
					return;
				}

#if DEBUG
				timer.Step("calling AttachStaticEvents");
#endif

				AttachStaticEvents();
				_typeEventsInitialized[type] = true;
			}

#if DEBUG
			timer.Stop();
#endif
		}

		/// <summary>
		/// Attaches plugin-specific event handlers once per plugin key.
		/// </summary>
		protected void AttachPluginEvents(string pluginKey, Action attachAction)
		{
			if (!_pluginEventsAttached.ContainsKey(pluginKey))
			{
				attachAction?.Invoke();
				_pluginEventsAttached[pluginKey] = true;
			}
		}

		/// <summary>
		/// Override in derived classes to attach plugin-specific handlers via <see cref="AttachPluginEvents"/>.
		/// This method is always called inside <see cref="InitializeStaticEvents"/>.
		/// </summary>
		protected virtual void AttachStaticEvents()
		{
			// API.Instance is null in the VS Designer — skip all event wiring.
			if (DesignerProperties.GetIsInDesignMode(this))
			{
				return;
			}

#if DEBUG
			var timer = new DebugTimer(GetType().Name + ".AttachStaticEvents");
#endif

			if (!_baseEventsAttached)
			{
				API.Instance.Database.Games.ItemUpdated += OnStaticGamesItemUpdated;
				_baseEventsAttached = true;
#if DEBUG
				timer.Step("Games.ItemUpdated attached");
#endif
			}

#if DEBUG
			timer.Stop();
#endif
		}

		private static void OnStaticGamesItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
		{
			NotifyAllInstances(instance => instance.Games_ItemUpdated(sender, e));
		}

		protected static PropertyChangedEventHandler CreatePluginSettingsHandler()
		{
			return (sender, e) =>
			{
				ScheduleCoalescedSettingsRefresh();
			};
		}

		/// <summary>
		/// Coalesces plugin settings <see cref="PropertyChanged"/> notifications before refreshing controls.
		/// </summary>
		private static void ScheduleCoalescedSettingsRefresh()
		{
			Dispatcher dispatcher = Application.Current?.Dispatcher;
			if (dispatcher == null)
			{
				return;
			}

			lock (_settingsCoalesceLock)
			{
				if (_settingsCoalesceTimer == null)
				{
					_settingsCoalesceTimer = new DispatcherTimer
					{
						Interval = TimeSpan.FromMilliseconds(SettingsCoalesceDelayMs)
					};
					_settingsCoalesceTimer.Tick += OnSettingsCoalesceTick;
				}

				_settingsCoalesceTimer.Stop();
				_settingsCoalesceTimer.Start();
			}
		}

		private static void OnSettingsCoalesceTick(object sender, EventArgs e)
		{
			lock (_settingsCoalesceLock)
			{
				_settingsCoalesceTimer?.Stop();
			}

			NotifyAllInstances(instance => instance.ApplyCoalescedSettingsRefresh());
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
		protected virtual void OnDatabaseItemUpdated(List<Guid> updatedIds)
		{
			if (GameContext == null)
			{
				return;
			}

			if (updatedIds.Contains(GameContext.Id))
			{
				ScheduleDataRefresh("database-item-updated");
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
				ScheduleDataRefresh("database-collection-changed");
			}
		}

		/// <summary>
		/// Dispatches an action to all alive instances.
		/// Fast-path: if all instances share the same dispatcher (typical in Playnite Desktop),
		/// skips LINQ GroupBy and dispatches in a single BeginInvoke.
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

			if (alive.Count == 0)
			{
				return;
			}

			Dispatcher sharedDispatcher = alive[0].Dispatcher;
			bool allSameDispatcher = true;
			for (int i = 1; i < alive.Count; i++)
			{
				if (!ReferenceEquals(alive[i].Dispatcher, sharedDispatcher))
				{
					allSameDispatcher = false;
					break;
				}
			}

			if (allSameDispatcher)
			{
				sharedDispatcher.BeginInvoke((Action)delegate
				{
					foreach (PluginUserControlExtendBase instance in alive)
					{
						try
						{
							action(instance);
						}
						catch (Exception ex)
						{
							instance.LogNotifyAllInstancesFailure(ex);
						}
					}
				}, DispatcherPriority.Background);
			}
			else
			{
				// Fallback: multi-dispatcher edge case
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
							catch (Exception ex)
							{
								instance.LogNotifyAllInstancesFailure(ex);
							}
						}
					}, DispatcherPriority.Background);
				}
			}
		}

		/// <summary>
		/// Removes the current instance (and any dead references) from the tracking list on unload.
		/// </summary>
		private void PluginUserControlExtendBase_Unloaded(object sender, RoutedEventArgs e)
		{
			StopUpdateTimers();
			LogControlTrace("Unloaded");
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
			ApplyCoalescedSettingsRefresh();
		}

		/// <summary>
		/// Applies plugin settings to the control and schedules a debounced data refresh.
		/// </summary>
		private void ApplyCoalescedSettingsRefresh()
		{
			SetDefaultDataContext();
			ScheduleDataRefresh("settings-changed");
		}

		public override void GameContextChanged(Game oldContext, Game newContext)
		{
#if DEBUG
			var timer = new DebugTimer(string.Format("{0}.GameContextChanged(game='{1}')", GetInstanceDiagnosticId(), newContext?.Name ?? "null"));
#endif

			// Cancel any in-flight update for the previous game
			CancelPendingUpdate();

#if DEBUG
			timer.Step("CancelPendingUpdate done");
#endif

			bool isContextSwitch = CurrentGame?.Id != newContext?.Id;
			CurrentGame = newContext;
			// Stop only the short debounce timer — do not stop the deadline timer here or
			// bursts of GameContextChanged (virtualized list, NotifyAllInstances) prevent it from ever firing.
			UpdateDataTimer.Stop();

			// Reset defaults only on actual game switch — re-arms for the same game
			// (triggered by data/settings changes) skip the reset to avoid redundant
			// UI flicker and unnecessary work while the timer debounces the burst.
			if (isContextSwitch)
			{
				SetVisibility(Visibility.Collapsed);
				SetDefaultDataContext();
#if DEBUG
				timer.Step("SetDefaultDataContext done (context switch)");
#endif

				// Leading edge: show data as soon as the UI thread can, without waiting for debounce/settings noise.
				if (newContext != null)
				{
					Guid targetGameId = newContext.Id;
					Dispatcher.BeginInvoke((Action)(async () =>
					{
						if (CurrentGame?.Id == targetGameId && GameContext?.Id == targetGameId)
						{
							await RunScheduledUpdateAsync("context-switch-immediate");
						}
					}), DispatcherPriority.Loaded);
				}
			}
#if DEBUG
			else
			{
				timer.Step("SetDefaultDataContext skipped (same game)");
			}
#endif

			MustDisplay = AlwaysShow || controlDataContext.IsActivated;

			if (!(controlDataContext?.IsActivated ?? false) || newContext == null || !MustDisplay)
			{
#if DEBUG
				timer.Stop("early exit (not activated or no context)");
#endif
				return;
			}

			// Virtualized list views fire GameContextChanged repeatedly for the selected game
			// without changing the id — skip when the control is already showing that game.
			if (!isContextSwitch && IsSameGameAlreadyDisplayed(newContext))
			{
#if DEBUG
				timer.Stop("early exit (same game, already visible)");
#endif
				return;
			}

			RestartTimer(isContextSwitch ? "context-switch" : "context-refresh");

#if DEBUG
			timer.Stop("timer restarted");
#endif
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
					CurrentGame = updated.NewData;
					ScheduleDataRefresh("playnite-game-updated");
				}
			});
		}

		#endregion

		public virtual void SetDefaultDataContext() { }

		/// <summary>
		/// Cancels any in-flight <see cref="UpdateDataAsync"/> for the current instance.
		/// Must be called on the UI thread (before restarting the timer).
		/// </summary>
		protected virtual void CancelPendingUpdate() { }

		/// <summary>
		/// Sets <see cref="UIElement.Visibility"/> only when the value actually changes,
		/// avoiding spurious WPF layout invalidations.
		/// </summary>
		protected void SetVisibility(Visibility value)
		{
			if (Visibility != value)
			{
				LogControlTrace("Visibility", string.Format("{0} -> {1}", Visibility, value));
				Visibility = value;
			}
		}

		private async void UpdateDataEvent(object sender, EventArgs e)
		{
			await RunScheduledUpdateAsync("debounce-timer");
		}

		private async void UpdateDeadlineEvent(object sender, EventArgs e)
		{
			_updateDeadlineTimer.Stop();
			LogControlIssue("Anti-starvation deadline fired (debounce timer did not complete in time)");
			await RunScheduledUpdateAsync("deadline-timer");
		}

		private async Task RunScheduledUpdateAsync(string source)
		{
			int generationAtStart = _scheduledUpdateGeneration;
			LogControlTrace("Update scheduled", string.Format("source={0}, generation={1}, game='{2}'",
				source,
				generationAtStart,
				GameContext?.Name ?? "null"));

			try
			{
				await UpdateDataAsync();

				if (generationAtStart != _scheduledUpdateGeneration)
				{
					LogControlTrace("Update finished but generation advanced",
						string.Format("started={0}, current={1}", generationAtStart, _scheduledUpdateGeneration));
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, string.Format("{0} UpdateDataAsync failed (source={1}, generation={2})",
					GetInstanceDiagnosticId(),
					source,
					generationAtStart));
			}
		}

		public virtual async Task UpdateDataAsync()
		{
#if DEBUG
			var timer = new DebugTimer(GetInstanceDiagnosticId() + ".UpdateDataAsync(base)");
#endif

			StopUpdateTimers();

			if (GameContext == null || CurrentGame == null || GameContext.Id != CurrentGame.Id)
			{
				SetVisibility(Visibility.Collapsed);
				LogControlIssue(string.Format("UpdateDataAsync aborted: context mismatch (GameContext={0}, CurrentGame={1})",
					FormatGameId(GameContext),
					FormatGameId(CurrentGame)));
#if DEBUG
				timer.Stop("early exit (context mismatch)");
#endif
				return;
			}

			await API.Instance.MainView.UIDispatcher?.InvokeAsync(() =>
			{
				SetData(GameContext);
				SetVisibility(MustDisplay ? Visibility.Visible : Visibility.Collapsed);
			}, DispatcherPriority.Render);

			LogControlTrace("UpdateDataAsync(base) completed", string.Format("visibility={0}", Visibility));

#if DEBUG
			timer.Stop();
#endif
		}

		/// <summary>Kept for backward compatibility with controls that override the single-argument form.</summary>
		public virtual void SetData(Game newContext) { }

		/// <summary>
		/// Schedules a debounced data refresh without running the full
		/// <see cref="GameContextChanged"/> collapse/reset path.
		/// </summary>
		/// <param name="reason">Caller label for diagnostics.</param>
		protected void ScheduleDataRefresh(string reason)
		{
			MustDisplay = AlwaysShow || (controlDataContext?.IsActivated ?? false);

			if (!(controlDataContext?.IsActivated ?? false) || GameContext == null || !MustDisplay)
			{
				return;
			}

			RestartTimer(reason);
		}

		/// <summary>
		/// Returns whether the control is already bound to <paramref name="newContext"/> and visible.
		/// </summary>
		protected bool IsSameGameAlreadyDisplayed(Game newContext)
		{
			return newContext != null
				&& GameContext != null
				&& newContext.Id == GameContext.Id
				&& CurrentGame != null
				&& newContext.Id == CurrentGame.Id
				&& Visibility == Visibility.Visible;
		}

		public void RestartTimer()
		{
			RestartTimer(null);
		}

		/// <summary>
		/// Restarts the debounced update timer and arms a sliding deadline timer.
		/// </summary>
		/// <param name="reason">Optional caller label for diagnostics.</param>
		protected void RestartTimer(string reason)
		{
			_scheduledUpdateGeneration++;
			UpdateDataTimer.Stop();
			UpdateDataTimer.Start();

			// Only slide the deadline on game-context work — settings/database bursts must not delay display.
			bool slideDeadline = reason == "context-switch" || reason == "context-refresh";
			if (slideDeadline)
			{
				_updateDeadlineTimer.Stop();
				_updateDeadlineTimer.Start();
			}
			else if (!_updateDeadlineTimer.IsEnabled)
			{
				_updateDeadlineTimer.Start();
			}

			LogControlTrace("RestartTimer", string.Format("reason={0}, generation={1}, delay={2}ms, deadline={3}ms, slideDeadline={4}",
				reason ?? "unspecified",
				_scheduledUpdateGeneration,
				Delay,
				MaxUpdateDelayMs,
				slideDeadline));
		}

		/// <summary>Stops debounce and anti-starvation timers.</summary>
		protected void StopUpdateTimers()
		{
			UpdateDataTimer?.Stop();
			_updateDeadlineTimer?.Stop();
		}

		/// <summary>Stable per-instance id for diagnostic logs (type name + hash code).</summary>
		protected string GetInstanceDiagnosticId()
		{
			return string.Format("{0}#{1:X8}", GetType().Name, GetHashCode());
		}

		/// <summary>Verbose trace — DEBUG builds only, marked as ignored in the log file.</summary>
		protected void LogControlTrace(string phase, string detail = null)
		{
			string message = detail == null
				? string.Format("[{0}] {1}", GetInstanceDiagnosticId(), phase)
				: string.Format("[{0}] {1} — {2}", GetInstanceDiagnosticId(), phase, detail);
			Common.LogDebug(true, message);
		}

		/// <summary>
		/// Important diagnostic — logged in DEBUG and Release at debug level without the [Ignored] prefix.
		/// </summary>
		protected void LogControlIssue(string message)
		{
			Common.LogDebug(false, string.Format("[{0}] {1}", GetInstanceDiagnosticId(), message));
		}

		private void LogNotifyAllInstancesFailure(Exception ex)
		{
			Logger.Error(ex, string.Format("{0} NotifyAllInstances handler failed", GetInstanceDiagnosticId()));
		}

		protected static string FormatGameId(Game game)
		{
			if (game == null)
			{
				return "null";
			}

			return string.Format("'{0}' ({1})", game.Name ?? "?", game.Id);
		}
	}
}