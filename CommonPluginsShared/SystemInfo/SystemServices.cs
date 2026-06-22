using CommonPlayniteShared.Common;
using CommonPluginsShared.Utilities;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;

namespace CommonPluginsShared.SystemInfo
{
	#region Models

	/// <summary>
	/// Represents a physical disk with storage information.
	/// </summary>
	public class SystemDisk
	{
		/// <summary>Gets or sets the disk volume label.</summary>
		public string Name { get; set; }

		/// <summary>Gets or sets the drive letter (e.g., "C:\").</summary>
		public string Drive { get; set; }

		/// <summary>Gets or sets the free space in bytes.</summary>
		public long FreeSpace { get; set; }

		/// <summary>Gets or sets the formatted free space string (e.g., "50.5 GB").</summary>
		public string FreeSpaceUsage { get; set; }
	}

	/// <summary>
	/// Represents a system hardware configuration snapshot.
	/// </summary>
	public class SystemConfiguration
	{
		/// <summary>Gets or sets the computer name.</summary>
		public string Name { get; set; }

		/// <summary>Gets or sets the operating system name.</summary>
		public string Os { get; set; }

		/// <summary>Gets or sets the CPU model name.</summary>
		public string Cpu { get; set; }

		/// <summary>Gets or sets the CPU maximum clock speed in MHz.</summary>
		public uint CpuMaxClockSpeed { get; set; }

		/// <summary>Gets or sets the GPU model name.</summary>
		public string GpuName { get; set; }

		/// <summary>Gets or sets the GPU dedicated RAM in bytes.</summary>
		public long GpuRam { get; set; }

		/// <summary>Gets or sets the current vertical screen resolution in pixels.</summary>
		public uint CurrentVerticalResolution { get; set; }

		/// <summary>Gets or sets the current horizontal screen resolution in pixels.</summary>
		public uint CurrentHorizontalResolution { get; set; }

		/// <summary>Gets or sets the total system RAM in bytes (rounded up to the nearest GiB).</summary>
		public long Ram { get; set; }

		/// <summary>Gets or sets the formatted RAM string (e.g., "16 GB").</summary>
		public string RamUsage { get; set; }

		/// <summary>Gets or sets the list of fixed physical disks.</summary>
		public List<SystemDisk> Disks { get; set; }
	}

	#endregion

	#region System Configuration Manager

	/// <summary>
	/// Manages local system configuration detection and persistence to a JSON file.
	/// On construction, detects the current hardware, matches it against previously saved
	/// configurations, and saves the updated list back to disk.
	/// </summary>
	public class SystemConfigurationManager
	{
		private static readonly ILogger Logger = LogManager.GetLogger();

		private readonly SystemConfiguration _systemConfiguration;
		private int _currentConfigurationIndex = -1;
		private List<SystemConfiguration> _allConfigurations = new List<SystemConfiguration>();

		/// <summary>
		/// Initialises the manager: detects current hardware, loads existing configurations
		/// from <paramref name="configurationsPath"/>, and persists any changes.
		/// </summary>
		/// <param name="configurationsPath">Absolute path to the configurations JSON file.</param>
		/// <param name="includeDiskInfo">
		/// When <c>true</c> (default), fixed-disk information is included in the configuration.
		/// Set to <c>false</c> to skip the (slower) disk enumeration.
		/// </param>
		public SystemConfigurationManager(string configurationsPath, bool includeDiskInfo = true)
		{
			_systemConfiguration = DetectSystemConfiguration(includeDiskInfo);

			if (File.Exists(configurationsPath))
			{
				if (!Serialization.TryFromJsonFile(configurationsPath, out List<SystemConfiguration> saved))
				{
					Logger.Warn($"Failed to load configurations from: {configurationsPath}");
				}
				else
				{
					_allConfigurations = saved ?? new List<SystemConfiguration>();
				}
			}

			_currentConfigurationIndex = FindMatchingConfiguration();

			if (_currentConfigurationIndex == -1)
			{
				// New machine / new hardware profile — append.
				_allConfigurations.Add(_systemConfiguration);
				_currentConfigurationIndex = _allConfigurations.Count - 1;
			}
			else
			{
				// Known machine — only update the resolution in case the monitor/settings changed.
				SystemConfiguration existing = _allConfigurations[_currentConfigurationIndex];
				existing.CurrentHorizontalResolution = _systemConfiguration.CurrentHorizontalResolution;
				existing.CurrentVerticalResolution = _systemConfiguration.CurrentVerticalResolution;
			}

			// Always persist so resolution is kept current.
			FileSystem.WriteStringToFileSafe(configurationsPath, Serialization.ToJson(_allConfigurations));
		}

