using CommonPlayniteShared;
using CommonPlayniteShared.PluginLibrary.EpicLibrary;
using CommonPlayniteShared.PluginLibrary.EpicLibrary.Models;
using CommonPlayniteShared.PluginLibrary.EpicLibrary.Services;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsStores.Epic.Models;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores.Epic
{
    public class EpicApi : StoreApi
    {
        #region Url
        private const string UrlBase = @"https://www.epicgames.com";

        private string UrlStore = UrlBase + @"/store/{0}/p/{1}";


        private string UrlGraphQL = @"https://graphql.epicgames.com/graphql";
        #endregion


        #region Url API

        #endregion


        protected EpicAccountClient _EpicAPI;
        internal EpicAccountClient EpicAPI
        {
            get
            {
                if (_EpicAPI == null)
                {
                    _EpicAPI = new EpicAccountClient(
                        API.Instance,
                        PlaynitePaths.ExtensionsDataPath + "\\00000002-DBD1-46C6-B5D0-B1BA559D10E4\\tokens.json"
                    );
                }
                return _EpicAPI;
            }

            set => _EpicAPI = value;
        }


        public EpicApi() : base("Epic")
        {

        }


        #region Cookies
        internal override List<HttpCookie> GetWebCookies()
        {
            List<HttpCookie> httpCookies = WebViewOffscreen.GetCookies()?.Where(x => x?.Domain?.Contains("epic") ?? false)?.ToList() ?? new List<HttpCookie>();
            return httpCookies;
        }
        #endregion



        #region Configuration
        protected override bool GetIsUserLoggedIn()
        {
            bool isLogged = EpicAPI.GetIsUserLoggedIn();

            if (isLogged)
            {
                OauthResponse tokens = EpicAPI.loadTokens();
                AuthToken = new StoreToken
                {
                    Token = tokens.access_token,
                    Type = tokens.token_type
                };
            }
            else
            {
                AuthToken = null;
            }

            return isLogged;
        }
        #endregion


        #region Current user
        protected override AccountInfos GetCurrentAccountInfos()
        {
            try
            {
                AccountInfos accountInfos = new AccountInfos{ IsCurrent = true };
                return accountInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }

        protected override ObservableCollection<AccountInfos> GetCurrentFriendsInfos()
        {
            try
            {

            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }
        #endregion


        #region User details
        public override ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos)
        {
            try
            {
                ObservableCollection<AccountGameInfos> accountGamesInfos = new ObservableCollection<AccountGameInfos>();


                return accountGamesInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }

        public override ObservableCollection<GameAchievement> GetAchievements(string Id, AccountInfos accountInfos)
        {
            try
            { 
                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();


                return gameAchievements;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }
        #endregion


        #region Game
        public override GameInfos GetGameInfos(string Id, AccountInfos accountInfos)
        {
            try
            {

            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }

        public override ObservableCollection<DlcInfos> GetDlcInfos(string Id, AccountInfos accountInfos)
        {
            try
            {
                string LocalLang = CodeLang.GetEpicLang(Local);
                ObservableCollection<DlcInfos> Dlcs = new ObservableCollection<DlcInfos>();

                // List DLC
                EpicAddonsByNamespace dataDLC = GetAddonsByNamespace(Id).GetAwaiter().GetResult();
                if (dataDLC?.data?.Catalog?.catalogOffers?.elements == null)
                {
                    logger.Warn($"No dlc for {Id}");
                    return null;
                }

                foreach (Element el in dataDLC?.data?.Catalog?.catalogOffers?.elements)
                {
                    bool IsOwned = false;
                    if (accountInfos != null && accountInfos.IsCurrent)
                    {
                        IsOwned = DlcIsOwned(Id, el.id);
                    }
                    
                    DlcInfos dlc = new DlcInfos
                    {
                        Id = el.id,
                        Name = el.title,
                        Description = el.description,
                        Image = el.keyImages.Find(x => x.type.IsEqual("OfferImageWide")).url.Replace("\u002F", "/"),
                        Link = string.Format(UrlStore, LocalLang, el.urlSlug),
                        IsOwned = IsOwned,
                        Price = el.price.totalPrice.fmtPrice.discountPrice,
                        PriceBase = el.price.totalPrice.fmtPrice.originalPrice,
                    };

                    Dlcs.Add(dlc);
                }

                return Dlcs;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }
        #endregion


        #region Epic
        public string GetProductSlug(string Name)
        {
            string ProductSlug = string.Empty;
            using (WebStoreClient client = new WebStoreClient())
            {
                List<WebStoreModels.QuerySearchResponse.SearchStoreElement> catalogs = client.QuerySearch(Name).GetAwaiter().GetResult();
                if (catalogs.HasItems())
                {
                    catalogs = catalogs.OrderBy(x => x.title.Length).ToList();
                    WebStoreModels.QuerySearchResponse.SearchStoreElement catalog = catalogs.FirstOrDefault(a => a.title.IsEqual(Name, true));
                    if (catalog == null)
                    {
                        catalog = catalogs[0];
                    }

                    ProductSlug = catalog?.productSlug?.Replace("/home", string.Empty);
                }
            }
            return ProductSlug;
        }

        public string GetNameSpace(string Name)
        {
            string NameSpace = string.Empty;
            using (WebStoreClient client = new WebStoreClient())
            {
                List<WebStoreModels.QuerySearchResponse.SearchStoreElement> catalogs = client.QuerySearch(Name).GetAwaiter().GetResult();
                if (catalogs.HasItems())
                {
                    catalogs = catalogs.OrderBy(x => x.title.Length).ToList();
                    WebStoreModels.QuerySearchResponse.SearchStoreElement catalog = catalogs.FirstOrDefault(a => a.title.IsEqual(Name, true));
                    if (catalog == null)
                    {
                        catalog = catalogs[0];
                    }

                    NameSpace = catalog?.epicNamespace;
                }
            }
            return NameSpace;
        }


        private bool DlcIsOwned(string productNameSpace, string Id)
        {
            try
            {
                EpicEntitledOfferItems ownedDLC = GetEntitledOfferItems(productNameSpace, Id, AuthToken.Token).GetAwaiter().GetResult();
                return (ownedDLC?.data?.Launcher?.entitledOfferItems?.entitledToAllItemsInOffer ?? false) && (ownedDLC?.data?.Launcher?.entitledOfferItems?.entitledToAnyItemInOffer ?? false);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return false;
            }
        }


        private async Task<EpicAddonsByNamespace> GetAddonsByNamespace(string epic_namespace)
        {
            var query = new QueryAddonsByNamespace();
            query.variables.epic_namespace = epic_namespace;
            query.variables.locale = CodeLang.GetEpicLang(Local); ;
            query.variables.country = CodeLang.GetOriginLangCountry(Local);
            var content = new StringContent(Serialization.ToJson(query), Encoding.UTF8, "application/json");
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.PostAsync(UrlGraphQL, content);
            var str = await response.Content.ReadAsStringAsync();
            var data = Serialization.FromJson<EpicAddonsByNamespace>(str);
            return data;
        }

        private async Task<EpicEntitledOfferItems> GetEntitledOfferItems(string productNameSpace, string offerId, string token)
        {
            var query = new QueryEntitledOfferItems();
            query.variables.productNameSpace = productNameSpace;
            query.variables.offerId = offerId;
            var content = new StringContent(Serialization.ToJson(query), Encoding.UTF8, "application/json");
            string str = await Web.PostStringData(UrlGraphQL, token, content);
            var data = Serialization.FromJson<EpicEntitledOfferItems>(str);
            return data;
        }
        #endregion
    }
}
