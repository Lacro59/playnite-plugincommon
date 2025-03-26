using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Models
{
    public class AccountInfos : ObservableObject
    {
        public string UserId { get; set; }
        public string ClientId { get; set; }
        public string Pseudo { get; set; }
        public string Avatar { get; set; }
        public string Link { get; set; }

        public DateTime? DateAdded { get; set; }


        public bool IsCurrent { get; set; } = false;
        [DontSerialize]
        public bool IsPrivate { get; set; } = true;

        private AccountStatus accountStatus = AccountStatus.Checking;
        [DontSerialize]
        public AccountStatus AccountStatus { get => accountStatus; set => SetValue(ref accountStatus, value); }

        public string ApiKey { get; set; }

        public DateTime LastCall => DateTime.Now;
    }
}
