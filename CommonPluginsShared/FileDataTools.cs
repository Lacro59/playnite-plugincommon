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
	/// <summary>
	/// Provides methods for loading and saving data to files with retry logic and validation.
	/// </summary>
	public class FileDataTools
	{
		internal ILogger Logger => LogManager.GetLogger();

		#region Properties

		/// <summary>
		/// Gets the name of the plugin using this tool.
		/// </summary>
		protected string PluginName { get; }

		/// <summary>
		/// Gets the name of the client associated with the plugin.
		/// </summary>
		protected string ClientName { get; }

		/// <summary>
		/// Gets or sets the handler for displaying notifications about old data.
		/// </summary>
		public Action<DateTime> ShowNotificationOldDataHandler;

		/// <summary>
		/// Gets or sets the maximum number of retry attempts for file operations.
		/// Default is 3.
		/// </summary>
		protected int MaxRetryAttempts { get; set; } = 3;

		/// <summary>
		/// Gets or sets the delay in milliseconds between retry attempts.
		/// Default is 100ms.
		/// </summary>
		protected int RetryDelayMs { get; set; } = 100;

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="FileDataTools"/> class.
		/// </summary>
		/// <param name="pluginName">The name of the plugin.</param>
		/// <param name="clientName">The name of the client.</param>
		public FileDataTools(string pluginName, string clientName)
		{
			PluginName = pluginName;
			ClientName = clientName;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Loads data from a file synchronously.
		/// </summary>
		/// <typeparam name="T">The type of data to load.</typeparam>
		/// <param name="filePath">The path to the file.</param>
		/// <param name="minutes">The maximum age of the file in minutes. 
		/// If 0, displays old data notification. If greater than 0, returns null if file is older than specified minutes.</param>
		/// <returns>The deserialized data, or null if the file doesn't exist or is too old.</returns>
		public T LoadData<T>(string filePath, int minutes) where T : class
		{
			return LoadDataAsync<T>(filePath, minutes).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Loads data from a file asynchronously.
		/// </summary>
		/// <typeparam name="T">The type of data to load.</typeparam>
		/// <param name="filePath">The path to the file.</param>
		/// <param name="minutes">The maximum age of the file in minutes. 
		/// If 0, displays old data notification. If greater than 0, returns null if file is older than specified minutes.</param>
		/// <returns>A task representing the asynchronous operation. The task result contains the deserialized data, 
		/// or null if the file doesn't exist or is too old.</returns>
		public async Task<T> LoadDataAsync<T>(string filePath, int minutes) where T : class
		{
			Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));

			// Validate and normalize the complete path
			filePath = PathValidator.ValidateAndNormalizePath(filePath);

			if (filePath == null)
			{
				Logger.Warn($"Invalid file path provided to LoadDataAsync");
				return null;
			}

			if (!File.Exists(filePath))
			{
				return null;
			}

			return await ExecuteWithRetryAsync(async () =>
			{
				var fileInfo = new FileInfo(filePath);
				DateTime dateLastWrite = fileInfo.LastWriteTime;

				// Check if file is too old
				if (minutes > 0 && dateLastWrite.AddMinutes(minutes) <= DateTime.Now)
				{
					return null;
				}

				// Show notification for old data if minutes is 0
				if (minutes == 0)
				{
					API.Instance.MainView.UIDispatcher?.Invoke(new Action(() =>
					{
						ShowNotificationOldData(dateLastWrite);
					}));
				}

				// Read and deserialize the file
				using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (var reader = new StreamReader(fileStream, Encoding.UTF8))
				{
					string jsonContent = await reader.ReadToEndAsync().ConfigureAwait(false);
					return Serialization.FromJson<T>(jsonContent);
				}
			}, filePath).ConfigureAwait(false);
		}

		/// <summary>
		/// Saves data to a file synchronously.
		/// </summary>
		/// <typeparam name="T">The type of data to save.</typeparam>
		/// <param name="filePath">The path to the file.</param>
		/// <param name="data">The data to save.</param>
		/// <returns>True if the save operation succeeded, otherwise false.</returns>
		public bool SaveData<T>(string filePath, T data) where T : class
		{
			return SaveDataAsync(filePath, data).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Saves data to a file asynchronously.
		/// Uses a temporary file and atomic replacement to ensure data integrity.
		/// </summary>
		/// <typeparam name="T">The type of data to save.</typeparam>
		/// <param name="filePath">The path to the file.</param>
		/// <param name="data">The data to save.</param>
		/// <returns>A task representing the asynchronous operation. The task result is true if the save succeeded, otherwise false.</returns>
		public async Task<bool> SaveDataAsync<T>(string filePath, T data) where T : class
		{
			Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));

			if (data == null)
			{
				return false;
			}

			// Validate and normalize the complete path + create directory if necessary
			filePath = PathValidator.ValidateAndNormalizePath(filePath, ensureDirectoryExists: true);

			if (filePath == null)
			{
				Logger.Warn($"Invalid file path provided to SaveDataAsync");
				return false;
			}

			// Create temporary file path
			string tempFilePath = filePath + ".tmp";
			tempFilePath = PathValidator.ValidateAndNormalizePath(tempFilePath);

			try
			{
				return await ExecuteWithRetryAsync(async () =>
				{
					FileSystem.PrepareSaveFile(filePath);

					// Serialize data to string
					string content = data is string s ? s : Serialization.ToJson(data);

					// Write to temporary file
					using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
					using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
					{
						await writer.WriteAsync(content).ConfigureAwait(false);
						await writer.FlushAsync().ConfigureAwait(false);
						fileStream.Flush(true);
					}

					if (File.Exists(filePath))
					{
						// Atomic replacement with backup (backup is automatically deleted after successful replace)
						File.Replace(tempFilePath, filePath, null);
					}
					else
					{
						// If destination doesn't exist, just move the temp file
						File.Move(tempFilePath, filePath);
					}

					return true;
				}, filePath).ConfigureAwait(false);
			}
			finally
			{
				// Clean up temporary file if it still exists
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

		/// <summary>
		/// Saves data with a dynamically generated filename.
		/// The filename will be automatically cleaned of invalid characters.
		/// </summary>
		/// <typeparam name="T">The type of data to save.</typeparam>
		/// <param name="directory">The directory where the file will be saved.</param>
		/// <param name="fileName">The filename (will be sanitized automatically).</param>
		/// <param name="data">The data to save.</param>
		/// <returns>A task representing the asynchronous operation. The task result is true if the save succeeded, otherwise false.</returns>
		public async Task<bool> SaveDataWithSafeNameAsync<T>(string directory, string fileName, T data) where T : class
		{
			Guard.Against.NullOrWhiteSpace(directory, nameof(directory));
			Guard.Against.NullOrWhiteSpace(fileName, nameof(fileName));

			if (data == null)
			{
				return false;
			}

			try
			{
				// Create a safe path with automatic filename sanitization
				string filePath = PathValidator.CreateSafePath(directory, fileName, ensureDirectoryExists: true);
				return await SaveDataAsync(filePath, data);
			}
			catch (ArgumentException ex)
			{
				Logger.Error($"Failed to create safe path: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Executes a file operation with retry logic for handling file locks.
		/// </summary>
		/// <typeparam name="T">The return type of the operation.</typeparam>
		/// <param name="operation">The operation to execute.</param>
		/// <param name="filePath">The file path being accessed (for logging).</param>
		/// <returns>The result of the operation, or default(T) if all retries fail.</returns>
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

		/// <summary>
		/// Determines if an IOException is caused by a file lock.
		/// </summary>
		/// <param name="exception">The IOException to check.</param>
		/// <returns>True if the exception is caused by a file lock, otherwise false.</returns>
		private bool IsFileLocked(IOException exception)
		{
			int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & 0xFFFF;
			// Error code 32: The process cannot access the file because it is being used by another process
			// Error code 33: The process cannot access the file because another process has locked a portion of the file
			return errorCode == 32 || errorCode == 33;
		}

		#endregion

		#region Notifications

		/// <summary>
		/// Displays a notification about old data being used.
		/// Can be overridden or replaced via ShowNotificationOldDataHandler.
		/// </summary>
		/// <param name="dateLastWrite">The last write date of the old data.</param>
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