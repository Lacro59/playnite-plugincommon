using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
    public class Sellable
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("primary")]
        public bool Primary { get; set; }

        [SerializationPropertyName("key")]
        public string Key { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("description")]
        public object Description { get; set; }

        [SerializationPropertyName("pricings")]
        public List<object> Pricings { get; set; }

        [SerializationPropertyName("linked_key_providers")]
        public List<object> LinkedKeyProviders { get; set; }

        [SerializationPropertyName("resource_type")]
        public string ResourceType { get; set; }

        [SerializationPropertyName("resource")]
        public Resource Resource { get; set; }
    }
}
