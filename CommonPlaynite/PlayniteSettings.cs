using System.IO;

namespace CommonPlaynite
{
    public class PlayniteSettings
    {
        public static bool IsPortable
        {
            get
            {
                return !File.Exists(PlaynitePaths.UninstallerPath);
            }
        }
    }
}
