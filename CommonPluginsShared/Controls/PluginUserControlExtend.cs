using CommonPluginsShared.Collections;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Threading;
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
	/// Heavy per-game computation (e.g. benchmark lookups) is offloaded via <see cref="SetDataAsync"/>.
	/// </summary>
	public abstract class PluginUserControlExtend : PluginUserControlExtendBase
	{
		protected abstract IPluginDatabase pluginDatabase { get; }

		private CancellationTokenSource _updateCts;

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
		/// Cancels the in-flight <see cref="UpdateDataAsync"/> CTS for this instance.
		/// Called from <see cref="PluginUserControlExtendBase.GameContextChanged"/> before restarting the timer.
		/// </summary>
		protected override void CancelPendingUpdate()
		{
			_updateCts?.Cancel();
			_updateCts?.Dispose();
			_updateCts = null;
		}

		/// <summary>
		/// Synchronous fallback. Override <see cref="SetDataAsync"/> instead when
		/// computation is expensive — this overload is only kept for backward compatibility
		/// with controls that have not yet migrated.
		/// </summary>
		/// <param name="newContext">The current selected game.</param>
		/// <param name="pluginGameData">The plugin-specific data for the game.</param>
		public virtual void SetData(Game newContext, PluginDataBaseGameBase pluginGameData) { }

		/// <summary>
		/// Async entry point for game-specific UI updates.
		/// Override this in derived classes to offload heavy computation (e.g. benchmark lookups)
		/// to a background thread via <see cref="Task.Run"/>, then marshal only the final
		/// UI mutation back to the UI thread.
		/// Default implementation delegates to the synchronous <see cref="SetData(Game, PluginDataBaseGameBase)"/> overload.
		/// </summary>
		/// <param name="newContext">The current selected game.</param>
		/// <param name="pluginGameData">The plugin-specific data for the game.</param>
		/// <param name="cancellationToken">Token to observe for cancellation.</param>
		public virtual Task SetDataAsync(Game newContext, PluginDataBaseGameBase pluginGameData, CancellationToken cancellationToken)
		{
			SetData(newContext, pluginGameData);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Updates the control. The session cache lookup is synchronous and sub-millisecond
		/// after pre-warm — no <see cref="Task.Run"/> overhead.
		/// For games absent from the session cache a background fetch is queued without
		/// blocking the UI thread.
		/// Heavy per-game computation is delegated to <see cref="SetDataAsync"/> which
		/// derived classes can override to run off the UI thread.
		/// In-flight updates are cancelled via <see cref="CancellationToken"/> when the game context changes.
		/// </summary>
		public override async Task UpdateDataAsync()
		{
#if DEBUG
			var timer = new DebugTimer(GetType().Name + ".UpdateDataAsync");
#endif

			UpdateDataTimer.Stop();

			_updateCts = new CancellationTokenSource();
			CancellationToken cancellationToken = _updateCts.Token;

			if (GameContext == null || CurrentGame == null || GameContext.Id != CurrentGame.Id)
			{
				SetVisibility(Visibility.Collapsed);
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

			PluginDataBaseGameBase pluginGameData = pluginDatabase.GetOnlyCache(gameSnapshot);

#if DEBUG
			timer.Step(string.Format("cache lookup done, hasData={0}", pluginGameData?.HasData));
#endif

			if (cancellationToken.IsCancellationRequested || GameContext == null || GameContext.Id != gameId)
			{
#if DEBUG
				timer.Stop("cancelled or context changed during lookup, abort");
#endif
				return;
			}

			if (pluginGameData == null)
			{
				SetVisibility(AlwaysShow ? Visibility.Visible : Visibility.Collapsed);
#if DEBUG
				timer.Stop(string.Format("no entry, visibility={0}", Visibility));
#endif
				return;
			}

			if (!pluginGameData.HasData)
			{
				SetVisibility(AlwaysShow ? Visibility.Visible : Visibility.Collapsed);
#if DEBUG
				timer.Stop(string.Format("no data, visibility={0}", Visibility));
#endif
				return;
			}

			SetVisibility(MustDisplay ? Visibility.Visible : Visibility.Collapsed);

#if DEBUG
			timer.Step("calling SetDataAsync");
#endif

			await SetDataAsync(gameSnapshot, pluginGameData, cancellationToken);

#if DEBUG
			timer.Stop();
#endif
		}
	}
}