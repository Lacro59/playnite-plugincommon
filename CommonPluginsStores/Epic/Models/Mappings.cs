using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models
{
    public class Mappings
    {
        [SerializationPropertyName("cmsSlug")]
        public string CmsSlug { get; set; }

        [SerializationPropertyName("offerId")]
        public object OfferId { get; set; }

        [SerializationPropertyName("prePurchaseOfferId")]
        public object PrePurchaseOfferId { get; set; }
    }
}
