using System.Collections.Generic;

namespace CommonPluginsStores.Ea.Models.Query
{
    /// <summary>
    /// Public Juno <c>gameSearch</c> query returning catalog slugs (no account).
    /// </summary>
    public class QueryGameSearch
    {
        /// <summary>
        /// GraphQL variables for <see cref="QueryGameSearch"/>.
        /// </summary>
        public class Variables
        {
            /// <summary>
            /// Page size (EA accepts up to ~9999 for a full BASE_GAME catalog dump).
            /// </summary>
            public int limit = 9999;
        }

        /// <summary>
        /// Query variables instance.
        /// </summary>
        public Variables variables = new Variables();

        /// <summary>
        /// GraphQL document listing base-game / collection slugs.
        /// </summary>
        public string query = @"
            query GetGameSearch($limit: Int!) {
              gameSearch(
                filter: { gameTypes: [BASE_GAME, COLLECTION] }
                paging: { limit: $limit }
              ) {
                items {
                  slug
                }
              }
            }";
    }
}
