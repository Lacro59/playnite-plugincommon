using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
    public class Trophy
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("user_id")]
        public int UserId { get; set; }

        [SerializationPropertyName("game_id")]
        public int GameId { get; set; }

        [SerializationPropertyName("game_trophy_id")]
        public int GameTrophyId { get; set; }

        [SerializationPropertyName("logged_on")]
        public long? LoggedOn { get; set; }

        [SerializationPropertyName("viewed_on")]
        public long? ViewedOn { get; set; }

        [SerializationPropertyName("game_trophy")]
        public GameTrophy GameTrophy { get; set; }

        [SerializationPropertyName("user")]
        public User User { get; set; }

        [SerializationPropertyName("game")]
        public Game Game { get; set; }

        [SerializationPropertyName("site_trophy_id")]
        public int? SiteTrophyId { get; set; }

        [SerializationPropertyName("site_trophy")]
        public SiteTrophy SiteTrophy { get; set; }
    }
}
