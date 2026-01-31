using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPlayniteShared.Common;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CommonPlayniteShared.Database
{
	/// <summary>
	/// Represents a thread-safe collection of database objects with optional file persistence.
	/// </summary>
	/// <typeparam name="TItem">The type of items in the collection, must inherit from DatabaseObject.</typeparam>
	public class ItemCollection<TItem> : IDisposable, ICollection<TItem>, IEnumerable<TItem>, IEnumerable
		where TItem : DatabaseObject
	{
		#region Constants and Fields

		private const int MAX_PARALLEL_LOAD_THREADS = 4;
		private const string JSON_FILE_EXTENSION = "*.json";
		private const string JSON_EXTENSION = ".json";

		private readonly ILogger logger = LogManager.GetLogger();
		private readonly object collectionLock = new object();
		private readonly Action<TItem> initMethod;
		private readonly bool isPersistent = true;

		private string storagePath;
		private bool isEventBufferEnabled = false;
		private int bufferDepth = 0;

		private List<TItem> addedItemsEventBuffer = new List<TItem>();
		private List<TItem> removedItemsEventBuffer = new List<TItem>();
		private Dictionary<Guid, ItemUpdateEvent<TItem>> itemUpdatesEventBuffer = new Dictionary<Guid, ItemUpdateEvent<TItem>>();

		#endregion

		#region Properties

		/// <summary>
		/// Gets the concurrent dictionary containing all items indexed by their ID.
		/// </summary>
		public ConcurrentDictionary<Guid, TItem> Items { get; }

		/// <summary>
		/// Gets the number of items in the collection.
		/// </summary>
		public int Count => Items.Count;

		/// <summary>
		/// Gets a value indicating whether the collection is read-only.
		/// </summary>
		public bool IsReadOnly => false;

		/// <summary>
		/// Gets the type of the collection.
		/// </summary>
		public GameDatabaseCollection CollectionType { get; }

		/// <summary>
		/// Gets or sets whether events are enabled for this collection.
		/// </summary>
		internal bool IsEventsEnabled { get; set; } = true;

		/// <summary>
		/// Gets or sets an item by its ID.
		/// </summary>
		/// <param name="id">The unique identifier of the item.</param>
		/// <returns>The item with the specified ID.</returns>
		public TItem this[Guid id]
		{
			get => Get(id);
			set => throw new NotImplementedException();
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when items are added to or removed from the collection.
		/// </summary>
		public event EventHandler<ItemCollectionChangedEventArgs<TItem>> ItemCollectionChanged;

		/// <summary>
		/// Occurs when an item in the collection is updated.
		/// </summary>
		public event EventHandler<ItemUpdatedEventArgs<TItem>> ItemUpdated;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the ItemCollection class.
		/// </summary>
		/// <param name="isPersistent">Indicates whether items should be persisted to disk.</param>
		/// <param name="type">The type of the collection.</param>
		public ItemCollection(bool isPersistent = true, GameDatabaseCollection type = GameDatabaseCollection.Uknown)
		{
			this.isPersistent = isPersistent;
			Items = new ConcurrentDictionary<Guid, TItem>();
			CollectionType = type;
		}

		/// <summary>
		/// Initializes a new instance of the ItemCollection class with an initialization method.
		/// </summary>
		/// <param name="initMethod">Method to invoke on each item after loading.</param>
		/// <param name="isPersistent">Indicates whether items should be persisted to disk.</param>
		/// <param name="type">The type of the collection.</param>
		public ItemCollection(Action<TItem> initMethod, bool isPersistent = true, GameDatabaseCollection type = GameDatabaseCollection.Uknown)
			: this(isPersistent, type)
		{
			this.initMethod = initMethod;
		}

		/// <summary>
		/// Initializes a new instance of the ItemCollection class with a storage path.
		/// </summary>
		/// <param name="path">The file system path where items are stored.</param>
		/// <param name="type">The type of the collection.</param>
		public ItemCollection(string path, GameDatabaseCollection type = GameDatabaseCollection.Uknown)
		{
			this.isPersistent = true;
			Items = new ConcurrentDictionary<Guid, TItem>();
			InitializeCollection(path);
			CollectionType = type;
		}

		/// <summary>
		/// Initializes a new instance of the ItemCollection class with a storage path and initialization method.
		/// </summary>
		/// <param name="path">The file system path where items are stored.</param>
		/// <param name="initMethod">Method to invoke on each item after loading.</param>
		/// <param name="type">The type of the collection.</param>
		public ItemCollection(string path, Action<TItem> initMethod, GameDatabaseCollection type = GameDatabaseCollection.Uknown)
			: this(path, type)
		{
			this.initMethod = initMethod;
		}

		#endregion

		#region Initialization

		/// <summary>
		/// Initializes the collection by loading all items from the specified directory path.
		/// </summary>
		/// <param name="path">The directory path containing the collection items.</param>
		/// <exception cref="Exception">Thrown if the collection is already initialized.</exception>
		public void InitializeCollection(string path)
		{
			if (!string.IsNullOrEmpty(storagePath))
			{
				throw new Exception("Collection already initialized.");
			}

			storagePath = path;

			if (Directory.Exists(storagePath))
			{
				LoadItemsFromDirectory();
			}
			else
			{
				FileSystem.CreateDirectory(storagePath);
			}
		}

		/// <summary>
		/// Loads all items from the storage directory in parallel.
		/// </summary>
		private void LoadItemsFromDirectory()
		{
			Parallel.ForEach(
				Directory.EnumerateFiles(storagePath, JSON_FILE_EXTENSION),
				new ParallelOptions { MaxDegreeOfParallelism = MAX_PARALLEL_LOAD_THREADS },
				LoadItemFromFile);
		}

		/// <summary>
		/// Loads a single item from a JSON file.
		/// </summary>
		/// <param name="objectFile">The path to the JSON file.</param>
		private void LoadItemFromFile(string objectFile)
		{
			if (!Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out Guid fileId))
			{
				logger.Warn($"Skipping non-ID collection item {objectFile}");
				return;
			}

			try
			{
				TItem obj = Serialization.FromJsonFile<TItem>(objectFile);
				if (obj != null)
				{
					obj.Id = fileId;
					initMethod?.Invoke(obj);
					Items.TryAdd(obj.Id, obj);
				}
				else
				{
					logger.Warn($"Failed to deserialize collection item {objectFile}");
				}
			}
			catch (Exception e)
			{
				logger.Error(e, $"Failed to load item from {objectFile}");
			}
		}

		#endregion

		#region File System Operations

		/// <summary>
		/// Gets the file path for an item with the specified ID.
		/// </summary>
		/// <param name="id">The item ID.</param>
		/// <returns>The full file path for the item.</returns>
		internal string GetItemFilePath(Guid id)
		{
			if (string.IsNullOrEmpty(storagePath))
			{
				throw new InvalidOperationException("Storage path not initialized. Call InitializeCollection(path) before using persistence.");
			}
			return Path.Combine(storagePath, $"{id}{JSON_EXTENSION}");
		}

		/// <summary>
		/// Saves an item's data to disk.
		/// </summary>
		/// <param name="item">The item to save.</param>
		internal void SaveItemData(TItem item)
		{
			FileSystem.WriteStringToFileSafe(GetItemFilePath(item.Id), Serialization.ToJson(item));
		}

		/// <summary>
		/// Loads an item's data from disk.
		/// </summary>
		/// <param name="id">The ID of the item to load.</param>
		/// <returns>The loaded item.</returns>
		internal TItem GetItemData(Guid id)
		{
			using (var fs = FileSystem.OpenReadFileStreamSafe(GetItemFilePath(id)))
			{
				return Serialization.FromJsonStream<TItem>(fs);
			}
		}

		#endregion

		#region Item Retrieval

		/// <summary>
		/// Gets an item by its ID.
		/// </summary>
		/// <param name="id">The unique identifier of the item.</param>
		/// <returns>The item if found; otherwise, null.</returns>
		public TItem Get(Guid id)
		{
			Items.TryGetValue(id, out TItem item);
			return item;
		}

		/// <summary>
		/// Checks if an item with the specified ID exists in the collection.
		/// </summary>
		/// <param name="id">The unique identifier to check.</param>
		/// <returns>True if the item exists; otherwise, false.</returns>
		public bool ContainsItem(Guid id)
		{
			return Items.ContainsKey(id);
		}

		/// <summary>
		/// Gets multiple items by their IDs.
		/// </summary>
		/// <param name="ids">The list of IDs to retrieve.</param>
		/// <returns>A list of found items.</returns>
		public List<TItem> Get(IList<Guid> ids)
		{
			var items = new List<TItem>(ids.Count);
			foreach (Guid id in ids)
			{
				TItem item = Get(id);
				if (item != null)
				{
					items.Add(item);
				}
			}

			return items;
		}

		#endregion

		#region Add Operations

		/// <summary>
		/// Adds a new item with the specified name, or returns an existing item if a match is found.
		/// </summary>
		/// <param name="itemName">The name of the item to add.</param>
		/// <param name="existingComparer">Function to compare existing items with the new name.</param>
		/// <returns>The newly added item or the existing matching item.</returns>
		/// <exception cref="ArgumentNullException">Thrown if itemName is null or empty.</exception>
		public virtual TItem Add(string itemName, Func<TItem, string, bool> existingComparer)
		{
			if (string.IsNullOrEmpty(itemName))
			{
				throw new ArgumentNullException(nameof(itemName));
			}

			TItem existingItem = this.FirstOrDefault(a => existingComparer(a, itemName));
			if (existingItem != null)
			{
				return existingItem;
			}

			TItem newItem = typeof(TItem).CrateInstance<TItem>(itemName);
			Add(newItem);
			return newItem;
		}

		/// <summary>
		/// Adds a new item with the specified name using case-insensitive name comparison.
		/// </summary>
		/// <param name="itemName">The name of the item to add.</param>
		/// <returns>The newly added item or the existing matching item.</returns>
		public virtual TItem Add(string itemName)
		{
			return Add(itemName, (existingItem, newName) =>
				existingItem.Name.Equals(newName, StringComparison.InvariantCultureIgnoreCase));
		}

		/// <summary>
		/// Adds multiple items by name, returning existing items where matches are found.
		/// </summary>
		/// <param name="itemsToAdd">The list of item names to add.</param>
		/// <param name="existingComparer">Function to compare existing items with new names.</param>
		/// <returns>An enumerable of added or existing items.</returns>
		public virtual IEnumerable<TItem> Add(List<string> itemsToAdd, Func<TItem, string, bool> existingComparer)
		{
			var toAdd = new List<TItem>();
			var result = new List<TItem>(itemsToAdd.Count);

			foreach (string itemName in itemsToAdd)
			{
				TItem existingItem = this.FirstOrDefault(a => existingComparer(a, itemName));
				if (existingItem != null)
				{
					result.Add(existingItem);
				}
				else
				{
					TItem newItem = typeof(TItem).CrateInstance<TItem>(itemName);
					toAdd.Add(newItem);
					result.Add(newItem);
				}
			}

			if (toAdd.Count > 0)
			{
				Add(toAdd);
			}

			return result;
		}

		/// <summary>
		/// Adds multiple items by name using case-insensitive name comparison.
		/// </summary>
		/// <param name="itemsToAdd">The list of item names to add.</param>
		/// <returns>An enumerable of added or existing items.</returns>
		public virtual IEnumerable<TItem> Add(List<string> itemsToAdd)
		{
			return Add(itemsToAdd, (existingItem, newName) =>
				existingItem.Name.Equals(newName, StringComparison.InvariantCultureIgnoreCase));
		}

		/// <summary>
		/// Adds a single item to the collection.
		/// </summary>
		/// <param name="itemToAdd">The item to add.</param>
		/// <exception cref="Exception">Thrown if an item with the same ID already exists.</exception>
		public virtual void Add(TItem itemToAdd)
		{
			if (Items.ContainsKey(itemToAdd.Id))
			{
				throw new Exception($"Item {itemToAdd.Id} already exists.");
			}

			lock (collectionLock)
			{
				if (isPersistent)
				{
					SaveItemData(itemToAdd);
				}

				Items.TryAdd(itemToAdd.Id, itemToAdd);
			}

			OnCollectionChanged(new List<TItem> { itemToAdd }, new List<TItem>());
		}

		/// <summary>
		/// Adds multiple items to the collection.
		/// </summary>
		/// <param name="itemsToAdd">The items to add.</param>
		/// <exception cref="Exception">Thrown if any item with the same ID already exists.</exception>
		public virtual void Add(IEnumerable<TItem> itemsToAdd)
		{
			if (itemsToAdd?.Any() != true)
			{
				return;
			}

			lock (collectionLock)
			{
				foreach (TItem item in itemsToAdd)
				{
					if (Items.ContainsKey(item.Id))
					{
						throw new Exception($"Item {item.Id} already exists.");
					}

					if (isPersistent)
					{
						SaveItemData(item);
					}

					Items.TryAdd(item.Id, item);
				}
			}

			OnCollectionChanged(itemsToAdd.ToList(), new List<TItem>());
		}

		#endregion

		#region Remove Operations

		/// <summary>
		/// Removes an item with the specified ID from the collection.
		/// </summary>
		/// <param name="id">The ID of the item to remove.</param>
		/// <returns>True if the item was removed successfully.</returns>
		/// <exception cref="Exception">Thrown if the item doesn't exist.</exception>
		public virtual bool Remove(Guid id)
		{
			TItem item;
			lock (collectionLock)
			{
				if (!Items.TryGetValue(id, out item))
				{
					throw new Exception($"Item {id} doesn't exist.");
				}

				if (isPersistent)
				{
					FileSystem.DeleteFile(GetItemFilePath(id));
				}

				Items.TryRemove(id, out _);
			}

			OnCollectionChanged(new List<TItem>(), new List<TItem> { item });
			return true;
		}

		/// <summary>
		/// Removes the specified item from the collection.
		/// </summary>
		/// <param name="itemToRemove">The item to remove.</param>
		/// <returns>True if the item was removed successfully.</returns>
		public virtual bool Remove(TItem itemToRemove)
		{
			return Remove(itemToRemove.Id);
		}

		/// <summary>
		/// Removes multiple items from the collection.
		/// </summary>
		/// <param name="itemsToRemove">The items to remove.</param>
		/// <returns>True if all items were removed successfully; otherwise, false.</returns>
		/// <exception cref="Exception">Thrown if any item doesn't exist.</exception>
		public virtual bool Remove(IEnumerable<TItem> itemsToRemove)
		{
			if (itemsToRemove?.Any() != true)
			{
				return false;
			}

			lock (collectionLock)
			{
				foreach (TItem item in itemsToRemove)
				{
					TItem existing = Get(item.Id);
					if (existing == null)
					{
						throw new Exception($"Item {item.Id} doesn't exist.");
					}

					if (isPersistent)
					{
						FileSystem.DeleteFile(GetItemFilePath(item.Id));
					}

					Items.TryRemove(item.Id, out _);
				}
			}

			OnCollectionChanged(new List<TItem>(), itemsToRemove.ToList());
			return true;
		}

		#endregion

		#region Update Operations

		/// <summary>
		/// Updates a single item in the collection.
		/// </summary>
		/// <param name="itemToUpdate">The item with updated data.</param>
		public virtual void Update(TItem itemToUpdate)
		{
			if (itemToUpdate == null)
			{
				throw new ArgumentNullException(nameof(itemToUpdate));
			}

			TItem oldData;
			TItem loadedItem = null;

			lock (collectionLock)
			{
				// Try to get the existing item.
				if (isPersistent)
				{
					try
					{
						oldData = GetItemData(itemToUpdate.Id);
					}
					catch (Exception e)
					{
						logger.Error(e, "Failed to read stored item data.");
						oldData = null;
					}

					// Fallback in case of corrupted database.
					if (oldData == null)
					{
						var existingItem = Get(itemToUpdate.Id);

						oldData = existingItem != null
							? Serialization.GetClone(existingItem)
							: null;
					}
				}
				else
				{
					var existingItem = Get(itemToUpdate.Id);
					oldData = existingItem != null ? Serialization.GetClone(existingItem) : null;
				}

				// If item doesn't exist, don't update.
				if (oldData == null)
				{
					Add(itemToUpdate);
					loadedItem = Get(itemToUpdate.Id);

					oldData = null;
				}
				else
				{
					// Save new data.
					if (isPersistent)
					{
						SaveItemData(itemToUpdate);
					}

					// Update memory instance using fresh deserialized data (ensures lists are correct).
					loadedItem = Get(itemToUpdate.Id);
					if (loadedItem != null)
					{
						var fresh = isPersistent ? GetItemData(itemToUpdate.Id) : itemToUpdate;
						if (!ReferenceEquals(fresh, loadedItem))
						{
							fresh.CopyDiffTo(loadedItem);
						}
					}
				}
			}

			// Notify change.
			OnItemUpdated(new List<ItemUpdateEvent<TItem>>
			{
				new ItemUpdateEvent<TItem>(oldData, loadedItem)
			});
		}

		/// <summary>
		/// Updates multiple items in the collection.
		/// </summary>
		/// <param name="itemsToUpdate">The items with updated data.</param>
		public virtual void Update(IEnumerable<TItem> itemsToUpdate)
		{
			var updates = new List<ItemUpdateEvent<TItem>>();

			lock (collectionLock)
			{
				foreach (TItem item in itemsToUpdate)
				{
					TItem oldData = GetOldItemData(item.Id);
					if (oldData == null)
					{
						logger.Error($"Item {item.Id} doesn't exist.");
						continue;
					}

					if (isPersistent)
					{
						SaveItemData(item);
					}

					TItem loadedItem = Get(item.Id);
					if (!ReferenceEquals(loadedItem, item))
					{
						item.CopyDiffTo(loadedItem);
					}

					updates.Add(new ItemUpdateEvent<TItem>(oldData, loadedItem));
				}
			}

			if (updates.Count > 0)
			{
				OnItemUpdated(updates);
			}
		}

		/// <summary>
		/// Retrieves the old data for an item, handling corruption gracefully.
		/// </summary>
		/// <param name="id">The ID of the item.</param>
		/// <returns>The old item data, or null if it doesn't exist.</returns>
		private TItem GetOldItemData(Guid id)
		{
			if (isPersistent)
			{
				try
				{
					return GetItemData(id);
				}
				catch (Exception e)
				{
					// Handle corrupted database files (e.g., from problematic game launchers)
					logger.Error(e, "Failed to read stored item data.");
					return Serialization.GetClone(this[id]);
				}
			}
			else
			{
				var existingItem = Get(id);
				return existingItem != null ? Serialization.GetClone(existingItem) : null;
			}
		}

		#endregion

		#region Event Management

		/// <summary>
		/// Triggers the ItemCollectionChanged event.
		/// </summary>
		/// <param name="addedItems">List of items that were added.</param>
		/// <param name="removedItems">List of items that were removed.</param>
		internal void OnCollectionChanged(List<TItem> addedItems, List<TItem> removedItems)
		{
			if (!IsEventsEnabled)
			{
				return;
			}

			if (!isEventBufferEnabled)
			{
				ItemCollectionChanged?.Invoke(this, new ItemCollectionChangedEventArgs<TItem>(addedItems, removedItems));
			}
			else
			{
				lock (collectionLock)
				{
					addedItemsEventBuffer.AddRange(addedItems);
					removedItemsEventBuffer.AddRange(removedItems);
				}
			}
		}

		/// <summary>
		/// Triggers the ItemUpdated event.
		/// </summary>
		/// <param name="updates">Collection of item update events.</param>
		internal void OnItemUpdated(IEnumerable<ItemUpdateEvent<TItem>> updates)
		{
			if (!IsEventsEnabled)
			{
				return;
			}

			if (!isEventBufferEnabled)
			{
				ItemUpdated?.Invoke(this, new ItemUpdatedEventArgs<TItem>(updates));
			}
			else
			{
				lock (collectionLock)
				{
					foreach (ItemUpdateEvent<TItem> update in updates)
					{
						if (itemUpdatesEventBuffer.TryGetValue(update.NewData.Id, out ItemUpdateEvent<TItem> existing))
						{
							existing.NewData = update.NewData;
						}
						else
						{
							itemUpdatesEventBuffer.Add(update.NewData.Id, update);
						}
					}
				}
			}
		}

		/// <summary>
		/// Begins buffering collection change events.
		/// </summary>
		public void BeginBufferUpdate()
		{
			lock (collectionLock)
			{
				isEventBufferEnabled = true;
				bufferDepth++;
			}
		}

		/// <summary>
		/// Ends buffering and triggers all buffered events.
		/// Supports nested buffer updates.
		/// </summary>
		public void EndBufferUpdate()
		{
			List<TItem> addedCopy = null;
			List<TItem> removedCopy = null;
			List<ItemUpdateEvent<TItem>> updatesCopy = null;

			lock (collectionLock)
			{
				if (bufferDepth > 0)
				{
					bufferDepth--;
				}

				if (bufferDepth == 0)
				{
					isEventBufferEnabled = false;

					if (addedItemsEventBuffer.Count > 0 || removedItemsEventBuffer.Count > 0)
					{
						addedCopy = addedItemsEventBuffer.ToList();
						removedCopy = removedItemsEventBuffer.ToList();
						addedItemsEventBuffer.Clear();
						removedItemsEventBuffer.Clear();
					}

					if (itemUpdatesEventBuffer.Count > 0)
					{
						updatesCopy = itemUpdatesEventBuffer.Values.ToList();
						itemUpdatesEventBuffer.Clear();
					}
				}
			}

			if (addedCopy != null && (addedCopy.Count > 0 || removedCopy.Count > 0))
			{
				ItemCollectionChanged?.Invoke(this, new ItemCollectionChangedEventArgs<TItem>(addedCopy, removedCopy));
			}

			if (updatesCopy != null && updatesCopy.Count > 0)
			{
				ItemUpdated?.Invoke(this, new ItemUpdatedEventArgs<TItem>(updatesCopy));
			}
		}

		#endregion

		#region ICollection Implementation

		/// <summary>
		/// Clears all items from the collection.
		/// </summary>
		public void Clear()
		{
			List<TItem> removedItems;
			lock (collectionLock)
			{
				removedItems = Items.Values.ToList();
				if (isPersistent)
				{
					foreach (var item in removedItems)
					{
						FileSystem.DeleteFile(GetItemFilePath(item.Id));
					}
				}
				Items.Clear();
			}

			if (removedItems.Count > 0)
			{
				OnCollectionChanged(new List<TItem>(), removedItems);
			}
		}

		/// <summary>
		/// Determines whether the collection contains a specific item.
		/// </summary>
		/// <param name="item">The item to locate.</param>
		/// <returns>True if the item is found; otherwise, false.</returns>
		public bool Contains(TItem item)
		{
			return Items.ContainsKey(item.Id);
		}

		/// <summary>
		/// Copies the items of the collection to an array.
		/// </summary>
		/// <param name="array">The destination array.</param>
		/// <param name="arrayIndex">The zero-based index at which copying begins.</param>
		public void CopyTo(TItem[] array, int arrayIndex)
		{
			Items.Values.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator for the collection.</returns>
		public IEnumerator<TItem> GetEnumerator()
		{
			return Items.Values.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator for the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return Items.Values.GetEnumerator();
		}

		#endregion

		#region Utility Methods

		/// <summary>
		/// Creates a deep clone of all items in the collection.
		/// </summary>
		/// <returns>An enumerable of cloned items.</returns>
		public IEnumerable<TItem> GetClone()
		{
			return this.Select(a => Serialization.GetClone(a));
		}

		private sealed class BufferScope : IDisposable
		{
			private readonly ItemCollection<TItem> owner;
			public BufferScope(ItemCollection<TItem> owner) => this.owner = owner;
			public void Dispose() => owner.EndBufferUpdate();
		}

		/// <summary>
		/// Creates a buffered update context.
		/// </summary>
		/// <returns>A disposable buffered update context.</returns>

		public IDisposable BufferedUpdate()
		{
			BeginBufferUpdate();
			return new BufferScope(this);
		}

		#endregion

		#region IDisposable Implementation

		/// <summary>
		/// Releases all resources used by the collection.
		/// </summary>
		public void Dispose()
		{
			addedItemsEventBuffer?.Clear();
			removedItemsEventBuffer?.Clear();
			itemUpdatesEventBuffer?.Clear();
		}

		#endregion
	}
}
