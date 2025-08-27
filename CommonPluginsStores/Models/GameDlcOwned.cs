using System;
using System.Collections.Generic;

namespace CommonPluginsStores.Models
{
    /// <summary>
    /// Represents a downloadable content (DLC) owned by the user.
    /// </summary>
    public class GameDlcOwned : ObservableObject
    {
        private string _id;

        /// <summary>
        /// Gets or sets the unique identifier for the DLC.
        /// </summary>
        public string Id { get => _id; set => _id = value; }

        private string _id2;

        /// <summary>
        /// Gets or sets the unique identifier for the DLC.
        /// </summary>
        public string Id2 { get => _id2; set => _id2 = value; }
    }
}
