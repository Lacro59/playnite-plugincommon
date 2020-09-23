using Playnite.SDK;
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


        public static async Task<string> DownloadStringData(string url)
        {
            using (var client = new HttpClient())
            {
                return await client.GetStringAsync(url).ConfigureAwait(false);
            }
        }

        public static async Task<string> DownloadStringData(string url, WebUserAgentType UserAgentType)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.TryParseAdd(StrWebUserAgentType(UserAgentType));
                return await client.GetStringAsync(url).ConfigureAwait(false);
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
