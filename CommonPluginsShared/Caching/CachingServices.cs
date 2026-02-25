using CommonPlayniteShared;
using CommonPlayniteShared.Common;
using CommonPlayniteShared.Common.Web;
using CommonPluginsShared.Images;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace CommonPluginsShared.Caching
{
	/// <summary>
	/// Represents a cached item with expiration tracking.
	/// </summary>
	/// <typeparam name="T">Type of cached data</typeparam>
	public class CacheItem<T>
	{
		/// <summary>
		/// Gets or sets the cached data.
		/// </summary>
		public T Data { get; set; }

		/// <summary>
		/// Gets or sets the expiration timestamp.
		/// </summary>
		public DateTime ExpirationTime { get; set; }

		/// <summary>
		/// Gets whether the cache item has expired.
		/// </summary>
		public bool IsExpired => DateTime.Now > ExpirationTime;

		/// <summary>
		/// Initializes a new cache item with TTL.
		/// </summary>
		/// <param name="data">Data to cache</param>
		/// <param name="ttl">Time to live</param>
		public CacheItem(T data, TimeSpan ttl)
		{
			Data = data;
			ExpirationTime = DateTime.Now.Add(ttl);
		}
	}

	/// <summary>
	/// Thread-safe generic cache with expiration support.
	/// </summary>
	/// <typeparam name="T">Type of values to cache</typeparam>
	public class SmartCache<T>
	{
		private static readonly ILogger Logger = LogManager.GetLogger();

		private readonly Dictionary<string, CacheItem<T>> _cache = new Dictionary<string, CacheItem<T>>();
		private readonly object _lockObject = new object();

		/// <summary>
		/// Gets a cached value or creates it using the factory function.
		/// </summary>
		/// <param name="key">Cache key</param>
		/// <param name="factory">Factory function to generate value if not cached</param>
		/// <param name="ttl">Time to live for the cached value</param>
		/// <returns>Cached or newly created value</returns>
		public T GetOrSet(string key, Func<T> factory, TimeSpan ttl)
		{
#if DEBUG
			var timer = new DebugTimer(string.Format("SmartCache<{0}>.GetOrSet(key='{1}')", typeof(T).Name, key));
#endif
			lock (_lockObject)
			{
				if (_cache.TryGetValue(key, out CacheItem<T> item) && !item.IsExpired)
				{
#if DEBUG
					timer.Stop("hit");
#endif
					return item.Data;
				}

#if DEBUG
				timer.Step("miss — invoking factory");
#endif

				T data = factory();
				_cache[key] = new CacheItem<T>(data, ttl);

#if DEBUG
				timer.Stop("factory done, value cached");
#endif
				return data;
			}
		}

		/// <summary>
		/// Manually sets a cache value.
		/// </summary>
		/// <param name="key">Cache key</param>
		/// <param name="value">Value to cache</param>
		/// <param name="ttl">Time to live</param>
		public void Set(string key, T value, TimeSpan ttl)
		{
#if DEBUG
			Logger.Debug(string.Format("[SmartCache<{0}>.Set] key='{1}'", typeof(T).Name, key));
#endif
			lock (_lockObject)
			{
				_cache[key] = new CacheItem<T>(value, ttl);
			}
		}

		/// <summary>
		/// Clears all cached items.
		/// </summary>
		public void Clear()
		{
#if DEBUG
			Logger.Debug(string.Format("[SmartCache<{0}>.Clear] all entries removed", typeof(T).Name));
#endif
			lock (_lockObject)
			{
				_cache.Clear();
			}
		}

		/// <summary>
		/// Removes a specific cache entry.
		/// </summary>
		/// <param name="key">Cache key to remove</param>
		/// <returns>True if item was removed, false if not found</returns>
		public bool Remove(string key)
		{
#if DEBUG
			Logger.Debug(string.Format("[SmartCache<{0}>.Remove] key='{1}'", typeof(T).Name, key));
#endif
			lock (_lockObject)
			{
				return _cache.Remove(key);
			}
		}
	}

	/// <summary>
	/// HTTP file caching service for plugin resources.
	/// Downloads and caches web files locally with optional resizing.
	/// </summary>
	public class HttpFileCacheService
	{
		private static readonly ILogger Logger = LogManager.GetLogger();
		private static readonly object CacheLock = new object();

		/// <summary>
		/// Gets or sets the cache directory path.
		/// Default: Playnite images cache path.
		/// </summary>
		public static string CacheDirectory { get; set; } = PlaynitePaths.ImagesCachePath;

		/// <summary>
		/// Generates a unique cache filename from URL using MD5 hash.
		/// </summary>
		/// <param name="url">Source URL</param>
		/// <returns>Filename with original extension</returns>
		private static string GetFileNameFromUrl(string url)
		{
			Uri uri = new Uri(url);
			string extension = Path.GetExtension(uri.Segments[uri.Segments.Length - 1]);
			string md5 = url.MD5();
			return md5 + extension;
		}

		/// <summary>
		/// Checks if a web file is already cached locally.
		/// </summary>
		/// <param name="url">URL to check</param>
		/// <returns>True if cached and valid, false otherwise</returns>
		public static bool IsFileCached(string url)
		{
			if (string.IsNullOrEmpty(url))
			{
				return true;
			}

			if (!StringExtensions.IsHttpUrl(url))
			{
				return true;
			}

			string cacheFile = Path.Combine(CacheDirectory, GetFileNameFromUrl(url));
			bool exists = File.Exists(cacheFile) && new FileInfo(cacheFile).Length > 0;

#if DEBUG
			Logger.Debug(string.Format("[HttpFileCacheService.IsFileCached] url='{0}' cached={1}", url, exists));
#endif
			return exists;
		}

		/// <summary>
		/// Downloads and caches a web file locally with optional resizing.
		/// Thread-safe implementation with file locking.
		/// </summary>
		/// <param name="url">Source URL</param>
		/// <param name="resize">Target size for resizing (0 = no resize)</param>
		/// <returns>Local cache file path or empty string on error</returns>
		public static string GetWebFile(string url, int resize = 0)
		{
#if DEBUG
			var timer = new DebugTimer(string.Format("HttpFileCacheService.GetWebFile(url='{0}', resize={1})", url, resize));
#endif
			if (string.IsNullOrEmpty(url))
			{
#if DEBUG
				timer.Stop("empty url, skip");
#endif
				return string.Empty;
			}

			string cacheFile = Path.Combine(CacheDirectory, GetFileNameFromUrl(url));

			lock (CacheLock)
			{
				if (File.Exists(cacheFile) && new FileInfo(cacheFile).Length > 0)
				{
#if DEBUG
					timer.Stop("disk hit");
#endif
					return cacheFile;
				}

#if DEBUG
				timer.Step("disk miss — downloading");
#endif

				FileSystem.CreateDirectory(CacheDirectory);

				try
				{
					if (resize > 0)
					{
						string tmpPath = Path.Combine(PlaynitePaths.ImagesCachePath, Path.GetFileName(cacheFile));
						HttpDownloader.DownloadFile(url, tmpPath);

#if DEBUG
						timer.Step("download done, resizing");
#endif

						ImageTools.Resize(tmpPath, resize, resize, cacheFile);
						FileSystem.DeleteFileSafe(tmpPath);
					}
					else
					{
						HttpDownloader.DownloadFile(url, cacheFile);
					}

#if DEBUG
					timer.Stop("cached to disk");
#endif
					return cacheFile;
				}
				catch (WebException e)
				{
					if (e.Response == null)
					{
#if DEBUG
						Logger.Debug(string.Format("[HttpFileCacheService.GetWebFile] network error (no response) for url='{0}': {1}", url, e.Message));
#endif
						throw;
					}

					HttpWebResponse response = (HttpWebResponse)e.Response;
					if (response.StatusCode != HttpStatusCode.NotFound)
					{
#if DEBUG
						Logger.Debug(string.Format("[HttpFileCacheService.GetWebFile] HTTP {0} for url='{1}'", (int)response.StatusCode, url));
#endif
						throw;
					}

#if DEBUG
					timer.Stop(string.Format("HTTP 404 — url='{0}'", url));
#endif
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// Removes a cached file from disk.
		/// </summary>
		/// <param name="url">Original URL of the cached file</param>
		public static void ClearCache(string url)
		{
			if (string.IsNullOrEmpty(url))
			{
				return;
			}

			lock (CacheLock)
			{
				string cacheFile = Path.Combine(CacheDirectory, GetFileNameFromUrl(url));
				if (File.Exists(cacheFile))
				{
					try
					{
#if DEBUG
						Logger.Debug(string.Format("[HttpFileCacheService.ClearCache] removing url='{0}'", url));
#endif
						FileSystem.DeleteFileSafe(cacheFile);
					}
					catch (Exception e)
					{
						Logger.Error(e, $"Failed to remove {url} from cache.");
					}
				}
			}
		}

		/// <summary>
		/// Clears all cached files in the cache directory.
		/// </summary>
		public static void ClearAllCache()
		{
#if DEBUG
			Logger.Debug("[HttpFileCacheService.ClearAllCache] start");
#endif
			lock (CacheLock)
			{
				try
				{
					if (Directory.Exists(CacheDirectory))
					{
						foreach (string file in Directory.GetFiles(CacheDirectory))
						{
							FileSystem.DeleteFileSafe(file);
						}
					}
#if DEBUG
					Logger.Debug("[HttpFileCacheService.ClearAllCache] done");
#endif
				}
				catch (Exception e)
				{
					Logger.Error(e, "Failed to clear all cache.");
				}
			}
		}
	}
}