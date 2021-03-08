﻿namespace CommonPluginsShared
{
    public class TransformIcon
    {
        #region font
        private static string steam = "\ue906";                  // e906
        private static string gogGalaxy = "\ue903";              // e903
        private static string gog = "\uea35";                    // ea35
        private static string battleNET = "\ue900";              // e900
        private static string origin = "\ue904";                 // e904
        private static string xbox = "\ue908";                   // e908
        private static string uplay = "\ue907";                  // e907
        private static string epic = "\ue902";                   // e902
        private static string playnite = "\ue905";               // e905
        private static string bethesda = "\ue901";               // e901
        private static string humble = "\ue909";                 // e909
        private static string twitch = "\ue90a";                 // e90a
        private static string itchio = "\ue90b";                 // e90b
        private static string indiegala = "\ue911";              // e911

        private static string retroachievements = "\ue910";      // e910
        private static string rpcs3 = "\uea37";                  // ea37

        private static string statistics = "\ue90c";             // e90c
        private static string howlongtobeat = "\ue90d";          // e90d
        private static string successstory = "\uea33";           // ea33
        private static string gameactivity = "\ue90f";           // e90f
        private static string checklocalizations = "\uea2c";     // ea2c
        private static string screenshotsvisualizer = "\uea38";  // ea38

        private static string gameHacked = "\uea36";             // ea36
        #endregion

        #region retrogaming
        private static string psp = "\uea46";                    // ea46
        private static string dreamcast = "\uea3c";              // ea3c
        private static string dos = "\uea3b";                    // ea3b
        private static string commodore64 = "\uea3a";            // ea3a
        private static string ds = "\ue904";                     // e904
        private static string gameboy = "\uea3d";                // ea3d
        private static string gameboyadvance = "\uea3f";         // ea3f
        private static string gamecube = "\uea40";               // ea40
        private static string megadrive = "\uea41";              // ea41
        private static string nintendo = "\uea42";               // ea42
        private static string nintendo64 = "\uea43";             // ea43
        private static string playstation = "\uea44";            // ea44
        private static string playstation4 = "\uea45";           // ea45
        private static string supernintendo = "\uea47";          // ea47
        private static string nintendoswitch = "\uea48";         // ea48
        private static string wii = "\uea49";                    // ea49
        private static string mame = "\uea4a";                   // ea4a
        #endregion


        /// <summary>
        /// Get icon from name for "font.ttf".
        /// </summary>
        public static string Get(string Name, bool ReturnEmpy = false)
        {
            string stringReturn = string.Empty;
            switch (Name.ToLower())
            {
                #region plugin
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
                #endregion

                #region sources
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
                case "ubisoft connect":
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
                case "sony playstation":
                case "sony playstation 2":
                case "sony playstation 3":
                    stringReturn = playstation;
                    break;
                case "sony playstation 4":
                    stringReturn = playstation4;
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
