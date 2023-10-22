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


        private bool _IsPrivateAccount = true;
        public bool IsPrivateAccount { get => _IsPrivateAccount; set => SetValue(ref _IsPrivateAccount, value); }


        private AccountStatus _AccountStatus = AccountStatus.Checking;
        public AccountStatus AccountStatus { get => _AccountStatus; set => SetValue(ref _AccountStatus, value); }
    }
}
