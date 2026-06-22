using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models
{
    public class Mapping
    {
        [SerializationPropertyName("createdDate")]
        public string CreatedDate { get; set; }

        [SerializationPropertyName("deletedDate")]
        public string DeletedDate { get; set; }

        [SerializationPropertyName("mappings")]
        public Mappings Mappings { get; set; }

        [SerializationPropertyName("pageSlug")]
        public string PageSlug { get; set; }

        [SerializationPropertyName("pageType")]
        public string PageType { get; set; }

        [SerializationPropertyName("productId")]
        public string ProductId { get; set; }

        [SerializationPropertyName("sandboxId")]
        public string SandboxId { get; set; }

        [SerializationPropertyName("updatedDate")]
        public string UpdatedDate { get; set; }
    }
}
