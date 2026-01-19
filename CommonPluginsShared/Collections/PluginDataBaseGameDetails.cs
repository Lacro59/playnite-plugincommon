using System.Collections.Generic;

namespace CommonPluginsShared.Collections
{
	public abstract class PluginDataBaseGameDetails<T, Y> : PluginDataBaseGame<T>
	{
		private Y itemsDetails = default;

		/// <summary>
		/// Gets or sets the list of itemsDetails associated with the game.
		/// Setting this property triggers the OnItemsChanged event and refreshes cached values.
		/// </summary>
		public Y ItemsDetails
		{
			get
			{
				return itemsDetails == null ? default : itemsDetails;
			}
			set
			{
				SetValue(ref itemsDetails, value);
				OnItemsChanged();
			}
		}
	}
}