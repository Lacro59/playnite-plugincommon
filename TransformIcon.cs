using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginCommon
{
    public class TransformIcon
    {
        private static string steam = "";          // e906
        private static string gog = "";            // e903
        private static string battleNET = "";      // e900
        private static string origin = "";         // e904
        private static string xbox = "";           // e908
        private static string uplay = "";          // e907
        private static string epic = "";           // e902
        private static string playnite = "";       // e905
        private static string bethesda = "";       // e901
        private static string humble = "";         // e909
        private static string twitch = "";         // e90a
        private static string itchio = "";         // e90b

        private static string statistics = "";     // e90c
        private static string howlongtobeat = "";  // e90d


        /// <summary>
        /// Get icon from name for "font.ttf".
        /// </summary>
        public static string Get(string Name)
        {
            string stringReturn;
            switch (Name.ToLower())
            {
                case "howlongtobeat":
                    stringReturn = howlongtobeat;
                    break;
                case "statistics":
                    stringReturn = statistics;
                    break;

                case "steam":
                    stringReturn = steam;
                    break;
                case "gog":
                    stringReturn = gog;
                    break;
                case "battle.net":
                    stringReturn = battleNET;
                    break;
                case "origin":
                    stringReturn = origin;
                    break;
                case "xbox":
                    stringReturn = xbox;
                    break;
                case "uplay":
                    stringReturn = uplay;
                    break;
                case "epic":
                    stringReturn = epic;
                    break;
                case "playnite":
                    stringReturn = playnite;
                    break;
                case "bethesda":
                    stringReturn = bethesda;
                    break;
                case "humble":
                    stringReturn = humble;
                    break;
                case "twitch":
                    stringReturn = twitch;
                    break;
                case "itch.io":
                    stringReturn = itchio;
                    break;
                default:
                    stringReturn = Name;
                    break;
            }
            return stringReturn;
        }
    }
}
