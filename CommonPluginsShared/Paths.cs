using System;
using System.Collections.Generic;
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
                    pathReturn += "\\" + CommonPlayniteShared.Common.Paths.GetSafePathName(folder);
                }
            }

            return pathReturn;
        }
    }
}
