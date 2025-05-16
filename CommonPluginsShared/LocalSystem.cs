using CommonPlayniteShared.Common;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;

namespace CommonPluginsShared
{
    /// <summary>
    /// Represents the local system configuration and manages saving/loading of configurations.
    /// </summary>
    public class LocalSystem
    {
        private static ILogger Logger => LogManager.GetLogger();

        private SystemConfiguration SystemConfiguration { get; set; }
        private int IdConfiguration { get; set; } = -1;
        private List<SystemConfiguration> Configurations { get; set; } = new List<SystemConfiguration>();


        /// <summary>
        /// Constructs LocalSystem and loads or adds the current configuration.
        /// </summary>
        /// <param name="ConfigurationsPath">Path to configuration JSON file.</param>
        /// <param name="WithDiskInfos">Include disk info.</param>
        public LocalSystem(string ConfigurationsPath, bool WithDiskInfos = true)
        {
            SystemConfiguration = GetPcInfo(WithDiskInfos);

            if (File.Exists(ConfigurationsPath))
            {
                if (!Serialization.TryFromJsonFile(ConfigurationsPath, out List<SystemConfiguration> conf))
                {
                    Logger.Warn(string.Format("Failed to load {0}", ConfigurationsPath));
                }
                else
                {
                    Configurations = conf ?? new List<SystemConfiguration>();
                }
            }

            IdConfiguration = Configurations.FindIndex(x =>
                x.Cpu == SystemConfiguration.Cpu &&
                x.Name == SystemConfiguration.Name &&
                x.GpuName == SystemConfiguration.GpuName &&
                x.RamUsage == SystemConfiguration.RamUsage);

            bool updated = false;

            if (IdConfiguration == -1)
            {
                Configurations.Add(SystemConfiguration);
                IdConfiguration = Configurations.Count - 1;
                updated = true;
            }
            else
            {
                var config = Configurations[IdConfiguration];
                config.CurrentHorizontalResolution = SystemConfiguration.CurrentHorizontalResolution;
                config.CurrentVerticalResolution = SystemConfiguration.CurrentVerticalResolution;
                updated = true;
            }

            if (updated)
            {
                FileSystem.WriteStringToFileSafe(ConfigurationsPath, Serialization.ToJson(Configurations));
            }
        }

        /// <summary>
        /// Gets the current system configuration.
        /// </summary>
        public SystemConfiguration GetSystemConfiguration() => SystemConfiguration;

        /// <summary>
        /// Gets the list of saved configurations.
        /// </summary>
        public List<SystemConfiguration> GetConfigurations() => Configurations;

        /// <summary>
        /// Gets the index of the current configuration in the list.
        /// </summary>
        public int GetIdConfiguration() => IdConfiguration;


        private bool CallIsNvidia(string name) => ContainsIgnoreCase(name, "nvidia", "geforce", "gtx", "rtx");
        private bool CallIsAmd(string name) => ContainsIgnoreCase(name, "amd", "radeon", "ati ", "rx ");
        private bool CallIsIntel(string name) => ContainsIgnoreCase(name, "intel");
        private bool IsNotIntegrated(string name) => !ContainsIgnoreCase(name, "graphics");

