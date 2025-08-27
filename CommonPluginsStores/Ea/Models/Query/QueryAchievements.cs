using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Ea.Models.Query
{
    public class QueryAchievements
    {
        public class Variables
        {
            public string offerId = string.Empty;
            public string playerPsd = string.Empty;
            public string locale = "US";
        }

        public Variables variables = new Variables();
        public string query = @"
            query GetAchievements($offerId: String!, $playerPsd: String!, $locale: Locale!) {
              achievements(
                offerId: $offerId
                playerPsd: $playerPsd
                showHidden: true
                locale: $locale
              ) {
                id
                achievements {
                  id
                  name
                  description
                  awardCount
                  date
                }
              }
            }";
    }
}