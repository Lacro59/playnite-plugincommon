using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Ea.Models.Query
{
    public class QueryIdentity
    {
        public string query = @"
            query {
                me {
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
            }";
    }
}