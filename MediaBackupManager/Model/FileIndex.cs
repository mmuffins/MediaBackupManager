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
        static Dictionary<string, BackupFile> files = new Dictionary<string, BackupFile>();
        public static Dictionary<string, BackupFile> Files { get => files; }

        static List<LogicalVolume> logicalVolumes = new List<LogicalVolume>();
        public static List<LogicalVolume> LogicalVolumes { get => logicalVolumes; }

        static List<BackupSet> backupSets = new List<BackupSet>();
        public static List<BackupSet> BackupSets { get => backupSets; }

        static FileIndex()
        {
            //LoadData();
            Database.CreateDatabase();
        }

        /// <summary>
        /// Populates the index with data stored in the database.</summary>  
        public static void LoadData()
        {
            // To properly create all needed relations, the objects should
            // be loaded in the following order:
            // LogicalVolume => BackupFile => BackupSet
            // FileNodes will be automatically loaded with the backup sets


            logicalVolumes = Database.GetLogicalVolume();
            RefreshMountPoints();

            files = Database.GetBackupFile()
                .Select(x => new { Key = x.CheckSum, Item = x })
                .ToDictionary(x => x.Key, x => x.Item);

            backupSets = Database.GetBackupSet();

            //var ab = Database.GetFileNode();
        }

        /// <summary>
        /// Refreshes the mount points for all logical volumes in the collection.</summary>  
        private static void RefreshMountPoints()
        {
            foreach (var item in LogicalVolumes)
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
            if (ContainsDirectory(dir) || IsSubsetOf(dir))
                return;

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
            if (!LogicalVolumes.Contains(logicalVolume))
            {
                LogicalVolumes.Add(logicalVolume);
                Database.InsertLogicalVolume(logicalVolume);
            }
        }

        /// <summary>
        /// Adds the specified file to the file index and returns its reference.</summary>  
        public static BackupFile IndexFile(string fileName)
        {
            string checkSum = BackupFile.CalculateChecksum(fileName);

            if (Files.ContainsKey(checkSum))
            {
                return Files[checkSum];
            }
            else
            {
                var newFile = new BackupFile(fileName, checkSum);
                Files.Add(checkSum, newFile);
                Database.InsertBackupFile(newFile);
                return newFile;
            }
        }

        /// <summary>
        /// Adds the specified file to the file index and returns its reference.</summary>  
        public static BackupFile IndexFile(BackupFile file)
        {
            if (Files.ContainsKey(file.CheckSum))
            {
                return Files[file.CheckSum];
            }
            else
            {
                Files.Add(file.CheckSum, file);
                Database.InsertBackupFile(file);
                return file;
            }
        }

        /// <summary>
        /// Removes the specified file from the file index.</summary>  
        public static void RemoveFile(BackupFile file)
        {
            Files.Remove(file.CheckSum);
            Database.DeleteBackupFile(file);
        }

        /// <summary>
        /// Removes the specified backup set and all children from the index.</summary>  
        /// <param name="item">The object to be removed.</param>
        public static void RemoveBackupSet(BackupSet item)
        {
            if(BackupSets.Where(x => x.Volume.Equals(item.Volume)).Count() < 2)
            {
                // No other backup set shares the logical volume of the 
                // set that's about to be deleted, it can therefore be removed
                RemoveLogicalVolume(item.Volume);
            }

            item.Clear();
            BackupSets.Remove(item);
            Database.DeleteBackupSet(item);
        }

        /// <summary>
        /// Removes the specified logical volume.</summary>  
        /// <param name="item">The object to be removed.</param>
        public static void RemoveLogicalVolume(LogicalVolume volume)
        {
            LogicalVolumes.Remove(volume);
            Database.DeleteLogicalVolume(volume);
        }

        /// <summary>
        /// Determines whether the provided directory is already indexed in one of the backup sets.</summary>  
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

        /// <summary>
        /// Determines whether the provided directory is a parent of one of the backup sets.</summary>  
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