		#region Public API

		/// <summary>Returns the hardware configuration detected for the current machine.</summary>
		public SystemConfiguration GetSystemConfiguration() => _systemConfiguration;

		/// <summary>Returns all saved hardware configurations (current and historical).</summary>
		public List<SystemConfiguration> GetConfigurations() => _allConfigurations;

		/// <summary>
		/// Returns the zero-based index of the current configuration in <see cref="GetConfigurations"/>,
		/// or <c>-1</c> if not found (should not occur after construction).
		/// </summary>
		public int GetConfigurationIndex() => _currentConfigurationIndex;

		#endregion

		#region Configuration detection

		/// <summary>
		/// Returns the index of a saved configuration whose CPU, machine name, GPU name, and RAM
		/// all match the currently detected values; or <c>-1</c> if none matches.
		/// </summary>
		private int FindMatchingConfiguration()
		{
			return _allConfigurations.FindIndex(x =>
				x.Cpu == _systemConfiguration.Cpu &&
				x.Name == _systemConfiguration.Name &&
				x.GpuName == _systemConfiguration.GpuName &&
				x.RamUsage == _systemConfiguration.RamUsage);
		}

		/// <summary>
		/// Queries WMI to build a full hardware snapshot of the current machine.
		/// Each WMI object and its searcher are properly disposed to avoid COM handle leaks.
		/// </summary>
		/// <param name="includeDiskInfo">Include fixed-disk enumeration.</param>
		private SystemConfiguration DetectSystemConfiguration(bool includeDiskInfo)
		{
			SystemConfiguration config = new SystemConfiguration
			{
				Name = Environment.MachineName,
				Disks = includeDiskInfo ? GetDiskInformation() : new List<SystemDisk>()
			};

			try
			{
				config.Os = QueryFirstWmiValue("SELECT Name FROM Win32_OperatingSystem", obj =>
				{
					string raw = obj["Name"]?.ToString();
					return raw?.Split('|')[0].Trim();
				});

				ReadCpuInfo(config);
				ReadGpuInfo(config);
				ReadRamInfo(config);

				config.RamUsage = UtilityTools.SizeSuffix(config.Ram, true);
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "Error detecting system configuration.");
			}

			return config;
		}

		/// <summary>Populates CPU fields on <paramref name="config"/> from WMI.</summary>
		private static void ReadCpuInfo(SystemConfiguration config)
		{
			ExecuteWmiQuery("SELECT Name, MaxClockSpeed FROM Win32_Processor", obj =>
			{
				config.Cpu = obj["Name"]?.ToString().Trim();
				uint.TryParse(obj["MaxClockSpeed"]?.ToString(), out uint speed);
				config.CpuMaxClockSpeed = speed;
				return false; // first record only
			});
		}

		/// <summary>
		/// Populates GPU fields on <paramref name="config"/> from WMI.
		/// Prefers a discrete GPU over an integrated one; falls back to the first adapter found.
		/// Resolution is taken from whichever adapter provides non-zero values.
		/// </summary>
		private static void ReadGpuInfo(SystemConfiguration config)
		{
			ExecuteWmiQuery("SELECT Name, AdapterRAM, CurrentHorizontalResolution, CurrentVerticalResolution FROM Win32_VideoController", obj =>
			{
				string gpuName = obj["Name"]?.ToString().Trim();
				if (string.IsNullOrEmpty(gpuName))
				{
					return true; // continue
				}

				// Capture resolution from the first adapter that reports it.
				if (config.CurrentHorizontalResolution == 0 &&
					uint.TryParse(obj["CurrentHorizontalResolution"]?.ToString(), out uint resX))
				{
					config.CurrentHorizontalResolution = resX;
				}

				if (config.CurrentVerticalResolution == 0 &&
					uint.TryParse(obj["CurrentVerticalResolution"]?.ToString(), out uint resY))
				{
					config.CurrentVerticalResolution = resY;
				}

				// Prefer discrete GPU; overwrite any previously stored integrated adapter.
				bool isDiscrete = IsDiscreteGpu(gpuName);
				bool hasGpuAlready = !string.IsNullOrEmpty(config.GpuName);

				if (!hasGpuAlready || isDiscrete)
				{
					config.GpuName = gpuName;

					if (long.TryParse(obj["AdapterRAM"]?.ToString(), out long gpuRam))
					{
						config.GpuRam = gpuRam;
					}
				}

				// Stop iterating once a discrete GPU has been recorded.
				return !isDiscrete;
			});
		}

