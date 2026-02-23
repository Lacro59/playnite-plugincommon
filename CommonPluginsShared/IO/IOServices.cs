using Ardalis.GuardClauses;
using CommonPlayniteShared.Common;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsShared.IO
{
	#region Path Validation

	/// <summary>
	/// Provides path validation, sanitization, and normalization utilities.
	/// Ensures paths are safe for file system operations and compatible with Windows.
	/// </summary>
	public static class PathValidator
	{
		#region Sanitization

		/// <summary>
		/// Sanitizes a complete path by cleaning each component.
		/// </summary>
		/// <param name="path">Path to sanitize</param>
		/// <param name="lastIsName">True if last component is a filename</param>
		/// <returns>Sanitized path</returns>
		/// <example>
		/// GetSafePath(@"C:\My*Folder\File?.txt", true) => @"C:\My Folder\File .txt"
		/// </example>
		public static string GetSafePath(string path, bool lastIsName = false)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return path;
			}

			string pathReturn = string.Empty;
			List<string> pathFolders = path.Split('\\').ToList();

			for (int i = 0; i < pathFolders.Count; i++)
			{
				string folder = pathFolders[i];

				if (pathReturn.IsNullOrEmpty())
				{
					pathReturn += folder;
				}
				else
				{
					bool isLastComponent = i == pathFolders.Count - 1;
					bool keepNameSpace = !isLastComponent || !lastIsName;
					pathReturn += "\\" + GetSafePathName(folder, keepNameSpace);
				}
			}

			return pathReturn;
		}

		/// <summary>
		/// Removes invalid characters from a filename or directory name.
		/// </summary>
		/// <param name="filename">Filename to sanitize</param>
		/// <param name="keepNameSpace">True to replace with spaces, false for aggressive cleaning</param>
		/// <returns>Sanitized filename</returns>
		public static string GetSafePathName(string filename, bool keepNameSpace = false)
		{
			if (string.IsNullOrWhiteSpace(filename))
			{
				return filename;
			}

			return keepNameSpace
				? string.Join(" ", filename.Split(Path.GetInvalidFileNameChars())).Trim()
				: CommonPlayniteShared.Common.Paths.GetSafePathName(filename);
		}

		#endregion

		#region Validation and Normalization

		/// <summary>
		/// Validates and normalizes a file path with automatic fixes.
		/// Performs sanitization, separator fixing, and long path handling.
		/// </summary>
		/// <param name="filePath">File path to process</param>
		/// <param name="ensureDirectoryExists">Create directory if missing</param>
		/// <returns>Normalized path or null if invalid</returns>
		public static string ValidateAndNormalizePath(string filePath, bool ensureDirectoryExists = false)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return null;
			}

			try
			{
				filePath = CommonPlayniteShared.Common.Paths.FixSeparators(filePath);

				string directory = Path.GetDirectoryName(filePath);
				string fileName = Path.GetFileName(filePath);

				if (string.IsNullOrWhiteSpace(fileName))
				{
					return null;
				}

				string safeFileName = GetSafePathName(fileName, keepNameSpace: false);
				if (string.IsNullOrWhiteSpace(safeFileName))
				{
					return null;
				}

				string normalizedPath = !string.IsNullOrWhiteSpace(directory)
					? Path.Combine(directory, safeFileName)
					: safeFileName;

				normalizedPath = CommonPlayniteShared.Common.Paths.FixSeparators(normalizedPath);

				if (!CommonPlayniteShared.Common.Paths.IsValidFilePath(normalizedPath))
				{
					return null;
				}

				normalizedPath = CommonPlayniteShared.Common.Paths.FixPathLength(normalizedPath);

				if (ensureDirectoryExists && !string.IsNullOrWhiteSpace(directory))
				{
					string normalizedDirectory = CommonPlayniteShared.Common.Paths.FixPathLength(directory);
					if (!Directory.Exists(normalizedDirectory))
					{
						Directory.CreateDirectory(normalizedDirectory);
					}
				}

				return normalizedPath;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Validates and normalizes with strict error handling (throws on failure).
		/// </summary>
		/// <param name="filePath">File path to validate</param>
		/// <param name="ensureDirectoryExists">Create directory if missing</param>
		/// <returns>Validated path</returns>
		/// <exception cref="ArgumentException">Thrown if path is invalid</exception>
		public static string ValidateAndNormalizePathStrict(string filePath, bool ensureDirectoryExists = false)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
			}

			string validatedPath = ValidateAndNormalizePath(filePath, ensureDirectoryExists);
			if (validatedPath == null)
			{
				throw new ArgumentException($"Invalid file path: {filePath}", nameof(filePath));
			}

			return validatedPath;
		}

		#endregion

		#region Path Creation

		/// <summary>
		/// Creates a safe file path by combining directory and filename with automatic sanitization.
		/// </summary>
		/// <param name="directory">Target directory</param>
		/// <param name="fileName">Filename (will be sanitized)</param>
		/// <param name="ensureDirectoryExists">Create directory if missing</param>
		/// <returns>Complete validated file path</returns>
		/// <exception cref="ArgumentException">Thrown if inputs are invalid</exception>
		public static string CreateSafePath(string directory, string fileName, bool ensureDirectoryExists = true)
		{
			if (string.IsNullOrWhiteSpace(directory))
			{
				throw new ArgumentException("Directory cannot be null or empty", nameof(directory));
			}

			if (string.IsNullOrWhiteSpace(fileName))
			{
				throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
			}

			string safeFileName = GetSafePathName(fileName, keepNameSpace: false);
			if (string.IsNullOrWhiteSpace(safeFileName))
			{
				throw new ArgumentException($"File name contains only invalid characters: {fileName}", nameof(fileName));
			}

			string filePath = Path.Combine(directory, safeFileName);
			return ValidateAndNormalizePathStrict(filePath, ensureDirectoryExists);
		}

		/// <summary>
		/// Creates a safe path from a complete path string (sanitizes all components).
		/// </summary>
		/// <param name="fullPath">Complete path to sanitize</param>
		/// <param name="ensureDirectoryExists">Create directory if missing</param>
		/// <returns>Complete sanitized path</returns>
		/// <exception cref="ArgumentException">Thrown if path is invalid</exception>
		public static string CreateSafePathFromFullPath(string fullPath, bool ensureDirectoryExists = true)
		{
			if (string.IsNullOrWhiteSpace(fullPath))
			{
				throw new ArgumentException("Path cannot be null or empty", nameof(fullPath));
			}

			string safePath = GetSafePath(fullPath, lastIsName: true);
			return ValidateAndNormalizePathStrict(safePath, ensureDirectoryExists);
		}

		#endregion

		#region Validation Checks

		/// <summary>
		/// Checks if a path is valid without normalizing it.
		/// </summary>
		/// <param name="filePath">Path to check</param>
		/// <returns>True if valid, false otherwise</returns>
		public static bool IsPathValid(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return false;
			}

			try
			{
				filePath = CommonPlayniteShared.Common.Paths.FixSeparators(filePath);
				return CommonPlayniteShared.Common.Paths.IsValidFilePath(filePath);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Checks if a filename contains invalid characters.
		/// </summary>
		/// <param name="fileName">Filename to check</param>
		/// <returns>True if valid, false otherwise</returns>
		public static bool IsFileNameValid(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
			{
				return false;
			}

			char[] invalidChars = Path.GetInvalidFileNameChars();
			return !fileName.Any(c => invalidChars.Contains(c));
		}

		/// <summary>
		/// Checks if a path requires sanitization.
		/// </summary>
		/// <param name="path">Path to check</param>
		/// <returns>True if path needs sanitization, false otherwise</returns>
		public static bool NeedsSanitization(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return false;
			}

			try
			{
				string fileName = Path.GetFileName(path);
				if (!string.IsNullOrWhiteSpace(fileName))
				{
					return !IsFileNameValid(fileName);
				}

				return false;
			}
			catch
			{
				return true;
			}
		}

		#endregion
	}

	#endregion

	#region File Data Tools

	/// <summary>
	/// Provides robust file I/O operations with retry logic, validation, and JSON serialization.
	/// Designed for plugin data persistence with automatic error handling.
	/// </summary>
	public class FileDataService
	{
		private readonly ILogger _logger = LogManager.GetLogger();

		#region Properties

		/// <summary>
		/// Gets the plugin name for logging purposes.
		/// </summary>
		protected string PluginName { get; }

		/// <summary>
		/// Gets the client name associated with this service.
		/// </summary>
		protected string ClientName { get; }

		/// <summary>
		/// Gets or sets the handler for old data notifications.
		/// </summary>
		public Action<DateTime> ShowNotificationOldDataHandler { get; set; }

		/// <summary>
		/// Gets or sets the maximum retry attempts for file operations.
		/// Default: 3
		/// </summary>
		protected int MaxRetryAttempts { get; set; } = 3;

		/// <summary>
		/// Gets or sets the delay between retry attempts in milliseconds.
		/// Default: 100ms
		/// </summary>
		protected int RetryDelayMs { get; set; } = 100;

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new file data service.
		/// </summary>
		/// <param name="pluginName">Name of the plugin</param>
		/// <param name="clientName">Name of the client</param>
		public FileDataService(string pluginName, string clientName)
		{
			PluginName = pluginName;
			ClientName = clientName;
		}

		#endregion

		#region Load Operations

		/// <summary>
		/// Loads data from a file synchronously.
		/// </summary>
		/// <typeparam name="T">Type of data to load</typeparam>
		/// <param name="filePath">Path to the file</param>
		/// <param name="minutes">Maximum file age in minutes (0 = show notification, >0 = reject old data)</param>
		/// <returns>Deserialized data or null if unavailable/too old</returns>
		public T LoadData<T>(string filePath, int minutes) where T : class
		{
			return LoadDataAsync<T>(filePath, minutes).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Loads data from a file asynchronously with age validation.
		/// </summary>
		/// <typeparam name="T">Type of data to load</typeparam>
		/// <param name="filePath">Path to the file</param>
		/// <param name="minutes">Maximum file age in minutes</param>
		/// <returns>Deserialized data or null</returns>
		public async Task<T> LoadDataAsync<T>(string filePath, int minutes) where T : class
		{
			Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));

			filePath = PathValidator.ValidateAndNormalizePath(filePath);
			if (filePath == null)
			{
				_logger.Warn("Invalid file path provided to LoadDataAsync");
				return null;
			}

			if (!File.Exists(filePath))
			{
				return null;
			}

			return await ExecuteWithRetryAsync(async () =>
			{
				FileInfo fileInfo = new FileInfo(filePath);
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

				using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (StreamReader reader = new StreamReader(fileStream, Encoding.UTF8))
				{
					string jsonContent = await reader.ReadToEndAsync().ConfigureAwait(false);
					return Serialization.FromJson<T>(jsonContent);
				}
			}, filePath).ConfigureAwait(false);
		}

		#endregion

		#region Save Operations

		/// <summary>
		/// Saves data to a file synchronously.
		/// </summary>
		/// <typeparam name="T">Type of data to save</typeparam>
		/// <param name="filePath">Path to the file</param>
		/// <param name="data">Data to save</param>
		/// <returns>True if successful, false otherwise</returns>
		public bool SaveData<T>(string filePath, T data) where T : class
		{
			return SaveDataAsync(filePath, data).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Saves data to a file asynchronously using atomic write (temp file + replace).
		/// Ensures data integrity by using temporary files.
		/// </summary>
		/// <typeparam name="T">Type of data to save</typeparam>
		/// <param name="filePath">Path to the file</param>
		/// <param name="data">Data to save</param>
		/// <returns>True if successful, false otherwise</returns>
		public async Task<bool> SaveDataAsync<T>(string filePath, T data) where T : class
		{
			Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));

			if (data == null)
			{
				return false;
			}

			filePath = PathValidator.ValidateAndNormalizePath(filePath, ensureDirectoryExists: true);
			if (filePath == null)
			{
				_logger.Warn("Invalid file path provided to SaveDataAsync");
				return false;
			}

			string tempFilePath = filePath + ".tmp";
			tempFilePath = PathValidator.ValidateAndNormalizePath(tempFilePath);

			try
			{
				return await ExecuteWithRetryAsync(async () =>
				{
					FileSystem.PrepareSaveFile(filePath);

					string content = data is string s ? s : Serialization.ToJson(data);

					using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
					using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8))
					{
						await writer.WriteAsync(content).ConfigureAwait(false);
						await writer.FlushAsync().ConfigureAwait(false);
						fileStream.Flush(true);
					}

					if (File.Exists(filePath))
					{
						File.Replace(tempFilePath, filePath, null);
					}
					else
					{
						File.Move(tempFilePath, filePath);
					}

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

		/// <summary>
		/// Saves data with automatic filename sanitization.
		/// </summary>
		/// <typeparam name="T">Type of data to save</typeparam>
		/// <param name="directory">Target directory</param>
		/// <param name="fileName">Filename (will be sanitized)</param>
		/// <param name="data">Data to save</param>
		/// <returns>True if successful, false otherwise</returns>
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
				string filePath = PathValidator.CreateSafePath(directory, fileName, ensureDirectoryExists: true);
				return await SaveDataAsync(filePath, data);
			}
			catch (ArgumentException ex)
			{
				_logger.Error($"Failed to create safe path: {ex.Message}");
				return false;
			}
		}

		#endregion

		#region Retry Logic

		/// <summary>
		/// Executes an operation with automatic retry on file lock errors.
		/// </summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="operation">Operation to execute</param>
		/// <param name="filePath">File path for logging</param>
		/// <returns>Operation result or default(T) on failure</returns>
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
						_logger.Debug($"File locked, retry {attempt + 1}/{MaxRetryAttempts}: {filePath}");
						await Task.Delay(RetryDelayMs).ConfigureAwait(false);
						continue;
					}

					_logger.Warn($"Failed after {MaxRetryAttempts} attempts: {filePath}");
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
		/// Checks if an IOException is caused by file locking.
		/// </summary>
		/// <param name="exception">Exception to check</param>
		/// <returns>True if file is locked, false otherwise</returns>
		private bool IsFileLocked(IOException exception)
		{
			int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & 0xFFFF;
			return errorCode == 32 || errorCode == 33;
		}

		#endregion

		#region Notifications

		/// <summary>
		/// Displays a notification about old cached data being used.
		/// </summary>
		/// <param name="dateLastWrite">Last modification date of the data</param>
		public virtual void ShowNotificationOldData(DateTime dateLastWrite)
		{
			if (ShowNotificationOldDataHandler != null)
			{
				ShowNotificationOldDataHandler(dateLastWrite);
				return;
			}

			LocalDateTimeConverter converter = new LocalDateTimeConverter();
			string formattedDate = converter.Convert(dateLastWrite, null, null, CultureInfo.CurrentCulture).ToString();

			_logger.Warn($"Using saved data from {formattedDate}");

			API.Instance.Notifications.Add(new NotificationMessage(
				$"{PluginName}-{(ClientName.IsNullOrEmpty() ? PluginName.RemoveWhiteSpace() : ClientName.RemoveWhiteSpace())}-LoadFileData",
				$"{PluginName}" + Environment.NewLine +
				string.Format(ResourceProvider.GetString("LOCCommonNotificationOldData"),
					ClientName.IsNullOrEmpty() ? PluginName : ClientName,
					formattedDate),
				NotificationType.Info
			));
		}

		#endregion
	}

	#endregion
}