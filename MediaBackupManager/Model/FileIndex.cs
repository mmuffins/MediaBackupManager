using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Manages a collection of FileHash objects.</summary>  

    public class FileIndex
    {
        Dictionary<string, FileHash> hashes = new Dictionary<string, FileHash>();
        public  Dictionary<string, FileHash> Hashes { get => hashes; }

        List<LogicalVolume> logicalVolumes = new List<LogicalVolume>();
        public List<LogicalVolume> LogicalVolumes { get => logicalVolumes; }

        List<BackupSet> backupSets = new List<BackupSet>();
        public List<BackupSet> BackupSets { get => backupSets; }

        static FileIndex()
        {
            //LoadData();
            Database.CreateDatabase();
        }

        /// <summary>
        /// Populates the index with data stored in the database.</summary>  
        public void LoadData()
        {
            // Add the logical volume to the collection and
            // refresh its mountpoint, then
            // populate the set with filenodes and add 
            // the related hashes to the index

            this.backupSets = Database.GetBackupSet();

            foreach (var set in BackupSets)
            {
                if (!LogicalVolumes.Contains(set.Volume))
                {
                    LogicalVolumes.Add(set.Volume);
                    RefreshMountPoint(set.Volume);
                }

                Database.LoadBackupSetNodes(set);
            }
        }

        /// <summary>
        /// Populates the specified BackupSet with data stored in the database.</summary>  
        private void LoadBackupSetData()
        {

        }

        /// <summary>
        /// Refreshes the mount points for a logical volume in the collection.</summary>  
        private void RefreshMountPoint(LogicalVolume volume)
        {
            DriveInfo mountPoint = volume.GetMountPoint();
            if(!(mountPoint is null))
            {
                volume.MountPoint = mountPoint.Name;
            }
        }

        /// <summary>
        /// Adds the specified directory as new BackupSet to the file index.</summary>  
        public void IndexDirectory(DirectoryInfo dir)
        {
            //TODO: Give the user a choice here
            if (ContainsDirectory(dir) || IsSubsetOf(dir))
                return;

            var newDrive = new LogicalVolume(dir);
            AddLogicalVolume(newDrive);
            
            var scanSet = new BackupSet(dir, newDrive, this);
            AddBackupSet(scanSet);
            scanSet.ScanFiles();
        }

        /// <summary>
        /// Adds the specified backup set to the local collection.</summary>  
        private void AddBackupSet(BackupSet backupSet)
        {
            BackupSets.Add(backupSet);
            Database.InsertBackupSet(backupSet);
        }

        /// <summary>
        /// Adds the specified logical volume to the local collection.</summary>  
        private void AddLogicalVolume(LogicalVolume logicalVolume)
        {
            if (!LogicalVolumes.Contains(logicalVolume))
            {
                LogicalVolumes.Add(logicalVolume);
                Database.InsertLogicalVolume(logicalVolume);
            }
        }

        /// <summary>
        /// Adds the specified file to the file index and returns its reference.</summary>  
        public FileHash IndexFile(string fileName)
        {
            string checkSum = FileHash.CalculateChecksum(fileName);

            if (Hashes.ContainsKey(checkSum))
            {
                return Hashes[checkSum];
            }
            else
            {
                var newFile = new FileHash(fileName, checkSum);
                Hashes.Add(checkSum, newFile);
                Database.InsertFileHash(newFile);
                return newFile;
            }
        }

        /// <summary>
        /// Adds the specified file to the file index and returns its reference.</summary>  
        public FileHash IndexFile(FileHash file)
        {
            if (Hashes.ContainsKey(file.CheckSum))
            {
                return Hashes[file.CheckSum];
            }
            else
            {
                Hashes.Add(file.CheckSum, file);
                Database.InsertFileHash(file);
                return file;
            }
        }

        /// <summary>
        /// Removes the specified hash from the file index.</summary>  
        public void RemoveHash(FileHash hash)
        {
            Hashes.Remove(hash.CheckSum);
            Database.DeleteFileHash(hash);
        }

        /// <summary>
        /// Removes a file node from the index. The related hash will be automatically removed if the last file node was removed.</summary>  
        public void RemoveFileNode(FileNode node)
        {
            FileHash removeFile;
            if(Hashes.TryGetValue(node.File.CheckSum, out removeFile))
            {
                removeFile.RemoveNode(node);
                if (removeFile.NodeCount > 1)
                    RemoveHash(removeFile);
            }
        }

        /// <summary>
        /// Removes the specified backup set and all children from the index.</summary>  
        /// <param name="item">The object to be removed.</param>
        public void RemoveBackupSet(BackupSet item)
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
        public void RemoveLogicalVolume(LogicalVolume volume)
        {
            LogicalVolumes.Remove(volume);
            Database.DeleteLogicalVolume(volume);
        }

        /// <summary>
        /// Determines whether the provided directory is already indexed in one of the backup sets.</summary>  
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

        /// <summary>
        /// Determines whether the provided directory is a parent of one of the backup sets.</summary>  
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
