using CommonPlayniteShared.Common;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace CommonPluginsShared
{
    /// <summary>
    /// Encrypts, stores, and retrieves HTTP cookies for plugin WebView authentication flows.
    /// </summary>
    public class CookiesTools
    {
        internal ILogger Logger => LogManager.GetLogger();

        #region Properties

        /// <summary>
        /// Plugin name associated with this tool.
        /// </summary>
        public string PluginName { get; }

        /// <summary>
        /// Client name used for filtering or identification.
        /// </summary>
        public string ClientName { get; }

        /// <summary>
        /// Full path to the cookie storage file.
        /// </summary>
        public string FileCookies { get; }

        /// <summary>
        /// List of cookie domains to include or filter.
        /// </summary>
        public List<string> CookiesDomains { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor to initialize cookies tool settings.
        /// </summary>
        public CookiesTools(string pluginName, string clientName, string fileCookies, List<string> cookiesDomains)
        {
            PluginName = pluginName;
            ClientName = clientName;
            FileCookies = fileCookies;
            CookiesDomains = cookiesDomains ?? new List<string>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads encrypted cookies from disk for the configured client.
        /// Expired cookies are removed before returning.
        /// </summary>
        /// <returns>The stored cookie list, or an empty list when none are available.</returns>
        public List<HttpCookie> GetStoredCookies()
        {
            string message = $"No stored cookies for {ClientName}";
            List<HttpCookie> storedCookies = new List<HttpCookie>();

            if (File.Exists(FileCookies))
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        var decrypted = Encryption.DecryptFromFile(
                            FileCookies,
                            Encoding.UTF8,
                            WindowsIdentity.GetCurrent().User.Value);

                        storedCookies = Serialization.FromJson<List<HttpCookie>>(decrypted);

                        storedCookies.RemoveAll(x => x.Expires != null && (DateTime)x.Expires <= DateTime.Now);
                        return storedCookies;
                    }
                    catch (CryptographicException ex)
                    {
                        Common.LogError(ex, false, $"Failed to load saved cookies for {ClientName} (CryptographicException)");
                        FileSystem.DeleteFile(FileCookies);
                        return storedCookies;
                    }
                    catch (Exception ex)
                    {
                        if (i == 4)
                        {
                            Common.LogError(ex, false, $"Failed to load saved cookies for {ClientName}");
                            FileSystem.DeleteFile(FileCookies);
                            return storedCookies;
                        }
                        Thread.Sleep(500);
                    }
                }
            }

            Logger.Warn(message);
            return storedCookies;
        }

        /// <summary>
        /// Encrypts and persists the provided cookies to the configured storage file.
        /// </summary>
        /// <param name="httpCookies">Cookies to save. Nothing is written when null or empty.</param>
        /// <returns><c>true</c> when cookies were saved; otherwise <c>false</c>.</returns>
        public bool SetStoredCookies(List<HttpCookie> httpCookies)
        {
            try
            {
                if (httpCookies != null && httpCookies.Any())
                {
                    FileSystem.CreateDirectory(Path.GetDirectoryName(FileCookies));
                    Encryption.EncryptToFile(
                        FileCookies,
                        Serialization.ToJson(httpCookies),
                        Encoding.UTF8,
                        WindowsIdentity.GetCurrent().User.Value);
                    return true;
                }
                else
                {
                    Logger.Warn($"No cookies saved for {PluginName}");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "Failed to save cookies");
            }

            return false;
        }

        /// <summary>
        /// Deletes the encrypted cookie file for this client, if it exists.
        /// </summary>
        public void ClearStoredCookies()
        {
            if (File.Exists(FileCookies))
            {
                FileSystem.DeleteFile(FileCookies);
                Logger.Info($"{ClientName} stored cookies file deleted");
                Common.LogDebug(true, $"{ClientName} ClearStoredCookies: {FileCookies}");
            }
            else
            {
                Common.LogDebug(true, $"{ClientName} ClearStoredCookies: no cookie file found");
            }
        }

        /// <summary>
        /// Clears cookies for configured domains from an offscreen WebView.
        /// </summary>
        public void ClearDomainCookies()
        {
            if (CookiesDomains == null || !CookiesDomains.Any())
            {
                Common.LogDebug(true, $"{ClientName} ClearDomainCookies: no domains configured");
                return;
            }

            IWebView webView = null;

            try
            {
                webView = API.Instance.WebViews.CreateOffscreenView();
                Logger.Info($"{ClientName} clearing WebView cookies for {CookiesDomains.Count} domain(s)");

                foreach (string domain in CookiesDomains)
                {
                    webView.DeleteDomainCookies(domain);
                    Common.LogDebug(true, $"{ClientName} ClearDomainCookies: {domain}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{ClientName} ClearDomainCookies failed");
            }
            finally
            {
                webView?.Dispose();
            }
        }

        /// <summary>
        /// Retrieves cookies from a WebView, optionally filtered by domain and optionally deleting them.
        /// </summary>
        /// <param name="deleteCookies">When <c>true</c>, clears domain cookies from the WebView after extraction.</param>
        /// <param name="webView">Optional WebView instance. An offscreen view is created when null.</param>
        /// <returns>Filtered cookies for the configured domains.</returns>
        public List<HttpCookie> GetWebCookies(bool deleteCookies = false, IWebView webView = null)
        {
            bool createdLocally = webView == null;

            try
            {
                if (createdLocally)
                {
                    webView = API.Instance.WebViews.CreateOffscreenView();
                }

                return ExtractCookies(webView, deleteCookies);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, false, PluginName);
                return new List<HttpCookie>();
            }
            finally
            {
                if (createdLocally && webView != null)
                {
                    webView.Dispose();
                }
            }
        }

        /// <summary>
        /// Navigates to the specified URLs in a WebView and returns cookies set by those pages.
        /// Uses a default post-navigation delay of 1000 ms.
        /// </summary>
        /// <param name="urls">Absolute URLs to visit in order.</param>
        /// <param name="deleteCookies">When <c>true</c>, clears domain cookies from the WebView after extraction.</param>
        /// <param name="webView">Optional WebView instance. An offscreen view is created when null.</param>
        /// <returns>Filtered cookies for the configured domains.</returns>
        public List<HttpCookie> GetNewWebCookies(List<string> urls, bool deleteCookies = false, IWebView webView = null)
        {
            return GetNewWebCookies(urls, deleteCookies, webView, 1000, null);
        }

        /// <summary>
        /// Navigates to the specified URLs in a WebView and returns cookies set by those pages.
        /// Stored cookies are injected into the WebView before navigation.
        /// </summary>
        /// <param name="urls">Absolute URLs to visit in order.</param>
        /// <param name="deleteCookies">When <c>true</c>, clears domain cookies from the WebView after extraction.</param>
        /// <param name="webView">Optional WebView instance. An offscreen view is created when null.</param>
        /// <param name="waitAfterNavigateMs">Delay in milliseconds after each navigation before reading cookies. Use 0 to skip the wait.</param>
        /// <returns>Filtered cookies for the configured domains, or an empty list on failure.</returns>
        public List<HttpCookie> GetNewWebCookies(List<string> urls, bool deleteCookies, IWebView webView, int waitAfterNavigateMs)
        {
            return GetNewWebCookies(urls, deleteCookies, webView, waitAfterNavigateMs, null);
        }

        /// <summary>
        /// Navigates to the specified URLs in a WebView and returns cookies set by those pages.
        /// </summary>
        /// <param name="urls">Absolute URLs to visit in order.</param>
        /// <param name="deleteCookies">When <c>true</c>, clears domain cookies from the WebView after extraction.</param>
        /// <param name="webView">Optional WebView instance. An offscreen view is created when null.</param>
        /// <param name="waitAfterNavigateMs">Delay in milliseconds after each navigation before reading cookies. Use 0 to skip the wait.</param>
        /// <param name="cookiesToInject">
        /// Cookies to inject before navigation. When <c>null</c>, all stored cookies are injected.
        /// Pass an empty list to skip injection.
        /// </param>
        /// <returns>Filtered cookies for the configured domains, or an empty list on failure.</returns>
        public List<HttpCookie> GetNewWebCookies(List<string> urls, bool deleteCookies, IWebView webView, int waitAfterNavigateMs, List<HttpCookie> cookiesToInject)
        {
            bool createdLocally = webView == null;

            try
            {
                if (createdLocally)
                {
                    WebViewSettings webViewSettings = new WebViewSettings
                    {
                        JavaScriptEnabled = true,
                        UserAgent = Web.UserAgent
                    };
                    webView = API.Instance.WebViews.CreateOffscreenView(webViewSettings);
                }

                List<HttpCookie> injectCookies = cookiesToInject ?? GetStoredCookies();
                if (injectCookies != null && injectCookies.Count > 0)
                {
                    Common.LogDebug(true, $"{ClientName} GetNewWebCookies: injecting {injectCookies.Count} cookie(s) before navigation");
                    foreach (HttpCookie cookie in injectCookies)
                    {
                        if (cookie == null)
                        {
                            continue;
                        }

                        string domain = cookie.Domain.StartsWith(".") ? cookie.Domain.Substring(1) : cookie.Domain;
                        Common.LogDebug(true, $"{ClientName} GetNewWebCookies: SetCookies domain='{domain}' name='{cookie.Name}'");
                        webView.SetCookies("https://" + domain, cookie);
                    }
                }
                else
                {
                    Common.LogDebug(true, $"{ClientName} GetNewWebCookies: skipping cookie injection");
                }

                int waitMs = waitAfterNavigateMs < 0 ? 0 : waitAfterNavigateMs;
                urls.ForEach(url =>
                {
                    Common.LogDebug(true, $"{ClientName} GetNewWebCookies: NavigateAndWait url='{url}'");
                    webView.NavigateAndWait(url);
                    if (waitMs > 0)
                    {
                        Thread.Sleep(waitMs);
                    }
                });

                return ExtractCookies(webView, deleteCookies);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, false, PluginName);
                return new List<HttpCookie>();
            }
            finally
            {
                if (createdLocally && webView != null)
                {
                    webView.Dispose();
                }
            }
        }

        /// <summary>
        /// Internal utility method to extract cookies from WebView, filtered and optionally cleared.
        /// </summary>
        private List<HttpCookie> ExtractCookies(IWebView webView, bool deleteCookies)
        {
            List<HttpCookie> httpCookies = CookiesDomains?.Count > 0
                ? webView.GetCookies()?.Where(x => CookiesDomains.Any(y => y.Contains(x?.Domain, StringComparison.OrdinalIgnoreCase)))?.ToList() ?? new List<HttpCookie>()
                : webView.GetCookies()?.Where(x => x?.Domain?.Contains(ClientName, StringComparison.OrdinalIgnoreCase) ?? false)?.ToList() ?? new List<HttpCookie>();

            if (deleteCookies)
            {
                CookiesDomains.ForEach(x => webView.DeleteDomainCookies(x));
            }

            return httpCookies;
        }

        #endregion
    }
}