using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace PluginCommon
{
    class Tools
    {
        // https://stackoverflow.com/a/48735199
        public static JArray RemoveValue(JArray oldArray, dynamic obj)
        {
            List<string> newArray = oldArray.ToObject<List<string>>();
            newArray.Remove(obj);
            return JArray.FromObject(newArray);
        }
    }
}
