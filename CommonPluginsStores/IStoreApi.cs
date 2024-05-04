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

    }
}
