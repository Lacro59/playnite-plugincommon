using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPlayniteShared.Database;
using System;

namespace CommonPluginsShared.Collections
{
    /// <summary>
    /// Represents a collection of plugin items of type <typeparamref name="TItem"/>.
    /// Provides methods to update game information from the Playnite database.
    /// </summary>
    /// <typeparam name="TItem">The type of items stored in the collection, must inherit from PluginDataBaseGameBase.</typeparam>
    public class PluginItemCollection<TItem> : ItemCollection<TItem> where TItem : PluginDataBaseGameBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginItemCollection{TItem}"/> class.
        /// </summary>
        /// <param name="path">The file path associated with the collection.</param>
        /// <param name="type">The type of the game database collection.</param>
        public PluginItemCollection(string path, GameDatabaseCollection type = GameDatabaseCollection.Uknown) : base(path, type)
        {
        }

        /// <summary>
        /// Updates the game information for the item with the specified ID, verifying the item type matches the expected type.
        /// Sets the item's Name and IsSaved flag if the game exists; otherwise marks the item as deleted.
        /// </summary>
        /// <param name="id">The unique identifier of the game item.</param>
        /// <param name="expectedType">The expected type of the item to update.</param>
        private void SetGameInfoInternal(Guid id, Type expectedType)
        {
            try
            {
                if (Items.TryGetValue(id, out TItem item))
                {
                    Game game = API.Instance.Database.Games.Get(id);

                    if (game != null && expectedType.IsInstanceOfType(item))
                    {
                        item.Name = game.Name;
                        item.IsSaved = true;
                    }
                    else
                    {
                        item.IsDeleted = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error updating game info for ID {id}");
            }
        }

        /// <summary>
        /// Updates the game information for the item with the specified ID,
        /// assuming the item is of type <see cref="PluginDataBaseGame{T}"/>.
        /// </summary>
        /// <typeparam name="T">The generic type parameter of the item.</typeparam>
        /// <param name="id">The unique identifier of the game item.</param>
        public void SetGameInfo<T>(Guid id)
        {
            SetGameInfoInternal(id, typeof(PluginDataBaseGame<T>));
        }

        /// <summary>
        /// Updates the game information for all items in the collection,
        /// assuming each item is of type <see cref="PluginDataBaseGame{T}"/>.
        /// Waits until the Playnite database is open before processing.
        /// </summary>
        /// <typeparam name="T">The generic type parameter of the items.</typeparam>
        public void SetGameInfo<T>()
        {
            _ = System.Threading.SpinWait.SpinUntil(() => API.Instance.Database.IsOpen, -1);
            foreach (var item in Items)
            {
                SetGameInfo<T>(item.Key);
            }
        }

        /// <summary>
        /// Updates the game information for the item with the specified ID,
        /// assuming the item is of type <see cref="PluginDataBaseGameDetails{T, Y}"/>.
        /// </summary>
        /// <typeparam name="T">The first generic type parameter of the item.</typeparam>
        /// <typeparam name="Y">The second generic type parameter of the item.</typeparam>
        /// <param name="id">The unique identifier of the game item.</param>
        public void SetGameInfoDetails<T, Y>(Guid id)
        {
            SetGameInfoInternal(id, typeof(PluginDataBaseGameDetails<T, Y>));
        }

        /// <summary>
        /// Updates the game information for all items in the collection,
        /// assuming each item is of type <see cref="PluginDataBaseGameDetails{T, Y}"/>.
        /// Waits until the Playnite database is open before processing.
        /// </summary>
        /// <typeparam name="T">The first generic type parameter of the items.</typeparam>
        /// <typeparam name="Y">The second generic type parameter of the items.</typeparam>
        public void SetGameInfoDetails<T, Y>()
        {
            _ = System.Threading.SpinWait.SpinUntil(() => API.Instance.Database.IsOpen, -1);
            foreach (var item in Items)
            {
                SetGameInfoDetails<T, Y>(item.Key);
            }
        }
    }
}