using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Represents a logical volume on a physical drive or network location</summary>  
    class LogicalVolume : IEquatable<LogicalVolume>
    {
        /// <summary>User defined label for the drive</summary>
        public string Label { get; set; }

        // Logical Disk
        //TODO: Check if using readonly could work here
        public string VolumeSerialNumber { get; private set; }
        public long Size { get; private set; }
        public DriveType Type { get; private set; }
        public string VolumeName { get; private set; }

        /// <summary>Mount point or drive letter, only valid in the current session.</summary>
        public string MountPoint { get; }

        public LogicalVolume() { }

        public LogicalVolume(DirectoryInfo directory)
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

        public override string ToString()
        {
            return VolumeSerialNumber;
        }

        public override int GetHashCode()
        {
            return VolumeSerialNumber.GetHashCode();
        }

        public bool Equals(LogicalVolume other)
        {
            if (other == null)
                return false;

            return this.VolumeSerialNumber.Equals(other.VolumeSerialNumber);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            LogicalVolume otherObj = obj as LogicalVolume;
            if (otherObj == null)
                return false;
            else
                return Equals(otherObj);
        }
    }
}
