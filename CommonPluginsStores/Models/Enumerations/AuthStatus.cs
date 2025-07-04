using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Models.Enumerations
{
    public enum AuthStatus
    {
        Ok,
        Checking,
        AuthRequired,
        PrivateAccount,
        Failed,
        Unknown
    }
}
