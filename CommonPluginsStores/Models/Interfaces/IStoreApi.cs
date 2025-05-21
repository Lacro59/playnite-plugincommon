namespace CommonPluginsStores.Models.Interfaces
{
    /// <summary>
    /// Represents the API contract for a store plugin.
    /// </summary>
    public interface IStoreApi
    {
        /// <summary>
        /// Gets or sets the store settings.
        /// </summary>
        StoreSettings StoreSettings { get; set; }

        /// <summary>
        /// Gets or sets whether the user is logged in.
        /// </summary>
        bool IsUserLoggedIn { get; set; }

        /// <summary>
        /// Gets or sets the current account information.
        /// </summary>
        AccountInfos CurrentAccountInfos { get; set; }

        /// <summary>
        /// Resets the user login status.
        /// </summary>
        void ResetIsUserLoggedIn();

        /// <summary>
        /// Sets the language or locale used by the store API.
        /// </summary>
        /// <param name="locale">The locale code, e.g., 'en-US'.</param>
        void SetLanguage(string locale);

        /// <summary>
        /// Performs the main login procedure.
        /// </summary>
        void Login();

        /// <summary>
        /// Performs an alternative login procedure.
        /// </summary>
        void LoginAlternative();
    }

    internal interface IStoreApiInternal
    {
        /// <summary>
        /// Loads the current user information.
        /// </summary>
        /// <returns>The loaded account information.</returns>
        AccountInfos LoadCurrentUser();

        /// <summary>
        /// Saves the current user information.
        /// </summary>
        void SaveCurrentUser();
    }
}