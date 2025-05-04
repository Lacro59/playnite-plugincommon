using System;
using System.Collections.Generic;

namespace CommonPluginsStores.Models
{
    /// <summary>
    /// Represents basic information about a game associated with an account.
    /// </summary>
    public class BasicAccountGameInfos : ObservableObject
    {
        private string _id;

        /// <summary>
        /// Gets or sets the game identifier.
        /// </summary>
        public string Id { get => _id; set => _id = value; }

        private string _name;

        /// <summary>
        /// Gets or sets the name of the game.
        /// </summary>
        public string Name { get => _name; set => _name = value; }

        private string _image;

        /// <summary>
        /// Gets or sets the URL of the game's image (e.g., thumbnail or poster).
        /// </summary>
        public string Image { get => _image; set => _image = value; }

        private DateTime? _released;

        /// <summary>
        /// Gets or sets the release date of the game.
        /// </summary>
        public DateTime? Released { get => _released; set => _released = value; }

        private string _link;

        /// <summary>
        /// Gets or sets the link to the game's page or store.
        /// </summary>
        public string Link { get => _link; set => _link = value; }
    }
}
