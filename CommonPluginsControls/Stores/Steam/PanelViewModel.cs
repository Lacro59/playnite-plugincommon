﻿using System.Collections.Generic;
using CommonPluginsStores.Models;
using CommonPluginsStores.Models.Enumerations;
using CommonPluginsStores.Models.Interfaces;
using Playnite.SDK;

namespace CommonPluginsControls.Stores.Steam
{
    public class PanelViewModel : ObservableObject
    {
        internal IStoreApi StoreApi { get; set; }
        public AccountInfos User => StoreApi?.CurrentAccountInfos;

        private bool useApi = true;
        public bool UseApi { get => useApi; set => SetValue(ref useApi, value); }

        private bool useAuth = true;
        public bool UseAuth { get => useAuth; set => SetValue(ref useAuth, value); }

        private bool forceAuth = false;
        public bool ForceAuth { get => forceAuth; set => SetValue(ref forceAuth, value); }

        public AuthStatus AuthStatus => StoreApi == null ? AuthStatus.Failed : StoreApi.IsUserLoggedIn ? AuthStatus.Ok : AuthStatus.AuthRequired;

        public RelayCommand<object> LoginCommand => new RelayCommand<object>((a) =>
        {
            StoreApi.Login();
            OnPropertyChanged(nameof(AuthStatus));
            OnPropertyChanged(nameof(User));
        });

        public void ResetIsUserLoggedIn()
        {
            StoreApi.ResetIsUserLoggedIn();
            OnPropertyChanged(nameof(AuthStatus));
        }
    }
}
