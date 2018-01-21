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
            BackupSets = new List<BackupSet>();
        }


        /// <summary>
        /// Adds the specified directory as new BackupSet to the file index.</summary>  
        public static void AddDirectory(DirectoryInfo dir)
        {
            bool containsDir = ContainsDirectory(dir);
            bool isSubset = IsSubsetOf(dir);

            var newDrive = new LogicalVolume(dir);
            logicalVolumes.Add(newDrive);
            
            var scanSet = new BackupSet(dir, newDrive);
            BackupSets.Add(scanSet);
            scanSet.ScanFiles();
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
