using MediaBackupManager.Model;
using MediaBackupManager.SupportingClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    public class FileIndexViewModel : ViewModelBase
    {
        #region Fields
        bool ignoreChanges = false;
        FileIndex index;
        FileDirectory currentDirectory;
        bool isOperationInProgress;
        ObservableCollection<BackupSetViewModel> backupSets;
        ObservableHashSet<FileHashViewModel> hashes;
        ObservableCollection<string> exclusions;

        #endregion

        #region Properties

        //TODO: Remove all direct references to the index
        public FileIndex Index
        {
            get { return index; }
            set
            {
                if (value != index)
                {
                    index = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current directory.</summary>  
        public FileDirectory CurrentDirectory
        {
            get { return currentDirectory; }
            set
            {
                if(currentDirectory != value)
                {
                    currentDirectory = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets a list of all backup sets on the file index.</summary>  
        public ObservableCollection<BackupSetViewModel> BackupSets
        {
            get { return backupSets; }
            set
            {
                if (value != backupSets)
                {
                    backupSets = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets a list of all file hashes on the file index.</summary>  
        public ObservableHashSet<FileHashViewModel> FileHashes
        {
            get { return hashes; }
            set
            {
                if (value != hashes)
                {
                    hashes = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets a list of file exclusions on the file index.</summary>  
        public ObservableCollection<string> Exclusions
        {
            get { return exclusions; }
            set
            {
                if (value != exclusions)
                {
                    exclusions = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or if a file scan or backup set deletion is currently in progress.</summary>  
        public bool IsOperationInProgress
        {
            get { return isOperationInProgress; }
            private set
            {
                if (value != isOperationInProgress)
                {
                    isOperationInProgress = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion

        #region Methods

        public FileIndexViewModel(FileIndex index)
        {
            this.index = index;
            this.BackupSets = new ObservableCollection<BackupSetViewModel>();
            this.FileHashes = new ObservableHashSet<FileHashViewModel>();
            this.Exclusions = new ObservableCollection<string>();
            foreach (var hash in index.Hashes)
                this.FileHashes.Add(new FileHashViewModel(hash));

            foreach (var set in index.BackupSets)
                this.BackupSets.Add(new BackupSetViewModel(set, this));

            foreach (var item in index.Exclusions)
                this.Exclusions.Add(item);

            index.Hashes.CollectionChanged += new NotifyCollectionChangedEventHandler(FileHashes_CollectionChanged);
            index.BackupSets.CollectionChanged += new NotifyCollectionChangedEventHandler(BackupSets_CollectionChanged);
            index.Exclusions.CollectionChanged += new NotifyCollectionChangedEventHandler(Exclusions_CollectionChanged);
        }

        private void FileHashes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ignoreChanges)
                return;

            ignoreChanges = true;

            // If the collection was reset, then e.OldItems is empty. Just clear and reload.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                FileHashes.Clear();

                foreach (var hash in index.Hashes)
                    hashes.Add(new FileHashViewModel(hash));
            }
            else
            {
                // Remove items from collection.
                var toRemove = new List<FileHashViewModel>();

                if (null != e.OldItems && e.OldItems.Count > 0)
                    foreach (var item in e.OldItems)
                        foreach (var existingItem in FileHashes)
                            if (existingItem.IsViewFor((FileHash)item))
                                toRemove.Add(existingItem);

                foreach (var item in toRemove)
                    FileHashes.Remove(item);

                // Add new items to the collection.
                if (null != e.NewItems && e.NewItems.Count > 0)
                    foreach (var item in e.NewItems)
                        FileHashes.Add(new FileHashViewModel((FileHash)item));
            }
            ignoreChanges = false;
            NotifyPropertyChanged("FileHashes");
        }

        private void BackupSets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ignoreChanges)
                return;

            ignoreChanges = true;

            // If the collection was reset, then e.OldItems is empty. Just clear and reload.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                BackupSets.Clear();

                foreach (var set in index.BackupSets)
                    BackupSets.Add(new BackupSetViewModel(set, this));
            }
            else
            {
                // Remove items from collection.
                var toRemove = new List<BackupSetViewModel>();

                if (null != e.OldItems && e.OldItems.Count > 0)
                    foreach (var item in e.OldItems)
                        foreach (var existingItem in BackupSets)
                            if (existingItem.IsViewFor((BackupSet)item))
                                toRemove.Add(existingItem);

                foreach (var item in toRemove)
                    BackupSets.Remove(item);

                // Add new items to the collection.
                if (null != e.NewItems && e.NewItems.Count > 0)
                    foreach (var item in e.NewItems)
                        BackupSets.Add(new BackupSetViewModel((BackupSet)item, this));
            }
            ignoreChanges = false;
        }

        private void Exclusions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ignoreChanges)
                return;

            ignoreChanges = true;

            // If the collection was reset, then e.OldItems is empty. Just clear and reload.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                Exclusions.Clear();

                foreach (var ex in index.Exclusions)
                    Exclusions.Add(ex);
            }
            else
            {
                // Remove items from collection.
                var toRemove = new List<string>();

                if (null != e.OldItems && e.OldItems.Count > 0)
                    foreach (var item in e.OldItems)
                        if (Exclusions.Contains(item.ToString()))
                            Exclusions.Remove(item.ToString());

                // Add new items to the collection.
                if (null != e.NewItems && e.NewItems.Count > 0)
                    foreach (var item in e.NewItems)
                        Exclusions.Add(item.ToString());
            }
            ignoreChanges = false;
        }

        /// <summary>
        /// Recursively scans the specified directory and adds it as new BackupSet to the file index.</summary>  
        /// <param name="dir">The directory thas should be scanned.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <param name="progress">Progress object used to report the progress of the operation.</param>
        /// <param name="statusText">Progress object used to report the current status of the operation.</param>
        /// <param name="label">The display name for the new backup set.</param>
        public async Task CreateBackupSetAsync(DirectoryInfo dir, CancellationToken cancellationToken, IProgress<int> progress, IProgress<string> statusText, string label = "")
        {
            IsOperationInProgress = true;

            await index.CreateBackupSetAsync(dir, cancellationToken, progress, statusText, label);

            IsOperationInProgress = false;
        }

        /// <summary>
        /// Rescans the provided BackupSet and refreshes all file hashes, nodes and the directory structure.</summary>  
        /// <param name="backupSet">The Backup Set that should be updated.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <param name="progress">Progress object used to report the progress of the operation.</param>
        /// <param name="statusText">Progress object used to report the current status of the operation.</param>
        /// <param name="label">The display name for the new backup set.</param>
        public async Task UpdateBackupSetAsync(BackupSetViewModel backupSet, CancellationToken cancellationToken, IProgress<int> progress, IProgress<string> statusText)
        {
            IsOperationInProgress = true;

            await index.UpdateBackupSetAsync(backupSet.BackupSet, cancellationToken, progress, statusText);

            IsOperationInProgress = false;
        }

        /// <summary>
        /// Removes the specified backup set and all children from the index.</summary>  
        public async Task RemoveBackupSetAsync(BackupSet backupSet)
        {
            IsOperationInProgress = true;

            await index.RemoveBackupSetAsync(backupSet, true);

            IsOperationInProgress = false;
        }

        /// <summary>
        /// Adds the default exclusions to the collection if they don't already exist.</summary>  
        public async Task RestoreDefaultExclusionsAsync()
        {
            await index.RestoreDefaultExclusionsAsync();
        }

        /// <summary>
        /// Populates the index with data stored in the database.</summary>  
        public async Task LoadDataAsync()
        {
            IsOperationInProgress = true;

            await index.LoadDataAsync();

            IsOperationInProgress = false;
        }

        /// <summary>
        /// Returns the FileHashViewModel object for the provided string.</summary>  
        public FileHashViewModel GetFileHashViewModel(string hash)
        {
            return FileHashes.FirstOrDefault(x => x.Checksum.Equals(hash));
        }

        /// <summary>
        /// Returns a list of all file nodes matching the provided search term.</summary>  
        public IEnumerable<FileNodeViewModel> FindFileNodes(string searchTerm)
        {
            return BackupSets.SelectMany(x => x.FindFileNodes(searchTerm));
        }

        /// <summary>
        /// Returns a list of all directories matching the provided search term.</summary>  
        public IEnumerable<FileDirectoryViewModel> FindDirectories(string searchTerm)
        {
            return BackupSets.SelectMany(x => x.FindDirectories(searchTerm));
        }

        /// <summary>
        /// Returns a list of all file nodes and directories matching the provided search term.</summary>  
        public List<object> FindElements(string searchTerm)
        {
            var nodes = FindFileNodes(searchTerm);
            var dirs = FindDirectories(searchTerm);



            var results = new List<object>(nodes);
            results.AddRange(dirs);


            return results;
        }

        /// <summary>
        /// Creates a new file exclusion which prevents files from being scanned if they match the provided string.</summary>  
        /// <param name="exclusion">A regex string matching a file or path name.</param>
        public async Task CreateFileExclusionAsync(string exclusion)
        {
            await index.CreateFileExclusionAsync(exclusion);
        }

        /// <summary>
        /// Removes the provided string from the file exclusion list.</summary>  
        /// <param name="exclusion">The string to be removed.</param>
        public async Task RemoveFileExclusionAsync(string exclusion)
        {
            await index.RemoveFileExclusionAsync(exclusion, true);
        }

        #endregion
    }
}
