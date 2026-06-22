using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using CommonPluginsStores.Steam;
using FuzzySharp;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.PCGamingWiki
{
	public class PCGamingWikiApi
	{
		internal static readonly ILogger Logger = LogManager.GetLogger();
		/// <summary>Minimum delay between PCGamingWiki HTTP calls to reduce throttling.</summary>
		private static readonly TimeSpan ApiRequestMinInterval = TimeSpan.FromMilliseconds(500);

		internal string PluginName { get; }
		internal string ClientName => "PCGamingWiki";
		internal ExternalPlugin PluginLibrary { get; }

		private SteamApi SteamApi { get; }
		private readonly RequestRateLimiter _apiRateLimiter = new RequestRateLimiter(ApiRequestMinInterval);

		private void WaitForApiRateLimit()
		{
			_apiRateLimiter.WaitAsync().GetAwaiter().GetResult();
		}

		#region Urls

		private string UrlBase => @"https://pcgamingwiki.com";
		private string UrlWithSteamId => UrlBase + @"/api/appid.php?appid={0}";
		private string UrlPCGamingWikiSearchWithApi => UrlBase + @"/w/api.php?action=opensearch&format=json&formatversion=2&search={0}&namespace=0&limit=10";
		
		#endregion

		public PCGamingWikiApi(string pluginName, ExternalPlugin pluginLibrary, SteamApi steamApi = null)
		{
			PluginName = pluginName;
			PluginLibrary = pluginLibrary;
			SteamApi = steamApi ?? new SteamApi(PluginName, pluginLibrary);
		}

		/// <summary>
		/// Resolves the best PCGamingWiki page URL for <paramref name="game"/>.
		/// Order: direct wiki link, Steam AppId redirect, then opensearch.
		/// </summary>
		/// <param name="game">Target game.</param>
		/// <param name="steamAppId">
		/// Optional pre-resolved Steam AppId. When zero and the AppId step runs,
		/// <see cref="SteamApi.ResolveAppId"/> is called and the result is written back
		/// so callers can reuse it for a Steam fallback without a second lookup.
		/// </param>
		/// <param name="steamAppIdLookupAttempted">
		/// Set to <see langword="true"/> once <see cref="SteamApi.ResolveAppId"/> has run
		/// during the AppId lookup step, allowing callers to skip a redundant Steam fallback lookup.
		/// </param>
		public string FindGoodUrl(Game game, ref uint steamAppId, ref bool steamAppIdLookupAttempted)
		{
			try
			{
				string directUrl = GetDirectPcgwLink(game);
				if (!directUrl.IsNullOrEmpty())
				{
					Logger.Info($"Url for PCGamingWiki find for {game.Name} - {directUrl}");
					return directUrl;
				}

				string appIdUrl = TryGetUrlFromSteamAppId(game, ref steamAppId, ref steamAppIdLookupAttempted);
				if (!appIdUrl.IsNullOrEmpty())
				{
					Logger.Info($"Url for PCGamingWiki find for {game.Name} - {appIdUrl}");
					return appIdUrl;
				}

				string searchUrl = TryGetUrlFromPcgwSearch(game);
				if (!searchUrl.IsNullOrEmpty())
				{
					Logger.Info($"Url for PCGamingWiki find for {game.Name} - {searchUrl}");
					return searchUrl;
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
			}

			Logger.Warn($"Url for PCGamingWiki not find for {game.Name}");
			return string.Empty;
		}

		/// <summary>
		/// Download and parse the Windows system requirements from a PCGamingWiki page.
		/// Returns null if the page contains no requirement data.
		/// </summary>
		public GameRequirements GetGameRequirements(string url)
		{
			if (url.IsNullOrEmpty())
			{
				throw new ArgumentNullException(nameof(url));
			}

			try
			{
				Common.LogDebug(true, $"PCGamingWikiApi.GetSystemRequirements - url: {url}");

				string html = string.Empty;
				try
				{
					html = Web.DownloadSourceDataWebView(url).GetAwaiter().GetResult().Item1;
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, $"Failed to download {url}", true, PluginName);
					return null;
				}

				HtmlParser parser = new HtmlParser();
				IHtmlDocument document = parser.Parse(html);

				IElement sysreqBlock = document.QuerySelector("div.sysreq_Windows");
				if (sysreqBlock == null)
				{
					Logger.Warn($"PCGamingWikiApi - No Windows sysreq block found at {url}");
					return null;
				}

				GameRequirements result = new GameRequirements
				{
					GameName = document.QuerySelector("h1.article-title")?.InnerHtml ?? string.Empty
				};

				foreach (IElement row in sysreqBlock.QuerySelectorAll(".table-sysreqs-body-row"))
				{
					string dataTitle = row.QuerySelector(".table-sysreqs-body-parameter")?.InnerHtml.ToLower() ?? string.Empty;
					string dataMinimum = row.QuerySelector(".table-sysreqs-body-minimum")?.InnerHtml.Trim() ?? string.Empty;
					string dataRecommended = row.QuerySelector(".table-sysreqs-body-recommended")?.InnerHtml.Trim() ?? string.Empty;

					Common.LogDebug(true, $"PCGamingWikiApi - [{dataTitle}] min: {dataMinimum} | rec: {dataRecommended}");

					ParseRow(dataTitle, dataMinimum, dataRecommended, result);
				}

				result.SourceLink = new SourceLink
				{
					Name = "PCGamingWiki",
					GameName = result.GameName,
					Url = url
				};

				return result;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
				return null;
			}
		}

		#region Private helpers

		private string GetDirectPcgwLink(Game game)
		{
			return game.Links?
				.Where(link =>
					link.Url.Contains("pcgamingwiki", StringComparison.OrdinalIgnoreCase) &&
					!IsPcgwSearchUrl(link.Url))
				.Select(link => link.Url)
				.FirstOrDefault();
		}

		/// <summary>
		/// Returns <see langword="true"/> for PCGamingWiki search URLs (not article pages),
		/// regardless of scheme or host casing.
		/// </summary>
		private static bool IsPcgwSearchUrl(string url)
		{
			return !url.IsNullOrEmpty()
				&& url.IndexOf("/w/index.php?search=", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private string TryGetUrlFromSteamAppId(Game game, ref uint steamAppId, ref bool steamAppIdLookupAttempted)
		{
			if (!steamAppIdLookupAttempted)
			{
				if (steamAppId == 0)
				{
					steamAppId = SteamApi.ResolveAppId(game);
				}

				steamAppIdLookupAttempted = true;
			}

			if (steamAppId == 0)
			{
				return string.Empty;
			}

			string url = string.Format(UrlWithSteamId, steamAppId);
			Common.LogDebug(true, $"[PCGamingWikiApi] Trying appid lookup for {game.Name} ({steamAppId}) with throttled request.");
			WaitForApiRateLimit();
			string response = Web.DownloadStringData(url).GetAwaiter().GetResult();
			if (!response.Contains("search results", StringComparison.OrdinalIgnoreCase))
			{
				return url;
			}

			Logger.Warn($"PCGamingWiki appid lookup returned search page for {game.Name} ({steamAppId}), using fallback strategies.");
			return string.Empty;
		}

		private string TryGetUrlFromPcgwSearch(Game game)
		{
			string name = PlayniteTools.NormalizeGameName(game.Name);

			if (game.ReleaseDate != null)
			{
				string urlWithYear = string.Format(
					UrlPCGamingWikiSearchWithApi,
					WebUtility.UrlEncode(name + $" ({((ReleaseDate)game.ReleaseDate).Year})"));
				string result = GetWithSearchApi(urlWithYear);
				if (!result.IsNullOrEmpty())
				{
					return result;
				}
			}

			string url = string.Format(UrlPCGamingWikiSearchWithApi, WebUtility.UrlEncode(name));
			return GetWithSearchApi(url);
		}

		private void ParseRow(string dataTitle, string dataMinimum, string dataRecommended, GameRequirements target)
		{
			switch (dataTitle)
			{
				case "operating system (os)":
					if (!dataMinimum.IsNullOrEmpty()) target.Minimum.Os = ParseOs(dataMinimum);
					if (!dataRecommended.IsNullOrEmpty()) target.Recommended.Os = ParseOs(dataRecommended);
					break;

				case "processor (cpu)":
					if (!dataMinimum.IsNullOrEmpty()) target.Minimum.Cpu = ParseCpu(dataMinimum);
					if (!dataRecommended.IsNullOrEmpty()) target.Recommended.Cpu = ParseCpu(dataRecommended);
					break;

				case "system memory (ram)":
					if (!dataMinimum.IsNullOrEmpty()) target.Minimum.RamSource = StripPlainText(dataMinimum);
					if (!dataRecommended.IsNullOrEmpty()) target.Recommended.RamSource = StripPlainText(dataRecommended);
					break;

				case "storage drive (hdd/ssd)":
					if (!dataMinimum.IsNullOrEmpty()) target.Minimum.StorageSource = StripPlainText(dataMinimum);
					if (!dataRecommended.IsNullOrEmpty()) target.Recommended.StorageSource = StripPlainText(dataRecommended);
					break;

				case "video card (gpu)":
					if (!dataMinimum.IsNullOrEmpty()) target.Minimum.Gpu.AddRange(ParseGpu(dataMinimum));
					if (!dataRecommended.IsNullOrEmpty()) target.Recommended.Gpu.AddRange(ParseGpu(dataRecommended));
					break;

				default:
					if (dataTitle.IndexOf("directx", StringComparison.OrdinalIgnoreCase) >= 0
						|| dataTitle.IndexOf("direct3d", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						ParseDirectXRow(dataTitle, dataMinimum, dataRecommended, target);
						break;
					}

					Logger.Warn($"PCGamingWikiApi - No handler for sysreq field: {dataTitle}");
					break;
			}
		}

		private string GetWithSearchApi(string url)
		{
			try
			{
				string response = Web.DownloadStringData(url).GetAwaiter().GetResult();
				if (Serialization.TryFromJson(response, out dynamic data) && data[3]?.Count > 0)
				{
					List<string> listName = Serialization.FromJson<List<string>>(Serialization.ToJson(data[1]));
					List<string> listUrl = Serialization.FromJson<List<string>>(Serialization.ToJson(data[3]));

					Dictionary<string, string> dataFound = new Dictionary<string, string>();
					for (int i = 0; i < listName.Count; i++)
					{
						dataFound.Add(listName[i], listUrl[i]);
					}

					var fuzzList = dataFound
						.Select(x => new
						{
							MatchPercent = Fuzz.Ratio(data[0].ToString().ToLower(), x.Key.ToLower()),
							Data = x
						})
						.OrderByDescending(x => x.MatchPercent)
						.ToList();

					return fuzzList.First().MatchPercent >= 95
						? fuzzList.First().Data.Value
						: string.Empty;
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginName);
			}

			return string.Empty;
		}

		#endregion

		#region Parsers

		private static List<string> ParseOs(string data)
		{
			data = data.Replace("<br>", "¤");
			data = Regex.Replace(data, @"<[^>]*>", string.Empty, RegexOptions.IgnoreCase);

			return data.Replace(",", "¤").Replace(" or ", "¤").Replace("/", "¤")
				.Split('¤')
				.Select(x => x.Trim())
				.Where(x => !x.IsNullOrEmpty())
				.ToList();
		}

		private static List<string> ParseCpu(string data)
		{
			data = data.Replace("<br>", "¤");
			data = Regex.Replace(data, @"<[^>]*>", string.Empty, RegexOptions.IgnoreCase);
			data = Regex.Replace(data, @"\d+ bit processor", string.Empty, RegexOptions.IgnoreCase);
			data = Regex.Replace(data, @"\d+ or newer dual-core Intel or AMD", string.Empty, RegexOptions.IgnoreCase);
			data = Regex.Replace(data, @"\d+ or newer Intel Core i3, i5 or i7", string.Empty, RegexOptions.IgnoreCase);
			data = Regex.Replace(data, @"\(\d+(\.\d+)? GHz if graphics card does not support T&amp;L\)", string.Empty, RegexOptions.IgnoreCase);
			data = Regex.Replace(data, @"Dual-core CPU \(\d+(\.\d+)? GHz or greater speed\)", "Dual-core CPU", RegexOptions.IgnoreCase);
			data = Regex.Replace(data, @"\(\d CPUs\), ~\d+(\.\d+)? GHz", string.Empty, RegexOptions.IgnoreCase);
			data = Regex.Replace(data, @"-\w+ @ \d+(\.\d+)? GHz", string.Empty, RegexOptions.IgnoreCase);

			data = data
				.Replace("one that works", string.Empty)
				.Replace("(approx)", string.Empty)
				.Replace("GHz+", "GHz")
				.Replace("SSE2 instruction set support", string.Empty)
				.Replace("(or equivalent)", string.Empty)
				.Replace("or equivalent", string.Empty)
				.Replace("(DXR)", string.Empty)
				.Replace(" from Intel or AMD at", string.Empty)
				.Replace("with SSE2 instruction set support", string.Empty)
				.Replace("faster", string.Empty)
				.Replace("(and graphics card with T&amp;L)", string.Empty)
				.Replace("or AMD equivalent", string.Empty)
				.Replace("or better", string.Empty)
				.Replace("or higher", string.Empty)
				.Replace("(D3D)/300", string.Empty)
				.Replace("(with 3D acceleration)", string.Empty)
				.Replace("(software)", string.Empty)
				.Replace(", x86", string.Empty)
				.Replace(" / ", "¤").Replace("<br>", "¤").Replace(" or ", "¤");

			List<string> result = data.Split('¤')
				.Select(x => x.Trim())
				.Where(x => !x.IsNullOrEmpty())
				.ToList();

			// Strip lone GHz specs that are only meaningful with a sup tag (e.g. "3.5 GHz<sup>...")
			result = result.Select(x =>
			{
				Match match = Regex.Match(x, @"^\d+(\.\d+)? GHz(?=<sup)", RegexOptions.IgnoreCase);
				return match.Success ? match.Value : x;
			}).ToList();

			return result;
		}

		private static void ParseDirectXRow(string dataTitle, string dataMinimum, string dataRecommended, GameRequirements target)
		{
			string titleToken = StripPlainText(dataTitle);
			if (!titleToken.IsNullOrEmpty())
			{
				target.Minimum.Gpu.Add(titleToken);
				if (!dataRecommended.IsNullOrEmpty())
				{
					target.Recommended.Gpu.Add(titleToken);
				}
			}

			if (!dataMinimum.IsNullOrEmpty())
			{
				target.Minimum.Gpu.AddRange(ParseGpu(dataMinimum));
			}

			if (!dataRecommended.IsNullOrEmpty())
			{
				target.Recommended.Gpu.AddRange(ParseGpu(dataRecommended));
			}
		}

		private static string StripPlainText(string data)
		{
			data = data.Replace("<br>", " ");
			return Regex.Replace(data ?? string.Empty, @"<[^>]*>", string.Empty, RegexOptions.IgnoreCase)
				.Replace("\t", " ")
				.Trim();
		}

		private static List<string> ParseGpu(string data)
		{
			data = data.Replace("<br>", "¤");
			data = Regex.Replace(data, @"<[^>]*>", string.Empty, RegexOptions.IgnoreCase);
			data = Regex.Replace(data, @"[(]\d+x\d+[)]", string.Empty, RegexOptions.IgnoreCase);
			data = Regex.Replace(data, @"\(AMD Catalyst \d+\.\d+, nVidia \d+\.\d+\)", string.Empty, RegexOptions.IgnoreCase);
			data = Regex.Replace(data, @"XNA \d+(\.\d+)? compatible", string.Empty, RegexOptions.IgnoreCase);
			data = Regex.Replace(data, @"\(shader model \d+(\.\d+)?\)\+", string.Empty, RegexOptions.IgnoreCase);

			data = data
				.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
				.Replace("a toaster - you really shouldn't have trouble", string.Empty)
				.Replace("Non-Dedicated (shared) video card", string.Empty)
				.Replace("Onboard graphics chipset", string.Empty)
				.Replace("compatible hardware", string.Empty)
				.Replace("AMD Radeon or Nvidia GeForce recommended", string.Empty)
				.Replace("A dedicated GPU (Nvidia or AMD) with at least", string.Empty)
				.Replace("strongly recommended", string.Empty)
				.Replace("XNA Hi Def Profile Compatible GPU", string.Empty)
				.Replace("(GTX 970 or above required for VR)", string.Empty)
				.Replace("DirectX-compliant", string.Empty)
				.Replace("Mobile or dedicated", string.Empty)
				.Replace("DirectX compatible card", string.Empty)
				.Replace("or better", string.Empty)
				.Replace("of VRAM", "VRAM")
				.Replace("(Shared Memory is not recommended)", string.Empty)
				.Replace("(DXR)", string.Empty)
				.Replace("TnL support", string.Empty)
				.Replace("Integrated graphics, monitor with resolution of 1280x720.", "1280x720")
				.Replace("+ compatible", string.Empty).Replace("compatible", string.Empty)
				.Replace("that supports DirectDraw at 640x480 resolution, 256 colors", string.Empty)
				.Replace("or higher", string.Empty)
				.Replace("capable GPU", string.Empty)
				.Replace("  ", " ")
				.Replace(" / ", "¤").Replace(" or ", "¤").Replace(", ", "¤");

			List<string> result = data.Split('¤')
				.Select(x => x.Trim())
				.Where(x => x.Length > 4)
				.Where(x => x.ToLower().IndexOf("shader") == -1)
				.Where(x => x.ToLower().IndexOf("anything") == -1)
				.Where(x => x.ToLower().IndexOf("any card") == -1)
				.Where(x => !x.IsNullOrEmpty())
				.ToList();

			result = result.Select(x =>
			{
				Match match = Regex.Match(x, @"^OpenGL ES \d+(\.\d+)?", RegexOptions.IgnoreCase);
				return match.Success ? match.Value : x;
			}).ToList();

			return result;
		}

		#endregion
	}
}