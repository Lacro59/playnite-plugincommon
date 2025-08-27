using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Ea.Models.Query
{
    public class QueryFriends
    {
        public string query = @"
            query {
              me {
                friends {
                  items {
                    player {
                    pd
                    psd
                    displayName
                    uniqueName
                    nickname
                    avatar {
                        medium {
                        path
                        }
                    }
                    }
                  }
                }
              }
            }";
    }
}