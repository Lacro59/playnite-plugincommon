using System;
using System.Collections.Generic;

namespace CommonPluginsStores.Models
{
    /// <summary>
    /// Represents configuration settings for the store.
    /// </summary>
    public class StoreSettings : ObservableObject
    {
        private bool _useApi;

        /// <summary>
        /// Gets or sets a value indicating whether to use the API.
        /// Default is <c>false</c>.
        /// </summary>
        public bool UseApi { get => _useApi; set => SetValue(ref _useApi, value); }

        private bool _useAuth;

        /// <summary>
        /// Gets or sets a value indicating whether to use authentication.
        /// Returns <c>true</c> if <see cref="ForceAuth"/> is <c>true</c>, even if the internal setting is <c>false</c>.
        /// Setting this value only affects the internal <c>_useAuth</c> field.
        /// </summary>
        public bool UseAuth { get => ForceAuth || _useAuth; set => SetValue(ref _useAuth, value); }


        private bool _forceAuth;

        /// <summary>
        /// Gets or sets a value indicating whether authentication is forced.
        /// When <c>true</c>, <see cref="UseAuth"/> will always return <c>true</c>, regardless of the internal <c>_useAuth</c> setting.
        /// </summary>
        public bool ForceAuth { get => _forceAuth; set => SetValue(ref _forceAuth, value); }
    }
}
