using CommonPluginsShared;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace CommonPluginsStores.Models
{
    /// <summary>
    /// Represents basic information about a game.
    /// </summary>
    public class BasicGameInfos : ObservableObject
    {
        private string _id;

        /// <summary>
        /// Gets or sets the unique identifier for the game.
        /// </summary>
        public string Id { get => _id; set => _id = value; }

        private string _id2;

        /// <summary>
        /// Gets or sets a secondary unique identifier for the game (if applicable).
        /// </summary>
        public string Id2 { get => _id2; set => _id2 = value; }

        private string _name;

        /// <summary>
        /// Gets or sets the name of the game.
        /// </summary>
        public string Name { get => _name; set => _name = value; }

        private string _link;

        /// <summary>
        /// Gets or sets the URL link to the game's page or store.
        /// </summary>
        public string Link { get => _link; set => _link = value; }

        private string _description;

        /// <summary>
        /// Gets or sets the description of the game.
        /// </summary>
        public string Description { get => _description; set => _description = value; }

        private string _languages;

        /// <summary>
        /// Gets or sets the supported languages for the game (comma-separated if multiple).
        /// </summary>
        public string Languages { get => _languages; set => _languages = value; }

        private DateTime? _released;

        /// <summary>
        /// Gets or sets the release date of the game.
        /// </summary>
        public DateTime? Released { get => _released; set => _released = value; }

        private string _image;

        /// <summary>
        /// Gets or sets the URL of the game's image (e.g., thumbnail or cover).
        /// </summary>
        public string Image { get => _image; set => _image = value; }

        /// <summary>
        /// Gets the local image path based on the URL of the image.
        /// </summary>
        [DontSerialize]
        public string ImagePath => ImageSourceManagerPlugin.GetImagePath(Image);
    }
}
