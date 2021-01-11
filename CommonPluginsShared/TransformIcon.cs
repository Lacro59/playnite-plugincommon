namespace CommonPluginsShared
{
    public class TransformIcon
    {
        #region font
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
        #endregion

        #region retrogaming
        private static string psp = "";                    // ea46
        private static string dreamcast = "";              // ea3c
        private static string dos = "";                    // ea3b
        private static string commodore64 = "";            // ea3a
        private static string ds = "";                     // e904
        private static string gameboy = "";                // ea3d
        private static string gameboyadvance = "";         // ea3f
        private static string gamecube = "";               // ea40
        private static string megadrive = "";              // ea41
        private static string nintendo = "";               // ea42
        private static string nintendo64 = "";             // ea43
        private static string playstation = "";            // ea44
        private static string playstation4 = "";           // ea45
        private static string supernintendo = "";          // ea47
        private static string nintendoswitch = "";         // ea48
        private static string wii = "";                    // ea49
        private static string mame = "";                   // ea4a
        #endregion


        /// <summary>
        /// Get icon from name for "font.ttf".
        /// </summary>
        public static string Get(string Name, bool ReturnEmpy = false)
        {
            string stringReturn = string.Empty;
            switch (Name.ToLower())
            {
                #region font
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
                #endregion

                #region retrogaming
                case "sony psp":
                    stringReturn = psp;
                    break;
                case "sega dreamcast":
                    stringReturn = dreamcast;
                    break;
                case "dos":
                    stringReturn = dos;
                    break;
                case "commodore 64":
                    stringReturn = commodore64;
                    break;
                case "nintendo 3ds":
                case "nintendo ds":
                    stringReturn = ds;
                    break;
                case "nintendo game boy":
                case "nintendo game boy color":
                    stringReturn = gameboy;
                    break;
                case "nintendo game boy advance":
                    stringReturn = gameboyadvance;
                    break;
                case "nintendo gamecube":
                    stringReturn = gamecube;
                    break;
                case "sega genesis":
                    stringReturn = megadrive;
                    break;
                case "nintendo entertainment system":
                    stringReturn = nintendo;
                    break;
                case "nintendo 64":
                    stringReturn = nintendo64;
                    break;
                case "sony playStation":
                    stringReturn = playstation;
                    break;
                case "super nintendo entertainment system":
                    stringReturn = supernintendo;
                    break;
                case "nintendo switch":
                    stringReturn = nintendoswitch;
                    break;
                case "nintendo wii":
                    stringReturn = wii;
                    break;
                case "mame 2003 plus":
                    stringReturn = mame;
                    break;
                #endregion

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