        private bool ContainsIgnoreCase(string source, params string[] values)
        {
            if (string.IsNullOrEmpty(source)) return false;
            foreach (var val in values)
            {
                if (source.IndexOf(val, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Retrieves the current PC hardware and OS information as a <see cref="SystemConfiguration"/> object.
        /// Optionally includes disk information.
        /// </summary>
        /// <param name="withDiskInfos">If true, includes information about physical disks.</param>
        /// <returns>
        /// A <see cref="SystemConfiguration"/> object containing details about the machine name, OS, CPU, GPU, RAM, 
        /// screen resolution, and optionally disk information. If an error occurs, returns a partially filled object.
        /// </returns>
        private SystemConfiguration GetPcInfo(bool withDiskInfos = true)
        {
            var systemConfiguration = new SystemConfiguration();
            try
            {
                systemConfiguration.Name = Environment.MachineName;
                systemConfiguration.Disks = withDiskInfos ? GetInfoDisks() : new List<SystemDisk>();

                foreach (var os in SafeQueryWMI("select * from Win32_OperatingSystem"))
                {
                    systemConfiguration.Os = os["Name"]?.ToString().Split('|')[0].Trim();
                    break;
                }

                foreach (var cpu in SafeQueryWMI("select * from Win32_Processor"))
                {
                    systemConfiguration.Cpu = cpu["Name"]?.ToString().Trim();
                    uint.TryParse(cpu["MaxClockSpeed"]?.ToString(), out uint speed);
                    systemConfiguration.CpuMaxClockSpeed = speed;
                    break;
                }

                foreach (var gpu in SafeQueryWMI("select * from Win32_VideoController"))
                {
                    var gpuName = gpu["Name"]?.ToString().Trim();
                    if (string.IsNullOrEmpty(gpuName)) continue;

                    systemConfiguration.GpuName = gpuName;

                    if (long.TryParse(gpu["AdapterRAM"]?.ToString(), out long gpuRam))
                    {
                        systemConfiguration.GpuRam = gpuRam;
                    }

                    if (uint.TryParse(gpu["CurrentHorizontalResolution"]?.ToString(), out uint resX))
                    {
                        systemConfiguration.CurrentHorizontalResolution = resX;
                    }

                    if (uint.TryParse(gpu["CurrentVerticalResolution"]?.ToString(), out uint resY))
                    {
                        systemConfiguration.CurrentVerticalResolution = resY;
                    }

                    if ((CallIsNvidia(gpuName) || CallIsAmd(gpuName) || CallIsIntel(gpuName)) && IsNotIntegrated(gpuName))
                        break;
                }

                foreach (var sys in SafeQueryWMI("select * from Win32_ComputerSystem"))
                {
                    double.TryParse(sys["TotalPhysicalMemory"]?.ToString(), out double totalRam);
                    totalRam = Math.Ceiling(totalRam / 1024 / 1024 / 1024);
                    systemConfiguration.Ram = (long)(totalRam * 1024 * 1024 * 1024);
                    break;
                }

                systemConfiguration.RamUsage = Tools.SizeSuffix(systemConfiguration.Ram, true);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
            return systemConfiguration;
        }

        /// <summary>
        /// Retrieves information about all fixed physical disks on the system.
        /// </summary>
        /// <returns>
        /// A list of <see cref="SystemDisk"/> objects, each containing the disk name, drive letter, free space, and formatted free space usage.
        /// </returns>
        private List<SystemDisk> GetInfoDisks()
        {
            var disks = new List<SystemDisk>();
            foreach (var d in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
            {
                try
                {
                    disks.Add(new SystemDisk
                    {
                        Name = d.VolumeLabel,
                        Drive = d.Name,
                        FreeSpace = d.TotalFreeSpace,
                        FreeSpaceUsage = Tools.SizeSuffix(d.TotalFreeSpace)
                    });
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to get disk info: " + ex.Message);
                }
            }
            return disks;
        }

        /// <summary>
        /// Executes a WMI query and returns the resulting collection of <see cref="ManagementObject"/>.
        /// Handles exceptions and logs warnings if the query fails.
        /// </summary>
        /// <param name="query">The WMI query string to execute.</param>
        /// <returns>
        /// An enumerable collection of <see cref="ManagementObject"/>. Returns an empty collection if the query fails.
        /// </returns>
        private IEnumerable<ManagementObject> SafeQueryWMI(string query)
        {
            try
            {
                var searcher = new ManagementObjectSearcher(query);
                return searcher.Get().Cast<ManagementObject>();
            }
            catch (Exception ex)
            {
                Logger.Warn("WMI query failed: " + query + " - " + ex.Message);
                return Enumerable.Empty<ManagementObject>();
            }
        }
    }


    /// <summary>
    /// Represents a system configuration including hardware and disk information.
    /// </summary>
    public class SystemConfiguration
    {
        public string Name { get; set; }
        public string Os { get; set; }
        public string Cpu { get; set; }
        public uint CpuMaxClockSpeed { get; set; }
        public string GpuName { get; set; }
        public long GpuRam { get; set; }
        public uint CurrentVerticalResolution { get; set; }
        public uint CurrentHorizontalResolution { get; set; }
        public long Ram { get; set; }
        public string RamUsage { get; set; }
        public List<SystemDisk> Disks { get; set; }
    }


    /// <summary>
    /// Represents a physical disk with free space information.
    /// </summary>
    public class SystemDisk
    {
        public string Name { get; set; }
        public string Drive { get; set; }
        public long FreeSpace { get; set; }
        public string FreeSpaceUsage { get; set; }
    }
}