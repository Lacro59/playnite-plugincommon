using System;
using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models
{
    public class Category
    {
        [SerializationPropertyName("path")]
        public string Path { get; set; }
    }
}
