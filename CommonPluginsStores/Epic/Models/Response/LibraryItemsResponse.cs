using System;
using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models.Response
{
    public class LibraryItemsResponse
    {
        [SerializationPropertyName("responseMetadata")]
        public ResponseMetadata ResponseMetadata { get; set; } = new ResponseMetadata();

        [SerializationPropertyName("records")]
        public List<Asset> Records { get; set; }
    }

    public class ResponseMetadata
    {
        [SerializationPropertyName("nextCursor")]
        public string NextCursor { get; set; }

        [SerializationPropertyName("stateToken")]
        public string StateToken { get; set; }
    }
}