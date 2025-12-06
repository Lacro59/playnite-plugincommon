using System;
using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models
{
    public class Asset
    {
        [SerializationPropertyName("namespace")]
        public string Namespace { get; set; }

        [SerializationPropertyName("catalogItemId")]
        public string CatalogItemId { get; set; }

        [SerializationPropertyName("appName")]
        public string AppName { get; set; }

        [SerializationPropertyName("productId")]
        public string ProductId { get; set; }

        [SerializationPropertyName("sandboxName")]
        public string SandboxName { get; set; }

        [SerializationPropertyName("sandboxType")]
        public string SandboxType { get; set; }

        [SerializationPropertyName("recordType")]
        public string RecordType { get; set; }

        [SerializationPropertyName("acquisitionDate")]
        public DateTime AcquisitionDate { get; set; }

        [SerializationPropertyName("dependencies")]
        public List<object> Dependencies { get; set; }
    }


}
