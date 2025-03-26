using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Epic.Models
{
    public class EpicFriendsSummary
    {
        [SerializationPropertyName("friends")]
        public List<Friend> Friends { get; set; }

        [SerializationPropertyName("incoming")]
        public List<Incoming> Incoming { get; set; }

        [SerializationPropertyName("outgoing")]
        public List<object> Outgoing { get; set; }

        [SerializationPropertyName("suggested")]
        public List<object> Suggested { get; set; }

        [SerializationPropertyName("blocklist")]
        public List<object> Blocklist { get; set; }

        [SerializationPropertyName("settings")]
        public Settings Settings { get; set; }

        [SerializationPropertyName("limitsReached")]
        public LimitsReached LimitsReached { get; set; }
    }

    public class Friend
    {
        [SerializationPropertyName("accountId")]
        public string AccountId { get; set; }

        [SerializationPropertyName("groups")]
        public List<object> Groups { get; set; }

        [SerializationPropertyName("mutual")]
        public int Mutual { get; set; }

        [SerializationPropertyName("alias")]
        public string Alias { get; set; }

        [SerializationPropertyName("note")]
        public string Note { get; set; }

        [SerializationPropertyName("favorite")]
        public bool Favorite { get; set; }

        [SerializationPropertyName("created")]
        public DateTime Created { get; set; }
    }

    public class Incoming
    {
        [SerializationPropertyName("accountId")]
        public string AccountId { get; set; }

        [SerializationPropertyName("mutual")]
        public int Mutual { get; set; }

        [SerializationPropertyName("favorite")]
        public bool Favorite { get; set; }

        [SerializationPropertyName("created")]
        public DateTime Created { get; set; }
    }

    public class LimitsReached
    {
        [SerializationPropertyName("incoming")]
        public bool Incoming { get; set; }

        [SerializationPropertyName("outgoing")]
        public bool Outgoing { get; set; }

        [SerializationPropertyName("accepted")]
        public bool Accepted { get; set; }
    }

    public class Settings
    {
        [SerializationPropertyName("acceptInvites")]
        public string AcceptInvites { get; set; }

        [SerializationPropertyName("mutualPrivacy")]
        public string MutualPrivacy { get; set; }
    }


}
