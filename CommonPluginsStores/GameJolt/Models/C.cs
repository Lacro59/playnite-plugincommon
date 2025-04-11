using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
    public class C
    {
        [SerializationPropertyName("eea")]
        public bool Eea { get; set; }

        [SerializationPropertyName("ads")]
        public bool Ads { get; set; }
    }
}
