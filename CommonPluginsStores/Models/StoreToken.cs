using System;

namespace CommonPluginsStores.Models
{
    /// <summary>
    /// Represents a store token, which consists of a type and an associated token value.
    /// </summary>
    public class StoreToken
    {
        /// <summary>
        /// Gets or sets the type of the token (e.g., authentication type).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the token value (e.g., authentication token or session token).
        /// </summary>
        public string Token { get; set; }
    }
}
