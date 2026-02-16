using System.Collections.Generic;

namespace CommonPluginsShared.Collections
{
	/// <summary>
	/// Extends <see cref="PluginDataBaseGame{T}"/> with an additional details object.
	/// This is typically used when a game has both a flat collection of items (T)
	/// and a richer aggregated/details structure (Y).
	/// </summary>
	/// <typeparam name="T">Type of the primary items stored for the game.</typeparam>
	/// <typeparam name="Y">Type of the additional details data.</typeparam>
	public abstract class PluginDataBaseGameDetails<T, Y> : PluginDataBaseGame<T>
	{
		private Y _itemsDetails = default;

		/// <summary>
		/// Gets or sets the details data associated with the game.
		/// Setting this property triggers <see cref="PluginDataBaseGame{T}.OnItemsChanged"/> and refreshes cached values.
		/// </summary>
		public Y ItemsDetails
		{
			get
			{
				return EqualityComparer<Y>.Default.Equals(_itemsDetails, default) ? default : _itemsDetails;
			}
			set
			{
				SetValue(ref _itemsDetails, value);
				OnItemsChanged();
			}
		}
	}
}