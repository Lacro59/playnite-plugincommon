using System;
using System.Collections.Generic;
using System.Windows;
using CommonPluginsShared;

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// Represents a single stateful toggle button hosted inside <see cref="PluginChartNavBar"/>.
    /// </summary>
    /// <remarks>
    /// The consumer keeps a reference to this object and writes <see cref="IsActive"/> directly
    /// to drive the button state from the outside (INPC propagates to the nav bar's item template).
    /// </remarks>
    public class NavBarToggle : ObservableObject
    {
        /// <summary>Unique key used to identify this toggle in <see cref="NavBarToggleChangedEventArgs"/>.</summary>
        public string Id { get; }

        private string _inactiveIcon;
        /// <summary>IcoFont glyph rendered when <see cref="IsActive"/> is <c>false</c>.</summary>
        public string InactiveIcon
        {
            get => _inactiveIcon;
            set
            {
                SetValue(ref _inactiveIcon, value);
                RefreshIcon();
            }
        }

        private string _activeIcon;
        /// <summary>IcoFont glyph rendered when <see cref="IsActive"/> is <c>true</c>.</summary>
        public string ActiveIcon
        {
            get => _activeIcon;
            set
            {
                SetValue(ref _activeIcon, value);
                RefreshIcon();
            }
        }

        private string _inactiveToolTip = string.Empty;
        /// <summary>Tooltip text shown when <see cref="IsActive"/> is <c>false</c>.</summary>
        public string InactiveToolTip
        {
            get => _inactiveToolTip;
            set
            {
                SetValue(ref _inactiveToolTip, value);
                RefreshToolTip();
            }
        }

        private string _activeToolTip = string.Empty;
        /// <summary>Tooltip text shown when <see cref="IsActive"/> is <c>true</c>.</summary>
        public string ActiveToolTip
        {
            get => _activeToolTip;
            set
            {
                SetValue(ref _activeToolTip, value);
                RefreshToolTip();
            }
        }

        private bool _isActive;
        /// <summary>
        /// Current toggle state.
        /// Setting this from the consumer propagates immediately to the nav bar's item template
        /// via <see cref="INotifyPropertyChanged"/>.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value) return;

                SetValue(ref _isActive, value);
                RefreshIcon();
                RefreshToolTip();

                // Notify the nav bar so it can bubble the routed event.
                IsActiveChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // ── Derived display properties (bound in the item template) ──────────

        private string _currentIcon = string.Empty;
        /// <summary>The glyph currently rendered — switches between <see cref="InactiveIcon"/> and <see cref="ActiveIcon"/>.</summary>
        public string CurrentIcon
        {
            get => _currentIcon;
            private set => SetValue(ref _currentIcon, value);
        }

        private string _currentToolTip = string.Empty;
        /// <summary>The tooltip currently shown — switches between <see cref="InactiveToolTip"/> and <see cref="ActiveToolTip"/>.</summary>
        public string CurrentToolTip
        {
            get => _currentToolTip;
            private set => SetValue(ref _currentToolTip, value);
        }

        // ── Internal event used by PluginChartNavBar ─────────────────────────

        /// <summary>
        /// Raised when <see cref="IsActive"/> changes.
        /// Consumed internally by <see cref="PluginChartNavBar"/> to bubble <see cref="PluginChartNavBar.NavBarToggleChangedEvent"/>.
        /// </summary>
        internal event EventHandler IsActiveChanged;

        /// <summary>
        /// Initializes a new <see cref="NavBarToggle"/>.
        /// </summary>
        /// <param name="id">Unique identifier for this toggle.</param>
        /// <param name="inactiveIcon">IcoFont glyph used when inactive.</param>
        /// <param name="activeIcon">IcoFont glyph used when active. Falls back to <paramref name="inactiveIcon"/> if null.</param>
        public NavBarToggle(string id, string inactiveIcon, string activeIcon = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Toggle id must not be null or empty.", nameof(id));

            Id = id;
            _inactiveIcon = inactiveIcon ?? string.Empty;
            _activeIcon = activeIcon ?? inactiveIcon ?? string.Empty;

            RefreshIcon();
            RefreshToolTip();
        }

        // ────────────────────────────────────────────────────────────────────

        private void RefreshIcon()
        {
            CurrentIcon = _isActive ? _activeIcon : _inactiveIcon;
        }

        private void RefreshToolTip()
        {
            CurrentToolTip = _isActive ? _activeToolTip : _inactiveToolTip;
        }
    }


    /// <summary>
    /// Provides data for the <see cref="PluginChartNavBar.NavBarToggleChangedEvent"/> routed event.
    /// </summary>
    public class NavBarToggleChangedEventArgs : RoutedEventArgs
    {
        /// <summary>Identifier of the toggle whose state changed.</summary>
        public string ToggleId { get; }

        /// <summary>New active state of the toggle.</summary>
        public bool IsActive { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="NavBarToggleChangedEventArgs"/>.
        /// </summary>
        public NavBarToggleChangedEventArgs(
            RoutedEvent routedEvent,
            string toggleId,
            bool isActive
        ) : base(routedEvent)
        {
            ToggleId = toggleId;
            IsActive = isActive;
        }
    }

    /// <summary>Handler delegate for <see cref="PluginChartNavBar.NavBarToggleChangedEvent"/>.</summary>
    public delegate void NavBarToggleChangedEventHandler(
        object sender,
        NavBarToggleChangedEventArgs e
    );
}