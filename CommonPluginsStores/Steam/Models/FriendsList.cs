using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Steam.Models
{
    public class FriendsList
    {
        public Friendslist friendslist { get; set; }
    }

    public class Friend
    {
        public string steamid { get; set; }
        public string relationship { get; set; }
        public int friend_since { get; set; }
    }

    public class Friendslist
    {
        public List<Friend> friends { get; set; }
    }
}
