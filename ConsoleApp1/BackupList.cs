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
    }
}
