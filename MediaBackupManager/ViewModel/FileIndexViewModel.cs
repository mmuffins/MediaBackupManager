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
        ObservableCollection<BackupSetViewModel> backupSets;
        FileDirectory currentDirectory;
        ObservableHashSet<FileHashViewModel> hashes;
        ObservableCollection<string> exclusions;


        #endregion

        #region Properties

        public FileIndex Index { get; }

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

        public ObservableCollection<BackupSetViewModel> BackupSets
        {
            get { return backupSets; }
            set
            {
                if (value != backupSets)
                {
                    backupSets = value;
                    NotifyPropertyChanged("");
                }
            }
        }

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


        #endregion

        #region Methods

        public FileIndexViewModel(FileIndex index)
        {
            this.Index = index;
            this.BackupSets = new ObservableCollection<BackupSetViewModel>();
            this.FileHashes = new ObservableHashSet<FileHashViewModel>();
            this.Exclusions = new ObservableCollection<string>();
            foreach (var hash in Index.Hashes)
                this.FileHashes.Add(new FileHashViewModel(hash));

            foreach (var set in Index.BackupSets)
                this.BackupSets.Add(new BackupSetViewModel(set, this));

            foreach (var item in Index.Exclusions)
                this.Exclusions.Add(item);

            //Index.PropertyChanged += new PropertyChangedEventHandler(OnIndexPropertyChanged);
            Index.Hashes.CollectionChanged += new NotifyCollectionChangedEventHandler(FileHashes_CollectionChanged);
            Index.BackupSets.CollectionChanged += new NotifyCollectionChangedEventHandler(BackupSets_CollectionChanged);
            Index.Exclusions.CollectionChanged += new NotifyCollectionChangedEventHandler(Exclusions_CollectionChanged);
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

                foreach (var hash in Index.Hashes)
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

                foreach (var set in Index.BackupSets)
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

                foreach (var ex in Index.Exclusions)
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

        public async Task CreateBackupSetAsync(DirectoryInfo dir, CancellationToken cancellationToken, IProgress<int> progress, IProgress<string> statusText, string label = "")
        {

            await Index.CreateBackupSetAsync(dir, cancellationToken, progress, statusText, label);
        }

        public async Task RemoveBackupSetAsync(BackupSet backupSet)
        {
            await Index.RemoveBackupSetAsync(backupSet, true);
        }

        /// <summary>
        /// Adds the default exclusions to the collection if they don't already exist.</summary>  
        public async Task RestoreDefaultExclusionsAsync()
        {
            await Index.RestoreDefaultExclusionsAsync();
        }

        /// <summary>
        /// Populates the index with data stored in the database.</summary>  
        public async Task LoadDataAsync()
        {
            await Index.LoadDataAsync();
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
            await Index.CreateFileExclusionAsync(exclusion);
        }

        /// <summary>
        /// Removes the provided string from the file exclusion list.</summary>  
        /// <param name="exclusion">The string to be removed.</param>
        public async Task RemoveFileExclusionAsync(string exclusion)
        {
            await Index.RemoveFileExclusionAsync(exclusion, true);
        }

        #endregion

        #region Implementations

        #endregion


    }
}
