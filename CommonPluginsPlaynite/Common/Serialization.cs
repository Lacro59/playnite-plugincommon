using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsPlaynite.Common
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

        public static T FromJsonStream<T>(Stream stream) where T : class
        {
            using (var sr = new StreamReader(stream))
            using (var reader = new JsonTextReader(sr))
            {
                return new JsonSerializer().Deserialize<T>(reader);
            }
        }

        public static T FromJsonFile<T>(string filePath) where T : class
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return FromJsonStream<T>(fs);
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
