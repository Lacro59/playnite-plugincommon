using CommonPluginsControls.Stores;
using Playnite.SDK;
using System;

namespace CommonPluginsControls.Stores.Epic
{
    public class PanelViewModel : StorePanelViewModelBase
    {
        protected override string LoginErrorContext => "Epic login failed";

        protected override string LogoutErrorContext => "Epic logout failed";

        public RelayCommand<object> LoginAlternativeCommand => new RelayCommand<object>((a) =>
        {
            try
            {
                StoreSettingsLog.LoginAlternativeRequested(StoreApi);
                StoreApi.LoginAlternative();
                NotifyAuthStatusChanged();
                StoreSettingsLog.LoginCompleted(StoreApi, AuthStatus);
            }
            catch (Exception ex)
            {
                StoreSettingsLog.Error(ex, "Epic alternative login failed");
                throw;
            }
        });
    }
}
