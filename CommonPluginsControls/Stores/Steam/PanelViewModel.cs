using CommonPluginsControls.Stores;
using CommonPluginsStores.Models;
using CommonPluginsStores.Models.Enumerations;
using CommonPluginsStores.Models.Interfaces;
using Playnite.SDK;
using System;
using System.Collections.Generic;

namespace CommonPluginsControls.Stores.Steam
{
    public class PanelViewModel : ObservableObject, IStorePanelViewModel
    {
        private IStoreApi _storeApi;
        internal IStoreApi StoreApi
        {
            get => _storeApi;
            set
            {
                _storeApi = value;
                OnPropertyChanged(nameof(StoreApi));
                NotifyAuthStatusChanged();
            }
        }

        public AccountInfos User => StoreApi?.CurrentAccountInfos;

        private bool useApi = true;
        public bool UseApi
        {
            get => useApi;
            set
            {
                SetValue(ref useApi, value);
                OnPropertyChanged(nameof(ShowApiKeyField));
            }
        }

        private bool useAuth = true;
        public bool UseAuth
        {
            get => useAuth;
            set
            {
                SetValue(ref useAuth, value);
                NotifyAuthUiProperties();
            }
        }

        private bool forceAuth = false;
        public bool ForceAuth
        {
            get => forceAuth;
            set
            {
                SetValue(ref forceAuth, value);
                NotifyAuthUiProperties();
            }
        }

        public bool ShowConnectionSection => ForceAuth || UseAuth;

        public bool IsManualAccountEntryEnabled => !ForceAuth && !UseAuth;

        /// <summary>
        /// API key field is only shown when authentication is optional and API mode is enabled.
        /// </summary>
        public bool ShowApiKeyField => !ForceAuth && UseApi;

        public AuthStatus AuthStatus => StoreApi == null ? AuthStatus.Failed : StoreApi.IsUserLoggedIn ? AuthStatus.Ok : AuthStatus.AuthRequired;

        public bool CanLogin => AuthStatus != AuthStatus.Ok && AuthStatus != AuthStatus.Checking;

        public bool CanLogout => AuthStatus == AuthStatus.Ok;

        public RelayCommand<object> LoginCommand => new RelayCommand<object>((a) =>
        {
            try
            {
                StoreSettingsLog.LoginRequested(StoreApi);
                StoreApi.Login();
                NotifyAuthStatusChanged();
                StoreSettingsLog.LoginCompleted(StoreApi, AuthStatus);
            }
            catch (Exception ex)
            {
                StoreSettingsLog.Error(ex, "Steam login failed");
                throw;
            }
        });

        public RelayCommand<object> ClearSessionCommand => new RelayCommand<object>((a) =>
        {
            try
            {
                StoreSettingsLog.LogoutRequested(StoreApi);
                StoreApi?.ClearSession();
                NotifyAuthStatusChanged();
                StoreSettingsLog.LogoutCompleted(StoreApi, AuthStatus);
            }
            catch (Exception ex)
            {
                StoreSettingsLog.Error(ex, "Steam logout failed");
                throw;
            }
        });

        public void ResetIsUserLoggedIn()
        {
            StoreApi?.ResetIsUserLoggedIn();
            NotifyAuthStatusChanged();
        }

        public void RefreshAuthCommandStates()
        {
            NotifyAuthStatusChanged();
        }

        private void NotifyAuthUiProperties()
        {
            OnPropertyChanged(nameof(ShowConnectionSection));
            OnPropertyChanged(nameof(IsManualAccountEntryEnabled));
            OnPropertyChanged(nameof(ShowApiKeyField));
        }

        private void NotifyAuthStatusChanged()
        {
            OnPropertyChanged(nameof(AuthStatus));
            OnPropertyChanged(nameof(CanLogin));
            OnPropertyChanged(nameof(CanLogout));
            OnPropertyChanged(nameof(User));
        }
    }
}