		/// <summary>Populates RAM fields on <paramref name="config"/> from WMI.</summary>
		private static void ReadRamInfo(SystemConfiguration config)
		{
			ExecuteWmiQuery("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem", obj =>
			{
				if (double.TryParse(obj["TotalPhysicalMemory"]?.ToString(), out double bytes))
				{
					// Round up to the nearest GiB (matches how Windows reports RAM).
					double gib = Math.Ceiling(bytes / 1024 / 1024 / 1024);
					config.Ram = (long)(gib * 1024 * 1024 * 1024);
				}
				return false; // first record only
			});
		}

		#endregion

		#region Disk enumeration

		/// <summary>Returns storage information for every fixed (non-removable) disk.</summary>
		private static List<SystemDisk> GetDiskInformation()
		{
			List<SystemDisk> disks = new List<SystemDisk>();

			foreach (DriveInfo drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
			{
				try
				{
					disks.Add(new SystemDisk
					{
						Name = drive.VolumeLabel,
						Drive = drive.Name,
						FreeSpace = drive.TotalFreeSpace,
						FreeSpaceUsage = UtilityTools.SizeSuffix(drive.TotalFreeSpace)
					});
				}
				catch (Exception ex)
				{
					LogManager.GetLogger().Warn($"Could not read disk info for {drive.Name}: {ex.Message}");
				}
			}

			return disks;
		}

		#endregion

		#region WMI helpers

		/// <summary>
		/// Executes <paramref name="query"/> and invokes <paramref name="visitor"/> for each result row.
		/// Both the searcher and the result collection are properly disposed.
		/// </summary>
		/// <param name="query">WMI SELECT query string.</param>
		/// <param name="visitor">
		/// Callback receiving each <see cref="ManagementObject"/>.
		/// Return <c>true</c> to continue to the next row, <c>false</c> to stop early.
		/// </param>
		private static void ExecuteWmiQuery(string query, Func<ManagementObject, bool> visitor)
		{
			try
			{
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
				using (ManagementObjectCollection results = searcher.Get())
				{
					foreach (ManagementObject obj in results)
					{
						using (obj)
						{
							if (!visitor(obj))
							{
								break;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				LogManager.GetLogger().Warn($"WMI query failed [{query}]: {ex.Message}");
			}
		}

		/// <summary>
		/// Convenience wrapper: executes <paramref name="query"/>, applies <paramref name="selector"/>
		/// to the first row, and returns the result (or <c>null</c> on failure / empty result set).
		/// </summary>
		private static string QueryFirstWmiValue(string query, Func<ManagementObject, string> selector)
		{
			string result = null;
			ExecuteWmiQuery(query, obj =>
			{
				result = selector(obj);
				return false; // first row only
			});
			return result;
		}

		#endregion

		#region GPU classification

		/// <summary>
		/// Returns <c>true</c> when <paramref name="gpuName"/> belongs to a discrete adapter.
		/// Heuristic: a known vendor keyword is present AND the name does not contain
		/// the word "graphics" (which typically marks integrated adapters such as
		/// "Intel HD Graphics 630" or "AMD Radeon Graphics").
		/// </summary>
		/// <remarks>
		/// Known limitation: some OEM integrated GPUs use non-standard names and may be
		/// misclassified. For the purposes of system requirements checking this is acceptable.
		/// </remarks>
		private static bool IsDiscreteGpu(string gpuName)
		{
			if (string.IsNullOrEmpty(gpuName))
			{
				return false;
			}

			string lower = gpuName.ToLowerInvariant();

			bool isKnownVendor = lower.Contains("nvidia") || lower.Contains("geforce")
							  || lower.Contains("gtx") || lower.Contains("rtx")
							  || lower.Contains("amd") || lower.Contains("radeon")
							  || lower.Contains("ati ") || lower.Contains(" rx ")
							  || lower.Contains("intel") || lower.Contains("arc");

			bool isIntegrated = lower.Contains("graphics");

			return isKnownVendor && !isIntegrated;
		}

		#endregion
	}

	#endregion
}