using CommonPluginsControls.Stores;
using CommonPluginsStores.Models;
using CommonPluginsStores.Models.Enumerations;
using CommonPluginsStores.Models.Interfaces;
using Playnite.SDK;
using System;
using System.Collections.Generic;

namespace CommonPluginsControls.Stores.Epic
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
                StoreSettingsLog.Error(ex, "Epic login failed");
                throw;
            }
        });

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
                StoreSettingsLog.Error(ex, "Epic logout failed");
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
