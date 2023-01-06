using CommonPluginsShared.Collections;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsShared.Controls
{
    public class PluginUserControlExtend : PluginUserControlExtendBase
    {
        internal virtual IPluginDatabase _PluginDatabase { get; set; }


        #region OnPropertyChange
        // When game selection is changed
        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            if (!_PluginDatabase?.IsLoaded ?? true)
            {
                return;
            }

            if (newContext == null || oldContext?.Id == newContext?.Id)
            {
                return;
            }

            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop && ActiveViewAtCreation != API.Instance.MainView.ActiveDesktopView)
            {
                return;
            }

            SetDefaultDataContext();

            MustDisplay = _ControlDataContext.IsActivated;

            // When control is not used
            if (!_ControlDataContext.IsActivated)
            {
                return;
            }

            try
            {
                PluginDataBaseGameBase PluginGameData = _PluginDatabase.Get(newContext, true);
                if (PluginGameData?.HasData ?? false)
                {
                    SetData(newContext, PluginGameData);
                }
                else if (AlwaysShow)
                {
                    SetData(newContext, PluginGameData);
                }
                // When there is no plugin data
                else
                {
                    MustDisplay = false;
                }

            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }
        #endregion

        public virtual void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
        }
    }
}
