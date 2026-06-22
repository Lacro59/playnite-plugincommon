using CommonPluginsControls.Stores.Models;
using Playnite.SDK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace CommonPluginsControls.Stores
{
    /// <summary>
    /// View model for the master-detail stores settings layout.
    /// </summary>
    public class StoresSettingsViewModel : ObservableObject
    {
        private StoreSettingsEntry _selectedEntry;
        private readonly ObservableCollection<StoreSettingsEntry> _stores = new ObservableCollection<StoreSettingsEntry>();
        private ICollectionView _visibleStoresView;

        /// <summary>
        /// Gets all store entries registered by the host plugin.
        /// </summary>
        public ObservableCollection<StoreSettingsEntry> Stores => _stores;

        /// <summary>
        /// Gets the filtered view of visible stores for the navigation list.
        /// </summary>
        public ICollectionView VisibleStoresView
        {
            get
            {
                if (_visibleStoresView == null)
                {
                    _visibleStoresView = CollectionViewSource.GetDefaultView(_stores);
                    _visibleStoresView.Filter = FilterVisibleStores;
                    _visibleStoresView.SortDescriptions.Add(new SortDescription(nameof(StoreSettingsEntry.SortOrder), ListSortDirection.Ascending));
                    _visibleStoresView.SortDescriptions.Add(new SortDescription(nameof(StoreSettingsEntry.DisplayName), ListSortDirection.Ascending));
                }

                return _visibleStoresView;
            }
        }

        /// <summary>
        /// Gets or sets the currently selected store entry.
        /// </summary>
        public StoreSettingsEntry SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                SetValue(ref _selectedEntry, value);
                OnPropertyChanged(nameof(HasSelectedEntry));
            }
        }

        /// <summary>
        /// Gets a value indicating whether a store entry is currently selected.
        /// </summary>
        public bool HasSelectedEntry => SelectedEntry != null;

        /// <summary>
        /// Refreshes the visible store list and keeps a valid selection.
        /// </summary>
        public void RefreshVisibleStores()
        {
            VisibleStoresView.Refresh();

            if (SelectedEntry == null || !SelectedEntry.IsVisible)
            {
                SelectedEntry = _stores.FirstOrDefault(store => store.IsVisible);
            }
        }

        private bool FilterVisibleStores(object item)
        {
            return item is StoreSettingsEntry entry && entry.IsVisible;
        }
    }
}
