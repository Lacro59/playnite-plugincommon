using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
    public class ProfileTrophiesGame
    {
        [SerializationPropertyName("payload")]
        public PayloadProfileTrophiesGame Payload { get; set; }

        [SerializationPropertyName("ver")]
        public string Ver { get; set; }

        [SerializationPropertyName("user")]
        public object User { get; set; }

        [SerializationPropertyName("c")]
        public C C { get; set; }
    }

    public class Completion
    {
        [SerializationPropertyName("experience")]
        public int Experience { get; set; }

        [SerializationPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [SerializationPropertyName("achievedCount")]
        public int AchievedCount { get; set; }
    }

    public class PayloadProfileTrophiesGame
    {
        [SerializationPropertyName("trophies")]
        public List<Trophy> Trophies { get; set; }

        [SerializationPropertyName("game")]
        public Game Game { get; set; }

        [SerializationPropertyName("completion")]
        public Completion Completion { get; set; }
    }
}
