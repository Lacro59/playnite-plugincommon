using CommonPluginsStores.Models.Enumerations;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace CommonPluginsStores.Models
{
    /// <summary>
    /// Represents user account information for a store.
    /// </summary>
    public class AccountInfos : ObservableObject
    {
        private string _userId;

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public string UserId { get => _userId; set => SetValue(ref _userId, value); }

        private string _clientId;

        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        public string ClientId { get => _clientId; set => SetValue(ref _clientId, value); }

        private string _pseudo;

        /// <summary>
        /// Gets or sets the user pseudo or display name.
        /// </summary>
        public string Pseudo { get => _pseudo; set => SetValue(ref _pseudo, value); }

        private string _avatar;

        /// <summary>
        /// Gets or sets the user avatar URL.
        /// </summary>
        public string Avatar { get => _avatar; set => SetValue(ref _avatar, value); }

        private string _link;

        /// <summary>
        /// Gets or sets the user profile link.
        /// </summary>
        public string Link { get => _link; set => SetValue(ref _link, value); }

        /// <summary>
        /// Gets or sets the date the account was added.
        /// </summary>
        public DateTime? DateAdded { get; set; }

        /// <summary>
        /// Gets or sets whether this account is the current active account.
        /// </summary>
        public bool IsCurrent { get; set; } = false;

        /// <summary>
        /// Gets or sets whether this account is private (not shared).
        /// </summary>
        [DontSerialize]
        public bool IsPrivate { get; set; } = true;

        private AccountStatus _accountStatus = AccountStatus.Unknown;

        /// <summary>
        /// Gets or sets the current account status.
        /// </summary>
        [DontSerialize]
        public AccountStatus AccountStatus { get => _accountStatus; set => SetValue(ref _accountStatus, value); }

        /// <summary>
        /// Gets or sets the API key associated with this account.
        /// </summary>
        public string ApiKey { get; set; }
    }
}
