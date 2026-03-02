using System.Collections.Generic;

namespace CommonPluginsShared.Collections
{
	/// <summary>
	/// Abstract base class for plugin game data that stores both a typed list of items
	/// and an associated details object.
	/// Extends <see cref="PluginGameCollection{T}"/> with a lazily-initialised details payload.
	/// </summary>
	/// <typeparam name="T">Type of items stored in <see cref="PluginGameCollection{T}.Items"/>.</typeparam>
	/// <typeparam name="TDetails">Type of the details object. Must have a public parameterless constructor.</typeparam>
	public abstract class PluginGameCollectionWithDetails<T, TDetails> : PluginGameCollection<T>
		where TDetails : new()
	{
		private TDetails _itemsDetails = default;

		/// <summary>
		/// Gets or sets the details object associated with the game.
		/// Lazy-initialises to a new <typeparamref name="TDetails"/> instance when accessed while null/default.
		/// Setting this property invalidates the HasData and Count caches via <see cref="PluginGameCollection{T}.OnItemsChanged"/>.
		/// </summary>
		public TDetails ItemsDetails
		{
			get
			{
				if (EqualityComparer<TDetails>.Default.Equals(_itemsDetails, default))
				{
					_itemsDetails = new TDetails();
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