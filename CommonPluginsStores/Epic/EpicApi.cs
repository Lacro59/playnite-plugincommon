using CommonPlayniteShared.Common;
using CommonPlayniteShared.PluginLibrary.EpicLibrary.Models;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores.Epic.Models;
using CommonPluginsStores.Epic.Models.Query;
using CommonPluginsStores.Epic.Models.Response;
using CommonPluginsStores.Models;
using CommonPluginsStores.Models.Enumerations;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.Epic
{
	/*
     * Playnite.SDK.Models.Game.GameId = CommonPluginsStores.Epic.Models.Assets.AppName
     */

	/// <summary>
	/// Provides access to the Epic Games Store API:
	/// authentication, library, playtime, achievements, wishlist, game info and DLC.
	/// <para>
	/// Methods are grouped by authentication requirement:
	/// <list type="bullet">
	///   <item><term>Anonymous (no token needed)</term>
	///     <description>
	///       <see cref="GetGameInfosAnonymous"/>, <see cref="GetDlcInfosAnonymous"/>,
	///       <see cref="GetGameRequirements"/>, <see cref="GetAchievementsSchema"/>,
	///       <see cref="GetNamespaceFromGameAnonymous"/>, <see cref="GetNamespaceFromSlug"/>,
	///       <see cref="ExtractSlugFromUrl"/>, <see cref="GetUrlSlugFromGameLinks"/>,
	///       <see cref="GetCatalogItem"/>, <see cref="GetProductSlug"/>.
	///     </description>
	///   </item>
	///   <item><term>Authenticated (token required)</term>
	///     <description>
	///       <see cref="GetAccountGamesInfos"/>, <see cref="GetAchievements"/>,
	///       <see cref="GetWishlist"/>, <see cref="RemoveWishlist"/>,
	///       <see cref="GetGameInfos"/>, <see cref="GetDlcInfos"/>,
	///       <see cref="GetAssets"/>, <see cref="GetCatalogs"/>,
	///       <see cref="GetPlaytimeItems"/>, <see cref="GetNamespaceFromGame"/>,
	///       <see cref="GetUrlSlugFromGame"/>, <see cref="GetProductIdFromGame"/>,
	///       <see cref="GetCatalogItemFromGame"/>.
	///     </description>
	///   </item>
	/// </list>
	/// </para>
	/// </summary>
	public class EpicApi : StoreApi
	{
		// Semaphore to ensure only one token refresh is performed at a time.
		private readonly SemaphoreSlim _tokenRefreshSemaphore = new SemaphoreSlim(1, 1);

		#region URLs

		private static string UrlBase => @"https://www.epicgames.com";
		private string UrlStore => UrlBase + @"/store/{0}/p/{1}";
		private string UrlAchievements => UrlBase + @"/store/{0}/achievements/{1}";
		private string UrlLogin => UrlBase + @"/id/login?responseType=code";
		private string UrlAuthCode => UrlBase + @"/id/api/redirect?clientId=34a02cf8f4414e29b15921876da36f9a&responseType=code";
		private string UrlGraphQL => @"https://launcher.store.epicgames.com/graphql";
		private string UrlApiServiceBase => @"https://account-public-service-prod03.ol.epicgames.com";
		private string UrlAccountAuth => UrlApiServiceBase + @"/account/api/oauth/token";
		private string UrlAccount => UrlApiServiceBase + @"/account/api/public/account/{0}";
		private string UrlStoreEpic => @"https://store.epicgames.com";
		private string UrlAccountProfile => UrlStoreEpic + @"/u/{0}";
		private string UrlAccountProfileUS => UrlStoreEpic + @"/en-US/u/{0}";
		private string UrlAccountLinkFriends => UrlStoreEpic + @"/u/{0}/friends";
		private string UrlAccountAchievements => UrlStoreEpic + @"/{0}/u/{1}/details/{2}";
		private string UrlApiFriendBase => @"https://friends-public-service-prod.ol.epicgames.com";
		private string UrlFriendsSummary => UrlApiFriendBase + @"/friends/api/v1/{0}/summary";
		private string UrlApiLibraryBase => @"https://library-service.live.use1a.on.epicgames.com";
		private string UrlPlaytimeAll => UrlApiLibraryBase + @"/library/api/public/playtime/account/{0}/all";
		private string UrlAsset => UrlApiLibraryBase + @"/library/api/public/items?includeMetadata=true&platform=Windows";
		private string UrlApiLauncherBase => @"https://launcher-public-service-prod06.ol.epicgames.com";
		private string UrlApiCatalog => @"https://catalog-public-service-prod06.ol.epicgames.com";
		private string UrlApiStoreContent => @"https://store-content-ipv4.ak.epicgames.com";
		// FIX: corrected malformed format string (removed extra closing braces).
		private string UrlApiGetProduct => UrlApiStoreContent + @"/api/{0}/content/products/{1}";

		#endregion

		#region Constants

		private static string UserAgent => @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) EpicGamesLauncher";

		/// <summary>Base64-encoded OAuth client credentials (clientId:clientSecret).</summary>
		private static string AuthEncodedString => "MzRhMDJjZjhmNDQxNGUyOWIxNTkyMTg3NmRhMzZmOWE6ZGFhZmJjY2M3Mzc3NDUwMzlkZmZlNTNkOTRmYzc2Y2Y=";

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of <see cref="EpicApi"/>.
		/// </summary>
		/// <param name="pluginName">Name of the host plugin (used for logging and notifications).</param>
		/// <param name="pluginLibrary">The enum value identifying the Epic library.</param>
		public EpicApi(string pluginName, ExternalPlugin pluginLibrary) : base(pluginName, pluginLibrary, "Epic")
		{
			CookiesDomains = new List<string> { ".epicgames.com", ".store.epicgames.com" };
		}

		#endregion

		#region Cookies

		/// <summary>
		/// Builds the set of HTTP cookies required to authenticate a WebView session
		/// against the Epic store front-end. Returns an empty list when no token is stored.
		/// </summary>
		protected override List<HttpCookie> GetWebCookies(bool deleteCookies = false, IWebView webView = null)
		{
			if (StoreToken == null)
			{
				Logger.Warn("GetWebCookies called but no tokens are available.");
				return new List<HttpCookie>();
			}

			List<HttpCookie> httpCookies = new List<HttpCookie>
			{
				new HttpCookie
				{
					Domain = ".store.epicgames.com",
					Name = "EPIC_EG1",
					Value = StoreToken.Token,
					Path = "/",
					Expires = StoreToken.ExpireAt,
					Creation = DateTime.Now,
					LastAccess = DateTime.Now,
					HttpOnly = false,
					Secure = false,
					SameSite = CookieSameSite.LaxMode,
					Priority = CookiePriority.Medium
				},
				new HttpCookie
				{
					Domain = ".store.epicgames.com",
					Name = "REFRESH_EPIC_EG1",
					Value = StoreToken.RefreshToken,
					Path = "/",
					Expires = StoreToken.RefreshExpireAt,
					Creation = DateTime.Now,
					LastAccess = DateTime.Now,
					HttpOnly = false,
					Secure = true,
					SameSite = CookieSameSite.NoRestriction,
					Priority = CookiePriority.Medium
				},
				new HttpCookie
				{
					Domain = ".store.epicgames.com",
					Name = "refreshTokenExpires",
					Value = StoreToken.RefreshExpireAt?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff").Replace(":", "%3A") + "Z",
					Path = "/",
					Expires = StoreToken.RefreshExpireAt,
					Creation = DateTime.Now,
					LastAccess = DateTime.Now,
					HttpOnly = false,
					Secure = false,
					SameSite = CookieSameSite.NoRestriction,
					Priority = CookiePriority.Medium
				},
				new HttpCookie
				{
					Domain = ".store.epicgames.com",
					Name = "storeTokenExpires",
					Value = StoreToken.RefreshExpireAt?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff").Replace(":", "%3A") + "Z",
					Path = "/",
					Expires = StoreToken.RefreshExpireAt,
					Creation = DateTime.Now,
					LastAccess = DateTime.Now,
					HttpOnly = false,
					Secure = false,
					SameSite = CookieSameSite.NoRestriction,
					Priority = CookiePriority.Medium
				}
			};

			return httpCookies;
		}

		/// <summary>Epic cookies are derived entirely from the stored token; no separate cookie store is used.</summary>
		protected override List<HttpCookie> GetStoredCookies() => GetWebCookies();

		/// <summary>Epic does not persist cookies independently — the token is the authoritative source.</summary>
		protected override bool SetStoredCookies(List<HttpCookie> httpCookies) => true;

		#endregion

		#region Configuration

		/// <summary>
		/// Determines whether the current user is authenticated.
		/// <para>
		/// For public accounts without forced auth, a valid stored token is sufficient.
		/// For private accounts or when <see cref="StoreSettings.UseAuth"/> is set,
		/// an active API call is made to verify the token.
		/// </para>
		/// </summary>
		/// <remarks>Does not require authentication to call; only requires it when the account is private.</remarks>
		protected override bool GetIsUserLoggedIn()
		{
			if (CurrentAccountInfos == null)
			{
				return false;
			}

			if (!CurrentAccountInfos.IsPrivate && !StoreSettings.UseAuth)
			{
				if (StoreToken == null)
				{
					StoreToken = GetStoredToken();
				}

				if (StoreToken != null)
				{
					CheckIsUserLoggedIn();
				}

				return !CurrentAccountInfos.UserId.IsNullOrEmpty();
			}

			bool isLogged = CheckIsUserLoggedIn();
			if (!isLogged)
			{
				StoreToken = null;
			}

			return isLogged;
		}

		/// <summary>
		/// Opens the Epic Games web login flow in a Playnite WebView, extracts the
		/// authorization code from the redirect, and exchanges it for an OAuth token.
		/// </summary>
		/// <remarks>Requires user interaction (opens a browser window).</remarks>
		public override void Login()
		{
			try
			{
				ResetIsUserLoggedIn();
				EpicLogin();
				SetUserAfterLogin();
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, false, PluginName);
			}
		}

		/// <summary>
		/// Presents the user with manual auth-code instructions (opens the Epic auth-code URL
		/// in the browser and prompts for the code via a dialog), then exchanges the code for a token.
		/// </summary>
		/// <remarks>Requires user interaction (opens browser + dialog).</remarks>
		public override void LoginAlternative()
		{
			try
			{
				ResetIsUserLoggedIn();
				EpicLoginAlternative();
				SetUserAfterLogin();
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, false, PluginName);
			}
		}

		/// <summary>
		/// Persists account information and cookies once a valid <see cref="StoreToken"/> has been obtained.
		/// </summary>
		private void SetUserAfterLogin()
		{
			if (StoreToken != null)
			{
				AccountInfos accountInfos = new AccountInfos
				{
					UserId = StoreToken.AccountId,
					Pseudo = StoreToken.AccountId.IsEqual(CurrentAccountInfos.UserId) ? CurrentAccountInfos.Pseudo : string.Empty,
					Link = string.Format(UrlAccountProfile, StoreToken.AccountId),
					IsPrivate = true,
					IsCurrent = true
				};

				CurrentAccountInfos = accountInfos;
				SaveCurrentUser();
				SetStoredCookies(GetWebCookies());
				Logger.Info($"{ClientName} logged");
			}
		}

		#endregion

		#region Current User

		/// <summary>
		/// Loads the persisted account info and asynchronously refreshes the public/private
		/// status and display name in the background.
		/// </summary>
		/// <returns>
		/// The cached <see cref="AccountInfos"/> if available; otherwise a blank current-user stub.
		/// </returns>
		protected override AccountInfos GetCurrentAccountInfos()
		{
			AccountInfos accountInfos = LoadCurrentUser();
			if (!accountInfos?.UserId?.IsNullOrEmpty() ?? false)
			{
				_ = Task.Run(async () =>
				{
					try
					{
						await Task.Delay(1000);
						CurrentAccountInfos.IsPrivate = !await CheckIsPublic(accountInfos);
						CurrentAccountInfos.AccountStatus = CurrentAccountInfos.IsPrivate ? AccountStatus.Private : AccountStatus.Public;

						if (CurrentAccountInfos.Pseudo.IsNullOrEmpty())
						{
							EpicAccountResponse epicAccountResponse = await GetAccountInfo(accountInfos.UserId);
							CurrentAccountInfos.Pseudo = epicAccountResponse?.DisplayName;
							SaveCurrentUser();
						}
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, true, PluginName);
					}
				});

				return accountInfos;
			}

			return new AccountInfos { IsCurrent = true };
		}

		/// <summary>
		/// Fetches the friends list for the current user in parallel and resolves
		/// display names via <see cref="GetAccountInfo"/>.
		/// </summary>
		/// <returns>
		/// An <see cref="ObservableCollection{AccountInfos}"/> of friend accounts,
		/// an empty collection when the friends endpoint returns no data,
		/// or null on error.
		/// </returns>
		/// <remarks>⚠️ Requires authentication (<see cref="IsUserLoggedIn"/> must be true).</remarks>
		protected override ObservableCollection<AccountInfos> GetCurrentFriendsInfos()
		{
			if (!IsUserLoggedIn)
			{
				return null;
			}

			try
			{
				ObservableCollection<AccountInfos> accountsInfos = new ObservableCollection<AccountInfos>();
				EpicFriendsSummary epicFriendsSummary = GetFriendsSummary().GetAwaiter().GetResult();

				if (epicFriendsSummary == null || epicFriendsSummary.Friends == null)
				{
					return new ObservableCollection<AccountInfos>();
				}

				var tasks = epicFriendsSummary.Friends.Select(async x =>
				{
					try
					{
						EpicAccountResponse epicAccountResponse = await GetAccountInfo(x.AccountId);
						string pseudo = epicAccountResponse?.DisplayName ?? x.AccountId ?? string.Empty;
						return new AccountInfos
						{
							DateAdded = null,
							UserId = x.AccountId,
							Avatar = string.Empty,
							Pseudo = pseudo,
							Link = string.Format(UrlAccountProfile, x.AccountId)
						};
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, true, PluginName);
						return new AccountInfos
						{
							DateAdded = null,
							UserId = x.AccountId,
							Avatar = string.Empty,
							Pseudo = x.AccountId ?? string.Empty,
							Link = string.Format(UrlAccountProfile, x.AccountId)
						};
					}
				}).ToList();

				var results = Task.WhenAll(tasks).GetAwaiter().GetResult();
				foreach (var userInfos in results)
				{
					accountsInfos.Add(userInfos);
				}

				return accountsInfos;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
			}

			return null;
		}

		#endregion

		#region User Details — Authenticated

		/// <summary>
		/// Builds the full game list for the current account, combining catalog data,
		/// playtime and achievements. Only supported for the current (authenticated) user.
		/// </summary>
		/// <param name="accountInfos">Must be the current user's account info.</param>
		/// <returns>An <see cref="ObservableCollection{AccountGameInfos}"/> or null on failure.</returns>
		/// <remarks>⚠️ Requires authentication.</remarks>
		public override ObservableCollection<AccountGameInfos> GetAccountGamesInfos(AccountInfos accountInfos)
		{
			if (!IsUserLoggedIn || !accountInfos.IsCurrent)
			{
				return null;
			}

			try
			{
				ObservableCollection<AccountGameInfos> accountGamesInfos = new ObservableCollection<AccountGameInfos>();
				var catalogs = GetCatalogs();
				List<PlaytimeItem> playtimeItems = GetPlaytimeItems();

				foreach (var catalog in catalogs)
				{
					try
					{
						bool isCommun = false;
						if (!accountInfos.IsCurrent)
						{
							isCommun = CurrentGamesInfos?.Where(y => y.Id.IsEqual(catalog.Id))?.Count() != 0;
						}

						string productSlug = GetProductSlug(catalog.Namespace);
						ObservableCollection<GameAchievement> achievements = GetAchievements(catalog.Namespace, accountInfos);

						AccountGameInfos agi = new AccountGameInfos
						{
							Id = catalog.Id,
							Name = catalog.Title.RemoveTrademarks(),
							Image = catalog.KeyImages?.First()?.Url,
							IsCommun = isCommun,
							Playtime = playtimeItems?.FirstOrDefault(x => x.ArtifactId == catalog.Id)?.TotalTime ?? 0,
							Achievements = achievements,
							Link = string.Empty,
							Released = null
						};

						accountGamesInfos.Add(agi);
					}
					catch (Exception ex)
					{
						Common.LogError(ex, false, false, PluginName);
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

		/// <summary>
		/// Returns the full achievement list for a game in the given sandbox/namespace,
		/// with each entry's <see cref="GameAchievement.DateUnlocked"/> populated
		/// from the player's profile when they have unlocked it.
		/// </summary>
		/// <param name="id">The Epic sandbox namespace (e.g. "fortnite").</param>
		/// <param name="accountInfos">Account whose unlock data is fetched.</param>
		/// <returns>
		/// An <see cref="ObservableCollection{GameAchievement}"/> (may be empty),
		/// or null when the user is not logged in or an error occurs.
		/// </returns>
		/// <remarks>⚠️ Requires authentication. The schema lookup itself (<see cref="GetAchievementsSchema"/>) is public.</remarks>
		public override ObservableCollection<GameAchievement> GetAchievements(string id, AccountInfos accountInfos)
		{
			if (!IsUserLoggedIn)
			{
				return null;
			}

			try
			{
				ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
				Tuple<string, ObservableCollection<GameAchievement>> data = GetAchievementsSchema(id);

				if (data?.Item2 == null || data.Item2.Count() == 0)
				{
					return gameAchievements;
				}

				string productId = data.Item1;
				gameAchievements = data.Item2;

				PlayerProfileAchievementsByProductIdResponse playerProfileAchievementsByProductId =
					QueryPlayerProfileAchievementsByProductId(accountInfos.UserId, productId).GetAwaiter().GetResult();

				playerProfileAchievementsByProductId?.Data?.PlayerProfile?.PlayerProfileInfo?.ProductAchievements?.Data?.PlayerAchievements?.ForEach(x =>
				{
					GameAchievement owned = gameAchievements.Where(y => y.Id.IsEqual(x.PlayerAchievement.AchievementName))?.FirstOrDefault();
					if (owned == null)
					{
						Logger.Warn($"Achievement not found: {x.PlayerAchievement.AchievementName} for productId: {productId}");
					}
					else if (x.PlayerAchievement.Unlocked)
					{
						owned.DateUnlocked = DateTime.ParseExact(
							x.PlayerAchievement.UnlockDate,
							"yyyy-MM-ddTHH:mm:ss.fffK",
							CultureInfo.InvariantCulture,
							DateTimeStyles.AdjustToUniversal);
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

		/// <summary>
		/// Builds the <see cref="SourceLink"/> that points to the achievement page
		/// of the given game on the Epic store.
		/// </summary>
		/// <param name="name">Display name of the game.</param>
		/// <param name="id">Product slug used in the achievements URL.</param>
		/// <param name="accountInfos">Account context (unused but required by the base signature).</param>
		/// <remarks>Does not require authentication.</remarks>
		public override SourceLink GetAchievementsSourceLink(string name, string id, AccountInfos accountInfos)
		{
			string localLang = CodeLang.GetEpicLang(Locale);
			string url = string.Format(UrlAchievements, localLang, id);
			return new SourceLink
			{
				GameName = name,
				Name = ClientName,
				Url = url
			};
		}

		/// <summary>
		/// Retrieves the authenticated user's Epic wishlist and maps each entry
		/// to an <see cref="AccountWishlist"/> item.
		/// </summary>
		/// <param name="accountInfos">Account for which the wishlist is fetched.</param>
		/// <returns>An <see cref="ObservableCollection{AccountWishlist}"/> or null on failure.</returns>
		/// <remarks>⚠️ Requires authentication.</remarks>
		public override ObservableCollection<AccountWishlist> GetWishlist(AccountInfos accountInfos)
		{
			if (accountInfos != null)
			{
				try
				{
					var wishlistResponse = QueryWishList().GetAwaiter().GetResult();
					if (wishlistResponse?.Data?.Wishlist?.WishlistItems?.Elements != null)
					{
						ObservableCollection<AccountWishlist> data = new ObservableCollection<AccountWishlist>();
						foreach (var gameWishlist in wishlistResponse.Data.Wishlist.WishlistItems.Elements)
						{
							string id = string.Empty;
							string name = string.Empty;
							DateTime? released = null;
							DateTime? added = null;
							string image = string.Empty;
							string link = string.Empty;

							try
							{
								id = gameWishlist.OfferId + "|" + gameWishlist.Namespace;
								name = WebUtility.HtmlDecode(gameWishlist.Offer.Title);
								image = gameWishlist.Offer.KeyImages?.FirstOrDefault(x => x.Type.IsEqual("Thumbnail"))?.Url;
								released = gameWishlist.Offer.EffectiveDate.ToUniversalTime();
								added = gameWishlist.Created.ToUniversalTime();
								link = gameWishlist.Offer?.CatalogNs?.Mappings?.FirstOrDefault()?.PageSlug;

								data.Add(new AccountWishlist
								{
									Id = id,
									Name = name,
									Link = link.IsNullOrEmpty() ? string.Empty : string.Format(UrlStore, CodeLang.GetEpicLang(Locale), link),
									Released = released,
									Added = added,
									Image = image
								});
							}
							catch (Exception ex)
							{
								Common.LogError(ex, true, $"Error in parse {ClientName} wishlist - {name}");
							}
						}

						return data;
					}
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, $"Error in parse {ClientName} wishlist", true, PluginName);
					return null;
				}
			}

			return null;
		}

		/// <summary>
		/// Removes a game from the wishlist.
		/// </summary>
		/// <param name="id">Composite ID in the format offerId|namespace.</param>
		/// <returns>Always false — not yet implemented.</returns>
		/// <remarks>
		/// ⚠️ Requires authentication.
		/// The GraphQL mutation body is present but commented out pending validation.
		/// This method is a stub and will always return false.
		/// </remarks>
		// TODO: Rewrite — implementation is disabled pending API validation.
		public override bool RemoveWishlist(string id)
		{
			if (IsUserLoggedIn)
			{
				/*
                try
                {
                    string epicOfferId = id.Split('|')[0];
                    string epicNamespace = id.Split('|')[1];
                    string query = @"mutation removeFromWishlistMutation($namespace: String!, $offerId: String!, $operation: RemoveOperation!) {
                        Wishlist {
                            removeFromWishlist(namespace: $namespace, offerId: $offerId, operation: $operation) { success }
                        }
                    }";
                    dynamic variables = new { @namespace = epicNamespace, offerId = epicOfferId, operation = "REMOVE" };
                    string response = QueryWishList(query, variables).GetAwaiter().GetResult();
                    return response.IndexOf("\"success\":true") > -1;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error remove {id} in {ClientName} wishlist", true, PluginName);
                }
                */
			}

			return false;
		}

		#endregion

		#region Game — Authenticated

		/// <summary>
		/// Retrieves detailed information for a game identified by its Epic namespace.
		/// Also populates the DLC list via <see cref="GetDlcInfos"/>.
		/// Populates <see cref="GameInfos.Id2"/> (AppName) from the authenticated asset list.
		/// </summary>
		/// <param name="id">The Epic sandbox namespace of the game.</param>
		/// <param name="accountInfos">Account context used to determine DLC ownership.</param>
		/// <returns>A populated <see cref="GameInfos"/> object, or null on failure.</returns>
		/// <remarks>
		/// ⚠️ Requires authentication (calls <see cref="GetAssets"/>).
		/// For an unauthenticated alternative (without <c>Id2</c>), use <see cref="GetGameInfosAnonymous"/>.
		/// </remarks>
		// TODO: Must be tested in production before relying on this method.
		public override GameInfos GetGameInfos(string id, AccountInfos accountInfos)
		{
			try
			{
				var assets = GetAssets();
				var asset = assets.FirstOrDefault(x => x.Namespace.IsEqual(id));
				if (asset == null)
				{
					Logger.Warn($"No asset for {id}");
					return null;
				}

				AddonsByNamespaceResponse addonsByNamespaceResponse =
					QueryAddonsByNamespace(id, "games/edition/base").GetAwaiter().GetResult();
				CatalogOffer catalogOffer =
					addonsByNamespaceResponse?.Data?.Catalog?.CatalogOffers?.Elements?.FirstOrDefault();

				if (catalogOffer == null)
				{
					Logger.Warn($"No game for {id}");
					return null;
				}

				string localLang = CodeLang.GetEpicLang(Locale);
				GameInfos gameInfos = new GameInfos
				{
					Id = catalogOffer.Id,
					Id2 = asset.AppName,
					Name = catalogOffer.Title,
					// FIX: was hardcoded to "the-escapists"; now uses the proper store URL format.
					Link = string.Format(UrlStore, localLang, catalogOffer.ProductSlug),
					Image = catalogOffer.KeyImages?.Find(x => x.Type.IsEqual("OfferImageWide"))?.Url?.Replace("\u002F", "/"),
					Description = catalogOffer.Description.Trim(),
					Released = catalogOffer.ReleaseDate
				};

				gameInfos.Dlcs = GetDlcInfos(id, accountInfos);
				return gameInfos;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
			}

			return null;
		}

		/// <summary>
		/// Returns the DLC list for a game, including pricing and ownership status
		/// for the current user.
		/// </summary>
		/// <param name="id">The Epic sandbox namespace of the parent game.</param>
		/// <param name="accountInfos">Account context used for ownership checks.</param>
		/// <returns>An <see cref="ObservableCollection{DlcInfos}"/> or null when no DLC is found or on error.</returns>
		/// <remarks>
		/// ⚠️ Requires authentication for ownership checks (<see cref="DlcIsOwned"/>).
		/// For an anonymous alternative (ownership always false), use <see cref="GetDlcInfosAnonymous"/>.
		/// </remarks>
		// TODO: Must be tested in production before relying on this method.
		public override ObservableCollection<DlcInfos> GetDlcInfos(string id, AccountInfos accountInfos)
		{
			try
			{
				string localLang = CodeLang.GetEpicLang(Locale);
				ObservableCollection<DlcInfos> dlcs = new ObservableCollection<DlcInfos>();
				AddonsByNamespaceResponse addonsByNamespaceResponse = QueryAddonsByNamespace(id).GetAwaiter().GetResult();

				if (addonsByNamespaceResponse?.Data?.Catalog?.CatalogOffers?.Elements == null)
				{
					Logger.Warn($"No dlc for {id}");
					return null;
				}

				foreach (var el in addonsByNamespaceResponse.Data.Catalog.CatalogOffers.Elements)
				{
					bool isOwned = false;
					if (accountInfos != null && accountInfos.IsCurrent)
					{
						isOwned = DlcIsOwned(id, el.Id);
					}

					DlcInfos dlc = new DlcInfos
					{
						Id = el.Id,
						Name = el.Title,
						Description = el.Description,
						Image = el.KeyImages?.Find(x => x.Type.IsEqual("OfferImageWide"))?.Url?.Replace("\u002F", "/"),
						Link = string.Format(UrlStore, localLang, el.UrlSlug),
						IsOwned = isOwned,
						Price = el.Price?.TotalPrice?.FmtPrice?.DiscountPrice,
						PriceBase = el.Price?.TotalPrice?.FmtPrice?.OriginalPrice
					};

					dlcs.Add(dlc);
				}

				return dlcs;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
			}

			return null;
		}

		#endregion

		#region Game — Anonymous (no authentication required)

		/// <summary>
		/// Returns game information using only public Epic APIs — no authentication required.
		/// Skips the <c>Id2</c> (AppName) field which requires an authenticated asset lookup.
		/// </summary>
		/// <remarks>
		/// For a fully populated <see cref="GameInfos"/> including <c>Id2</c>,
		/// use the authenticated <see cref="GetGameInfos"/> override instead.
		/// </remarks>
		/// <param name="namespace">The Epic sandbox namespace of the game.</param>
		/// <returns>A <see cref="GameInfos"/> object (without <c>Id2</c>), or null on failure.</returns>
		public GameInfos GetGameInfosAnonymous(string @namespace)
		{
			if (@namespace.IsNullOrEmpty())
			{
				Logger.Warn("EpicApi.GetGameInfosAnonymous: namespace is null or empty.");
				return null;
			}

			try
			{
				AddonsByNamespaceResponse response =
					QueryAddonsByNamespace(@namespace, "games/edition/base").GetAwaiter().GetResult();
				CatalogOffer catalogOffer = response?.Data?.Catalog?.CatalogOffers?.Elements?.FirstOrDefault();

				if (catalogOffer == null)
				{
					Logger.Warn($"EpicApi.GetGameInfosAnonymous: No catalog offer found for namespace '{@namespace}'.");
					return null;
				}

				string localLang = CodeLang.GetEpicLang(Locale);
				return new GameInfos
				{
					Id = catalogOffer.Id,
					Id2 = string.Empty, // AppName unavailable without authentication.
					Name = catalogOffer.Title,
					Link = string.Format(UrlStore, localLang, catalogOffer.ProductSlug),
					Image = catalogOffer.KeyImages?.Find(x => x.Type.IsEqual("OfferImageWide"))?.Url?.Replace("\u002F", "/"),
					Description = catalogOffer.Description?.Trim(),
					Released = catalogOffer.ReleaseDate
				};
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return null;
			}
		}

		/// <summary>
		/// Returns the DLC list for a game using only public Epic APIs.
		/// Ownership status (<see cref="DlcInfos.IsOwned"/>) is always <c>false</c>
		/// since entitlement checks require authentication.
		/// </summary>
		/// <param name="namespace">The Epic sandbox namespace of the parent game.</param>
		/// <returns>An <see cref="ObservableCollection{DlcInfos}"/> or null when no DLC is found.</returns>
		public ObservableCollection<DlcInfos> GetDlcInfosAnonymous(string @namespace)
		{
			if (@namespace.IsNullOrEmpty())
			{
				Logger.Warn("EpicApi.GetDlcInfosAnonymous: namespace is null or empty.");
				return null;
			}

			try
			{
				string localLang = CodeLang.GetEpicLang(Locale);
				AddonsByNamespaceResponse response = QueryAddonsByNamespace(@namespace).GetAwaiter().GetResult();

				if (response?.Data?.Catalog?.CatalogOffers?.Elements == null)
				{
					Logger.Warn($"EpicApi.GetDlcInfosAnonymous: No DLC found for namespace '{@namespace}'.");
					return null;
				}

				ObservableCollection<DlcInfos> dlcs = new ObservableCollection<DlcInfos>();
				foreach (CatalogOffer el in response.Data.Catalog.CatalogOffers.Elements)
				{
					dlcs.Add(new DlcInfos
					{
						Id = el.Id,
						Name = el.Title,
						Description = el.Description,
						Image = el.KeyImages?.Find(x => x.Type.IsEqual("OfferImageWide"))?.Url?.Replace("\u002F", "/"),
						Link = string.Format(UrlStore, localLang, el.UrlSlug),
						IsOwned = false, // entitlement check requires authentication.
						Price = el.Price?.TotalPrice?.FmtPrice?.DiscountPrice,
						PriceBase = el.Price?.TotalPrice?.FmtPrice?.OriginalPrice
					});
				}

				return dlcs;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return null;
			}
		}

		/// <summary>
		/// Builds a <see cref="GameRequirements"/> from the Epic store product page for the given slug.
		/// Returns null when the product cannot be fetched or contains no PC requirements.
		/// </summary>
		/// <param name="slug">
		/// Epic store product slug as it appears in the store URL
		/// (e.g. "the-escapists" from epicgames.com/store/…/p/the-escapists).
		/// </param>
		/// <remarks>Does not require authentication — uses public store content API.</remarks>
		public GameRequirements GetGameRequirements(string slug)
		{
			if (slug.IsNullOrEmpty())
			{
				Logger.Warn("EpicApi.GetGameRequirements: slug is null or empty.");
				return null;
			}

			try
			{
				Product product = GetProduct(slug);
				if (product == null)
				{
					Logger.Warn($"EpicApi.GetGameRequirements: No product for slug '{slug}'.");
					return null;
				}

				Page requirementsPage =
					product.Pages?.FirstOrDefault(p => p.TemplateName.IsEqual("productHome") && p.Data?.Requirements != null)
					?? product.Pages?.FirstOrDefault(p => p.Data?.Requirements != null);

				Requirements epicRequirements = requirementsPage?.Data?.Requirements;
				Epic.Models.System windowsSystem = epicRequirements?.Systems?.FirstOrDefault(
					s => string.Equals(s.SystemType, "Windows", StringComparison.OrdinalIgnoreCase));

				if (windowsSystem?.Details == null || windowsSystem.Details.Count == 0)
				{
					Logger.Warn($"EpicApi.GetGameRequirements: No Windows requirements found for slug '{slug}'.");
					return null;
				}

				string storeUrl = string.Format(UrlStore, CodeLang.GetEpicLang(Locale), slug);
				GameRequirements result = new GameRequirements
				{
					Id = slug,
					GameName = product.Title ?? string.Empty,
					SourceLink = new SourceLink
					{
						Name = ClientName,
						GameName = product.Title ?? string.Empty,
						Url = storeUrl
					}
				};

				foreach (Detail detail in windowsSystem.Details)
				{
					MapDetailToRequirements(detail, result.Minimum, result.Recommended);
				}

				return result;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return null;
			}
		}

		/// <summary>
		/// Returns the achievements schema (definition + rarity) for the given sandbox namespace.
		/// Results are cached to disk for 24 hours (1440 minutes).
		/// </summary>
		/// <param name="namespace">The Epic sandbox namespace that identifies the game's achievement set.</param>
		/// <returns>
		/// A <see cref="Tuple{String, ObservableCollection}"/> where Item1 is the Epic productId and
		/// Item2 is the collection of <see cref="GameAchievement"/> definitions.
		/// Returns a tuple with an empty string and an empty collection when no data is available.
		/// </returns>
		/// <remarks>Does not require authentication — uses public GraphQL endpoint.</remarks>
		public override Tuple<string, ObservableCollection<GameAchievement>> GetAchievementsSchema(string @namespace)
		{
			string cacheFile = Path.Combine(PathAchievementsData, $"{@namespace}.json");
			Tuple<string, ObservableCollection<GameAchievement>> data =
				FileDataService.LoadData<Tuple<string, ObservableCollection<GameAchievement>>>(cacheFile, 1440);

			if (data?.Item2 == null || data.Item2.Count() == 0)
			{
				ObservableCollection<GameAchievement> gameAchievements = new ObservableCollection<GameAchievement>();
				var achievementResponse = QueryAchievement(@namespace).GetAwaiter().GetResult();

				if (achievementResponse == null)
				{
					Logger.Warn($"EpicApi.GetAchievementsSchema: QueryAchievement returned null for {@namespace}");
					return new Tuple<string, ObservableCollection<GameAchievement>>(string.Empty, gameAchievements);
				}

				string productId = achievementResponse.Data?.Achievement?.ProductAchievementsRecordBySandbox?.ProductId;
				achievementResponse?.Data?.Achievement?.ProductAchievementsRecordBySandbox?.Achievements?.ForEach(x =>
				{
					GameAchievement gameAchievement = new GameAchievement
					{
						Id = x.Achievement.Name,
						Name = x.Achievement.UnlockedDisplayName.Trim(),
						Description = x.Achievement.UnlockedDescription.Trim(),
						UrlUnlocked = x.Achievement.UnlockedIconLink,
						UrlLocked = x.Achievement.LockedIconLink,
						DateUnlocked = default,
						Percent = x.Achievement.Rarity.Percent,
						GamerScore = x.Achievement.XP
					};
					gameAchievements.Add(gameAchievement);
				});

				data = new Tuple<string, ObservableCollection<GameAchievement>>(productId, gameAchievements);
				FileDataService.SaveData(cacheFile, data);
			}

			return data;
		}

		#endregion

		#region Epic Authentication

		/// <summary>
		/// Exchanges an OAuth authorization code for an EG1 token pair
		/// and stores the result via <see cref="SetToken"/>.
		/// </summary>
		/// <param name="authorizationCode">The one-time authorization code returned by Epic's OAuth redirect.</param>
		private void AuthenticateUsingAuthCode(string authorizationCode)
		{
			StoreToken = null;
			using (HttpClient httpClient = new HttpClient())
			{
				httpClient.DefaultRequestHeaders.Clear();
				httpClient.DefaultRequestHeaders.Add("Authorization", "basic " + AuthEncodedString);
				using (StringContent content = new StringContent($"grant_type=authorization_code&code={authorizationCode}&token_type=eg1"))
				{
					content.Headers.Clear();
					content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
					HttpResponseMessage response = httpClient.PostAsync(UrlAccountAuth, content).GetAwaiter().GetResult();
					string respContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
					SetToken(respContent);
				}
			}
		}

		/// <summary>
		/// Checks whether the Epic store profile of <paramref name="accountInfos"/> is public
		/// by downloading the en-US profile page and looking for the "unavailable" marker.
		/// </summary>
		/// <param name="accountInfos">The account whose profile visibility is checked.</param>
		/// <returns>
		/// true if the profile page does not contain the private-profile marker;
		/// false if it is private, the account is null, or an error occurs.
		/// </returns>
		/// <remarks>Does not require authentication.</remarks>
		public async Task<bool> CheckIsPublic(AccountInfos accountInfos)
		{
			if (accountInfos == null || accountInfos.UserId.IsNullOrEmpty())
			{
				Logger.Info("[EpicApi] CheckIsPublic: accountInfos or UserId is null.");
				return false;
			}

			try
			{
				accountInfos.AccountStatus = AccountStatus.Checking;
				string url = string.Format(UrlAccountProfileUS, accountInfos.UserId);
				var pageSource = await Web.DownloadSourceDataWebView(url);
				string source = pageSource.Item1;
				return !source.Contains("This profile is unavailable", StringComparison.OrdinalIgnoreCase);
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
			}

			return false;
		}

		/// <summary>
		/// Validates the stored token against the Epic account API.
		/// On failure, attempts a token refresh using the stored refresh token.
		/// Also populates <see cref="AccountInfos.Pseudo"/> when missing.
		/// </summary>
		/// <returns>true when a valid, confirmed token is in place; otherwise false.</returns>
		private bool CheckIsUserLoggedIn()
		{
			if (StoreToken == null)
			{
				return false;
			}

			try
			{
				EpicAccountResponse account = GetAccountInfo(StoreToken.AccountId).GetAwaiter().GetResult();
				if (account == null)
				{
					string refreshToken = StoreToken?.RefreshToken;
					if (string.IsNullOrEmpty(refreshToken))
					{
						return false;
					}

					_tokenRefreshSemaphore.Wait();
					try
					{
						if (StoreToken == null || StoreToken.Token.IsNullOrEmpty() || StoreToken.RefreshToken == refreshToken)
						{
							RenewTokens(refreshToken);
						}
					}
					finally
					{
						_tokenRefreshSemaphore.Release();
					}

					if (StoreToken == null || StoreToken.AccountId.IsNullOrEmpty() || StoreToken.Token.IsNullOrEmpty())
					{
						return false;
					}

					account = GetEpicAccount();
				}

				if (CurrentAccountInfos.Pseudo.IsNullOrEmpty() && account.Id.IsEqual(StoreToken.AccountId))
				{
					CurrentAccountInfos.Pseudo = account?.DisplayName;
					CurrentAccountInfos.Link = string.Format(UrlAccountProfile, account.Id);
				}

				return account != null && account.Id.IsEqual(StoreToken.AccountId);
			}
			catch
			{
				try
				{
					Logger.Warn("Retry CheckIsUserLoggedIn()");
					string refreshToken = StoreToken?.RefreshToken;
					if (string.IsNullOrEmpty(refreshToken))
					{
						return false;
					}

					_tokenRefreshSemaphore.Wait();
					try
					{
						if (StoreToken == null || StoreToken.Token.IsNullOrEmpty() || StoreToken.RefreshToken == refreshToken)
						{
							RenewTokens(refreshToken);
						}
					}
					finally
					{
						_tokenRefreshSemaphore.Release();
					}

					if (StoreToken == null || StoreToken.AccountId.IsNullOrEmpty() || StoreToken.Token.IsNullOrEmpty())
					{
						return false;
					}

					EpicAccountResponse account = GetAccountInfo(StoreToken.AccountId).GetAwaiter().GetResult();
					if (CurrentAccountInfos.Pseudo.IsNullOrEmpty() && account.Id.IsEqual(StoreToken.AccountId))
					{
						CurrentAccountInfos.Pseudo = account?.DisplayName;
						CurrentAccountInfos.Link = string.Format(UrlAccountProfile, account.Id);
					}

					return account != null && account.Id.IsEqual(StoreToken.AccountId);
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, "Failed to validation Epic authentication.", false, PluginName);
					return false;
				}
			}
		}

		/// <summary>
		/// Opens a Playnite WebView to the Epic login page, intercepts the OAuth redirect
		/// and extracts the authorization code from the callback URL.
		/// </summary>
		private void EpicLogin()
		{
			var loggedIn = false;
			var authorizationCode = string.Empty;

			using (IWebView webView = API.Instance.WebViews.CreateView(new WebViewSettings
			{
				WindowWidth = 580,
				WindowHeight = 700,
				UserAgent = UserAgent
			}))
			{
				webView.LoadingChanged += async (s, e) =>
				{
					var pageText = await webView.GetPageTextAsync();
					if (!pageText.IsNullOrEmpty() && pageText.Contains(@"localhost") && !e.IsLoading)
					{
						var source = await webView.GetPageSourceAsync();
						var matches = Regex.Matches(source, @"localhost\/launcher\/authorized\?code=([a-zA-Z0-9]+)", RegexOptions.IgnoreCase);
						if (matches.Count > 0)
						{
							authorizationCode = matches[0].Groups[1].Value;
							loggedIn = true;
						}

						webView.Close();
					}
				};

				CookiesDomains.ForEach(x => { webView.DeleteDomainCookies(x); });
				webView.Navigate(UrlLogin);
				_ = webView.OpenDialog();
			}

			if (!loggedIn)
			{
				return;
			}

			if (string.IsNullOrEmpty(authorizationCode))
			{
				Logger.Error("Failed to get login exchange key for Epic account.");
				return;
			}

			AuthenticateUsingAuthCode(authorizationCode);
		}

		/// <summary>
		/// Guides the user through the manual auth-code flow:
		/// opens the Epic authorization URL in the default browser and
		/// prompts the user to paste the resulting code into a dialog.
		/// </summary>
		private void EpicLoginAlternative()
		{
			_ = API.Instance.Dialogs.ShowMessage(
				ResourceProvider.GetString("LOCEpicAlternativeAuthInstructions"), "",
				System.Windows.MessageBoxButton.OK,
				System.Windows.MessageBoxImage.None);

			_ = ProcessStarter.StartUrl(UrlAuthCode);
			StringSelectionDialogResult res = API.Instance.Dialogs.SelectString("LOCEpicAuthCodeInputMessage", "", "");

			if (!res.Result || res.SelectedString.IsNullOrWhiteSpace())
			{
				return;
			}

			AuthenticateUsingAuthCode(res.SelectedString.Trim().Trim('"'));
		}

		/// <summary>
		/// Uses the stored refresh token to obtain a new EG1 access/refresh token pair.
		/// The result is persisted via <see cref="SetToken"/>.
		/// </summary>
		/// <param name="refreshToken">The current refresh token.</param>
		private void RenewTokens(string refreshToken)
		{
			using (HttpClient httpClient = new HttpClient())
			{
				httpClient.DefaultRequestHeaders.Clear();
				httpClient.DefaultRequestHeaders.Add("Authorization", "basic " + AuthEncodedString);
				using (StringContent content = new StringContent($"grant_type=refresh_token&refresh_token={refreshToken}&token_type=eg1"))
				{
					content.Headers.Clear();
					content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
					HttpResponseMessage response = httpClient.PostAsync(UrlAccountAuth, content).GetAwaiter().GetResult();
					string respContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
					SetToken(respContent);
				}
			}
		}

		/// <summary>
		/// Deserializes an OAuth JSON response and stores the resulting token in
		/// <see cref="StoreToken"/>. Persists it via <see cref="SetStoredToken"/>.
		/// </summary>
		/// <param name="respContent">Raw JSON body of the OAuth token endpoint response.</param>
		private void SetToken(string respContent)
		{
			if (Serialization.TryFromJson(respContent, out OauthResponse oauthResponse, out Exception exception))
			{
				StoreToken = new StoreToken
				{
					AccountId = oauthResponse.account_id,
					Type = oauthResponse.token_type,
					Token = oauthResponse.access_token,
					ExpireAt = oauthResponse.expires_at,
					RefreshToken = oauthResponse.refresh_token,
					RefreshExpireAt = oauthResponse.refresh_expires_at
				};
				SetStoredToken(StoreToken);
			}
			else if (exception != null)
			{
				Common.LogError(exception, false, false, PluginName);
			}
		}

		/// <summary>
		/// Convenience wrapper: fetches the Epic account record for the currently stored token's account ID.
		/// </summary>
		/// <returns><see cref="EpicAccountResponse"/> or null when no token is available.</returns>
		private EpicAccountResponse GetEpicAccount()
		{
			if (StoreToken != null)
			{
				return GetAccountInfo(StoreToken.AccountId).GetAwaiter().GetResult();
			}

			return null;
		}

		#endregion

		#region Epic Library — Authenticated

		/// <summary>
		/// Builds the filtered game catalog for the current account.
		/// Excludes Unreal Engine assets, private sandboxes, digital extras, standalone plugins,
		/// and (optionally) third-party managed titles.
		/// </summary>
		/// <returns>A <see cref="List{CatalogItem}"/> containing playable game entries.</returns>
		/// <remarks>⚠️ Requires authentication (calls <see cref="GetAssets"/>).</remarks>
		public List<CatalogItem> GetCatalogs()
		{
			List<CatalogItem> catalogItems = new List<CatalogItem>();
			List<Asset> assets = GetAssets();

			foreach (var gameAsset in assets.Where(a => a.Namespace != "ue" && a.SandboxType != "PRIVATE" && !a.AppName.IsNullOrEmpty()))
			{
				try
				{
					CatalogItem catalogItem = GetCatalogItem(gameAsset.Namespace, gameAsset.CatalogItemId);
					if (catalogItem?.Categories?.Any(a => a.Path == "applications") != true)
					{
						continue;
					}

					if ((catalogItem?.MainGameItem != null) && (catalogItem.Categories?.Any(a => a.Path == "addons/launchable") == false))
					{
						continue;
					}

					if (catalogItem?.Categories?.Any(a => a.Path == "digitalextras" || a.Path == "plugins" || a.Path == "plugins/engine") == true)
					{
						continue;
					}

					// EA App titles — filtering disabled; kept for future settings integration.
					if ((catalogItem?.CustomAttributes?.ContainsKey("ThirdPartyManagedApp") == true) &&
						(catalogItem?.CustomAttributes["ThirdPartyManagedApp"].Value.ToLower() == "the ea app"))
					{
						//if (!SettingsViewModel.Settings.ImportEAGames) { continue; }
					}

					// Ubisoft Connect titles — filtering disabled; kept for future settings integration.
					if ((catalogItem?.CustomAttributes?.ContainsKey("partnerLinkType") == true) &&
						(catalogItem.CustomAttributes["partnerLinkType"].Value == "ubisoft"))
					{
						//if (!SettingsViewModel.Settings.ImportUbisoftGames) { continue; }
					}

					catalogItems.Add(catalogItem);
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, false, PluginName);
				}
			}

			return catalogItems;
		}

		/// <summary>
		/// Fetches playtime records for all games owned by the current account.
		/// Returns an empty list when authentication fails or no data is available.
		/// </summary>
		/// <remarks>⚠️ Requires authentication (<see cref="CurrentAccountInfos.UserId"/> must be set).</remarks>
		public List<PlaytimeItem> GetPlaytimeItems()
		{
			string formattedPlaytimeUrl = string.Format(UrlPlaytimeAll, CurrentAccountInfos.UserId);
			var result = InvokeRequest<List<PlaytimeItem>>(formattedPlaytimeUrl).GetAwaiter().GetResult();

			if (result == null || result.Item2 == null)
			{
				return new List<PlaytimeItem>();
			}

			return result.Item2;
		}

		/// <summary>
		/// Fetches the raw asset records from the Epic library service.
		/// Results are cached to disk for 10 minutes. Handles pagination via nextCursor.
		/// Shows a Playnite notification and returns an empty list when no token is available.
		/// </summary>
		/// <remarks>⚠️ Requires authentication (<see cref="StoreToken"/> must not be null).</remarks>
		public List<Asset> GetAssets()
		{
			string cacheFile = Path.Combine(PathAppsData, Paths.GetSafePathName("assets.json"));
			var result = FileDataService.LoadData<List<Asset>>(cacheFile, 10);

			if (result == null || result.Count() == 0)
			{
				if (StoreToken == null)
				{
					Logger.Warn("EpicApi.GetAssets: StoreToken is null, cannot fetch assets.");
					API.Instance.Notifications.Add(new NotificationMessage(
						$"{PluginName}-Epic-NoToken",
						string.Format("{0}: Login required to fetch Epic assets.", PluginName),
						NotificationType.Error,
						() => ProcessStarter.StartUrl(UrlLogin)
					));
					return result ?? new List<Asset>();
				}

				var response = InvokeRequest<LibraryItemsResponse>(UrlAsset, StoreToken).GetAwaiter().GetResult();
				result = new List<Asset>();

				if (response?.Item2?.Records != null)
				{
					result.AddRange(response.Item2.Records);
				}
				else
				{
					Logger.Warn("EpicApi.GetAssets: Failed to fetch assets or no records returned.");
					FileDataService.SaveData(cacheFile, result);
					return result;
				}

				string nextCursor = response?.Item2?.ResponseMetadata?.NextCursor;
				while (nextCursor != null)
				{
					response = InvokeRequest<LibraryItemsResponse>($"{UrlAsset}&cursor={nextCursor}", StoreToken).GetAwaiter().GetResult();
					if (response?.Item2?.Records != null)
					{
						result.AddRange(response.Item2.Records);
						nextCursor = response?.Item2?.ResponseMetadata?.NextCursor;
					}
					else
					{
						Logger.Warn("EpicApi.GetAssets: Records is null in paginated response; stopping pagination.");
						break;
					}
				}

				FileDataService.SaveData(cacheFile, result);
			}

			return result;
		}

		/// <summary>
		/// Resolves the <see cref="Asset"/> for a Playnite game by matching
		/// <see cref="Game.GameId"/> against <see cref="Asset.AppName"/>.
		/// Results are drawn from the cached asset list (10-minute TTL).
		/// </summary>
		/// <param name="game">The Playnite game to resolve.</param>
		/// <returns>The matching <see cref="Asset"/>, or null when not found.</returns>
		/// <remarks>⚠️ Requires authentication (calls <see cref="GetAssets"/>).</remarks>
		private Asset GetAssetFromGame(Game game)
		{
			if (game == null || game.GameId.IsNullOrEmpty())
			{
				Logger.Warn("EpicApi.GetAssetFromGame: game or GameId is null.");
				return null;
			}

			Asset asset = GetAssets()?.FirstOrDefault(a => a.AppName.IsEqual(game.GameId));
			if (asset == null)
			{
				Logger.Warn($"EpicApi.GetAssetFromGame: No asset found for GameId '{game.GameId}'.");
			}

			return asset;
		}

		/// <summary>
		/// Resolves the Epic sandbox namespace for a Playnite game.
		/// <para>
		/// In Epic's API, namespace and sandboxId are the same value.
		/// This is the primary identifier used by catalog, achievement and DLC endpoints.
		/// </para>
		/// </summary>
		/// <param name="game">The Playnite game to resolve.</param>
		/// <returns>The Epic namespace string, or null when no asset is found.</returns>
		/// <remarks>⚠️ Requires authentication (calls <see cref="GetAssets"/>). For an anonymous alternative use <see cref="GetNamespaceFromGameAnonymous"/>.</remarks>
		public string GetNamespaceFromGame(Game game)
		{
			return GetAssetFromGame(game)?.Namespace;
		}

		/// <summary>
		/// Resolves the Epic store page slug (urlSlug / pageSlug) for a Playnite game.
		/// The result is cached indefinitely on disk.
		/// </summary>
		/// <param name="game">The Playnite game to resolve.</param>
		/// <returns>The store page slug (e.g. "fall-guys"), or null when not resolvable.</returns>
		/// <remarks>⚠️ Requires authentication (calls <see cref="GetAssets"/> via <see cref="GetNamespaceFromGame"/>).</remarks>
		public string GetUrlSlugFromGame(Game game)
		{
			string @namespace = GetNamespaceFromGame(game);
			if (@namespace.IsNullOrEmpty())
			{
				Logger.Warn($"EpicApi.GetUrlSlugFromGame: No namespace resolved for GameId '{game?.GameId}'.");
				return null;
			}

			return GetProductSlug(@namespace);
		}

		/// <summary>
		/// Resolves the Epic achievement productId for a Playnite game.
		/// <para>
		/// The product ID is distinct from the namespace/sandboxId and is required by
		/// <see cref="QueryPlayerProfileAchievementsByProductId"/>.
		/// Results are cached to disk for 24 hours via <see cref="GetAchievementsSchema"/>.
		/// </para>
		/// </summary>
		/// <param name="game">The Playnite game to resolve.</param>
		/// <returns>The product ID string, or null/empty when no achievement schema exists.</returns>
		/// <remarks>⚠️ Requires authentication (calls <see cref="GetAssets"/>).</remarks>
		public string GetProductIdFromGame(Game game)
		{
			string @namespace = GetNamespaceFromGame(game);
			if (@namespace.IsNullOrEmpty())
			{
				Logger.Warn($"EpicApi.GetProductIdFromGame: No namespace resolved for GameId '{game?.GameId}'.");
				return null;
			}

			Tuple<string, ObservableCollection<GameAchievement>> schema = GetAchievementsSchema(@namespace);
			return schema?.Item1;
		}

		/// <summary>
		/// Resolves the <see cref="CatalogItem"/> for a Playnite game.
		/// Results are cached indefinitely on disk.
		/// </summary>
		/// <param name="game">The Playnite game to resolve.</param>
		/// <returns>The <see cref="CatalogItem"/>, or null when not found or on error.</returns>
		/// <remarks>⚠️ Requires authentication (calls <see cref="GetAssets"/>).</remarks>
		public CatalogItem GetCatalogItemFromGame(Game game)
		{
			Asset asset = GetAssetFromGame(game);
			if (asset == null)
			{
				return null;
			}

			try
			{
				return GetCatalogItem(asset.Namespace, asset.CatalogItemId);
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return null;
			}
		}

		/// <summary>
		/// Checks whether the current account is entitled to a specific DLC offer.
		/// </summary>
		/// <param name="nameSpace">The Epic sandbox namespace of the parent game.</param>
		/// <param name="offerId">The offer ID of the DLC.</param>
		/// <returns>true when the account is entitled to all items in the offer; otherwise false.</returns>
		/// <remarks>⚠️ Requires authentication.</remarks>
		private bool DlcIsOwned(string nameSpace, string offerId)
		{
			try
			{
				EntitledOfferItemsResponse ownedDLC = QueryEntitledOfferItems(nameSpace, offerId).GetAwaiter().GetResult();
				return (ownedDLC?.Data?.Launcher?.EntitledOfferItems?.EntitledToAllItemsInOffer ?? false)
					&& (ownedDLC?.Data?.Launcher?.EntitledOfferItems?.EntitledToAnyItemInOffer ?? false);
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return false;
			}
		}

		#endregion

		#region Epic Library — Anonymous (no authentication required)

		/// <summary>
		/// Fetches the store product data for a given slug. Results are cached indefinitely on disk.
		/// </summary>
		/// <param name="slug">The store product slug (e.g. "the-escapists").</param>
		/// <remarks>Does not require authentication — uses public store content API.</remarks>
		private Product GetProduct(string slug)
		{
			string cacheFile = Path.Combine(PathAppsData, Paths.GetSafePathName($"products_{slug}.json"));
			Product result = FileDataService.LoadData<Product>(cacheFile, -1);

			if (result == null)
			{
				string url = string.Format(UrlApiGetProduct, CodeLang.GetCountryFromLast(Locale), slug);
				Tuple<string, Product> catalogResponse = InvokeRequest<Product>(url).GetAwaiter().GetResult();
				result = catalogResponse.Item2;
				FileDataService.SaveData(cacheFile, result);
			}

			return result;
		}

		/// <summary>
		/// Fetches the catalog item metadata for a given namespace + catalog item ID pair.
		/// Results are cached indefinitely on disk.
		/// </summary>
		/// <param name="nameSpace">The Epic sandbox namespace.</param>
		/// <param name="catalogItemId">The unique catalog item identifier.</param>
		/// <returns>
		/// The <see cref="CatalogItem"/>, or null when the item cannot be retrieved.
		/// Throws when the item ID is absent from the returned dictionary.
		/// </returns>
		/// <remarks>Does not require authentication — uses public catalog API.</remarks>
		public CatalogItem GetCatalogItem(string nameSpace, string catalogItemId)
		{
			string cacheFile = Path.Combine(PathAppsData, Paths.GetSafePathName($"{nameSpace}_{catalogItemId}.json"));
			Dictionary<string, CatalogItem> result = FileDataService.LoadData<Dictionary<string, CatalogItem>>(cacheFile, -1);

			if (result == null)
			{
				string url = string.Format(
					"/catalog/api/shared/namespace/{0}/bulk/items?id={1}&country={2}&locale={3}&includeMainGameDetails=true",
					nameSpace, catalogItemId,
					CodeLang.GetCountryFromLast(Locale),
					CodeLang.GetEpicLang(Locale));

				Tuple<string, Dictionary<string, CatalogItem>> catalogResponse =
					InvokeRequest<Dictionary<string, CatalogItem>>(UrlApiCatalog + url).GetAwaiter().GetResult();
				result = catalogResponse.Item2;
				FileDataService.SaveData(cacheFile, result);
			}

			if (result == null)
			{
				Logger.Warn($"EpicApi.GetCatalogItem: Failed to retrieve catalog item for {nameSpace} {catalogItemId}");
				return null;
			}

			return result.TryGetValue(catalogItemId, out CatalogItem catalogItem)
				? catalogItem
				: throw new Exception($"Epic catalog item for {catalogItemId} {nameSpace} not found.");
		}

		/// <summary>
		/// Resolves the store page slug for a given catalog namespace.
		/// The result is cached indefinitely on disk via <see cref="FileDataService"/>.
		/// </summary>
		/// <param name="namespace">The Epic catalog namespace.</param>
		/// <returns>The page slug string, or null when no mapping exists.</returns>
		/// <remarks>Does not require authentication — uses public GraphQL endpoint.</remarks>
		public string GetProductSlug(string @namespace)
		{
			string cacheFile = Path.Combine(PathAppsData, Paths.GetSafePathName($"CatalogMappings_{@namespace}.json"));
			var result = FileDataService.LoadData<CatalogMappingsResponse>(cacheFile, -1);

			if (result?.Data?.Catalog?.CatalogNs?.Mappings?.FirstOrDefault()?.PageSlug == null)
			{
				result = QueryCatalogMappings(@namespace).GetAwaiter().GetResult();
				FileDataService.SaveData(cacheFile, result);
			}

			return result?.Data?.Catalog?.CatalogNs?.Mappings?.FirstOrDefault()?.PageSlug;
		}

		/// <summary>
		/// Attempts to resolve the Epic sandbox namespace for a Playnite game
		/// without requiring user authentication.
		/// <para>
		/// Resolution order:
		/// <list type="number">
		///   <item>Game Links → store URL → slug → <see cref="GetNamespaceFromSlug"/></item>
		///   <item>Normalized game name → <see cref="GetProductSlug"/> → <see cref="GetNamespaceFromSlug"/></item>
		/// </list>
		/// Falls back to null when all paths fail.
		/// </para>
		/// </summary>
		/// <param name="game">The Playnite game to resolve.</param>
		/// <returns>The Epic namespace string, or null when not resolvable without auth.</returns>
		public string GetNamespaceFromGameAnonymous(Game game)
		{
			if (game == null)
			{
				Logger.Warn("EpicApi.GetNamespaceFromGameAnonymous: game is null.");
				return null;
			}

			string slugFromLinks = GetUrlSlugFromGameLinks(game);
			if (!slugFromLinks.IsNullOrEmpty())
			{
				string ns = GetNamespaceFromSlug(slugFromLinks);
				if (!ns.IsNullOrEmpty())
				{
					Logger.Info($"EpicApi.GetNamespaceFromGameAnonymous: resolved via Links for '{game.GameId}' → {ns}");
					return ns;
				}
			}

			string normalizedName = PlayniteTools.NormalizeGameName(
				game.Name?.RemoveTrademarks()?.Replace("'", string.Empty)?.Replace(",", string.Empty)
				?? string.Empty);

			if (!normalizedName.IsNullOrEmpty())
			{
				string slugFromName = GetProductSlug(normalizedName);
				if (!slugFromName.IsNullOrEmpty())
				{
					string ns = GetNamespaceFromSlug(slugFromName);
					if (!ns.IsNullOrEmpty())
					{
						Logger.Info($"EpicApi.GetNamespaceFromGameAnonymous: resolved via Name for '{game.GameId}' → {ns}");
						return ns;
					}
				}
			}

			Logger.Warn($"EpicApi.GetNamespaceFromGameAnonymous: could not resolve namespace for '{game.GameId}'.");
			return null;
		}

		/// <summary>
		/// Resolves the Epic sandbox namespace from a store product slug using
		/// the public <c>getMappingByPageSlug</c> GraphQL query. No authentication required.
		/// <para>
		/// Resolution order:
		/// <list type="number">
		///   <item><c>Mapping.SandboxId</c> — direct field from the page mapping.</item>
		///   <item><c>Mapping.Mappings.Offer.Namespace</c> — fallback from the primary offer.</item>
		///   <item><c>Mapping.Mappings.PrePurchaseOffer.Namespace</c> — fallback from the pre-purchase offer.</item>
		/// </list>
		/// Results are cached to disk indefinitely.
		/// </para>
		/// </summary>
		/// <param name="slug">Epic store product slug (e.g. <c>"fall-guys"</c>).</param>
		/// <returns>The namespace/sandboxId string, or null when the slug is not found.</returns>
		public string GetNamespaceFromSlug(string slug)
		{
			if (slug.IsNullOrEmpty())
			{
				Logger.Warn("EpicApi.GetNamespaceFromSlug: slug is null or empty.");
				return null;
			}

			try
			{
				string cacheFile = Path.Combine(PathAppsData, Paths.GetSafePathName($"mapping_{slug}.json"));
				GetMappingByPageSlugResponse result = FileDataService.LoadData<GetMappingByPageSlugResponse>(cacheFile, -1);

				if (result == null)
				{
					result = QueryMappingByPageSlug(slug).GetAwaiter().GetResult();
					if (result != null)
					{
						FileDataService.SaveData(cacheFile, result);
					}
				}

				var mapping = result?.Data?.StorePageMapping?.Mapping;
				if (mapping == null)
				{
					Logger.Warn($"EpicApi.GetNamespaceFromSlug: No mapping found for slug '{slug}'.");
					return null;
				}

				// SandboxId is the canonical namespace identifier.
				if (!mapping.SandboxId.IsNullOrEmpty())
				{
					return mapping.SandboxId;
				}

				// Fallback: namespace from primary offer.
				string nsFromOffer = mapping.Mappings?.Offer?.Namespace;
				if (!nsFromOffer.IsNullOrEmpty())
				{
					return nsFromOffer;
				}

				// Fallback: namespace from pre-purchase offer.
				string nsFromPrePurchase = mapping.Mappings?.PrePurchaseOffer?.Namespace;
				if (!nsFromPrePurchase.IsNullOrEmpty())
				{
					return nsFromPrePurchase;
				}

				Logger.Warn($"EpicApi.GetNamespaceFromSlug: Mapping found for '{slug}' but all namespace fields are empty.");
				return null;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return null;
			}
		}


		/// <summary>
		/// Extracts an Epic store slug from a full store URL by matching path segments
		/// against the normalized game name.
		/// </summary>
		/// <param name="url">Full Epic store URL (must contain <c>store.epicgames.com</c>).</param>
		/// <param name="normalizedGameName">Normalized game name used for slug matching.</param>
		/// <returns>The slug segment, or an empty string when no match is found.</returns>
		/// <remarks>Does not require authentication.</remarks>
		public string ExtractSlugFromUrl(string url, string normalizedGameName)
		{
			return GetProductSlugByUrl(url, normalizedGameName);
		}

		/// <summary>
		/// Extracts the Epic store page slug from the game's stored links without any API call.
		/// Iterates <see cref="Game.Links"/> looking for a URL that contains <c>store.epicgames.com</c>
		/// and delegates to <see cref="GetProductSlugByUrl"/>.
		/// </summary>
		/// <param name="game">The Playnite game to inspect.</param>
		/// <returns>The slug string (e.g. "fall-guys"), or null when no Epic store link is found.</returns>
		/// <remarks>Does not require authentication.</remarks>
		public string GetUrlSlugFromGameLinks(Game game)
		{
			if (game == null)
			{
				Logger.Warn("EpicApi.GetUrlSlugFromGameLinks: game is null.");
				return null;
			}

			if (game.Links == null || game.Links.Count == 0)
			{
				return null;
			}

			string normalizedName = PlayniteTools.NormalizeGameName(
				game.Name?.RemoveTrademarks()?.Replace("'", string.Empty)?.Replace(",", string.Empty)
				?? string.Empty);

			foreach (Playnite.SDK.Models.Link link in game.Links)
			{
				if (link?.Url == null)
				{
					continue;
				}

				if (!link.Url.Contains("store.epicgames.com", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				string slug = GetProductSlugByUrl(link.Url, normalizedName);
				if (!slug.IsNullOrEmpty())
				{
					return slug;
				}
			}

			return null;
		}

		/// <summary>
		/// Extracts a product slug from an Epic store URL by matching path segments
		/// against the normalized game name.
		/// </summary>
		/// <param name="url">Full Epic store URL.</param>
		/// <param name="gameName">Normalized game name used for slug matching.</param>
		/// <remarks>Does not require authentication.</remarks>
		private string GetProductSlugByUrl(string url, string gameName)
		{
			string productSlug = string.Empty;
			if (url.Contains("store.epicgames.com", StringComparison.InvariantCultureIgnoreCase))
			{
				try
				{
					string[] urlSplit = url.Split('/');
					foreach (string slug in urlSplit)
					{
						if (slug.ContainsInvariantCulture(gameName.ToLower(), System.Globalization.CompareOptions.IgnoreSymbols))
						{
							productSlug = slug;
						}
					}
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, true, PluginName);
				}
			}

			return productSlug;
		}

		#endregion

		#region HTTP / GraphQL — Anonymous (no authentication required)

		/// <summary>
		/// Sends a GET request and deserializes the JSON response to <typeparamref name="T"/>.
		/// On authentication errors, attempts a single token refresh and retries via <see cref="TryRefreshAndRetry{T}"/>.
		/// When called without a token, the request is sent anonymously.
		/// </summary>
		/// <typeparam name="T">Expected response type.</typeparam>
		/// <param name="url">Target URL.</param>
		/// <param name="token">Optional token; when provided its value is sent as the Authorization header.</param>
		/// <returns>
		/// A <see cref="Tuple{String, T}"/> of (raw JSON, deserialized object), or null on failure.
		/// </returns>
		private async Task<Tuple<string, T>> InvokeRequest<T>(string url, StoreToken token = null) where T : class
		{
			using (var httpClient = new HttpClient())
			{
				httpClient.DefaultRequestHeaders.Clear();
				if (token != null)
				{
					httpClient.DefaultRequestHeaders.Add("Authorization", token.Type + " " + token.Token);
				}

				var response = await httpClient.GetAsync(url);
				var str = await response.Content.ReadAsStringAsync();

				if (Serialization.TryFromJson(str, out ErrorResponse error) && !string.IsNullOrEmpty(error.errorCode))
				{
					if (error.errorCode.IndexOf("authentication", StringComparison.OrdinalIgnoreCase) >= 0
						|| error.errorCode.IndexOf("invalid_token", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						var retryResult = await TryRefreshAndRetry<T>(url, token);
						if (retryResult == null)
						{
							Logger.Error($"[EpicApi] Authentication refresh failed for request {url}");
							return null;
						}

						return retryResult;
					}

					Logger.Error($"[EpicApi] Error response from Epic: {error.errorCode}");
					return null;
				}
				else
				{
					try
					{
						return new Tuple<string, T>(str, Serialization.FromJson<T>(str));
					}
					catch (Exception ex)
					{
						Logger.Error(str);
						Common.LogError(ex, false, true, PluginName);
						return null;
					}
				}
			}
		}

		/// <summary>
		/// Executes a GraphQL query against the Epic launcher store endpoint.
		/// Adds the EG1 bearer token only when the account is private or
		/// when <see cref="StoreSettings.UseAuth"/> is set.
		/// When neither condition applies, the query is sent anonymously.
		/// </summary>
		/// <typeparam name="T">Expected deserialized response type.</typeparam>
		/// <param name="queryObject">
		/// An object exposing OperationName, Query and Variables properties
		/// (typically a strongly-typed query class from CommonPluginsStores.Epic.Models.Query).
		/// </param>
		/// <returns>The deserialized response, or null on HTTP error or parse failure.</returns>
		private async Task<T> QueryGraphQL<T>(object queryObject) where T : class
		{
			try
			{
				var queryType = queryObject.GetType();
				string operationName = queryType.GetProperty("OperationName")?.GetValue(queryObject) as string;
				string query = queryType.GetProperty("Query")?.GetValue(queryObject) as string;
				object variables = queryType.GetProperty("Variables")?.GetValue(queryObject);

				var payload = new { query, variables };
				StringContent content = new StringContent(
					Serialization.ToJson(payload),
					Encoding.UTF8,
					"application/json");

				using (HttpClient httpClient = new HttpClient())
				{
					httpClient.DefaultRequestHeaders.Clear();
					httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);

					bool needsAuth = (CurrentAccountInfos?.IsPrivate ?? false) || StoreSettings.UseAuth;
					if (needsAuth && StoreToken != null && !StoreToken.Token.IsNullOrEmpty())
					{
						httpClient.DefaultRequestHeaders.Add("Authorization", StoreToken.Type + " " + StoreToken.Token);
					}

					HttpResponseMessage response = await httpClient.PostAsync(UrlGraphQL, content);
					string str = await response.Content.ReadAsStringAsync();

					if (!response.IsSuccessStatusCode)
					{
						if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
						{
							IsUserLoggedIn = false;
							ShowNotificationUserNoAuthenticate();
						}
						else
						{
							Logger.Error($"[GraphQL] HTTP Error {response.StatusCode}: {operationName} - {str}");
						}

						return null;
					}

					if (Serialization.TryFromJson(str, out T data, out Exception ex))
					{
						return data;
					}
					else if (ex != null)
					{
						Common.LogError(ex, false, false, PluginName, $"Failed to deserialize response - {operationName}");
					}

					return null;
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return null;
			}
		}

		/// <summary>
		/// Queries catalog offer details for a specific namespace and offer ID.
		/// </summary>
		/// <remarks>Does not require authentication.</remarks>
		// TODO: Must be tested in production before relying on this method.
		private async Task<CatalogOfferResponse> QueryCatalogOffer(string @namespace, string offerId, bool includeSubItems = true)
		{
			var query = new QueryCatalogQuery
			{
				Variables =
				{
					Namespace = @namespace,
					OfferId = offerId,
					Locale = CodeLang.GetEpicLang(Locale),
					Country = CodeLang.GetCountryFromLast(Locale),
					IncludeSubItems = includeSubItems
				}
			};
			return await QueryGraphQL<CatalogOfferResponse>(query);
		}

		/// <summary>
		/// Queries the Epic store page mapping for a given product slug.
		/// Returns the <see cref="GetMappingByPageSlugResponse.Mapping"/> containing
		/// <c>SandboxId</c>, <c>ProductId</c>, and the primary offer's <c>Namespace</c>.
		/// Results are cached to disk indefinitely.
		/// </summary>
		/// <param name="pageSlug">
		/// Epic store product slug (e.g. <c>"fall-guys"</c>).
		/// </param>
		/// <returns>
		/// The deserialized <see cref="GetMappingByPageSlugResponse"/>, or null on failure.
		/// </returns>
		/// <remarks>Does not require authentication — uses public GraphQL endpoint.</remarks>
		private async Task<GetMappingByPageSlugResponse> QueryMappingByPageSlug(string pageSlug)
		{
			var query = new QueryGetMappingByPageSlug
			{
				Variables =
				{
					PageSlug = pageSlug,
					Locale = CodeLang.GetEpicLang(Locale)
				}
			};
			return await QueryGraphQL<GetMappingByPageSlugResponse>(query);
		}

		/// <summary>
		/// Executes a keyword search against the Epic store catalog.
		/// Returns raw paginated results including namespace, slugs and key images per offer.
		/// </summary>
		/// <param name="keywords">Search terms (e.g. the game name).</param>
		/// <param name="category">
		/// Epic category filter (e.g. <c>"games/edition/base"</c>).
		/// </param>
		/// <param name="count">Maximum number of results to return (default: 10).</param>
		/// <param name="start">Pagination offset (default: 0).</param>
		/// <param name="withPrice">When true, includes pricing data in the response.</param>
		/// <returns>The <see cref="SearchStoreResponse"/>, or null on failure.</returns>
		/// <remarks>Does not require authentication — uses public GraphQL endpoint.</remarks>
		public async Task<SearchStoreResponse> QuerySearchStore(
			string keywords,
			string category = "games|edition|base|bundles|editors",
			int count = 10,
			int start = 0,
			bool withPrice = false)
		{
			var query = new QuerySearchStore
			{
				Variables =
				{
					Keywords      = keywords,
					Count         = count,
					Start         = start,
					Category      = category,
					Country       = CodeLang.GetCountryFromLast(Locale),
					Locale        = CodeLang.GetEpicLang(Locale),
					AllowCountries = CodeLang.GetCountryFromLast(Locale),
					SortBy        = "title", //title, relevancy, releaseDate, currentPrice
					SortDir       = "ASC", // ASC, DESC
					WithPrice     = withPrice
				}
			};
			return await QueryGraphQL<SearchStoreResponse>(query);
		}


		/// <summary>
		/// Queries the list of add-ons (DLC) available for a given namespace.
		/// </summary>
		/// <param name="namespace">The Epic sandbox namespace of the parent game.</param>
		/// <param name="categories">
		/// Pipe-delimited Epic category filter (default: "addons|digitalextras").
		/// Pass "games/edition/base" to retrieve the base game offer.
		/// </param>
		/// <remarks>Does not require authentication.</remarks>
		private async Task<AddonsByNamespaceResponse> QueryAddonsByNamespace(string @namespace, string categories = "addons|digitalextras")
		{
			var query = new QueryGetAddonsByNamespace
			{
				Variables =
				{
					Categories = categories,
					Count = 1000,
					Country = CodeLang.GetCountryFromLast(Locale),
					Locale = CodeLang.GetEpicLang(Locale),
					Namespace = @namespace,
					SortBy = "effectiveDate",
					SortDir = "DESC"
				}
			};
			return await QueryGraphQL<AddonsByNamespaceResponse>(query);
		}

		/// <summary>
		/// Queries the catalog namespace-to-slug mappings for a given namespace.
		/// </summary>
		/// <remarks>Does not require authentication.</remarks>
		private async Task<CatalogMappingsResponse> QueryCatalogMappings(string @namespace, string pageType = "productHome")
		{
			var query = new QueryGetCatalogMappings
			{
				Variables =
				{
					Namespace = @namespace,
					PageType = pageType
				}
			};
			return await QueryGraphQL<CatalogMappingsResponse>(query);
		}

		/// <summary>
		/// Queries the achievement schema (definitions + rarity) for a given Epic sandbox.
		/// </summary>
		/// <param name="sandboxId">The Epic sandbox identifier.</param>
		/// <remarks>Does not require authentication.</remarks>
		private async Task<AchievementResponse> QueryAchievement(string sandboxId)
		{
			var query = new QueryAchievement
			{
				Variables =
				{
					SandboxId = sandboxId,
					Locale = CodeLang.GetCountryFromFirst(Locale)
				}
			};
			return await QueryGraphQL<AchievementResponse>(query);
		}

		#endregion

		#region HTTP / GraphQL — Authenticated

		/// <summary>
		/// Attempts to refresh the OAuth token and replay the original request exactly once.
		/// Uses <see cref="_tokenRefreshSemaphore"/> to prevent concurrent refresh races.
		/// Clears and nullifies <see cref="StoreToken"/> when the refresh ultimately fails.
		/// </summary>
		/// <typeparam name="T">Expected response type.</typeparam>
		/// <param name="url">The URL of the request to retry.</param>
		/// <param name="token">The token that produced the authentication error.</param>
		/// <returns>
		/// A <see cref="Tuple{String, T}"/> of (raw JSON, deserialized object) on success, or null on failure.
		/// </returns>
		private async Task<Tuple<string, T>> TryRefreshAndRetry<T>(string url, StoreToken token = null) where T : class
		{
			Logger.Info("[EpicApi] Authentication error detected. Attempting token refresh and retry.");
			try
			{
				string refreshToken = token?.RefreshToken ?? StoreToken?.RefreshToken;
				if (!string.IsNullOrEmpty(refreshToken))
				{
					await _tokenRefreshSemaphore.WaitAsync();
					try
					{
						if (StoreToken == null || StoreToken.Token.IsNullOrEmpty() || StoreToken.RefreshToken == refreshToken)
						{
							RenewTokens(refreshToken);
						}

						if (StoreToken != null && !StoreToken.Token.IsNullOrEmpty())
						{
							using (var httpClient2 = new HttpClient())
							{
								httpClient2.DefaultRequestHeaders.Clear();
								if (!string.IsNullOrEmpty(StoreToken.Token))
								{
									string authType2 = string.IsNullOrEmpty(StoreToken.Type) ? "bearer" : StoreToken.Type;
									httpClient2.DefaultRequestHeaders.Add("Authorization", authType2 + " " + StoreToken.Token);
								}

								var retryResp = await httpClient2.GetAsync(url);
								var retryStr = await retryResp.Content.ReadAsStringAsync();

								if (Serialization.TryFromJson(retryStr, out ErrorResponse retryError) && !string.IsNullOrEmpty(retryError.errorCode))
								{
									Logger.Error($"[EpicApi] Token refresh failed to resolve error: {retryError.errorCode}");
									StoreToken = null;
									SetStoredToken(null);
									return null;
								}

								try
								{
									return new Tuple<string, T>(retryStr, Serialization.FromJson<T>(retryStr));
								}
								catch
								{
									Logger.Error(retryStr);
									throw new Exception("Failed to get data from Epic service.");
								}
							}
						}
					}
					finally
					{
						_tokenRefreshSemaphore.Release();
					}
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
			}

			StoreToken = null;
			SetStoredToken(null);
			return null;
		}

		/// <summary>
		/// Fetches the friends summary (friend list, incoming/outgoing requests) for the current user.
		/// </summary>
		/// <remarks>⚠️ Requires authentication.</remarks>
		private async Task<EpicFriendsSummary> GetFriendsSummary()
		{
			string url = string.Format(UrlFriendsSummary, CurrentAccountInfos.UserId);
			Tuple<string, EpicFriendsSummary> data = await InvokeRequest<EpicFriendsSummary>(url);
			return data?.Item2;
		}

		/// <summary>
		/// Fetches the public Epic account record for the given account ID.
		/// </summary>
		/// <param name="id">Epic account ID (GUID string).</param>
		/// <remarks>⚠️ Requires authentication (sends <see cref="StoreToken"/>).</remarks>
		private async Task<EpicAccountResponse> GetAccountInfo(string id)
		{
			string url = string.Format(UrlAccount, id);
			Tuple<string, EpicAccountResponse> data = await InvokeRequest<EpicAccountResponse>(url, StoreToken);
			return data?.Item2;
		}

		/// <summary>
		/// Queries Epic's entitlement service to determine whether the current account
		/// owns all items in the specified offer.
		/// </summary>
		/// <remarks>⚠️ Requires authentication.</remarks>
		private async Task<EntitledOfferItemsResponse> QueryEntitledOfferItems(string epic_namespace, string offerId)
		{
			var query = new QueryGetEntitledOfferItems
			{
				Variables =
				{
					Namespace = epic_namespace,
					OfferId = offerId
				}
			};
			return await QueryGraphQL<EntitledOfferItemsResponse>(query);
		}

		/// <summary>
		/// Queries the authenticated user's Epic wishlist.
		/// </summary>
		/// <param name="pageType">Store page type filter (default: "productHome").</param>
		/// <remarks>⚠️ Requires authentication.</remarks>
		public async Task<WishlistResponse> QueryWishList(string pageType = "productHome")
		{
			var query = new QueryWishlist
			{
				Variables =
				{
					PageType = pageType
				}
			};
			return await QueryGraphQL<WishlistResponse>(query);
		}

		/// <summary>
		/// Queries a player's achievement unlock records for a given Epic sandbox directly,
		/// using the sandbox ID (no product ID required).
		/// Unlike <see cref="QueryPlayerProfileAchievementsByProductId"/>, this query does not
		/// require resolving the product ID from the achievement schema first.
		/// </summary>
		/// <param name="epicAccountId">Epic account ID of the player.</param>
		/// <param name="sandboxId">The Epic sandbox ID of the game.</param>
		/// <remarks>⚠️ Requires authentication.</remarks>
		private async Task<PlayerAchievementBySandboxResponse> QueryPlayerAchievementBySandbox(string epicAccountId, string sandboxId)
		{
			var query = new QueryPlayerAchievementBySandbox
			{
				Variables =
				{
					EpicAccountId = epicAccountId,
					SandboxId = sandboxId
				}
			};
			return await QueryGraphQL<PlayerAchievementBySandboxResponse>(query);
		}

		/// <summary>
		/// Queries a player's achievement unlock data for a specific product.
		/// </summary>
		/// <param name="accountId">Epic account ID of the player.</param>
		/// <param name="productId">The Epic product ID obtained from the achievement schema.</param>
		/// <remarks>⚠️ Requires authentication.</remarks>
		private async Task<PlayerProfileAchievementsByProductIdResponse> QueryPlayerProfileAchievementsByProductId(string accountId, string productId)
		{
			var query = new QueryPlayerProfileAchievementsByProductId
			{
				Variables =
				{
					EpicAccountId = accountId,
					ProductId = productId
				}
			};
			return await QueryGraphQL<PlayerProfileAchievementsByProductIdResponse>(query);
		}

		#endregion

		#region Game Requirements Helpers

		/// <summary>
		/// Maps a single Epic <see cref="Detail"/> entry into the appropriate field
		/// of <paramref name="minimum"/> and <paramref name="recommended"/>.
		/// <para>
		/// Epic encodes all requirement values as plain strings. RAM and Storage are parsed
		/// from human-readable size strings (e.g. "8 GB", "50 GB") into bytes.
		/// Unrecognised titles (e.g. "DirectX", "Sound Card") are silently skipped.
		/// </para>
		/// </summary>
		private static void MapDetailToRequirements(Detail detail, RequirementEntry minimum, RequirementEntry recommended)
		{
			if (detail == null)
			{
				return;
			}

			string title = detail.Title?.Trim() ?? string.Empty;

			if (string.Equals(title, "OS", StringComparison.OrdinalIgnoreCase))
			{
				AddIfNotEmpty(minimum.Os, detail.Minimum);
				AddIfNotEmpty(recommended.Os, detail.Recommended);
			}
			else if (string.Equals(title, "Processor", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(title, "CPU", StringComparison.OrdinalIgnoreCase))
			{
				AddIfNotEmpty(minimum.Cpu, detail.Minimum);
				AddIfNotEmpty(recommended.Cpu, detail.Recommended);
			}
			else if (string.Equals(title, "Graphics", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(title, "GPU", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(title, "Video Card", StringComparison.OrdinalIgnoreCase))
			{
				AddIfNotEmpty(minimum.Gpu, detail.Minimum);
				AddIfNotEmpty(recommended.Gpu, detail.Recommended);
			}
			else if (string.Equals(title, "Memory", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(title, "RAM", StringComparison.OrdinalIgnoreCase))
			{
				minimum.Ram = ParseSizeToBytes(detail.Minimum);
				recommended.Ram = ParseSizeToBytes(detail.Recommended);
			}
			else if (string.Equals(title, "Storage", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(title, "Hard Drive", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(title, "Hard disk space", StringComparison.OrdinalIgnoreCase))
			{
				minimum.Storage = ParseSizeToBytes(detail.Minimum);
				recommended.Storage = ParseSizeToBytes(detail.Recommended);
			}
		}

		/// <summary>
		/// Appends <paramref name="value"/> to <paramref name="list"/> when non-null and non-whitespace.
		/// </summary>
		private static void AddIfNotEmpty(List<string> list, string value)
		{
			if (!value.IsNullOrEmpty())
			{
				list.Add(value.Trim());
			}
		}

		/// <summary>
		/// Parses a human-readable size string into bytes.
		/// Handles formats such as "8 GB", "8192 MB", "50GB".
		/// Returns 0 when the input is null, empty, or cannot be parsed.
		/// </summary>
		private static double ParseSizeToBytes(string value)
		{
			if (value.IsNullOrEmpty())
			{
				return 0;
			}

			string normalised = value.Trim().Replace("\u00A0", " ");
			int unitStart = normalised.Length;

			while (unitStart > 0 && (char.IsLetter(normalised[unitStart - 1]) || normalised[unitStart - 1] == ' '))
			{
				unitStart--;
			}

			string numericPart = normalised.Substring(0, unitStart).Trim();
			string unit = normalised.Substring(unitStart).Trim().ToUpperInvariant();

			if (!double.TryParse(
				numericPart,
				System.Globalization.NumberStyles.Any,
				System.Globalization.CultureInfo.InvariantCulture,
				out double number))
			{
				return 0;
			}

			switch (unit)
			{
				case "TB": return number * 1024L * 1024 * 1024 * 1024;
				case "GB": return number * 1024L * 1024 * 1024;
				case "MB": return number * 1024L * 1024;
				case "KB": return number * 1024L;
				default: return number;
			}
		}

		#endregion
	}
}