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
    }
}
