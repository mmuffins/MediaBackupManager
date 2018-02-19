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
        ObservableCollection<ArchiveViewModel> archives;
        ObservableHashSet<FileHashViewModel> hashes;
        ObservableCollection<string> exclusions;

        #endregion

        #region Properties

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
        /// Gets a list of all archives on the file index.</summary>  
        public ObservableCollection<ArchiveViewModel> Archives
        {
            get { return archives; }
            set
            {
                if (value != archives)
                {
                    archives = value;
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
        /// Gets or if a file scan or archive deletion is currently in progress.</summary>  
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
            this.Archives = new ObservableCollection<ArchiveViewModel>();
            this.FileHashes = new ObservableHashSet<FileHashViewModel>();
            this.Exclusions = new ObservableCollection<string>();
            foreach (var hash in index.Hashes)
                this.FileHashes.Add(new FileHashViewModel(hash));

            foreach (var archive in index.Archives)
                this.Archives.Add(new ArchiveViewModel(archive, this));

            foreach (var item in index.Exclusions)
                this.Exclusions.Add(item);

            index.Hashes.CollectionChanged += new NotifyCollectionChangedEventHandler(FileHashes_CollectionChanged);
            index.Archives.CollectionChanged += new NotifyCollectionChangedEventHandler(Archives_CollectionChanged);
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

        private void Archives_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ignoreChanges)
                return;

            ignoreChanges = true;

            // If the collection was reset, then e.OldItems is empty. Just clear and reload.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                Archives.Clear();

                foreach (var archive in index.Archives)
                    Archives.Add(new ArchiveViewModel(archive, this));
            }
            else
            {
                // Remove items from collection.
                var toRemove = new List<ArchiveViewModel>();

                if (null != e.OldItems && e.OldItems.Count > 0)
                    foreach (var item in e.OldItems)
                        foreach (var existingItem in Archives)
                            if (existingItem.IsViewFor((Archive)item))
                                toRemove.Add(existingItem);

                foreach (var item in toRemove)
                    Archives.Remove(item);

                // Add new items to the collection.
                if (null != e.NewItems && e.NewItems.Count > 0)
                    foreach (var item in e.NewItems)
                        Archives.Add(new ArchiveViewModel((Archive)item, this));
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
        /// Recursively scans the specified directory and adds it as new Archive to the file index.</summary>  
        /// <param name="dir">The directory thas should be scanned.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <param name="progress">Progress object used to report the progress of the operation.</param>
        /// <param name="statusText">Progress object used to provide feedback over the current status of the operation.</param>
        /// <param name="label">The display name for the new archive.</param>
        public async Task CreateArchiveAsync(DirectoryInfo dir, CancellationToken cancellationToken, IProgress<int> progress, IProgress<string> statusText, string label = "")
        {
            IsOperationInProgress = true;

            await index.CreateArchiveAsync(dir, cancellationToken, progress, statusText, label);

            IsOperationInProgress = false;
        }

        /// <summary>
        /// Rescans the provided Archive and refreshes all file hashes, nodes and the directory structure.</summary>  
        /// <param name="archive">The Archive that should be updated.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <param name="progress">Progress object used to report the progress of the operation.</param>
        /// <param name="statusText">Progress object used to provide feedback over the current status of the operation.</param>
        /// <param name="label">The display name for the new archive.</param>
        public async Task UpdateArchiveAsync(ArchiveViewModel archive, CancellationToken cancellationToken, IProgress<int> progress, IProgress<string> statusText)
        {
            IsOperationInProgress = true;

            await index.UpdateArchiveAsync(archive.Archive, cancellationToken, progress, statusText);

            IsOperationInProgress = false;
        }

        /// <summary>
        /// Removes the specified archive and all children from the index.</summary>  
        public async Task RemoveArchiveAsync(Archive archive)
        {
            IsOperationInProgress = true;

            await index.RemoveArchiveAsync(archive, true);

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
        /// Refreshes the connected status for all logical volumes in the file index.</summary>  
        public async Task RefreshVolumeStatus()
        {
            await Task.Run(() => index.RefreshVolumeStatus());
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
            return Archives.SelectMany(x => x.FindFileNodes(searchTerm));
        }

        /// <summary>
        /// Returns a list of all directories matching the provided search term.</summary>  
        public IEnumerable<FileDirectoryViewModel> FindDirectories(string searchTerm)
        {
            return Archives.SelectMany(x => x.FindDirectories(searchTerm));
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
        public async Task AddFileExclusionAsync(string exclusion)
        {
            await index.AddFileExclusionAsync(exclusion, true);
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
