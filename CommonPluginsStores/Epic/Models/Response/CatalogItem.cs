using System;
using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models.Response
{
    public class CatalogItem
    {
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("description")]
        public string Description { get; set; }

        [SerializationPropertyName("keyImages")]
        public List<KeyImage> KeyImages { get; set; }

        [SerializationPropertyName("categories")]
        public List<Category> Categories { get; set; }

        [SerializationPropertyName("namespace")]
        public string Namespace { get; set; }

        [SerializationPropertyName("status")]
        public string Status { get; set; }

        [SerializationPropertyName("creationDate")]
        public DateTime CreationDate { get; set; }

        [SerializationPropertyName("lastModifiedDate")]
        public DateTime LastModifiedDate { get; set; }

        [SerializationPropertyName("customAttributes")]
        public Dictionary<string, CustomAttribute> CustomAttributes { get; set; }

        [SerializationPropertyName("entitlementName")]
        public string EntitlementName { get; set; }

        [SerializationPropertyName("entitlementType")]
        public string EntitlementType { get; set; }

        [SerializationPropertyName("itemType")]
        public string ItemType { get; set; }

        [SerializationPropertyName("releaseInfo")]
        public List<ReleaseInfo> ReleaseInfo { get; set; }

        [SerializationPropertyName("developer")]
        public string Developer { get; set; }

        [SerializationPropertyName("developerId")]
        public string DeveloperId { get; set; }

        [SerializationPropertyName("eulaIds")]
        public List<string> EulaIds { get; set; }

        [SerializationPropertyName("endOfSupport")]
        public bool EndOfSupport { get; set; }

        [SerializationPropertyName("mainGameItem")]
        public MainGameItem MainGameItem { get; set; }

        [SerializationPropertyName("mainGameItemList")]
        public List<MainGameItem> MainGameItemList { get; set; }

        [SerializationPropertyName("ageGatings")]
        public AgeGatings AgeGatings { get; set; }

        [SerializationPropertyName("unsearchable")]
        public bool Unsearchable { get; set; }
    }
}