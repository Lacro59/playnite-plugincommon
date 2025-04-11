using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.GameJolt.Models
{
    public class Developer
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("type")]
        public string Type { get; set; }

        [SerializationPropertyName("username")]
        public string Username { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("display_name")]
        public string DisplayName { get; set; }

        [SerializationPropertyName("web_site")]
        public string WebSite { get; set; }

        [SerializationPropertyName("url")]
        public string Url { get; set; }

        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        [SerializationPropertyName("img_avatar")]
        public string ImgAvatar { get; set; }

        [SerializationPropertyName("shouts_enabled")]
        public bool ShoutsEnabled { get; set; }

        [SerializationPropertyName("friend_requests_enabled")]
        public bool FriendRequestsEnabled { get; set; }

        [SerializationPropertyName("is_spawnday")]
        public bool IsSpawnday { get; set; }

        [SerializationPropertyName("status")]
        public int Status { get; set; }

        [SerializationPropertyName("permission_level")]
        public int PermissionLevel { get; set; }

        [SerializationPropertyName("created_on")]
        public long CreatedOn { get; set; }

        [SerializationPropertyName("theme")]
        public Theme Theme { get; set; }

        [SerializationPropertyName("follower_count")]
        public int FollowerCount { get; set; }

        [SerializationPropertyName("following_count")]
        public int FollowingCount { get; set; }

        [SerializationPropertyName("avatar_frame")]
        public object AvatarFrame { get; set; }
    }
}
