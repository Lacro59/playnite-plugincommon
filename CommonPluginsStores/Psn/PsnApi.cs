using CommonPlayniteShared.Common.Web;
using CommonPlayniteShared.PluginLibrary.PSNLibrary.Models;
using CommonPlayniteShared.PluginLibrary.PSNLibrary.Services;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using CommonPluginsStores.Psn.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Psn
{
    public class PsnApi : StoreApi
    {
        #region Urls

        private static string UrlGraphql => "https://web.np.playstation.com/api/graphql/v1";
        private static string UrlGetAddOnProductsByProductId => UrlGraphql + "/op?operationName=getAddOnProductsByProductId&variables={0}";

        private static string UrlStore => "https://store.playstation.com";
        private static string UrlGameStore => UrlStore + "/product/{0}/";
        #endregion

        private static readonly Lazy<PSNClient> _psnClient = new Lazy<PSNClient>(() => new PSNClient(PsnDataPath));
        private static PSNClient PsnClient => _psnClient.Value;

        #region Paths

        private static string PsnDataPath { get; set; }

        #endregion

        public PsnApi(string PluginName) : base(PluginName, ExternalPlugin.PSNLibrary, "PSN")
        {
            PsnDataPath = PathStoresData + "\\..\\" + GetPluginId(ExternalPlugin.PSNLibrary);
        }

        #region Configuration

        protected override bool GetIsUserLoggedIn()
        {
            PsnClient.CheckAuthentication().GetAwaiter().GetResult();
            bool isLogged = PsnClient.GetIsUserLoggedIn().GetAwaiter().GetResult();
            return isLogged;
        }

        #endregion

        #region Current user

        protected override AccountInfos GetCurrentAccountInfos()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                return new AccountInfos { IsCurrent = true };
            }
            catch (Exception ex) 
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        protected override ObservableCollection<AccountInfos> GetCurrentFriendsInfos()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {

            }
            catch (Exception ex) 
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        #endregion

        #region User details

        public override ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override ObservableCollection<GameAchievement> GetAchievements(string id, AccountInfos accountInfos)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                
            }
            catch (Exception ex)
            {
                // Error 403 when no data
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override SourceLink GetAchievementsSourceLink(string name, string id, AccountInfos accountInfos)
        {
            string langUrl = CodeLang.GetEpicLang(Locale);
            return new SourceLink
            {
                GameName = name,
                Name = ClientName,
                Url = $""
            };
        }
        
        public override ObservableCollection<AccountWishlist> GetWishlist(AccountInfos accountInfos)
        {
            if (accountInfos != null)
            {
                try
                {

                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error in {ClientName} wishlist", true, PluginName);
                }
            }

            return null;
        }

        public override bool RemoveWishlist(string id)
        {
            if (IsUserLoggedIn)
            {
                try
                {

                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error remove {id} in {ClientName} wishlist", true, PluginName);
                }
            }

            return false;
        }

        #endregion

        #region Game

        public override GameInfos GetGameInfos(string id, AccountInfos accountInfos)
        {
            try
            {

            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override ObservableCollection<DlcInfos> GetDlcInfos(string id, AccountInfos accountInfos)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            ObservableCollection<DlcInfos> dlcs = new ObservableCollection<DlcInfos>();
            try
            {
                CookieContainer cookieContainer = PsnClient.ReadCookiesFromDisk();
                using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                {
                    using (var httpClient = new HttpClient(handler))
                    {
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-apollo-operation-name", "pn_psn");

                        string productId = GetProducId(id);
                        string query = "{\"productId\":\"" + productId + "\",\"pageArgs\":{\"size\":48}}&extensions={\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"9ee5e2d58aa82800e95a444b08e8a3b116dc476b963513b3a74c362cdba2cecc\"}}";

                        string url = string.Format(UrlGetAddOnProductsByProductId, query);

                        HttpResponseMessage resp = httpClient.GetAsync(url).GetAwaiter().GetResult();
                        string response = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        _ = Serialization.TryFromJson(response, out PsnAddOnProducts psnAddOnProducts, out Exception ex);
                        if (ex != null)
                        {
                            throw ex;
                        }

                        var priceTitle = psnAddOnProducts?.Data?.AddOnProductsByProductIdRetrieve?.AddOnProducts
                            ?.Where(x => !x.Price.IsFree && x.Price.ServiceBranding == null)
                            ?.Select(x => x.Price.BasePrice)
                            ?.Distinct()
                            ?.ToList();
                        string buy = priceTitle?.FirstOrDefault();

                        psnAddOnProducts?.Data?.AddOnProductsByProductIdRetrieve?.AddOnProducts?.ForEach(x =>
                        {
                            DlcInfos dlc = new DlcInfos
                            {
                                Id = x.Id,
                                Name = x.Name,
                                Link = string.Format(UrlGameStore, x.Id),
                                Image = x.BoxArt.Url,
                                Description = string.Empty,
                                IsOwned = x.Price.BasePrice.IsEqual(buy),
                                Price = Regex.IsMatch(x.Price.DiscountedPrice, @"\d") ? x.Price.DiscountedPrice : "0",
                                PriceBase = Regex.IsMatch(x.Price.DiscountedPrice, @"\d") ? x.Price.BasePrice : "0"
                            };

                            dlcs.Add(dlc);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return dlcs;
        }

        private string GetProducId(string titleId)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            string productId = string.Empty;
            try
            {
                List<AccountTitlesResponseData.AccountTitlesRetrieve.Title> gamesAccountTitles = PsnClient.GetAccountTitles().GetAwaiter().GetResult();
                productId = gamesAccountTitles?.FirstOrDefault(x => x.titleId.IsEqual(titleId))?.productId;
                if (!productId.IsNullOrEmpty())
                {
                    return productId;
                }

                List<PlayedTitlesResponseData.PlayedTitlesRetrieve.Title> gamesPlayedTitles = PsnClient.GetPlayedTitles().GetAwaiter().GetResult();
                productId = gamesPlayedTitles?.FirstOrDefault(x => x.titleId.IsEqual(titleId))?.productId;
                if (!productId.IsNullOrEmpty())
                {
                    return productId;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return productId;
        }

        #endregion

        #region Games owned

        protected override ObservableCollection<GameDlcOwned> GetGamesDlcsOwned()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {

            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        #endregion

        #region PSN
        
        #endregion
    }
}