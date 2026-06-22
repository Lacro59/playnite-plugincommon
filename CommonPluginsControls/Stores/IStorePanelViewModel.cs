using CommonPluginsStores.Models.Enumerations;

namespace CommonPluginsControls.Stores
{
    /// <summary>
    /// Contract exposed by store settings panels for sidebar auth status binding.
    /// </summary>
    public interface IStorePanelViewModel
    {
        /// <summary>
        /// Gets the current authentication status for the store account.
        /// </summary>
        AuthStatus AuthStatus { get; }

        /// <summary>
        /// Gets a value indicating whether the connection UI is shown for this store.
        /// </summary>
        bool ShowConnectionSection { get; }

        /// <summary>
        /// Gets a value indicating whether the sign-in action is available.
        /// </summary>
        bool CanLogin { get; }

        /// <summary>
        /// Gets a value indicating whether the log out action is available.
        /// </summary>
        bool CanLogout { get; }

        /// <summary>
        /// Refreshes auth status and command availability bindings.
        /// </summary>
        void RefreshAuthCommandStates();

        /// <summary>
        /// Schedules a full auth verification on a background thread.
        /// </summary>
        void RequestBackgroundAuthRefresh();
    }
}
