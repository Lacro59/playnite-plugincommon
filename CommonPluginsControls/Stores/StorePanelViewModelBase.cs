using CommonPluginsStores.Models;
using CommonPluginsStores.Models.Enumerations;
using CommonPluginsStores.Models.Interfaces;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CommonPluginsControls.Stores
{
    /// <summary>
    /// Shared view-model logic for store settings panels (auth mode, manual account entry).
    /// </summary>
    public abstract class StorePanelViewModelBase : ObservableObject, IStorePanelViewModel
    {
        private IStoreApi _storeApi;
        private INotifyPropertyChanged _accountInfosNotify;
        private bool _useAuth = true;
        private bool _forceAuth;

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

        public bool UseAuth
        {
            get => _useAuth;
            set
            {
                SetValue(ref _useAuth, value);
                NotifyAuthUiProperties();
            }
        }

        public bool ForceAuth
        {
            get => _forceAuth;
            set
            {
                SetValue(ref _forceAuth, value);
                NotifyAuthUiProperties();
            }
        }

        public bool ShowConnectionSection => ForceAuth || UseAuth;

        /// <summary>
        /// Manual account fields are editable only without forced/optional auth and on a public profile.
        /// </summary>
        public bool IsManualAccountEntryEnabled =>
            !ForceAuth && !UseAuth && User != null && User.AccountStatus == AccountStatus.Public;

        /// <summary>
        /// Help link to find account ID is shown only when manual entry is allowed.
        /// </summary>
        public bool ShowGetAccountIdLink => IsManualAccountEntryEnabled;

        public AuthStatus AuthStatus =>
            StoreApi == null ? AuthStatus.Failed : StoreApi.IsUserLoggedIn ? AuthStatus.Ok : AuthStatus.AuthRequired;

        public bool CanLogin => AuthStatus != AuthStatus.Ok && AuthStatus != AuthStatus.Checking;

        public bool CanLogout => AuthStatus == AuthStatus.Ok;

        public RelayCommand<object> LoginCommand => new RelayCommand<object>((a) => ExecuteLogin(useAlternative: false));

        public RelayCommand<object> ClearSessionCommand => new RelayCommand<object>((a) => ExecuteLogout());

        public void ResetIsUserLoggedIn()
        {
            StoreApi?.ResetIsUserLoggedIn();
            NotifyAuthStatusChanged();
        }

        public void RefreshAuthCommandStates()
        {
            NotifyAuthStatusChanged();
        }

        protected abstract string LoginErrorContext { get; }

        protected abstract string LogoutErrorContext { get; }

        protected virtual void ExecuteLogin(bool useAlternative)
        {
            try
            {
                if (useAlternative)
                {
                    StoreSettingsLog.LoginAlternativeRequested(StoreApi);
                    StoreApi.LoginAlternative();
                }
                else
                {
                    StoreSettingsLog.LoginRequested(StoreApi);
                    StoreApi.Login();
                }

                NotifyAuthStatusChanged();
                StoreSettingsLog.LoginCompleted(StoreApi, AuthStatus);
            }
            catch (Exception ex)
            {
                StoreSettingsLog.Error(ex, LoginErrorContext);
                throw;
            }
        }

        protected void ExecuteLogout()
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
                StoreSettingsLog.Error(ex, LogoutErrorContext);
                throw;
            }
        }

        protected virtual void NotifyAuthUiProperties()
        {
            OnPropertyChanged(nameof(ShowConnectionSection));
            NotifyAccountEntryUi();
        }

        protected void NotifyAuthStatusChanged()
        {
            AttachAccountInfosNotifier();
            OnPropertyChanged(nameof(AuthStatus));
            OnPropertyChanged(nameof(CanLogin));
            OnPropertyChanged(nameof(CanLogout));
            OnPropertyChanged(nameof(User));
            NotifyAccountEntryUi();
        }

        private void AttachAccountInfosNotifier()
        {
            if (_accountInfosNotify != null)
            {
                _accountInfosNotify.PropertyChanged -= AccountInfos_PropertyChanged;
                _accountInfosNotify = null;
            }

            _accountInfosNotify = User as INotifyPropertyChanged;
            if (_accountInfosNotify != null)
            {
                _accountInfosNotify.PropertyChanged += AccountInfos_PropertyChanged;
            }
        }

        private void AccountInfos_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(AccountInfos.AccountStatus))
            {
                NotifyAccountEntryUi();
            }
        }

        private void NotifyAccountEntryUi()
        {
            OnPropertyChanged(nameof(IsManualAccountEntryEnabled));
            OnPropertyChanged(nameof(ShowGetAccountIdLink));
        }
    }
}
