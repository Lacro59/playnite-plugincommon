using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginCommon.PlayniteResources.Common
{
    public class Serialization
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public static T FromJson<T>(string json) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                logger.Error(e, $"Failed to deserialize {typeof(T).FullName} from json:");
                logger.Debug(json);
                throw;
            }
        }

        public static bool TryFromJson<T>(string json, out T deserialized) where T : class
        {
            try
            {
                deserialized = JsonConvert.DeserializeObject<T>(json);
                return true;
            }
            catch
            {
                deserialized = null;
                return false;
            }
        }
    }
}
