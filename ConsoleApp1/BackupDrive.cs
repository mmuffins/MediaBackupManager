using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    /// <summary>
    /// Represents a logical volume on a physical drive or network location</summary>  
    class BackupDrive
    {
        /// <summary>User defined label for the drive</summary>
        public string Label { get; set; }

        // Logical Disk
        public string VolumeSerialNumber { get; private set; }
        public long Size { get; private set; }
        public DriveType Type { get; private set; }
        public string VolumeName { get; private set; }

        /// <summary>Mount point or drive letter, only valid in the current session.</summary>
        public string MountPoint { get; }

        public BackupDrive() { }

        public BackupDrive(DirectoryInfo directory)
        {
            this.MountPoint = directory.Root.Name;

            GetLogicalDriveInformation();
        }

        private void GetLogicalDriveInformation()
        {
            //var dependent = $"\\\\{Environment.MachineName}\\root\\cimv2W:Win32_LogicalDisk.DeviceID=\"{MountPoint.Replace("\\", "")}\"";
            var w32LogicalDisk = new ManagementObjectSearcher("root\\CIMV2", $"SELECT * FROM Win32_LogicalDisk WHERE Name = '{ MountPoint.Replace("\\", "") }'").Get();

            if (w32LogicalDisk.Count == 0) //No drive with this letter was found, exit function
                return;

            foreach (var drive in w32LogicalDisk)
            {
                // Since we are querying with a drive letter, the collection can only contain a single object
                this.Size = long.Parse(drive["Size"].ToString());
                this.VolumeSerialNumber = drive["VolumeSerialNumber"].ToString().Trim();
                this.Type = (DriveType)Enum.Parse(typeof(DriveType), drive["DriveType"].ToString());
            }
        }

        // Does the general pattern make sense?
        private async Task GetLogicalDriveInformationAsync()
        {
            await Task.Run(() =>
            {
                var w32LogicalDisk = new ManagementObjectSearcher("root\\CIMV2", $"SELECT * FROM Win32_LogicalDisk WHERE Name = '{ MountPoint.Replace("\\", "") }'").Get();

                if (w32LogicalDisk.Count == 0) //No drive with this letter was found, exit function
                    return;

                foreach (var drive in w32LogicalDisk)
                {
                    // Since we are querying with a drive letter, the collection can only contain a single object
                    this.Size = long.Parse(drive["Size"].ToString());
                    this.VolumeSerialNumber = drive["VolumeSerialNumber"].ToString().Trim();
                    this.Type = (DriveType)Enum.Parse(typeof(DriveType), drive["DriveType"].ToString());
                }
            });

            return;
        }
    }
}
