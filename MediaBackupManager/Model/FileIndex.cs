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
        Dictionary<string, BackupFile> fileIndex = new Dictionary<string, BackupFile>();
        Dictionary<string, LogicalVolume> logicalVolumes = new Dictionary<string, LogicalVolume>();
        List<BackupSet> BackupSets = new List<BackupSet>();

        /// <summary>
        /// Adds the specified directory as new BackupSet to the file index.</summary>  
        public void AddDirectory(DirectoryInfo dir)
        {
            bool containsDir = ContainsDirectory(dir);
            bool isSubset = IsSubsetOf(dir); 

            if (!logicalVolumes.ContainsKey(dir.Root.Name))
            {
                logicalVolumes.Add(dir.Root.Name, new LogicalVolume(dir));
            }

            var scanSet = new BackupSet(this, dir, logicalVolumes[dir.Root.Name]);
            BackupSets.Add(scanSet);
            scanSet.ScanFiles();
        }

        /// <summary>
        /// Adds the specified file to the file index and returns its reference.</summary>  
        public BackupFile AddFile(string fileName)
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

        public bool ContainsDirectory(DirectoryInfo dir)
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

        public bool IsSubsetOf(DirectoryInfo dir)
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
