using CommonPluginsControls.Stores.Models;
using CommonPluginsStores.Models.Enumerations;
using CommonPluginsStores.Models.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsControls.Stores
{
    /// <summary>
    /// Master-detail settings layout for store library authentication panels.
    /// Each plugin registers only the stores it supports via <see cref="RegisterStore"/>.
    /// </summary>
    public partial class StoresSettingsView : UserControl
    {
        private readonly StoresSettingsViewModel _viewModel = new StoresSettingsViewModel();

        /// <summary>
        /// Initializes a new instance of the <see cref="StoresSettingsView"/> class.
        /// </summary>
        public StoresSettingsView()
        {
            InitializeComponent();
            DataContext = _viewModel;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.Stores.CollectionChanged += Stores_CollectionChanged;
            Loaded += StoresSettingsView_Loaded;
        }

        /// <summary>
        /// Gets the store entries displayed in the navigation list.
        /// </summary>
        public ObservableCollection<StoreSettingsEntry> Stores => _viewModel.Stores;

        /// <summary>
        /// Registers a store panel in the master-detail layout.
        /// </summary>
        /// <param name="entry">The store entry to register.</param>
        public void RegisterStore(StoreSettingsEntry entry)
        {
            if (entry == null)
            {
                StoreSettingsLog.StoreRegistrationSkipped("entry is null");
                return;
            }

            if (entry.Panel == null)
            {
                StoreSettingsLog.StoreRegistrationSkipped($"panel is null for store '{entry.Id}'");
                return;
            }

            try
            {
                entry.Panel.Visibility = Visibility.Collapsed;
                entry.BindAuthStatus(entry.Panel);
                AttachPanelToDetailHost(entry.Panel);
                _viewModel.Stores.Add(entry);
                _viewModel.RefreshVisibleStores();
                UpdateSelectedPanelVisibility();
                StoreSettingsLog.StoreRegistered(entry.Id, entry.IsVisible, entry.SortOrder);
            }
            catch (Exception ex)
            {
                StoreSettingsLog.Error(ex, $"Failed to register store '{entry.Id}'");
                throw;
            }
        }

        /// <summary>
        /// Refreshes store visibility and selection after external state changes.
        /// UI bindings only; network auth checks are scheduled by store panel binding or <see cref="RequestStoresAuthRefresh"/>.
        /// </summary>
        public void RefreshStores()
        {
            StoreSettingsLog.RefreshStores(_viewModel.Stores.Count);

            foreach (StoreSettingsEntry entry in _viewModel.Stores)
            {
                entry.RefreshAuthStatus();
            }

            _viewModel.RefreshVisibleStores();
            UpdateSelectedPanelVisibility();
        }

        /// <summary>
        /// Schedules a background auth refresh for each registered store panel.
        /// </summary>
        public void RequestStoresAuthRefresh()
        {
            foreach (StoreSettingsEntry entry in _viewModel.Stores)
            {
                entry.RequestAuthRefresh();
            }
        }

        private void StoresSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            StoreSettingsLog.Debug("StoresSettingsView loaded");
            _viewModel.RefreshVisibleStores();
            UpdateSelectedPanelVisibility();
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StoresSettingsViewModel.SelectedEntry))
            {
                StoreSettingsLog.SelectedStoreChanged(_viewModel.SelectedEntry?.Id);
                UpdateSelectedPanelVisibility();
            }
        }

        private void Stores_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (StoreSettingsEntry entry in e.NewItems)
                {
                    entry.PropertyChanged += StoreEntry_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (StoreSettingsEntry entry in e.OldItems)
                {
                    entry.PropertyChanged -= StoreEntry_PropertyChanged;
                    PART_DetailHost.Children.Remove(entry.Panel);
                    StoreSettingsLog.Debug($"Store removed from navigation: {entry.Id}");
                }
            }
        }

        private void StoreEntry_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StoreSettingsEntry.IsVisible)
                && sender is StoreSettingsEntry entry)
            {
                StoreSettingsLog.Debug($"Store visibility changed for {entry.Id}: visible={entry.IsVisible}");
                _viewModel.RefreshVisibleStores();
                UpdateSelectedPanelVisibility();
            }
        }

        private void UpdateSelectedPanelVisibility()
        {
            foreach (StoreSettingsEntry entry in _viewModel.Stores)
            {
                if (entry.Panel == null)
                {
                    continue;
                }

                bool isSelected = entry == _viewModel.SelectedEntry && entry.IsVisible;
                entry.Panel.Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
                StoreSettingsLog.PanelVisibilityUpdated(entry.Id, isSelected);
            }
        }

        private void AttachPanelToDetailHost(FrameworkElement panel)
        {
            DetachPanelFromParent(panel);

            if (!PART_DetailHost.Children.Contains(panel))
            {
                PART_DetailHost.Children.Add(panel);
                StoreSettingsLog.Debug($"Panel attached to detail host: {panel.GetType().Name}");
            }
        }

        private static void DetachPanelFromParent(FrameworkElement panel)
        {
            if (panel.Parent is Panel parentPanel)
            {
                parentPanel.Children.Remove(panel);
                StoreSettingsLog.Debug($"Panel detached from parent panel: {panel.GetType().Name}");
            }
            else if (panel.Parent is Decorator decorator)
            {
                decorator.Child = null;
                StoreSettingsLog.Debug($"Panel detached from parent decorator: {panel.GetType().Name}");
            }
            else if (panel.Parent is ContentControl contentControl)
            {
                contentControl.Content = null;
                StoreSettingsLog.Debug($"Panel detached from parent content control: {panel.GetType().Name}");
            }
        }
    }
}
