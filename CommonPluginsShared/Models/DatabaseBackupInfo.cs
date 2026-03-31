using System;

namespace CommonPluginsShared.Models
{
	/// <summary>
	/// Metadata for a plugin LiteDB backup file.
	/// </summary>
	public class DatabaseBackupInfo
	{
		public string FilePath { get; set; }

		public string FileName { get; set; }

		public DateTime FileDate { get; set; }

		public int TotalRows { get; set; }

		public DateTime? LastEntryDate { get; set; }

		public long FileSizeBytes { get; set; }

		public string FileSizeDisplay => FormatFileSize(FileSizeBytes);

		private static string FormatFileSize(long bytes)
		{
			if (bytes <= 0)
			{
				return "0 B";
			}

			string[] units = { "B", "KB", "MB", "GB", "TB" };
			double size = bytes;
			int unitIndex = 0;

			while (size >= 1024d && unitIndex < units.Length - 1)
			{
				size /= 1024d;
				unitIndex++;
			}

			return string.Format("{0:0.##} {1}", size, units[unitIndex]);
		}
	}
}
