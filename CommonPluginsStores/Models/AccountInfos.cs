using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Models
{
    public class AccountInfos
    {
        public long UserId { get; set; }
        public long ClientId { get; set; }
        public string Pseudo { get; set; }
        public string Avatar { get; set; }
        public string Link { get; set; }
        public DateTime? DateAdded { get; set; }
        public bool IsCurrent { get; set; }
    }
}
