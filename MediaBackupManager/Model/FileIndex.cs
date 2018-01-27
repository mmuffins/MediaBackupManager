using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public async Task LoadDataAsync()
        {
            this.Exclusions = new HashSet<string>(await Database.GetExclusionsAsync());
            this.Hashes = (await Database.GetFileHashAsync()).ToDictionary(x => x.Checksum);
            this.BackupSets = await Database.GetBackupSetAsync();

            foreach (var set in BackupSets)
            {
                set.Index = this;

                // Add the logical volume to the collection and
                // refresh its mountpoint
                set.Volume = (await Database.GetLogicalVolumeAsync(set.Guid.ToString())).FirstOrDefault();
                if (LogicalVolumes.Contains(set.Volume))
                {
                    set.Volume = LogicalVolumes.Where(x => x.SerialNumber.Equals(set.Volume.SerialNumber)).FirstOrDefault();
                }
                else
                {
                    LogicalVolumes.Add(set.Volume);
                    RefreshMountPoint(set.Volume);
                }

                // Load all nodes for the set, then iterate through each item
                // and rebuild the relationship between nodes and hashes
                foreach (var item in await Database.GetFileNodeAsync(set.Guid.ToString()))
                {
                    item.BackupSet = set;
                    set.FileNodes.Add(item);

                    if(item is FileNode)
                    {
                        FileHash hash;
                        if (Hashes.TryGetValue(((FileNode)item).Checksum, out hash))
                        {
                            hash.AddNode((FileNode)item);
                            ((FileNode)item).Hash = hash;
                        }

                    }
                }

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
        public async Task CreateBackupSetAsync(DirectoryInfo dir)
        {
            if (!Directory.Exists(dir.FullName))
                return;

            //TODO: Promt the user on what to do when the directory is already indexed
            if (ContainsDirectory(dir) || IsSubsetOf(dir))
                return;

            var stagingVolume = new LogicalVolume(dir);
            var stagingSet = new BackupSet(dir, stagingVolume, Exclusions);

            //AddBackupSet(scanSet);
            await stagingSet.ScanFilesAsync();

            //TODO: Inform the user if he tries to add a root directory on the exclusion list
            // There is either an issue with the provided directory or it's
            // on the exclusion list. In either case, abort the function
            if (stagingSet.FileNodes.Count == 0)
                return;

            await stagingSet.HashFilesAsync();

            // At this point the staging set and all children have been properly created
            // merge it into the main list and write new data into the db

            await AppendBackupSetAsync(stagingSet);

        }

        /// <summary>
        /// Adds the specified string collection of file exclusions.</summary>  
        private async Task AddExclusionAsync(string exclusion)
        {
            if (Exclusions.Add(exclusion))
            {
                await Database.InsertExclusionAsync(exclusion);
            }
        }

        /// <summary>
        /// Adds the specified file to the file index and returns its reference.</summary>  
        public FileHash AddHash(FileHash file)
        {
            if (Hashes.ContainsKey(file.Checksum))
            {
                return Hashes[file.Checksum];
            }
            else
            {
                Hashes.Add(file.Checksum, file);
                //Database.InsertFileHash(file);
                return file;
            }
        }

        /// <summary>
        /// Removes a file node from the index.</summary>  
        public void RemoveFileNode(FileNode node)
        {
            FileHash removeFile;
            if (Hashes.TryGetValue(node.Hash.Checksum, out removeFile))
                removeFile.RemoveNode(node);
        }

        /// <summary>
        /// Removes the specified backup set and all children from the index.</summary>  
        public async Task RemoveBackupSetAsync(BackupSet set)
        {
            if(BackupSets.Where(x => x.Volume.Equals(set.Volume)).Count() < 2)
            {
                // No other backup set shares the logical volume of the 
                // set that's about to be deleted, it can therefore be removed
                LogicalVolumes.Remove(set.Volume);
                await Database.DeleteLogicalVolumeAsync(set.Volume);
            }

            // Get a list of all hashes related to the current set,
            // remove all nodes from these hashes.
            var setHashes = set.GetFileHashes();
            await set.ClearAsync();

            // All hashes in the collection without any other node 
            // can be clearly removed from the index
            await Database.BatchDeleteFileHashAsync(setHashes.Where(x => x.NodeCount.Equals(0)).ToList());

            BackupSets.Remove(set);
            await Database.DeleteBackupSetAsync(set);
        }

        /// <summary>
        /// Removes the specified exclusion.</summary>  
        public async Task RemoveExclusionAsync(string exclusion)
        {
            Exclusions.Remove(exclusion);
            await Database.DeleteExclusionAsync(exclusion);
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
        public async Task RestoreDefaultExclusionsAsync()
        {
            //TODO:Remove test exclusions before going live
            await AddExclusionAsync(@".*usrclass.dat.log.*");
            await AddExclusionAsync(@".*\\nzb.*");
            await AddExclusionAsync(@".*\\filme.*");
            await AddExclusionAsync(@".*\.zip");
        }

        /// <summary>
        /// Appends the provided BackupSet to the index and writes new elements into the Database.</summary>  
        public async Task AppendBackupSetAsync(BackupSet stagingSet)
        {
            // Hashes
            // To prevent any issues, rebuild the hash index by looping through each filenode and
            // reapplying the correct values/relations
            var newHashes = new List<FileHash>();

            foreach (var file in stagingSet.FileNodes.OfType<FileNode>())
            {
                if (file.Hash is null)
                    continue;

                FileHash hash;
                if (Hashes.TryGetValue(file.Hash.Checksum, out hash))
                {
                    // Hash is already on the index, change the reference of the file node
                    // and add the new node location to the hash
                    hash.AddNode(file);
                    file.Hash = hash;
                }
                else
                {
                    Hashes.Add(file.Hash.Checksum, file.Hash);
                    newHashes.Add(file.Hash); 
                }
            }
            await Database.BatchInsertFileHashAsync(newHashes);

            // All filenodes are unique to a backup set so they can be added to the DB in any case
            await Database.BatchInsertFileNodeAsync(stagingSet.FileNodes.ToList());

            if (!LogicalVolumes.Contains(stagingSet.Volume))
            {
                LogicalVolumes.Add(stagingSet.Volume);
                await Database.InsertLogicalVolumeAsync(stagingSet.Volume);
            }
            else
            {
                // Update the reference to make sure the set points to the correct object
                stagingSet.Volume = LogicalVolumes.FirstOrDefault((x => x.Equals(stagingSet.Volume)));
            }

            BackupSets.Add(stagingSet);
            await Database.InsertBackupSetAsync(stagingSet);
        }

        #endregion
    }
}
