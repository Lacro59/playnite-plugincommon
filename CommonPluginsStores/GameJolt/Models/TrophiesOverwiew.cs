using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
    public class TrophiesOverwiew
    {
        [SerializationPropertyName("payload")]
        public PayloadTrophies Payload { get; set; }

        [SerializationPropertyName("ver")]
        public string Ver { get; set; }

        [SerializationPropertyName("user")]
        public object User { get; set; }

        [SerializationPropertyName("c")]
        public C C { get; set; }
    }

    public class PayloadTrophies
    {
        [SerializationPropertyName("trophies")]
        public List<Trophy> Trophies { get; set; }

        [SerializationPropertyName("pageSize")]
        public int PageSize { get; set; }
    }
}
