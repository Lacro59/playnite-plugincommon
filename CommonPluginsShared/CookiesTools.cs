using CommonPlayniteShared.Common;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace CommonPluginsShared
{
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

        public List<HttpCookie> GetStoredCookies()
        {
            string message = $"No stored cookies for {ClientName}";
            List<HttpCookie> storedCookies = new List<HttpCookie>();

            if (File.Exists(FileCookies))
            {
                try
                {
                    storedCookies = Serialization.FromJson<List<HttpCookie>>(
                        Encryption.DecryptFromFile(
                            FileCookies,
                            Encoding.UTF8,
                            WindowsIdentity.GetCurrent().User.Value));

                    bool hasExpired = storedCookies.Any(x => x.Expires != null && (DateTime)x.Expires <= DateTime.Now);
                    if (hasExpired)
                    {
                        message = $"Expired cookies for {ClientName}";
                    }
                    else
                    {
                        return storedCookies;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to load saved cookies for {ClientName}");
                }
            }

            Logger.Warn(message);
            return storedCookies;
        }

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
        /// Retrieve cookies from WebView, optionally filtered by domain and optionally deleting them.
        /// </summary>
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
        /// Retrieve new cookies from specified URLs using a WebView, optionally deleting them.
        /// </summary>
        public List<HttpCookie> GetNewWebCookies(List<string> urls, bool deleteCookies = false, IWebView webView = null)
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

                List<HttpCookie> oldCookies = GetStoredCookies();
                oldCookies?.ForEach(cookie =>
                {
                    string domain = cookie.Domain.StartsWith(".") ? cookie.Domain.Substring(1) : cookie.Domain;
                    webView.SetCookies("https://" + domain, cookie);
                });

                urls.ForEach(url =>
                {
                    webView.NavigateAndWait(url);
                    Thread.Sleep(1000);
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