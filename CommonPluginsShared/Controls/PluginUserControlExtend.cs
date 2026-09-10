using CommonPluginsShared.Collections;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.ComponentModel;
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
			if (DesignerProperties.GetIsInDesignMode(this))
			{
				return;
			}

#if DEBUG
			var timer = new DebugTimer(GetType().Name + ".OnLoaded");
#endif

			EnsureRegistered();

#if DEBUG
			timer.Step("EnsureRegistered done");
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
		/// Attaches shared plugin-database handlers for all <see cref="PluginUserControlExtend"/> derivatives,
		/// including post-batch UI refresh via <see cref="IPluginDatabase.BatchRefreshCompleted"/>.
		/// </summary>
		protected override void AttachStaticEvents()
		{
			base.AttachStaticEvents();

			if (DesignerProperties.GetIsInDesignMode(this) || pluginDatabase == null)
			{
				return;
			}

			AttachPluginEvents(pluginDatabase.PluginName + ".BatchRefreshCompleted", () =>
			{
				pluginDatabase.BatchRefreshCompleted += CreateBatchRefreshCompletedHandler();
			});
		}

		/// <summary>
		/// Synchronous fallback. Override <see cref="SetDataAsync"/> instead when
		/// computation is expensive — this overload is only kept for backward compatibility
		/// with controls that have not yet migrated.
		/// </summary>
		/// <param name="newContext">The current selected game.</param>
		/// <param name="pluginGameData">The plugin-specific data for the game.</param>
		public virtual void SetData(Game newContext, PluginGameEntry pluginGameData) { }

		/// <summary>
		/// Async entry point for game-specific UI updates.
		/// Override this in derived classes to offload heavy computation (e.g. benchmark lookups)
		/// to a background thread via <see cref="Task.Run"/>, then marshal only the final
		/// UI mutation back to the UI thread.
		/// Default implementation delegates to the synchronous <see cref="SetData(Game, PluginGameEntry)"/> overload.
		/// </summary>
		/// <param name="newContext">The current selected game.</param>
		/// <param name="pluginGameData">The plugin-specific data for the game.</param>
		/// <param name="cancellationToken">Token to observe for cancellation.</param>
		public virtual Task SetDataAsync(Game newContext, PluginGameEntry pluginGameData, CancellationToken cancellationToken)
		{
			SetData(newContext, pluginGameData);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Clears visible media when the session cache has no entry for the current game.
		/// Override in controls that keep bitmap/video layers between updates.
		/// </summary>
		/// <param name="gameContext">The current selected game.</param>
		/// <param name="cancellationToken">Token to observe for cancellation.</param>
		/// <remarks>
		/// Default is a no-op. Called from <see cref="UpdateDataAsync"/> before visibility is applied.
		/// Attention: plugins that retain Image/Video layers across games should override and clear them;
		/// otherwise the previous game's media can remain visible when the new game has no cache entry.
		/// </remarks>
		protected virtual Task OnNoPluginCacheEntryAsync(Game gameContext, CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Recalculates <see cref="PluginUserControlExtendBase.MustDisplay"/> from activation and
		/// whether the current game has displayable plugin data, then applies visibility.
		/// </summary>
		/// <param name="hasDisplayableData">
		/// <c>true</c> when the session cache has usable data for the current game
		/// (typically <see cref="PluginGameEntry.HasData"/>); <c>false</c> on cache miss or empty entry.
		/// </param>
		/// <remarks>
		/// Contract: <c>MustDisplay = AlwaysShow || (IsActivated &amp;&amp; hasDisplayableData)</c>.
		/// Controls with <see cref="PluginUserControlExtendBase.AlwaysShow"/> (e.g. theme buttons that
		/// open a search when empty) stay visible without data; ProgressBar / ViewItem collapse.
		/// Call after cache lookup / <see cref="SetDataAsync"/> so visibility matches real data.
		/// </remarks>
		protected void RefreshMustDisplayFromData(bool hasDisplayableData)
		{
			bool isActivated = controlDataContext?.IsActivated ?? false;
			MustDisplay = AlwaysShow || (isActivated && hasDisplayableData);
			ApplyVisibilityFromMustDisplay();
		}

		/// <summary>
		/// Applies <see cref="UIElement.Visibility"/> from <see cref="PluginUserControlExtendBase.MustDisplay"/>
		/// and <see cref="PluginUserControlExtendBase.AlwaysShow"/> after <see cref="SetDataAsync"/> may have
		/// refreshed in-memory plugin data (e.g. Playnite default media mirrors).
		/// </summary>
		/// <remarks>
		/// Uses <c>AlwaysShow || MustDisplay</c>. Prefer <see cref="RefreshMustDisplayFromData"/> after
		/// cache lookup so <see cref="PluginUserControlExtendBase.MustDisplay"/> reflects HasData.
		/// </remarks>
		protected void ApplyVisibilityFromMustDisplay()
		{
			SetVisibility(AlwaysShow || MustDisplay ? Visibility.Visible : Visibility.Collapsed);
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
		/// <remarks>
		/// Behavioral contract shared by all plugins using this control (breaking vs older early-return):
		/// <list type="bullet">
		/// <item>
		/// <description>
		/// Cache miss (<c>pluginGameData == null</c>): calls <see cref="OnNoPluginCacheEntryAsync"/> then
		/// <see cref="RefreshMustDisplayFromData"/> with <c>hasDisplayableData=false</c> — does not call
		/// <see cref="SetDataAsync"/>.
		/// </description>
		/// </item>
		/// <item>
		/// <description>
		/// Cache hit with <c>!HasData</c>: still calls <see cref="SetDataAsync"/> so derived controls can
		/// clear/reset UI. Older code returned early without SetData — overrides must tolerate empty entries
		/// (guard on HasData / null collections) and must not assume data is always present.
		/// </description>
		/// </item>
		/// <item>
		/// <description>
		/// Visibility after SetData is driven by <see cref="RefreshMustDisplayFromData"/>:
		/// <c>AlwaysShow || (IsActivated &amp;&amp; HasData)</c>. Without data, non-AlwaysShow controls collapse
		/// (ProgressBar / ViewItem); AlwaysShow controls (e.g. PluginButton) stay visible.
		/// </description>
		/// </item>
		/// </list>
		/// Smoke other plugins after publishing this submodule: game without plugin data → game with data.
		/// </remarks>
		public override async Task UpdateDataAsync()
		{
#if DEBUG
			var timer = new DebugTimer(GetInstanceDiagnosticId() + ".UpdateDataAsync");
#endif

			StopUpdateTimers();

			_updateCts = new CancellationTokenSource();
			CancellationToken cancellationToken = _updateCts.Token;

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

			Game gameSnapshot = GameContext;
			Guid gameId = gameSnapshot.Id;

			if (pluginDatabase.FilterSettings != null
				&& !PlayniteTools.ShouldIncludeLibraryGame(gameSnapshot, pluginDatabase.FilterSettings))
			{
				PlayniteTools.LogLibraryFilterExclusion(
					string.Format("{0}.PluginUserControl", pluginDatabase.PluginName),
					gameSnapshot,
					PlayniteTools.GetLibraryFilterExclusionReason(gameSnapshot, pluginDatabase.FilterSettings));
				SetVisibility(Visibility.Collapsed);
				return;
			}

#if DEBUG
			timer.Step(string.Format("cache lookup for game='{0}'", gameSnapshot.Name));
#endif

			PluginGameEntry pluginGameData = pluginDatabase.GetOnlyCache(gameSnapshot);

#if DEBUG
			timer.Step(string.Format("cache lookup done, hasData={0}", pluginGameData?.HasData));
#endif

			if (cancellationToken.IsCancellationRequested || GameContext == null || GameContext.Id != gameId)
			{
				LogControlIssue(string.Format("UpdateDataAsync aborted: cancelled or context changed during lookup (gameId={0})", gameId));
#if DEBUG
				timer.Stop("cancelled or context changed during lookup, abort");
#endif
				return;
			}

			if (pluginGameData == null)
			{
				LogControlIssue(string.Format("UpdateDataAsync: no cache entry for '{0}'",
					gameSnapshot.Name));

				if (cancellationToken.IsCancellationRequested || GameContext == null || GameContext.Id != gameId)
				{
					LogControlIssue(string.Format("UpdateDataAsync aborted: cancelled or context changed before no-entry reset (gameId={0})", gameId));
#if DEBUG
					timer.Stop("cancelled or context changed before no-entry reset, abort");
#endif
					return;
				}

				// Hook for media controls; default no-op — other plugins can ignore.
				await OnNoPluginCacheEntryAsync(gameSnapshot, cancellationToken);
				RefreshMustDisplayFromData(hasDisplayableData: false);
#if DEBUG
				timer.Stop(string.Format("no entry, visibility={0}", Visibility));
#endif
				return;
			}

			// Do not early-return: SetDataAsync must still run so derived controls can clear UI.
			// Overrides that assumed HasData == true must be hardened (shared across plugins).
			bool hasDisplayableData = pluginGameData.HasData;
			if (!hasDisplayableData)
			{
				LogControlIssue(string.Format("UpdateDataAsync: cache entry without data for '{0}' (stale HasData cache possible)",
					gameSnapshot.Name));
			}

			if (cancellationToken.IsCancellationRequested || GameContext == null || GameContext.Id != gameId)
			{
				LogControlIssue(string.Format("UpdateDataAsync aborted: cancelled or context changed before SetData (gameId={0})", gameId));
#if DEBUG
				timer.Stop("cancelled or context changed before SetData, abort");
#endif
				return;
			}

#if DEBUG
			timer.Step("calling SetDataAsync");
#endif

			await SetDataAsync(gameSnapshot, pluginGameData, cancellationToken);

			RefreshMustDisplayFromData(hasDisplayableData);

			LogControlTrace("UpdateDataAsync completed", string.Format("game='{0}', visibility={1}", gameSnapshot.Name, Visibility));

#if DEBUG
			timer.Stop();
#endif
		}
	}
}