using CommonPlayniteShared;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommonPluginsShared
{
    // TODO https://stackoverflow.com/questions/62802238/very-slow-httpclient-sendasync-call

    public enum WebUserAgentType
    {
        Request
    }


    public class HttpHeader
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }


    public class Web
    {
        private static ILogger Logger => LogManager.GetLogger();

        public static string UserAgent => $"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:126.0) Gecko/20100101 Firefox/126.0";

        private static readonly HttpClient SharedClient;
        private static readonly HttpClientHandler SharedHandler;
        private const int MaxRedirects = 5;

        static Web()
        {
            try
            {
                SharedHandler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                SharedClient = new HttpClient(SharedHandler, disposeHandler: true)
                {
                    Timeout = TimeSpan.FromSeconds(60)
                };
                SharedClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            }
            catch (Exception ex)
            {
                // Fallback: if static client creation fails, leave SharedClient null and methods will create per-call clients
                Common.LogError(ex, false, "Failed to create shared HttpClient");
                SharedClient = null;
                SharedHandler = null;
            }
        }


        private static string StrWebUserAgentType(WebUserAgentType userAgentType)
        {
            switch (userAgentType)
            {
                case WebUserAgentType.Request:
                    return "request";
                default:
                    break;
            }
            return string.Empty;
        }


        /// <summary>
        /// Download file image and resize in icon format (64x64).
        /// </summary>
        /// <param name="imageFileName"></param>
        /// <param name="url"></param>
        /// <param name="imagesCachePath"></param>
        /// <param name="pluginName"></param>
        /// <returns></returns>
        public static Task<bool> DownloadFileImage(string imageFileName, string url, string imagesCachePath, string pluginName)
        {
            string PathImageFileName = Path.Combine(imagesCachePath, pluginName.ToLower(), imageFileName);

            if (!System.StringExtensions.IsHttpUrl(url))
            {
                return Task.FromResult(false);
            }

            using (var client = new HttpClient())
            {
                try
                {
                    var cachedFile = HttpFileCache.GetWebFile(url);


                    if (string.IsNullOrEmpty(cachedFile))
                    {
                        //logger.Warn("Web file not found: " + url);
                        return Task.FromResult(false);
                    }

                    ImageTools.Resize(cachedFile, 64, 64, PathImageFileName);
                }
                catch (Exception ex)
                {
                    if (!url.Contains("steamcdn-a.akamaihd.net", StringComparison.InvariantCultureIgnoreCase) && !ex.Message.Contains("(403)"))
                    {
                        Common.LogError(ex, false, $"Error on download {url}");
                    }
                    return Task.FromResult(false);
                }
            }

            // Delete file is empty
            try
            {
                if (File.Exists(PathImageFileName + ".png"))
                {
                    FileInfo fi = new FileInfo(PathImageFileName + ".png");
                    if (fi.Length == 0)
                    {
                        File.Delete(PathImageFileName + ".png");
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on delete file image");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public static async Task<bool> DownloadFileImageTest(string url)
        {
            if (!url.ToLower().Contains("http"))
            {
                return false;
            }

            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                    HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on download {url}");
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Download file stream.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<Stream> DownloadFileStream(string url)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                    HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);
                    return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on download {url}");
                    return null;
                }
            }
        }

        public static async Task<Stream> DownloadFileStream(string url, List<HttpCookie> cookies)
        {
            HttpClientHandler handler = new HttpClientHandler();
            if (cookies != null)
            {
                handler.CookieContainer = CreateCookiesContainer(cookies);
            }

            using (var client = new HttpClient(handler))
            {
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                    HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);
                    return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on download {url}");
                    return null;
                }
            }
        }


        /// <summary>
        /// Download string data and keep url parameter when there is a redirection.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string> DownloadStringDataKeepParam(string url)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get
                };

                HttpResponseMessage response;
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                    response = await client.SendAsync(request).ConfigureAwait(false);

                    var uri = response.RequestMessage.RequestUri.ToString();
                    if (uri != url)
                    {
                        var urlParams = url.Split('?').ToList();
                        if (urlParams.Count == 2)
                        {
                            uri += "?" + urlParams[1];
                        }

                        return await DownloadStringDataKeepParam(uri);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on download {url}");
                    return string.Empty;
                }

                if (response == null)
                {
                    return string.Empty;
                }

                int statusCode = (int)response.StatusCode;

                // We want to handle redirects ourselves so that we can determine the final redirect Location (via header)
                if (statusCode >= 300 && statusCode <= 399)
                {
                    var redirectUri = response.Headers.Location;
                    if (!redirectUri.IsAbsoluteUri)
                    {
                        redirectUri = new Uri(request.RequestUri.GetLeftPart(UriPartial.Authority) + redirectUri);
                    }

                    Common.LogDebug(true, string.Format("DownloadStringData() redirecting to {0}", redirectUri));

                    return await DownloadStringDataKeepParam(redirectUri.ToString());
                }
                else
                {
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
        }


        /// <summary>
        /// Download compressed string data.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string> DownloadStringDataWithGz(string url)
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                return await client.GetStringAsync(url).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Download string data with manage redirect url.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string> DownloadStringData(string url, int redirectDepth = 0)
        {
            if (redirectDepth >= MaxRedirects)
            {
                Logger.Warn($"Maximum redirect depth {MaxRedirects} reached for {url}");
                return string.Empty;
            }

            // Prefer using a shared HttpClient for connection reuse and higher parallelism. Fall back to per-call if unavailable.
            if (SharedClient != null)
            {
                using (var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get
                })
                {
                    HttpResponseMessage response = null;
                    try
                    {
                        using (response = await SharedClient.SendAsync(request).ConfigureAwait(false))
                        {
                            return await ProcessDownloadStringResponse(response, request, redirectDepth).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error on download {url}");
                        return string.Empty;
                    }
                }
            }
            else
            {
                // Fallback behaviour: create a per-call HttpClient as before
                using (HttpClient client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage()
                    {
                        RequestUri = new Uri(url),
                        Method = HttpMethod.Get
                    })
                    {
                        HttpResponseMessage response = null;
                        try
                        {
                            client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                            using (response = await client.SendAsync(request).ConfigureAwait(false))
                            {
                                return await ProcessDownloadStringResponse(response, request, redirectDepth).ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, $"Error on download {url}");
                            return string.Empty;
                        }
                    }
                }
            }
        }

        private static async Task<string> ProcessDownloadStringResponse(HttpResponseMessage response, HttpRequestMessage request, int redirectDepth)
        {
            if (response == null)
            {
                return string.Empty;
            }

            int statusCode = (int)response.StatusCode;

            // Handle redirects similarly to previous logic
            if (statusCode >= 300 && statusCode <= 399)
            {
                var redirectUri = response.Headers.Location;
                if (redirectUri == null)
                {
                    Logger.Warn($"DownloadStringData() redirect response missing Location header for {request?.RequestUri}");
                    return string.Empty;
                }

                if (!redirectUri.IsAbsoluteUri)
                {
                    redirectUri = new Uri(request.RequestUri.GetLeftPart(UriPartial.Authority) + redirectUri);
                }

                Common.LogDebug(true, string.Format("DownloadStringData() redirecting to {0}", redirectUri));

                // perform recursive call afterwards with increased depth
                return await DownloadStringData(redirectUri.ToString(), redirectDepth + 1).ConfigureAwait(false);
            }
            else
            {
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Download string data with a specific UserAgent.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="userAgentType"></param>
        /// <returns></returns>
        public static async Task<string> DownloadStringData(string url, WebUserAgentType userAgentType)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get
                };

                HttpResponseMessage response;
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                    client.DefaultRequestHeaders.UserAgent.TryParseAdd(StrWebUserAgentType(userAgentType));
                    response = await client.SendAsync(request).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on download {url}");
                    return string.Empty;
                }

                if (response == null)
                {
                    return string.Empty;
                }

                int statusCode = (int)response.StatusCode;
                if (statusCode == 200)
                {
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                else
                {
                    Logger.Warn($"DownloadStringData() with statuscode {statusCode} for {url}");
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Download string data with custom cookies.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookies"></param>
        /// <param name="userAgent"></param>
        /// <returns></returns>
        public static async Task<string> DownloadStringData(string url, List<HttpCookie> cookies = null, string userAgent = "", bool keepParam = false, int redirectDepth = 0)
        {
            if (redirectDepth >= MaxRedirects)
            {
                Logger.Warn($"Maximum redirect depth {MaxRedirects} reached for {url}");
                return string.Empty;
            }
            HttpClientHandler handler = new HttpClientHandler();
            if (cookies != null)
            {
                handler.CookieContainer = CreateCookiesContainer(cookies);
            }

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };

            HttpResponseMessage response;
            using (var client = new HttpClient(handler))
            {
                if (userAgent.IsNullOrEmpty())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                }
                else
                {
                    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                }

                try
                {
                    response = await client.SendAsync(request).ConfigureAwait(false);
                    int statusCode = (int)response.StatusCode;
                    bool IsRedirected = (request.RequestUri.ToString() != url) || (statusCode >= 300 && statusCode <= 399);

                    // We want to handle redirects ourselves so that we can determine the final redirect Location (via header)
                    if (IsRedirected)
                    {
                        string urlNew = request.RequestUri.ToString();
                        var redirectUri = response.Headers.Location;
                        if (!redirectUri?.IsAbsoluteUri ?? false)
                        {
                            redirectUri = new Uri(request.RequestUri.GetLeftPart(UriPartial.Authority) + redirectUri);
                            urlNew = redirectUri.ToString();
                        }

                        if (keepParam)
                        {
                            var urlParams = url.Split('?').ToList();
                            if (urlParams.Count == 2)
                            {
                                var urlNewParams = urlNew.Split('?').ToList();
                                if (urlNewParams.Count == 2)
                                {
                                    if (urlParams[1] != urlNewParams[1])
                                    {
                                        urlNew += "&" + urlParams[1];
                                    }
                                }
                                else
                                {
                                    urlNew += "?" + urlParams[1];
                                }
                            }
                        }

                        Common.LogDebug(true, string.Format("DownloadStringData() redirecting to {0}", urlNew));
                        return await DownloadStringData(urlNew, cookies, userAgent, keepParam, redirectDepth + 1);
                    }
                    else
                    {
                        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }

                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Section=ResponseHeader Detail=CR"))
                    {
                        Logger.Warn($"Used UserAgent: Anything");
                        return DownloadStringData(url, cookies, "Anything").GetAwaiter().GetResult();
                    }
                    else
                    {
                        Common.LogError(ex, false, $"Error on Get {url}");
                    }
                }
            }

            return string.Empty;
        }

        public static async Task<string> DownloadStringData(string url, CookieContainer cookies = null, string userAgent = "")
        {
            var response = string.Empty;

            HttpClientHandler handler = new HttpClientHandler();
            if (cookies?.Count > 0)
            {
                handler.CookieContainer = cookies;
            }

            using (var client = new HttpClient(handler))
            {
                if (userAgent.IsNullOrEmpty())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                }
                else
                {
                    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                }

                HttpResponseMessage result;
                try
                {
                    result = await client.GetAsync(url).ConfigureAwait(false);
                    if (result.IsSuccessStatusCode)
                    {
                        response = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        Logger.Error($"Web error with status code {result.StatusCode.ToString()}");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on Get {url}");
                }
            }

            return response;
        }

        public static async Task<string> DownloadStringData(string url, List<HttpHeader> httpHeaders = null, List<HttpCookie> cookies = null)
        {
            HttpClientHandler handler = new HttpClientHandler();
            if (cookies != null)
            {
                handler.CookieContainer = CreateCookiesContainer(cookies);
            }

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };

            string response = string.Empty;
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);

                if (httpHeaders != null)
                {
                    httpHeaders.ForEach(x =>
                    {
                        client.DefaultRequestHeaders.Add(x.Key, x.Value);
                    });
                }

                HttpResponseMessage result;
                try
                {
                    result = await client.GetAsync(url).ConfigureAwait(false);
                    if (result.IsSuccessStatusCode)
                    {
                        response = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        Logger.Error($"Web error with status code {result.StatusCode.ToString()}");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on Get {url}");
                }
            }

            return response;
        }

        /// <summary>
        /// Downloads string data from a URL using an optional token and language header.
        /// Optionally performs a pre-request to another URL before the main call.
        /// </summary>
        /// <param name="url">The URL to fetch the data from.</param>
        /// <param name="token">The Bearer token for Authorization header.</param>
        /// <param name="urlBefore">An optional URL to call before the main request (e.g., for session setup).</param>
        /// <param name="langHeader">Optional Accept-Language header value (e.g., "en-US").</param>
        /// <returns>The response content as a string.</returns>
        public static async Task<string> DownloadStringData(string url, string token, string urlBefore = "", string langHeader = "")
        {
            using (var client = new HttpClient())
            {
                // Set the user agent for the request
                client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);

                // Add Accept-Language header if provided
                if (!langHeader.IsNullOrWhiteSpace())
                {
                    client.DefaultRequestHeaders.Add("Accept-Language", langHeader);
                }

                // Make an optional preliminary request if specified
                if (!urlBefore.IsNullOrWhiteSpace())
                {
                    try
                    {
                        await client.GetStringAsync(urlBefore).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, $"Pre-request to {urlBefore} failed.");
                    }
                }

                // Add the Authorization header
                if (!token.IsNullOrWhiteSpace())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }

                // Perform the main request
                string result = await client.GetStringAsync(url).ConfigureAwait(false);
                return result;
            }
        }



        public static async Task<string> DownloadPageText(string url, List<HttpCookie> cookies = null, string userAgent = "")
        {
            WebViewSettings webViewSettings = new WebViewSettings
            {
                JavaScriptEnabled = true,
                UserAgent = userAgent.IsNullOrEmpty() ? Web.UserAgent : userAgent
            };

            using (IWebView webView = API.Instance.WebViews.CreateOffscreenView(webViewSettings))
            {
                cookies?.ForEach(x =>
                {
                    string domain = x.Domain.StartsWith(".") ? x.Domain.Substring(1) : x.Domain;
                    webView.SetCookies("https://" + domain, x);
                });

                webView.NavigateAndWait(url);
                return await webView.GetPageTextAsync();
            }
        }


        public static async Task<string> DownloadStringDataWithUrlBefore(string url, string urlBefore = "", string langHeader = "")
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);

                if (!langHeader.IsNullOrEmpty())
                {
                    client.DefaultRequestHeaders.Add("Accept-Language", langHeader);
                }

                if (!urlBefore.IsNullOrEmpty())
                {
                    await client.GetStringAsync(urlBefore).ConfigureAwait(false);
                }

                string result = await client.GetStringAsync(url).ConfigureAwait(false);
                return result;
            }
        }


        public static async Task<string> DownloadStringDataJson(string url)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                client.DefaultRequestHeaders.Add("Accept", "*/*");

                string result = await client.GetStringAsync(url).ConfigureAwait(false);
                return result;
            }
        }


        public static async Task<string> PostStringData(string url, string token, StringContent content)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                var response = await client.PostAsync(url, content);
                var str = await response.Content.ReadAsStringAsync();
                return str;
            }
        }

        public static async Task<string> PostStringData(string url, StringContent content)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                var response = await client.PostAsync(url, content);
                var str = await response.Content.ReadAsStringAsync();
                return str;
            }
        }

        /// <summary>
        /// Post data with a payload.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public static async Task<string> PostStringDataPayload(string url, string payload, List<HttpCookie> cookies = null, List<KeyValuePair<string, string>> moreHeader = null)
        {
            //var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            //var settings = (SettingsSection)config.GetSection("system.net/settings");
            //var defaultValue = settings.HttpWebRequest.UseUnsafeHeaderParsing;
            //settings.HttpWebRequest.UseUnsafeHeaderParsing = true;
            //config.Save(ConfigurationSaveMode.Modified);
            //ConfigurationManager.RefreshSection("system.net/settings");

            var response = string.Empty;

            HttpClientHandler handler = new HttpClientHandler();
            if (cookies != null)
            {
                handler.CookieContainer = CreateCookiesContainer(cookies);
            }

            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                client.DefaultRequestHeaders.Add("accept", "application/json, text/javascript, */*; q=0.01");

                moreHeader?.ForEach(x =>
                {
                    client.DefaultRequestHeaders.Add(x.Key, x.Value);
                });

                HttpContent c = new StringContent(payload, Encoding.UTF8, "application/json");

                HttpResponseMessage result;
                try
                {
                    result = await client.PostAsync(url, c).ConfigureAwait(false);
                    if (result.IsSuccessStatusCode)
                    {
                        response = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        Logger.Error($"Web error with status code {result.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on Post {url}");
                }
            }

            //settings.HttpWebRequest.UseUnsafeHeaderParsing = defaultValue;
            //config.Save(ConfigurationSaveMode.Modified);
            //ConfigurationManager.RefreshSection("system.net/settings");

            return response;
        }

        public static async Task<string> PostStringDataCookies(string url, FormUrlEncodedContent formContent, List<HttpCookie> cookies = null)
        {
            var response = string.Empty;

            HttpClientHandler handler = new HttpClientHandler();
            if (cookies != null)
            {
                handler.CookieContainer = CreateCookiesContainer(cookies);
            }

            using (var client = new HttpClient(handler))
            {
                var els = url.Split('/');
                string baseUrl = els[0] + "//" + els[2];

                client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                client.DefaultRequestHeaders.Add("origin", baseUrl);
                client.DefaultRequestHeaders.Add("referer", baseUrl);

                HttpResponseMessage result;
                try
                {
                    result = await client.PostAsync(url, formContent).ConfigureAwait(false);
                    if (result.IsSuccessStatusCode)
                    {
                        response = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        Logger.Error($"Web error with status code {result.StatusCode.ToString()}");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on Post {url}");
                }
            }

            return response;
        }


        private static async Task<Tuple<string, List<HttpCookie>>> DownloadWebView(bool getSource, string url, List<HttpCookie> cookies = null, bool getCookies = false, List<string> domains = null, bool deleteDomainsCookies = true, bool javaScriptEnabled = true)
        {
            WebViewSettings webViewSettings = new WebViewSettings
            {
                UserAgent = UserAgent,
                JavaScriptEnabled = javaScriptEnabled
            };

            using (IWebView webViewOffscreen = API.Instance.WebViews.CreateOffscreenView(webViewSettings))
            {
                try
                {
                    // 1. Set cookies
                    cookies?.ForEach(cookie => webViewOffscreen.SetCookies(url, cookie));

                    // 2. Prepare asynchronous wait
                    using (var loadingCompleted = new ManualResetEventSlim(false))
                    {
                        EventHandler<Playnite.SDK.Events.WebViewLoadingChangedEventArgs> loadingHandler = (s, e) =>
                        {
                            if (!e.IsLoading)
                            {
                                try
                                {
                                    loadingCompleted.Set();
                                }
                                catch (ObjectDisposedException) { }
                            }
                        };

                        webViewOffscreen.LoadingChanged += loadingHandler;
                        try
                        {
                            // 3. Navigate and wait for page to be fully loaded
                            webViewOffscreen.Navigate(url);
                            TimeSpan waitTimeout = TimeSpan.FromSeconds(30);
                            if (!loadingCompleted.Wait(waitTimeout))
                            {
                                Logger.Error($"Timeout {waitTimeout.TotalSeconds} seconds for {url}.");
                                return new Tuple<string, List<HttpCookie>>(string.Empty, null);
                            }
                        }
                        finally
                        {
                            try 
                            { 
                                webViewOffscreen.LoadingChanged -= loadingHandler; 
                            } 
                            catch (Exception ex) 
                            { 
                                // Ignore exceptions during cleanup (webView may already be disposed)
                                Common.LogDebug(true, $"Exception during event handler cleanup: {ex.Message}");
                            }
                        }
                    }

                    // 4. Get content
                    string data = getSource ? await webViewOffscreen.GetPageSourceAsync() : await webViewOffscreen.GetPageTextAsync();

                    // 5. Get cookies
                    List<HttpCookie> refreshedCookies = null;
                    if (getCookies)
                    {
                        refreshedCookies = webViewOffscreen.GetCookies();
                        if (domains?.Count > 0)
                        {
                            refreshedCookies = refreshedCookies
                                .Where(c => domains.Any(d => c.Domain.IsEqual(d)))
                                .ToList();
                        }
                    }

                    return new Tuple<string, List<HttpCookie>>(data, refreshedCookies);
                }
                finally
                {
                    // 6. Delete cookies for domains
                    if (deleteDomainsCookies && domains?.Count > 0)
                    {
                        foreach (var domain in domains)
                        {
                            webViewOffscreen.DeleteDomainCookies(domain);
                        }
                    }
                }
            }
        }

        public static async Task<Tuple<string, List<HttpCookie>>> DownloadJsonDataWebView(string url, List<HttpCookie> cookies = null, bool getCookies = false, List<string> domains = null, bool deleteDomainsCookies = true, bool javaScriptEnabled = true)
        {
            return await DownloadWebView(false, url, cookies, getCookies, domains, deleteDomainsCookies, javaScriptEnabled);
        }

        public static async Task<Tuple<string, List<HttpCookie>>> DownloadSourceDataWebView(string url, List<HttpCookie> cookies = null, bool getCookies = false, List<string> domains = null, bool deleteDomainsCookies = true, bool javaScriptEnabled = true)
        {
            return await DownloadWebView(true, url, cookies, getCookies, domains, deleteDomainsCookies, javaScriptEnabled);
        }


        private static CookieContainer CreateCookiesContainer(List<HttpCookie> cookies)
        {
            CookieContainer cookieContainer = new CookieContainer
            {
                PerDomainCapacity = 100,
                Capacity = 300,
                MaxCookieSize = 4096 * 10
            };

            int addedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            var cookiesByDomain = cookies.GroupBy(c => c.Domain ?? "null");
            Common.LogDebug(true, $"Cookies distribution: {string.Join(", ", cookiesByDomain.Select(g => $"{g.Key}={g.Count()}"))}");

            foreach (HttpCookie cookie in cookies)
            {
                if (string.IsNullOrEmpty(cookie.Domain))
                {
                    Logger.Warn($"Cookie '{cookie.Name}' has no domain, skipping");
                    skippedCount++;
                    continue;
                }

                try
                {
                    string fixedValue = Tools.FixCookieValue(cookie.Value);

                    Cookie c = new Cookie
                    {
                        Name = cookie.Name,
                        Value = fixedValue,
                        Domain = cookie.Domain,
                        Path = cookie.Path ?? "/",
                        Secure = cookie.Secure,
                        HttpOnly = cookie.HttpOnly
                    };

                    if (cookie.Expires.HasValue)
                    {
                        c.Expires = cookie.Expires.Value;
                    }

                    cookieContainer.Add(c);
                    addedCount++;
                }
                catch (CookieException ex)
                {
                    errorCount++;
                    Logger.Error($"CookieException for '{cookie.Name}' on domain '{cookie.Domain}': {ex.Message}");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Common.LogError(ex, true, $"Failed to add cookie: {cookie.Name} - Domain: {cookie.Domain}");
                }
            }

            Common.LogDebug(true, $"CookieContainer: {addedCount} added, {skippedCount} skipped, {errorCount} errors from {cookies.Count} total");
            Common.LogDebug(true, $"Final container count: {cookieContainer.Count}");

            return cookieContainer;
        }

        private static async Task<Tuple<string, int, string>> PostJsonWithClient(HttpClient client, string url, string payload, List<HttpHeader> headers)
        {
            try
            {
                using (var content = new StringContent(payload ?? string.Empty, Encoding.UTF8, "application/json"))
                using (var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content })
                {
                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }

                    using (var response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        var body = response != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
                        var retry = response?.Headers?.RetryAfter?.ToString();
                        return Tuple.Create(body, response != null ? (int)response.StatusCode : 0, retry);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on PostJson {url}");
                return Tuple.Create(string.Empty, 0, (string)null);
            }
        }

        /// <summary>
        /// Post a JSON payload using the shared HttpClient when available and return body, status code and Retry-After header.
        /// </summary>
        public static async Task<Tuple<string, int, string>> PostJsonWithSharedClientWithStatus(string url, string payload, List<HttpHeader> headers = null)
        {
            if (SharedClient == null)
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    return await PostJsonWithClient(client, url, payload, headers).ConfigureAwait(false);
                }
            }

            return await PostJsonWithClient(SharedClient, url, payload, headers).ConfigureAwait(false);
        }
    }
}