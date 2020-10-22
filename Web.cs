using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PluginCommon
{
    public enum WebUserAgentType
    {
        Request
    }

    public class Web
    {
        private static ILogger logger = LogManager.GetLogger();


        private static string StrWebUserAgentType(WebUserAgentType UserAgentType)
        {
            switch (UserAgentType)
            {
                case (WebUserAgentType.Request):
                    return "request";
            }
            return string.Empty;
        }


        public static async Task<bool> DownloadFileImage(string ImageFileName, string url, string ImagesCachePath, string PluginName)
        {
            string PathImageFileName = Path.Combine(ImagesCachePath, PluginName.ToLower(), ImageFileName);

            using (var client = new HttpClient())
            {
                var response = client.GetAsync(url).Result;
                var filetype = response.Content.Headers.ContentType.MediaType;

                var imageStream = response.Content.ReadAsStreamAsync().Result;
                ImageTools.Resize(imageStream, 64, 64, PathImageFileName);

                client.Dispose();
            }
            
            // Delete id file is empty
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
                Common.LogError(ex, "PluginCommon", $"Error on Delete file image");
            }

            return true;
        }



        public static async Task<string> DownloadStringData(string url)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get
                };

                HttpResponseMessage response = client.SendAsync(request).Result;
                int statusCode = (int)response.StatusCode;

                // We want to handle redirects ourselves so that we can determine the final redirect Location (via header)
                if (statusCode >= 300 && statusCode <= 399)
                {
                    var redirectUri = response.Headers.Location;
                    if (!redirectUri.IsAbsoluteUri)
                    {
                        redirectUri = new Uri(request.RequestUri.GetLeftPart(UriPartial.Authority) + redirectUri);
                    }
                    logger.Info(string.Format("DownloadStringData - Redirecting to {0}", redirectUri));

                    return await DownloadStringData(redirectUri.ToString());
                }
                else
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        public static async Task<string> DownloadStringData(string url, WebUserAgentType UserAgentType)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.TryParseAdd(StrWebUserAgentType(UserAgentType));

                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get
                };

                HttpResponseMessage response = client.SendAsync(request).Result;
                int statusCode = (int)response.StatusCode;

                if (statusCode == 403)
                {
                    return string.Empty;
                }
                else
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }


        public static async Task<string> DownloadStringDataWithGz(string url)
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            using (HttpClient client = new HttpClient(handler))
            {
                return await client.GetStringAsync(url).ConfigureAwait(false);
            }
        }
    }
}
