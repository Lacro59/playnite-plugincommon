using Newtonsoft.Json.Linq;
using Playnite.SDK;
using CommonShared;
using CommonPlaynite;
using CommonPlaynite.API;
using CommonPlaynite.Common;
using CommonPlaynite.Converters;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using YamlDotNet.Serialization;

namespace CommonShared
{
    public class CheckVersion
    {
        private static ILogger logger = LogManager.GetLogger();

        private string PluginName = string.Empty;
        private ExtensionManifest PluginInfo;

        private readonly string urlGithub = @"https://api.github.com/repos/Lacro59/playnite-{0}-plugin/releases";

        private string LastReleaseUrl = string.Empty;
        private string LastReleaseTagName = string.Empty;
        private string LastReleaseBody = string.Empty;


        public bool Check(string PluginName, string PluginFolder)
        {
            this.PluginName = PluginName;
            var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

            var path = Path.Combine(PluginFolder, PlaynitePaths.ExtensionManifestFileName);
            PluginInfo = ExtensionManifest.FromFile(path);

            // Get Github info
            string url = string.Format(urlGithub, PluginName.ToLower());

#if DEBUG
            logger.Debug($"CommonShared [Ignored] - Download {url} for {PluginName}");
#endif

            string ResultWeb = string.Empty;
            try
            {
                ResultWeb = Web.DownloadStringData(url, WebUserAgentType.Request).GetAwaiter().GetResult();
            }
            catch (WebException ex)
            {
                logger.Error($"Failed to load from {url} for {PluginName}");
#if DEBUG
                Common.LogError(ex, "CommonShared [Ignored]", $"Failed to load from {url} for {PluginName}");
#endif
            }

            LastReleaseUrl = string.Empty;
            LastReleaseTagName = string.Empty;
            LastReleaseBody = string.Empty;

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

                    logger.Info($"CommonShared - {PluginName} - Find {LastReleaseTagName} - Actual v{PluginInfo.Version}");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "CommonShared", $"Failed to parse Github response for {PluginName} - {ResultWeb}");
                    return false;
                }
            }
            else
            {
                logger.Warn($"CommonShared - No Data from {url} for {PluginName}");
            }

            //Check actual vs Github
            return (!LastReleaseTagName.IsNullOrEmpty() && LastReleaseTagName != "v" + PluginInfo.Version);
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

        public void GetRelease()
        {
        }
    }
}
