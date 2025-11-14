using System;
using System.Collections.Generic;
using Playnite.SDK.Data;

namespace CommonPluginsStores.Epic.Models.Response
{
    public class PlaytimeItem
    {
        [SerializationPropertyName("accountId")]
        public string AccountId;

        [SerializationPropertyName("artifactId")]
        public string ArtifactId;

        [SerializationPropertyName("totalTime")]
        public int TotalTime;
    }
}
