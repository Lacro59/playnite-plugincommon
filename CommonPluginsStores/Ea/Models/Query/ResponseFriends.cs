using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Ea.Models.Query
{
    public class ResponseFriends
    {
        [SerializationPropertyName("data")]
        public DataFriends Data { get; set; }
    }

    public class DataFriends
    {
        [SerializationPropertyName("me")]
        public MeFriends Me { get; set; }
    }

    public class Friends
    {
        [SerializationPropertyName("items")]
        public List<ItemFriends> Items { get; set; }
    }

    public class ItemFriends
    {
        [SerializationPropertyName("player")]
        public Player Player { get; set; }
    }

    public class MeFriends
    {
        [SerializationPropertyName("friends")]
        public Friends Friends { get; set; }
    }
}
