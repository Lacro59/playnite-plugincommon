using System.Collections.Generic;

namespace CommonPluginsStores.Ea.Models.Query
{
    /// <summary>
    /// Public Juno <c>games(slugs)</c> query for names and Origin offer ids.
    /// </summary>
    public class QueryGamesBySlugs
    {
        /// <summary>
        /// GraphQL variables for <see cref="QueryGamesBySlugs"/>.
        /// </summary>
        public class Variables
        {
            /// <summary>
            /// Catalog slugs to resolve.
            /// </summary>
            public List<string> slugs = new List<string>();

            /// <summary>
            /// EA locale token.
            /// </summary>
            public string locale = "DEFAULT";
        }

        /// <summary>
        /// Query variables instance.
        /// </summary>
        public Variables variables = new Variables();

        /// <summary>
        /// GraphQL document selecting product name and originOfferId per slug.
        /// </summary>
        public string query = @"
            query GetGamesBySlugs($slugs: [String!]!, $locale: Locale) {
              games(slugs: $slugs, locale: $locale) {
                items {
                  slug
                  products {
                    items {
                      id
                      name
                      originOfferId
                      gameSlug
                      baseItem {
                        title
                        gameType
                      }
                    }
                  }
                }
              }
            }";
    }
}
