using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
    public class TrophiesGame
    {
        [SerializationPropertyName("payload")]
        public Payload Payload { get; set; }

        [SerializationPropertyName("ver")]
        public string Ver { get; set; }

        [SerializationPropertyName("user")]
        public User User { get; set; }

        [SerializationPropertyName("c")]
        public C C { get; set; }
    }

    public class Payload
    {
        [SerializationPropertyName("trophies")]
        public List<TrophyGame> Trophies { get; set; }

        [SerializationPropertyName("trophiesAchieved")]
        public List<TrophiesAchieved> TrophiesAchieved { get; set; }

        [SerializationPropertyName("trophiesExperienceAchieved")]
        public int TrophiesExperienceAchieved { get; set; }

        [SerializationPropertyName("trophiesShowInvisibleTrophyMessage")]
        public bool TrophiesShowInvisibleTrophyMessage { get; set; }
    }

    public class TrophiesAchieved
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
        public long LoggedOn { get; set; }

        [SerializationPropertyName("viewed_on")]
        public object ViewedOn { get; set; }
    }

    public class TrophyGame
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("game_id")]
        public int GameId { get; set; }

        [SerializationPropertyName("difficulty")]
        public int Difficulty { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("description")]
        public string Description { get; set; }

        [SerializationPropertyName("secret")]
        public bool Secret { get; set; }

        [SerializationPropertyName("visible")]
        public bool Visible { get; set; }

        [SerializationPropertyName("sort")]
        public int Sort { get; set; }

        [SerializationPropertyName("experience")]
        public int Experience { get; set; }

        [SerializationPropertyName("img_thumbnail")]
        public string ImgThumbnail { get; set; }

        [SerializationPropertyName("has_thumbnail")]
        public bool HasThumbnail { get; set; }

        [SerializationPropertyName("is_owner")]
        public bool IsOwner { get; set; }

        [SerializationPropertyName("is_achieved")]
        public bool IsAchieved { get; set; }

        [SerializationPropertyName("has_perms")]
        public bool HasPerms { get; set; }
    }
}
