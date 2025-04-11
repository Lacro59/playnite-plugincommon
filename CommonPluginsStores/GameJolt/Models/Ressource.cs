using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
    public class Resource
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("game_id")]
        public int GameId { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("description")]
        public object Description { get; set; }

        [SerializationPropertyName("sort")]
        public int Sort { get; set; }

        [SerializationPropertyName("added_on")]
        public long AddedOn { get; set; }

        [SerializationPropertyName("published_on")]
        public long PublishedOn { get; set; }

        [SerializationPropertyName("updated_on")]
        public long UpdatedOn { get; set; }

        [SerializationPropertyName("visibility")]
        public string Visibility { get; set; }

        [SerializationPropertyName("partner_visibility")]
        public object PartnerVisibility { get; set; }

        [SerializationPropertyName("status")]
        public string Status { get; set; }

        [SerializationPropertyName("game")]
        public Game Game { get; set; }
    }
}
