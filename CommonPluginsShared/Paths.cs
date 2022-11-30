using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CommonPluginsShared
{
    public class Paths
    {
        public static string GetSafePath(string path)
        {
            string pathReturn = string.Empty;
            string[] PathFolders = path.Split('\\');
            foreach (string folder in PathFolders)
            {
                if (pathReturn.IsNullOrEmpty())
                {
                    pathReturn += folder;
                }
                else
                {
                    pathReturn += "\\" + Paths.GetSafePathName(folder, true);
                }
            }

            return pathReturn;
        }

        public static string GetSafePathName(string filename, bool keepNameSpace = false)
        {
            if (keepNameSpace)
            {
                return string.Join(" ", filename.Split(Path.GetInvalidFileNameChars())).Trim();
            }
            else
            {
                return CommonPlayniteShared.Common.Paths.GetSafePathName(filename);
            }
        }
    }
}
