using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Ea.Models.Query
{
    public class QueryRecentGames
    {
        public class Variables
        {
            public List<string> gameSlugs = new List<string>();
        }

        public Variables variables = new Variables();
        public string query = @"
            query GetRecentGames($gameSlugs: [String!]!) {
              me {
                recentGames(gameSlugs: $gameSlugs) {
                  items {
                    gameSlug
                    lastSessionEndDate
                    totalPlayTimeSeconds
                  }
                }
              }
            }";
    }
}