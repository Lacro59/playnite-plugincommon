using System;
using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models.Response
{
    public class PlaytimeItem
    {
        [SerializationPropertyName("accountId")]
        public string AccountId { get; set; }

        [SerializationPropertyName("artifactId")]
        public string ArtifactId { get; set; }

        [SerializationPropertyName("totalTime")]
        public int TotalTime { get; set; }
    }
}
