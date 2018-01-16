using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Text.RegularExpressions;

namespace ConsoleApp1
{
    /// <summary>
    /// Manages a collection of BackupFile objects.</summary>  
    public class BackupList
    {
        Dictionary<string, BackupFile> fileIndex = new Dictionary<string, BackupFile>();
        public HashSet<string> Locations { get; }
        Dictionary<string, BackupDrive> logicalVolumes = new Dictionary<string, BackupDrive>();

        public void Add(FileInfo fileInfo)
        {

            var ad = new BackupFile(fileInfo);
            ad.Locations.Add("ddee1");
            ad.Locations.Add("ddee2");
            ad.Locations.Add("ddee3");
            ad.Locations.Add("ddee4");

            ad.Locations.Remove("ddee5");

            if (ad.Locations.Contains("ddee3"))
            {
                ad.Locations.Remove("ddee3");
            }

        }

        public void Add(DirectoryInfo dir)
        {
            //var ab = dir.Root;
            //var drives = DriveInfo.GetDrives();
            //var dd = GetPhysicalDrive(dir);
            //var driveList = GetLogicalDisks();


            if (!logicalVolumes.ContainsKey(dir.Root.Name))
            {
                logicalVolumes.Add(dir.Root.Name, new BackupDrive(dir));
            }
        }

        /// <summary>Returns a list of all available physical drives</summary>  
        private List<BackupDrive> GetPhysicalDriveList()
        {
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DiskPartition");

            foreach (var queryObj in searcher.Get())
            {
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("Win32_DiskPartition instance");
                Console.WriteLine("Name:{0}", (string)queryObj["Name"]);
                Console.WriteLine("Index:{0}", (uint)queryObj["Index"]);
                Console.WriteLine("DiskIndex:{0}", (uint)queryObj["DiskIndex"]);
                Console.WriteLine("BootPartition:{0}", (bool)queryObj["BootPartition"]);
            }

            var driveList = new List<BackupDrive>();

            // Get list of all physical drives
            var w32DiskDrive = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            foreach (ManagementObject wmi_HD in w32DiskDrive.Get())
            {
                //foreach (var item in wmi_HD.Properties)
                //{
                //    Console.WriteLine(item.Name + ": " + wmi_HD[item.Name]);
                //}

                //Query the associated partitions of the current DeviceID
                string _AssocQuery = "Associators of {Win32_DiskDrive.DeviceID='" +
                                                      wmi_HD.Properties["DeviceID"].Value.ToString() + "'}" +
                                                      "where AssocClass=Win32_DiskDriveToDiskPartition";
                ManagementObjectSearcher _AssocPart = new ManagementObjectSearcher(_AssocQuery);
                _AssocPart.Options.Timeout = EnumerationOptions.InfiniteTimeout;

                //For each Disk Drive, query the associated partitions
                foreach (ManagementObject _objAssocPart in _AssocPart.Get())
                {
                    Console.WriteLine("DeviceID: {0}  BootPartition: {1}",
                                      _objAssocPart.Properties["DeviceID"].Value.ToString(),
                                      _objAssocPart.Properties["BootPartition"].Value.ToString());

                    //Query the associated logical disk of the current PartitionID
                    string _LogDiskQuery = "Associators of {Win32_DiskPartition.DeviceID='" +
                                            _objAssocPart.Properties["DeviceID"].Value.ToString() + "'} " +
                                            "where AssocClass=Win32_LogicalDiskToPartition";

                    ManagementObjectSearcher _LogDisk = new ManagementObjectSearcher(_LogDiskQuery);
                    _LogDisk.Options.Timeout = EnumerationOptions.InfiniteTimeout;

                    //For each partition, query the Logical Drives
                    foreach (var _LogDiskEnu in _LogDisk.Get())
                    {
                        Console.WriteLine("Volume Name: {0}  Serial Number: {1}  System Name: {2}",
                                          _LogDiskEnu.Properties["VolumeName"].Value.ToString(),
                                          _LogDiskEnu.Properties["VolumeSerialNumber"].Value.ToString(),
                                          _LogDiskEnu.Properties["SystemName"].Value.ToString());
                        Console.WriteLine("Description: {0}  DriveType: {1}  MediaType: {2}",
                                          _LogDiskEnu.Properties["Description"].Value.ToString(),
                                          _LogDiskEnu.Properties["DriveType"].Value.ToString(),
                                          _LogDiskEnu.Properties["MediaType"].Value.ToString());
                        Console.WriteLine("Name: {0}  MediaType: {1}  DeviceID: {2} Caption: {3}",
                                          _LogDiskEnu.Properties["Name"].Value.ToString(),
                                          _LogDiskEnu.Properties["MediaType"].Value.ToString(),
                                          _LogDiskEnu.Properties["DeviceID"].Value.ToString(),
                                        _LogDiskEnu.Properties["Caption"].Value.ToString());
                        Console.WriteLine();
                    }
                }


                driveList.Add(new BackupDrive(wmi_HD));
            }

            // Match physical drives with partitions
            var w32DiskToPartition = new ManagementObjectSearcher("SELECT * FROM win32_DiskDriveToDiskPartition");
            foreach (ManagementObject partition in w32DiskToPartition.Get())
            {
                var dd = partition["Antecedent"].ToString().Split('=');
                Console.WriteLine(partition);
                //driveList.Add(new BackupDrive(wmi_HD));
            }



            //var drives = DriveInfo.GetDrives();
            //var fileDrive = fileInfo.Directory.Root;
            return driveList;
        }

        /// <summary>Returns a list of all available logical disks</summary>  
        private List<string> GetLogicalDisks()
        {
            var w32LogicalDisk = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_LogicalDisk");

            foreach (var wmi_HD in w32LogicalDisk.Get())
            {
                foreach (var item in wmi_HD.Properties)
                {
                    Console.WriteLine(item.Name + ": " + wmi_HD[item.Name]);
                }
                Console.WriteLine();
            }

            return new List<string>();
        }

        /// <summary>Returns the physical drive for a directory</summary>  
        private BackupDrive GetPhysicalDrive(DirectoryInfo dir)
        {

            // Match mountpoint of the given directory with all available logical disks
            var dependent = $"\\\\{Environment.MachineName}\\root\\cimv2:Win32_LogicalDisk.DeviceID=\"{dir.Root.Name.Replace("\\", "")}\"";
            var w32LogicalDiskToPartition = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_LogicalDiskToPartition");
            foreach (var queryObj in w32LogicalDiskToPartition.Get())
            {
                if (queryObj["Dependent"].ToString() == dependent)
                {
                    // A match will return disk and partition index which can be correlated with a physical disk
                    var partitionName = queryObj["Antecedent"].ToString().Replace($"\\\\{Environment.MachineName}\\root\\cimv2:Win32_DiskPartition.DeviceID=","");

                    var matches = new Regex(@"Disk #(\d+), Partition #(\d+)").Match(queryObj["Antecedent"].ToString());
                    var w32DiskDrive = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DiskDrive WHERE Index = " + matches.Groups[1]);
                    foreach (ManagementObject item in w32DiskDrive.Get())
                    {
                        var backupDrive = new BackupDrive(item);
                        //backupDrive.CurrentPartition = matches.Groups[2].Value;
                        //backupDrive.CurrentMountPoint = dir.Root.Name.Replace("\\", "");
                        return backupDrive;
                    }

                    //var w32DiskPartition = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DiskPartition WHERE Name = " + partitionName).Get().;

                }
            }

            return new BackupDrive();
        }
    }
}
