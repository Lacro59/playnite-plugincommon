using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Ea.Models.Query
{
    public class QueryOwnedGameProducts
    {
        public class Variables
        {
            public string locale = "DEFAULT";
            public bool entitlementEnabled = true;
            public List<string> storefronts = new List<string> { "EA" };
            public List<string> type = new List<string>
            {
                "DIGITAL_FULL_GAME",
                "PACKAGED_FULL_GAME",
                "DIGITAL_EXTRA_CONTENT",
                "PACKAGED_EXTRA_CONTENT"
            };
            public List<string> platforms = new List<string> { "PC" };
            public int limit = 9999;
        }

        public Variables variables = new Variables();

        public string query = @"
            query GetOwnedGameProducts(
                $locale: Locale!
                $entitlementEnabled: Boolean!
                $storefronts: [UserGameProductStorefront!]!
                $type: [GameProductType!]!
                $platforms: [GamePlatform!]!
                $limit: Int!
            ) {
              me {
                ownedGameProducts(
                  locale: $locale
                  entitlementEnabled: $entitlementEnabled
                  storefronts: $storefronts
                  type: $type
                  platforms: $platforms
                  paging: { limit: $limit }
                ) {
                  items {
                    originOfferId
                    product {
                      id
                      name
                      gameSlug
                      baseItem {
                        gameType
                      }
                      gameProductUser {
                        ownershipMethods
                        entitlementId
                      }
                    }
                  }
                }
              }
            }";
    }
}