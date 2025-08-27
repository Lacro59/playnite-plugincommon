using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Ea.Models.Query
{
    public class ResponseAchievements
    {
        [SerializationPropertyName("data")]
        public DataAchievements Data { get; set; }
    }

    public class Achievement
    {
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("description")]
        public string Description { get; set; }

        [SerializationPropertyName("awardCount")]
        public int AwardCount { get; set; }

        [SerializationPropertyName("date")]
        public DateTime Date { get; set; }
    }

    public class Achievements
    {
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        [SerializationPropertyName("achievements")]
        public List<Achievement> AchievementsData { get; set; }
    }

    public class DataAchievements
    {
        [SerializationPropertyName("achievements")]
        public List<Achievements> Achievements { get; set; }
    }
}