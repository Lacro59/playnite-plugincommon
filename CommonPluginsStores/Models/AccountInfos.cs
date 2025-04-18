using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Models
{
    public class AccountInfos : ObservableObject
    {
        private string _userId;
        public string UserId { get => _userId; set => SetValue(ref _userId, value); }

        private string _clientId;
        public string ClientId { get => _clientId; set => SetValue(ref _clientId, value); }

        private string _pseudo;
        public string Pseudo { get => _pseudo; set => SetValue(ref _pseudo, value); }

        private string _avatar;
        public string Avatar { get => _avatar; set => SetValue(ref _avatar, value); }

        private string _link;
        public string Link { get => _link; set => SetValue(ref _link, value); }

        public DateTime? DateAdded { get; set; }

        public bool IsCurrent { get; set; } = false;
        [DontSerialize]
        public bool IsPrivate { get; set; } = true;

        private AccountStatus _accountStatus = AccountStatus.Checking;
        [DontSerialize]
        public AccountStatus AccountStatus { get => _accountStatus; set => SetValue(ref _accountStatus, value); }

        public string ApiKey { get; set; }

        public DateTime LastCall => DateTime.Now;
    }
}
