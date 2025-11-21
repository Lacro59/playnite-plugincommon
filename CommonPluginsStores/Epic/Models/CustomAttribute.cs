using System;
using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models
{
    public class CustomAttribute
    {
        [SerializationPropertyName("key")]
        public string Key { get; set; }

        [SerializationPropertyName("value")]
        public string Value { get; set; }

        [SerializationPropertyName("type")]
        public string Type { get; set; }
    }
}
