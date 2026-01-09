using Ardalis.GuardClauses;
using CommonPlayniteShared.Common;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommonPluginsShared
{
	public class FileDataTools
	{
		internal ILogger Logger => LogManager.GetLogger();

		#region Properties

		protected string PluginName { get; }
		protected string ClientName { get; }
		public Action<DateTime> ShowNotificationOldDataHandler;
		protected int MaxRetryAttempts { get; set; } = 3;
		protected int RetryDelayMs { get; set; } = 100;

		#endregion

		#region Constructor

		public FileDataTools(string pluginName, string clientName)
		{
			PluginName = pluginName;
			ClientName = clientName;
		}

		#endregion

		#region Methods

		public T LoadData<T>(string filePath, int minutes) where T : class
		{
			return LoadDataAsync<T>(filePath, minutes).GetAwaiter().GetResult();
		}

		public async Task<T> LoadDataAsync<T>(string filePath, int minutes) where T : class
		{
			Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));

			if (!File.Exists(filePath))
			{
				return null;
			}

			return await ExecuteWithRetryAsync(async () =>
			{
				var fileInfo = new FileInfo(filePath);
				DateTime dateLastWrite = fileInfo.LastWriteTime;

				if (minutes > 0 && dateLastWrite.AddMinutes(minutes) <= DateTime.Now)
				{
					return null;
				}

				if (minutes == 0)
				{
					API.Instance.MainView.UIDispatcher?.Invoke(new Action(() =>
					{
						ShowNotificationOldData(dateLastWrite);
					}));
				}

				using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (var reader = new StreamReader(fileStream, Encoding.UTF8))
				{
					string jsonContent = await reader.ReadToEndAsync().ConfigureAwait(false);
					return await Task.Run(() => Serialization.FromJson<T>(jsonContent)).ConfigureAwait(false);
				}
			}, filePath).ConfigureAwait(false);
		}

		public bool SaveData<T>(string filePath, T data) where T : class
		{
			return SaveDataAsync(filePath, data).GetAwaiter().GetResult();
		}

		public async Task<bool> SaveDataAsync<T>(string filePath, T data) where T : class
		{
			Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));

			if (data == null)
			{
				return false;
			}

			string tempFilePath = filePath + ".tmp";

			try
			{
				return await ExecuteWithRetryAsync(async () =>
				{
					FileSystem.PrepareSaveFile(filePath);

					string content = data is string s ? s : Serialization.ToJson(data);

					using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
					using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
					{
						await writer.WriteAsync(content).ConfigureAwait(false);
						await writer.FlushAsync().ConfigureAwait(false);
						fileStream.Flush(true);
					}

					await Task.Run(() =>
					{
						if (File.Exists(filePath))
						{
							File.Delete(filePath);
						}
						File.Move(tempFilePath, filePath);
					}).ConfigureAwait(false);

					return true;
				}, filePath).ConfigureAwait(false);
			}
			finally
			{
				if (File.Exists(tempFilePath))
				{
					try
					{
						File.Delete(tempFilePath);
					}
					catch
					{
						// Ignore cleanup errors
					}
				}
			}
		}

		private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string filePath)
		{
			for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
			{
				try
				{
					return await operation().ConfigureAwait(false);
				}
				catch (IOException ex) when (IsFileLocked(ex))
				{
					if (attempt < MaxRetryAttempts - 1)
					{
						Logger.Debug($"File is locked, retrying in {RetryDelayMs}ms (attempt {attempt + 1}/{MaxRetryAttempts}): {filePath}");
						await Task.Delay(RetryDelayMs).ConfigureAwait(false);
						continue;
					}
					Logger.Warn($"Failed to access file after {MaxRetryAttempts} attempts: {filePath}");
					Common.LogError(ex, false, true, PluginName);
					return default;
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, true, PluginName);
					return default;
				}
			}

			return default;
		}

		private bool IsFileLocked(IOException exception)
		{
			int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & 0xFFFF;
			return errorCode == 32 || errorCode == 33;
		}

		#endregion

		#region Notifications

		public virtual void ShowNotificationOldData(DateTime dateLastWrite)
		{
			if (ShowNotificationOldDataHandler != null)
			{
				ShowNotificationOldDataHandler(dateLastWrite);
				return;
			}

			LocalDateTimeConverter localDateTimeConverter = new LocalDateTimeConverter();
			string formatedDateLastWrite = localDateTimeConverter.Convert(dateLastWrite, null, null, CultureInfo.CurrentCulture).ToString();
			Logger.Warn($"Use saved UserData - {formatedDateLastWrite}");
			API.Instance.Notifications.Add(new NotificationMessage(
				$"{PluginName}-{(ClientName.IsNullOrEmpty() ? PluginName.RemoveWhiteSpace() : ClientName.RemoveWhiteSpace())}-LoadFileData",
				$"{PluginName}" + Environment.NewLine
					+ string.Format(ResourceProvider.GetString("LOCCommonNotificationOldData"), ClientName.IsNullOrEmpty() ? PluginName : ClientName, formatedDateLastWrite),
				NotificationType.Info
			));
		}

		#endregion
	}
}