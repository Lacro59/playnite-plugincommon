using CommonPluginsShared;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using CommonPluginsStores.Xbox.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.ObjectModel;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Xbox
{
    public class XboxApi : StoreApi
    {
        #region Urls

        private static string UrlBase => @"https://www.microsoft.com";

        #endregion

        #region Urls API

        private static string UrlApiWishlist => UrlBase + @"/msstoreapiprod/api/wishlist/details?locale={0}";
        private static string UrlApiWishlistShared => UrlApiWishlist + @"&wishlistId={1}";

        #endregion

        public XboxApi(string PluginName) : base(PluginName, ExternalPlugin.XboxLibrary, "Xbox")
        {
            
        }

        #region Configuration

        protected override bool GetIsUserLoggedIn()
        {
            return false;
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
            return null;
        }

        public override ObservableCollection<AccountWishlist> GetWishlist(AccountInfos accountInfos)
        {
            if (accountInfos != null && !accountInfos.Link.IsNullOrEmpty())
            {
                try
                {
                    ObservableCollection<AccountWishlist> data = new ObservableCollection<AccountWishlist>();
                    string wishlistId = accountInfos.Link.Split('=')[1];
                    string response = Web.DownloadStringData(string.Format(UrlApiWishlistShared, CodeLang.GetEpicLang(Locale), wishlistId)).GetAwaiter().GetResult();
                    _ = Serialization.TryFromJson(response, out Wishlists wishlists);

                    foreach (Product product in wishlists.Products)
                    {
                        data.Add(new AccountWishlist
                        {
                            Id = product.Id,
                            Name = product.Title,
                            Link = product.PdpUri,
                            Released = null,
                            Added = null,
                            Image = "https:" + product.Image.BaseUri
                        });
                    }

                    return data;
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
            return null;
        }

        #endregion

        #region Game owned

        protected override ObservableCollection<GameDlcOwned> GetGamesDlcsOwned()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                ObservableCollection<GameDlcOwned> GamesDlcsOwned = new ObservableCollection<GameDlcOwned>();
                return GamesDlcsOwned;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }

        #endregion

        #region Xbox
     
        #endregion
    }
}