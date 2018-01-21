using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Manages a collection of BackupFile objects.</summary>  

    class FileIndex
    {
        static Dictionary<string, BackupFile> fileIndex = new Dictionary<string, BackupFile>();
        static HashSet<LogicalVolume> logicalVolumes = new HashSet<LogicalVolume>();
        public static List<BackupSet> BackupSets { get; }

        static FileIndex()
        {
            Database.CreateDatabase();
            BackupSets = new List<BackupSet>();
            logicalVolumes = Database.GetLogicalVolume();
            RefreshMountPoints();
        }


        /// <summary>
        /// Refreshes the mount points for all logical volumes in the collection.</summary>  
        private static void RefreshMountPoints()
        {
            foreach (var item in logicalVolumes)
            {
                DriveInfo mountPoint = item.GetMountPoint();
                if(!(mountPoint is null))
                {
                    item.MountPoint = mountPoint.Name;
                }
            }
        }

        /// <summary>
        /// Adds the specified directory as new BackupSet to the file index.</summary>  
        public static void IndexDirectory(DirectoryInfo dir)
        {
            bool containsDir = ContainsDirectory(dir);
            bool isSubset = IsSubsetOf(dir);

            var newDrive = new LogicalVolume(dir);
            AddLogicalVolume(newDrive);
            
            var scanSet = new BackupSet(dir, newDrive);
            AddBackupSet(scanSet);
            scanSet.ScanFiles();
        }

        /// <summary>
        /// Adds the specified backup set to the local collection.</summary>  
        private static void AddBackupSet(BackupSet backupSet)
        {
            BackupSets.Add(backupSet);
            Database.InsertBackupSet(backupSet);
        }

        /// <summary>
        /// Adds the specified logical volume to the local collection.</summary>  
        private static void AddLogicalVolume(LogicalVolume logicalVolume)
        {
            if (!logicalVolumes.Contains(logicalVolume))
            {
                logicalVolumes.Add(logicalVolume);
                Database.InsertLogicalVolume(logicalVolume);
            }
        }

        /// <summary>
        /// Adds the specified file to the file index and returns its reference.</summary>  
        public static BackupFile IndexFile(string fileName)
        {
            string checkSum = BackupFile.CalculateChecksum(fileName);

            if (fileIndex.ContainsKey(checkSum))
            {
                return fileIndex[checkSum];
            }
            else
            {
                var newFile = new BackupFile(fileName, checkSum);
                fileIndex.Add(checkSum, newFile);
                return newFile;
            }
        }

        /// <summary>
        /// Removes the specified file from the file index.</summary>  
        public static void RemoveFile(BackupFile file)
        {
            fileIndex.Remove(file.CheckSum);
        }

        /// <summary>
        /// Removes the specified backup set and all children from the index.</summary>  
        public static void RemoveSet(BackupSet item)
        {
            if(BackupSets.Where(x => x.Drive.Equals(item.Drive)).Count() < 2)
            {
                // No other backup set shares the logical volume of the 
                // set that's about to be deleted, it can therefore be removed
                logicalVolumes.Remove(item.Drive);
            }

            item.Clear();
            BackupSets.Remove(item);
        }

        public static bool ContainsDirectory(DirectoryInfo dir)
        {
            bool result = false;

            foreach (var set in BackupSets)
            {
                if (set.ContainsDirectory(dir))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        public static bool IsSubsetOf(DirectoryInfo dir)
        {
            bool result = false;

            foreach (var set in BackupSets)
            {
                if (set.IsSubsetOf(dir))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
    }
}
