﻿using CommonPluginsStores.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores
{
    public enum AuthStatus
    {
        Ok,
        Checking,
        AuthRequired,
        PrivateAccount,
        Failed
    }

    public enum AccountStatus
    {
        Checking,
        Private,
        Public
    }


    public interface IStoreApi
    {
        StoreSettings StoreSettings { get; set; }

        bool IsUserLoggedIn { get; set; }
        AccountInfos CurrentAccountInfos { get; set; }

        void ResetIsUserLoggedIn();

        void SetLanguage(string local);

        void Login();
        void LoginAlternative();
        AccountInfos LoadCurrentUser();
        void SaveCurrentUser();
    }
}
