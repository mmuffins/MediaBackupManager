using MediaBackupManager.SupportingClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Manages a collection of Backup Sets and provides an index of unique file hash objects.</summary>  
    public class FileIndex
    {
        #region Properties

        /// <summary>
        /// Gets a list of file hashes contained in the current file index.</summary>  
        public ObservableHashSet<FileHash> Hashes { get; private set; }

        /// <summary>
        /// Gets a list of logical volumes contained in the current file index.</summary>  
        public List<LogicalVolume> LogicalVolumes { get; private set; }

        /// <summary>
        /// Gets a list of Backup Sets contained in the current file index.</summary>  
        public ObservableCollection<BackupSet> BackupSets { get; private set; }

        /// <summary>
        /// Gets a list of file exclusions contained in the current file index.</summary>  
        public ObservableHashSet<string> Exclusions { get; private set; }

        #endregion

        #region Methods

        public FileIndex()
        {
            this.Hashes = new ObservableHashSet<FileHash>();
            this.LogicalVolumes = new List<LogicalVolume>();
            this.BackupSets = new ObservableCollection<BackupSet>();
            this.Exclusions = new ObservableHashSet<string>();
        }

        /// <summary>
        /// Populates the index with data stored in the database.</summary>  
        public async Task LoadDataAsync()
        {
            foreach (var ex in await Database.GetExclusionsAsync())
                await AddFileExclusionAsync(ex, false);

            foreach (var hash in await Database.GetFileHashAsync())
                AddFileHash(hash);

            var loadedSets = await Database.GetBackupSetAsync();

            // Prepare loaded sets before adding them to the index
            foreach (var set in loadedSets)
            {
                set.Index = this;

                set.Volume = (await Database.GetLogicalVolumeAsync(set.Guid.ToString())).FirstOrDefault();
                set.Volume.RefreshStatus();
                set.Volume = await AddLogicalVolume(set.Volume, false);

                // rebuild the directory tree of the set

                // load all nodes of the set
                var loadedNodes = await Database.GetFileNodeAsync(set.Guid.ToString());
                loadedNodes.Sort();

                // create a lookup collection by grouping each node by their respective directory name
                var groupedNodes = loadedNodes.GroupBy(x => x.DirectoryName);

                // iterate through all loaded node and find their respective
                // child items in the lookup collection
                foreach (var node in loadedNodes)
                {
                    node.BackupSet = set;

                    if(node is FileNode)
                    {
                        // don't look for further subdirectories if the current item is a file node
                        // instead link it with it's file hash
                        var hash = Hashes.FirstOrDefault(x => x.Checksum.Equals(((FileNode)node).Checksum));
                        if (hash != null)
                        {
                            hash.AddNode((FileNode)node);
                            ((FileNode)node).Hash = hash;
                        }
                    }
                    else
                    {
                        var childNodes = groupedNodes.FirstOrDefault(x => x.Key == (Path.Combine(node.DirectoryName, node.Name)));

                        if (node.Name == @"\")
                            childNodes = groupedNodes.FirstOrDefault(x => x.Key == (node.DirectoryName));

                        if (childNodes is null || childNodes.Count() == 0)
                            continue;

                        foreach (var childNode in childNodes)
                        {
                            // both the name and directory name for root directories are \
                            // it's not possible for these directories to have a parent, so skipt them
                            if (childNode.DirectoryName == @"\" && childNode.Name == @"\")
                                continue;

                            childNode.Parent = node;
                            if (childNode is FileNode)
                                node.FileNodes.Add((FileNode)childNode);
                            else
                                node.SubDirectories.Add(childNode);
                        }
                    }
                }

                // The only directory left without a parent is the root directory

                set.RootDirectory = loadedNodes
                    .OfType<FileDirectory>()
                    .FirstOrDefault(x => x.Parent == null && x.GetType() == typeof(FileDirectory));
            }

            foreach (var set in loadedSets)
                BackupSets.Add(set);
        }

        /// <summary>
        /// Refreshes the connected status for all logical volumes in the file index.</summary>  
        public void RefreshVolumeStatus()
        {
            LogicalVolumes.ForEach(x => x.RefreshStatus());
        }

        /// <summary>
        /// Recursively scans the specified directory and adds it as new BackupSet to the file index.</summary>  
        /// <param name="directoryPath">The directory thas should be scanned.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <param name="progress">Progress object used to report the progress of the operation.</param>
        /// <param name="statusText">Progress object used to provide feedback over the current status of the operation.</param>
        /// <param name="label">The display name for the new backup set.</param>
        public async Task<BackupSet> CreateBackupSetAsync(DirectoryInfo directoryPath, CancellationToken cancellationToken, IProgress<int> progress, IProgress<string> statusText, string label = "")
        {
            if (statusText != null)
                statusText.Report("Starting scan");

            if (FileIndexContainsDirectory(directoryPath))
            {
                MessageService.SendMessage(this, "ScanLogicException", new ApplicationException("Directory " + directoryPath.FullName + " is already indexed in another Backup Set."));
                return null;
            }

            if (IsDirectoryParentOfFileIndex(directoryPath))
            {
                MessageService.SendMessage(this, "ScanLogicException", new ApplicationException("Directory " + directoryPath.FullName + " is the parent directory of another Backup Set. Please remove the existing Set before proceeding."));
                return null;
            }

            var stagingSet = await PrepareBackupSetAsync(directoryPath, cancellationToken, progress, statusText, label);

            // No need to inform the user here, all errors have been handled inside PrepareBackupSet
            if (stagingSet is null)
                return null;

            if (statusText != null)
                statusText.Report("Hashing files");

            await stagingSet.HashFilesAsync(cancellationToken, progress, statusText);

            if (cancellationToken.IsCancellationRequested)
            {
                MessageService.SendMessage(this, "ScanLogicException", new ApplicationException("User requested to abort the scanning operation."));
                if (statusText != null)
                    statusText.Report("Operation cancelled");
                return null;
            }

            // Point of no return for the method,
            // if we cancel after this point, we risk database corruption

            if (statusText != null)
                statusText.Report("Writing Backup Set to Database");

            // At this point the staging set and all children have been properly created
            // merge it into the main list and write new data into the db
            await AppendBackupSetAsync(stagingSet);

            if (statusText != null)
                statusText.Report("Done!");

            return stagingSet;
        }

        /// <summary>
        /// Prepares a BackupSet without hashing any files.</summary>  
        /// <param name="directoryPath">The directory thas should be scanned.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <param name="progress">Progress object used to report the progress of the operation.</param>
        /// <param name="statusText">Progress object used to provide feedback over the current status of the operation.</param>
        /// <param name="label">The display name for the new backup set.</param>
        private async Task<BackupSet> PrepareBackupSetAsync(DirectoryInfo directoryPath, CancellationToken cancellationToken, IProgress<int> progress, IProgress<string> statusText, string label = "")
        {

            if (!Directory.Exists(directoryPath.FullName))
            {
                MessageService.SendMessage(this, "ScanLogicException", new ApplicationException("Could not find directory" + directoryPath.FullName));
                return null;
            }

            if (statusText != null)
                statusText.Report("Scanning logical volumes");

            var stagingVolume = new LogicalVolume(directoryPath);
            
            if (stagingVolume.SerialNumber is null || stagingVolume.MountPoint is null || stagingVolume.IsConnected == false)
            {
                MessageService.SendMessage(this, "ScanLogicException", new ApplicationException("Could not scan the logical volume for " + directoryPath.FullName));
                return null;
            }

            var stagingSet = new BackupSet(directoryPath, stagingVolume, Exclusions.ToList());
            if (!string.IsNullOrWhiteSpace(label))
                stagingSet.Label = label;

            // Up to this point the function should be fast enough that we don't need 
            // to check for task cancellation

            if (statusText != null)
                statusText.Report("Getting file list");

            await stagingSet.ScanFilesAsync(cancellationToken, statusText);

            if (cancellationToken.IsCancellationRequested)
                return null;

            // There is either an issue with the provided directory or it's
            // on the exclusion list. In either case, abort the function
            var stagingSetNodes = stagingSet.GetFileDirectories();
            stagingSetNodes.AddRange(stagingSet.GetFileNodes());
            if (stagingSetNodes is null || stagingSetNodes.Count == 0)
            {
                MessageService.SendMessage(this, "ScanLogicException", new ApplicationException("Could not access the scan directory: " + directoryPath.FullName));
                return null;
            }

            return stagingSet;
        }

        /// <summary>
        /// Rescans the provided BackupSet and refreshes all file hashes, nodes and the directory structure.</summary>  
        /// <param name="backupSet">The Backup Set that should be updated.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <param name="progress">Progress object used to report the progress of the operation.</param>
        /// <param name="statusText">Progress object used to provide feedback over the current status of the operation.</param>
        /// <param name="label">The display name for the new backup set.</param>
        public async Task UpdateBackupSetAsync(BackupSet backupSet, CancellationToken cancellationToken, IProgress<int> progress, IProgress<string> statusText)
        {
            if (statusText != null)
                statusText.Report("Checking if drive is connected");

            // Make sure that the volume is connected and the directory not deleted
            backupSet.Volume.RefreshStatus();

            if (!backupSet.Volume.IsConnected)
            {
                MessageService.SendMessage(this, "ScanLogicException", new ApplicationException("Could not verify that logical volume " + backupSet.Volume.SerialNumber + " is connected."));
                return;
            }

            var guid = backupSet.Guid;
            var rootDirectory = backupSet.RootDirectoryPath;
            var label = backupSet.Label;
            var mountPoint = backupSet.MountPoint;

            DirectoryInfo rootDirObject;

            if(rootDirectory == @"\")
                rootDirObject = new DirectoryInfo(Path.Combine(mountPoint));
            else
                rootDirObject = new DirectoryInfo(Path.Combine(mountPoint,rootDirectory));

            if (!rootDirObject.Exists)
            {
                MessageService.SendMessage(this, "ScanLogicException", new ApplicationException("Could not find directory " + rootDirObject.FullName + "."));
                return;
            }

            // We now know that the drive is connected and the directory still exists
            // Create a temporary backup set to get a list of all files
            var newSet = await PrepareBackupSetAsync(rootDirObject, cancellationToken, progress, statusText, label);

            if (cancellationToken.IsCancellationRequested)
            {
                MessageService.SendMessage(this, "ScanLogicException", new ApplicationException("User requested to abort the scanning operation."));
                if (statusText != null)
                    statusText.Report("Operation cancelled");
                return;
            }

            if (newSet is null)
                return;

            newSet.Guid = guid;

            // Hash the temporary set before attempting to remove the previous set in case the user changes his mind
            if (statusText != null)
                statusText.Report("Hashing files");

            await newSet.HashFilesAsync(cancellationToken, progress, statusText);

            if (cancellationToken.IsCancellationRequested)
            {
                MessageService.SendMessage(this, "ScanLogicException", new ApplicationException("User requested to abort the scanning operation."));
                if (statusText != null)
                    statusText.Report("Operation cancelled");
                return;
            }

            // Now that all files are hashed, remove the old backup set and append the new one
            // ignore the cancellation token in order to avoid DB corruption

            await RemoveBackupSetAsync(backupSet, true);

            if (statusText != null)
                statusText.Report("Writing changes to database");

            // At this point the staging set and all children have been properly created
            // merge it into the main list and write new data into the db
            await AppendBackupSetAsync(newSet);

            if (statusText != null)
                statusText.Report("Done!");
        }

        /// <summary>
        /// Adds the specified string collection of file exclusions.</summary>  
        /// <param name="exclusion">A regex string matching a file or path name.</param>
        /// <param name="writeToDb">If true, the object will be written to the Database.</param>
        public async Task AddFileExclusionAsync(string exclusion, bool writeToDb)
        {
            if (string.IsNullOrWhiteSpace(exclusion))
                return;

            if (Exclusions.Add(exclusion))
            {
                if (writeToDb)
                    await Database.InsertExclusionAsync(exclusion);
            }
        }

        /// <summary>
        /// Adds the specified file hash to the index.</summary>  
        public FileHash AddFileHash(FileHash hash)
        {
            // if a hash is not yet on the index, add it and return the provided pointer
            // otherwise return the pointer of the object that is already in the index
            if (Hashes.Add(hash))
                return hash;
            else
                return Hashes.FirstOrDefault(x => x.Equals(hash));
        }

        /// <summary>
        /// Adds the specified string collection of file exclusions.</summary>  
        /// <param name="writeToDb">If true, the object will be written to the Database.</param>
        public async Task<LogicalVolume> AddLogicalVolume(LogicalVolume volume, bool writeToDb)
        {
            if (LogicalVolumes.Contains(volume))
                return LogicalVolumes.First(x => x.Equals(volume));

            LogicalVolumes.Add(volume);
            if (writeToDb) await Database.InsertLogicalVolumeAsync(volume);

            return volume;
        }

        /// <summary>
        /// Adds the provided Backup set to the collection.</summary>  
        /// <param name="writeToDb">If true, the object will be written to the Database.</param>
        private async Task AddBackupSetAsync(BackupSet backupSet, bool writeToDb)
        {
            BackupSets.Add(backupSet);

            if (writeToDb)
                await Database.InsertBackupSetAsync(backupSet);
        }

        /// <summary>
        /// Removes the specified backup set and all children from the index.</summary>  
        /// <param name="writeToDb">If true, the object will be removed from the Database.</param>
        public async Task RemoveBackupSetAsync(BackupSet set, bool writeToDb)
        {
            if (set is null)
                return;

            if(BackupSets.Where(x => x.Volume.Equals(set.Volume)).Count() < 2)
            {
                // No other backup set shares the logical volume of the 
                // set that's about to be deleted, it can therefore be removed
                LogicalVolumes.Remove(set.Volume);
                if(writeToDb)
                    await Database.DeleteLogicalVolumeAsync(set.Volume);
            }

            // Get a list of all hashes related to the current set,
            // remove all nodes from these hashes.
            var setHashes = set.GetFileHashes();
            await set.ClearAsync();
            set.Dispose();

            // Get hashes in the collection with node count 0 
            // these can be removed from the index
            var emptyHashes = setHashes.Where(x => x.NodeCount.Equals(0)).ToList();
            emptyHashes.ForEach(x => Hashes.Remove(x));

            if (writeToDb)
                await Database.BatchDeleteFileHashAsync(emptyHashes);

            BackupSets.Remove(set);
            //NotifyPropertyChanged("BackupSet");
            if(writeToDb)
                await Database.DeleteBackupSetAsync(set);
        }

        /// <summary>
        /// Removes the specified exclusion.</summary>  
        /// <param name="writeToDb">If true, the object will be removed from the Database.</param>
        public async Task RemoveFileExclusionAsync(string exclusion, bool writeToDb)
        {
            if (Exclusions.Contains(exclusion))
            {
                Exclusions.Remove(exclusion);

                if (writeToDb)
                    await Database.DeleteExclusionAsync(exclusion);
            }
        }

        /// <summary>
        /// Determines whether the provided directory is already indexed in one of the backup sets.</summary>  
        public bool FileIndexContainsDirectory(DirectoryInfo dir)
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
        public bool IsDirectoryParentOfFileIndex(DirectoryInfo dir)
        {
            bool result = false;

            foreach (var set in BackupSets)
            {
                if (set.IsParentDirectory(dir))
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
            await AddFileExclusionAsync(@".*usrclass.dat.log.*", true);
            await AddFileExclusionAsync(@".*\$RECYCLE\.BIN*", true);
            await AddFileExclusionAsync(@".*System Volume Information*", true);
            await AddFileExclusionAsync(@".*\.lnk", true);
            await AddFileExclusionAsync(@".*thumbs\.db", true);
        }

        /// <summary>
        /// Appends the provided BackupSet to the index and writes new elements into the Database.</summary>  
        private async Task AppendBackupSetAsync(BackupSet stagingSet)
        {
            // Hashes
            // To prevent any issues, rebuild the hash index by looping through each filenode and
            // reapplying the correct values/relations
            var newHashes = new List<FileHash>();

            foreach (var file in stagingSet.GetFileNodes())
            {
                if (file.Hash is null)
                    continue;

                // For each hash in the staging set, check if it already exists in the file index.
                // If so, update the pointer of the file node
                // in the staging set to use the existing object. If not, add the hash to the index

                if (Hashes.Contains(file.Hash))
                {
                    file.Hash = AddFileHash(file.Hash);
                    file.Hash.AddNode(file);
                }
                else
                {
                    AddFileHash(file.Hash);
                    newHashes.Add(file.Hash);
                }

            }
            await Database.BatchInsertFileHashAsync(newHashes);

            // All filenodes are unique to a backup set so they can be added to the DB in any case
            var dbInsertList = stagingSet.GetFileDirectories();
            dbInsertList.AddRange(stagingSet.GetFileNodes());
            await Database.BatchInsertFileNodeAsync(dbInsertList);

            stagingSet.Volume = await AddLogicalVolume(stagingSet.Volume, true);

            await AddBackupSetAsync(stagingSet, true);
        }

        #endregion
    }
}
