using System.Collections.Generic;

namespace CommonPluginsShared.Collections
{
	/// <summary>
	/// Abstract base class for plugin data with game details.
	/// Extends <see cref="PluginDataBaseGame{T}"/> and adds strongly-typed items details.
	/// </summary>
	/// <typeparam name="T">Type of the base game data.</typeparam>
	/// <typeparam name="Y">Type of the items details collection.</typeparam>
	public abstract class PluginDataBaseGameDetails<T, Y> : PluginDataBaseGame<T> where Y : new()
	{
		private Y _itemsDetails = default;

		/// <summary>
		/// Gets or sets the items details associated with the game.
		/// Lazy-initializes to a new instance if accessed while null.
		/// Setting this property triggers <see cref="OnItemsChanged"/> and refreshes cached values.
		/// </summary>
		public Y ItemsDetails
		{
			get
			{
				if (EqualityComparer<Y>.Default.Equals(_itemsDetails, default))
				{
					_itemsDetails = new Y();
				}
				return _itemsDetails;
			}
			set
			{
				SetValue(ref _itemsDetails, value);
				OnItemsChanged();
			}
		}
	}
}