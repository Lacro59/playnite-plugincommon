using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.GameJolt.Models;
using CommonPluginsStores.Models;
using Playnite.SDK.Data;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.GameJolt
{
    public class GameJoltApi : StoreApi
    {
        #region urls
        private static string UrlBase => @"https://gamejolt.com";
        private static string UrlSiteApi => UrlBase + "/site-api";
        private static string UrlTrophiesGame => UrlSiteApi + "/web/discover/games/trophies/{0}";
        private static string UrlTrophiesOverview => UrlSiteApi + "/web/profile/trophies/overview/{0}";
        private static string UrlUserGameAchievements => UrlBase + "/{0}/trophies/game/{1}";
        #endregion


        public GameJoltApi(string pluginName, ExternalPlugin pluginLibrary) : base(pluginName, pluginLibrary, "Game Jolt")
        {
        }

        #region Configuration
        protected override bool GetIsUserLoggedIn()
        {
            return true;
        }
        #endregion

        #region Current user
        protected override AccountInfos GetCurrentAccountInfos()
        {
            return new AccountInfos { IsCurrent = true };
        }
        #endregion

        #region User details
        public override ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos)
        {
            throw new NotImplementedException();
        }

        public override ObservableCollection<GameAchievement> GetAchievements(string id, AccountInfos accountInfos)
        {
            try
            {
                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
                Tuple<string, ObservableCollection<GameAchievement>> data = GetAchievementsSchema(id);

                if (data?.Item2?.Count() == 0)
                {
                    return gameAchievements;
                }

                gameAchievements = data.Item2;

                string url = string.Format(UrlTrophiesOverview, accountInfos.Pseudo);
                string response = Web.DownloadStringData(url, null, Web.UserAgent).GetAwaiter().GetResult();
                if (response.IsNullOrEmpty())
                {
                    Thread.Sleep(1000);
                    response = Web.DownloadStringData(url, null, Web.UserAgent).GetAwaiter().GetResult();
                }
                _ = Serialization.TryFromJson(response, out TrophiesOverwiew trophiesOverwiew, out Exception ex);

                trophiesOverwiew?.Payload?.Trophies?
                    .Where(x => x?.GameId.ToString().IsEqual(id) ?? false)?
                    .ForEach(x =>
                    {
                        GameAchievement item = gameAchievements.FirstOrDefault(y => y.Id.IsEqual(x.GameTrophyId.ToString()));
                        if (item != null)
                        {
                            item.DateUnlocked = x.LoggedOn == null ? new DateTime(1982, 15, 12, 0, 0, 0, 0) : new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((long)x.LoggedOn);
                        }
                    });

                return gameAchievements;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override SourceLink GetAchievementsSourceLink(string name, string id, AccountInfos accountInfos)
        {
            return new SourceLink
            {
                GameName = name,
                Name = ClientName,
                Url = string.Format(UrlUserGameAchievements, accountInfos.Pseudo, id)
            };
        }
        #endregion

        #region Game
        public override Tuple<string, ObservableCollection<GameAchievement>> GetAchievementsSchema(string id)
        {
            string cachePath = Path.Combine(PathAchievementsData, id + ".json");
            Tuple<string, ObservableCollection<GameAchievement>> data = LoadData<Tuple<string, ObservableCollection<GameAchievement>>>(cachePath, 1440);

            if (data?.Item2 == null)
            {
                string url = string.Format(UrlTrophiesGame, id);
                string response = Web.DownloadStringData(url, null, Web.UserAgent).GetAwaiter().GetResult();
                if (response.IsNullOrEmpty())
                {
                    Thread.Sleep(1000);
                    response = Web.DownloadStringData(url, null, Web.UserAgent).GetAwaiter().GetResult();
                }
                _ = Serialization.TryFromJson(response, out TrophiesGame trophiesGame);

                ObservableCollection<GameAchievement> gameAchievements = trophiesGame?.Payload?.Trophies?
                    .Where(x => x?.GameId.ToString().IsEqual(id) ?? false)?
                    .Select(x => new GameAchievement
                    {
                        Id = x.Id.ToString(),
                        Name = x.Title,
                        Description = x.Description,
                        UrlUnlocked = x.ImgThumbnail,
                        Percent = 100,
                        GamerScore = x.Experience,
                        IsHidden = x.Secret
                    }).ToObservable() ?? new ObservableCollection<GameAchievement>();

                data = new Tuple<string, ObservableCollection<GameAchievement>>(id, gameAchievements);
                FileSystem.WriteStringToFile(cachePath, Serialization.ToJson(data));
            }

            return data;
        }
        #endregion

        #region Games owned

        #endregion

        #region Game Jolt

        #endregion
    }
}
