using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPlayniteShared.PluginLibrary.GogLibrary.Models;
using CommonPlayniteShared.PluginLibrary.Services.GogLibrary;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Gog.Models;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Gog
{
    // https://gogapidocs.readthedocs.io/en/latest/
    public class GogApi : StoreApi
    {
        #region Url
        private const string UrlBase = @"https://www.gog.com";

        private const string UrlUserData = UrlBase + @"/userData.json";
        private const string UrlUserOwned = "https://embed.gog.com/user/data/games";

        private const string UrlUserFriends = UrlBase + @"/u/{0}/friends";
        private const string UrlUserGames = UrlBase + @"/u/{0}/games/stats?page={1}";
        private const string UrlGogLang = UrlBase + @"/user/changeLanguage/{0}";

        private const string UrlGogGame = UrlBase + @"/game/{0}";

        private const string UrlFriends = @"https://embed.gog.com/users/info/{0}?expand=friendStatus";

        private static string UrlWishlist => UrlBase + @"/u/{0}/wishlist";
        #endregion

        #region Url API
        private static string UrlApiGamePlay => @"https://gameplay.gog.com";
        private static string UrlApi => @"https://api.gog.com";
        private static string UrlEmbed => @"https://embed.gog.com";

        private static string UrlApiGamePlayUserAchievements => UrlApiGamePlay + @"/clients/{0}/users/{1}/achievements";
        private static string UrlApiGamePlayFriendAchievements => UrlApiGamePlay + @"/clients/{0}/users/{1}/friends_achievements_unlock_progresses";

        private static string UrlApiGameInfo => UrlApi + @"/products/{0}?expand=description&locale={1}";

        private static string UrlApiPrice => UrlApi + @"/products/prices?ids={0}&countryCode={1}&currency={2}";

        private static string UrlApiWishlist => UrlEmbed + @"/user/wishlist.json";
        private static string UrlApiRemoveWishlist => UrlEmbed + @"/user/wishlist/remove/{0}";
        #endregion


        protected static readonly Lazy<GogAccountClient> gogAPI = new Lazy<GogAccountClient>(() => new GogAccountClient(API.Instance.WebViews.CreateOffscreenView()));
        internal static GogAccountClient GogAPI => gogAPI.Value;


        private string UserId { get; set; }
        private string UserName { get; set; }

        private static StoreCurrency LocalCurrency { get; set; } = new StoreCurrency { country = "US", currency = "USD", symbol = "$" };


        public GogApi(string PluginName) : base(PluginName, ExternalPlugin.GogLibrary, "GOG")
        {

        }

        #region Configuration
        protected override bool GetIsUserLoggedIn()
        {
            bool isLogged = GogAPI.GetIsUserLoggedIn();

            if (isLogged)
            {
                AccountBasicResponse AccountBasic = GogAPI.GetAccountInfo();
                AuthToken = new StoreToken
                {
                    Token = AccountBasic.accessToken
                };

                UserId = AccountBasic.userId;
                UserName = AccountBasic.username;
            }
            else
            {
                AuthToken = null;
            }

            return isLogged;
        }

        /// <summary>
        /// Set currency.
        /// </summary>
        /// <param name="currency"></param>
        public void SetCurrency(StoreCurrency currency)
        {
            LocalCurrency = currency;
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
                string WebData = Web.DownloadStringData(string.Format(UrlUserFriends, UserName), GetStoredCookies()).GetAwaiter().GetResult();
                if (WebData.IndexOf("window.profilesData.currentUser") == -1)
                {
                    IsUserLoggedIn = false;
                    Logger.Warn($"Not found: window.profilesData.currentUser");
                    return null;
                }

                string JsonDataString = Tools.GetJsonInString(WebData, "window.profilesData.currentUser = ", "window.profilesData.profileUser = ", "]}};");
                if (Serialization.TryFromJson(JsonDataString, out ProfileUser profileUser))
                {
                    string UserId = profileUser.userId;
                    string Avatar = profileUser.avatar.Replace("\\", string.Empty);
                    string Pseudo = profileUser.username;
                    string Link = string.Format(UrlUserFriends, profileUser.username);

                    AccountInfos userInfos = new AccountInfos
                    {
                        UserId = UserId,
                        Avatar = Avatar,
                        Pseudo = Pseudo,
                        Link = Link,
                        IsCurrent = true
                    };
                    return userInfos;
                }
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
                string WebData = Web.DownloadStringData(string.Format(UrlUserFriends, UserName), GetStoredCookies()).GetAwaiter().GetResult();
                if (WebData.IndexOf("window.profilesData.currentUser") == -1)
                {
                    IsUserLoggedIn = false;
                    Logger.Warn($"Not found: window.profilesData.profileUserFriends");
                    return null;
                }

                string JsonDataString = Tools.GetJsonInString(WebData, "window.profilesData.profileUserFriends = ", "window.profilesData.currentUserFriends = ", "}}];");
                Serialization.TryFromJson(JsonDataString, out List<ProfileUserFriends> profileUserFriends);

                if (profileUserFriends == null)
                {
                    return null;
                }

                ObservableCollection<AccountInfos> accountsInfos = new ObservableCollection<AccountInfos>();
                profileUserFriends.ForEach(x =>
                {
                    _ = DateTime.TryParse(x.date_accepted.date, out DateTime DateAdded);
                    string UserId = x.user.id;
                    string Avatar = x.user.avatar.Replace("\\", string.Empty);
                    string Pseudo = x.user.username;
                    string Link = string.Format(UrlUserFriends, Pseudo);

                    AccountInfos userInfos = new AccountInfos
                    {
                        DateAdded = DateAdded,
                        UserId = UserId,
                        Avatar = Avatar,
                        Pseudo = Pseudo,
                        Link = Link
                    }; 
                    accountsInfos.Add(userInfos);
                });

                return accountsInfos;
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
                ObservableCollection<AccountGameInfos> accountGamesInfos = new ObservableCollection<AccountGameInfos>();
                for (int idx = 1; idx < 50; idx++)
                {
                    try
                    {
                        string WebData = Web.DownloadStringData(string.Format(UrlUserGames, accountInfos.Pseudo, idx), GetStoredCookies()).GetAwaiter().GetResult();
                        Serialization.TryFromJson(WebData, out ProfileGames profileGames);

                        if (profileGames == null)
                        {
                            break;
                        }

                        profileGames?._embedded?.items?.ForEach(x =>
                        {
                            string Id = x.game.id;
                            string Name = x.game.title;

                            bool IsCommun = false;
                            if (!accountInfos.IsCurrent)
                            {
                                IsCommun = CurrentGamesInfos?.Where(y => y.Id.IsEqual(Id))?.Count() != 0;
                            }

                            long Playtime = 0;
                            foreach (dynamic data in (dynamic)x.stats)
                            {
                                long.TryParse(((dynamic)x.stats)[data.Path]["playtime"].ToString(), out Playtime);
                                Playtime *= 60;

                                if (Playtime != 0)
                                {
                                    break;
                                }
                            }

                            string Link = UrlBase + x.game.url.Replace("\\", string.Empty);
                            ObservableCollection<GameAchievement> Achievements = GetAchievements(Id, accountInfos);

                            AccountGameInfos accountGameInfos = new AccountGameInfos
                            {
                                Id = Id,
                                Name = Name,
                                Link = Link,
                                IsCommun = IsCommun,
                                Achievements = Achievements,
                                Playtime = Playtime
                            };
                            accountGamesInfos.Add(accountGameInfos);
                        });
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }
                }

                return accountGamesInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override ObservableCollection<GameAchievement> GetAchievements(string id, AccountInfos accountInfos)
        {
            return GetAchievements(id, accountInfos, false);
        }

        private ObservableCollection<GameAchievement> GetAchievements(string id, AccountInfos accountInfos, bool isRetry)
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                string Url = string.Empty;
                //if (accountInfos.IsCurrent)
                //{
                //    Url = string.Format(UrlApiGamePlayUserAchievements, Id, UserId);
                //}
                //else
                //{
                //    Url = string.Format(UrlApiGamePlayFriendAchievements, Id, UserId);
                //}
                Url = string.Format(UrlApiGamePlayUserAchievements, id, UserId);

                string urlLang = string.Format(UrlGogLang, CodeLang.GetGogLang(Local).ToLower());
                string webData = Web.DownloadStringData(Url, AuthToken.Token, urlLang).GetAwaiter().GetResult();

                ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
                if (!webData.IsNullOrEmpty())
                {
                    dynamic resultObj = Serialization.FromJson<dynamic>(webData);
                    try
                    {
                        dynamic resultItems = resultObj["items"];
                        if (resultItems.Count > 0)
                        {
                            for (int i = 0; i < resultItems.Count; i++)
                            {
                                GameAchievement gameAchievement = new GameAchievement
                                {
                                    Id = (string)resultItems[i]["achievement_key"],
                                    Name = (string)resultItems[i]["name"],
                                    Description = (string)resultItems[i]["description"],
                                    UrlUnlocked = (string)resultItems[i]["image_url_unlocked"],
                                    UrlLocked = (string)resultItems[i]["image_url_locked"],
                                    DateUnlocked = ((string)resultItems[i]["date_unlocked"] == null) ? default : (DateTime)resultItems[i]["date_unlocked"],
                                    Percent = (float)resultItems[i]["rarity"],
                                    GamerScore = CalcGamerScore((float)resultItems[i]["rarity"])
                                };
                                gameAchievements.Add(gameAchievement);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }
                }

                return gameAchievements;
            }
            catch (Exception ex)
            {
                // Reset login status when 401 error
                if (ex.Message.Contains("401") || ex.Message.Contains("access_denied"))
                {
                    if (isRetry)
                    {
                        Logger.Warn($"Error 401");
                        ResetIsUserLoggedIn();
                    }
                    else
                    {
                        Logger.Warn($"Error 401 - Wait and retry");
                        Thread.Sleep(5000);
                        return GetAchievements(id, accountInfos, true);
                    }
                }
                else
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }

            return null;
        }

        public override SourceLink GetAchievementsSourceLink(string name, string id, AccountInfos accountInfos)
        {
            return new SourceLink
            {
                GameName = name,
                Name = ClientName,
                Url = $"https://www.gog.com/u/{UserName}/game/{id}?sort=user_unlock_date&sort_user_id={accountInfos.UserId}"
            };
        }

        public override ObservableCollection<AccountWishlist> GetWishlist(AccountInfos accountInfos)
        {
            if (accountInfos != null)
            {
                string response;
                bool HasError = false;
                ObservableCollection<AccountWishlist> data = new ObservableCollection<AccountWishlist>();

                // With api
                if (accountInfos.IsCurrent)
                {
                    try
                    {
                        using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                        {
                            WebViewOffscreen.NavigateAndWait(UrlApiWishlist);
                            response = WebViewOffscreen.GetPageText();

                            if (Serialization.TryFromJson(response, out WishlistResult wishlistResult))
                            {
                                foreach (dynamic gameWishlist in wishlistResult.wishlist)
                                {
                                    if ((bool)gameWishlist.Value)
                                    {
                                        string StoreId = gameWishlist.Name;
                                        GameInfos gameInfos = GetGameInfos(StoreId, null);
                                        if (gameInfos != null)
                                        {
                                            data.Add(new AccountWishlist
                                            {
                                                Id = gameInfos.Id,
                                                Name = gameInfos.Name,
                                                Link = gameInfos.Link,
                                                Released = gameInfos.Released,
                                                Added = null,
                                                Image = gameInfos.Image
                                            });
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Logger.Warn($"GOG is disconnected");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                        HasError = true;
                    }
                }

                if (!HasError && data.Count > 0)
                {
                    return data;
                }

                // Without Api
                try
                {
                    using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                    {
                        WebViewOffscreen.NavigateAndWait(string.Format(UrlWishlist, accountInfos.Pseudo));
                        response = WebViewOffscreen.GetPageSource();

                        // Get game information for wishlist
                        if (!response.IsNullOrEmpty())
                        {
                            HtmlParser parser = new HtmlParser();
                            IHtmlDocument HtmlRequirement = parser.Parse(response);

                            foreach (var el in HtmlRequirement.QuerySelectorAll(".product-row-wrapper .product-state-holder"))
                            {
                                string StoreId = el.GetAttribute("gog-product");
                                GameInfos gameInfos = GetGameInfos(StoreId, null);
                                if (gameInfos != null)
                                {
                                    data.Add(new AccountWishlist
                                    {
                                        Id = gameInfos.Id,
                                        Name = gameInfos.Name,
                                        Link = gameInfos.Link,
                                        Released = gameInfos.Released,
                                        Added = null,
                                        Image = gameInfos.Image
                                    });
                                }
                            }
                        }

                        return data;
                    }
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
                    using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                    {
                        WebViewOffscreen.NavigateAndWait(string.Format(UrlApiRemoveWishlist, id));
                        return WebViewOffscreen.GetPageSource().ToLower().IndexOf("unable to remove product from wishlist") == -1;
                    }
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
        public override GameInfos GetGameInfos(string Id, AccountInfos accountInfos)
        {
            try
            {
                string Url = string.Format(UrlApiGameInfo, Id, CodeLang.GetGogLang(Local).ToLower());
                string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(WebData, out Models.ProductApiDetail productApiDetail);

                GameInfos gameInfos = new GameInfos
                {
                    Id = productApiDetail?.id.ToString(),
                    Name = productApiDetail?.title,
                    Link = productApiDetail?.links?.product_card,
                    Image = "https:" + productApiDetail?.images?.logo2x,
                    Description = RemoveDescriptionPromos(productApiDetail.description.full).Trim(),
                    Released = productApiDetail?.release_date
                };

                // DLC
                string stringDlcs = Serialization.ToJson(productApiDetail?.dlcs);
                if (!stringDlcs.IsNullOrEmpty() && !stringDlcs.IsEqual("[]"))
                {
                    GogDlcs DlcsData = Serialization.FromJson<GogDlcs>(stringDlcs);
                    ObservableCollection<DlcInfos> Dlcs = GetDlcInfos(DlcsData, accountInfos);
                    gameInfos.Dlcs = Dlcs;
                }

                return gameInfos;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return null;
        }

        public override ObservableCollection<DlcInfos> GetDlcInfos(string Id, AccountInfos accountInfos)
        {
            string Url = string.Format(UrlApiGameInfo, Id, CodeLang.GetGogLang(Local).ToLower());
            string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();
            Serialization.TryFromJson(WebData, out Models.ProductApiDetail productApiDetail);
            
            string stringDlcs = Serialization.ToJson(productApiDetail?.dlcs);
            if (!stringDlcs.IsNullOrEmpty() && !stringDlcs.IsEqual("[]"))
            {
                GogDlcs DlcsData = Serialization.FromJson<GogDlcs>(stringDlcs);
                return GetDlcInfos(DlcsData, accountInfos);
            }

            return null;
        }

        private ObservableCollection<DlcInfos> GetDlcInfos(GogDlcs DlcsData, AccountInfos accountInfos)
        {
            ObservableCollection<DlcInfos> Dlcs = new ObservableCollection<DlcInfos>();

            if (DlcsData?.products == null)
            {
                return Dlcs;
            }

            foreach (Product el in DlcsData?.products)
            {
                try
                {
                    string dataDlc = Web.DownloadStringData(string.Format(UrlApiGameInfo, el.id, CodeLang.GetGogLang(Local).ToLower())).GetAwaiter().GetResult();
                    if (!dataDlc.Contains("<!DOCTYPE html>", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Models.ProductApiDetail productApiDetailDlc = Serialization.FromJson<Models.ProductApiDetail>(dataDlc);

                        bool IsOwned = false;
                        if (accountInfos != null && accountInfos.IsCurrent)
                        {
                            IsOwned = IsDlcOwned(el.id.ToString());
                        }

                        DlcInfos dlc = new DlcInfos
                        {
                            Id = el.id.ToString(),
                            Name = productApiDetailDlc?.title,
                            Description = productApiDetailDlc?.description?.full,
                            Image = "https:" + productApiDetailDlc?.images?.logo2x,
                            Link = string.Format(UrlGogGame, productApiDetailDlc?.slug),
                            IsOwned = IsOwned
                        };

                        Dlcs.Add(dlc);
                    }
                    else
                    {
                        Logger.Warn($"No dlc data for {el.id}");
                    }
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("404"))
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }
                    else
                    {
                        Logger.Warn($"No dlc data for {el.id}");
                    }
                }
            }

            // Price
            if (Dlcs?.Count > 0)
            {
                try
                {
                    PriceData priceData = GetPrice(Dlcs.Select(x => x.Id).ToList(), Local, LocalCurrency);
                    string dataObjString = Serialization.ToJson(priceData?.dataObj["_embedded"]);
                    PriceResult priceResult = Serialization.FromJson<PriceResult>(dataObjString);

                    foreach (PriceItem el in priceResult?.items)
                    {
                        int idx = Dlcs.ToList().FindIndex(x => x.Id.IsEqual(el._embedded.product.id.ToString()));
                        if (idx > -1)
                        {
                            double.TryParse(el._embedded.prices[0].finalPrice.Replace(priceData.CodeCurrency, string.Empty, StringComparison.InvariantCultureIgnoreCase), out double Price);
                            double.TryParse(el._embedded.prices[0].basePrice.Replace(priceData.CodeCurrency, string.Empty, StringComparison.InvariantCultureIgnoreCase), out double PriceBase);

                            Price *= 0.01;
                            PriceBase *= 0.01;

                            Dlcs[idx].Price = Price + " " + priceData.SymbolCurrency;
                            Dlcs[idx].PriceBase = PriceBase + " " + priceData.SymbolCurrency;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }

            return Dlcs;
        }
        #endregion


        #region Games owned
        internal override ObservableCollection<GameDlcOwned> GetGamesDlcsOwned()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }
            
            try
            {
                ObservableCollection<GameDlcOwned> GamesDlcsOwned = new ObservableCollection<GameDlcOwned>();
                string data = Web.DownloadStringData(UrlUserOwned, AuthToken.Token).GetAwaiter().GetResult();
                UserDataOwned UserDataOwned = Serialization.FromJson<UserDataOwned>(data);

                UserDataOwned?.owned?.ForEach(x =>
                {
                    GamesDlcsOwned.Add(new GameDlcOwned { Id = x.ToString() });
                });
                return GamesDlcsOwned;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }
        #endregion


        #region GOG
        private PriceData GetPrice(List<string> ids, string Local, StoreCurrency LocalCurrency)
        {
            string joined = string.Join(",", ids);
            string UrlPrice = string.Format(UrlApiPrice, joined, LocalCurrency.country.ToUpper(), LocalCurrency.currency.ToUpper());
            string DataPrice = Web.DownloadStringData(UrlPrice).GetAwaiter().GetResult();

            Serialization.TryFromJson<dynamic>(DataPrice, out dynamic dataObj);

            string CodeCurrency = LocalCurrency.currency;
            string SymbolCurrency = LocalCurrency.symbol;

            // When no data or error, try with USD
            if (dataObj["message"] != null && ((string)dataObj["message"]).Contains("is not supported in", StringComparison.InvariantCultureIgnoreCase))
            {
                return GetPrice(ids, Local, new StoreCurrency { country = "US", currency = "USD", symbol = "$" });
            }

            if (dataObj["message"] != null)
            {
                Logger.Info($"{dataObj["message"]}");
                return null;
            }

            return new PriceData
            {
                dataObj = dataObj,
                CodeCurrency = CodeCurrency,
                SymbolCurrency = SymbolCurrency
            };
        }

        public List<StoreCurrency> GetCurrencies()
        {
            try
            {
                if (IsUserLoggedIn)
                {
                    string webData = Web.DownloadStringData(UrlUserData).GetAwaiter().GetResult();
                    Serialization.TryFromJson<UserData>(webData, out UserData userData);

                    if (userData?.currencies != null)
                    {
                        return userData.currencies.Select(x => new StoreCurrency { country = userData.country, currency = x.code.ToUpper(), symbol = x.symbol }).ToList();
                    }                    
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, ClientName);
            }

            Logger.Warn("Used USD only");
            return new List<StoreCurrency>
            {
                new StoreCurrency { country = "US", currency = "USD", symbol = "$" }
            };
        }

        internal string RemoveDescriptionPromos(string originalDescription)
        {
            if (originalDescription.IsNullOrEmpty())
            {
                return originalDescription;
            }

            // Get opening element in description. Promos are always at the start of description.
            // It has been seen that descriptions start with <a> or <div> elements
            var parser = new HtmlParser();
            var document = parser.Parse(originalDescription);
            var firstChild = document.Body.FirstChild;
            if (firstChild == null || firstChild.NodeType != NodeType.Element || !firstChild.HasChildNodes)
            {
                return originalDescription;
            }

            // It's possible to check if a description has a promo if the first element contains
            // a child img element with a src that points to know promo image url patterns
            var htmlElement = firstChild as IHtmlElement;
            var promoUrlsRegex = @"https:\/\/items.gog.com\/(promobanners|autumn|fall|summer|winter)\/";
            var containsPromoImage = htmlElement.QuerySelectorAll("img")
                        .Any(img => img.HasAttribute("src") && Regex.IsMatch(img.GetAttribute("src"), promoUrlsRegex, RegexOptions.IgnoreCase));
            if (!containsPromoImage)
            {
                return originalDescription;
            }

            // Remove all following <hr> and <br> elements that GOG adds after a promo
            var nextSibling = firstChild.NextSibling;
            while (nextSibling != null && (nextSibling is IHtmlHrElement || nextSibling is IHtmlBreakRowElement))
            {
                document.Body.RemoveChild(nextSibling);
                nextSibling = firstChild.NextSibling;
            }

            // Remove initial opening element and return description without promo
            document.Body.RemoveChild(firstChild);
            return document.Body.InnerHtml;
        }
        #endregion
    }
}
