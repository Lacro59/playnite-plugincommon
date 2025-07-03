using System;
using System.Collections.Generic;

namespace CommonPluginsShared
{
    /// <summary>
    /// Represents a cached item with its data and expiration time.
    /// </summary>
    /// <typeparam name="T">Type of the cached data.</typeparam>
    public class CacheItem<T>
    {
        /// <summary>
        /// Gets or sets the cached data.
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Gets or sets the expiration time of the cached item.
        /// </summary>
        public DateTime ExpirationTime { get; set; }

        /// <summary>
        /// Gets a value indicating whether the cached item is expired.
        /// </summary>
        public bool IsExpired => DateTime.Now > ExpirationTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="data">The data to cache.</param>
        /// <param name="ttl">Time to live for the cached data.</param>
        public CacheItem(T data, TimeSpan ttl)
        {
            Data = data;
            ExpirationTime = DateTime.Now.Add(ttl);
        }
    }

    /// <summary>
    /// Provides a simple thread-safe cache mechanism with expiration.
    /// </summary>
    /// <typeparam name="T">Type of the values to cache.</typeparam>
    public class SmartCache<T>
    {
        private readonly Dictionary<string, CacheItem<T>> _cache = new Dictionary<string, CacheItem<T>>();
        private readonly object _lockObject = new object();

        /// <summary>
        /// Gets a value from the cache, or creates and stores it using the factory method if not present or expired.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">The factory method to generate the value if not cached.</param>
        /// <param name="ttl">Time to live for the cached value.</param>
        /// <returns>The cached or newly created value.</returns>
        public T GetOrSet(string key, Func<T> factory, TimeSpan ttl)
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var item) && !item.IsExpired)
                {
                    return item.Data;
                }

                var data = factory();
                _cache[key] = new CacheItem<T>(data, ttl);
                return data;
            }
        }

        /// <summary>
        /// Sets a value in the cache manually.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="ttl">Time to live for the cached value.</param>
        public void Set(string key, T value, TimeSpan ttl)
        {
            lock (_lockObject)
            {
                _cache[key] = new CacheItem<T>(value, ttl);
            }
        }

        /// <summary>
        /// Clears all items from the cache.
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _cache.Clear();
            }
        }
    }
}