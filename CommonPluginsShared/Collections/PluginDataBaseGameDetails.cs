using System.Collections.Generic;

namespace CommonPluginsShared.Collections
{
	public abstract class PluginDataBaseGameDetails<T, Y> : PluginDataBaseGame<T>
	{
		private Y _itemsDetails = default;

		/// <summary>
		/// Gets or sets the list of items details associated with the game.
		/// Setting this property triggers the OnItemsChanged event and refreshes cached values.
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