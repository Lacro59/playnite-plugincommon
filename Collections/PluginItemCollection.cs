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
    public class PluginItemCollection<TItem> : ItemCollection<TItem> where TItem : PluginDataBaseGameBase
    {
        private ILogger logger = LogManager.GetLogger();


        public PluginItemCollection(string path, GameDatabaseCollection type = GameDatabaseCollection.Uknown) : base(path, type)
        {
        }

        public void SetGameInfo<T>(IPlayniteAPI PlayniteApi)
        {
            while (!PlayniteApi.Database.IsOpen)
            {

            }

            foreach (var item in Items)
            {
                try
                {
                    Game game = PlayniteApi.Database.Games.Get(item.Key);
                    var temp = item.Value as PluginDataBaseGame<T>;

                    if (game != null && item.Value is PluginDataBaseGame<T>)
                    {
                        temp.Name = game.Name;
                        temp.Hidden = game.Hidden;
                        temp.Icon = game.Icon;
                        temp.CoverImage = game.CoverImage;
                        temp.GenreIds = game.GenreIds;
                        temp.Genres = game.Genres;
                        temp.Playtime = game.Playtime;
                        temp.IsSaved = true;
                    }
                    else
                    {
                        temp.IsDeleted = true;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "PluginCommon");
                }
            }
        }

        public void SetGameInfoDetails<T, Y>(IPlayniteAPI PlayniteApi)
        {
            while (!PlayniteApi.Database.IsOpen)
            {

            }

            foreach (var item in Items)
            {
                try
                {
                    Game game = PlayniteApi.Database.Games.Get(item.Key);
                    var temp = item.Value as PluginDataBaseGameDetails<T, Y>;

                    if (game != null && item.Value is PluginDataBaseGameDetails<T, Y>)
                    {
                        temp.Name = game.Name;
                        temp.Hidden = game.Hidden;
                        temp.Icon = game.Icon;
                        temp.CoverImage = game.CoverImage;
                        temp.GenreIds = game.GenreIds;
                        temp.Genres = game.Genres;
                        temp.Playtime = game.Playtime;
                        temp.IsSaved = true;
                    }
                    else
                    {
                        temp.IsDeleted = true;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "PluginCommon");
                }
            }
        }
    }
}
