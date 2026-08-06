using System.Collections.Generic;

namespace CommonPluginsStores.Ea.Models.Query
{
    /// <summary>
    /// Juno GraphQL query for catalog game products by Origin offer ids (public, no account).
    /// </summary>
    public class QueryGameProducts
    {
        /// <summary>
        /// GraphQL variables for <see cref="QueryGameProducts"/>.
        /// </summary>
        public class Variables
        {
            /// <summary>
            /// Origin / EA offer identifiers (Playnite GameId values).
            /// </summary>
            public List<string> offerIds = new List<string>();

            /// <summary>
            /// EA locale token (use DEFAULT when unsure).
            /// </summary>
            public string locale = "DEFAULT";
        }

        /// <summary>
        /// Query variables instance.
        /// </summary>
        public Variables variables = new Variables();

        /// <summary>
        /// GraphQL document selecting product id, name, originOfferId and gameSlug.
        /// </summary>
        public string query = @"
            query GetGameProducts($offerIds: [String!]!, $locale: Locale) {
              gameProducts(offerIds: $offerIds, locale: $locale) {
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
            }";
    }
}
