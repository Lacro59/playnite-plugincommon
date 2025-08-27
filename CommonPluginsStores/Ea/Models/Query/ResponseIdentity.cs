using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Ea.Models.Query
{
    public class ResponseIdentity
    {
        [SerializationPropertyName("data")]
        public DataIdentity Data { get; set; }
    }
    public class DataIdentity
    {
        [SerializationPropertyName("me")]
        public MeIdentity Me { get; set; }
    }

    public class MeIdentity
    {
        [SerializationPropertyName("player")]
        public Player Player { get; set; }
    }

    public class Avatar
    {
        [SerializationPropertyName("medium")]
        public Medium Medium { get; set; }
    }

    public class Medium
    {
        [SerializationPropertyName("path")]
        public string Path { get; set; }
    }

    public class Player
    {
        [SerializationPropertyName("pd")]
        public string Pd { get; set; }

        [SerializationPropertyName("psd")]
        public string Psd { get; set; }

        [SerializationPropertyName("displayName")]
        public string DisplayName { get; set; }

        [SerializationPropertyName("uniqueName")]
        public string UniqueName { get; set; }

        [SerializationPropertyName("nickname")]
        public string Nickname { get; set; }

        [SerializationPropertyName("avatar")]
        public Avatar Avatar { get; set; }
    }
}
