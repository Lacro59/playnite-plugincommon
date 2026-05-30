using CommonPluginsShared;
using CommonPluginsControls.Stores;
using CommonPluginsStores.Models.Enumerations;
using Playnite.SDK;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsControls.Stores.Models
{
    /// <summary>
    /// Describes one store entry displayed in <see cref="StoresSettingsView"/>.
    /// Each plugin registers only the stores it supports.
    /// </summary>
    public class StoreSettingsEntry : ObservableObject
    {
        private string _id;
        private string _nameResourceKey;
        private string _categoryResourceKey;
        private string _subtitleResourceKey;
        private string _iconName;
        private FrameworkElement _panel;
        private bool _isVisible = true;
        private int _sortOrder;
        private IStorePanelViewModel _authProvider;
        private INotifyPropertyChanged _authSource;

        /// <summary>
        /// Gets or sets the unique store identifier (for example, "Steam").
        /// </summary>
        public string Id
        {
            get => _id;
            set
            {
                SetValue(ref _id, value);
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(StoreIcon));
                OnPropertyChanged(nameof(HasStoreIcon));
            }
        }

        /// <summary>
        /// Gets or sets the localization resource key for the store display name.
        /// </summary>
        public string NameResourceKey
        {
            get => _nameResourceKey;
            set
            {
                SetValue(ref _nameResourceKey, value);
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        /// <summary>
        /// Gets or sets the localization resource key for the sidebar category header.
        /// </summary>
        public string CategoryResourceKey
        {
            get => _categoryResourceKey;
            set
            {
                SetValue(ref _categoryResourceKey, value);
                OnPropertyChanged(nameof(CategoryName));
            }
        }

        /// <summary>
        /// Gets or sets an optional localization resource key shown as a subtitle under the store name.
        /// </summary>
        public string SubtitleResourceKey
        {
            get => _subtitleResourceKey;
            set
            {
                SetValue(ref _subtitleResourceKey, value);
                OnPropertyChanged(nameof(Subtitle));
            }
        }

        /// <summary>
        /// Gets or sets an optional icon key passed to <see cref="TransformIcon.Get(string, bool, bool)"/>.
        /// When empty, <see cref="Id"/> is used (lowercased).
        /// </summary>
        public string IconName
        {
            get => _iconName;
            set
            {
                SetValue(ref _iconName, value);
                OnPropertyChanged(nameof(StoreIcon));
                OnPropertyChanged(nameof(HasStoreIcon));
            }
        }

        /// <summary>
        /// Gets or sets the store settings panel hosted in the detail area.
        /// </summary>
        public FrameworkElement Panel
        {
            get => _panel;
            set => SetValue(ref _panel, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this store appears in the navigation list.
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetValue(ref _isVisible, value);
        }

        /// <summary>
        /// Gets or sets the sort order within a category (lower values appear first).
        /// </summary>
        public int SortOrder
        {
            get => _sortOrder;
            set => SetValue(ref _sortOrder, value);
        }

        /// <summary>
        /// Gets the localized store display name resolved from <see cref="NameResourceKey"/>.
        /// </summary>
        public string DisplayName =>
            string.IsNullOrEmpty(NameResourceKey) ? Id : ResourceProvider.GetString(NameResourceKey);

        /// <summary>
        /// Gets the localized category name resolved from <see cref="CategoryResourceKey"/>.
        /// </summary>
        public string CategoryName =>
            string.IsNullOrEmpty(CategoryResourceKey) ? string.Empty : ResourceProvider.GetString(CategoryResourceKey);

        /// <summary>
        /// Gets the localized subtitle resolved from <see cref="SubtitleResourceKey"/>.
        /// </summary>
        public string Subtitle =>
            string.IsNullOrEmpty(SubtitleResourceKey) ? string.Empty : ResourceProvider.GetString(SubtitleResourceKey);

        /// <summary>
        /// Gets the store icon glyph from <see cref="TransformIcon"/>.
        /// </summary>
        public string StoreIcon => TransformIcon.Get(ResolveIconName(), returnEmpty: true);

        /// <summary>
        /// Gets a value indicating whether a store icon glyph is available.
        /// </summary>
        public bool HasStoreIcon => !string.IsNullOrEmpty(StoreIcon);

        /// <summary>
        /// Gets the authentication status reported by the store panel view model.
        /// </summary>
        public AuthStatus AuthStatus => _authProvider?.AuthStatus ?? AuthStatus.Unknown;

        /// <summary>
        /// Gets a value indicating whether the sidebar auth indicator should be shown.
        /// </summary>
        public bool ShowAuthStatusIndicator => _authProvider?.ShowConnectionSection == true;

        /// <summary>
        /// Binds auth status updates from the store settings panel data context.
        /// </summary>
        /// <param name="panel">The registered store panel.</param>
        public void BindAuthStatus(FrameworkElement panel)
        {
            if (panel == null)
            {
                return;
            }

            panel.DataContextChanged += Panel_DataContextChanged;
            AttachAuthProvider(panel.DataContext);
        }

        /// <summary>
        /// Refreshes auth status properties on the entry (for example after external session changes).
        /// </summary>
        public void RefreshAuthStatus()
        {
            NotifyAuthStatusProperties();
        }

        private void Panel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachAuthProvider(e.NewValue);
        }

        private void AttachAuthProvider(object dataContext)
        {
            if (_authSource != null)
            {
                _authSource.PropertyChanged -= AuthSource_PropertyChanged;
                _authSource = null;
            }

            _authProvider = dataContext as IStorePanelViewModel;
            _authSource = dataContext as INotifyPropertyChanged;

            if (_authSource != null)
            {
                _authSource.PropertyChanged += AuthSource_PropertyChanged;
            }

            StoreSettingsLog.AuthProviderBound(Id, _authProvider != null);
            NotifyAuthStatusProperties();
        }

        private void AuthSource_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName == nameof(IStorePanelViewModel.AuthStatus)
                || e.PropertyName == nameof(IStorePanelViewModel.ShowConnectionSection))
            {
                NotifyAuthStatusProperties();
            }
        }

        private void NotifyAuthStatusProperties()
        {
            StoreSettingsLog.AuthStatusChanged(Id, AuthStatus, ShowAuthStatusIndicator);
            OnPropertyChanged(nameof(AuthStatus));
            OnPropertyChanged(nameof(ShowAuthStatusIndicator));
        }

        private string ResolveIconName()
        {
            if (!string.IsNullOrEmpty(IconName))
            {
                return IconName;
            }

            return string.IsNullOrEmpty(Id) ? null : Id.ToLowerInvariant();
        }
    }
}
