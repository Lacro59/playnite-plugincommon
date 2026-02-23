using CommonPlayniteShared.Common;
using CommonPluginsShared.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CommonPluginsShared
{
	/// <summary>
	/// Provides shared helper methods for resource loading, logging and common UI helpers.
	/// </summary>
	public class Common
	{
		private static readonly ILogger Logger = LogManager.GetLogger();

		/// <summary>
		/// Load common resources (localization, styles, fonts) from the plugin directory.
		/// </summary>
		/// <param name="pluginFolder">Path to the plugin folder.</param>
		/// <param name="language">Current language code.</param>
		public static void Load(string pluginFolder, string language)
		{
			// Load localization first so XAML bindings can use localized resources.
			PluginLocalization.SetPluginLanguage(pluginFolder, language);

			LoadResourceDictionaries(pluginFolder);
			LoadFontResource(pluginFolder);
		}

		/// <summary>
		/// Load and merge XAML resource dictionaries if they have been modified since the last load.
		/// This avoids re-loading unchanged dictionaries on each startup.
		/// </summary>
		/// <param name="pluginFolder">Path to the plugin folder.</param>
		private static void LoadResourceDictionaries(string pluginFolder)
		{
			if (Application.Current == null)
			{
				Logger.Warn("LoadResourceDictionaries called while Application.Current is null.");
				return;
			}

			var resourceFiles = new List<string>
			{
				Path.Combine(pluginFolder, "Resources\\Common.xaml"),
				Path.Combine(pluginFolder, "Resources\\LiveChartsCommon\\Common.xaml"),
				Path.Combine(pluginFolder, "Resources\\Controls\\ListExtendStyle.xaml")
			};

			foreach (var file in resourceFiles)
			{
				if (!File.Exists(file))
				{
					Logger.Warn(string.Format("File {0} not found", file));
					continue;
				}

				var fileName = Path.GetFileName(file);
				var folderName = Path.GetFileName(Path.GetDirectoryName(file));
				var resourceKey = string.Format("{0}_{1}", folderName, fileName);

				var lastModified = File.GetLastWriteTime(file);
				var lastDate = ResourceProvider.GetResource(resourceKey) as DateTime? ?? default(DateTime);

				// Skip if not modified since last load.
				if (lastModified <= lastDate)
				{
					continue;
				}

				try
				{
					var dictionary = Xaml.FromFile<ResourceDictionary>(file);
					dictionary.Source = new Uri(file, UriKind.Absolute);

					// Clean up empty string resources (often produced by localization bindings).
					var keys = dictionary.Keys.Cast<object>().ToList();
					for (var i = 0; i < keys.Count; i++)
					{
						var key = keys[i];
						var value = dictionary[key] as string;
						if (!string.IsNullOrEmpty(value))
						{
							continue;
						}

						// Only remove if value is a string and is empty.
						if (value != null && value.IsNullOrEmpty())
						{
							dictionary.Remove(key);
						}
					}

					// Track last modified time so we reload only when needed.
					if (Application.Current.Resources.Contains(resourceKey))
					{
						Application.Current.Resources.Remove(resourceKey);
					}

					Application.Current.Resources.Add(resourceKey, lastModified);
					Application.Current.Resources.MergedDictionaries.Add(dictionary);

					LogDebug(true, string.Format("Loaded resource {0} - {1:yyyy-MM-dd HH:mm:ss}", file, lastModified));
				}
				catch (Exception ex)
				{
					LogError(ex, false, string.Format("Failed to load resource dictionary: {0}", file));
				}
			}
		}

		/// <summary>
		/// Load a custom font from the plugin folder if it is newer or not yet loaded.
		/// The font is exposed through Application.Current.Resources as "CommonFont" and "CommonFontSize".
		/// </summary>
		/// <param name="pluginFolder">Path to the plugin folder.</param>
		private static void LoadFontResource(string pluginFolder)
		{
			if (Application.Current == null)
			{
				Logger.Warn("LoadFontResource called while Application.Current is null.");
				return;
			}

			var fontPath = Path.Combine(pluginFolder, "Resources\\font.ttf");
			if (!File.Exists(fontPath))
			{
				Logger.Warn(string.Format("Font file {0} not found", fontPath));
				return;
			}

			var existingSize = ResourceProvider.GetResource("CommonFontSize") as long? ?? 0;
			var newSize = new FileInfo(fontPath).Length;

			// If font size did not change, we consider font as already loaded.
			if (newSize <= existingSize)
			{
				return;
			}

			if (Application.Current.Resources.Contains("CommonFontSize"))
			{
				Application.Current.Resources.Remove("CommonFontSize");
			}

			Application.Current.Resources.Add("CommonFontSize", newSize);

			// Use absolute Uri to ensure proper font loading.
			var fontUri = new Uri(fontPath, UriKind.Absolute);
			var fontFamily = new FontFamily(fontUri, "./#font");

			if (Application.Current.Resources.Contains("CommonFont"))
			{
				Application.Current.Resources.Remove("CommonFont");
			}

			Application.Current.Resources.Add("CommonFont", fontFamily);
		}

		#region Logging

		/// <summary>
		/// Write a debug message.
		/// In release builds, messages marked as ignored will not be logged.
		/// </summary>
		/// <param name="isIgnored">If true, the message is prefixed and only logged in debug builds.</param>
		/// <param name="message">Message to log.</param>
		public static void LogDebug(bool isIgnored, string message)
		{
			if (isIgnored)
			{
				message = string.Format("[Ignored] {0}", message);
			}

#if DEBUG
			Logger.Debug(message);
#else
            if (!isIgnored)
            {
                Logger.Debug(message);
            }
#endif
		}

		/// <summary>
		/// Log an error with optional ignore flag.
		/// </summary>
		public static void LogError(Exception ex, bool isIgnored)
		{
			LogError(ex, isIgnored, string.Empty, false, string.Empty, string.Empty);
		}

		/// <summary>
		/// Log an error with an additional message.
		/// </summary>
		public static void LogError(Exception ex, bool isIgnored, string message)
		{
			LogError(ex, isIgnored, message, false, string.Empty, string.Empty);
		}

		/// <summary>
		/// Log an error and optionally display a user notification.
		/// </summary>
		public static void LogError(Exception ex, bool isIgnored, bool showNotification, string pluginName)
		{
			LogError(ex, isIgnored, string.Empty, showNotification, pluginName, string.Empty);
		}

		/// <summary>
		/// Log an error and optionally display a user notification with a custom message.
		/// </summary>
		public static void LogError(Exception ex, bool isIgnored, bool showNotification, string pluginName, string notificationMessage)
		{
			LogError(ex, isIgnored, string.Empty, showNotification, pluginName, notificationMessage);
		}

		/// <summary>
		/// Log an error with an additional message and optionally display a user notification.
		/// </summary>
		public static void LogError(Exception ex, bool isIgnored, string message, bool showNotification, string pluginName)
		{
			LogError(ex, isIgnored, message, showNotification, pluginName, string.Empty);
		}

		/// <summary>
		/// Main error logging handler.
		/// Builds a contextual message from the stack trace and can emit a Playnite notification.
		/// </summary>
		/// <param name="ex">Exception to log.</param>
		/// <param name="isIgnored">If true, logs are marked as ignored and notifications are suppressed.</param>
		/// <param name="message">Optional additional message.</param>
		/// <param name="showNotification">If true, a Playnite notification is created.</param>
		/// <param name="pluginName">Plugin name used in the notification.</param>
		/// <param name="notificationMessage">Custom notification message; if empty, exception message is used.</param>
		public static void LogError(
			Exception ex,
			bool isIgnored,
			string message,
			bool showNotification,
			string pluginName,
			string notificationMessage)
		{
			var trace = new TraceInfos(ex);

			if (message.IsNullOrEmpty())
			{
				message = !trace.InitialCaller.IsNullOrEmpty()
					? string.Format("Error on {0}()", trace.InitialCaller)
					: "Error on ???";
			}
			else if (!trace.InitialCaller.IsNullOrEmpty())
			{
				message = string.Format("Error on {0}(): {1}", trace.InitialCaller, message);
			}

			if (isIgnored)
			{
				message = string.Format("[Ignored] {0}", message);
			}

			message = string.Format("{0}|{1}|{2}", message, trace.FileName, trace.LineNumber);

#if DEBUG
			Logger.Error(ex, message);
#else
            if (!isIgnored)
            {
                Logger.Error(ex, message);
            }
#endif

			// Do not show notifications for ignored errors.
			if (!showNotification || isIgnored)
			{
				return;
			}

			// Use a deterministic id so the same logical error reuses the same notification.
			var notificationId = BuildNotificationId(ex, pluginName);
			var notificationText = string.Format(
				"{0}{1}{2}",
				pluginName,
				Environment.NewLine,
				notificationMessage.IsNullOrEmpty() ? ex.Message : notificationMessage);

			API.Instance.Notifications.Add(
				new NotificationMessage(
					notificationId,
					notificationText,
					NotificationType.Error,
					() => PlayniteTools.CreateLogPackage(pluginName)));
		}

		/// <summary>
		/// Builds a stable notification id for the same logical error.
		/// Same plugin + same exception type + same message => same id.
		/// </summary>
		/// <param name="ex">Exception used to build the key.</param>
		/// <param name="pluginName">Plugin name.</param>
		/// <returns>Deterministic notification identifier.</returns>
		private static string BuildNotificationId(Exception ex, string pluginName)
		{
			if (string.IsNullOrEmpty(pluginName))
			{
				pluginName = "UnknownPlugin";
			}

			if (ex == null)
			{
				return string.Format("{0}-UnknownException", pluginName);
			}

			// Build a stable, reasonably unique key for this exception.
			var baseKey = string.Format(
				"{0}|{1}|{2}",
				pluginName,
				ex.GetType().FullName,
				ex.Message ?? string.Empty);

			// Hash to avoid very long ids in the Playnite NotificationMessage.
			var hashCode = GetStableHashCode(baseKey);
			return string.Format("{0}-{1}", pluginName, hashCode.ToString("X8"));
		}

		/// <summary>
		/// Stable string hash (deterministic across the process lifetime).
		/// This avoids relying on string.GetHashCode which can differ between runtimes.
		/// </summary>
		/// <param name="text">Input text.</param>
		/// <returns>Deterministic 32-bit hash.</returns>
		private static int GetStableHashCode(string text)
		{
			if (text == null)
			{
				return 0;
			}

			unchecked
			{
				var hash = 23;
				for (var i = 0; i < text.Length; i++)
				{
					hash = (hash * 31) + text[i];
				}

				return hash;
			}
		}

		#endregion

		/// <summary>
		/// Add a single ICO font based resource if it does not already exist.
		/// The resource value is a TextBlock using the shared icon font.
		/// </summary>
		/// <param name="key">Resource key to register.</param>
		/// <param name="text">Glyph text to display.</param>
		public static void AddTextIcoFontResource(string key, string text)
		{
			if (Application.Current == null)
			{
				Logger.Warn("AddTextIcoFontResource called while Application.Current is null.");
				return;
			}

			if (Application.Current.Resources.Contains(key))
			{
				return;
			}

			var fontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily;
			if (fontFamily == null)
			{
				Logger.Warn("FontIcoFont resource not found when adding icon font resource.");
			}

			var textBlock = new TextBlock
			{
				Text = text,
				FontSize = 16,
				FontFamily = fontFamily
			};

			Application.Current.Resources.Add(key, textBlock);
		}

		/// <summary>
		/// Add multiple ICO font based resources.
		/// </summary>
		/// <param name="iconsResourcesToAdd">
		/// Dictionary where the key is the resource key and the value is the glyph text.
		/// </param>
		public static void AddTextIcoFontResource(Dictionary<string, string> iconsResourcesToAdd)
		{
			if (iconsResourcesToAdd == null || iconsResourcesToAdd.Count == 0)
			{
				return;
			}

			foreach (var iconResource in iconsResourcesToAdd)
			{
				AddTextIcoFontResource(iconResource.Key, iconResource.Value);
			}
		}
	}
}