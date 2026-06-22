using CommonPluginsShared;
using CommonPluginsShared.Models;
using CommonPluginsShared.Utilities;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Models
{
    public class GameRequirements
	{
		public string Id { get; set; } = string.Empty;
		public string GameName { get; set; } = string.Empty;
		public SourceLink SourceLink { get; set; }
		public RequirementEntry Minimum { get; set; } = new RequirementEntry { IsMinimum = true };
		public RequirementEntry Recommended { get; set; } = new RequirementEntry { IsMinimum = false };
	}

	public class RequirementEntry
	{
		public bool IsMinimum { get; set; }
		public List<string> Os { get; set; } = new List<string>();
		public List<string> Cpu { get; set; } = new List<string>();
		public List<string> Gpu { get; set; } = new List<string>();
		/// <summary>Raw RAM text from the store page, normalised at import time.</summary>
		public string RamSource { get; set; }
		public double Ram { get; set; }
		public string RamUsage => UtilityTools.SizeSuffix(Ram, true);
		/// <summary>Raw storage text from the store page, normalised at import time.</summary>
		public string StorageSource { get; set; }
		public double Storage { get; set; }
		public string StorageUsage => UtilityTools.SizeSuffix(Storage);

		[DontSerialize]
		public bool HasData => Os.Count >= 1 || Cpu.Count >= 1 || Gpu.Count >= 1 || Ram > 0 || !RamSource.IsNullOrEmpty();
	}
}