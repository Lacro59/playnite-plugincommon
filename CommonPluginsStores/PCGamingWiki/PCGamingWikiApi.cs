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
using System.Threading;
using static CommonPluginsShared.PlayniteTools;

namespace CommonPluginsStores.PCGamingWiki
{
	public class PCGamingWikiApi
	{
		internal static readonly ILogger Logger = LogManager.GetLogger();

		internal string PluginName { get; }
		internal string ClientName => "PCGamingWiki";
		internal ExternalPlugin PluginLibrary { get; }

		#region Urls

		private string UrlBase => @"https://pcgamingwiki.com";
		private string UrlWithSteamId => UrlBase + @"/api/appid.php?appid={0}";
		private string UrlPCGamingWikiSearch => UrlBase + @"/w/index.php?search=";
		private string UrlPCGamingWikiSearchWithApi => UrlBase + @"/w/api.php?action=opensearch&format=json&formatversion=2&search={0}&namespace=0&limit=10";
		
		#endregion

		public PCGamingWikiApi(string pluginName, ExternalPlugin pluginLibrary)
		{
			PluginName = pluginName;
			PluginLibrary = pluginLibrary;
		}

		/// <summary>
		/// Get the url for PCGamingWiki from url on Game or with Steam appId or with a search on website.
		/// </summary>
		public string FindGoodUrl(Game game)
		{
			try
			{
				string url = string.Empty;

				#region With Steam appId

				uint appId = 0;

				if (game.PluginId == GetPluginId(ExternalPlugin.SteamLibrary))
				{
					appId = uint.Parse(game.GameId);
				}
				else
				{
					SteamApi steamApi = new SteamApi(PluginName, ExternalPlugin.CheckLocalizations);
					appId = steamApi.GetAppId(game);
				}

				if (appId != 0)
				{
					url = string.Format(UrlWithSteamId, appId);
					Thread.Sleep(500);
					string response = Web.DownloadStringData(url).GetAwaiter().GetResult();
					if (!response.Contains("search results", StringComparison.OrdinalIgnoreCase))
					{
						Logger.Info($"Url for PCGamingWiki find for {game.Name} - {url}");
						return url;
					}
				}

				#endregion

				#region With game links

				url = game.Links?
					.FirstOrDefault(link =>
						link.Url.Contains("pcgamingwiki", StringComparison.OrdinalIgnoreCase) &&
						!link.Url.StartsWith(UrlPCGamingWikiSearch, StringComparison.OrdinalIgnoreCase))
					?.Url;

				if (!url.IsNullOrEmpty())
				{
					Logger.Info($"Url for PCGamingWiki find for {game.Name} - {url}");
					return url;
				}

				#endregion

				#region With PCGamingWiki search

				string name = PlayniteTools.NormalizeGameName(game.Name);

				if (game.ReleaseDate != null)
				{
					url = string.Format(UrlPCGamingWikiSearchWithApi,
						WebUtility.UrlEncode(name + $" ({((ReleaseDate)game.ReleaseDate).Year})"));
					url = GetWithSearchApi(url);
					if (!url.IsNullOrEmpty())
					{
						Logger.Info($"Url for PCGamingWiki find for {game.Name} - {url}");
						return url;
					}
				}

				url = string.Format(UrlPCGamingWikiSearchWithApi, WebUtility.UrlEncode(name));
				url = GetWithSearchApi(url);
				if (!url.IsNullOrEmpty())
				{
					Logger.Info($"Url for PCGamingWiki find for {game.Name} - {url}");
					return url;
				}

				#endregion
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
					if (!dataMinimum.IsNullOrEmpty()) target.Minimum.Ram = ParseSize(dataMinimum);
					// Note: original code used dataMinimum for recommended — bug preserved intentionally; fix if needed.
					if (!dataRecommended.IsNullOrEmpty()) target.Recommended.Ram = ParseSize(dataRecommended);
					break;

				case "hard disk drive (hdd)":
					if (!dataMinimum.IsNullOrEmpty()) target.Minimum.Storage = ParseSize(dataMinimum);
					if (!dataRecommended.IsNullOrEmpty()) target.Recommended.Storage = ParseSize(dataRecommended);
					break;

				case "video card (gpu)":
					if (!dataMinimum.IsNullOrEmpty()) target.Minimum.Gpu = ParseGpu(dataMinimum);
					if (!dataRecommended.IsNullOrEmpty()) target.Recommended.Gpu = ParseGpu(dataRecommended);
					break;

				default:
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

			data = data
				.Replace("Anything made in the last decade", string.Empty)
				.Replace("(latest service pack)", string.Empty)
				.Replace("(1803 or later)", string.Empty)
				.Replace(" (Only inclusive until patch 1.16.1. Patch 1.17+ Needs XP and greater.)", string.Empty)
				.Replace("Windows", string.Empty)
				.Replace("10 October 2018 Update", string.Empty)
				.Replace("(DXR)", string.Empty)
				.Replace("or better", string.Empty)
				.Replace(",", "¤").Replace(" or ", "¤").Replace("/", "¤");

			return data.Split('¤')
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

		/// <summary>
		/// Parses a RAM or storage size string into bytes.
		/// Handles MB and GB suffixes.
		/// </summary>
		private static long ParseSize(string data)
		{
			data = data.ToLower()
				.Replace("ram mb ram", string.Empty)
				.Replace("ram", string.Empty)
				.Trim();

			if (data.Contains("mb"))
			{
				data = data.Substring(0, data.IndexOf("mb", StringComparison.Ordinal));
				double.TryParse(
					NormalizeDecimalSeparator(data),
					NumberStyles.Any, CultureInfo.CurrentCulture,
					out double value);
				return (long)(1024L * 1024 * value);
			}

			if (data.Contains("gb"))
			{
				data = data.Substring(0, data.IndexOf("gb", StringComparison.Ordinal));
				double.TryParse(
					NormalizeDecimalSeparator(data),
					NumberStyles.Any, CultureInfo.CurrentCulture,
					out double value);
				return (long)(1024L * 1024 * 1024 * value);
			}

			return 0;
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
				.Replace("Integrated graphics", string.Empty)
				.Replace("Integrated", string.Empty).Replace("Dedicated", string.Empty)
				.Replace("+ compatible", string.Empty).Replace("compatible", string.Empty)
				.Replace("that supports DirectDraw at 640x480 resolution, 256 colors", string.Empty)
				.Replace("or higher", string.Empty)
				.Replace("capable GPU", string.Empty)
				.Replace("  ", " ")
				.Replace(" / ", "¤").Replace(" or ", "¤").Replace(", ", "¤");

			data = Regex.Replace(data, @"DX(\d+)[+]?", "DirectX $1");

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

		private static string NormalizeDecimalSeparator(string value)
		{
			return value
				.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
				.Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
				.Trim();
		}

		#endregion
	}
}