using CommonPluginsStores.Models.Enumerations;

namespace CommonPluginsControls.Stores.Steam
{
    public class PanelViewModel : StorePanelViewModelBase
    {
        private bool _useApi = true;

        public bool UseApi
        {
            get => _useApi;
            set
            {
                SetValue(ref _useApi, value);
                OnPropertyChanged(nameof(ShowApiKeyField));
                OnPropertyChanged(nameof(IsApiKeyEntryEnabled));
            }
        }

        /// <summary>
        /// API key field is only shown when authentication is optional and API mode is enabled.
        /// </summary>
        public bool ShowApiKeyField => !ForceAuth && UseApi;

        /// <summary>
        /// API key can be edited when optional API mode is active (private profiles with API key).
        /// </summary>
        public bool IsApiKeyEntryEnabled => !ForceAuth && UseApi;

        protected override string LoginErrorContext => "Steam login failed";

        protected override string LogoutErrorContext => "Steam logout failed";

        protected override void NotifyAuthUiProperties()
        {
            base.NotifyAuthUiProperties();
            OnPropertyChanged(nameof(ShowApiKeyField));
            OnPropertyChanged(nameof(IsApiKeyEntryEnabled));
        }
    }
}
