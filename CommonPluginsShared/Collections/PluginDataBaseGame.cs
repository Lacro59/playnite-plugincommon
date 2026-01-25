using Playnite.SDK.Data;
using System.Collections.Generic;

namespace CommonPluginsShared.Collections
{
    public abstract class PluginDataBaseGame<T> : PluginDataBaseGameBase
    {
        #region Cache fields

        private bool? hasData;
        private int? count;

        #endregion


        private List<T> items = new List<T>();

        /// <summary>
        /// Gets or sets the list of items associated with the game.
        /// Setting this property triggers the OnItemsChanged event and refreshes cached values.
        /// </summary>
        public List<T> Items
        {
            get => items ?? new List<T>();
            set
            {
                SetValue(ref items, value);
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
                if (!hasData.HasValue)
                {
                    hasData = Items != null && Items.Count > 0;
                }
                return hasData.Value;
            }
        }

        /// <summary>
        /// Returns the count of items, cast to ulong.
        /// Uses cached value for better performance.
        /// </summary>
        [DontSerialize]
        public override ulong Count
        {
            get
            {
                if (!count.HasValue)
                {
                    count = Items?.Count ?? 0;
                }
                return (ulong)count.Value;
            }
        }


        /// <summary>
        /// Called when the Items list changes.
        /// Override in derived classes to refresh stats or notify property changes.
        /// This base implementation clears cached values.
        /// </summary>
        protected virtual void OnItemsChanged()
        {
            hasData = null;
            count = null;

            RefreshCachedValues();
        }

        protected virtual void RefreshCachedValues()
        {
        }
    }
}