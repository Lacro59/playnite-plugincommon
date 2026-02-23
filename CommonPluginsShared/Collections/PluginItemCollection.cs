using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPlayniteShared.Database;
using System;
using System.Threading;

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
		/// Timeout in milliseconds when waiting for the Playnite database to open.
		/// </summary>
		private const int DatabaseOpenTimeoutMilliseconds = 30000;

		/// <summary>
		/// Gets a value indicating whether the Playnite database is currently open and available.
		/// </summary>
		private static bool IsDatabaseOpen
		{
			get
			{
				return API.Instance != null
					&& API.Instance.Database != null
					&& API.Instance.Database.IsOpen;
			}
		}

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
						Common.LogDebug(true, $"Marking item {id} as deleted because game is missing or type does not match {expectedType.Name}.");
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
			if (!WaitForDatabase("SetGameInfo"))
			{
				return;
			}

			if (Items.Count == 0)
			{
				return;
			}

			Common.LogDebug(true, $"Starting bulk game info update for {Items.Count} items using PluginDataBaseGame<{typeof(T).Name}>.");

			using (BufferedUpdate())
			{
				foreach (var item in Items)
				{
					SetGameInfo<T>(item.Key);
				}
			}

			Common.LogDebug(true, $"Completed bulk game info update for {Items.Count} items using PluginDataBaseGame<{typeof(T).Name}>.");
		}

		/// <summary>
		/// Updates the game information for the item with the specified ID,
		/// assuming the item is of type <see cref="PluginDataBaseGameDetails{T, Y}"/>.
		/// </summary>
		/// <typeparam name="T">The first generic type parameter of the item.</typeparam>
		/// <typeparam name="Y">The second generic type parameter of the item.</typeparam>
		/// <param name="id">The unique identifier of the game item.</param>
		public void SetGameInfoDetails<T, Y>(Guid id) where Y : new()
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
		public void SetGameInfoDetails<T, Y>() where Y : new()
		{
			if (!WaitForDatabase("SetGameInfoDetails"))
			{
				return;
			}

			if (Items.Count == 0)
			{
				return;
			}

			Common.LogDebug(true, $"Starting bulk game info details update for {Items.Count} items using PluginDataBaseGameDetails<{typeof(T).Name}, {typeof(Y).Name}>.");

			using (BufferedUpdate())
			{
				foreach (var item in Items)
				{
					SetGameInfoDetails<T, Y>(item.Key);
				}
			}

			Common.LogDebug(true, $"Completed bulk game info details update for {Items.Count} items using PluginDataBaseGameDetails<{typeof(T).Name}, {typeof(Y).Name}>.");
		}

		/// <summary>
		/// Waits for the Playnite database to be opened, with a timeout and logging.
		/// </summary>
		/// <param name="operationName">Name of the operation waiting on the database (for logging).</param>
		/// <returns>
		/// True if the database became available before the timeout; otherwise, false.
		/// </returns>
		private static bool WaitForDatabase(string operationName)
		{
			if (IsDatabaseOpen)
			{
				return true;
			}

			Common.LogDebug(true, $"Waiting for Playnite database to open before '{operationName}'.");

			bool isOpen = SpinWait.SpinUntil(() => IsDatabaseOpen, DatabaseOpenTimeoutMilliseconds);
			if (!isOpen)
			{
				var exception = new TimeoutException("Timed out while waiting for Playnite database to open.");
				Common.LogError(exception, false, $"{operationName} aborted because the database did not open within {DatabaseOpenTimeoutMilliseconds} ms.");
			}

			return isOpen;
		}
	}
}