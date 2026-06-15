using CommonPluginsShared;
using CommonPluginsStores.Models;
using CommonPluginsStores.Models.Enumerations;
using CommonPluginsStores.Models.Interfaces;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

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
        private int _authRefreshGeneration;

        internal IStoreApi StoreApi
        {
            get => _storeApi;
            set
            {
                if (ReferenceEquals(_storeApi, value))
                {
                    return;
                }

                _storeApi = value;
                OnPropertyChanged(nameof(StoreApi));
                NotifyAuthStatusChanged();
                ScheduleBackgroundAuthRefresh();
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
            ScheduleBackgroundAuthRefresh();
        }

        public void RefreshAuthCommandStates()
        {
            NotifyAuthStatusChanged();
        }

        /// <inheritdoc />
        public void RequestBackgroundAuthRefresh()
        {
            ScheduleBackgroundAuthRefresh();
        }

        /// <summary>
        /// Runs a full login check off the UI thread and refreshes auth bindings when it completes.
        /// </summary>
        private void ScheduleBackgroundAuthRefresh()
        {
            IStoreApi storeApi = StoreApi;
            if (storeApi == null)
            {
                Common.LogDebug(true, "[StorePanel] ScheduleBackgroundAuthRefresh skipped: StoreApi is null.");
                return;
            }

            int generation = Interlocked.Increment(ref _authRefreshGeneration);
            Common.LogDebug(true, $"[StorePanel] ScheduleBackgroundAuthRefresh generation={generation}, store={storeApi.GetType().Name}.");
            storeApi.RefreshIsUserLoggedInInBackground(() =>
            {
                if (generation != _authRefreshGeneration || !ReferenceEquals(StoreApi, storeApi))
                {
                    Common.LogDebug(true, $"[StorePanel] Background auth refresh discarded (generation={generation}, current={_authRefreshGeneration}).");
                    return;
                }

                Common.LogDebug(true, $"[StorePanel] Background auth refresh applied for {storeApi.GetType().Name}, AuthStatus={AuthStatus}.");
                NotifyAuthStatusChanged();
            });
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
                ScheduleBackgroundAuthRefresh();
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
                ScheduleBackgroundAuthRefresh();
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
            if (!ShouldRefreshUserBinding(e.PropertyName))
            {
                return;
            }

            NotifyUserBindingChanged();
        }

        private static bool ShouldRefreshUserBinding(string propertyName)
        {
            return string.IsNullOrEmpty(propertyName)
                || propertyName == nameof(AccountInfos.AccountStatus)
                || propertyName == nameof(AccountInfos.Pseudo)
                || propertyName == nameof(AccountInfos.Avatar)
                || propertyName == nameof(AccountInfos.Link)
                || propertyName == nameof(AccountInfos.UserId);
        }

        private void NotifyUserBindingChanged()
        {
            void notify()
            {
                OnPropertyChanged(nameof(User));
                NotifyAccountEntryUi();
            }

            Dispatcher dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(notify));
                return;
            }

            notify();
        }

        private void NotifyAccountEntryUi()
        {
            OnPropertyChanged(nameof(IsManualAccountEntryEnabled));
            OnPropertyChanged(nameof(ShowGetAccountIdLink));
        }
    }
}
