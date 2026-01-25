using CommonPlayniteShared.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CommonPluginsShared
{
	/// <summary>
	/// Provides methods for validating, normalizing, and sanitizing file paths.
	/// </summary>
	public static class PathValidator
	{
		#region Path Sanitization

		/// <summary>
		/// Creates a safe path by sanitizing each component of the path.
		/// </summary>
		/// <param name="path">The path to sanitize.</param>
		/// <param name="lastIsName">If true, treats the last component as a filename and preserves its extension. 
		/// If false, treats all components as directory names.</param>
		/// <returns>A sanitized path with invalid characters removed from each component.</returns>
		/// <example>
		/// GetSafePath(@"C:\My*Folder\File?.txt", true) returns @"C:\My Folder\File .txt"
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
					// First component (usually drive letter like "C:")
					pathReturn += folder;
				}
				else
				{
					// Check if this is the last component and should be treated as a filename
					if (i == pathFolders.Count - 1 && lastIsName)
					{
						// Last component is a filename, don't keep namespace
						pathReturn += "\\" + GetSafePathName(folder, keepNameSpace: false);
					}
					else
					{
						// Middle components are directories, keep namespace
						pathReturn += "\\" + GetSafePathName(folder, keepNameSpace: true);
					}
				}
			}

			return pathReturn;
		}

		/// <summary>
		/// Removes invalid characters from a filename or directory name.
		/// </summary>
		/// <param name="filename">The filename or directory name to sanitize.</param>
		/// <param name="keepNameSpace">If true, replaces invalid characters with spaces. 
		/// If false, uses more aggressive sanitization.</param>
		/// <returns>A sanitized filename with invalid characters removed or replaced.</returns>
		/// <example>
		/// GetSafePathName("my*file?.txt", false) returns "my file .txt"
		/// </example>
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

		#region Path Validation and Normalization

		/// <summary>
		/// Validates and normalizes a complete file path.
		/// Performs validation, sanitization, separator fixing, and long path handling.
		/// </summary>
		/// <param name="filePath">The complete file path to validate and normalize.</param>
		/// <param name="ensureDirectoryExists">If true, creates the directory if it doesn't exist.</param>
		/// <returns>A validated and normalized path, or null if the path is invalid.</returns>
		/// <example>
		/// ValidateAndNormalizePath(@"C:/Data\my*file?.txt", true) 
		/// returns @"C:\Data\my file .txt" and creates C:\Data if needed
		/// </example>
		public static string ValidateAndNormalizePath(string filePath, bool ensureDirectoryExists = false)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return null;
			}

			try
			{
				// Step 1: Fix path separators (convert / to \)
				filePath = CommonPlayniteShared.Common.Paths.FixSeparators(filePath);

				// Step 2: Extract directory and filename
				string directory = Path.GetDirectoryName(filePath);
				string fileName = Path.GetFileName(filePath);

				// Step 3: Verify filename exists
				if (string.IsNullOrWhiteSpace(fileName))
				{
					return null;
				}

				// Step 4: Sanitize the filename (remove invalid characters)
				string safeFileName = GetSafePathName(fileName, keepNameSpace: false);

				if (string.IsNullOrWhiteSpace(safeFileName))
				{
					return null;
				}

				// Step 5: Rebuild the path with sanitized filename
				string normalizedPath;
				if (!string.IsNullOrWhiteSpace(directory))
				{
					normalizedPath = Path.Combine(directory, safeFileName);
				}
				else
				{
					normalizedPath = safeFileName;
				}

				// Step 6: Fix separators again after reconstruction
				normalizedPath = CommonPlayniteShared.Common.Paths.FixSeparators(normalizedPath);

				// Step 7: Validate the path is well-formed
				if (!CommonPlayniteShared.Common.Paths.IsValidFilePath(normalizedPath))
				{
					return null;
				}

				// Step 8: Handle long paths (> 260 characters on Windows)
				normalizedPath = CommonPlayniteShared.Common.Paths.FixPathLength(normalizedPath);

				// Step 9: Optionally create the directory if it doesn't exist
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
				// Return null for invalid paths
				return null;
			}
		}

		/// <summary>
		/// Validates and normalizes a file path with strict error handling.
		/// Throws an exception if the path is invalid instead of returning null.
		/// </summary>
		/// <param name="filePath">The complete file path to validate and normalize.</param>
		/// <param name="ensureDirectoryExists">If true, creates the directory if it doesn't exist.</param>
		/// <returns>A validated and normalized path.</returns>
		/// <exception cref="ArgumentException">Thrown if the path is null, empty, or invalid.</exception>
		/// <example>
		/// ValidateAndNormalizePathStrict(@"C:\Data\file.txt", true) 
		/// returns normalized path or throws ArgumentException
		/// </example>
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
		/// Creates a safe file path by combining a directory and filename with automatic sanitization.
		/// </summary>
		/// <param name="directory">The directory where the file will be located.</param>
		/// <param name="fileName">The filename (will be sanitized automatically).</param>
		/// <param name="ensureDirectoryExists">If true, creates the directory if it doesn't exist.</param>
		/// <returns>A complete, validated, and normalized file path.</returns>
		/// <exception cref="ArgumentException">Thrown if directory or filename is invalid.</exception>
		/// <example>
		/// CreateSafePath(@"C:\Data", "my*file?.txt", true) 
		/// returns @"C:\Data\my file .txt" and creates C:\Data if needed
		/// </example>
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

			// Sanitize the filename
			string safeFileName = GetSafePathName(fileName, keepNameSpace: false);

			if (string.IsNullOrWhiteSpace(safeFileName))
			{
				throw new ArgumentException($"File name contains only invalid characters: {fileName}", nameof(fileName));
			}

			// Combine and validate the path
			string filePath = Path.Combine(directory, safeFileName);
			return ValidateAndNormalizePathStrict(filePath, ensureDirectoryExists);
		}

		/// <summary>
		/// Creates a safe file path by sanitizing the entire path including all directory components.
		/// </summary>
		/// <param name="fullPath">The complete path to sanitize (including directories and filename).</param>
		/// <param name="ensureDirectoryExists">If true, creates the directory if it doesn't exist.</param>
		/// <returns>A complete, sanitized, and normalized file path.</returns>
		/// <exception cref="ArgumentException">Thrown if the path is invalid.</exception>
		/// <example>
		/// CreateSafePathFromFullPath(@"C:\My*Folder\my*file?.txt", true) 
		/// returns @"C:\My Folder\my file .txt" and creates the directory
		/// </example>
		public static string CreateSafePathFromFullPath(string fullPath, bool ensureDirectoryExists = true)
		{
			if (string.IsNullOrWhiteSpace(fullPath))
			{
				throw new ArgumentException("Path cannot be null or empty", nameof(fullPath));
			}

			// Sanitize the entire path (all components)
			string safePath = GetSafePath(fullPath, lastIsName: true);

			// Validate and normalize
			return ValidateAndNormalizePathStrict(safePath, ensureDirectoryExists);
		}

		#endregion

		#region Path Validation Checks

		/// <summary>
		/// Checks if a path is valid without normalizing it.
		/// </summary>
		/// <param name="filePath">The path to check.</param>
		/// <returns>True if the path is valid, otherwise false.</returns>
		/// <example>
		/// IsPathValid(@"C:\Data\file.txt") returns true
		/// IsPathValid(@"C:\Data\file*.txt") returns false
		/// </example>
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
		/// Checks if a filename contains any invalid characters.
		/// </summary>
		/// <param name="fileName">The filename to check.</param>
		/// <returns>True if the filename is valid, otherwise false.</returns>
		/// <example>
		/// IsFileNameValid("file.txt") returns true
		/// IsFileNameValid("file*.txt") returns false
		/// </example>
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
		/// <param name="path">The path to check.</param>
		/// <returns>True if the path contains invalid characters and needs sanitization, otherwise false.</returns>
		/// <example>
		/// NeedsSanitization(@"C:\Data\file*.txt") returns true
		/// NeedsSanitization(@"C:\Data\file.txt") returns false
		/// </example>
		public static bool NeedsSanitization(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return false;
			}

			try
			{
				// Check if filename contains invalid characters
				string fileName = Path.GetFileName(path);
				if (!string.IsNullOrWhiteSpace(fileName))
				{
					return !IsFileNameValid(fileName);
				}
				return false;
			}
			catch
			{
				// If we can't even extract the filename, it needs sanitization
				return true;
			}
		}

		#endregion
	}
}