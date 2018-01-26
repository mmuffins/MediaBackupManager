using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Manages a collection of FileHash objects.</summary>  

    public class FileIndex
    {
        #region Fields

        //private Dictionary<string, FileHash> hashes = new Dictionary<string, FileHash>();
        //private List<LogicalVolume> logicalVolumes = new List<LogicalVolume>();
        //private List<BackupSet> backupSets = new List<BackupSet>();
        //private HashSet<string> exclusions = new HashSet<string>();

        #endregion

        #region Properties

        public Dictionary<string, FileHash> Hashes { get; private set; }
        public List<LogicalVolume> LogicalVolumes { get; private set; }
        public List<BackupSet> BackupSets { get; private set; }
        public HashSet<string> Exclusions { get; private set; }

        #endregion

        #region Methods
        public FileIndex()
        {
            //LoadData();
            //Database.CreateDatabase();
            this.Hashes = new Dictionary<string, FileHash>();
            this.LogicalVolumes = new List<LogicalVolume>();
            this.BackupSets = new List<BackupSet>();
            this.Exclusions = new HashSet<string>();
        }

        /// <summary>
        /// Populates the index with data stored in the database.</summary>  
        public void LoadData()
        {
            this.Exclusions = new HashSet<string>(Database.GetExclusions());

            this.BackupSets = Database.GetBackupSet();

            foreach (var set in BackupSets)
            {
                // Add the logical volume to the collection and
                // refresh its mountpoint
                if (!LogicalVolumes.Contains(set.Volume))
                {
                    LogicalVolumes.Add(set.Volume);
                    RefreshMountPoint(set.Volume);
                }

                // populate the set with filenodes and add 
                // the related hashes to the index                
                Database.LoadBackupSetNodes(set);
            }
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
            //TODO: Promt the user on what to do when the directory is already indexed
            if (ContainsDirectory(dir) || IsSubsetOf(dir))
                return;

            //TODO: Inform the user if he tries to add a root directory on the exclusion list
            if (IsFileExcluded(dir.FullName))
                return;

            var newDrive = new LogicalVolume(dir);
            AddLogicalVolume(newDrive);
            
            var scanSet = new BackupSet(dir, newDrive, this);
            AddBackupSet(scanSet);
            scanSet.ScanFiles();
        }

        /// <summary>
        /// Adds the specified directory as new BackupSet to the file index.</summary>  
        public async Task IndexDirectoryAsync(DirectoryInfo dir)
        {
            //TODO: Promt the user on what to do when the directory is already indexed
            if (ContainsDirectory(dir) || IsSubsetOf(dir))
                return;

            //TODO: Inform the user if he tries to add a root directory on the exclusion list
            if (IsFileExcluded(dir.FullName))
                return;

            var newDrive = new LogicalVolume(dir);
            AddLogicalVolume(newDrive);

            var scanSet = new BackupSet(dir, newDrive, this);
            AddBackupSet(scanSet);
            await scanSet.ScanFilesAsync();
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
        /// Adds the specified string collection of file exclusions.</summary>  
        private void AddExclusion(string exclusion)
        {
            if (Exclusions.Add(exclusion))
            {
                Database.InsertExclusion(exclusion);
            }
        }

        /// <summary>
        /// Adds the specified file to the file index and returns its reference.</summary>  
        public FileHash IndexFile(string fileName)
        {

            string checkSum;
            try { checkSum = FileHash.CalculateChecksum(fileName); }
            catch (Exception)
            {
                // The file couldn't be hashed for some reason, don't add it to the index
                //TODO: Inform the user that something went wrong
                return null;
            }

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
        public async Task<FileHash> IndexFileAsync(string fileName)
        {
            string checkSum;
            try { checkSum = await Task<string>.Run(() => FileHash.CalculateChecksum(fileName)); }
            catch (Exception)
            {
                // The file couldn't be hashed for some reason, don't add it to the index
                //TODO: Inform the user that something went wrong
                return null;
            }

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
                if (removeFile.NodeCount <= 0)
                    RemoveHash(removeFile);
            }
        }

        /// <summary>
        /// Removes the specified backup set and all children from the index.</summary>  
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
        public void RemoveLogicalVolume(LogicalVolume volume)
        {
            LogicalVolumes.Remove(volume);
            Database.DeleteLogicalVolume(volume);
        }

        /// <summary>
        /// Removes the specified exclusion.</summary>  
        public void RemoveExclusion(string exclusion)
        {
            Exclusions.Remove(exclusion);
            Database.DeleteExclusion(exclusion);
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

        /// <summary>
        /// Adds the default exclusions to the collection if they don't already exist.</summary>  
        public void RestoreDefaultExclusions()
        {
            //TODO:Remove test exclusions before going live
            AddExclusion(@".*usrclass.dat.log.*");
            AddExclusion(@".*\\nzb.*");
            AddExclusion(@".*\\filme.*");
            AddExclusion(@".*\.zip");
        }

        /// <summary>
        /// Determines whether the provided file or directory is excluded based on the file exclusion list.</summary>  
        public bool IsFileExcluded(string path)
        {
            foreach (var item in Exclusions)
            {
                if (Regex.IsMatch(path, item, RegexOptions.IgnoreCase))
                    return true;

                //var dd = Regex.IsMatch("F:\\Archive", ".*\\\\archive.*", RegexOptions.IgnoreCase);
                //dd = Regex.IsMatch("F:\\SomeDir\\Archive", ".*\\\\archive.*", RegexOptions.IgnoreCase);
                //dd = Regex.IsMatch("F:\\SomeDir\\Archive\file.zip", ".*\\.zip.*", RegexOptions.IgnoreCase);
            }
            return false;
        }

        #endregion
    }
}
