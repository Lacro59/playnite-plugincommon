using Newtonsoft.Json.Linq;
using Playnite.API;
using Playnite.SDK;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PluginCommon
{
    public class CheckVersion
    {
        private static ILogger logger = LogManager.GetLogger();

        private string PluginName = "";
        private ExtensionDescription PluginInfo;

        private string LastReleaseUrl = "";
        private string LastReleaseTagName = "";
        private string LastReleaseBody = "";

        internal async Task<string> DownloadStringData(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
                    string responseData = await client.GetStringAsync(url).ConfigureAwait(false);
                    return responseData;
                }
            }
            catch
            {
                return null;
            }
        }

        public bool Check(string PluginName, string PluginFolder)
        {
            this.PluginName = PluginName;
            PluginInfo = ExtensionDescription.FromFile(PluginFolder + "\\extension.yaml");

            // Get Github info
            string url = string.Format(@"https://api.github.com/repos/Lacro59/playnite-{0}-plugin/releases", PluginName.ToLower());

            string ResultWeb = "";
            try
            {
                ResultWeb = DownloadStringData(url).GetAwaiter().GetResult();
            }
            catch (WebException ex)
            {
                Common.LogError(ex, "PluginCommon", $"Failed to load from {url}");
            }

            LastReleaseUrl = "";
            LastReleaseTagName = "";
            LastReleaseBody = "";
            if (!ResultWeb.IsNullOrEmpty())
            {
                try
                {
                    JArray resultObj = JArray.Parse(ResultWeb);
                    if (resultObj[0]["html_url"] != null)
                    {
                        LastReleaseUrl = (string)resultObj[0]["html_url"];
                        LastReleaseTagName = (string)resultObj[0]["tag_name"];
                        LastReleaseBody = (string)resultObj[0]["body"];
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "PluginCommon", $"Failed to parse Github response {ResultWeb}");
                    return false;
                }
            }

            // Check actual vs Github
            if (!LastReleaseTagName.IsNullOrEmpty() && LastReleaseTagName.IndexOf(PluginInfo.Version) == -1)
            {
                return true;
            }

            return false;
        }

        public void ShowNotification(IPlayniteAPI PlayniteApi, string Message)
        {
            PlayniteApi.Notifications.Add(new NotificationMessage(
                $"CheckVersion-{PluginName}",
                Message + " (v" + PluginInfo.Version + " -> " + LastReleaseTagName + ")",
                NotificationType.Info,
                () => Process.Start(LastReleaseUrl)
            ));
        }
    }
}
