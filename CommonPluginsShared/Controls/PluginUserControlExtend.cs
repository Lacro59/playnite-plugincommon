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
	/// Database access is offloaded to a background thread to keep the UI responsive.
	/// </summary>
	public class PluginUserControlExtend : PluginUserControlExtendBase
	{
		protected virtual IPluginDatabase pluginDatabase { get; }

		protected void OnLoaded(object sender, RoutedEventArgs e)
		{
			InitializeStaticEvents();
			PluginSettings_PropertyChanged(null, null);
		}

		/// <summary>
		/// Sets the control data using the provided game and associated plugin data.
		/// Override in derived classes to apply game-specific UI logic.
		/// </summary>
		/// <param name="newContext">The current selected game.</param>
		/// <param name="pluginGameData">The plugin-specific data for the game.</param>
		public virtual void SetData(Game newContext, PluginDataBaseGameBase pluginGameData) { }

		/// <summary>
		/// Updates the control asynchronously. Database I/O runs on a background thread;
		/// the result is marshalled back to the UI thread for rendering.
		/// </summary>
		public override async Task UpdateDataAsync()
		{
			UpdateDataTimer.Stop();
			Visibility = MustDisplay ? Visibility.Visible : Visibility.Collapsed;

			if (GameContext == null || CurrentGame == null || GameContext.Id != CurrentGame.Id)
			{
				return;
			}

			// Capture before await — GameContext may change while awaiting.
			Game gameSnapshot = GameContext;
			Guid gameId = gameSnapshot.Id;

			// Run potentially slow I/O off the UI thread.
			PluginDataBaseGameBase pluginGameData = await Task.Run(() => pluginDatabase.Get(gameSnapshot, true));

			// Game context may have changed while the background task ran.
			if (GameContext == null || GameContext.Id != gameId)
			{
				return;
			}

			if (pluginGameData == null || !pluginGameData.HasData)
			{
				Visibility = AlwaysShow ? Visibility.Visible : Visibility.Collapsed;
				return;
			}

			SetData(GameContext, pluginGameData);
		}
	}
}