using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsShared.Interfaces
{
    public interface IPluginSettings
    {
        /// <summary>
        /// Display the plugin menu in the Extensions menu.
        /// </summary>
        bool MenuInExtensions { get; set; }

        /// <summary>
        /// Enable tag features.
        /// </summary>
        bool EnableTag { get; set; }

        #region Automatic update when updating library
        /// <summary>
        /// Date of the last automatic library update assets download.
        /// </summary>
        DateTime LastAutoLibUpdateAssetsDownload { get; set; }

        /// <summary>
        /// Enable automatic import of data.
        /// </summary>
        bool AutoImport { get; set; }
        #endregion
    }
}
