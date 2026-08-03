using CommonPlayniteShared.Common;//using Playnite.Common;
using CommonPlayniteShared.Common.Web;//using Playnite.Common.Web;
using Playnite.SDK;
using CommonPlayniteShared;//using Playnite.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommonPlayniteShared
{
    public class HttpFileCache
    {
        private static ILogger logger = LogManager.GetLogger();
        private static readonly object cacheLock = new object();
        private static readonly ConcurrentDictionary<string, object> fileLocks = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public static string CacheDirectory { get; set; } = PlaynitePaths.ImagesCachePath;

        private static string GetFileNameFromUrl(string url)
        {
            var uri = new Uri(url);
            var extension = Path.GetExtension(uri.Segments[uri.Segments.Length - 1]);
            var md5 = url.MD5();
            return md5 + extension;
        }

        public static string GetWebFile(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            var cacheFile = Path.Combine(CacheDirectory, GetFileNameFromUrl(url));

            if (File.Exists(cacheFile) && (new FileInfo(cacheFile)).Length != 0)
            {
                return cacheFile;
            }

            var fileLock = fileLocks.GetOrAdd(cacheFile, _ => new object());
            lock (fileLock)
            {
                if (File.Exists(cacheFile) && (new FileInfo(cacheFile)).Length != 0)
                {
                    return cacheFile;
                }

                FileSystem.CreateDirectory(CacheDirectory);

                try
                {
                    HttpDownloader.DownloadFile(url, cacheFile);
                    return cacheFile;
                }
                catch (WebException e)
                {
                    if (e.Response == null)
                    {
                        throw;
                    }

                    var response = (HttpWebResponse)e.Response;
                    if (response.StatusCode != HttpStatusCode.NotFound)
                    {
                        throw;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
        }

        public static void ClearCache(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            lock (cacheLock)
            {
                var cacheFile = Path.Combine(CacheDirectory, GetFileNameFromUrl(url));
                if (File.Exists(cacheFile))
                {
                    //logger.Debug($"Removing {url} from file cache: {cacheFile}");
                    try
                    {
                        FileSystem.DeleteFileSafe(cacheFile);
                    }
                    catch (Exception e) //when (!PlayniteEnvironment.ThrowAllErrors)
                    {
                        logger.Error(e, $"Failed to remove {url} from cache.");
                    }
                }
            }
        }
    }
}
