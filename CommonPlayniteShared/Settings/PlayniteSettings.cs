using System.IO;

namespace CommonPlayniteShared
{
    public enum ImageLoadScaling
    {
        //[Description(LOC.SettingsImageScalingQuality)]
        None,
        //[Description(LOC.SettingsImageScalingBalanced)]
        BitmapDotNet,
        //[Description(LOC.SettingsImageScalingAlternative)]
        Custom
    }

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
