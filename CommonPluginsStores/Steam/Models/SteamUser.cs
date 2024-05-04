using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Steam.Models
{
    public class SteamUser : ObservableObject
    {
        public ulong SteamId { get; set; }
        public uint AccountId => SteamApi.GetAccountId(SteamId);
        public string AccountName { get; set; }
        public string PersonaName { get; set; }
        public string ApiKey { get; set; }


        private bool isPrivateAccount = true;
        public bool IsPrivateAccount { get => isPrivateAccount; set => SetValue(ref isPrivateAccount, value); }


        private AccountStatus accountStatus = AccountStatus.Checking;
        public AccountStatus AccountStatus { get => accountStatus; set => SetValue(ref accountStatus, value); }
    }
}
