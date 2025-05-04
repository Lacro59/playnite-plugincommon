using System;

namespace CommonPluginsStores.Models
{
    /// <summary>
    /// Represents a user's game wishlist in an account.
    /// Inherits from <see cref="BasicAccountGameInfos"/> to include basic game details.
    /// </summary>
    public class AccountWishlist : BasicAccountGameInfos
    {
        private DateTime? _added;

        /// <summary>
        /// Gets or sets the date when the game was added to the wishlist.
        /// </summary>
        public DateTime? Added { get => _added; set => _added = value; }
    }
}
