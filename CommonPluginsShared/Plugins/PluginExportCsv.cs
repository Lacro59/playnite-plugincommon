using CommonPlayniteShared.Commands;
using CommonPlayniteShared.Common;
using CommonPluginsControls.ViewModels;
using CommonPluginsControls.Views;
using CommonPluginsShared.Collections;
using Microsoft.Win32;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace CommonPluginsShared.Plugins
{
	public abstract class PluginExportCsv<TItem> where TItem : PluginDataBaseGameBase
	{
		protected static readonly ILogger Logger = LogManager.GetLogger();

		/// <summary>
		/// CSV Separator (e.g. ";", ",").
		/// </summary>
		public string Separator { get; set; } = ";";

		protected PluginExportCsv()
		{
		}

		public virtual bool ExportToCsv(string pluginName, IEnumerable<TItem> items, List<string> ignored = null)
		{
			bool isSuccess = false;

			// 1. Initialise the ViewModel — pass pluginName so it can suggest a filename
			var viewModel = new ExportCsvViewModel(GetHeader(), pluginName);
			var view = new ExportCsvView { DataContext = viewModel };

			// 2. Create the Playnite window
			var windowOptions = new WindowOptions
			{
				ShowMinimizeButton = false,
				ShowMaximizeButton = false,
				CanBeResizable = false,
				Width = 500,
				MinHeight = 500
			};
			var window = PlayniteUiHelper.CreateExtensionWindow(
				ResourceProvider.GetString("LOCCommonExport"), view, windowOptions);
			window.ShowDialog();

			// 3. If the user confirmed AND selected an output file
			if (viewModel.Result && !string.IsNullOrWhiteSpace(viewModel.OutputPath))
			{
				this.Separator = viewModel.GetRealSeparator();
				var selectedFields = viewModel.GetSelectedFields();
				string filePath = viewModel.OutputPath;

				Stopwatch stopWatch = Stopwatch.StartNew();

				GlobalProgressResult progressResult = API.Instance.Dialogs.ActivateGlobalProgress(
					args => ProcessExport(filePath, items.ToList(), args, selectedFields),
					new GlobalProgressOptions(ResourceProvider.GetString("LOCCommonProcessingExtraction"), true));

				isSuccess = progressResult.Result is true;

				// 4. Open the containing folder on success
				if (isSuccess && File.Exists(filePath))
				{
					string directoryPath = Path.GetDirectoryName(filePath);
					GlobalCommands.NavigateDirectoryCommand.Execute(directoryPath);
				}

				stopWatch.Stop();
				TimeSpan ts = stopWatch.Elapsed;

				string fieldsInfo = selectedFields?.Any() == true
					? string.Join(", ", selectedFields)
					: "All Fields";
				var canceled = progressResult.Canceled ? " (canceled)" : string.Empty;
				var duration = $"{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
				var count = items?.Count();

				Logger.Info($"ExtractToCsv({fieldsInfo}){canceled} – {duration} for {count} items");
			}

			return isSuccess;
		}

		private bool ProcessExport(string filePath, List<TItem> items, GlobalProgressActionArgs args, List<string> selectedFields)
		{
			args.ProgressMaxValue = items.Count;

			// Get header dictionary: Key = Technical Name, Value = Localized Name
			Dictionary<string, string> fullHeader = GetHeader();

			// Filter keys based on user selection
			List<string> enabledKeys = selectedFields == null || selectedFields.Count == 0
				? fullHeader.Keys.ToList()
				: fullHeader.Keys.Where(k => selectedFields.Contains(k)).ToList();

			using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
			{
				sw.Write('\uFEFF'); // BOM Excel

				// Write Localized Headers
				List<string> localizedHeaders = enabledKeys.Select(k => fullHeader[k]).ToList();
				sw.WriteLine(FormatCsvLine(localizedHeaders));

				foreach (TItem item in items)
				{
					if (args.CancelToken.IsCancellationRequested) return false;

					args.Text = GetExportMessage(item, (int)args.CurrentProgressValue, (int)args.ProgressMaxValue);

					IEnumerable<Dictionary<string, string>> rows = GetRows(item);
					if (rows != null)
					{
						foreach (var row in rows)
						{
							// Create line by matching enabled keys
							List<string> lineCells = enabledKeys.Select(k => row.ContainsKey(k) ? row[k] : string.Empty).ToList();
							sw.WriteLine(FormatCsvLine(lineCells));
						}
					}
					args.CurrentProgressValue++;
				}
			}
			return true;
		}

		protected virtual string FormatCsvLine(List<string> cells)
		{
			return string.Join(Separator, cells.Select(c =>
			{
				string content = c ?? string.Empty;
				if (content.Contains(Separator) || content.Contains("\"") || content.Contains("\r") || content.Contains("\n"))
				{
					return "\"" + content.Replace("\"", "\"\"") + "\"";
				}
				return content;
			}));
		}

		protected virtual string GetExportMessage(TItem item, int current, int total)
		{
			return string.Format("{0}\n{1}\n{2}/{3}",
				ResourceProvider.GetString("LOCCommonProcessingExtraction"),
				item.Game?.Name ?? "Unknown", current + 1, total);
		}

		/// <summary>
		/// Technical Name vs Localized Name.
		/// </summary>
		protected abstract Dictionary<string, string> GetHeader();

		/// <summary>
		/// Returns a list of rows, each row is a dictionary of Technical Name vs Value.
		/// </summary>
		protected abstract IEnumerable<Dictionary<string, string>> GetRows(TItem item);
	}
}