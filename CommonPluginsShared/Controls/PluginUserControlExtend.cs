using CommonPluginsShared.Collections;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace CommonPluginsShared.Controls
{
	/// <summary>
	/// Extended user control base class that integrates with a plugin database.
	/// Automatically handles data retrieval and UI updates based on the current game context.
	/// Cache-only lookup runs synchronously on the UI thread — no ThreadPool overhead.
	/// Background web fetch is fired-and-forgotten for games absent from the session cache.
	/// </summary>
	public abstract class PluginUserControlExtend : PluginUserControlExtendBase
	{
		protected abstract IPluginDatabase pluginDatabase { get; }

		protected void OnLoaded(object sender, RoutedEventArgs e)
		{
#if DEBUG
			var timer = new DebugTimer(GetType().Name + ".OnLoaded");
#endif

			InitializeStaticEvents();

#if DEBUG
			timer.Step("InitializeStaticEvents done");
#endif

			PluginSettings_PropertyChanged(null, null);

#if DEBUG
			timer.Stop();
#endif
		}

		/// <summary>
		/// Sets the control data using the provided game and associated plugin data.
		/// Override in derived classes to apply game-specific UI logic.
		/// </summary>
		/// <param name="newContext">The current selected game.</param>
		/// <param name="pluginGameData">The plugin-specific data for the game.</param>
		public virtual void SetData(Game newContext, PluginDataBaseGameBase pluginGameData) { }

		/// <summary>
		/// Updates the control. The session cache lookup is synchronous and sub-millisecond
		/// after pre-warm — no <see cref="Task.Run"/> overhead.
		/// For games absent from the session cache a background fetch is queued without
		/// blocking the UI thread.
		/// </summary>
		public override async Task UpdateDataAsync()
		{
#if DEBUG
			var timer = new DebugTimer(GetType().Name + ".UpdateDataAsync");
#endif

			UpdateDataTimer.Stop();

			if (GameContext == null || CurrentGame == null || GameContext.Id != CurrentGame.Id)
			{
				Visibility = Visibility.Collapsed;
#if DEBUG
				timer.Stop("early exit (context mismatch)");
#endif
				return;
			}

			Game gameSnapshot = GameContext;
			Guid gameId = gameSnapshot.Id;

#if DEBUG
			timer.Step(string.Format("cache lookup for game='{0}'", gameSnapshot.Name));
#endif

			// Sub-millisecond ConcurrentDictionary hit after pre-warm.
			// No Task.Run — avoids ThreadPool starvation with many simultaneous controls.
			PluginDataBaseGameBase pluginGameData = pluginDatabase.GetOnlyCache(gameSnapshot);

#if DEBUG
			timer.Step(string.Format("cache lookup done, hasData={0}", pluginGameData?.HasData));
#endif

			if (GameContext == null || GameContext.Id != gameId)
			{
#if DEBUG
				timer.Stop("context changed during lookup, abort");
#endif
				return;
			}

			if (pluginGameData == null)
			{
				Visibility = AlwaysShow ? Visibility.Visible : Visibility.Collapsed;

				// Fire-and-forget web fetch for games not yet in the session cache.
				// Does not block the UI thread; DatabaseItemUpdated will trigger a redraw
				// once the fetch completes and the item is upserted.
				Guid capturedId = gameId;
				_ = Task.Run(() =>
				{
					try
					{
						pluginDatabase.Get(capturedId, onlyCache: false);
					}
					catch (Exception ex)
					{
						Logger.Warn(string.Format(
							"Background fetch failed for {0}: {1}", capturedId, ex.Message));
					}
				});

#if DEBUG
				timer.Stop("no cache entry — background fetch queued");
#endif
				return;
			}

			if (!pluginGameData.HasData)
			{
				Visibility = AlwaysShow ? Visibility.Visible : Visibility.Collapsed;
#if DEBUG
				timer.Stop(string.Format("no data, visibility={0}", Visibility));
#endif
				return;
			}

			Visibility = MustDisplay ? Visibility.Visible : Visibility.Collapsed;

#if DEBUG
			timer.Step("calling SetData");
#endif

			// SetData executes on the UI thread — correct because UpdateDataAsync
			// is invoked from DispatcherTimer.Tick which fires on the UI thread.
			SetData(GameContext, pluginGameData);

#if DEBUG
			timer.Stop();
#endif

			await Task.CompletedTask;
		}
	}
}