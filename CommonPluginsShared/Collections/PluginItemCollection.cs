using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPluginsPlaynite.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CommonPluginsShared.Collections
{
    public class PluginItemCollection<TItem> : ItemCollection<TItem> where TItem : PluginDataBaseGameBase
    {
        private ILogger logger = LogManager.GetLogger();


        public PluginItemCollection(string path, GameDatabaseCollection type = GameDatabaseCollection.Uknown) : base(path, type)
        {
        }

        public void SetGameInfo<T>(IPlayniteAPI PlayniteApi, Guid Id)
        {
            try
            {
                Items.TryGetValue(Id, out var item);
                Game game = PlayniteApi.Database.Games.Get(Id);

                if (game != null && item is PluginDataBaseGame<T>)
                {
                    item.Name = game.Name;
                    item.SourceId = game.SourceId;
                    item.Hidden = game.Hidden;
                    item.Icon = game.Icon;
                    item.CoverImage = game.CoverImage;
                    item.BackgroundImage = game.BackgroundImage;
                    item.GenreIds = game.GenreIds;
                    item.Genres = game.Genres;
                    item.Playtime = game.Playtime;
                    item.LastActivity = game.LastActivity;
                    item.IsSaved = true;
                }
                else
                {
                    item.IsDeleted = true;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        public void SetGameInfo<T>(IPlayniteAPI PlayniteApi)
        {
            System.Threading.SpinWait.SpinUntil(() => PlayniteApi.Database.IsOpen, -1);

            foreach (var item in Items)
            {
                SetGameInfo<T>(PlayniteApi, item.Key);
            }
        }

        public void SetGameInfoDetails<T, Y>(IPlayniteAPI PlayniteApi, Guid Id)
        {
            try
            {
                Items.TryGetValue(Id, out var item);
                Game game = PlayniteApi.Database.Games.Get(Id);

                if (game != null && item is PluginDataBaseGameDetails<T, Y>)
                {
                    item.Name = game.Name;
                    item.SourceId = game.SourceId;
                    item.Hidden = game.Hidden;
                    item.Icon = game.Icon;
                    item.CoverImage = game.CoverImage;
                    item.BackgroundImage = game.BackgroundImage;
                    item.GenreIds = game.GenreIds;
                    item.Genres = game.Genres;
                    item.Playtime = game.Playtime;
                    item.LastActivity = game.LastActivity;
                    item.IsSaved = true;
                }
                else
                {
                    item.IsDeleted = true;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        public void SetGameInfoDetails<T, Y>(IPlayniteAPI PlayniteApi)
        {
            System.Threading.SpinWait.SpinUntil(() => PlayniteApi.Database.IsOpen, -1);

            foreach (var item in Items)
            {
                SetGameInfoDetails<T, Y>(PlayniteApi, item.Key);
            }
        }
    }
}
