namespace PluginCommon
{
    public class TransformIcon
    {
        private static string steam = "";                  // e906
        private static string gogGalaxy = "";              // e903
        private static string gog = "";                    // ea35
        private static string battleNET = "";              // e900
        private static string origin = "";                 // e904
        private static string xbox = "";                   // e908
        private static string uplay = "";                  // e907
        private static string epic = "";                   // e902
        private static string playnite = "";               // e905
        private static string bethesda = "";               // e901
        private static string humble = "";                 // e909
        private static string twitch = "";                 // e90a
        private static string itchio = "";                 // e90b
        private static string indiegala = "";              // e911
        private static string retroachievements = "";      // e910
        private static string rpcs3 = "";                  // ea37

        private static string statistics = "";             // e90c
        private static string howlongtobeat = "";          // e90d
        private static string successstory = "";           // ea33
        private static string gameactivity = "";           // e90f
        private static string checklocalizations = "";     // ea2c
        private static string screenshotsvisualizer = "";  // ea38

        private static string gameHacked = "";             // ea36


        /// <summary>
        /// Get icon from name for "font.ttf".
        /// </summary>
        public static string Get(string Name, bool ReturnEmpy = false)
        {
            string stringReturn = string.Empty;
            switch (Name.ToLower())
            {
                case "howlongtobeat":
                    stringReturn = howlongtobeat;
                    break;
                case "statistics":
                    stringReturn = statistics;
                    break;
                case "successstory":
                    stringReturn = successstory;
                    break;
                case "gameactivity":
                    stringReturn = gameactivity;
                    break;
                case "checklocalizations":
                    stringReturn = checklocalizations;
                    break;
                case "screenshotsvisualizer":
                    stringReturn = screenshotsvisualizer;
                    break;

                case "hacked":
                    stringReturn = gameHacked;
                    break;

                case "retroachievements":
                    stringReturn = retroachievements;
                    break;
                case "indiegala":
                    stringReturn = indiegala;
                    break;
                case "steam":
                    stringReturn = steam;
                    break;
                case "gog":
                    stringReturn = gog;
                    break;
                case "goggalaxy":
                    stringReturn = gogGalaxy;
                    break;
                case "battle.net":
                    stringReturn = battleNET;
                    break;
                case "origin":
                    stringReturn = origin;
                    break;
                case "microsoft store":
                    stringReturn = xbox;
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
                case "rpcs3":
                    stringReturn = rpcs3;
                    break;

                default:
                    if (!ReturnEmpy)
                    {
                        stringReturn = Name;
                    }
                    break;
            }
            return stringReturn;
        }
    }
}
