using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// A logical volume on a fixed or removable drive.</summary>  
    public class LogicalVolume : IEquatable<LogicalVolume>, INotifyPropertyChanged
    {
        #region Fields

        public event PropertyChangedEventHandler PropertyChanged;
        string serialNumber;
        ulong size;
        DriveType type;
        string volumeName;
        string mountPoint;
        bool isConnected;

        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the volume serial number of the current logical volume.</summary>  
        public string SerialNumber
        {
            get { return serialNumber; }
            set
            {
                if (value != serialNumber)
                {
                    serialNumber = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the total size of the current logical volume.</summary>  
        public ulong Size
        {
            get { return size; }
            set
            {
                if (value != size)
                {
                    size = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the drive type of the current logical volume.</summary>  
        public DriveType Type
        {
            get { return type; }
            set
            {
                if (value != type)
                {
                    type = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the friendly name of the current logical volume.</summary>  
        public string VolumeName
        {
            get { return volumeName; }
            set
            {
                if (value != volumeName)
                {
                    volumeName = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the mount point or drive letter of the current logical volume, which is only valid in the current session.</summary>  
        public string MountPoint
        {
            get { return mountPoint; }
            set
            {
                if (value != mountPoint)
                {
                    mountPoint = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating if the logical volume is currently connected to the host. To update, execute RefreshStatus.</summary>  
        public bool IsConnected
        {
            get { return isConnected; }
            set
            {
                if (value != isConnected)
                {
                    isConnected = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion

        #region Methods

        public LogicalVolume() { }

        public LogicalVolume(DirectoryInfo directory)
        {
            this.MountPoint = directory.Root.Name;
            this.VolumeName = directory.Root.Name;

            GetLogicalDriveInformation();
            RefreshStatus();
        }

        /// <summary>
        /// Populates internal properties from WMI.</summary>
        private void GetLogicalDriveInformation()
        {
            var w32LogicalDisk = new ManagementObjectSearcher("root\\CIMV2", $"SELECT * FROM Win32_LogicalDisk WHERE Name = '{MountPoint.Replace("\\", "") }'").Get();

            if (w32LogicalDisk.Count == 0) // No drive with this letter was found, exit function
                return;

            foreach (var drive in w32LogicalDisk)
            {
                // Since we are querying with a drive letter, the collection can only contain a single object
                this.Size = ulong.Parse(drive["Size"].ToString());
                this.SerialNumber = drive["VolumeSerialNumber"].ToString().Trim();
                this.Type = (DriveType)Enum.Parse(typeof(DriveType), drive["DriveType"].ToString());
                this.VolumeName = drive["VolumeName"].ToString();
            }
        }

        /// <summary>
        /// Refreshes mount point and connected status of the current volume.</summary>
        public void RefreshStatus()
        {
            var w32LogicalDisk = new ManagementObjectSearcher("root\\CIMV2", $"SELECT * FROM Win32_LogicalDisk WHERE VolumeSerialNumber = '{SerialNumber}'").Get();

            if (w32LogicalDisk.Count == 0)
            {
                // Drive is currently not mounted, exit
                IsConnected = false;
                MountPoint = null;
                return;
            }

            foreach (var drive in w32LogicalDisk)
            {
                // Since we are querying by volume label, the collection can only contain a single object
                if (!string.IsNullOrWhiteSpace(drive["Name"].ToString()))
                {
                    MountPoint = new DriveInfo(drive["Name"].ToString()).Name;
                    IsConnected = true;
                }
            }

            return;
        }

        #endregion

        #region Implementations

        public override string ToString()
        {
            return SerialNumber;
        }

        public override int GetHashCode()
        {
            return SerialNumber.GetHashCode();
        }

        public bool Equals(LogicalVolume other)
        {
            if (other == null)
                return false;

            return this.SerialNumber.Equals(other.SerialNumber);
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

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion


    }
}
