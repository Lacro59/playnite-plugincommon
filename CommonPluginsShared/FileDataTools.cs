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

		/// <summary>
		/// Plugin name associated with this tool.
		/// </summary>
		protected string PluginName { get; }

		/// <summary>
		/// Client name used for filtering or identification.
		/// </summary>
		protected string ClientName { get; }

		public Action<DateTime> ShowNotificationOldDataHandler;

		/// <summary>
		/// Maximum number of retry attempts when file is locked.
		/// </summary>
		protected int MaxRetryAttempts { get; set; } = 3;

		/// <summary>
		/// Delay in milliseconds between retry attempts.
		/// </summary>
		protected int RetryDelayMs { get; set; } = 100;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor to initialize file data tool settings.
		/// </summary>
		public FileDataTools(string pluginName, string clientName)
		{
			PluginName = pluginName;
			ClientName = clientName;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Generic method to load data from file with optional age validation.
		/// Implements retry logic for locked files.
		/// </summary>
		/// <typeparam name="T">Type of data to load</typeparam>
		/// <param name="filePath">Path to the data file</param>
		/// <param name="minutes">Maximum age in minutes (0 to ignore age, positive to validate freshness)</param>
		/// <returns>Loaded data of type T or null if loading fails or data is too old</returns>
		public T LoadData<T>(string filePath, int minutes) where T : class
		{
			Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));

			if (!File.Exists(filePath))
			{
				return null;
			}

			for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
			{
				try
				{
					DateTime dateLastWrite = File.GetLastWriteTime(filePath);

					if (minutes > 0 && dateLastWrite.AddMinutes(minutes) <= DateTime.Now)
					{
						return null;
					}

					if (minutes == 0)
					{
						ShowNotificationOldData(dateLastWrite);
					}

					// Use FileShare.ReadWrite to allow other processes to access the file
					using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (var reader = new StreamReader(fileStream, Encoding.UTF8))
					{
						string jsonContent = reader.ReadToEnd();
						return Serialization.FromJson<T>(jsonContent);
					}
				}
				catch (IOException ex) when (IsFileLocked(ex))
				{
					if (attempt < MaxRetryAttempts - 1)
					{
						Logger.Debug($"File is locked, retrying in {RetryDelayMs}ms (attempt {attempt + 1}/{MaxRetryAttempts}): {filePath}");
						Thread.Sleep(RetryDelayMs);
						continue;
					}
					Logger.Warn($"Failed to load file after {MaxRetryAttempts} attempts: {filePath}");
					Common.LogError(ex, false, true, PluginName);
					return null;
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, true, PluginName);
					return null;
				}
			}

			return null;
		}

		/// <summary>
		/// Asynchronously loads data from file with optional age validation.
		/// Implements retry logic for locked files with async delays.
		/// </summary>
		/// <typeparam name="T">Type of data to load</typeparam>
		/// <param name="filePath">Path to the data file</param>
		/// <param name="minutes">Maximum age in minutes (0 to ignore age, positive to validate freshness)</param>
		/// <returns>Task that returns loaded data of type T or null if loading fails or data is too old</returns>
		public async Task<T> LoadDataAsync<T>(string filePath, int minutes) where T : class
		{
			Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));

			if (!File.Exists(filePath))
			{
				return null;
			}

			for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
			{
				try
				{
					var fileInfo = new FileInfo(filePath);
					DateTime dateLastWrite = fileInfo.LastWriteTime;

					if (minutes > 0 && dateLastWrite.AddMinutes(minutes) <= DateTime.Now)
					{
						return null;
					}

					if (minutes == 0)
					{
						ShowNotificationOldData(dateLastWrite);
					}

					// Use FileShare.ReadWrite to allow other processes to access the file
					using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (var reader = new StreamReader(fileStream, Encoding.UTF8))
					{
						string jsonContent = await reader.ReadToEndAsync();
						return await Task.Run(() => Serialization.FromJson<T>(jsonContent));
					}
				}
				catch (IOException ex) when (IsFileLocked(ex))
				{
					if (attempt < MaxRetryAttempts - 1)
					{
						Logger.Debug($"File is locked, retrying in {RetryDelayMs}ms (attempt {attempt + 1}/{MaxRetryAttempts}): {filePath}");
						await Task.Delay(RetryDelayMs);
						continue;
					}
					Logger.Warn($"Failed to load file after {MaxRetryAttempts} attempts: {filePath}");
					Common.LogError(ex, false, true, PluginName);
					return null;
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, true, PluginName);
					return null;
				}
			}

			return null;
		}

		/// <summary>
		/// Saves data to file in JSON format or as raw text.
		/// Implements retry logic for locked files and atomic write operations.
		/// </summary>
		/// <typeparam name="T">Type of data to save</typeparam>
		/// <param name="filePath">Path to the data file</param>
		/// <param name="data">Data object to save</param>
		/// <returns>True if save was successful, false otherwise</returns>
		public bool SaveData<T>(string filePath, T data) where T : class
		{
			Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));

			if (data == null)
			{
				return false;
			}

			string tempFilePath = filePath + ".tmp";

			for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
			{
				try
				{
					FileSystem.PrepareSaveFile(filePath);

					string content = data is string s ? s : Serialization.ToJson(data);

					// Write to temporary file first (atomic operation)
					using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
					using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
					{
						writer.Write(content);
						writer.Flush();
						fileStream.Flush(true); // Force flush to disk
					}

					// Replace original file with temp file (atomic on most systems)
					if (File.Exists(filePath))
					{
						File.Delete(filePath);
					}
					File.Move(tempFilePath, filePath);

					return true;
				}
				catch (IOException ex) when (IsFileLocked(ex))
				{
					if (attempt < MaxRetryAttempts - 1)
					{
						Logger.Debug($"File is locked, retrying in {RetryDelayMs}ms (attempt {attempt + 1}/{MaxRetryAttempts}): {filePath}");
						Thread.Sleep(RetryDelayMs);
						continue;
					}
					Logger.Warn($"Failed to save file after {MaxRetryAttempts} attempts: {filePath}");
					Common.LogError(ex, false, true, PluginName);
					return false;
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, true, PluginName);
					return false;
				}
				finally
				{
					// Clean up temp file if it still exists
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

			return false;
		}

		/// <summary>
		/// Asynchronously saves data to file in JSON format or as raw text.
		/// Implements retry logic for locked files and atomic write operations.
		/// </summary>
		/// <typeparam name="T">Type of data to save</typeparam>
		/// <param name="filePath">Path to the data file</param>
		/// <param name="data">Data object to save</param>
		/// <returns>Task that returns true if save was successful, false otherwise</returns>
		public async Task<bool> SaveDataAsync<T>(string filePath, T data) where T : class
		{
			Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));

			if (data == null)
			{
				return false;
			}

			string tempFilePath = filePath + ".tmp";

			for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
			{
				try
				{
					FileSystem.PrepareSaveFile(filePath);

					string content = data is string s ? s : Serialization.ToJson(data);

					// Write to temporary file first (atomic operation)
					using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
					using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
					{
						await writer.WriteAsync(content);
						await writer.FlushAsync();
						fileStream.Flush(true); // Force flush to disk
					}

					// Replace original file with temp file (atomic on most systems)
					await Task.Run(() =>
					{
						if (File.Exists(filePath))
						{
							File.Delete(filePath);
						}
						File.Move(tempFilePath, filePath);
					});

					return true;
				}
				catch (IOException ex) when (IsFileLocked(ex))
				{
					if (attempt < MaxRetryAttempts - 1)
					{
						Logger.Debug($"File is locked, retrying in {RetryDelayMs}ms (attempt {attempt + 1}/{MaxRetryAttempts}): {filePath}");
						await Task.Delay(RetryDelayMs);
						continue;
					}
					Logger.Warn($"Failed to save file after {MaxRetryAttempts} attempts: {filePath}");
					Common.LogError(ex, false, true, PluginName);
					return false;
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, true, PluginName);
					return false;
				}
				finally
				{
					// Clean up temp file if it still exists
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

			return false;
		}

		/// <summary>
		/// Determines if an IOException is caused by a file lock.
		/// </summary>
		/// <param name="exception">The IOException to check</param>
		/// <returns>True if the exception indicates a file lock, false otherwise</returns>
		private bool IsFileLocked(IOException exception)
		{
			int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & 0xFFFF;
			// ERROR_SHARING_VIOLATION = 32, ERROR_LOCK_VIOLATION = 33
			return errorCode == 32 || errorCode == 33;
		}

		#endregion

		#region Notifications

		/// <summary>
		/// Shows a notification to the user about using old cached data.
		/// </summary>
		/// <param name="dateLastWrite">The date when the data was last updated</param>
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