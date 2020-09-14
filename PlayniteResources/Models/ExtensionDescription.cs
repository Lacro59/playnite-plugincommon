using Playnite.SDK.Models;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace PluginCommon.PlayniteResources.Models
{
    public enum ExtensionType
    {
        GenericPlugin,
        GameLibrary,
        Script,
        MetadataProvider
    }

    public class BaseExtensionDescription
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public List<Link> Links { get; set; }
    }

    public class ExtensionDescription : BaseExtensionDescription
    {
        [YamlIgnore]
        public string DescriptionPath { get; set; }
        [YamlIgnore]
        public string FolderName { get; set; }
        public string Module { get; set; }
        public string Icon { get; set; }
        public ExtensionType Type { get; set; }
    }
}
