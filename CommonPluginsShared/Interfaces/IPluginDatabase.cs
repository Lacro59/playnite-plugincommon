using CommonPluginsShared.Collections;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsShared.Interfaces
{
    public interface IPluginDatabase
    {
        string PluginName { get; set; }

        bool IsLoaded { get; set; }

        Task<bool> InitializeDatabase();


        PluginDataBaseGameBase Get(Game game, bool OnlyCache = false, bool Force = false);
        PluginDataBaseGameBase Get(Guid Id, bool OnlyCache = false, bool Force = false);

        PluginDataBaseGameBase GetClone(Guid Id);
        PluginDataBaseGameBase GetClone(Game game);

        PluginDataBaseGameBase MergeData(Guid fromId, Guid toId);


        bool Remove(Game game);
        bool Remove(Guid Id);

        void AddOrUpdate(PluginDataBaseGameBase item);


        void Refresh(Guid Id);
        void Refresh(List<Guid> Ids);

        void RefreshWithNoData(List<Guid> Ids);

        List<Game> GetGamesWithNoData();
    }
}
