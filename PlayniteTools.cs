using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon.PlayniteResources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginCommon
{
    public class PlayniteTools
    {
        private static readonly ILogger logger = LogManager.GetLogger();


        #region Emulators
        public static List<Emulator> GetListEmulators(IPlayniteAPI PlayniteApi)
        {
            List<Emulator> ListEmulators = new List<Emulator>();
            foreach (Emulator item in PlayniteApi.Database.Emulators)
            {
                ListEmulators.Add(item);
            }
            return ListEmulators;
        }

        public static bool IsGameEmulated(IPlayniteAPI PlayniteApi, Game game)
        {
            List<Emulator> ListEmulators = GetListEmulators(PlayniteApi);
            return game.PlayAction != null && game.PlayAction.EmulatorId != null && ListEmulators.FindAll(x => x.Id == game.PlayAction.EmulatorId).Count > 0;
        }
        #endregion


        public static string GetCacheFile(string ImageFileName)
        {
            try
            {
                string PathImageFileName = Path.Combine(PlaynitePaths.ImagesCachePath, ImageFileName);

                if (File.Exists(PathImageFileName + ".png"))
                {
                    return PathImageFileName + ".png";
                }
#if DEBUG
                logger.Debug($"PluginCommon - GetCacheFile() not find - {PathImageFileName}");
#endif
            }
            catch(Exception ex)
            {
                Common.LogError(ex, "PluginCommon", "Error on GetCacheFile()");
            }

            return string.Empty;
        }


        public static bool IsDisabledPlaynitePlugins(string PluginName, string ConfigurationPath)
        {
            JArray DisabledPlugins = new JArray();
            JObject PlayniteConfig = new JObject();
            try
            {
                string FileConfig = ConfigurationPath + "\\config.json";
                if (File.Exists(FileConfig))
                {
                    PlayniteConfig = JObject.Parse(File.ReadAllText(FileConfig));
                    DisabledPlugins = (JArray)PlayniteConfig["DisabledPlugins"];

                    if (DisabledPlugins != null)
                    {
                        foreach (string name in DisabledPlugins)
                        {
                            if (name.ToLower() == PluginName.ToLower())
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    logger.Warn($"PluginCommon - File not found {FileConfig}");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", "Error on IsDisabledPlaynitePlugins()");
                return false;
            }

            return false;
        }
    }
}
