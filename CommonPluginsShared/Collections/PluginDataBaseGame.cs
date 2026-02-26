using Playnite.SDK.Data;
using System.Collections.Generic;

namespace CommonPluginsShared.Collections
{
	/// <summary>
	/// Base class for plugin game data that stores a typed list of items associated
	/// with a Playnite game. Provides cached access to item count and data presence.
	/// </summary>
	/// <typeparam name="T">Type of items stored for the game.</typeparam>
	public abstract class PluginDataBaseGame<T> : PluginDataBaseGameBase
	{
		private List<T> _items = new List<T>();

		/// <summary>
		/// Gets or sets the list of items associated with the game.
		/// Setting this property invalidates the HasData and Count caches.
		/// </summary>
		public List<T> Items
		{
			get => _items ?? new List<T>();
			set
			{
				SetValue(ref _items, value);
				OnItemsChanged();
			}
		}

		/// <summary>
		/// Indicates if there is any data in Items list.
		/// Uses cached value for better performance.
		/// </summary>
		[DontSerialize]
		public override bool HasData
		{
			get
			{
				if (!_hasData.HasValue)
				{
					_hasData = _items != null && _items.Count > 0;
				}
				return _hasData.Value;
			}
		}

		/// <summary>
		/// Returns the count of items as ulong.
		/// Uses cached value for better performance.
		/// </summary>
		[DontSerialize]
		public override ulong Count
		{
			get
			{
				if (!_count.HasValue)
				{
					_count = (ulong)(_items?.Count ?? 0);
				}
				return _count.Value;
			}
		}

		/// <summary>
		/// Called when the Items list changes. Delegates cache invalidation to
		/// <see cref="PluginDataBaseGameBase.RefreshCachedValues"/>.
		/// Override in derived classes to perform additional post-change logic.
		/// </summary>
		protected virtual void OnItemsChanged()
		{
			RefreshCachedValues();
		}
	}
}