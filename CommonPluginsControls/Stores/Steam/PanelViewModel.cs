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
        /// API key field: optional under forced login (<see cref="ForceAuth"/>), or when API mode is enabled without ForceAuth.
        /// </summary>
        public bool ShowApiKeyField => ForceAuth || UseApi;

        /// <summary>
        /// API key can be edited whenever the field is shown (forced-auth plugins keep login mandatory and key optional).
        /// </summary>
        public bool IsApiKeyEntryEnabled => ForceAuth || UseApi;

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
