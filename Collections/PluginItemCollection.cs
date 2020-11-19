using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon.PlayniteResources.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PluginCommon.Collections
{
    public class PluginItemCollection<TItem> : ItemCollection<TItem> where TItem : DatabaseObject
    {
        public PluginItemCollection(string path, GameDatabaseCollection type = GameDatabaseCollection.Uknown) : base(path, type)
        {
        }

        public void SetGameInfo<T>(IPlayniteAPI PlayniteApi)
        {
            foreach (var item in Items)
            {
                Game game = PlayniteApi.Database.Games.Get(item.Key);

                if (game != null && item.Value is PluginDataBaseGame<T>)
                {
                    var temp = item.Value as PluginDataBaseGame<T>;

                    temp.Name = game.Name;
                    temp.Hidden = game.Hidden;
                    temp.Icon = game.Icon;
                    temp.CoverImage = game.CoverImage;
                    temp.GenreIds = game.GenreIds;
                    temp.Genres = game.Genres;
                }
            }
        }

        public void SetGameInfoDetails<T, Y>(IPlayniteAPI PlayniteApi)
        {
            foreach (var item in Items)
            {
                Game game = PlayniteApi.Database.Games.Get(item.Key);

                if (game != null && item.Value is PluginDataBaseGameDetails<T, Y>)
                {
                    var temp = item.Value as PluginDataBaseGameDetails<T, Y>;

                    temp.Name = game.Name;
                    temp.Hidden = game.Hidden;
                    temp.Icon = game.Icon;
                    temp.CoverImage = game.CoverImage;
                    temp.GenreIds = game.GenreIds;
                    temp.Genres = game.Genres;
                }
            }
        }
    }
}
