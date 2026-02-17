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
		/// <summary>
		/// Gets or sets the disk volume label.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the drive letter (e.g., "C:\").
		/// </summary>
		public string Drive { get; set; }

		/// <summary>
		/// Gets or sets the free space in bytes.
		/// </summary>
		public long FreeSpace { get; set; }

		/// <summary>
		/// Gets or sets the formatted free space string (e.g., "50.5 GB").
		/// </summary>
		public string FreeSpaceUsage { get; set; }
	}

	/// <summary>
	/// Represents a system hardware configuration.
	/// </summary>
	public class SystemConfiguration
	{
		/// <summary>
		/// Gets or sets the computer name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the operating system name.
		/// </summary>
		public string Os { get; set; }

		/// <summary>
		/// Gets or sets the CPU model name.
		/// </summary>
		public string Cpu { get; set; }

		/// <summary>
		/// Gets or sets the CPU maximum clock speed in MHz.
		/// </summary>
		public uint CpuMaxClockSpeed { get; set; }

		/// <summary>
		/// Gets or sets the GPU name.
		/// </summary>
		public string GpuName { get; set; }

		/// <summary>
		/// Gets or sets the GPU RAM in bytes.
		/// </summary>
		public long GpuRam { get; set; }

		/// <summary>
		/// Gets or sets the current vertical screen resolution.
		/// </summary>
		public uint CurrentVerticalResolution { get; set; }

		/// <summary>
		/// Gets or sets the current horizontal screen resolution.
		/// </summary>
		public uint CurrentHorizontalResolution { get; set; }

		/// <summary>
		/// Gets or sets the total system RAM in bytes.
		/// </summary>
		public long Ram { get; set; }

		/// <summary>
		/// Gets or sets the formatted RAM string (e.g., "16 GB").
		/// </summary>
		public string RamUsage { get; set; }

		/// <summary>
		/// Gets or sets the list of physical disks.
		/// </summary>
		public List<SystemDisk> Disks { get; set; }
	}

	#endregion

	#region System Configuration Manager

	/// <summary>
	/// Manages local system configuration detection and persistence.
	/// Automatically detects hardware and saves configuration to JSON file.
	/// </summary>
	public class SystemConfigurationManager
	{
		private static readonly ILogger Logger = LogManager.GetLogger();

		private SystemConfiguration _systemConfiguration;
		private int _currentConfigurationIndex = -1;
		private List<SystemConfiguration> _allConfigurations = new List<SystemConfiguration>();

		#region Constructor

		/// <summary>
		/// Initializes the system configuration manager.
		/// Loads existing configurations or creates a new one.
		/// </summary>
		/// <param name="configurationsPath">Path to configurations JSON file</param>
		/// <param name="includeDiskInfo">Include disk information in detection</param>
		public SystemConfigurationManager(string configurationsPath, bool includeDiskInfo = true)
		{
			_systemConfiguration = DetectSystemConfiguration(includeDiskInfo);

			if (File.Exists(configurationsPath))
			{
				if (!Serialization.TryFromJsonFile(configurationsPath, out List<SystemConfiguration> configurations))
				{
					Logger.Warn($"Failed to load configurations: {configurationsPath}");
				}
				else
				{
					_allConfigurations = configurations ?? new List<SystemConfiguration>();
				}
			}

			_currentConfigurationIndex = FindMatchingConfiguration();

			bool updated = false;

			if (_currentConfigurationIndex == -1)
			{
				_allConfigurations.Add(_systemConfiguration);
				_currentConfigurationIndex = _allConfigurations.Count - 1;
				updated = true;
			}
			else
			{
				SystemConfiguration config = _allConfigurations[_currentConfigurationIndex];
				config.CurrentHorizontalResolution = _systemConfiguration.CurrentHorizontalResolution;
				config.CurrentVerticalResolution = _systemConfiguration.CurrentVerticalResolution;
				updated = true;
			}

			if (updated)
			{
				FileSystem.WriteStringToFileSafe(configurationsPath, Serialization.ToJson(_allConfigurations));
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Gets the current system configuration.
		/// </summary>
		/// <returns>Current system configuration</returns>
		public SystemConfiguration GetSystemConfiguration() => _systemConfiguration;

		/// <summary>
		/// Gets all saved configurations.
		/// </summary>
		/// <returns>List of all configurations</returns>
		public List<SystemConfiguration> GetConfigurations() => _allConfigurations;

		/// <summary>
		/// Gets the index of the current configuration in the saved list.
		/// </summary>
		/// <returns>Configuration index or -1 if not found</returns>
		public int GetConfigurationIndex() => _currentConfigurationIndex;

		#endregion

		#region Private Methods

		/// <summary>
		/// Finds a matching configuration in the saved list.
		/// </summary>
		/// <returns>Index of matching configuration or -1</returns>
		private int FindMatchingConfiguration()
		{
			return _allConfigurations.FindIndex(x =>
				x.Cpu == _systemConfiguration.Cpu &&
				x.Name == _systemConfiguration.Name &&
				x.GpuName == _systemConfiguration.GpuName &&
				x.RamUsage == _systemConfiguration.RamUsage);
		}

		/// <summary>
		/// Detects the current system hardware configuration using WMI.
		/// </summary>
		/// <param name="includeDiskInfo">Include disk information</param>
		/// <returns>Detected system configuration</returns>
		private SystemConfiguration DetectSystemConfiguration(bool includeDiskInfo = true)
		{
			SystemConfiguration config = new SystemConfiguration();

			try
			{
				config.Name = Environment.MachineName;
				config.Disks = includeDiskInfo ? GetDiskInformation() : new List<SystemDisk>();

				// Operating System
				foreach (ManagementObject os in SafeQueryWMI("SELECT * FROM Win32_OperatingSystem"))
				{
					config.Os = os["Name"]?.ToString().Split('|')[0].Trim();
					break;
				}

				// CPU
				foreach (ManagementObject cpu in SafeQueryWMI("SELECT * FROM Win32_Processor"))
				{
					config.Cpu = cpu["Name"]?.ToString().Trim();
					uint.TryParse(cpu["MaxClockSpeed"]?.ToString(), out uint speed);
					config.CpuMaxClockSpeed = speed;
					break;
				}

				// GPU
				foreach (ManagementObject gpu in SafeQueryWMI("SELECT * FROM Win32_VideoController"))
				{
					string gpuName = gpu["Name"]?.ToString().Trim();
					if (string.IsNullOrEmpty(gpuName))
					{
						continue;
					}

					config.GpuName = gpuName;

					if (long.TryParse(gpu["AdapterRAM"]?.ToString(), out long gpuRam))
					{
						config.GpuRam = gpuRam;
					}

					if (uint.TryParse(gpu["CurrentHorizontalResolution"]?.ToString(), out uint resX))
					{
						config.CurrentHorizontalResolution = resX;
					}

					if (uint.TryParse(gpu["CurrentVerticalResolution"]?.ToString(), out uint resY))
					{
						config.CurrentVerticalResolution = resY;
					}

					if (IsDiscreteGPU(gpuName))
					{
						break;
					}
				}

				// RAM
				foreach (ManagementObject sys in SafeQueryWMI("SELECT * FROM Win32_ComputerSystem"))
				{
					double.TryParse(sys["TotalPhysicalMemory"]?.ToString(), out double totalRam);
					totalRam = Math.Ceiling(totalRam / 1024 / 1024 / 1024);
					config.Ram = (long)(totalRam * 1024 * 1024 * 1024);
					break;
				}

				config.RamUsage = UtilityTools.SizeSuffix(config.Ram, true);
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "Error detecting system configuration");
			}

			return config;
		}

		/// <summary>
		/// Gets information about all fixed physical disks.
		/// </summary>
		/// <returns>List of disk information</returns>
		private List<SystemDisk> GetDiskInformation()
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
					Logger.Warn($"Failed to get disk info: {ex.Message}");
				}
			}

			return disks;
		}

		/// <summary>
		/// Executes a WMI query and returns results.
		/// </summary>
		/// <param name="query">WMI query string</param>
		/// <returns>Enumerable of ManagementObjects or empty if query fails</returns>
		private IEnumerable<ManagementObject> SafeQueryWMI(string query)
		{
			try
			{
				ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
				return searcher.Get().Cast<ManagementObject>();
			}
			catch (Exception ex)
			{
				Logger.Warn($"WMI query failed: {query} - {ex.Message}");
				return Enumerable.Empty<ManagementObject>();
			}
		}

		/// <summary>
		/// Determines if a GPU is a discrete (non-integrated) graphics card.
		/// </summary>
		/// <param name="gpuName">GPU name</param>
		/// <returns>True if discrete GPU, false if integrated</returns>
		private bool IsDiscreteGPU(string gpuName)
		{
			if (string.IsNullOrEmpty(gpuName))
			{
				return false;
			}

			string lowerName = gpuName.ToLower();

			bool isNvidia = lowerName.Contains("nvidia") || lowerName.Contains("geforce") || lowerName.Contains("gtx") || lowerName.Contains("rtx");
			bool isAmd = lowerName.Contains("amd") || lowerName.Contains("radeon") || lowerName.Contains("ati ") || lowerName.Contains("rx ");
			bool isIntel = lowerName.Contains("intel");
			bool isIntegrated = lowerName.Contains("graphics");

			return (isNvidia || isAmd || isIntel) && !isIntegrated;
		}

		#endregion
	}

	#endregion
}