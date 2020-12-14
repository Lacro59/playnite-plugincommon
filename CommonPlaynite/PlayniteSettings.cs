using System.IO;

namespace PluginCommon.PlayniteResources
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
